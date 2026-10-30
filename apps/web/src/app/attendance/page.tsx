"use client";

import { useEffect, useRef, useState } from "react";
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
type AttendanceScope = { key: string; generation: object; phase: "loading" | "ready" | "error" };
const initialScope = (key: string): AttendanceScope => ({ key, generation: {}, phase: "loading" });

async function readAttendance(academyId: string, sessionId: string): Promise<Attendance[]> {
  const response = await academyApi(`/api/academies/${academyId}/sessions/${sessionId}/attendance`, { cache: "no-store" });
  if (!response.ok) throw new Error();
  const data: Attendance[] = await response.json();
  if (!Array.isArray(data) || !data.every(row => row !== null && typeof row === "object"
      && typeof row.studentId === "string" && Boolean(row.studentId.trim())
      && typeof row.status === "string" && Boolean(row.status.trim())
      && (row.notes == null || typeof row.notes === "string")) || new Set(data.map(row => row.studentId)).size !== data.length)
    throw new Error("Invalid attendance records response.");
  return data;
}

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
  const [records, setRecords] = useState<Record<string, Attendance[]>>({});
  const [message, setMessage] = useState("Loading attendance…");
  const [savingId, setSavingId] = useState("");
  const [notesByStudent, setNotesByStudent] = useState<Record<string, string>>({});
  const pendingSave = useRef<object | null>(null);
  const selectionKey = academy?.id && sessionId ? `${academy.id}:${sessionId}` : "";
  const [recordScope, setRecordScope] = useState(initialScope(selectionKey));
  const currentScope = useRef(recordScope);
  const mounted = useRef(false);
  if (recordScope.key !== selectionKey) { setRecordScope(initialScope(selectionKey)); setSavingId(""); setMessage(""); }
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  useEffect(() => { currentScope.current = recordScope; }, [recordScope]);
  function selectSession(id = sessionId) {
    const next = initialScope(academy?.id && id ? `${academy.id}:${id}` : "");
    currentScope.current = next; setRecordScope(next); setSessionId(id); setSavingId(""); setMessage("");
  }

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

  useEffect(() => { async function initialise() { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (response.status === 401) return setMessage("Please sign in before marking attendance."); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create an academy, batch, and class session before marking attendance."); setAcademy(academies[0]); await loadWorkspace(academies[0].id); } catch { setMessage("Attendance could not be loaded. Confirm the API is running on port 5092."); } } void initialise(); }, []);
  useEffect(() => {
    if (!academy?.id || !sessionId) return;
    let active = true;
    const generation = recordScope.generation;
    void readAttendance(academy.id, sessionId).then((data) => {
      if (active && mounted.current && currentScope.current.generation === generation) {
        setRecords(current => ({ ...current, [sessionId]: data }));
        setRecordScope(current => current.generation === generation ? { ...current, phase: "ready" } : current);
      }
    }).catch(() => {
      if (active && mounted.current && currentScope.current.generation === generation) {
        setRecordScope(current => current.generation === generation ? { ...current, phase: "error" } : current);
        setMessage("Attendance records could not be loaded.");
      }
    });
    return () => { active = false; };
  }, [sessionId, academy?.id, recordScope.generation]);

  const selectedSession = sessions.find((session) => session.id === sessionId);
  const roster = selectedSession ? enrollments.filter((item) => item.batchId === selectedSession.batchId && item.status === "Active").map((item) => students.find((student) => student.id === item.studentId)).filter((student): student is Student => Boolean(student)) : [];
  const recordsLoaded = recordScope.key === selectionKey && recordScope.phase === "ready" && Object.hasOwn(records, sessionId);
  const recordFor = (studentId: string) => records[sessionId]?.find((record) => record.studentId === studentId);
  const notesFor = (studentId: string) => notesByStudent[`${sessionId}:${studentId}`] ?? recordFor(studentId)?.notes ?? "";

  async function mark(studentId: string, status: string) {
    const generation = recordScope.generation;
    const active = () => mounted.current && currentScope.current.generation === generation;
    if (!academy || !sessionId || !recordsLoaded || !active() || pendingSave.current === generation || !roster.some(student => student.id === studentId)) return;
    const targetSession = sessionId;
    const draftKey = `${targetSession}:${studentId}`;
    pendingSave.current = generation;
    setSavingId(studentId); setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/sessions/${targetSession}/attendance`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, status, notes: notesFor(studentId) || null }) });
      if (!active()) return;
      if (!response.ok) {
        const details = await response.json().catch(() => null);
        const detailMessage = typeof details?.message === "string" ? details.message.trim() : "";
        if (active()) setMessage(detailMessage || "Attendance could not be saved. Your notes have been retained.");
        return;
      }
      setMessage("Attendance saved.");
      try {
        const data = await readAttendance(academy.id, targetSession);
        if (!active()) return;
        setRecords(current => ({ ...current, [targetSession]: data }));
        setNotesByStudent((current) => { const next = { ...current }; delete next[draftKey]; return next; });
      } catch { if (active()) setMessage("Attendance saved, but records could not be refreshed. Your notes have been retained; refresh to see the saved record."); }
    } catch { if (active()) setMessage("Attendance could not be confirmed. Your notes have been retained; check the saved record before retrying."); }
    finally { if (pendingSave.current === generation) pendingSave.current = null; if (active()) setSavingId(""); }
  }

  return <main className="enterprise-settings enterprise-legacy-standard attendance-standard min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="attendance-content mx-auto max-w-5xl px-6 py-10">
    <header className="attendance-heading"><div className="attendance-title"><span className="attendance-title-icon" aria-hidden="true">✓</span><div><p>Class records</p><h1>Attendance</h1></div></div></header>
    {message && <p role="status" aria-live="polite" className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className="attendance-panel"><header className="attendance-panel-header"><div><p>Session</p><h2>Mark attendance</h2></div><span>{roster.length} students</span></header><div className="attendance-session-picker"><StandardSelectField name="session" value={sessionId} disabled={Boolean(savingId)} onChange={selectSession} placeholder="Select a class" options={sessions.map((session) => ({ value: session.id, label: sessionLabel(session, batches) }))} /></div>
      {!sessionId ? <p className="attendance-empty">Schedule a class first.</p> : roster.length === 0 ? <p className="attendance-empty">No active enrolments exist for this batch yet.</p> : !recordsLoaded ? <div className="attendance-empty" role="status">{recordScope.phase === "error" ? <>Attendance records are unavailable. <button type="button" onClick={() => selectSession()}>Retry records</button></> : "Loading attendance records…"}</div> : <ul className="attendance-roster">{roster.map((student) => { const record = recordFor(student.id); return <li key={student.id}><div><b>{student.firstName} {student.lastName}</b><small>{record ? `Marked: ${record.status}` : "Not marked"}</small></div><StandardSelectField name={`attendance-${student.id}`} value={record?.status ?? ""} disabled={Boolean(savingId)} onChange={(status) => { void mark(student.id, status); }} placeholder="Select status" options={statuses.map((status) => ({ value: status, label: status }))} /><input value={notesFor(student.id)} disabled={Boolean(savingId)} maxLength={500} onChange={(event) => setNotesByStudent((current) => ({ ...current, [`${sessionId}:${student.id}`]: event.target.value }))} placeholder="Notes (optional)" /><button disabled={Boolean(savingId) || !record?.status} onClick={() => { const status = record?.status; if (status) void mark(student.id, status); }} className="attendance-save">{savingId === student.id ? "Saving…" : "Save"}</button></li>; })}</ul>}
    </section>
  </div></main>;
}
