"use client";

import { useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Batch = { id: string; name: string };
type Enrollment = { studentId: string; batchId: string; status: string };
type Session = { id: string; batchId: string; startUtc: string };
type Attendance = { studentId: string; status: string; notes?: string | null };
const statuses = ["Present", "Absent", "Late", "Excused", "Online"];

function sessionLabel(session: Session, batches: Batch[]) {
  const batch = batches.find((item) => item.id === session.batchId)?.name ?? "Unknown batch";
  const time = new Intl.DateTimeFormat("en-IN", { dateStyle: "medium", timeStyle: "short" }).format(new Date(session.startUtc));
  return `${batch} — ${time}`;
}

export default function AttendancePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [sessionId, setSessionId] = useState("");
  const [records, setRecords] = useState<Attendance[]>([]);
  const [message, setMessage] = useState("Loading attendance…");
  const [savingId, setSavingId] = useState("");
  const [notesByStudent, setNotesByStudent] = useState<Record<string, string>>({});

  async function loadWorkspace(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [studentResponse, batchResponse, enrollmentResponse, sessionResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/students`, { cache: "no-store" }), academyApi(`/api/academies/${id}/batches`, { cache: "no-store" }), academyApi(`/api/academies/${id}/enrollments`, { cache: "no-store" }), academyApi(`/api/academies/${id}/sessions`, { cache: "no-store" }),
    ]);
    if (![studentResponse, batchResponse, enrollmentResponse, sessionResponse].every((response) => response.ok)) throw new Error();
    const sessionData: Session[] = await sessionResponse.json();
    setStudents(await studentResponse.json()); setBatches(await batchResponse.json()); setEnrollments(await enrollmentResponse.json()); setSessions(sessionData);
    if (!sessionId && sessionData.length) setSessionId(sessionData[0].id);
    setMessage("");
  }

  async function loadRecords(id: string) {
    if (!academy || !id) return setRecords([]);
    const response = await academyApi(`/api/academies/${academy.id}/sessions/${id}/attendance`, { cache: "no-store" });
    if (!response.ok) throw new Error();
    setRecords(await response.json());
  }

  useEffect(() => { async function initialise() { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (response.status === 401) return setMessage("Please sign in before marking attendance."); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create an academy, batch, and class session before marking attendance."); setAcademy(academies[0]); await loadWorkspace(academies[0].id); } catch { setMessage("Attendance could not be loaded. Confirm the API is running on port 5092."); } } void initialise(); }, []);
  useEffect(() => { void loadRecords(sessionId).catch(() => setMessage("Attendance records could not be loaded.")); }, [sessionId, academy]);

  const selectedSession = sessions.find((session) => session.id === sessionId);
  const roster = selectedSession ? enrollments.filter((item) => item.batchId === selectedSession.batchId && item.status === "Active").map((item) => students.find((student) => student.id === item.studentId)).filter((student): student is Student => Boolean(student)) : [];
  const recordFor = (studentId: string) => records.find((record) => record.studentId === studentId);

  async function mark(studentId: string, status: string) {
    if (!academy || !sessionId) return;
    setSavingId(studentId); setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/sessions/${sessionId}/attendance`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, status, notes: notesByStudent[studentId] || null }) });
      if (!response.ok) throw new Error();
      await loadRecords(sessionId);
    } catch { setMessage("Attendance could not be saved. The student must have an active enrolment in this batch."); }
    finally { setSavingId(""); }
  }

  return <main className="enterprise-settings enterprise-legacy-standard attendance-standard min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="attendance-content mx-auto max-w-5xl px-6 py-10">
    <header className="attendance-heading"><div className="attendance-title"><span className="attendance-title-icon" aria-hidden="true">✓</span><div><p>Class records</p><h1>Attendance</h1></div></div></header>
    {message && <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className="attendance-panel"><header className="attendance-panel-header"><div><p>Session</p><h2>Mark attendance</h2></div><span>{roster.length} students</span></header><div className="attendance-session-picker"><StandardSelectField name="session" value={sessionId} onChange={setSessionId} placeholder="Select a class" options={sessions.map((session) => ({ value: session.id, label: sessionLabel(session, batches) }))} /></div>
      {!sessionId ? <p className="attendance-empty">Schedule a class first.</p> : roster.length === 0 ? <p className="attendance-empty">No active enrolments exist for this batch yet.</p> : <ul className="attendance-roster">{roster.map((student) => { const record = recordFor(student.id); return <li key={student.id}><div><b>{student.firstName} {student.lastName}</b><small>{record ? `Marked: ${record.status}` : "Not marked"}</small></div><StandardSelectField name={`attendance-${student.id}`} value={record?.status ?? ""} onChange={(status) => { if (savingId !== student.id) void mark(student.id, status); }} placeholder="Select status" options={statuses.map((status) => ({ value: status, label: status }))} /><input value={notesByStudent[student.id] ?? record?.notes ?? ""} onChange={(event) => setNotesByStudent((current) => ({ ...current, [student.id]: event.target.value }))} placeholder="Notes (optional)" /><button disabled={savingId === student.id} onClick={() => { const status = record?.status; if (status) void mark(student.id, status); }} className="attendance-save">{savingId === student.id ? "Saving…" : "Save"}</button></li>; })}</ul>}
    </section>
  </div></main>;
}
