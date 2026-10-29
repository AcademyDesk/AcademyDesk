"use client";
import Image from "next/image";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { TeacherMaterialUploader } from "@/components/teacher-material-uploader";
import { PrivateMaterialActions } from "@/components/private-material-actions";
import { useRouter } from "next/navigation";
import { academyApi, apiHeaders, apiUrl, clearPortalTokens } from "@/lib/api";
import { ThemeToggle } from "@/components/theme-toggle";
import { TeacherWorkspaceError, teacherWorkspaceFailure } from "@/lib/teacher-workspace";
import { PayrollPayslip, type Payout } from "../payroll/payslip";
import { payrollMoney, payrollTotals } from "../payroll/payroll-money";
import { ManualSectionNavigation } from "@/components/manual-section-navigation";
import manualStyles from "@/components/manual-workspace.module.css";
import { usePentaManualTheme } from "@/components/penta-manual-theme";
type P = {
  userId: string;
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
type Announcement = { id: string; title: string; message: string };
type PortalNotification = { id: string; title: string; message: string; channel: string; status: string; createdAtUtc: string };
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
type T = "today" | "classes" | "classroom" | "homework" | "batchProgress" | "progress" | "more";
const tabs: [T, string, string][] = [
  ["today", "Today", "⌂"],
  ["classes", "My classes", "◷"],
  ["classroom", "Classroom", "♙"],
  ["homework", "Homework", "✓"],
  ["batchProgress", "My progress", "◷"],
  ["progress", "Pay slip", "₹"],
  ["more", "Profile & leave", "•••"],
];
const statuses = ["Present", "Absent"];
const dt = (x: string) =>
  new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Asia/Kolkata",
  }).format(new Date(x));
const indiaDateKey = (value: string | Date) => {
  const parts = new Intl.DateTimeFormat("en-GB", { timeZone: "Asia/Kolkata", year: "numeric", month: "2-digit", day: "2-digit" }).formatToParts(new Date(value));
  const get = (type: string) => parts.find((part) => part.type === type)?.value ?? "";
  return `${get("year")}-${get("month")}-${get("day")}`;
};
export default function Teacher() {
  const manualTheme = usePentaManualTheme();
  const [p, setP] = useState<P>();
  const [sid, setSid] = useState("");
  const [roster, setRoster] = useState<S[]>([]);
  const [attendance, setAttendance] = useState<A[]>([]);
  const [t, setT] = useState<T>("today");
  const [m, setM] = useState("Loading your teaching workspace…");
  const [needsSignIn, setNeedsSignIn] = useState(false);
  const [busy, setBusy] = useState("");
  const [announcements, setAnnouncements] = useState<Announcement[]>([]);
  const [messages, setMessages] = useState<PortalNotification[]>([]);
  const rosterVersion = useRef({ version: 0 });
  const loadRoster = useCallback(async (id: string, signal?: AbortSignal) => {
    const version = ++rosterVersion.current.version;
    if (!id) return;
    const [a, b] = await Promise.all([
      academyApi(`/api/teacher/sessions/${id}/roster`, { signal }),
      academyApi(`/api/teacher/sessions/${id}/attendance`, { signal }),
    ]);
    if (!a.ok) throw Error();
    const [people, records]: [S[], A[]] = await Promise.all([a.json(), b.ok ? b.json() : []]);
    if (signal?.aborted || version !== rosterVersion.current.version) return;
    if (!Array.isArray(people) || !Array.isArray(records)) throw Error();
    setRoster(people);
    setAttendance(records);
  }, []);
  useEffect(() => {
    const controller = new AbortController();
    const epoch = rosterVersion.current;
    async function initialise() {
      const r = await academyApi("/api/teacher/me", { signal: controller.signal });
      if (!r.ok) throw new TeacherWorkspaceError(r.status);
      const x: P = await r.json();
      if (controller.signal.aborted) return;
      if (!x || !Array.isArray(x.sessions) || !Array.isArray(x.batches)) throw Error();
      setP(x);
      if (x.sessions[0]) {
        setSid(x.sessions[0].id);
        await loadRoster(x.sessions[0].id, controller.signal);
      }
      if (!controller.signal.aborted) setM("");
    }
    void initialise().catch((error: unknown) => {
      if (controller.signal.aborted) return;
      const failure = teacherWorkspaceFailure(error);
      setM(failure.message);
      setNeedsSignIn(failure.signIn);
    });
    void academyApi("/api/portal/announcements", { signal: controller.signal }).then(async (response) => { if (response.ok) { const rows = await response.json(); if (!controller.signal.aborted && Array.isArray(rows)) setAnnouncements(rows); } }).catch(() => undefined);
    void academyApi("/api/portal/notifications", { signal: controller.signal }).then(async (response) => { if (response.ok) { const rows = await response.json(); if (!controller.signal.aborted && Array.isArray(rows)) setMessages(rows); } }).catch(() => undefined);
    return () => { controller.abort(); epoch.version++; };
  }, [loadRoster]);
  async function submitAttendance(teacherStatus: string, records: { studentId: string; status: string }[]) {
    if (!sid) return;
    setBusy("attendance");
    const [studentsResponse, teacherResponse] = await Promise.all([
      academyApi(`/api/teacher/sessions/${sid}/attendance/bulk`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ records: records.map((record) => ({ ...record, notes: null })) }) }),
      academyApi(`/api/teacher/sessions/${sid}/teacher-attendance`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ status: teacherStatus }) }),
    ]);
    if (studentsResponse.ok && teacherResponse.ok) {
      setP((current) => current ? { ...current, sessions: current.sessions.map((session) => session.id === sid ? { ...session, teacherAttendanceStatus: teacherStatus } : session) } : current);
      await loadRoster(sid);
    } else setM("Attendance could not be submitted. Please try again.");
    setBusy("");
  }
  async function selectClassroomSession(id: string) {
    rosterVersion.current.version++;
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
    <main {...manualTheme} className={`enterprise-app-shell teacher-portal-shell ${manualStyles.shell} ${manualStyles.teacher} ${manualTheme.className}`}>
      <aside className="enterprise-sidebar teacher-portal-sidebar">
        <a href="/teacher" className="enterprise-brand">
          <span>A</span>
          <strong>AcademyDesk</strong>
        </a>
        <nav className="enterprise-nav-section teacher-portal-nav" aria-label="Teacher workspace">
          <p>Teacher portal</p>
          {tabs.map(([k, x, i]) => (
            <button key={k} data-active={t === k} aria-current={t === k ? "page" : undefined} onClick={() => { if (k === "classroom") { rosterVersion.current.version++; setSid(""); setRoster([]); setAttendance([]); } setT(k); }}>
              <i aria-hidden="true">{i}</i>
              {x}
            </button>
          ))}
        </nav>
      </aside>
      <section className="enterprise-workspace teacher-portal-workspace">
        <header className="enterprise-topbar">
          <div className="teacher-portal-context"><span>Teacher workspace</span><strong>{p ? `${p.firstName} ${p.lastName}` : "Teacher workspace"}</strong></div>
          <div className="enterprise-utilities"><ThemeToggle /><TeacherPortalProfile /></div>
        </header>
        {announcements.length > 0 && <AnnouncementTicker announcements={announcements} />}
        <ManualSectionNavigation className="learner-bottom-nav" label="Teacher section navigation" sections={tabs.map(([value, label, icon]) => ({ value, label, icon }))} value={t} onSelect={value => setT(value as T)} />
        <section className="learner-content teacher-portal-content">
          {m ? (
            <p className="learner-state" role="status" aria-live="polite">
              {m}
              {needsSignIn && <><br /><a href="/login" className="enterprise-action-button enterprise-action-button-secondary">Sign in</a></>}
            </p>
          ) : (
            p && (
              <>
                {t === "classroom" && p.sessions.find((session) => session.id === sid)?.status === "InProgress" && <TeacherActiveClassBanner batch={p.batches.find((batch) => batch.id === p.sessions.find((session) => session.id === sid)?.batchId)} rosterCount={roster.length} status="In progress" />}
                {t === "classroom" && sid && <TeacherClassroomHero batch={p.batches.find((batch) => batch.id === p.sessions.find((session) => session.id === sid)?.batchId)} sessionId={sid} sessionStatus={p.sessions.find((session) => session.id === sid)?.status} onStatusChange={(status) => updateSessionStatus(sid, status)} />}
                {t === "classroom" && sid && <TeacherAttendanceRoster sessionId={sid} roster={roster} attendance={attendance} teacherStatus={p.sessions.find((session) => session.id === sid)?.teacherAttendanceStatus} busy={busy === "attendance"} onSubmit={submitAttendance} />}
                {t === "classroom" && <TeacherClassroom ownerId={p.userId} batches={p.batches} sessions={p.sessions} sessionId={sid} roster={roster} onSessionSelect={(id) => void selectClassroomSession(id)} />}
                {t === "today" && <header className="learner-heading">
                  <p>Teacher workspace</p>
                  <h1>{`Welcome, ${p.firstName}`}</h1>
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
                {t === "today" && <TeacherOverviewFinance />}
                {t === "today" && <TeacherMessages messages={messages} />}
                {t === "today" && (
                  <section className="teacher-timetable-panel">
                    <header className="teacher-section-heading">
                      <div><i aria-hidden="true">◷</i><h2>My timetable</h2></div>
                    </header>
                    <div className="teacher-timetable-list">
                    {p.sessions.filter((session) => indiaDateKey(session.startUtc) === indiaDateKey(new Date())).map((x) => (
                      <button
                        className="teacher-timetable-row"
                        key={x.id}
                        onClick={() => {
                          void selectClassroomSession(x.id);
                          setT("classroom");
                        }}
                      >
                        <time dateTime={x.startUtc}><b>{new Intl.DateTimeFormat("en-IN", { day: "2-digit" }).format(new Date(x.startUtc))}</b><span>{new Intl.DateTimeFormat("en-IN", { month: "short" }).format(new Date(x.startUtc))}</span><strong>{new Intl.DateTimeFormat("en-IN", { weekday: "short" }).format(new Date(x.startUtc))}</strong></time>
                        <div><b>{name(x.batchId)}</b><small>{dt(x.startUtc) + " · " + x.deliveryMode + (x.roomName ? " · " + x.roomName : "")}</small></div>
                      </button>
                    ))}
                    {!p.sessions.some((session) => indiaDateKey(session.startUtc) === indiaDateKey(new Date())) && <p className="teacher-timetable-empty">No classes scheduled for today.</p>}
                    </div>
                  </section>
                )}
                {t === "classes" && <TeacherCalendar batches={p.batches} compact onOpen={(id) => { void selectClassroomSession(id); setT("classroom"); }} />}
                {t === "homework" && <TeacherTasks batches={p.batches} />}{" "}
                {t === "batchProgress" && <section className="teacher-my-progress-view"><TeacherProgress includeMetrics /><TeacherBatchProgress /></section>}
                {t === "progress" && <TeacherProgress />}
                {t === "more" && <TeacherSelfService />}
              </>
            )
          )}
        </section>
      </section>
    </main>
  );
}
function AnnouncementTicker({ announcements }: { announcements: Announcement[] }) { const text = announcements.map((item) => `${item.title}: ${item.message}`).join("   •   "); return <div className="learner-announcement" role="status" aria-label="Important announcement"><span>Important</span><div><p>{text}   •   {text}</p></div></div>; }
function TeacherMessages({ messages }: { messages: PortalNotification[] }) { return <section className="teacher-home-messages"><header><div><p>Messages</p><h2>From your academy</h2></div><span>{messages.length}</span></header>{messages.length ? <ul>{messages.slice(0, 4).map(message => <li key={message.id}><div><b>{message.title}</b><p>{message.message}</p><small>{new Date(message.createdAtUtc).toLocaleString("en-IN")}</small></div><span>{message.channel}</span></li>)}</ul> : <p>No direct messages from your academy.</p>}</section>; }

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
  function signOut() { clearPortalTokens(); router.push("/login"); }
  return <div className="enterprise-profile" ref={ref}>
    <button type="button" className="enterprise-profile-trigger" onClick={() => { setMessage(""); setOpen((value) => !value); }} aria-label="Open profile menu" aria-expanded={open}>
      {imageUrl ? <Image unoptimized loading="eager" width={44} height={44} src={imageUrl} alt="Profile" /> : <span>{initials}</span>}
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
function TeacherActionPanel({ title, icon, children }: { title: string; icon: string; children: React.ReactNode }) {
  return <section className="teacher-action-panel"><header><i aria-hidden="true">{icon}</i><h3>{title}</h3></header><div className="learner-list">{children}</div></section>;
}
type TeacherCalendarSession = P["sessions"][number];
type TeacherCalendarHoliday = { id: string; name: string; holidayDate: string; isClosed: boolean };
function TeacherCalendar({ batches, onOpen, compact = false }: { batches: P["batches"]; onOpen: (id: string) => void; compact?: boolean }) {
  const [month, setMonth] = useState(() => new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  const [data, setData] = useState<{ sessions: TeacherCalendarSession[]; holidays: TeacherCalendarHoliday[] }>({ sessions: [], holidays: [] });
  const [selectedDay, setSelectedDay] = useState<string>(indiaDateKey(new Date()));
  useEffect(() => {
    void academyApi(`/api/teacher/calendar?year=${month.getFullYear()}&month=${month.getMonth() + 1}`).then(async (response) => { if (response.ok) setData(await response.json()); });
  }, [month]);
  const sessionByDay = new Map<string, TeacherCalendarSession[]>();
  data.sessions.forEach((session) => { const key = indiaDateKey(session.startUtc); sessionByDay.set(key, [...(sessionByDay.get(key) ?? []), session]); });
  const holidayByDay = new Map(data.holidays.map((holiday) => [holiday.holidayDate, holiday]));
  const firstOffset = (month.getDay() + 6) % 7;
  const daysInMonth = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
  const cells = Array.from({ length: Math.ceil((firstOffset + daysInMonth) / 7) * 7 }, (_, index) => index - firstOffset + 1);
  const selectedSessions = sessionByDay.get(selectedDay) ?? [];
  const selectedHoliday = holidayByDay.get(selectedDay);
  const batchName = (id: string) => batches.find((batch) => batch.id === id)?.name ?? "Assigned class";
  return <section className="teacher-calendar-panel" data-compact={compact}>
    <header className="teacher-calendar-header"><div><span>Class calendar</span><h2>{new Intl.DateTimeFormat("en-IN", { month: "long", year: "numeric" }).format(month)}</h2></div><div><button type="button" aria-label="Previous month" onClick={() => setMonth((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))}>‹</button><button type="button" onClick={() => { const today = new Date(); setMonth(new Date(today.getFullYear(), today.getMonth(), 1)); setSelectedDay(indiaDateKey(today)); }}>Today</button><button type="button" aria-label="Next month" onClick={() => setMonth((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1))}>›</button></div></header>
    <div className="teacher-calendar-weekdays">{["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((day) => <span key={day}>{day}</span>)}</div>
    <div className="teacher-calendar-grid">{cells.map((day, index) => {
      if (day < 1 || day > daysInMonth) return <div className="teacher-calendar-blank" key={`blank-${index}`} />;
      const date = new Date(month.getFullYear(), month.getMonth(), day); const key = indiaDateKey(date); const sessions = sessionByDay.get(key) ?? []; const holiday = holidayByDay.get(key);
      return <button type="button" key={key} className="teacher-calendar-day" data-selected={selectedDay === key} data-holiday={Boolean(holiday)} onClick={() => setSelectedDay(key)}><time>{day}</time>{holiday && <small>{holiday.name}</small>}{sessions.map((session) => <span key={session.id} onClick={(event) => { event.stopPropagation(); onOpen(session.id); }}>{batchName(session.batchId)}</span>)}</button>;
    })}</div>
    <section className="teacher-calendar-detail"><b>{new Intl.DateTimeFormat("en-IN", { weekday: "long", day: "numeric", month: "long" }).format(new Date(`${selectedDay}T00:00:00`))}</b>{selectedHoliday && <em>{selectedHoliday.name}</em>}{selectedSessions.length ? selectedSessions.map((session) => <button type="button" key={session.id} onClick={() => onOpen(session.id)}><span>{batchName(session.batchId)}</span><small>{dt(session.startUtc)} · {session.deliveryMode}</small></button>) : !selectedHoliday && <small>No assigned classes.</small>}</section>
  </section>;
}
type TeacherBatch = P["batches"][number];
type Resource = { id: string; batchId: string; studentId?: string; classSessionId?: string; title: string; description?: string; type: string; url: string; createdAtUtc: string };

type AttendanceRosterProps = { sessionId: string; roster: S[]; attendance: A[]; teacherStatus?: string | null; busy: boolean; onSubmit: (teacherStatus: string, records: { studentId: string; status: string }[]) => Promise<void> };
function TeacherAttendanceRoster(props: AttendanceRosterProps) {
  // A changed session or authoritative attendance snapshot owns a fresh editor.
  // Keying avoids committing the previous class's draft for one effect cycle.
  return <TeacherAttendanceDraft key={JSON.stringify([props.sessionId, props.roster.map(student => student.id), props.attendance, props.teacherStatus])} {...props} />;
}
function TeacherAttendanceDraft({ sessionId, roster, attendance, teacherStatus, busy, onSubmit }: AttendanceRosterProps) {
  const [draft, setDraft] = useState<Record<string, string>>(() => Object.fromEntries(roster.map(student => [student.id, attendance.find(item => item.studentId === student.id)?.status ?? ""])));
  const [teacherDraft, setTeacherDraft] = useState(teacherStatus ?? "");
  const recorded = attendance.length;
  const attendanceReady = Boolean(teacherDraft) && roster.every((student) => Boolean(draft[student.id]));
  return <section className="teacher-attendance-panel">
    <header><div><p>Attendance</p></div><strong>{recorded ? `${recorded} / ${roster.length}` : `${roster.length} students`}</strong></header>
    <div className="teacher-attendance-teacher"><div><b>Teacher attendance</b><small>Record your attendance for this class.</small></div><AttendanceStatusMenu value={teacherDraft} disabled={busy} label="Teacher attendance" onChange={setTeacherDraft} /></div>
    <div className="teacher-attendance-grid">{roster.map((student) => {
      const value = draft[student.id] ?? "";
      const initials = `${student.firstName[0] ?? ""}${student.lastName[0] ?? ""}`.toUpperCase();
      return <article key={student.id}><span className="teacher-student-avatar">{initials}</span><div><b>{student.firstName} {student.lastName}</b><small data-status={value}>{value}</small></div><AttendanceStatusMenu value={value} disabled={busy} label={`Attendance for ${student.firstName} ${student.lastName}`} onChange={(status) => setDraft((current) => ({ ...current, [student.id]: status }))} /></article>;
    })}</div>
    <div className="teacher-attendance-submit"><button type="button" className="enterprise-action-button" disabled={busy || !sessionId || !attendanceReady} onClick={() => void onSubmit(teacherDraft, roster.map((student) => ({ studentId: student.id, status: draft[student.id] ?? "" })))}>{busy ? "Submitting attendance…" : "Submit attendance"}</button></div>
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
  return <div className="teacher-attendance-menu" ref={ref}><button type="button" aria-label={label} aria-haspopup="listbox" aria-expanded={open} disabled={disabled} onClick={() => setOpen((current) => !current)}><span data-status={value}>{value || "Select status"}</span><i>⌄</i></button>{open && <div role="listbox" aria-label={label}>{statuses.map((status) => <button type="button" role="option" aria-selected={value === status} key={status} data-active={value === status} onClick={() => { onChange(status); setOpen(false); }}>{status}</button>)}</div>}</div>;
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
function validClassroomActivity(value: unknown): value is ClassroomActivity {
  if (!value || typeof value !== "object" || Array.isArray(value)) return false;
  const data = value as ClassroomActivity;
  return Array.isArray(data.resources) && Array.isArray(data.homework)
    && data.resources.every(item => item && typeof item.id === "string" && typeof item.title === "string" && typeof item.url === "string" && typeof item.createdAtUtc === "string" && Number.isFinite(Date.parse(item.createdAtUtc)))
    && data.homework.every(item => item && typeof item.id === "string" && typeof item.title === "string" && (!item.dueAtUtc || Number.isFinite(Date.parse(item.dueAtUtc))));
}
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
    <div className="teacher-classroom-hero-actions">{sessionId && isLiveDelivery && <button type="button" className="enterprise-action-button" onClick={() => void startOnlineClass()}>{sessionStatus === "Scheduled" ? "Start online class" : "Open class link"}</button>}{sessionId && !isLiveDelivery && sessionStatus !== "InProgress" && sessionStatus !== "Completed" && <button type="button" className="enterprise-action-button" onClick={() => void updateStatus("InProgress")}>Start class</button>}{sessionId && sessionStatus === "InProgress" && <button type="button" className="enterprise-action-button enterprise-action-button-secondary" onClick={() => void updateStatus("Completed")}>Complete class</button>}</div>
  </section>;
}
function TeacherClassroom({ ownerId, batches, sessions, sessionId, roster, onSessionSelect }: { ownerId: string; batches: TeacherBatch[]; sessions: P["sessions"]; sessionId: string; roster: S[]; onSessionSelect: (id: string) => void }) {
  const [batchChoice, setBatchChoice] = useState({ sessionId: "", value: "" });
  const [studentChoice, setStudentId] = useState("");
  const batchId = sessions.find(session => session.id === sessionId)?.batchId ?? (batchChoice.sessionId === sessionId ? batchChoice.value : "");
  const selected = batches.find(batch => batch.id === batchId);
  const studentId = selected?.capacity === 1 ? roster[0]?.id ?? "" : roster.some(student => student.id === studentChoice) ? studentChoice : "";
  const [activityResult, setActivityResult] = useState<{ scope: string; data: ClassroomActivity }>();
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [message, setMessage] = useState("");
  const activityScope = JSON.stringify([batchId, studentId, fromDate, toDate]);
  const activity: ClassroomActivity = activityResult?.scope === activityScope ? activityResult.data : { resources: [], homework: [] };
  const setActivity = useCallback((data: ClassroomActivity) => { setActivityResult({ scope: activityScope, data }); }, [activityScope]);
  const selectedStudent = roster.find((student) => student.id === studentId);
  async function refresh(id = batchId) {
    if (!id) return;
    const history = await academyApi(`/api/teacher/classroom-activity?batchId=${id}${studentId ? `&studentId=${studentId}` : ""}${fromDate ? `&fromUtc=${encodeURIComponent(fromDate)}` : ""}${toDate ? `&toUtc=${encodeURIComponent(toDate)}` : ""}`);
    if (!history.ok) throw Error("Class history could not be refreshed.");
    const data: unknown = await history.json();
    if (!validClassroomActivity(data)) throw Error("Class history could not be refreshed.");
    setActivity(data);
  }
  useEffect(() => {
    if (!batchId) return;
    let active = true;
    void academyApi(`/api/teacher/classroom-activity?batchId=${batchId}${studentId ? `&studentId=${studentId}` : ""}${fromDate ? `&fromUtc=${encodeURIComponent(fromDate)}` : ""}${toDate ? `&toUtc=${encodeURIComponent(toDate)}` : ""}`)
      .then(async response => { if (!response.ok) throw Error(); const history: unknown = await response.json(); if (!validClassroomActivity(history)) throw Error(); if (active) setActivity(history); })
      .catch(() => { if (active) setMessage("Class history could not be refreshed. Try again."); });
    return () => { active = false; };
  }, [batchId, studentId, fromDate, toDate, setActivity]);
  async function addNote(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const formElement = event.currentTarget; const form = new FormData(formElement);
    const response = await academyApi("/api/teacher/resources/note", { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ batchId, studentId: studentId || null, classSessionId: sessionId || null, title: form.get("title"), notes: form.get("notes"), type: "Note" }) });
    setMessage(response.ok ? "Class note saved." : "Class note could not be saved."); if (response.ok) { formElement.reset(); await refresh(); }
  }
  const uploadScope = useMemo(() => ({ ownerId, batchId, studentId: studentId || null, classSessionId: sessionId || null }), [ownerId, batchId, studentId, sessionId]);
  return <>
    <section className="teacher-classroom-controls" data-selected={Boolean(selected)}>
      <label>Class<TeacherDropdown label="Class" value={batchId} onChange={(value) => { setStudentId(""); const nextSession = sessions.find((session) => session.batchId === value && session.status !== "Completed") ?? sessions.find((session) => session.batchId === value); setBatchChoice({ sessionId: nextSession?.id ?? "", value }); onSessionSelect(nextSession?.id ?? ""); }} options={[{ value: "", label: "Select class" }, ...batches.map((batch) => ({ value: batch.id, label: `${batch.name}${batch.capacity === 1 ? " · 1:1" : ""}` }))]} /></label>
      {selected && (selected.capacity === 1 ? <label>Teaching focus<div className="teacher-single-focus">{selectedStudent ? `${selectedStudent.firstName} ${selectedStudent.lastName}` : "Loading student…"}</div></label> : <label>Teaching focus<TeacherDropdown label="Teaching focus" value={studentId} onChange={setStudentId} options={[{ value: "", label: "Whole class" }, ...roster.map((student) => ({ value: student.id, label: `${student.firstName} ${student.lastName}` }))]} /></label>)}
    </section>
    {selected && <>
    <section className="teacher-classroom-grid">
      <TeacherActionPanel icon="✎" title={selectedStudent ? `Notes for ${selectedStudent.firstName}` : "Notes"}><form className="learner-form teacher-note-form" onSubmit={(event) => void addNote(event)}><input required name="title" placeholder="Note title" /><textarea required name="notes" placeholder="Add a comment" /><button>Save note</button></form></TeacherActionPanel>
      <TeacherActionPanel icon="⌁" title={selectedStudent ? `Attachments & recordings for ${selectedStudent.firstName}` : "Attachments & recordings"}><TeacherMaterialUploader key={`${ownerId}:${batchId}:${studentId}:${sessionId}`} scope={uploadScope} onUploaded={() => refresh()} /></TeacherActionPanel>
    </section>
    <TeacherActionPanel icon="◴" title={selectedStudent ? `${selectedStudent.firstName}'s history` : "Class history"}><div className="teacher-history-filter"><label>From<input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} /></label><label>To<input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} /></label><button type="button" onClick={() => { setFromDate(""); setToDate(""); }}>Clear</button></div><div className="teacher-activity-list">{activity.resources.length || activity.homework.length ? <>{activity.resources.map((item) => <article key={item.id}><div><b>{item.title}</b><small>{item.type}{item.studentId ? " · individual" : " · whole class"} · {dt(item.createdAtUtc)}</small>{item.description && <p>{item.description}</p>}</div>{!item.url.startsWith("note://") && <PrivateMaterialActions url={item.url} title={item.title} />}</article>)}{activity.homework.map((item) => <article key={item.id}><div><b>{item.title}</b><small>Homework{item.studentId ? " · individual" : " · whole class"}{item.dueAtUtc ? ` · due ${dt(item.dueAtUtc)}` : ""}</small>{item.description && <p>{item.description}</p>}</div></article>)}</> : <p className="learner-empty">No class history for this period.</p>}</div></TeacherActionPanel>
    {message && <p className="teacher-classroom-message" role="status">{message}</p>}
    </>}
  </>;
}

function TeacherProgress({ includeMetrics = false }: { includeMetrics?: boolean }) {
  const [progress, setProgress] = useState<{ completedClasses: number; upcomingClasses: number; attendanceRecords: number; presentOrOnline: number }>();
  const [payments, setPayments] = useState<TeacherPaymentData>();
  const [viewing, setViewing] = useState<Payout | null>(null);
  const [payslipMonth, setPayslipMonth] = useState("");
  const [notice, setNotice] = useState("");
  useEffect(() => { if (includeMetrics) void academyApi("/api/teacher/progress").then(async response => { if (response.ok) setProgress(await response.json()); }); }, [includeMetrics]);
  useEffect(() => {
    if (includeMetrics) return;
    let active = true;
    const [year, month] = payslipMonth.split("-"), query = year && month ? `?year=${year}&month=${Number(month)}` : "";
    void (async () => {
      try {
        const response = await academyApi(`/api/teacher/payments${query}`, { cache: "no-store" });
        if (!response.ok) throw new Error();
        const data: TeacherPaymentData = await response.json();
        if (!Array.isArray(data.payslips)) throw new Error();
        if (active) { setPayments(data); setNotice(""); }
      } catch { if (active) { setPayments(undefined); setNotice("Payslips could not be loaded. Please try again later."); } }
    })();
    return () => { active = false; };
  }, [includeMetrics, payslipMonth]);
  if (includeMetrics) return <section className="learner-kpis"><K a="Classes completed" b={String(progress?.completedClasses ?? "—")} c="Delivered sessions" /><K a="Upcoming classes" b={String(progress?.upcomingClasses ?? "—")} c="Scheduled ahead" /><K a="Attendance marked" b={String(progress?.attendanceRecords ?? "—")} c="Student records" /><K a="Present" b={String(progress?.presentOrOnline ?? "—")} c="Attendance outcomes" /></section>;
  return <><TeacherActionPanel icon="₹" title="Payment and payslips">
    <section className="teacher-payment-model"><b>Payment model: {payments?.paymentModel ?? "Not configured"}</b></section>
    <p>Only records with verified payout-time teacher ownership are shown. Unverified historical identity requires administrator review.</p>
    {notice && <p role="status">{notice}</p>}
    <div className="teacher-payslip-filter"><label>Month<input type="month" value={payslipMonth} onChange={event => { setViewing(null); setPayslipMonth(event.target.value); }} /></label>{payslipMonth && <button type="button" onClick={() => { setViewing(null); setPayslipMonth(""); }}>All months</button>}</div>
    <section className="teacher-payslip-list">{payments?.payslips?.length ? payments.payslips.map(item => <article key={item.id}><div><b>{item.periodLabel}</b><small>{item.payslipNumber} · {payrollMoney(item.netAmount, item.netAmountExact, item.currency)}</small></div><div><em data-paid={item.status === "Paid"}>{item.status === "NoCashDue" ? "No cash due" : item.status}</em><button type="button" onClick={() => setViewing(item)}>Preview / Print</button></div></article>) : <p className="learner-empty">No verified payslips or earnings statements for this view.</p>}</section>
  </TeacherActionPanel>{viewing && <PayrollPayslip payout={viewing} onClose={() => setViewing(null)} />}</>;
}

type TeacherPaymentData = { paymentModel?: string; monthlyAmount?: number; amountPerCycle?: number; sessionsPerCycle?: number; payslips: Payout[] };
function TeacherOverviewFinance() {
  const [payments, setPayments] = useState<TeacherPaymentData>();
  useEffect(() => { void academyApi("/api/teacher/payments").then(async (response) => { if (response.ok) setPayments(await response.json()); }); }, []);
  const payouts = payments?.payslips ?? [];
  const paid = payouts.filter((item) => item.status === "Paid");
  const pending = payouts.filter((item) => item.status !== "Paid" && item.status !== "NoCashDue");
  const next = pending[0];
  const expected = next?.netAmount ?? payments?.monthlyAmount ?? payments?.amountPerCycle ?? 0;
  return <TeacherActionPanel icon="₹" title="Salary & payments"><section className="teacher-finance-summary"><article><span>Earnings so far</span><b>{payrollTotals(paid)}</b><small>Paid salary and session payouts</small></article><article><span>Upcoming payment</span><b>₹{expected.toLocaleString("en-IN")}</b><small>{next ? next.periodLabel : payments?.paymentModel ?? "Not configured"}</small></article><article data-pending={pending.length > 0}><span>Awaiting approval</span><b>{payrollTotals(pending)}</b><small>{pending.length ? `${pending.length} payment awaiting approval` : "No payments awaiting approval"}</small></article></section><section className="teacher-recent-payments"><header><span>Recent payments</span></header>{paid.length ? paid.slice(0, 3).map((item) => <article key={item.id}><div><b>{item.periodLabel}</b><small>{item.payslipNumber} · {item.paidAtUtc ? dt(item.paidAtUtc) : "No cash payment date"}</small></div><strong>{payrollMoney(item.netAmount, item.netAmountExact, item.currency)}</strong></article>) : <p className="learner-empty">No payments have been made yet.</p>}</section></TeacherActionPanel>;
}

type TeacherBatchProgressData = { batchId: string; batchName: string; sessionMinutes: number; sessionsPerWeek: number; paymentCycle: string; cycleTotal: number; completedInCycle: number; remainingInCycle: number; paymentReady: boolean; coveredClassDates: string[]; upcomingClasses: { startUtc: string; endUtc: string }[] };
function TeacherBatchProgress() {
  const [batches, setBatches] = useState<TeacherBatchProgressData[]>([]);
  useEffect(() => { void academyApi("/api/teacher/batch-progress").then(async (response) => { if (response.ok) setBatches(await response.json()); }); }, []);
  const date = (value: string) => new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric", timeZone: "Asia/Kolkata" }).format(new Date(value));
  return <section className="teacher-batch-progress"><header><i aria-hidden="true">◷</i><div><h3>My progress</h3><span>Class delivery and payment-cycle progress by batch</span></div></header><div className="teacher-batch-progress-list">{batches.length ? batches.map((batch) => <article key={batch.batchId}><header><div><b>{batch.batchName}</b><small>{batch.sessionsPerWeek} class{batch.sessionsPerWeek === 1 ? "" : "es"} / week · {batch.sessionMinutes} min each</small></div><em data-ready={batch.paymentReady}>{batch.paymentReady ? "Ready for payment" : batch.paymentCycle}</em></header><div className="teacher-batch-cycle"><div><span>Cycle progress</span><b>{batch.completedInCycle} / {batch.cycleTotal} classes</b></div><div className="teacher-batch-progress-track"><i style={{ width: `${Math.min(100, (batch.completedInCycle / Math.max(1, batch.cycleTotal)) * 100)}%` }} /></div><small>{batch.paymentReady ? "Payment threshold reached." : `${batch.remainingInCycle} class${batch.remainingInCycle === 1 ? "" : "es"} remaining in this cycle.`}</small></div><div className="teacher-batch-date-columns"><section><span>Classes covered</span>{batch.coveredClassDates.length ? <div>{batch.coveredClassDates.map((value) => <time key={value}>{date(value)}</time>)}</div> : <small>No completed classes yet.</small>}</section><section><span>Upcoming classes</span>{batch.upcomingClasses.length ? <div>{batch.upcomingClasses.map((item) => <time key={item.startUtc}>{date(item.startUtc)} · {new Intl.DateTimeFormat("en-IN", { hour: "numeric", minute: "2-digit", timeZone: "Asia/Kolkata" }).format(new Date(item.startUtc))}</time>)}</div> : <small>No future classes scheduled.</small>}</section></div></article>) : <p className="learner-empty">No assigned batches yet.</p>}</div></section>;
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
      .catch(() => undefined);
  }, []);
  async function create(e: React.FormEvent<HTMLFormElement>, path: string) {
    e.preventDefault();
    const form = e.currentTarget;
    const f = new FormData(form);
    const body = Object.fromEntries(f) as Record<string, unknown>;
    if (!body.batchId) {
      setM("Select an assigned batch before saving.");
      return;
    }
    if ("maxScore" in body) body.maxScore = Number(body.maxScore);
    if ("isPublished" in body) body.isPublished = body.isPublished === "true";
    if ("scheduledAtUtc" in body)
      body.scheduledAtUtc = body.scheduledAtUtc
        ? new Date(String(body.scheduledAtUtc)).toISOString()
        : null;
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
    if (r.ok) form.reset();
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
    <section className="teacher-compact-panels teacher-homework-panels">
      <TeacherActionPanel icon="✓" title="Assign homework">
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
      </TeacherActionPanel>
      <TeacherActionPanel icon="◫" title="Create lesson plan">
        <form
          className="learner-form"
          onSubmit={(e) => void create(e, "/api/teacher/lesson-plans")}
        >
          {batch}
          <input required name="title" placeholder="Lesson title" />
          <textarea name="objectives" placeholder="Learning objectives" />
          <button>Save lesson plan</button>
        </form>
      </TeacherActionPanel>
      <TeacherActionPanel icon="★" title="Create assessment">
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
      </TeacherActionPanel>
      <TeacherActionPanel icon="✦" title="Practice feedback">
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
      </TeacherActionPanel>
    </section>
  );
}

function TeacherSelfService() {
  const [message, setMessage] = useState("");
  const [profile, setProfile] = useState<Record<string, string>>({});
  const [leaveRequests, setLeaveRequests] = useState<{ id: string; startDate: string; endDate: string; reason: string; status: string; decisionNotes?: string }[]>([]);
  const imageInput = useRef<HTMLInputElement>(null);
  useEffect(() => { void academyApi("/api/teacher/profile").then(async (response) => { if (response.ok) setProfile(await response.json()); }); void academyApi("/api/teacher/leave-requests").then(async (response) => { if (response.ok) setLeaveRequests(await response.json()); }); }, []);
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
    if (response.ok) {
      if (path === "/api/teacher/leave-requests") { const created = await response.json(); setLeaveRequests((current) => [created, ...current]); event.currentTarget.reset(); }
      if (path === "/api/teacher/profile") setProfile(await response.json());
    }
  }
  async function uploadPicture(event: React.ChangeEvent<HTMLInputElement>) { const image = event.target.files?.[0]; event.target.value = ""; if (!image) return; const body = new FormData(); body.append("image", image); const response = await academyApi("/api/auth/session/profile-image", { method: "POST", body }); setMessage(response.ok ? "Profile picture updated." : "Profile picture could not be updated."); }
  return (
    <section className="teacher-compact-panels teacher-profile-panels">
      <TeacherActionPanel icon="◉" title="My profile">
        <form
          className="learner-form"
          onSubmit={(event) => void save(event, "/api/teacher/profile")}
        >
          <div className="teacher-profile-photo"><input ref={imageInput} className="sr-only" type="file" accept="image/png,image/jpeg,image/webp" onChange={(event) => void uploadPicture(event)} /><button type="button" onClick={() => imageInput.current?.click()}>Update profile picture</button></div>
          <input required name="firstName" value={profile.firstName ?? ""} onChange={(event) => setProfile((current) => ({ ...current, firstName: event.target.value }))} placeholder="First name" />
          <input required name="lastName" value={profile.lastName ?? ""} onChange={(event) => setProfile((current) => ({ ...current, lastName: event.target.value }))} placeholder="Last name" />
          <input name="preferredName" value={profile.preferredName ?? ""} onChange={(event) => setProfile((current) => ({ ...current, preferredName: event.target.value }))} placeholder="Preferred name" />
          <input name="email" type="email" value={profile.email ?? ""} onChange={(event) => setProfile((current) => ({ ...current, email: event.target.value }))} placeholder="Email address" />
          <input name="phone" value={profile.phone ?? ""} onChange={(event) => setProfile((current) => ({ ...current, phone: event.target.value }))} placeholder="Phone number" />
          <input name="addressLine1" value={profile.addressLine1 ?? ""} onChange={(event) => setProfile((current) => ({ ...current, addressLine1: event.target.value }))} placeholder="Address" />
          <input name="city" value={profile.city ?? ""} onChange={(event) => setProfile((current) => ({ ...current, city: event.target.value }))} placeholder="City" />
          <input name="state" value={profile.state ?? ""} onChange={(event) => setProfile((current) => ({ ...current, state: event.target.value }))} placeholder="State" />
          <input name="postalCode" value={profile.postalCode ?? ""} onChange={(event) => setProfile((current) => ({ ...current, postalCode: event.target.value }))} placeholder="Postal / PIN code" />
          <input name="emergencyContactName" value={profile.emergencyContactName ?? ""} onChange={(event) => setProfile((current) => ({ ...current, emergencyContactName: event.target.value }))} placeholder="Emergency contact name" />
          <input name="emergencyContactPhone" value={profile.emergencyContactPhone ?? ""} onChange={(event) => setProfile((current) => ({ ...current, emergencyContactPhone: event.target.value }))} placeholder="Emergency contact phone" />
          <button>Save profile</button>
        </form>
      </TeacherActionPanel>
      <TeacherActionPanel icon="◷" title="Request leave">
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
        <section className="teacher-leave-list">{leaveRequests.length ? leaveRequests.map((request) => <article key={request.id}><div><b>{request.startDate} to {request.endDate}</b><small>{request.reason}</small>{request.decisionNotes && <small>{request.decisionNotes}</small>}</div><em data-status={request.status}>{request.status === "Pending" ? "Pending approval" : request.status}</em></article>) : <p className="learner-empty">No leave requests yet.</p>}</section>
      </TeacherActionPanel>
      <TeacherActionPanel icon="✓" title="Teaching review queues">
        <R
          a="Submission review"
          b="Open a published assignment to review learner work and return feedback."
        />
        <R
          a="Assessment results"
          b="Record and publish results for active learners in your assigned batches."
        />
      </TeacherActionPanel>
    </section>
  );
}
