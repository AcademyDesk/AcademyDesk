"use client";

import { FormEvent, useEffect, useState } from "react";
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
  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [leadResponse, teacherResponse, trialResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/leads`),
      academyApi(`/api/academies/${id}/teachers`),
      academyApi(`/api/academies/${id}/sales-marketing/trials`),
    ]);
    if (
      ![leadResponse, teacherResponse, trialResponse].every(
        (response) => response.ok,
      )
    )
      throw new Error();
    setLeads(await leadResponse.json());
    setTeachers(await teacherResponse.json());
    setTrials(await trialResponse.json());
    setMessage("");
  }
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        const academies: Academy[] = await response.json();
        if (!response.ok || !academies[0]) throw new Error();
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setMessage(
          "Trial bookings could not be loaded. Please sign in and restart the API if needed.",
        );
      }
    })();
  }, []);
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    if (!leadId || !scheduledDate)
      return setMessage("Select a lead and date for the trial.");
    const response = await academyApi(
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
    );
    if (!response.ok)
      return setMessage("Select a lead and a valid time for the trial.");
    setLeadId("");
    setTeacherId("");
    setScheduledDate("");
    setScheduledTime("10:00");
    setNotes("");
    await load();
  }
  async function update(trial: Trial, status: string) {
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/sales-marketing/trials/${trial.id}/status`,
      {
        method: "PATCH",
        headers: apiHeaders(true),
        body: JSON.stringify({ status }),
      },
    );
    if (!response.ok) return setMessage("Trial status could not be updated.");
    await load();
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
          <p className="enterprise-page-state trials-message">{message}</p>
        )}
        <section className="trials-layout">
          <form onSubmit={create} className="trials-panel">
            <header className="trials-panel-header">
              <div>
                <p>New booking</p>
                <h2>Book trial class</h2>
              </div>
            </header>
            <div className="trials-fields">
              <StandardSelectField
                name="trial-lead"
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
                disabled={!academy}
              >
                Book trial class
              </button>
            </div>
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
