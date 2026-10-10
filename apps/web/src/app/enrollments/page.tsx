"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField } from "@/components/standard-date-field";

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
        setMessage(response.status === 409 ? "This student is already actively enrolled in that batch." : typeof payload?.message === "string" ? payload.message : "The change was not accepted. Your draft has been retained.");
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
  async function updateStatus(enrollment: Enrollment, status: string) {
    if (!academy || pending.current) return;
    setSavingId(enrollment.id);
    await mutate(() => academyApi(`/api/academies/${academy.id}/enrollments/${enrollment.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ status, endDate: status === "Active" ? null : enrollment.endDate }) }), "Enrolment updated.");
  }

  const studentName = (id: string) => { const student = students.find((item) => item.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };
  const batchName = (id: string) => batches.find((item) => item.id === id)?.name ?? "Unknown batch";

  return <main className="enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="mx-auto max-w-6xl px-6 py-10">
    <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Learning</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Student enrolments</h1><p className="mt-3 text-slate-300">Connect each learner to the batch they attend. Only active enrolments can be marked for attendance.</p>
    {message && <p role="status" aria-live="polite" className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className="mt-8 grid gap-6 lg:grid-cols-[0.9fr_1.1fr]"><form onSubmit={createEnrollment} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Enrol a student</h2><fieldset disabled={saving || !academy} className="m-0 min-w-0 border-0">
      <select value={studentId} onChange={(event) => setStudentId(event.target.value)} className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required><option value="">Select student</option>{students.map((student) => <option key={student.id} value={student.id}>{student.firstName} {student.lastName}</option>)}</select>
      <select value={batchId} onChange={(event) => setBatchId(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required><option value="">Select batch</option>{batches.map((batch) => <option key={batch.id} value={batch.id}>{batch.name}</option>)}</select>
      <div className="mt-4"><StandardDateField name="startDate" label="Start date" value={startDate} onChange={setStartDate} /></div><select value={initialStatus} onChange={(event) => setInitialStatus(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option>Active</option><option>Waitlisted</option></select>
      <button disabled={saving || !academy || !students.length || !batches.length} className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60">Enrol student</button></fieldset>
    </form>
    <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Current enrolments</h2>{enrollments.length === 0 ? <p className="mt-6 text-slate-400">No students enrolled yet.</p> : <ul className="mt-5 space-y-3">{enrollments.map((enrollment) => <li key={enrollment.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="font-medium">{studentName(enrollment.studentId)}</div><div className="mt-1 text-sm text-cyan-200">{batchName(enrollment.batchId)}</div><div className="mt-2 text-sm text-slate-400">Started {enrollment.startDate}{enrollment.endDate ? ` · Ended ${enrollment.endDate}` : ""}</div><div className="mt-3 flex items-center gap-2"><select value={enrollment.status} onChange={(event) => void updateStatus(enrollment, event.target.value)} disabled={saving || savingId === enrollment.id} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm"><option>Active</option><option>Waitlisted</option><option>Paused</option><option>Completed</option><option>Withdrawn</option><option>Cancelled</option></select><span className="text-xs text-slate-500">Lifecycle status</span></div></li>)}</ul>}</section>
    </section>
  </div></main>;
}
