"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

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
  const requestedStudentId = typeof window === "undefined" ? "" : new URLSearchParams(window.location.search).get("studentId") ?? "";

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [studentResponse, batchResponse, enrollmentResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/students`, { cache: "no-store" }), academyApi(`/api/academies/${id}/batches`, { cache: "no-store" }), academyApi(`/api/academies/${id}/enrollments`, { cache: "no-store" }),
    ]);
    if (![studentResponse, batchResponse, enrollmentResponse].every((response) => response.ok)) throw new Error();
    const studentData: Student[] = await studentResponse.json();
    const batchData: Batch[] = await batchResponse.json();
    setStudents(studentData); setBatches(batchData); setEnrollments(await enrollmentResponse.json());
    if (!studentId && studentData.length) setStudentId(studentData.some((student) => student.id === requestedStudentId) ? requestedStudentId : studentData[0].id);
    if (!batchId && batchData.length) setBatchId(batchData[0].id);
    setMessage("");
  }

  useEffect(() => { async function initialise() { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (response.status === 401) return setMessage("Please sign in before managing enrolments."); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create an academy before enrolling students."); setAcademy(academies[0]); await load(academies[0].id); } catch { setMessage("Enrolments could not be loaded. Confirm the API is running on port 5092."); } } void initialise(); }, []);

  async function createEnrollment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !studentId || !batchId) return;
    const response = await academyApi(`/api/academies/${academy.id}/enrollments`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, batchId, startDate: startDate || null, status: initialStatus }) });
    if (response.status === 409) return setMessage("This student is already actively enrolled in that batch.");
    if (!response.ok) return setMessage("The enrolment could not be saved.");
    setStartDate(""); setMessage(""); await load();
  }
  async function updateStatus(enrollment: Enrollment, status: string) { if (!academy) return; setSavingId(enrollment.id); const response = await academyApi(`/api/academies/${academy.id}/enrollments/${enrollment.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ status, endDate: status === "Active" ? null : enrollment.endDate }) }); setSavingId(null); if (!response.ok) return setMessage("The enrolment status could not be updated."); setMessage("Enrolment updated."); await load(); }

  const studentName = (id: string) => { const student = students.find((item) => item.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };
  const batchName = (id: string) => batches.find((item) => item.id === id)?.name ?? "Unknown batch";

  return <main className="min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="mx-auto max-w-6xl px-6 py-10">
    <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Learning</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Student enrolments</h1><p className="mt-3 text-slate-300">Connect each learner to the batch they attend. Only active enrolments can be marked for attendance.</p>
    {message && <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className="mt-8 grid gap-6 lg:grid-cols-[0.9fr_1.1fr]"><form onSubmit={createEnrollment} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Enrol a student</h2>
      <select value={studentId} onChange={(event) => setStudentId(event.target.value)} className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required><option value="">Select student</option>{students.map((student) => <option key={student.id} value={student.id}>{student.firstName} {student.lastName}</option>)}</select>
      <select value={batchId} onChange={(event) => setBatchId(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required><option value="">Select batch</option>{batches.map((batch) => <option key={batch.id} value={batch.id}>{batch.name}</option>)}</select>
      <label className="mt-4 block text-sm text-slate-300">Start date</label><input type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><select value={initialStatus} onChange={(event) => setInitialStatus(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option>Active</option><option>Waitlisted</option></select>
      <button disabled={!academy || !students.length || !batches.length} className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60">Enrol student</button>
    </form>
    <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Current enrolments</h2>{enrollments.length === 0 ? <p className="mt-6 text-slate-400">No students enrolled yet.</p> : <ul className="mt-5 space-y-3">{enrollments.map((enrollment) => <li key={enrollment.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="font-medium">{studentName(enrollment.studentId)}</div><div className="mt-1 text-sm text-cyan-200">{batchName(enrollment.batchId)}</div><div className="mt-2 text-sm text-slate-400">Started {enrollment.startDate}{enrollment.endDate ? ` · Ended ${enrollment.endDate}` : ""}</div><div className="mt-3 flex items-center gap-2"><select value={enrollment.status} onChange={(event) => void updateStatus(enrollment, event.target.value)} disabled={savingId === enrollment.id} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm"><option>Active</option><option>Waitlisted</option><option>Paused</option><option>Completed</option><option>Withdrawn</option><option>Cancelled</option></select><span className="text-xs text-slate-500">Lifecycle status</span></div></li>)}</ul>}</section>
    </section>
  </div></main>;
}
