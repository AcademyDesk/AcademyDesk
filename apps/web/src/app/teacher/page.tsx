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
  async function mark(studentId: string, status: string) {
    setBusy(studentId);
    const r = await academyApi(`/api/teacher/sessions/${sid}/attendance`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify({ studentId, status, notes: null }),
    });
    if (r.ok) await loadRoster();
    else setM("Attendance could not be saved.");
    setBusy("");
  }
  const name = (id: string) =>
    p?.batches.find((x) => x.id === id)?.name || "Assigned batch";
  const current = (id: string) =>
    attendance.find((x) => x.studentId === id)?.status || "Not marked";
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
            <button key={k} data-active={t === k} onClick={() => setT(k)}>
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
                <header className="learner-heading">
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
                </header>
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
                {t === "classroom" && (
                  <Panel title="Attendance and class roster">
                    {!sid ? (
                      <p className="learner-empty">Select a class first.</p>
                    ) : (
                      roster.map((x) => (
                        <article className="learner-row" key={x.id}>
                          <b>{x.firstName + " " + x.lastName}</b>
                          <small>{current(x.id)}</small>
                          <select
                            disabled={busy === x.id}
                            value={
                              current(x.id) === "Not marked"
                                ? "Present"
                                : current(x.id)
                            }
                            onChange={(e) => void mark(x.id, e.target.value)}
                          >
                            {statuses.map((v) => (
                              <option key={v}>{v}</option>
                            ))}
                          </select>
                        </article>
                      ))
                    )}
                  </Panel>
                )}
                {t === "classroom" && <TeacherClassroom batches={p.batches} sessionId={sid} sessionBatchId={p.sessions.find((session) => session.id === sid)?.batchId} sessionStatus={p.sessions.find((session) => session.id === sid)?.status} roster={roster} />}
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

type ClassroomActivity = { resources: Resource[]; homework: { id: string; batchId: string; studentId?: string; title: string; description?: string; dueAtUtc?: string; type: string; isPublished: boolean }[] };
function TeacherClassroom({ batches, sessionId, sessionBatchId, sessionStatus, roster }: { batches: TeacherBatch[]; sessionId: string; sessionBatchId?: string; sessionStatus?: string; roster: S[] }) {
  const [batchId, setBatchId] = useState(batches[0]?.id ?? "");
  const [studentId, setStudentId] = useState("");
  const [resources, setResources] = useState<Resource[]>([]);
  const [activity, setActivity] = useState<ClassroomActivity>({ resources: [], homework: [] });
  const [message, setMessage] = useState("");
  const [recording, setRecording] = useState(false);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [pendingPreviewUrl, setPendingPreviewUrl] = useState("");
  const recorder = useRef<MediaRecorder | null>(null);
  const chunks = useRef<Blob[]>([]);
  const selected = batches.find((batch) => batch.id === batchId);
  const selectedStudent = roster.find((student) => student.id === studentId);
  const isLiveDelivery = selected?.deliveryMode === "Online" || selected?.deliveryMode === "Hybrid";
  function stageFile(file: File | null) {
    if (pendingPreviewUrl) URL.revokeObjectURL(pendingPreviewUrl);
    setPendingFile(file); setPendingPreviewUrl(file ? URL.createObjectURL(file) : "");
  }
  async function refresh(id = batchId) {
    if (!id) return;
    const [materials, history] = await Promise.all([
      academyApi(`/api/teacher/resources?batchId=${id}`),
      academyApi(`/api/teacher/classroom-activity?batchId=${id}${studentId ? `&studentId=${studentId}` : ""}`),
    ]);
    if (materials.ok) setResources(await materials.json());
    if (history.ok) setActivity(await history.json());
  }
  useEffect(() => { if (sessionBatchId) setBatchId(sessionBatchId); }, [sessionBatchId]);
  useEffect(() => { void refresh(); }, [batchId, studentId]);
  async function addNote(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget);
    const response = await academyApi("/api/teacher/resources/note", { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ batchId, studentId: studentId || null, classSessionId: sessionId || null, title: form.get("title"), notes: form.get("notes"), type: form.get("type") }) });
    setMessage(response.ok ? "Class note saved." : "Class note could not be saved."); if (response.ok) { event.currentTarget.reset(); await refresh(); }
  }
  async function upload(file: File, title?: string, description?: string, type = "Class material") {
    const form = new FormData(); form.append("batchId", batchId); if (studentId) form.append("studentId", studentId); if (sessionId) form.append("classSessionId", sessionId); form.append("file", file); form.append("title", title || file.name); form.append("description", description || ""); form.append("type", type);
    const response = await academyApi("/api/teacher/resources/upload", { method: "POST", body: form });
    setMessage(response.ok ? "Material uploaded." : "Material could not be uploaded."); if (response.ok) await refresh();
  }
  async function addFile(event: React.FormEvent<HTMLFormElement>) { event.preventDefault(); const form = new FormData(event.currentTarget); if (pendingFile) { await upload(pendingFile, String(form.get("title") || pendingFile.name), String(form.get("description") || ""), String(form.get("type") || "Class material")); stageFile(null); event.currentTarget.reset(); } else setMessage("Choose a file or record audio before uploading."); }
  async function toggleRecording() {
    if (recording) { recorder.current?.stop(); return; }
    try { const stream = await navigator.mediaDevices.getUserMedia({ audio: true }); const current = new MediaRecorder(stream); chunks.current = []; current.ondataavailable = (event) => chunks.current.push(event.data); current.onstop = () => { stream.getTracks().forEach((track) => track.stop()); setRecording(false); stageFile(new File([new Blob(chunks.current, { type: current.mimeType || "audio/webm" })], `class-recording-${Date.now()}.webm`, { type: current.mimeType || "audio/webm" })); setMessage("Recording ready. Listen to it below, then upload when you are happy."); }; recorder.current = current; current.start(); setRecording(true); } catch { setMessage("Microphone access is required to record class audio."); }
  }
  async function setClassStatus(status: "InProgress" | "Completed") {
    if (!sessionId) return setMessage("Select a scheduled class first.");
    const response = await academyApi(`/api/teacher/sessions/${sessionId}/status`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ status }) });
    setMessage(response.ok ? `Class marked ${status === "Completed" ? "completed" : "in progress"}.` : "Class status could not be updated.");
  }
  async function startOnlineClass() {
    if (!selected?.meetingLink) return setMessage("A meeting link is needed before this class can start.");
    await setClassStatus("InProgress");
    window.open(selected.meetingLink, "_blank", "noopener,noreferrer");
  }
  async function assignHomework(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget);
    const response = await academyApi("/api/teacher/assignments", { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ batchId, studentId: studentId || null, title: form.get("title"), description: form.get("description"), dueAtUtc: form.get("dueAtUtc") ? new Date(String(form.get("dueAtUtc"))).toISOString() : null, type: "Homework", isPublished: true }) });
    setMessage(response.ok ? "Homework assigned." : "Homework could not be assigned.");
    if (response.ok) { event.currentTarget.reset(); await refresh(); }
  }
  return <>
    <section className="teacher-active-class-banner">
      <span className="teacher-live-dot" /> <div><b>Active class</b><small>{selected?.name ?? "Select a class to begin"} · {selected?.capacity === 1 ? "1:1 lesson" : `${roster.length || selected?.capacity || 0} students`}</small></div><em>{sessionStatus ?? "Ready"}</em>
    </section>
    <section className="teacher-classroom-hero">
      <div><p>Classroom</p><h2>{selected?.name ?? "Select a class"}</h2><span>{selected?.deliveryMode ?? ""}{selected?.roomName ? ` · ${selected.roomName}` : ""}</span></div>
      <div className="teacher-classroom-hero-actions">{isLiveDelivery && sessionId && <button type="button" className="enterprise-action-button" onClick={() => void startOnlineClass()}>{sessionStatus === "InProgress" ? "Rejoin online class" : "Start online class"}</button>}{sessionId && sessionStatus === "InProgress" && <button type="button" className="enterprise-action-button enterprise-action-button-secondary" onClick={() => void setClassStatus("Completed")}>Complete class</button>}</div>
    </section>
    <section className="teacher-classroom-controls"><label>Class<select value={batchId} onChange={(event) => { setBatchId(event.target.value); setStudentId(""); }}><option value="">Select class</option>{batches.map((batch) => <option key={batch.id} value={batch.id}>{batch.name}{batch.capacity === 1 ? " · 1:1" : ""}</option>)}</select></label><label>Teaching focus<select value={studentId} onChange={(event) => setStudentId(event.target.value)}><option value="">Whole class</option>{roster.map((student) => <option key={student.id} value={student.id}>{student.firstName} {student.lastName}</option>)}</select></label><aside><b>{selectedStudent ? `${selectedStudent.firstName} ${selectedStudent.lastName}` : "Class overview"}</b><span>History and homework are filtered to this selection.</span></aside></section>
    <section className="teacher-classroom-grid">
      <Panel title="Capture today’s learning"><form className="learner-form" onSubmit={(event) => void addNote(event)}><select name="type"><option>Class note</option><option>Student remark</option><option>Music repertoire progress</option><option>Syllabus / textbook reference</option></select><input required name="title" placeholder="Topic, repertoire, chapter, or page" /><textarea required name="notes" placeholder="What was taught, progress, feedback, and the next practice step" /><button>Save learning record</button></form><form className="learner-form teacher-upload-form" onSubmit={(event) => void addFile(event)}><select name="type"><option>Class material</option><option>Homework material</option><option>Reference recording</option><option>Syllabus / textbook</option></select><input name="title" placeholder="File title" /><input name="description" placeholder="Short description" /><label className="teacher-file-picker"><input name="file" type="file" accept="image/*,.pdf,audio/*,video/*,.doc,.docx" onChange={(event) => stageFile(event.target.files?.[0] ?? null)} /><span>{pendingFile ? pendingFile.name : "Choose photo, video, audio, PDF, or document"}</span></label>{pendingFile && <div className="teacher-file-preview"><b>Ready to review</b>{pendingFile.type.startsWith("audio/") && <audio controls src={pendingPreviewUrl} />}{pendingFile.type.startsWith("video/") && <video controls src={pendingPreviewUrl} />}{pendingFile.type.startsWith("image/") && <img src={pendingPreviewUrl} alt="Selected upload preview" />} {!/^(audio|video|image)\//.test(pendingFile.type) && <small>{pendingFile.name} · {(pendingFile.size / 1024 / 1024).toFixed(1)} MB</small>}<button type="button" className="teacher-clear-file" onClick={() => stageFile(null)}>Remove</button></div>}<div className="teacher-material-actions"><button disabled={!pendingFile}>Upload confirmed file</button><button type="button" className="enterprise-action-button enterprise-action-button-secondary teacher-mic-button" onClick={() => void toggleRecording()}>{recording ? "● Stop recording" : "🎙 Record class audio"}</button></div></form></Panel>
      <Panel title={selectedStudent ? `Homework for ${selectedStudent.firstName}` : "Homework for the class"}><form className="learner-form" onSubmit={(event) => void assignHomework(event)}><input required name="title" placeholder="Practice or homework title" /><textarea name="description" placeholder="Clear instructions, duration, or textbook page" /><input name="dueAtUtc" type="datetime-local" /><button>Assign homework</button></form><div className="teacher-activity-list">{activity.homework.length ? activity.homework.map((item) => <article key={item.id}><b>{item.title}</b><small>{item.studentId ? "Individual" : "Whole class"}{item.dueAtUtc ? ` · due ${dt(item.dueAtUtc)}` : " · no due date"}</small></article>) : <p className="learner-empty">No homework has been assigned for this view.</p>}</div></Panel>
    </section>
    <Panel title={selectedStudent ? `${selectedStudent.firstName}'s learning history` : "Class learning history"}><div className="teacher-activity-list">{activity.resources.length ? activity.resources.map((item) => <article key={item.id}><div><b>{item.title}</b><small>{item.type}{item.studentId ? " · individual record" : " · class record"} · {dt(item.createdAtUtc)}</small>{item.description && <p>{item.description}</p>}</div>{!item.url.startsWith("note://") && <a href={`${apiUrl}${item.url}`} target="_blank" rel="noreferrer">Open</a>}</article>) : <p className="learner-empty">No learning records yet. Save notes, recordings, or files above to build the class history.</p>}</div></Panel>
    {message && <p className="teacher-classroom-message" role="status">{message}</p>}
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
  const batch = (
    <select required name="batchId">
      <option value="">Select assigned batch…</option>
      {batches.map((x) => (
        <option key={x.id} value={x.id}>
          {x.name}
        </option>
      ))}
    </select>
  );
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
