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
type AttendanceWorkspace = { students: Student[]; batches: Batch[]; enrollments: Enrollment[]; sessions: Session[] };
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
  const workspaceVersion = useRef(0), readingWorkspace = useRef(false);
  const [workspacePhase, setWorkspacePhase] = useState<"loading" | "ready" | "error">("loading");
  const workspaceReady = workspacePhase === "ready";
  if (recordScope.key !== selectionKey) { setRecordScope(initialScope(selectionKey)); setSavingId(""); setMessage(""); }
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; ++workspaceVersion.current; readingWorkspace.current = false; }; }, []);
  useEffect(() => { currentScope.current = recordScope; }, [recordScope]);
  function selectSession(id = sessionId) {
    const next = initialScope(academy?.id && id ? `${academy.id}:${id}` : "");
    currentScope.current = next; setRecordScope(next); setSessionId(id); setSavingId(""); setMessage("");
  }

  async function loadWorkspace() {
    if (!mounted.current || readingWorkspace.current || pendingSave.current) return;
    const version = ++workspaceVersion.current; readingWorkspace.current = true; setWorkspacePhase("loading");
    const next = initialScope(selectionKey); currentScope.current = next; setRecordScope(next);
    try {
      const context = await academyApi("/api/academies", { cache: "no-store" });
      if (!context.ok) throw new Error(context.status === 401 ? "Sign in again before marking attendance." : "Check your academy access and try again.");
      const academies: Academy[] = await context.json();
      if (!Array.isArray(academies) || !academies.every(x => x && typeof x.id === "string" && x.id.trim()) || !academies.length) throw new Error("No academy is available to this account.");
      const id = academy?.id ?? academies[0].id;
      if (!academies.some(x => x.id === id)) throw new Error("The current academy is unavailable to this account.");
      const response = await academyApi(`/api/academies/${id}/attendance/options`, { cache: "no-store" });
      if (!response.ok) throw new Error(response.status === 401 ? "Sign in again, then reload." : response.status === 403 ? "Ask an administrator to check your attendance access." : "Try reloading the workspace.");
      const value: AttendanceWorkspace = await response.json();
      const ss = value?.students, bs = value?.batches, es = value?.enrollments, cs = value?.sessions;
      const uniqueIds = (rows: { id: string }[]) => rows.every(x => typeof x.id === "string" && Boolean(x.id.trim())) && new Set(rows.map(x => x.id)).size === rows.length;
      if (!Array.isArray(ss) || !ss.every(x => x && typeof x.firstName === "string" && typeof x.lastName === "string") || !uniqueIds(ss)
        || !Array.isArray(bs) || !bs.every(x => x && typeof x.name === "string") || !uniqueIds(bs)
        || !Array.isArray(cs) || !cs.every(x => x && typeof x.batchId === "string" && bs.some(b => b.id === x.batchId) && typeof x.startUtc === "string" && Number.isFinite(Date.parse(x.startUtc))) || !uniqueIds(cs)
        || !Array.isArray(es) || !es.every(x => x && x.status === "Active" && ss.some(s => s.id === x.studentId) && bs.some(b => b.id === x.batchId)) || new Set(es.map(x => JSON.stringify([x.studentId, x.batchId]))).size !== es.length) throw new Error("The attendance workspace returned incomplete records.");
      if (!mounted.current || workspaceVersion.current !== version) return;
      setAcademy({ id }); setStudents(ss); setBatches(bs); setEnrollments(es); setSessions(cs);
      setSessionId(current => cs.some(x => x.id === current) ? current : cs[0]?.id ?? "");
      setWorkspacePhase("ready"); setMessage("");
    } catch (error) {
      if (mounted.current && workspaceVersion.current === version) {
        setWorkspacePhase("error"); setMessage("Attendance workspace could not be loaded. " + (error instanceof Error ? error.message : "Try reloading the workspace."));
      }
    } finally { if (workspaceVersion.current === version) readingWorkspace.current = false; }
  }
  const initialWorkspaceLoad = useRef(loadWorkspace);
  useEffect(() => { void initialWorkspaceLoad.current(); }, []);
  useEffect(() => {
    if (!workspaceReady || !academy?.id || !sessionId) return;
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
  }, [sessionId, academy?.id, recordScope.generation, workspaceReady]);

  const selectedSession = sessions.find((session) => session.id === sessionId);
  const roster = selectedSession ? enrollments.filter((item) => item.batchId === selectedSession.batchId && item.status === "Active").map((item) => students.find((student) => student.id === item.studentId)).filter((student): student is Student => Boolean(student)) : [];
  const recordsLoaded = recordScope.key === selectionKey && recordScope.phase === "ready" && Object.hasOwn(records, sessionId);
  const recordFor = (studentId: string) => records[sessionId]?.find((record) => record.studentId === studentId);
  const notesFor = (studentId: string) => notesByStudent[`${sessionId}:${studentId}`] ?? recordFor(studentId)?.notes ?? "";

  async function mark(studentId: string, status: string) {
    const generation = recordScope.generation;
    const active = () => mounted.current && currentScope.current.generation === generation;
    if (!workspaceReady || !academy || !sessionId || !recordsLoaded || !active() || pendingSave.current === generation || !roster.some(student => student.id === studentId)) return;
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
    <button type="button" className="enterprise-action-button mt-4" disabled={workspacePhase === "loading" || Boolean(savingId)} onClick={() => loadWorkspace()}>Reload attendance workspace</button>
    <section className="attendance-panel"><header className="attendance-panel-header"><div><p>Session</p><h2>Mark attendance</h2></div><span>{workspaceReady ? `${roster.length} students` : "Unavailable"}</span></header><div className="attendance-session-picker"><StandardSelectField name="session" value={sessionId} disabled={Boolean(savingId) || !workspaceReady} onChange={selectSession} placeholder="Select a class" options={sessions.map((session) => ({ value: session.id, label: sessionLabel(session, batches) }))} /></div>
      {!workspaceReady ? <p className="attendance-empty" role="status">{workspacePhase === "loading" ? "Loading attendance workspace…" : "Attendance workspace is unavailable. Reload to try again."}</p> : !sessionId ? <p className="attendance-empty">Schedule a class first.</p> : roster.length === 0 ? <p className="attendance-empty">No active enrolments exist for this batch yet.</p> : !recordsLoaded ? <div className="attendance-empty" role="status">{recordScope.phase === "error" ? <>Attendance records are unavailable. <button type="button" onClick={() => selectSession()}>Retry records</button></> : "Loading attendance records…"}</div> : <ul className="attendance-roster">{roster.map((student) => { const record = recordFor(student.id); return <li key={student.id}><div><b>{student.firstName} {student.lastName}</b><small>{record ? `Marked: ${record.status}` : "Not marked"}</small></div><StandardSelectField name={`attendance-${student.id}`} value={record?.status ?? ""} disabled={Boolean(savingId)} onChange={(status) => { void mark(student.id, status); }} placeholder="Select status" options={statuses.map((status) => ({ value: status, label: status }))} /><input value={notesFor(student.id)} disabled={Boolean(savingId)} maxLength={500} onChange={(event) => setNotesByStudent((current) => ({ ...current, [`${sessionId}:${student.id}`]: event.target.value }))} placeholder="Notes (optional)" /><button disabled={Boolean(savingId) || !record?.status} onClick={() => { const status = record?.status; if (status) void mark(student.id, status); }} className="attendance-save">{savingId === student.id ? "Saving…" : "Save"}</button></li>; })}</ul>}
    </section>
  </div></main>;
}
