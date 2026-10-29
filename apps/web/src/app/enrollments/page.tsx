"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField } from "@/components/standard-date-field";
import { StandardSelectField } from "@/components/design-system/controls";
import styles from "@/components/design-system/operations-workspace.module.css";

type Academy = { id: string; name: string };
type Student = { id: string; firstName: string; lastName: string };
type Batch = { id: string; name: string };
type Enrollment = { id: string; studentId: string; batchId: string; startDate: string; endDate?: string | null; status: string };

export default function EnrollmentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [studentId, setStudentId] = useState("");
  const [batchId, setBatchId] = useState("");
  const [startDate, setStartDate] = useState("");
  const [initialStatus, setInitialStatus] = useState("Active");
  const [message, setMessage] = useState("Loading enrolments…");
  const [savingId, setSavingId] = useState<string | null>(null);


  const pending = useRef(false);
  const [saving, setSaving] = useState(false);
  const [lifecycleDrafts, setLifecycleDrafts] = useState<Record<string, { status: string; reason: string }>>({});
  async function fetchWorkspace(id: string) {
    const responses = await Promise.all([academyApi(`/api/academies/${id}/students`, { cache: "no-store" }), academyApi(`/api/academies/${id}/batches`, { cache: "no-store" }), academyApi(`/api/academies/${id}/enrollments`, { cache: "no-store" })]);
    if (!responses.every(response => response.ok)) throw new Error();
    const data = await Promise.all(responses.map(response => response.json()));
    if (!data.every(Array.isArray)) throw new Error();
    return data;
  }
  function applyWorkspace(data: Awaited<ReturnType<typeof fetchWorkspace>>) {
    setStudents(data[0]); setBatches(data[1]); setEnrollments(data[2]);
  }
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const rows: Academy[] = await response.json();
        if (!Array.isArray(rows) || !rows[0]?.id) throw new Error();
        const data = await fetchWorkspace(rows[0].id);
        if (!active) return;
        setStudents(data[0]); setBatches(data[1]); setEnrollments(data[2]); setAcademy(rows[0]);
        const requested = new URLSearchParams(window.location.search).get("studentId");
        setStudentId(data[0].some((student: Student) => student.id === requested) ? requested! : data[0][0]?.id ?? "");
        setBatchId(data[1][0]?.id ?? "");
        setMessage("");
      } catch { if (active) setMessage("Enrolments could not be loaded. Please refresh or check your access."); }
    })();
    return () => { active = false; };
  }, []);
  async function mutate(request: () => Promise<Response>, success: string, reset?: () => void) {
    if (!academy || pending.current) return;
    pending.current = true; setSaving(true); setMessage("");
    try {
      const response = await request();
      if (!response.ok) {
        if (response.status >= 500) throw new Error();
        const payload = await response.json().catch(() => null);
        const explanation = typeof payload?.message === "string" ? payload.message.trim() : "";
        setMessage(explanation || (response.status === 409 ? "The change conflicts with the current enrolment rules. Your draft has been retained." : "The change was not accepted. Your draft has been retained."));
        return;
      }
      reset?.(); setMessage(success);
      try { applyWorkspace(await fetchWorkspace(academy.id)); }
      catch { setMessage(`${success} The register could not be refreshed. Do not repeat the action; refresh to check the saved record.`); }
    } catch { setMessage("The result could not be confirmed. Your draft has been retained. Check the register before trying again."); }
    finally { pending.current = false; setSaving(false); setSavingId(null); }
  }
  async function createEnrollment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !studentId || !batchId) return;
    if (pending.current) return;
    await mutate(() => academyApi(`/api/academies/${academy.id}/enrollments`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, batchId, startDate: startDate || null, status: initialStatus }) }), "Enrolment created.", () => setStartDate(""));
  }
  function chooseStatus(enrollment: Enrollment, status: string) {
    if (!academy || pending.current) return;
    setLifecycleDrafts(previous => ({ ...previous, [enrollment.id]: { status, reason: previous[enrollment.id]?.reason ?? "" } }));
  }
  function cancelStatus(id: string) {
    if (pending.current) return;
    setLifecycleDrafts(previous => { const next = { ...previous }; delete next[id]; return next; });
  }
  async function updateStatus(enrollment: Enrollment, status: string, reason: string) {
    if (!academy || pending.current) return;
    const lifecycleReason = status === "Active" ? null : reason.trim();
    if (status !== "Active" && !lifecycleReason) {
      setMessage("A lifecycle reason is required for a non-Active enrolment status.");
      return;
    }
    setSavingId(enrollment.id);
    await mutate(() => academyApi(`/api/academies/${academy.id}/enrollments/${enrollment.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ status, endDate: status === "Active" ? null : enrollment.endDate, lifecycleReason }) }), "Enrolment updated.", () => setLifecycleDrafts(previous => { const next = { ...previous }; delete next[enrollment.id]; return next; }));
  }

  const studentName = (id: string) => { const student = students.find((item) => item.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };
  const batchName = (id: string) => batches.find((item) => item.id === id)?.name ?? "Unknown batch";

  return <main className={`${styles.page} enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100`}><WorkspaceNav /><div className={styles.body}>
    <header className={styles.heading}><span aria-hidden="true">↗</span><div><p>Learning</p><h1>Student enrolments</h1></div></header>
    {message && <p role="status" aria-live="polite" className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className={styles.layout}><form onSubmit={createEnrollment} className={styles.panel} aria-label="Enrol a student" aria-busy={saving}><h2>Enrol a student</h2><fieldset disabled={saving || !academy} className={styles.fields}>
      <StandardSelectField name="student" label="Student (required)" value={studentId} onChange={setStudentId} disabled={saving || !academy} required placeholder="Select student" options={students.map(student => ({ value: student.id, label: `${student.firstName} ${student.lastName}` }))} />
      <StandardSelectField name="batch" label="Batch (required)" value={batchId} onChange={setBatchId} disabled={saving || !academy} required placeholder="Select batch" options={batches.map(batch => ({ value: batch.id, label: batch.name }))} />
      <StandardDateField name="startDate" label="Start date (optional)" value={startDate} onChange={setStartDate} disabled={saving || !academy} />
      <StandardSelectField name="initial-status" label="Initial status" value={initialStatus} onChange={value => { if (value) setInitialStatus(value); }} disabled={saving || !academy} placeholder="Select initial status" options={["Active", "Waitlisted"].map(value => ({ value, label: value }))} />
      <button disabled={saving || !academy || !students.length || !batches.length} className={`${styles.action} ${styles.primary}`}>Enrol student</button></fieldset>
    </form>
    <section className={styles.panel}><h2>Current enrolments</h2>{enrollments.length === 0 ? <p className="mt-6 text-slate-400">No students enrolled yet.</p> : <ul className={styles.register}>{enrollments.map((enrollment) => {
      const draft = lifecycleDrafts[enrollment.id];
      return <li key={enrollment.id} className={styles.record}>
        <div className="font-medium">{studentName(enrollment.studentId)}</div><div className="mt-1 text-sm text-cyan-200">{batchName(enrollment.batchId)}</div>
        <div className="mt-2 text-sm text-slate-400">Started {enrollment.startDate}{enrollment.endDate ? ` · Ended ${enrollment.endDate}` : ""} · Current status: {enrollment.status}</div>
        <form className="mt-3" onSubmit={(event) => { event.preventDefault(); if (draft) void updateStatus(enrollment, draft.status, draft.reason); }}>
          <fieldset disabled={saving || !academy} className="m-0 min-w-0 border-0">
            <StandardSelectField name={`status-${enrollment.id}`} label={`Lifecycle status for ${studentName(enrollment.studentId)}`} value={draft?.status ?? enrollment.status} onChange={value => { if (value) chooseStatus(enrollment, value); }} disabled={saving || savingId === enrollment.id} placeholder="Select lifecycle status" options={["Active", "Waitlisted", "Paused", "Completed", "Withdrawn", "Cancelled"].map(value => ({ value, label: value }))} />
            {draft && <div className="mt-3 space-y-3">
              <p className="text-sm text-slate-300">Review change: {enrollment.status} → {draft.status}. Nothing changes until you save.</p>
              {draft.status !== "Active" && <div>
                <label htmlFor={`reason-${enrollment.id}`} className="block text-sm font-medium">Lifecycle reason (required)</label>
                <textarea id={`reason-${enrollment.id}`} value={draft.reason} onChange={(event) => { const reason = event.target.value; if (!pending.current) setLifecycleDrafts(previous => ({ ...previous, [enrollment.id]: { ...draft, reason } })); }} required rows={3} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm" aria-describedby={`reason-help-${enrollment.id}`} />
                <p id={`reason-help-${enrollment.id}`} className="mt-1 text-xs text-slate-400">Explain this status change. Avoid unnecessary sensitive personal information.</p>
              </div>}
              <div className={styles.actions}>
                <button type="submit" disabled={saving} className={`${styles.action} ${styles.primary}`}>Save status</button>
                <button type="button" disabled={saving} onClick={() => cancelStatus(enrollment.id)} className={styles.action}>Cancel change</button>
              </div>
            </div>}
          </fieldset>
        </form>
      </li>;
    })}</ul>}</section>
    </section>
  </div></main>;
}
