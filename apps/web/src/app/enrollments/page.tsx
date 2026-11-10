"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField } from "@/components/standard-date-field";

type Academy = { id: string; name: string };
type Student = { id: string; firstName: string; lastName: string };
type Batch = { id: string; name: string };
type Enrollment = { id: string; studentId: string; batchId: string; startDate: string; endDate?: string | null; status: string };
type Review = { source: Enrollment; status: string; reason: string };
type Recovery = "" | "uncertain" | "reviewed" | "confirmed" | "stale";
const statuses = ["Active", "Waitlisted", "Paused", "Completed", "Withdrawn", "Cancelled"];
const nonempty = (x: unknown): x is string => typeof x === "string" && Boolean(x.trim());
const dateOnly = (x: unknown): x is string => typeof x === "string" && /^\d{4}-\d{2}-\d{2}$/.test(x) && !x.startsWith("0000") && Number.isFinite(Date.parse(x)) && new Date(x).toISOString().slice(0, 10) === x;
function isEnrollment(x: Enrollment): boolean {
  return !!x && nonempty(x.id) && nonempty(x.studentId) && nonempty(x.batchId) && dateOnly(x.startDate) && (x.endDate == null || dateOnly(x.endDate)) && nonempty(x.status);
}
function enrollmentRows(value: unknown): value is Enrollment[] {
  return Array.isArray(value) && value.every(isEnrollment) && new Set(value.map(x => x.id)).size === value.length;
}
function sameEnrollment(a: Enrollment, b: Enrollment) {
  return a.id === b.id && a.studentId === b.studentId && a.batchId === b.batchId && a.startDate === b.startDate && (a.endDate ?? null) === (b.endDate ?? null) && a.status === b.status;
}
function guidance(status: number) {
  return status === 401 ? " Sign in again, then reload the records." : status === 403 ? " Ask an administrator to check your access, then reload the records." : status === 409 ? " Reload and review the current records before trying again." : status === 429 ? " Wait a moment before trying again." : "";
}
class EnrollmentReadError extends Error { constructor(public status: number) { super("Enrollment read failed"); } }

export default function EnrollmentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]), [batches, setBatches] = useState<Batch[]>([]), [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [studentId, setStudentId] = useState(""), [batchId, setBatchId] = useState(""), [startDate, setStartDate] = useState(""), [initialStatus, setInitialStatus] = useState("Active");
  const [message, setMessage] = useState("Loading enrolments…"), [ready, setReady] = useState(false), [loading, setLoading] = useState(true), [saving, setSaving] = useState(false);
  const [review, setReview] = useState<Review | null>(null), [recovery, setRecovery] = useState<Recovery>(""), [lastOperation, setLastOperation] = useState<"create" | "status">("create"), [intent, setIntent] = useState(0);
  const alive = useRef(true), reading = useRef(false), pending = useRef(false), readReady = useRef(false), readVersion = useRef(0), intentVersion = useRef(0), recoveryState = useRef<Recovery>("");
  const records = useRef<Enrollment[]>([]), currentReview = useRef<Review | null>(null), currentStudents = useRef<Student[]>([]), currentBatches = useRef<Batch[]>([]);
  const canWrite = ready && !loading && !saving && !recovery, visibleIntent = intent;
  const requestedStudentId = typeof window === "undefined" ? "" : new URLSearchParams(window.location.search).get("studentId") ?? "";
  function notify(value: string) { if (alive.current) setMessage(value); }
  function invalidateIntent() { ++intentVersion.current; setIntent(intentVersion.current); }
  function recoverAs(value: Recovery) { recoveryState.current = value; if (alive.current) setRecovery(value); }
  function replaceReview(value: Review | null) { currentReview.current = value; setReview(value); invalidateIntent(); }
  async function load(id?: string) {
    if (!alive.current) return false;
    const version = ++readVersion.current; readReady.current = false; setReady(false); setLoading(true);
    try {
      if (!id) {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new EnrollmentReadError(response.status);
        const list: Academy[] = await response.json();
        if (!Array.isArray(list) || !list.length || !list.every(x => x && nonempty(x.id) && typeof x.name === "string")) throw Error("No academy context");
        if (!alive.current || readVersion.current !== version) return false;
        id = list[0].id; setAcademy(list[0]);
      }
      const responses = await Promise.all([academyApi(`/api/academies/${id}/enrollments/options`, { cache: "no-store" }), academyApi(`/api/academies/${id}/enrollments`, { cache: "no-store" })]);
      const failure = responses.find(x => !x.ok); if (failure) throw new EnrollmentReadError(failure.status);
      const [options, es]: [{ students: Student[]; batches: Batch[] }, Enrollment[]] = await Promise.all([responses[0].json(), responses[1].json()]);
      const ss = options?.students, bs = options?.batches;
      if (!Array.isArray(ss) || !ss.every(x => x && nonempty(x.id) && typeof x.firstName === "string" && typeof x.lastName === "string") || new Set(ss.map(x => x.id)).size !== ss.length || !Array.isArray(bs) || !bs.every(x => x && nonempty(x.id) && typeof x.name === "string") || new Set(bs.map(x => x.id)).size !== bs.length || !enrollmentRows(es)) throw Error("Invalid enrollment records");
      if (!alive.current || readVersion.current !== version) return false;
      currentStudents.current = ss; currentBatches.current = bs; records.current = es;
      setStudents(ss); setBatches(bs); setEnrollments(es);
      // Preserve explicit selections/drafts through recovery; never silently replace one.
      setStudentId(value => value || (ss.some(x => x.id === requestedStudentId) ? requestedStudentId : ss[0]?.id ?? ""));
      setBatchId(value => value || bs[0]?.id || "");
      invalidateIntent(); readReady.current = true; setReady(true); return true;
    } catch (error) {
      if (!alive.current || readVersion.current !== version) return false;
      throw error;
    } finally { if (alive.current && readVersion.current === version) setLoading(false); }
  }
  async function reload() {
    if (!alive.current || reading.current || pending.current) return;
    reading.current = true; const version = readVersion.current + 1;
    try {
      if (!await load() || !alive.current || readVersion.current !== version) return;
      if (["uncertain", "reviewed", "stale"].includes(recoveryState.current)) {
        recoverAs("reviewed"); notify("Records refreshed. Review the register before starting a new action. The earlier result has not been resubmitted.");
      } else if (recoveryState.current === "confirmed") { recoverAs(""); notify("The earlier change was confirmed. Enrolment records refreshed."); }
      else notify("");
    } catch (error) { notify((recoveryState.current === "confirmed" ? "The earlier change was confirmed. " : recoveryState.current ? "The earlier result remains unresolved. " : "") + "Enrolment records could not be loaded." + guidance(error instanceof EnrollmentReadError ? error.status : 503)); }
    finally { if (readVersion.current === version) reading.current = false; }
  }
  const initialLoad = useRef(reload);
  useEffect(() => { alive.current = true; void initialLoad.current(); return () => { alive.current = false; readReady.current = false; ++readVersion.current; reading.current = false; }; }, []);
  function eligible(version: number) { return alive.current && !!academy && readReady.current && !reading.current && !pending.current && !recoveryState.current && intentVersion.current === version; }
  function editDraft(change: () => void) { if (alive.current && intentVersion.current === visibleIntent && !pending.current && !reading.current && !recoveryState.current) { change(); invalidateIntent(); } }
  function startReview(enrollment: Enrollment, status: string) {
    if (!eligible(visibleIntent) || !statuses.includes(status) || status === enrollment.status || !records.current.some(x => sameEnrollment(x, enrollment))) return;
    replaceReview({ source: { ...enrollment }, status, reason: "" }); notify("");
  }
  async function mutate(kind: "create" | "status", body: object, expected: (value: Enrollment) => boolean, source?: Enrollment) {
    let confirmed = false, submitted = false;
    try {
      if (source) {
        const response = await academyApi(`/api/academies/${academy!.id}/enrollments`, { cache: "no-store" });
        if (!response.ok) throw new EnrollmentReadError(response.status);
        const rows: unknown = await response.json(); if (!enrollmentRows(rows)) throw Error("Invalid preflight read");
        if (!alive.current) return;
        const latest = rows.find(x => x.id === source.id);
        if (!latest || !sameEnrollment(latest, source)) {
          readReady.current = false; setReady(false); recoverAs("stale"); notify("This enrolment changed since this review. Reload enrolment records and start a new status review. Your reason is kept until then."); return;
        }
      }
      if (!alive.current) return;
      submitted = true;
      const response = await academyApi(`/api/academies/${academy!.id}/enrollments${source ? `/${source.id}` : ""}`, { method: source ? "PUT" : "POST", headers: apiHeaders(true), body: JSON.stringify(body) });
      if (!alive.current) return;
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408) throw Error("Unconfirmed write");
        if ([401, 403, 409].includes(response.status)) { readReady.current = false; setReady(false); }
        const detail = await response.json().catch(() => null);
        notify((nonempty(detail?.message) ? detail.message : "The enrolment change was refused.") + guidance(response.status)); return;
      }
      const value: Enrollment = await response.json();
      if (!isEnrollment(value) || !expected(value)) throw Error("Invalid acknowledgement");
      if (!alive.current) return;
      confirmed = true;
      if (kind === "status") replaceReview(null); else { setStartDate(""); invalidateIntent(); }
      await load(academy!.id);
      notify(kind === "status" ? "Enrolment status change confirmed. Records refreshed." : "Enrolment creation confirmed. Records refreshed.");
    } catch (error) {
      if (!alive.current) return;
      readReady.current = false; setReady(false);
      if (!submitted && !confirmed) {
        notify("The enrolment could not be checked. No change was submitted. Reload enrolment records before confirming." + guidance(error instanceof EnrollmentReadError ? error.status : 503));
      } else {
        recoverAs(confirmed ? "confirmed" : "uncertain");
        notify(confirmed ? "The enrolment change was confirmed, but records could not be refreshed. Reload enrolment records; do not submit again." : "The enrolment change could not be confirmed. Your entries are kept. Reload enrolment records and review them before starting a new action; reloading does not submit them.");
      }
    } finally { pending.current = false; if (alive.current) setSaving(false); }
  }
  async function createEnrollment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!eligible(visibleIntent) || currentReview.current) return;
    if (!currentStudents.current.some(x => x.id === studentId) || !currentBatches.current.some(x => x.id === batchId) || !["Active", "Waitlisted"].includes(initialStatus) || (startDate !== "" && !dateOnly(startDate))) return notify("Select a current student, batch and valid start date.");
    pending.current = true; invalidateIntent(); setSaving(true); setLastOperation("create"); notify("");
    await mutate("create", { studentId, batchId, startDate: startDate || null, status: initialStatus }, value => value.studentId === studentId && value.batchId === batchId && value.status === initialStatus && (!startDate || value.startDate === startDate) && value.endDate == null);
  }
  async function confirmStatus(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!eligible(visibleIntent) || !review || currentReview.current !== review) return;
    if (!statuses.includes(review.status) || review.status === review.source.status || !records.current.some(x => sameEnrollment(x, review.source))) return;
    const reason = review.reason.trim(); if (review.status !== "Active" && !reason) return notify("Enter a lifecycle reason before confirming.");
    const endDate = review.status === "Active" ? null : review.source.endDate ?? null;
    pending.current = true; invalidateIntent(); setSaving(true); setLastOperation("status"); notify("");
    await mutate("status", { status: review.status, endDate, lifecycleReason: review.status === "Active" ? null : reason }, value => sameEnrollment(value, { ...review.source, status: review.status, endDate }), review.source);
  }
  function startNew() {
    if (!alive.current || pending.current || reading.current || !readReady.current || recoveryState.current !== "reviewed") return;
    recoverAs(""); replaceReview(null); notify("Start a new action using the refreshed records. Previous entries were not resubmitted.");
  }
  const studentName = (id: string) => { const student = students.find(x => x.id === id); return student ? `${student.firstName} ${student.lastName}` : "Student not available in current lookup"; };
  const batchName = (id: string) => batches.find(x => x.id === id)?.name ?? "Batch not available in current lookup";

  return <main className="enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="mx-auto max-w-6xl px-6 py-10">
    <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Learning</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Student enrolments</h1><p className="mt-3 text-slate-300">Connect each learner to the batch they attend. Only active enrolments can be marked for attendance.</p>
    {message && <p role="status" className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <div className="mt-4 flex flex-wrap gap-3">
      <button type="button" className="enterprise-action-button" disabled={loading || saving} onClick={() => void reload()}>Reload enrolment records</button>
      {recovery === "reviewed" && <button type="button" className="enterprise-action-button" onClick={startNew}>{lastOperation === "status" ? "Start a new status review" : "Start a new enrolment"}</button>}
    </div>
    <section className="mt-8 grid gap-6 lg:grid-cols-[0.9fr_1.1fr]"><form onSubmit={createEnrollment} className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Enrol a student</h2>
      <fieldset disabled={!canWrite || !!review} className="min-w-0">
        <label className="mt-5 block">Student<select name="studentId" value={studentId} onChange={e => editDraft(() => setStudentId(e.target.value))} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required><option value="">Select student</option>{students.map(x => <option key={x.id} value={x.id}>{x.firstName} {x.lastName}</option>)}</select></label>
        <label className="mt-3 block">Batch<select name="batchId" value={batchId} onChange={e => editDraft(() => setBatchId(e.target.value))} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required><option value="">Select batch</option>{batches.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
        <div className="mt-4"><StandardDateField name="startDate" label="Start date" value={startDate} onChange={value => editDraft(() => setStartDate(value))} /></div>
        <label className="mt-3 block">Initial status<select name="initialStatus" value={initialStatus} onChange={e => editDraft(() => setInitialStatus(e.target.value))} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option>Active</option><option>Waitlisted</option></select></label>
        <button disabled={!canWrite || !!review || !students.some(x => x.id === studentId) || !batches.some(x => x.id === batchId)} className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60">Enrol student</button>
      </fieldset>
    </form>
    <section className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Current enrolments</h2>
      {!ready ? <p className="mt-6">{loading ? "Loading enrolment records…" : "The enrolment register is unavailable. Reload to try again."}</p> : enrollments.length === 0 ? <p className="mt-6 text-slate-400">No students enrolled yet.</p> : <ul className="mt-5 space-y-3">{enrollments.map(enrollment => <li key={enrollment.id} className="break-words rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="font-medium">{studentName(enrollment.studentId)}</div><div className="mt-1 text-sm text-cyan-200">{batchName(enrollment.batchId)}</div><div className="mt-2 text-sm text-slate-400">Started {enrollment.startDate}{enrollment.endDate ? ` · Ended ${enrollment.endDate}` : ""}</div>
        <label className="mt-3 block text-sm">Lifecycle status<select name={`status.${enrollment.id}`} value={enrollment.status} onChange={e => startReview(enrollment, e.target.value)} disabled={!canWrite || !!review} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm">{!statuses.includes(enrollment.status) && <option>{enrollment.status}</option>}{statuses.map(status => <option key={status}>{status}</option>)}</select></label>
      </li>)}</ul>}
    </section></section>
    {review && <form data-enrollment-review="true" aria-label="Review enrolment status" onSubmit={confirmStatus} className="mt-6 min-w-0 rounded-2xl border border-slate-700 bg-slate-900 p-6">
      <h2 className="text-xl font-semibold">Review enrolment status</h2><p className="mt-3 break-words">{studentName(review.source.studentId)} · {batchName(review.source.batchId)}</p>
      <p className="mt-3">Reviewed status: <strong>{review.source.status}</strong> → Requested: <strong>{review.status}</strong></p>
      <p className="mt-2 text-sm">{review.status === "Active" ? "Reactivation clears the end date and the previous lifecycle reason. The server rechecks admission and capacity." : `The end date stays ${review.source.endDate ?? "unset"}. A lifecycle reason is required.`}</p>
      <fieldset disabled={!canWrite} className="min-w-0">
        {review.status !== "Active" && <label className="mt-4 block">Lifecycle reason<textarea name="lifecycleReason" value={review.reason} required rows={3} onChange={e => { if (eligible(visibleIntent) && currentReview.current === review) replaceReview({ ...review, reason: e.target.value }); }} className="mt-2 block w-full min-w-0 rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /></label>}
        <div className="mt-4 flex flex-wrap gap-3"><button type="submit" className="enterprise-action-button" disabled={!canWrite || (review.status !== "Active" && !review.reason.trim())}>Confirm status change</button><button type="button" className="enterprise-action-button" disabled={!canWrite} onClick={() => { if (eligible(visibleIntent)) replaceReview(null); }}>Cancel status change</button></div>
      </fieldset>
    </form>}
  </div></main>;
}
