"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import {
  StandardDateField,
  StandardSelectField,
  StandardTimeField,
} from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Lead = { id: string; fullName: string; programInterest?: string | null };
type Teacher = { id: string; firstName: string; lastName: string };
type Trial = {
  id: string;
  leadId: string;
  teacherId?: string | null;
  scheduledAtUtc: string;
  status: string;
  notes?: string | null;
};
const trialStatuses = ["Booked", "Completed", "Cancelled", "NoShow"];
const statusLabel = (status: string) =>
  status === "NoShow" ? "No show" : status;

async function fetchTrials(id: string) {
  const responses = await Promise.all([
    academyApi(`/api/academies/${id}/leads`, { cache: "no-store" }),
    academyApi(`/api/academies/${id}/teachers`, { cache: "no-store" }),
    academyApi(`/api/academies/${id}/sales-marketing/trials`, { cache: "no-store" }),
  ]);
  if (!responses.every((response) => response.ok)) throw new Error();
  const rows: unknown[] = await Promise.all(responses.map((response) => response.json()));
  if (!rows.every(Array.isArray)) throw new Error();
  return { leads: rows[0] as Lead[], teachers: rows[1] as Teacher[], trials: rows[2] as Trial[] };
}

export default function TrialBookingsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [leads, setLeads] = useState<Lead[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [trials, setTrials] = useState<Trial[]>([]);
  const [message, setMessage] = useState("Loading trial bookings…");
  const [leadId, setLeadId] = useState("");
  const [teacherId, setTeacherId] = useState("");
  const [scheduledDate, setScheduledDate] = useState("");
  const [scheduledTime, setScheduledTime] = useState("10:00");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) throw new Error();
        const data = await fetchTrials(academies[0].id);
        if (!active) return;
        setAcademy(academies[0]);
        setLeads(data.leads);
        setTeachers(data.teachers);
        setTrials(data.trials);
        setMessage("");
      } catch {
        if (!active) return;
        setMessage(
          "Trial bookings could not be loaded. Please refresh or contact your administrator.",
        );
      }
    })();
    return () => { active = false; };
  }, []);
  async function mutate(action: () => Promise<Response>, success: string, failure: string, reset?: () => void) {
    if (!academy || pending.current) return;
    pending.current = true;
    setSaving(true);
    setMessage("");
    const uncertain = "The trial change could not be confirmed. Your draft has been retained. Check the trial register before retrying.";
    try {
      const response = await action();
      if (!response.ok) {
        setMessage(response.status >= 500 ? uncertain : failure);
        return;
      }
      reset?.();
      setMessage(success);
      try {
        const data = await fetchTrials(academy.id);
        setLeads(data.leads);
        setTeachers(data.teachers);
        setTrials(data.trials);
      } catch {
        setMessage(`${success} The trial register could not be refreshed; do not repeat the action. Refresh the page to see the latest data.`);
      }
    } catch {
      setMessage(uncertain);
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || pending.current) return;
    if (!leadId || !scheduledDate)
      return setMessage("Select a lead and date for the trial.");
    await mutate(() => academyApi(
      `/api/academies/${academy.id}/sales-marketing/trials`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          leadId,
          teacherId: teacherId || null,
          scheduledAtUtc: new Date(
            `${scheduledDate}T${scheduledTime}:00`,
          ).toISOString(),
          notes: notes || null,
        }),
      },
    ), "Trial class booked.", "Trial class could not be booked. Select a lead and a valid time. Your draft has been retained.", () => {
      setLeadId("");
      setTeacherId("");
      setScheduledDate("");
      setScheduledTime("10:00");
      setNotes("");
    });
  }
  async function update(trial: Trial, status: string) {
    if (!academy) return;
    await mutate(() => academyApi(
      `/api/academies/${academy.id}/sales-marketing/trials/${trial.id}/status`,
      {
        method: "PATCH",
        headers: apiHeaders(true),
        body: JSON.stringify({ status }),
      },
    ), `Trial status updated to ${statusLabel(status)}.`, "Trial status could not be updated. Your draft has been retained.");
  }
  const leadName = (id: string) =>
    leads.find((lead) => lead.id === id)?.fullName ?? "Lead";
  const teacherName = (id?: string | null) => {
    const teacher = teachers.find((item) => item.id === id);
    return teacher ? `${teacher.firstName} ${teacher.lastName}` : "Unassigned";
  };
  return (
    <main className="enterprise-settings trials-standard min-h-screen">
      <WorkspaceNav />
      <div className="trials-content mx-auto max-w-6xl px-6 py-10">
        <header className="trials-heading">
          <div className="trials-title">
            <span className="trials-title-icon" aria-hidden="true">
              ◌
            </span>
            <div>
              <p>Sales &amp; marketing</p>
              <h1>Trial-Class Bookings</h1>
            </div>
          </div>
        </header>
        {message && (
          <p role="status" aria-live="polite" className="enterprise-page-state trials-message">{message}</p>
        )}
        <section className="trials-layout">
          <form onSubmit={create} className="trials-panel">
            <header className="trials-panel-header">
              <div>
                <p>New booking</p>
                <h2>Book trial class</h2>
              </div>
            </header>
            <fieldset disabled={!academy || saving} className="trials-fields min-w-0 border-0 m-0">
              <StandardSelectField
                name="trial-lead"
                disabled={!academy || saving}
                value={leadId}
                onChange={setLeadId}
                placeholder="Select lead"
                options={leads.map((lead) => ({
                  value: lead.id,
                  label: `${lead.fullName}${lead.programInterest ? ` · ${lead.programInterest}` : ""}`,
                }))}
              />
              <StandardSelectField
                name="trial-teacher"
                disabled={!academy || saving}
                value={teacherId}
                onChange={setTeacherId}
                placeholder="No teacher assigned"
                options={teachers.map((teacher) => ({
                  value: teacher.id,
                  label: `${teacher.firstName} ${teacher.lastName}`,
                }))}
              />
              <div className="trials-fields-two">
                <StandardDateField
                  name="trial-date"
                  label="Trial date"
                  value={scheduledDate}
                  onChange={setScheduledDate}
                  required
                />
                <StandardTimeField
                  name="trial-time"
                  label="Start time"
                  value={scheduledTime}
                  onChange={setScheduledTime}
                />
              </div>
              <label>
                <span>Notes</span>
                <textarea
                  value={notes}
                  onChange={(event) => setNotes(event.target.value)}
                  placeholder="Notes for the trial"
                />
              </label>
              <button
                className="enterprise-action-button trials-book-button"
                disabled={!academy || saving}
              >
                {saving ? "Saving…" : "Book trial class"}
              </button>
            </fieldset>
          </form>
          <section className="trials-panel trials-list-panel">
            <header className="trials-panel-header">
              <div>
                <p>Schedule</p>
                <h2>Trial classes</h2>
              </div>
              <span>{trials.length} total</span>
            </header>
            {trials.length === 0 ? (
              <p className="trials-empty">No trial classes booked yet.</p>
            ) : (
              <ul>
                {trials.map((trial) => (
                  <li key={trial.id}>
                    <div>
                      <b>{leadName(trial.leadId)}</b>
                      <small>
                        {new Intl.DateTimeFormat("en-IN", {
                          dateStyle: "medium",
                          timeStyle: "short",
                          timeZone: "Asia/Kolkata",
                        }).format(new Date(trial.scheduledAtUtc))}{" "}
                        IST · {teacherName(trial.teacherId)}
                      </small>
                      {trial.notes && <small>{trial.notes}</small>}
                    </div>
                    <StandardSelectField
                      name={`trial-status-${trial.id}`}
                      disabled={!academy || saving}
                      value={trial.status}
                      onChange={(status) => void update(trial, status)}
                      placeholder="Status"
                      options={trialStatuses.map((value) => ({
                        value,
                        label: statusLabel(value),
                      }))}
                    />
                  </li>
                ))}
              </ul>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
