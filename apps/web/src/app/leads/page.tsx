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
type Lead = {
  id: string;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  dateOfBirth?: string | null;
  parentName?: string | null;
  programInterest?: string | null;
  source: string;
  stage: string;
  followUpAtUtc?: string | null;
  notes?: string | null;
  convertedStudentId?: string | null;
};
const stages = [
  "New",
  "Contacted",
  "TrialBooked",
  "FollowUp",
  "Won",
  "Lost",
  "Converted",
];
const sources = [
  "WalkIn",
  "Website",
  "Referral",
  "Instagram",
  "WhatsApp",
  "Phone",
];
const display = (value: string) =>
  value === "TrialBooked"
    ? "Trial booked"
    : value === "FollowUp"
      ? "Follow-up"
      : value;

export default function LeadsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [leads, setLeads] = useState<Lead[]>([]);
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [parentName, setParentName] = useState("");
  const [programInterest, setProgramInterest] = useState("");
  const [source, setSource] = useState("WalkIn");
  const [followUpDate, setFollowUpDate] = useState("");
  const [followUpTime, setFollowUpTime] = useState("10:00");
  const [notes, setNotes] = useState("");
  const [message, setMessage] = useState("Loading leads…");
  const [savingId, setSavingId] = useState("");
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const leadResponse = await academyApi(`/api/academies/${academyId}/leads`, {
      cache: "no-store",
    });
    if (!leadResponse.ok) throw new Error();
    setLeads(await leadResponse.json());
    setMessage("");
  }
  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (response.status === 401)
          return setMessage("Please sign in before using admissions.");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create your academy first.");
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setMessage(
          "Admissions could not be loaded. Apply the leads migration and restart the API.",
        );
      }
    }
    void initialise();
  }, []);
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const response = await academyApi(`/api/academies/${academy.id}/leads`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify({
        fullName,
        email: email || null,
        phone: phone || null,
        dateOfBirth: dateOfBirth || null,
        parentName: parentName || null,
        programInterest: programInterest || null,
        source,
        followUpAtUtc: followUpDate
          ? new Date(`${followUpDate}T${followUpTime}:00`).toISOString()
          : null,
        notes: notes || null,
      }),
    });
    if (!response.ok) return setMessage("Lead name is required.");
    setFullName("");
    setEmail("");
    setPhone("");
    setDateOfBirth("");
    setParentName("");
    setProgramInterest("");
    setFollowUpDate("");
    setFollowUpTime("10:00");
    setNotes("");
    setMessage("");
    await load();
  }
  async function setStage(lead: Lead, stage: string) {
    if (!academy || lead.convertedStudentId) return;
    setSavingId(lead.id);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/leads/${lead.id}/stage`,
        {
          method: "PATCH",
          headers: apiHeaders(true),
          body: JSON.stringify({ stage, followUpAtUtc: lead.followUpAtUtc }),
        },
      );
      if (!response.ok) throw new Error();
      await load();
    } catch {
      setMessage("The lead stage could not be updated.");
    } finally {
      setSavingId("");
    }
  }
  async function convert(lead: Lead) {
    if (!academy || lead.convertedStudentId) return;
    if (!window.confirm(`Convert ${lead.fullName} into a student record?`))
      return;
    setSavingId(lead.id);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/leads/${lead.id}/convert`,
        {
          method: "POST",
          headers: apiHeaders(true),
          body: JSON.stringify({ firstName: null, lastName: null }),
        },
      );
      if (!response.ok) throw new Error();
      setMessage(
        `${lead.fullName} was converted to a student. You can now enrol them in a batch.`,
      );
      await load();
    } catch {
      setMessage("The lead could not be converted.");
    } finally {
      setSavingId("");
    }
  }
  return (
    <main className="enterprise-settings leads-standard min-h-screen">
      <WorkspaceNav />
      <div className="leads-content mx-auto max-w-6xl px-6 py-10">
        <header className="leads-heading">
          <div className="leads-title">
            <span className="leads-title-icon" aria-hidden="true">
              ◌
            </span>
            <div>
              <p>Sales &amp; marketing</p>
              <h1>Leads</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state leads-message">{message}</p>
        )}
        <section className="leads-layout">
          <form onSubmit={create} className="leads-panel">
            <header className="leads-panel-header">
              <div>
                <p>New enquiry</p>
                <h2>Add lead</h2>
              </div>
            </header>
            <div className="leads-fields">
              <label>
                <span>Lead name</span>
                <input
                  value={fullName}
                  onChange={(event) => setFullName(event.target.value)}
                  placeholder="Full name"
                  required
                />
              </label>
              <div className="leads-fields-two">
                <label>
                  <span>Email</span>
                  <input
                    type="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    placeholder="name@example.com"
                  />
                </label>
                <label>
                  <span>Phone</span>
                  <input
                    value={phone}
                    onChange={(event) => setPhone(event.target.value)}
                    placeholder="Phone number"
                  />
                </label>
              </div>
              <div className="leads-fields-two">
                <StandardDateField
                  name="dateOfBirth"
                  label="Date of birth"
                  value={dateOfBirth}
                  onChange={setDateOfBirth}
                />
                <label>
                  <span>Parent name</span>
                  <input
                    value={parentName}
                    onChange={(event) => setParentName(event.target.value)}
                    placeholder="Parent name"
                  />
                </label>
              </div>
              <label>
                <span>Interested program</span>
                <input
                  value={programInterest}
                  onChange={(event) => setProgramInterest(event.target.value)}
                  placeholder="e.g. Guitar"
                />
              </label>
              <StandardSelectField
                name="source"
                value={source}
                onChange={setSource}
                placeholder="Lead source"
                options={sources.map((value) => ({
                  value,
                  label: value === "WalkIn" ? "Walk-in" : value,
                }))}
              />
              <div className="leads-fields-two">
                <StandardDateField
                  name="follow-up-date"
                  label="Follow-up date"
                  value={followUpDate}
                  onChange={setFollowUpDate}
                />
                <StandardTimeField
                  name="follow-up-time"
                  label="Follow-up time"
                  value={followUpTime}
                  onChange={setFollowUpTime}
                />
              </div>
              <label>
                <span>Enquiry notes</span>
                <textarea
                  value={notes}
                  onChange={(event) => setNotes(event.target.value)}
                  placeholder="Enquiry notes"
                />
              </label>
              <button
                disabled={!academy}
                className="enterprise-action-button leads-add-button"
              >
                Add lead
              </button>
            </div>
          </form>
          <section className="leads-panel leads-pipeline-panel">
            <header className="leads-panel-header">
              <div>
                <p>Pipeline</p>
                <h2>Lead pipeline</h2>
              </div>
              <span>{leads.length} total</span>
            </header>
            {leads.length === 0 ? (
              <p className="leads-empty">No leads yet.</p>
            ) : (
              <ul>
                {leads.map((lead) => (
                  <li key={lead.id}>
                    <div className="lead-record-header">
                      <div>
                        <b>{lead.fullName}</b>
                        <small>
                          {lead.email || lead.phone || "No contact details"} ·{" "}
                          {lead.programInterest || "Program not set"}
                        </small>
                      </div>
                      <StandardSelectField
                        name={`lead-stage-${lead.id}`}
                        value={lead.stage}
                        onChange={(stage) => void setStage(lead, stage)}
                        placeholder="Stage"
                        options={stages.map((value) => ({
                          value,
                          label: display(value),
                        }))}
                        disabled={
                          Boolean(lead.convertedStudentId) ||
                          savingId === lead.id
                        }
                      />
                    </div>
                    <p>
                      {lead.source}
                      {lead.parentName ? ` · Parent: ${lead.parentName}` : ""}
                      {lead.dateOfBirth
                        ? ` · DOB: ${new Intl.DateTimeFormat("en-IN", { dateStyle: "medium", timeZone: "Asia/Kolkata" }).format(new Date(`${lead.dateOfBirth}T00:00:00+05:30`))}`
                        : ""}
                    </p>
                    {lead.followUpAtUtc && (
                      <p>
                        Follow up:{" "}
                        {new Intl.DateTimeFormat("en-IN", {
                          dateStyle: "medium",
                          timeStyle: "short",
                          timeZone: "Asia/Kolkata",
                        }).format(new Date(lead.followUpAtUtc))}{" "}
                        IST
                      </p>
                    )}
                    {lead.notes && <p>{lead.notes}</p>}
                    <button
                      type="button"
                      disabled={
                        Boolean(lead.convertedStudentId) || savingId === lead.id
                      }
                      onClick={() => void convert(lead)}
                    >
                      {lead.convertedStudentId
                        ? "Converted to student"
                        : savingId === lead.id
                          ? "Saving…"
                          : "Convert to student"}
                    </button>
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
