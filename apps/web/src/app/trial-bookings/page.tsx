"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardDateField, StandardSelectField, StandardTimeField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import { trialBookingUtc } from "@/lib/trial-booking-clock";

type Academy = { id: string };
type Lead = { id: string; fullName: string; programInterest?: string | null; stage: string; convertedStudentId?: string | null };
type Teacher = { id: string; firstName: string; lastName: string };
type Trial = { id: string; leadId: string; teacherId?: string | null; scheduledAtUtc: string; status: string; notes?: string | null };
type Recovery = "" | "unconfirmed" | "confirmed" | "reviewed";
const trialStatuses = ["Booked", "Completed", "Cancelled", "NoShow"];
const statusLabel = (status: string) => status === "NoShow" ? "No show" : status;
const converted = (lead: Lead) => Boolean(lead.convertedStudentId) || lead.stage.trim().toLowerCase() === "converted";
const validTrial = (row: Trial) => row && typeof row.id === "string" && typeof row.leadId === "string" && typeof row.scheduledAtUtc === "string" && Number.isFinite(Date.parse(row.scheduledAtUtc)) && trialStatuses.includes(row.status) && (row.teacherId == null || typeof row.teacherId === "string") && (row.notes == null || typeof row.notes === "string");
class TrialReadError extends Error { constructor(readonly status: number) { super("Trial bookings could not be loaded."); } }
const guidance = (status: number) => status === 401 ? " Sign in again." : status === 403 ? " This account does not have access to the required records." : status === 429 ? " Please wait before trying again." : " Reload trial records before trying again.";

export default function TrialBookingsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [leads, setLeads] = useState<Lead[]>([]), [teachers, setTeachers] = useState<Teacher[]>([]), [trials, setTrials] = useState<Trial[]>([]);
  const [message, setMessage] = useState("Loading trial bookings…"), [problem, setProblem] = useState(false);
  const [leadId, setLeadId] = useState(""), [teacherId, setTeacherId] = useState("");
  const [scheduledDate, setScheduledDate] = useState(""), [scheduledTime, setScheduledTime] = useState("10:00"), [notes, setNotes] = useState("");
  const [ready, setReady] = useState(false), [loading, setLoading] = useState(true), [busy, setBusy] = useState(false);
  const [recovery, setRecovery] = useState<Recovery>(""), [kind, setKind] = useState("booking"), [intent, setIntent] = useState(0);
  const alive = useRef(true), reading = useRef(false), pending = useRef(false), readReady = useRef(false);
  const readVersion = useRef(0), intentVersion = useRef(0), recoveryState = useRef<Recovery>("");
  const currentLeads = useRef<Lead[]>([]), currentTeachers = useRef<Teacher[]>([]), currentTrials = useRef<Trial[]>([]);
  const canWrite = ready && !loading && !busy && !recovery, visibleIntent = intent;
  function invalidateIntent() { ++intentVersion.current; setIntent(intentVersion.current); }
  function notify(value: string, failed = false) { if (alive.current) { setMessage(value); setProblem(failed); } }
  function recoverAs(value: Recovery) { recoveryState.current = value; if (alive.current) setRecovery(value); }
  async function load(id?: string) {
    if (!alive.current) return false;
    const version = ++readVersion.current;
    readReady.current = false; setReady(false); setLoading(true);
    try {
      if (!id) {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new TrialReadError(response.status);
        const rows: Academy[] = await response.json();
        if (!Array.isArray(rows) || !rows.length || !rows.every(row => row && typeof row.id === "string")) throw Error("No academy context");
        if (!alive.current || readVersion.current !== version) return false;
        id = rows[0].id; setAcademy(rows[0]);
      }
      const responses = await Promise.all([academyApi(`/api/academies/${id}/leads`, { cache: "no-store" }), academyApi(`/api/academies/${id}/sales-marketing/teacher-options`, { cache: "no-store" }), academyApi(`/api/academies/${id}/sales-marketing/trials`, { cache: "no-store" })]);
      const failure = responses.find(response => !response.ok);
      if (failure) throw new TrialReadError(failure.status);
      const [ls, ts, bs]: [Lead[], Teacher[], Trial[]] = await Promise.all([responses[0].json(), responses[1].json(), responses[2].json()]);
      if (!Array.isArray(ls) || !ls.every(row => row && typeof row.id === "string" && typeof row.fullName === "string" && typeof row.stage === "string" && (row.convertedStudentId == null || typeof row.convertedStudentId === "string"))
        || !Array.isArray(ts) || !ts.every(row => row && typeof row.id === "string" && typeof row.firstName === "string" && typeof row.lastName === "string")
        || !Array.isArray(bs) || !bs.every(validTrial)) throw Error("Invalid trial records");
      if (!alive.current || readVersion.current !== version) return false;
      currentLeads.current = ls; currentTeachers.current = ts; currentTrials.current = bs;
      setLeads(ls); setTeachers(ts); setTrials(bs); invalidateIntent(); readReady.current = true; setReady(true);
      return true;
    } catch (error) {
      if (!alive.current || readVersion.current !== version) return false;
      throw error;
    } finally { if (alive.current && readVersion.current === version) setLoading(false); }
  }
  async function reload() {
    if (!alive.current || reading.current || pending.current) return;
    reading.current = true;
    const version = readVersion.current + 1;
    try {
      if (!await load() || !alive.current || readVersion.current !== version) return;
      if (recoveryState.current === "unconfirmed" || recoveryState.current === "reviewed") { recoverAs("reviewed"); notify("Records refreshed. The earlier result is still unconfirmed. Review trial history before starting a new action.", true); }
      else if (recoveryState.current === "confirmed") { recoverAs(""); notify("The earlier change was confirmed. Trial records refreshed."); }
      else notify("");
    } catch (error) { notify((recoveryState.current === "confirmed" ? "The earlier change was confirmed. " : recoveryState.current ? "The earlier result remains unconfirmed. " : "") + "Trial bookings could not be loaded." + guidance(error instanceof TrialReadError ? error.status : 503), true); }
    finally { if (readVersion.current === version) reading.current = false; }
  }
  // The initial read uses no draft state; edits never restart this mount effect.
  const initialLoad = useRef(reload);
  useEffect(() => { alive.current = true; void initialLoad.current(); return () => { alive.current = false; readReady.current = false; ++readVersion.current; reading.current = false; }; }, []);
  function begin(version: number) {
    if (!alive.current || !academy || !readReady.current || reading.current || pending.current || recoveryState.current || intentVersion.current !== version) return false;
    pending.current = true; invalidateIntent(); setBusy(true); return true;
  }
  async function mutate(url: string, method: string, body: object, validate: (receipt: Trial) => boolean, confirmation: string, booking: boolean) {
    let confirmed = false;
    try {
      const response = await academyApi(url, { method, headers: apiHeaders(true), body: JSON.stringify(body) });
      if (!alive.current) return;
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408) throw Error("Unconfirmed change");
        if ([401, 403, 409].includes(response.status)) { readReady.current = false; setReady(false); }
        notify((booking ? "Trial class was not booked." : "Trial status was not updated.") + guidance(response.status), true); return;
      }
      const receipt: Trial = await response.json();
      if (!validTrial(receipt) || !validate(receipt)) throw Error("Invalid confirmation");
      if (!alive.current) return;
      confirmed = true;
      if (booking) { setLeadId(""); setTeacherId(""); setScheduledDate(""); setScheduledTime("10:00"); setNotes(""); }
      await load(academy!.id);
      if (alive.current) notify(confirmation);
    } catch {
      if (alive.current) {
        readReady.current = false; setReady(false); setKind(booking ? "booking" : "status"); recoverAs(confirmed ? "confirmed" : "unconfirmed");
        notify(confirmed ? `${confirmation} The change was confirmed, but trial records could not be refreshed. Reload trial records before making another change.`
          : `Trial ${booking ? "booking" : "status update"} could not be confirmed. Reload trial records and review the history before starting a new action.`, true);
      }
    } finally { pending.current = false; if (alive.current) setBusy(false); }
  }
  async function create(event: FormEvent<HTMLFormElement>, version: number) {
    event.preventDefault();
    if (!alive.current || !readReady.current || reading.current || pending.current || recoveryState.current || intentVersion.current !== version) return;
    const lead = currentLeads.current.find(row => row.id === leadId);
    const utc = trialBookingUtc(scheduledDate, scheduledTime);
    if (!lead || converted(lead) || !utc || teacherId && !currentTeachers.current.some(row => row.id === teacherId)) return notify("Select an unconverted lead, valid IST date and time, and an available teacher if needed.", true);
    if (!begin(version) || !academy) return;
    return mutate(`/api/academies/${academy.id}/sales-marketing/trials`, "POST", { leadId, teacherId: teacherId || null, scheduledAtUtc: utc, notes: notes || null }, receipt => receipt.leadId === leadId && (receipt.teacherId ?? null) === (teacherId || null) && Date.parse(receipt.scheduledAtUtc) === Date.parse(utc) && receipt.status === "Booked", "Trial class booked.", true);
  }
  async function update(trial: Trial, status: string, version: number) {
    if (!trialStatuses.includes(status) || !currentTrials.current.some(row => row.id === trial.id) || !begin(version) || !academy) return;
    return mutate(`/api/academies/${academy.id}/sales-marketing/trials/${trial.id}/status`, "PATCH", { status }, receipt => receipt.id === trial.id && receipt.status === status, "Trial status updated.", false);
  }
  function startNewAction() {
    if (!alive.current || !readReady.current || reading.current || pending.current || recoveryState.current !== "reviewed") return;
    invalidateIntent(); recoverAs(""); notify("Ready for a new action. Review the retained draft before submitting.");
  }
  const leadName = (id: string) => leads.find(lead => lead.id === id)?.fullName ?? "Lead";
  const teacherName = (id?: string | null) => { const teacher = teachers.find(item => item.id === id); return teacher ? `${teacher.firstName} ${teacher.lastName}` : "Unassigned"; };
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
          <p className="enterprise-page-state trials-message" role={problem ? "alert" : "status"}>{message}</p>
        )}
        {!ready && !loading && !recovery && <button type="button" className="enterprise-action-button" onClick={() => void reload()}>Try again</button>}
        {recovery && <button type="button" className="enterprise-action-button" disabled={loading || busy} onClick={() => void reload()}>Reload trial records</button>}
        {recovery === "reviewed" && ready && <button type="button" className="enterprise-action-button" onClick={startNewAction}>{kind === "booking" ? "Start a new booking" : "Continue after review"}</button>}
        <section className="trials-layout">
          <form onSubmit={event => void create(event, visibleIntent)} className="trials-panel">
            <header className="trials-panel-header">
              <div>
                <p>New booking</p>
                <h2>Book trial class</h2>
              </div>
            </header>
            <fieldset className="trials-fields" disabled={!canWrite} style={{ minWidth: 0, margin: 0, border: 0 }}>
              <StandardSelectField
                name="trial-lead"
                value={leadId}
                onChange={setLeadId}
                placeholder="Select lead"
                options={leads.filter(lead => !converted(lead)).map((lead) => ({
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
                  label="Trial date (IST)"
                  value={scheduledDate}
                  onChange={setScheduledDate}
                  required
                />
                <StandardTimeField
                  name="trial-time"
                  label="Start time (IST)"
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
                disabled={!canWrite}
              >
                Book trial class
              </button>
            </fieldset>
          </form>
          <section className="trials-panel trials-list-panel">
            <header className="trials-panel-header">
              <div>
                <p>Schedule</p>
                <h2>Trial classes</h2>
              </div>
              <span>{ready && !loading ? `${trials.length} total` : "History unavailable"}</span>
            </header>
            {!ready || loading ? <p className="trials-empty">{loading ? "Loading trial records…" : "Trial history is unavailable until records load."}</p> : trials.length === 0 ? (
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
                      onChange={(status) => void update(trial, status, visibleIntent)}
                      disabled={!canWrite}
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
