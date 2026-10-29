"use client";

import { type FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import { practiceDraftError, practicePayload, validPracticeLog, validPracticeStudent, type PracticeDraft, type PracticeLog, type PracticeStudent } from "@/lib/practice-log";
import visual from "@/components/design-system/learning-workspace.module.css";

type Academy = { id: string };
async function readWorkspace(signal?: AbortSignal) {
  const response = await academyApi("/api/academies", { cache: "no-store", signal });
  if (!response.ok) throw new Error("Practice logs could not be loaded. Check your sign-in and academy access.");
  const academies: unknown = await response.json();
  if (!Array.isArray(academies) || !academies[0] || typeof academies[0].id !== "string" || !academies[0].id.trim()) throw new Error("An academy is required before managing practice logs.");
  const academy: Academy = { id: academies[0].id };
  const [studentsResponse, logsResponse] = await Promise.all([
    academyApi(`/api/academies/${academy.id}/students`, { cache: "no-store", signal }),
    academyApi(`/api/academies/${academy.id}/practice-logs`, { cache: "no-store", signal }),
  ]);
  if (!studentsResponse.ok || !logsResponse.ok) throw new Error("Practice logs could not be loaded. Check your sign-in and academy access.");
  const students: unknown = await studentsResponse.json(), logs: unknown = await logsResponse.json();
  if (!Array.isArray(students) || !students.every(validPracticeStudent) || !Array.isArray(logs) || !logs.every(validPracticeLog)) throw new Error("The practice-log response could not be verified. Reload the list before continuing.");
  return { academy, students, logs };
}

export default function PracticeLogsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<PracticeStudent[]>([]);
  const [logs, setLogs] = useState<PracticeLog[]>([]);
  const [draft, setDraft] = useState<PracticeDraft>({ studentId: "", practiceDate: "", minutesPracticed: 30, focusArea: "", notes: "" });
  const [message, setMessage] = useState("Loading practice logs…");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [uncertain, setUncertain] = useState(false);
  const pending = useRef(false), reading = useRef(false), mounted = useRef(true), mustCheckList = useRef(false);
  function applyWorkspace(data: Awaited<ReturnType<typeof readWorkspace>>) {
    setAcademy(data.academy); setStudents(data.students); setLogs(data.logs);
  }
  useEffect(() => {
    const controller = new AbortController(); mounted.current = true;
    void (async () => {
      try {
        const data = await readWorkspace(controller.signal);
        if (controller.signal.aborted) return;
        applyWorkspace(data); setDraft(current => ({ ...current, practiceDate: new Date().toISOString().slice(0,10) })); setMessage("");
      } catch (error) { if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Practice logs could not be loaded. Reload the list to try again."); }
      finally { if (!controller.signal.aborted) setLoading(false); }
    })();
    return () => { mounted.current = false; controller.abort(); };
  }, []);
  async function reload() {
    if (pending.current || reading.current) return;
    reading.current = true; setLoading(true);
    try {
      const data = await readWorkspace();
      if (!mounted.current) return;
      applyWorkspace(data); mustCheckList.current = false; setUncertain(false); setMessage("Practice logs refreshed. Check the list before manually repeating any unconfirmed save or review.");
    } catch { if (mounted.current) setMessage("Practice logs could not be refreshed. Your draft is retained; no save or review was repeated."); }
    finally { reading.current = false; if (mounted.current) setLoading(false); }
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending.current || reading.current || loading || uncertain || mustCheckList.current || !academy) return;
    const problem = practiceDraftError(draft, students, new Date().toISOString().slice(0,10));
    if (problem) { setMessage(problem); return; }
    pending.current = true; setSaving(true); setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/practice-logs`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify(practicePayload(draft)) });
      if (!mounted.current) return;
      if (!response.ok) {
        if (response.status >= 500) throw new Error();
        setMessage(response.status === 401 ? "Sign in again before saving a practice log." : response.status === 403 ? "You do not have permission to save this practice log." : response.status === 429 ? "Too many requests. Wait before manually trying again; your draft is retained." : "The practice log was not saved. Check the active student, UTC date and minute limits. Your draft is retained.");
        return;
      }
      const saved: unknown = await response.json();
      if (!mounted.current) return;
      if (!validPracticeLog(saved) || logs.some(row => row.id === saved.id) || saved.studentId !== draft.studentId || saved.minutesPracticed !== draft.minutesPracticed || saved.status !== "Logged" || saved.focusArea !== draft.focusArea || saved.notes !== draft.notes || (draft.practiceDate && saved.practiceDate !== draft.practiceDate)) throw new Error();
      setLogs(current => [saved, ...current]); setDraft(current => ({ ...current, focusArea: "", notes: "" })); setMessage("Practice log saved.");
    } catch {
      if (mounted.current) { mustCheckList.current = true; setUncertain(true); setMessage("Practice-log save could not be confirmed. Your draft is retained. Reload and check the list before manually retrying; this request will not be sent again automatically."); }
    } finally { pending.current = false; if (mounted.current) setSaving(false); }
  }
  async function review(id: string, feedback: string) {
    if (pending.current || reading.current || loading || uncertain || mustCheckList.current || !academy || feedback.length > 2000) return false;
    const before = logs.find(row => row.id === id);
    if (!before || before.status !== "Logged") return false;
    pending.current = true; setSaving(true); setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/practice-logs/${id}/review`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ teacherFeedback: feedback }) });
      if (!mounted.current) return false;
      if (!response.ok) {
        if (response.status >= 500) throw new Error();
        setMessage(response.status === 401 ? "Sign in again before reviewing a practice log." : response.status === 403 ? "You do not have permission to review this practice log." : response.status === 429 ? "Too many requests. Wait before manually trying again; feedback is retained." : "The practice log could not be reviewed. Reload the list and check access; feedback is retained.");
        return false;
      }
      const saved: unknown = await response.json();
      if (!mounted.current) return false;
      if (!validPracticeLog(saved) || saved.id !== id || saved.studentId !== before.studentId || saved.practiceDate !== before.practiceDate || saved.minutesPracticed !== before.minutesPracticed || saved.focusArea !== before.focusArea || saved.notes !== before.notes || saved.status !== "Reviewed" || saved.teacherFeedback !== feedback.trim()) throw new Error();
      setLogs(current => current.map(row => row.id === id ? saved : row)); setMessage("Practice log reviewed."); return true;
    } catch {
      if (mounted.current) { mustCheckList.current = true; setUncertain(true); setMessage("Practice-log review could not be confirmed. Feedback is retained. Reload and check the list before manually retrying; this request will not be sent again automatically."); }
      return false;
    } finally { pending.current = false; if (mounted.current) setSaving(false); }
  }
  const disabled = loading || saving || uncertain || !academy;
  const studentName = (id: string) => {
    const student = students.find(student => student.id === id);
    return student ? student.firstName + " " + student.lastName : "Student reference unavailable";
  };
  return <main className={["enterprise-settings", visual.page].join(" ")}><WorkspaceNav /><div>
    <header className={visual.heading}><p>Learning</p><h1>Practice / study logs</h1><span>Record practice and review feedback. Practice dates use UTC, matching the academy service.</span></header>
    {message && <p className={visual.notice} role="status" aria-live="polite">{message}</p>}
    <button type="button" className="enterprise-action-button" disabled={loading || saving} onClick={reload}>Reload practice logs</button>
    <section className={visual.layout}>
      <form className={visual.panel} onSubmit={save} aria-busy={saving}>
        <h2>Record practice</h2><fieldset className={visual.fields} disabled={disabled}>
          <StandardSelectField name="studentId" label="Student" value={draft.studentId} required disabled={disabled} onChange={studentId => setDraft(current => ({ ...current, studentId }))} placeholder="Select active student" options={students.filter(student => student.isActive).map(student => ({ value: student.id, label: studentName(student.id) }))} />
          <StandardDateField name="practiceDate" label="Practice date (UTC)" value={draft.practiceDate} disabled={disabled} onChange={practiceDate => setDraft(current => ({ ...current, practiceDate }))} />
          <label><span>Minutes practiced</span><input name="minutesPracticed" type="number" min={1} max={1440} step={1} required value={Number.isFinite(draft.minutesPracticed) ? draft.minutesPracticed : ""} onChange={event => setDraft(current => ({ ...current, minutesPracticed: event.target.valueAsNumber }))} /></label>
          <label><span>Focus area (optional)</span><input name="focusArea" maxLength={250} value={draft.focusArea} onChange={event => setDraft(current => ({ ...current, focusArea: event.target.value }))} /></label>
          <label><span>Notes (optional)</span><textarea name="notes" maxLength={2000} value={draft.notes} onChange={event => setDraft(current => ({ ...current, notes: event.target.value }))} /></label>
          <button className="enterprise-action-button" disabled={disabled || !draft.studentId}>{saving ? "Saving…" : "Save log"}</button>
        </fieldset>
      </form>
      <section className={visual.panel} aria-label="Practice log records"><h2>Practice logs</h2>
        {!loading && !academy ? <p>Practice records are unavailable until the list loads.</p> : !loading && !logs.length ? <p>No practice logs are available in this view.</p> : <ul className={visual.records}>{logs.map(row => <li className={visual.record} key={row.id}>
          <b>{row.practiceDate} · {row.minutesPracticed} min · {row.focusArea || "Practice"}</b><small>{studentName(row.studentId)}</small>
          {row.notes && <p>{row.notes}</p>}<p>{row.status}</p>
          {row.status === "Reviewed" ? <p>Teacher feedback: {row.teacherFeedback || "Reviewed"}</p> : <Review disabled={disabled} onSave={feedback => review(row.id, feedback)} />}
        </li>)}</ul>}
      </section>
    </section>
  </div></main>;
}
function Review({ onSave, disabled }: { onSave: (feedback: string) => Promise<boolean>; disabled: boolean }) {
  const [feedback, setFeedback] = useState("");
  return <form className={visual.fields} onSubmit={async event => { event.preventDefault(); if (disabled) return; if (await onSave(feedback)) setFeedback(""); }}>
    <fieldset className={visual.fields} disabled={disabled}><label><span>Teacher feedback (optional)</span><textarea maxLength={2000} value={feedback} onChange={event => setFeedback(event.target.value)} /></label><button className="enterprise-action-button" disabled={disabled}>Review</button></fieldset>
  </form>;
}
