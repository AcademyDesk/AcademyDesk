"use client";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { academyApi, apiHeaders, apiUrl } from "@/lib/api";
import { ThemeToggle } from "@/components/theme-toggle";
type P = {
  firstName: string;
  lastName: string;
  batches: { id: string; name: string; capacity: number; deliveryMode: string; meetingLink?: string; roomName?: string }[];
  sessions: {
    id: string;
    batchId: string;
    startUtc: string;
    deliveryMode: string;
    roomName?: string;
    status: string;
    teacherAttendanceStatus?: string | null;
  }[];
};
type S = { id: string; firstName: string; lastName: string };
type A = { studentId: string; status: string };
type PracticeLog = {
  id: string;
  studentName: string;
  practiceDate: string;
  minutesPracticed: number;
  focusArea?: string;
  notes?: string;
  teacherFeedback?: string;
  status: string;
};
type T = "today" | "classes" | "classroom" | "homework" | "progress" | "more";
const tabs: [T, string, string][] = [
  ["today", "Today", "⌂"],
  ["classes", "Classes", "◷"],
  ["classroom", "Classroom", "♙"],
  ["homework", "Homework", "✓"],
  ["progress", "Progress & pay", "◌"],
  ["more", "Profile & leave", "•••"],
];
const statuses = ["Present", "Absent", "Late", "Excused", "Online"];
const dt = (x: string) =>
  new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Asia/Kolkata",
  }).format(new Date(x));
export default function Teacher() {
  const [p, setP] = useState<P>();
  const [sid, setSid] = useState("");
  const [roster, setRoster] = useState<S[]>([]);
  const [attendance, setAttendance] = useState<A[]>([]);
  const [t, setT] = useState<T>("today");
  const [m, setM] = useState("Loading your teaching workspace…");
  const [busy, setBusy] = useState("");
  async function loadRoster(id = sid) {
    if (!id) return;
    const [a, b] = await Promise.all([
      academyApi(`/api/teacher/sessions/${id}/roster`),
      academyApi(`/api/teacher/sessions/${id}/attendance`),
    ]);
    if (!a.ok) throw Error();
    setRoster(await a.json());
    setAttendance(b.ok ? await b.json() : []);
  }
  async function load() {
    const r = await academyApi("/api/teacher/me");
    if (!r.ok) throw Error();
    const x: P = await r.json();
    setP(x);
    if (x.sessions[0]) {
      setSid(x.sessions[0].id);
      await loadRoster(x.sessions[0].id);
    }
    setM("");
  }
  useEffect(() => {
    void load().catch(() =>
      setM("This account is not linked to an active teacher profile."),
    );
  }, []);
  async function submitAttendance(teacherStatus: string, records: { studentId: string; status: string }[]) {
    if (!sid) return;
    setBusy("attendance");
    const [studentsResponse, teacherResponse] = await Promise.all([
      academyApi(`/api/teacher/sessions/${sid}/attendance/bulk`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ records: records.map((record) => ({ ...record, notes: null })) }) }),
      academyApi(`/api/teacher/sessions/${sid}/teacher-attendance`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ status: teacherStatus }) }),
    ]);
    if (studentsResponse.ok && teacherResponse.ok) {
      setP((current) => current ? { ...current, sessions: current.sessions.map((session) => session.id === sid ? { ...session, teacherAttendanceStatus: teacherStatus } : session) } : current);
      await loadRoster();
    } else setM("Attendance could not be submitted. Please try again.");
    setBusy("");
  }
  async function selectClassroomSession(id: string) {
    setSid(id);
    setRoster([]);
    setAttendance([]);
    if (id) await loadRoster(id);
  }
  function updateSessionStatus(id: string, status: "InProgress" | "Completed") {
    setP((current) => current ? { ...current, sessions: current.sessions.map((session) => session.id === id ? { ...session, status } : session) } : current);
  }
  const name = (id: string) =>
    p?.batches.find((x) => x.id === id)?.name || "Assigned batch";
  return (
    <main className="enterprise-app-shell teacher-portal-shell">
      <aside className="enterprise-sidebar teacher-portal-sidebar">
        <a href="/teacher" className="enterprise-brand">
          <span>A</span>
          <strong>AcademyDesk</strong>
        </a>
        <nav className="enterprise-nav-section teacher-portal-nav" aria-label="Teacher workspace">
          <p>Teacher portal</p>
          {tabs.map(([k, x, i]) => (
            <button key={k} data-active={t === k} onClick={() => { if (k === "classroom") { setSid(""); setRoster([]); setAttendance([]); } setT(k); }}>
              <i>{i}</i>
              {x}
            </button>
          ))}
        </nav>
      </aside>
      <section className="enterprise-workspace teacher-portal-workspace">
        <header className="enterprise-topbar">
          <div className="teacher-portal-context"><span>Teacher portal</span><strong>{p ? `${p.firstName} ${p.lastName}` : "Teacher workspace"}</strong></div>
          <div className="enterprise-utilities"><ThemeToggle /><TeacherPortalProfile /></div>
        </header>
        <section className="learner-content teacher-portal-content">
          {m ? (
            <p className="learner-state" role="status" aria-live="polite">
              {m}
            </p>
          ) : (
            p && (
              <>
                {t === "classroom" && p.sessions.find((session) => session.id === sid)?.status === "InProgress" && <TeacherActiveClassBanner batch={p.batches.find((batch) => batch.id === p.sessions.find((session) => session.id === sid)?.batchId)} rosterCount={roster.length} status="In progress" />}
                {t === "classroom" && sid && <TeacherClassroomHero batch={p.batches.find((batch) => batch.id === p.sessions.find((session) => session.id === sid)?.batchId)} sessionId={sid} sessionStatus={p.sessions.find((session) => session.id === sid)?.status} onStatusChange={(status) => updateSessionStatus(sid, status)} />}
                {t === "classroom" && sid && <TeacherAttendanceRoster sessionId={sid} roster={roster} attendance={attendance} teacherStatus={p.sessions.find((session) => session.id === sid)?.teacherAttendanceStatus} busy={busy === "attendance"} onSubmit={submitAttendance} />}
                {t === "classroom" && <TeacherClassroom batches={p.batches} sessions={p.sessions} sessionId={sid} roster={roster} onSessionSelect={(id) => void selectClassroomSession(id)} />}
                {t !== "classroom" && <header className="learner-heading">
                  <p>Teacher workspace</p>
                  <h1>
                    {t === "today"
                      ? `Welcome, ${p.firstName}`
                      : t[0].toUpperCase() + t.slice(1)}
                  </h1>
                  <span>
                    Teaching delivery, learner progress, and class actions
                    scoped to your assigned batches.
                  </span>
                </header>}
                {t === "today" && (
                  <section className="learner-kpis">
                    <K
                      a="Assigned batches"
                      b={String(p.batches.length)}
                      c="Active teaching groups"
                    />
                    <K
                      a="Upcoming classes"
                      b={String(p.sessions.length)}
                      c="Your timetable"
                    />
                    <K
                      a="Selected roster"
                      b={String(roster.length)}
                      c="Ready for attendance"
                    />
                    <K
                      a="Class status"
                      b={p.sessions.find((x) => x.id === sid)?.status || "—"}
                      c="Current session"
                    />
                  </section>
                )}
                {(t === "today" || t === "classes") && (
                  <Panel title="My timetable">
                    {p.sessions.map((x) => (
                      <button
                        className="learner-row"
                        key={x.id}
                        onClick={() => {
                          setSid(x.id);
                          void loadRoster(x.id);
                          setT("classroom");
                        }}
                      >
                        <b>{name(x.batchId)}</b>
                        <small>
                          {dt(x.startUtc) +
                            " · " +
                            x.deliveryMode +
                            (x.roomName ? " · " + x.roomName : "")}
                        </small>
                        <small>Open classroom →</small>
                      </button>
                    ))}
                  </Panel>
                )}
                {t === "homework" && <TeacherTasks batches={p.batches} />}{" "}
                {t === "progress" && <TeacherProgress />}
                {t === "more" && <TeacherSelfService />}
              </>
            )
          )}
        </section>
      </section>
      <nav className="learner-bottom-nav">
        {tabs.map(([k, x, i]) => (
          <button key={k} data-active={t === k} onClick={() => setT(k)}>
            <i>{i}</i>
            <span>{x}</span>
          </button>
        ))}
      </nav>
    </main>
  );
}

function TeacherPortalProfile() {
  const router = useRouter();
  const [account, setAccount] = useState<{ displayName?: string; profileImageUrl?: string | null }>({});
  const [open, setOpen] = useState(false);
  const [message, setMessage] = useState("");
  const ref = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const name = account.displayName || "Teacher";
  const initials = name.split(" ").map((part) => part[0]).join("").slice(0, 2).toUpperCase();
  const imageUrl = account.profileImageUrl ? account.profileImageUrl.startsWith("http") ? account.profileImageUrl : `${apiUrl}${account.profileImageUrl}` : undefined;
  useEffect(() => { void academyApi("/api/auth/session").then(async (response) => response.ok && setAccount(await response.json())).catch(() => undefined); }, []);
  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) setOpen(false); };
    const escape = (event: KeyboardEvent) => { if (event.key === "Escape") setOpen(false); };
    document.addEventListener("mousedown", close); document.addEventListener("keydown", escape);
    return () => { document.removeEventListener("mousedown", close); document.removeEventListener("keydown", escape); };
  }, [open]);
  async function upload(event: React.ChangeEvent<HTMLInputElement>) {
    const image = event.target.files?.[0]; event.target.value = ""; if (!image) return;
    setMessage("Uploading…"); const body = new FormData(); body.append("image", image);
    const response = await academyApi("/api/auth/session/profile-image", { method: "POST", body }); const result = await response.json().catch(() => null);
    if (!response.ok) return setMessage(result?.message ?? "Profile image could not be saved.");
    setAccount((current) => ({ ...current, profileImageUrl: result.profileImageUrl })); setMessage(""); setOpen(false);
  }
  function signOut() { window.localStorage.removeItem("academydesk.accessToken"); window.localStorage.removeItem("academydesk.refreshToken"); router.push("/login"); }
  return <div className="enterprise-profile" ref={ref}>
    <button type="button" className="enterprise-profile-trigger" onClick={() => { setMessage(""); setOpen((value) => !value); }} aria-label="Open profile menu" aria-expanded={open}>
      {imageUrl ? <img src={imageUrl} alt="Profile" /> : <span>{initials}</span>}
    </button>
    {open && <div className="enterprise-profile-menu" role="menu"><strong>{name}</strong><small>Teacher portal</small><input ref={inputRef} className="sr-only" type="file" accept="image/png,image/jpeg,image/webp" onChange={(event) => void upload(event)} /><button type="button" className="enterprise-profile-menu-action" onClick={() => inputRef.current?.click()}>Edit profile picture</button><button type="button" className="enterprise-profile-menu-action" onClick={signOut}>Sign out</button>{message && <small role="status">{message}</small>}</div>}
  </div>;
}
function K({ a, b, c }: { a: string; b: string; c: string }) {
  return (
    <article className="learner-kpi">
      <span>{a}</span>
      <b>{b}</b>
      <small>{c}</small>
    </article>
  );
}
function R({ a, b }: { a: string; b: string }) {
  return (
    <article className="learner-row">
      <b>{a}</b>
      <small>{b}</small>
    </article>
  );
}
function Panel({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section className="learner-panel">
      <h3>{title}</h3>
      <div className="learner-list">{children}</div>
    </section>
  );
}
type TeacherBatch = P["batches"][number];
type Resource = { id: string; batchId: string; studentId?: string; classSessionId?: string; title: string; description?: string; type: string; url: string; createdAtUtc: string };

function TeacherAttendanceRoster({ sessionId, roster, attendance, teacherStatus, busy, onSubmit }: { sessionId: string; roster: S[]; attendance: A[]; teacherStatus?: string | null; busy: boolean; onSubmit: (teacherStatus: string, records: { studentId: string; status: string }[]) => Promise<void> }) {
  const [draft, setDraft] = useState<Record<string, string>>({});
  const [teacherDraft, setTeacherDraft] = useState(teacherStatus ?? "Present");
  useEffect(() => { setDraft(Object.fromEntries(roster.map((student) => [student.id, attendance.find((item) => item.studentId === student.id)?.status ?? "Present"]))); setTeacherDraft(teacherStatus ?? "Present"); }, [attendance, roster, teacherStatus, sessionId]);
  const recorded = attendance.length;
  return <section className="teacher-attendance-panel">
    <header><div><p>Attendance</p><h2>Class attendance</h2><span>{recorded ? `${recorded} of ${roster.length} students submitted` : "Mark the teacher and students, then submit once."}</span></div><strong>{roster.length} students</strong></header>
    <div className="teacher-attendance-teacher"><div><b>Teacher attendance</b><small>Record your attendance for this class.</small></div><AttendanceStatusMenu value={teacherDraft} disabled={busy} label="Teacher attendance" onChange={setTeacherDraft} /></div>
    <div className="teacher-attendance-grid">{roster.map((student) => {
      const value = draft[student.id] ?? "Present";
      const initials = `${student.firstName[0] ?? ""}${student.lastName[0] ?? ""}`.toUpperCase();
      return <article key={student.id}><span className="teacher-student-avatar">{initials}</span><div><b>{student.firstName} {student.lastName}</b><small data-status={value}>{value}</small></div><AttendanceStatusMenu value={value} disabled={busy} label={`Attendance for ${student.firstName} ${student.lastName}`} onChange={(status) => setDraft((current) => ({ ...current, [student.id]: status }))} /></article>;
    })}</div>
    <div className="teacher-attendance-submit"><button type="button" className="enterprise-action-button" disabled={busy || !sessionId} onClick={() => void onSubmit(teacherDraft, roster.map((student) => ({ studentId: student.id, status: draft[student.id] ?? "Present" })))}>{busy ? "Submitting attendance…" : "Submit attendance"}</button></div>
  </section>;
}
function AttendanceStatusMenu({ value, disabled, label, onChange }: { value: string; disabled: boolean; label: string; onChange: (status: string) => void }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) setOpen(false); };
    const escape = (event: KeyboardEvent) => { if (event.key === "Escape") setOpen(false); };
    document.addEventListener("mousedown", close); document.addEventListener("keydown", escape);
    return () => { document.removeEventListener("mousedown", close); document.removeEventListener("keydown", escape); };
  }, [open]);
  return <div className="teacher-attendance-menu" ref={ref}><button type="button" aria-label={label} aria-haspopup="listbox" aria-expanded={open} disabled={disabled} onClick={() => setOpen((current) => !current)}><span data-status={value}>{value}</span><i>⌄</i></button>{open && <div role="listbox" aria-label={label}>{statuses.map((status) => <button type="button" role="option" aria-selected={value === status} key={status} data-active={value === status} onClick={() => { onChange(status); setOpen(false); }}>{status}</button>)}</div>}</div>;
}

type TeacherDropdownOption = { value: string; label: string };
function TeacherDropdown({ name, value, options, onChange, label, required = false }: { name?: string; value: string; options: TeacherDropdownOption[]; onChange: (value: string) => void; label: string; required?: boolean }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const current = options.find((option) => option.value === value) ?? options[0];
  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) setOpen(false); };
    const escape = (event: KeyboardEvent) => { if (event.key === "Escape") setOpen(false); };
    document.addEventListener("mousedown", close); document.addEventListener("keydown", escape);
    return () => { document.removeEventListener("mousedown", close); document.removeEventListener("keydown", escape); };
  }, [open]);
  return <div className="teacher-dropdown" ref={ref}>
    {name && <input type="hidden" name={name} value={value} required={required} />}
    <button type="button" aria-label={label} aria-haspopup="listbox" aria-expanded={open} onClick={() => setOpen((currentOpen) => !currentOpen)}><span>{current?.label ?? label}</span><i aria-hidden="true">⌄</i></button>
    {open && <div role="listbox" aria-label={label}>{options.map((option) => <button type="button" role="option" aria-selected={option.value === value} data-active={option.value === value} key={option.value} onClick={() => { onChange(option.value); setOpen(false); }}>{option.label}</button>)}</div>}
  </div>;
}
function TeacherBatchDropdown({ batches }: { batches: { id: string; name: string }[] }) {
  const [value, setValue] = useState("");
  return <TeacherDropdown name="batchId" required label="Assigned batch" value={value} onChange={setValue} options={[{ value: "", label: "Select assigned batch…" }, ...batches.map((batch) => ({ value: batch.id, label: batch.name }))]} />;
}

type ClassroomActivity = { resources: Resource[]; homework: { id: string; batchId: string; studentId?: string; title: string; description?: string; dueAtUtc?: string; type: string; isPublished: boolean }[] };
function TeacherActiveClassBanner({ batch, rosterCount, status }: { batch?: TeacherBatch; rosterCount: number; status?: string }) {
  return <section className="teacher-active-class-banner">
    <span className="teacher-live-dot" /><div><b>Active class</b><small>{batch ? `${batch.name} · ${batch.capacity === 1 ? "1:1 lesson" : `${rosterCount || batch.capacity} students`}` : "Select a class to begin"}</small></div><em>{status ?? "Ready"}</em>
  </section>;
}
function TeacherClassroomHero({ batch, sessionId, sessionStatus, onStatusChange }: { batch?: TeacherBatch; sessionId: string; sessionStatus?: string; onStatusChange: (status: "InProgress" | "Completed") => void }) {
  const isLiveDelivery = batch?.deliveryMode === "Online" || batch?.deliveryMode === "Hybrid";
  async function updateStatus(status: "InProgress" | "Completed") {
    if (!sessionId) return;
    const response = await academyApi(`/api/teacher/sessions/${sessionId}/status`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ status }) });
    if (response.ok) onStatusChange(status);
  }
  async function startOnlineClass() {
    if (!batch?.meetingLink) return;
    await updateStatus("InProgress");
    window.open(batch.meetingLink, "_blank", "noopener,noreferrer");
  }
  if (!batch) return null;
  return <section className="teacher-classroom-hero">
    <div><p>Classroom</p><h2>{batch?.name ?? "Select a class"}</h2><span>{batch?.deliveryMode ?? ""}{batch?.roomName ? ` · ${batch.roomName}` : ""}</span></div>
    <div className="teacher-classroom-hero-actions">{sessionId && sessionStatus !== "InProgress" && sessionStatus !== "Completed" && <button type="button" className="enterprise-action-button" onClick={() => void (isLiveDelivery ? startOnlineClass() : updateStatus("InProgress"))}>{isLiveDelivery ? "Start online class" : "Start class"}</button>}{isLiveDelivery && sessionId && sessionStatus === "InProgress" && <button type="button" className="enterprise-action-button" onClick={() => void startOnlineClass()}>Rejoin online class</button>}{sessionId && sessionStatus === "InProgress" && <button type="button" className="enterprise-action-button enterprise-action-button-secondary" onClick={() => void updateStatus("Completed")}>Complete class</button>}</div>
  </section>;
}
function TeacherClassroom({ batches, sessions, sessionId, roster, onSessionSelect }: { batches: TeacherBatch[]; sessions: P["sessions"]; sessionId: string; roster: S[]; onSessionSelect: (id: string) => void }) {
  const [batchId, setBatchId] = useState("");
  const [studentId, setStudentId] = useState("");
  const [resources, setResources] = useState<Resource[]>([]);
  const [activity, setActivity] = useState<ClassroomActivity>({ resources: [], homework: [] });
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [message, setMessage] = useState("");
  const [recording, setRecording] = useState(false);
  const [recordingPaused, setRecordingPaused] = useState(false);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [pendingPreviewUrl, setPendingPreviewUrl] = useState("");
  const recorder = useRef<MediaRecorder | null>(null);
  const chunks = useRef<Blob[]>([]);
  const selected = batches.find((batch) => batch.id === batchId);
  const selectedStudent = roster.find((student) => student.id === studentId);
  function stageFile(file: File | null) {
    if (pendingPreviewUrl) URL.revokeObjectURL(pendingPreviewUrl);
    setPendingFile(file); setPendingPreviewUrl(file ? URL.createObjectURL(file) : "");
  }
  async function refresh(id = batchId) {
    if (!id) return;
    const [materials, history] = await Promise.all([
      academyApi(`/api/teacher/resources?batchId=${id}`),
      academyApi(`/api/teacher/classroom-activity?batchId=${id}${studentId ? `&studentId=${studentId}` : ""}${fromDate ? `&fromUtc=${encodeURIComponent(fromDate)}` : ""}${toDate ? `&toUtc=${encodeURIComponent(toDate)}` : ""}`),
    ]);
    if (materials.ok) setResources(await materials.json());
    if (history.ok) setActivity(await history.json());
  }
  useEffect(() => { setBatchId(sessions.find((session) => session.id === sessionId)?.batchId ?? ""); }, [sessionId, sessions]);
  useEffect(() => { void refresh(); }, [batchId, studentId, fromDate, toDate]);
  async function addNote(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget);
    const response = await academyApi("/api/teacher/resources/note", { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ batchId, studentId: studentId || null, classSessionId: sessionId || null, title: form.get("title"), notes: form.get("notes"), type: "Note" }) });
    setMessage(response.ok ? "Class note saved." : "Class note could not be saved."); if (response.ok) { event.currentTarget.reset(); await refresh(); }
  }
  async function upload(file: File, title?: string, description?: string, type = "Attachment") {
    const form = new FormData(); form.append("batchId", batchId); if (studentId) form.append("studentId", studentId); if (sessionId) form.append("classSessionId", sessionId); form.append("file", file); form.append("title", title || file.name); form.append("description", description || ""); form.append("type", type);
    const response = await academyApi("/api/teacher/resources/upload", { method: "POST", body: form });
    setMessage(response.ok ? "Material uploaded." : "Material could not be uploaded."); if (response.ok) await refresh();
  }
  async function addFile(event: React.FormEvent<HTMLFormElement>) { event.preventDefault(); const form = new FormData(event.currentTarget); if (pendingFile) { await upload(pendingFile, String(form.get("title") || pendingFile.name), String(form.get("description") || ""), "Attachment"); stageFile(null); event.currentTarget.reset(); } else setMessage("Choose a file or record audio before uploading."); }
  async function toggleRecording() {
    if (recording) { recorder.current?.stop(); return; }
    try { const stream = await navigator.mediaDevices.getUserMedia({ audio: true }); const current = new MediaRecorder(stream); chunks.current = []; current.ondataavailable = (event) => chunks.current.push(event.data); current.onstop = () => { stream.getTracks().forEach((track) => track.stop()); setRecording(false); setRecordingPaused(false); stageFile(new File([new Blob(chunks.current, { type: current.mimeType || "audio/webm" })], `class-recording-${Date.now()}.webm`, { type: current.mimeType || "audio/webm" })); setMessage("Recording ready. Listen to it below, then upload when you are happy."); }; recorder.current = current; current.start(); setRecording(true); } catch { setMessage("Microphone access is required to record class audio."); }
  }
  function toggleRecordingPause() { if (!recorder.current) return; if (recorder.current.state === "recording") { recorder.current.pause(); setRecordingPaused(true); } else if (recorder.current.state === "paused") { recorder.current.resume(); setRecordingPaused(false); } }
  async function assignHomework(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget);
    const response = await academyApi("/api/teacher/assignments", { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ batchId, studentId: studentId || null, title: form.get("title"), description: form.get("description"), dueAtUtc: form.get("dueAtUtc") ? new Date(String(form.get("dueAtUtc"))).toISOString() : null, type: "Homework", isPublished: true }) });
    setMessage(response.ok ? "Homework assigned." : "Homework could not be assigned.");
    if (response.ok) { event.currentTarget.reset(); await refresh(); }
  }
  return <>
    <section className="teacher-classroom-controls" data-selected={Boolean(selected)}>
      <label>Class<TeacherDropdown label="Class" value={batchId} onChange={(value) => { setBatchId(value); setStudentId(""); const nextSession = sessions.find((session) => session.batchId === value && session.status !== "Completed") ?? sessions.find((session) => session.batchId === value); onSessionSelect(nextSession?.id ?? ""); }} options={[{ value: "", label: "Select class" }, ...batches.map((batch) => ({ value: batch.id, label: `${batch.name}${batch.capacity === 1 ? " · 1:1" : ""}` }))]} /></label>
      {selected && <label>Teaching focus<TeacherDropdown label="Teaching focus" value={studentId} onChange={setStudentId} options={[{ value: "", label: "Whole class" }, ...roster.map((student) => ({ value: student.id, label: `${student.firstName} ${student.lastName}` }))]} /></label>}
    </section>
    {selected && <>
    <section className="teacher-classroom-grid">
      <Panel title={selectedStudent ? `Notes for ${selectedStudent.firstName}` : "Notes"}><form className="learner-form teacher-note-form" onSubmit={(event) => void addNote(event)}><input required name="title" placeholder="Note title" /><textarea required name="notes" placeholder="Add a comment" /><button>Save note</button></form></Panel>
      <Panel title={selectedStudent ? `Attachments & recordings for ${selectedStudent.firstName}` : "Attachments & recordings"}><form className="learner-form teacher-upload-form" onSubmit={(event) => void addFile(event)}><input name="title" placeholder="Attachment title" /><input name="description" placeholder="Comment (optional)" /><label className="teacher-file-picker"><input name="file" type="file" accept="image/*,.pdf,audio/*,video/*,.doc,.docx" onChange={(event) => stageFile(event.target.files?.[0] ?? null)} /><span>{pendingFile ? pendingFile.name : "Choose photo, video, audio, PDF, or document"}</span></label>{pendingFile && <div className="teacher-file-preview"><b>Ready to review</b>{pendingFile.type.startsWith("audio/") && <audio controls src={pendingPreviewUrl} />}{pendingFile.type.startsWith("video/") && <video controls src={pendingPreviewUrl} />}{pendingFile.type.startsWith("image/") && <img src={pendingPreviewUrl} alt="Selected upload preview" />} {!/^(audio|video|image)\//.test(pendingFile.type) && <small>{pendingFile.name} · {(pendingFile.size / 1024 / 1024).toFixed(1)} MB</small>}<button type="button" className="teacher-clear-file" onClick={() => stageFile(null)}>Remove</button></div>}<div className="teacher-material-actions"><button disabled={!pendingFile}>Upload confirmed file</button>{!recording ? <button type="button" className="teacher-mic-button" aria-label="Start audio recording" title="Start audio recording" onClick={() => void toggleRecording()}>🎙</button> : <><span className="teacher-recording-state">● Recording{recordingPaused ? " paused" : ""}</span><button type="button" className="enterprise-action-button enterprise-action-button-secondary" onClick={toggleRecordingPause}>{recordingPaused ? "Resume" : "Pause"}</button><button type="button" className="teacher-stop-button" onClick={() => void toggleRecording()}>Stop</button></>}</div></form></Panel>
      <Panel title={selectedStudent ? `Homework for ${selectedStudent.firstName}` : "Homework (optional)"}><form className="learner-form" onSubmit={(event) => void assignHomework(event)}><input required name="title" placeholder="Homework title" /><textarea name="description" placeholder="Add a comment" /><button>Assign homework</button></form><div className="teacher-activity-list">{activity.homework.length ? activity.homework.map((item) => <article key={item.id}><b>{item.title}</b><small>{item.studentId ? "Individual" : "Whole class"}</small></article>) : <p className="learner-empty">No homework has been assigned for this view.</p>}</div></Panel>
    </section>
    <Panel title={selectedStudent ? `${selectedStudent.firstName}'s history` : "Class history"}>{!selectedStudent && <div className="teacher-history-filter"><label>From<input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} /></label><label>To<input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} /></label><button type="button" onClick={() => { setFromDate(""); setToDate(""); }}>Clear</button></div>}<div className="teacher-activity-list">{activity.resources.length || activity.homework.length ? <>{activity.resources.map((item) => <article key={item.id}><div><b>{item.title}</b><small>{item.type}{item.studentId ? " · individual" : " · whole class"} · {dt(item.createdAtUtc)}</small>{item.description && <p>{item.description}</p>}</div>{!item.url.startsWith("note://") && <a href={`${apiUrl}${item.url}`} target="_blank" rel="noreferrer">Open</a>}</article>)}{activity.homework.map((item) => <article key={item.id}><div><b>{item.title}</b><small>Homework{item.studentId ? " · individual" : " · whole class"}{item.dueAtUtc ? ` · due ${dt(item.dueAtUtc)}` : ""}</small>{item.description && <p>{item.description}</p>}</div></article>)}</> : <p className="learner-empty">No class history for this period.</p>}</div></Panel>
    {message && <p className="teacher-classroom-message" role="status">{message}</p>}
    </>}
  </>;
}

function TeacherProgress() {
  const [progress, setProgress] = useState<{ completedClasses: number; upcomingClasses: number; attendanceRecords: number; presentOrOnline: number }>();
  const [payments, setPayments] = useState<{ paymentModel?: string; monthlyAmount?: number; amountPerCycle?: number; sessionsPerCycle?: number; payslips: { id: string; payslipNumber: string; periodLabel: string; netAmount: number; currency: string; status: string; paidAtUtc: string }[] }>();
  useEffect(() => { void Promise.all([academyApi("/api/teacher/progress"), academyApi("/api/teacher/payments")]).then(async ([a, b]) => { if (a.ok) setProgress(await a.json()); if (b.ok) setPayments(await b.json()); }); }, []);
  return <><section className="learner-kpis"><K a="Classes completed" b={String(progress?.completedClasses ?? "—")} c="Delivered sessions" /><K a="Upcoming classes" b={String(progress?.upcomingClasses ?? "—")} c="Scheduled ahead" /><K a="Attendance marked" b={String(progress?.attendanceRecords ?? "—")} c="Student records" /><K a="Present / online" b={String(progress?.presentOrOnline ?? "—")} c="Attendance outcomes" /></section><Panel title="Payment and payslips"><div className="learner-list"><R a={`Payment model: ${payments?.paymentModel ?? "Not configured"}`} b={payments?.monthlyAmount ? `Monthly salary ₹${payments.monthlyAmount.toLocaleString("en-IN")}` : payments?.amountPerCycle ? `₹${payments.amountPerCycle.toLocaleString("en-IN")} per ${payments.sessionsPerCycle ?? "configured"} sessions` : "Payment details will appear when finance completes setup."} />{payments?.payslips?.length ? payments.payslips.map((item) => <R key={item.id} a={`${item.payslipNumber} · ${item.periodLabel}`} b={`₹${item.netAmount.toLocaleString("en-IN")} · ${item.status}`} />) : <p className="learner-empty">No payslips issued yet.</p>}</div></Panel></>;
}

function TeacherTasks({
  batches,
}: {
  batches: { id: string; name: string }[];
}) {
  const [m, setM] = useState("");
  const [practiceLogs, setPracticeLogs] = useState<PracticeLog[]>([]);
  const [feedback, setFeedback] = useState<Record<string, string>>({});
  const [reviewing, setReviewing] = useState("");
  useEffect(() => {
    void academyApi("/api/teacher/practice-logs")
      .then(async (response) => {
        if (!response.ok) throw new Error();
        setPracticeLogs(await response.json());
      })
      .catch(() => setM("Practice feedback could not be loaded."));
  }, []);
  async function create(e: React.FormEvent<HTMLFormElement>, path: string) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    const body = Object.fromEntries(f) as Record<string, unknown>;
    if (!body.batchId) {
      setM("Select an assigned batch before saving.");
      return;
    }
    if ("maxScore" in body) body.maxScore = Number(body.maxScore);
    if ("scheduledAtUtc" in body && body.scheduledAtUtc)
      body.scheduledAtUtc = new Date(String(body.scheduledAtUtc)).toISOString();
    const r = await academyApi(path, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify(body),
    });
    setM(
      r.ok
        ? "Saved for your assigned batch."
        : "The teaching record could not be saved.",
    );
    if (r.ok) e.currentTarget.reset();
  }
  const batch = <TeacherBatchDropdown batches={batches} />;
  async function reviewPractice(log: PracticeLog) {
    setReviewing(log.id);
    const response = await academyApi(`/api/teacher/practice-logs/${log.id}/review`, {
      method: "PATCH",
      headers: apiHeaders(true),
      body: JSON.stringify({ teacherFeedback: feedback[log.id] ?? log.teacherFeedback ?? null }),
    });
    if (!response.ok) {
      setM("Practice feedback could not be saved.");
    } else {
      const result = await response.json();
      setPracticeLogs((current) => current.map((item) => item.id === log.id ? { ...item, status: result.status, teacherFeedback: result.teacherFeedback } : item));
      setM("Practice feedback saved.");
    }
    setReviewing("");
  }
  return (
    <>
      <section className="learner-panel">
        <h3>Assign homework</h3>
        <form
          className="learner-form"
          onSubmit={(e) => void create(e, "/api/teacher/assignments")}
        >
          {batch}
          <input required name="title" placeholder="Assignment title" />
          <textarea
            name="description"
            placeholder="Instructions for learners"
          />
          <input name="type" defaultValue="Homework" />
          <input name="isPublished" type="hidden" value="true" />
          <button>Assign homework</button>
        </form>
      </section>
      <section className="learner-panel">
        <h3>Create lesson plan</h3>
        <form
          className="learner-form"
          onSubmit={(e) => void create(e, "/api/teacher/lesson-plans")}
        >
          {batch}
          <input required name="title" placeholder="Lesson title" />
          <textarea name="objectives" placeholder="Learning objectives" />
          <button>Save lesson plan</button>
        </form>
      </section>
      <section className="learner-panel">
        <h3>Create assessment</h3>
        <form
          className="learner-form"
          onSubmit={(e) => void create(e, "/api/teacher/assessments")}
        >
          {batch}
          <input required name="title" placeholder="Assessment title" />
          <input
            required
            name="maxScore"
            type="number"
            min="1"
            placeholder="Maximum score"
          />
          <input name="type" defaultValue="Performance" />
          <input name="scheduledAtUtc" type="datetime-local" />
          <input name="isPublished" type="hidden" value="false" />
          <button>Create assessment</button>
        </form>
        <small>{m}</small>
      </section>
      <section className="learner-panel">
        <h3>Practice feedback</h3>
        <div className="learner-list">
          {practiceLogs.length === 0 ? <p className="learner-empty">No practice logs from your assigned students.</p> : practiceLogs.map((log) => (
            <article className="learner-row" key={log.id}>
              <b>{log.studentName} · {log.minutesPracticed} min</b>
              <small>{log.practiceDate}{log.focusArea ? ` · ${log.focusArea}` : ""}{log.notes ? ` · ${log.notes}` : ""}</small>
              <input value={feedback[log.id] ?? log.teacherFeedback ?? ""} onChange={(event) => setFeedback((current) => ({ ...current, [log.id]: event.target.value }))} placeholder="Feedback for student" />
              <button disabled={reviewing === log.id} onClick={() => void reviewPractice(log)}>{reviewing === log.id ? "Saving…" : log.status === "Reviewed" ? "Update feedback" : "Save feedback"}</button>
            </article>
          ))}
        </div>
      </section>
    </>
  );
}

function TeacherSelfService() {
  const [message, setMessage] = useState("");
  async function save(event: React.FormEvent<HTMLFormElement>, path: string) {
    event.preventDefault();
    const response = await academyApi(path, {
      method: path === "/api/teacher/profile" ? "PUT" : "POST",
      headers: apiHeaders(true),
      body: JSON.stringify(
        Object.fromEntries(new FormData(event.currentTarget)),
      ),
    });
    setMessage(
      response.ok ? "Saved successfully." : "The request could not be saved.",
    );
    if (response.ok) event.currentTarget.reset();
  }
  return (
    <>
      <Panel title="My contact profile">
        <form
          className="learner-form"
          onSubmit={(event) => void save(event, "/api/teacher/profile")}
        >
          <input name="email" type="email" placeholder="Email address" />
          <input name="phone" placeholder="Phone number" />
          <button>Save profile</button>
        </form>
      </Panel>
      <Panel title="Request leave">
        <form
          className="learner-form"
          onSubmit={(event) => void save(event, "/api/teacher/leave-requests")}
        >
          <input required name="startDate" type="date" />
          <input required name="endDate" type="date" />
          <textarea required name="reason" placeholder="Reason for leave" />
          <button>Send leave request</button>
          <small>{message}</small>
        </form>
      </Panel>
      <Panel title="Teaching review queues">
        <R
          a="Submission review"
          b="Open a published assignment to review learner work and return feedback."
        />
        <R
          a="Assessment results"
          b="Record and publish results for active learners in your assigned batches."
        />
      </Panel>
    </>
  );
}
