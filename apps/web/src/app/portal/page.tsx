"use client";
import { FormEvent, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { academyApi, apiHeaders, apiUrl } from "@/lib/api";
import { ThemeToggle } from "@/components/theme-toggle";
type Me = {
  role: "Student" | "Parent";
  displayName: string;
  studentId?: string;
  parentId?: string;
  parentEmail?: string;
  parentPhone?: string;
  children?: {
    id: string;
    name: string;
    canViewAcademicProgress: boolean;
    canViewFinance: boolean;
    canViewDocuments: boolean;
    canManageLeave: boolean;
  }[];
};
type D = {
  name: string;
  email?: string;
  phone?: string;
  firstName: string;
  lastName: string;
  preferredName?: string;
  gender?: string;
  dateOfBirth?: string;
  addressLine1?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  schedule: {
    id: string;
    batchName: string;
    startUtc: string;
    deliveryMode: string;
    roomName?: string;
    meetingLink?: string;
  }[];
  assignments: { id: string; title: string; type: string; dueAtUtc?: string }[];
  attendanceSummary: { total: number; present: number; late: number };
  practiceSummary: { logCount: number; totalMinutes: number };
  practiceLogs: {
    practiceDate: string;
    minutesPracticed: number;
    teacherFeedback?: string;
  }[];
  music: { title: string; status: string }[];
  resources: { title: string; type: string; url: string }[];
  assessmentResults: {
    title: string;
    score: number;
    maxScore: number;
    grade?: string;
  }[];
  certificates: {
    certificateNumber: string;
    title: string;
    issuedDate: string;
    notes?: string;
  }[];
  invoices: {
    id: string;
    invoiceNumber: string;
    balance: number;
    currency: string;
    dueDate: string;
    status: string;
  }[];
  classHistory: {
    sessionId: string;
    batchName: string;
    startUtc: string;
    endUtc: string;
    deliveryMode: string;
    status: string;
    attendanceStatus: string;
    resources: { title: string; description?: string; type: string; url: string }[];
  }[];
  cycleProgress: {
    batchId: string;
    batchName: string;
    sessionMinutes: number;
    cycleTotal: number;
    completedInCycle: number;
    remainingInCycle: number;
    coveredDates: string[];
    upcomingDates: string[];
  }[];
};
type Notice = { id: string; title: string; message: string; status: string; createdAtUtc?: string };
type Announcement = { id: string; title: string; message: string };
type Leave = {
  id: string;
  startDate: string;
  endDate: string;
  reason: string;
  status: string;
};
type Tab = "home" | "classes" | "library" | "tasks" | "progress" | "more";
const tabs: [Tab, string, string][] = [
  ["home", "Home", "⌂"],
  ["classes", "My Classes", "◷"],
  ["library", "Library", "▤"],
  ["tasks", "Tasks", "✓"],
  ["progress", "Progress", "↗"],
  ["more", "More", "•••"],
];
const dt = (x: string) =>
  new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Asia/Kolkata",
  }).format(new Date(x));
const cash = (x: number, c = "INR") =>
  new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: c,
    maximumFractionDigits: 0,
  }).format(x);
const indiaDateKey = (value: string | Date) => {
  const parts = new Intl.DateTimeFormat("en-IN", { timeZone: "Asia/Kolkata", year: "numeric", month: "2-digit", day: "2-digit" }).formatToParts(new Date(value));
  const part = (type: string) => parts.find(item => item.type === type)?.value ?? "";
  return `${part("year")}-${part("month")}-${part("day")}`;
};
export default function Portal() {
  const [me, setMe] = useState<Me>();
  const [id, setId] = useState("");
  const [d, setD] = useState<D>();
  const [n, setN] = useState<Notice[]>([]);
  const [announcements, setAnnouncements] = useState<Announcement[]>([]);
  const [l, setL] = useState<Leave[]>([]);
  const [t, setT] = useState<Tab>("home");
  const [m, setM] = useState("Loading your learning workspace…");
  async function load(student = id) {
    if (!student) return;
    const [a, b, c, e] = await Promise.all([
      academyApi(`/api/portal/students/${student}`),
      academyApi(`/api/portal/students/${student}/leave-requests`),
      academyApi("/api/portal/notifications"),
      academyApi("/api/portal/announcements"),
    ]);
    if (!a.ok) throw Error("Your student record could not be loaded.");
    setD(await a.json());
    if (b.ok) setL(await b.json());
    if (c.ok) setN(await c.json());
    if (e.ok) setAnnouncements(await e.json());
    setM("");
  }
  useEffect(() => {
    void academyApi("/api/portal/me")
      .then(async (r) => {
        if (!r.ok) throw Error();
        const x: Me = await r.json();
        const student =
          x.role === "Student" ? x.studentId : x.children?.[0]?.id;
        if (!student) throw Error();
        setMe(x);
        setId(student);
        await load(student);
      })
      .catch(() =>
        setM("This account is not linked to a student or Parent portal."),
      );
  }, []);
  return (
    <main className="enterprise-app-shell teacher-portal-shell learner-portal-shell">
      <aside className="enterprise-sidebar teacher-portal-sidebar learner-portal-sidebar">
        <a href="/portal" className="enterprise-brand">
          <span>A</span>
          <strong>AcademyDesk</strong>
        </a>
        <nav className="enterprise-nav-section teacher-portal-nav learner-portal-nav" aria-label="Student workspace">
          <p>{me?.role === "Parent" ? "Parent portal" : "Student portal"}</p>
          {tabs.map(([k, x, i]) => (
            <button key={k} data-active={t === k} onClick={() => setT(k)}>
              <i>{i}</i>
              {x}
            </button>
          ))}
        </nav>
      </aside>
      <section className="enterprise-workspace teacher-portal-workspace learner-portal-workspace">
        <header className="enterprise-topbar">
          <div className="teacher-portal-context"><strong>{me?.role === "Parent" ? "Parent workspace" : d?.name ?? "Student workspace"}</strong></div>
          <div className="enterprise-utilities"><PortalNotifications notices={n} setNotices={setN} /><ThemeToggle /><StudentPortalProfile /></div>
        </header>
        {announcements.length > 0 && <AnnouncementTicker announcements={announcements} />}
        <section className="learner-content teacher-portal-content learner-portal-content">
          {me?.role === "Parent" && (
            <div className="learner-switcher">
              <label>
                Viewing
                <select
                  value={id}
                  onChange={(e) => {
                    setId(e.target.value);
                    void load(e.target.value);
                  }}
                >
                  {me.children?.map((x) => (
                    <option key={x.id} value={x.id}>
                      {x.name}
                    </option>
                  ))}
                </select>
              </label>
              <small>
                {me.children?.length || 0} permitted student
                {me.children?.length === 1 ? "" : "s"}
              </small>
            </div>
          )}
          {m ? (
            <p className="learner-state" role="status" aria-live="polite">
              {m}
            </p>
          ) : (
            d && (
              <View
                d={d}
                id={id}
                n={n}
                l={l}
                tab={t}
                refresh={load}
                parentId={me?.role === "Parent" ? me.parentId : undefined}
                parentEmail={me?.role === "Parent" ? me.parentEmail : undefined}
                parentPhone={me?.role === "Parent" ? me.parentPhone : undefined}
                parentAccess={
                  me?.role === "Parent"
                    ? me.children?.find((child) => child.id === id)
                    : undefined
                }
              />
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
function StudentPortalProfile() {
  const router = useRouter();
  const [account, setAccount] = useState<{ displayName?: string; profileImageUrl?: string | null }>({});
  const [open, setOpen] = useState(false);
  const [message, setMessage] = useState("");
  const ref = useRef<HTMLDivElement>(null);
  const input = useRef<HTMLInputElement>(null);
  const name = account.displayName || "Student";
  const initials = name.split(" ").map((part) => part[0]).join("").slice(0, 2).toUpperCase();
  const imageUrl = account.profileImageUrl ? account.profileImageUrl.startsWith("http") ? account.profileImageUrl : `${apiUrl}${account.profileImageUrl}` : undefined;
  useEffect(() => { void academyApi("/api/auth/session").then(async (response) => response.ok && setAccount(await response.json())).catch(() => undefined); }, []);
  useEffect(() => { if (!open) return; const close = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) setOpen(false); }; document.addEventListener("mousedown", close); return () => document.removeEventListener("mousedown", close); }, [open]);
  async function upload(event: React.ChangeEvent<HTMLInputElement>) { const file = event.target.files?.[0]; event.target.value = ""; if (!file) return; const body = new FormData(); body.append("image", file); const response = await academyApi("/api/auth/session/profile-image", { method: "POST", body }); const result = await response.json().catch(() => null); if (!response.ok) return setMessage(result?.message ?? "Profile image could not be saved."); setAccount((current) => ({ ...current, profileImageUrl: result.profileImageUrl })); setMessage(""); setOpen(false); }
  function signOut() { window.localStorage.removeItem("academydesk.accessToken"); window.localStorage.removeItem("academydesk.refreshToken"); router.push("/login"); }
  return <div className="enterprise-profile" ref={ref}><button type="button" className="enterprise-profile-trigger" onClick={() => setOpen((value) => !value)} aria-label="Open profile menu" aria-expanded={open}>{imageUrl ? <img src={imageUrl} alt="Profile" /> : <span>{initials}</span>}</button>{open && <div className="enterprise-profile-menu" role="menu"><strong>{name}</strong><small>Student portal</small><input ref={input} className="sr-only" type="file" accept="image/png,image/jpeg,image/webp" onChange={(event) => void upload(event)} /><button type="button" className="enterprise-profile-menu-action" onClick={() => input.current?.click()}>Edit profile picture</button><button type="button" className="enterprise-profile-menu-action" onClick={signOut}>Sign out</button>{message && <small role="status">{message}</small>}</div>}</div>;
}
function AnnouncementTicker({ announcements }: { announcements: Announcement[] }) {
  const text = announcements.map((item) => `${item.title}: ${item.message}`).join("   •   ");
  return <div className="learner-announcement" role="status" aria-label="Academy announcement"><span>Important</span><div><p>{text}   •   {text}</p></div></div>;
}
function PortalNotifications({ notices, setNotices }: { notices: Notice[]; setNotices: React.Dispatch<React.SetStateAction<Notice[]>> }) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const unread = notices.filter((notice) => notice.status !== "Read").length;
  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => { if (!ref.current?.contains(event.target as Node)) setOpen(false); };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [open]);
  async function openNotifications() {
    setOpen((value) => !value);
    const unreadItems = notices.filter((notice) => notice.status !== "Read");
    if (unreadItems.length === 0) return;
    await Promise.all(unreadItems.map((notice) => academyApi(`/api/portal/notifications/${notice.id}/read`, { method: "PATCH" })));
    setNotices((items) => items.map((item) => ({ ...item, status: "Read" })));
  }
  return <div className="learner-notifications" ref={ref}>
    <button type="button" className="learner-notification-button" onClick={() => void openNotifications()} aria-label="Open notifications" aria-expanded={open}>♢{unread > 0 && <em>{unread > 9 ? "9+" : unread}</em>}</button>
    {open && <section className="learner-notification-menu"><header><strong>Notifications</strong><small>{unread ? `${unread} new` : "All caught up"}</small></header>{notices.length ? notices.slice(0, 8).map((notice) => <article key={notice.id}><b>{notice.title}</b><span>{notice.message}</span></article>) : <p>No notifications yet.</p>}</section>}
  </div>;
}
function View({
  d,
  id,
  n,
  l,
  tab,
  refresh,
  parentId,
  parentEmail,
  parentPhone,
  parentAccess,
}: {
  d: D;
  id: string;
  n: Notice[];
  l: Leave[];
  tab: Tab;
  refresh: () => Promise<void>;
  parentId?: string;
  parentEmail?: string;
  parentPhone?: string;
  parentAccess?: NonNullable<Me["children"]>[number];
}) {
  const rate = d.attendanceSummary.total
    ? Math.round(
        ((d.attendanceSummary.present + d.attendanceSummary.late) * 100) /
          d.attendanceSummary.total,
      )
    : 0;
  const balance = d.invoices.reduce((s, x) => s + x.balance, 0);
  return (
    <>
      <header className="learner-heading">
        <p>
          {parentAccess
            ? "Parent workspace / permitted student"
            : "Student workspace"}
        </p>
        <h1>
          {tab === "home"
            ? `Good day, ${d.name.split(" ")[0]}`
            : tab === "classes" ? "My Classes" : tab[0].toUpperCase() + tab.slice(1)}
        </h1>
        <span>
          {parentAccess
            ? "You are viewing only the information this student has permitted for your Parent account."
            : "Your private academy records and daily learning actions."}
        </span>
      </header>
      {tab === "home" && (
        <>
          <section className="learner-kpis">
            <K
              a="Next class"
              b={d.schedule[0] ? dt(d.schedule[0].startUtc) : "—"}
              c={d.schedule[0]?.batchName || "No upcoming class"}
            />
            <K
              a="Attendance"
              b={rate + "%"}
              c={d.attendanceSummary.total + " classes"}
            />
            <K
              a="Practice"
              b={d.practiceSummary.totalMinutes + " min"}
              c={d.practiceSummary.logCount + " logs"}
            />
            <K
              a="Fee balance"
              b={cash(balance)}
              c={balance ? "Review fees" : "No outstanding balance"}
            />
          </section>
          <P title="Today’s classes">
            <TodayClasses sessions={d.schedule} />
          </P>
        </>
      )}
      {tab === "classes" && (
        <section className="learner-tab-grid">
          <P title="Class calendar" className="learner-grid-wide learner-calendar-panel">
            <ClassCalendar sessions={d.schedule} />
          </P>
        </section>
      )}
      {tab === "library" && (
        <section className="learner-tab-grid">
          <P title="Learning resources">
            {d.resources.map((x, i) => (
              <ResourceRow key={i} x={x} />
            ))}
          </P>
          <P title="Class history">
            <ClassHistoryFilter items={d.classHistory} />
          </P>
        </section>
      )}
      {tab === "tasks" && (
        <section className="learner-tab-grid">
          <P title="Assignments">
            {d.assignments.map((x) =>
              parentAccess ? (
                <R key={x.id} a={x.title} b={`${x.type} · Parent view`} />
              ) : (
                <Assignment key={x.id} id={id} x={x} refresh={refresh} />
              ),
            )}
          </P>
          {!parentAccess && <Practice id={id} refresh={refresh} />}
          {(!parentAccess || parentAccess.canManageLeave) && (
            <Leave id={id} items={l} />
          )}
        </section>
      )}
      {tab === "progress" && (
        <section className="learner-tab-grid">
          <P title="Current class cycle">
            {d.cycleProgress.map((cycle) => <CycleRow key={cycle.batchId} cycle={cycle} />)}
          </P>
          <P title="Music and practice progress">
            {d.music.map((x, i) => (
              <R key={i} a={x.title} b={x.status} />
            ))}
            {d.practiceLogs.map((x, i) => (
              <R
                key={i}
                a={x.practiceDate + " · " + x.minutesPracticed + " min"}
                b={x.teacherFeedback || "Practice logged"}
              />
            ))}
          </P>
          <P title="Assessment results">
            {d.assessmentResults.map((x, i) => (
              <R
                key={i}
                a={x.title + (x.grade ? " · " + x.grade : "")}
                b={x.score + "/" + x.maxScore}
              />
            ))}
          </P>
          <P title="Certificates">
            {d.certificates.map((x) => (
              <DownloadRow key={x.certificateNumber} a={x.title} b={x.certificateNumber + " · " + x.issuedDate} label="Download certificate" path={`/api/portal/students/${id}/certificates/${encodeURIComponent(x.certificateNumber)}/download`} filename={`${x.certificateNumber}.html`} />
            ))}
          </P>
        </section>
      )}
      {tab === "more" && (
        <section className="learner-tab-grid">
          <P title="Fees and payments">
            <InvoicePaymentFilter studentId={id} invoices={d.invoices} />
          </P>
          {parentAccess ? (
            <ParentProfile
              parentId={parentId}
              email={parentEmail}
              phone={parentPhone}
            />
          ) : (
            <Profile id={id} d={d} />
          )}
          <P title="Notifications">
            {n.map((x) => (
              <R key={x.id} a={x.title} b={x.message} />
            ))}
          </P>
        </section>
      )}
    </>
  );
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
function P({ title, children, className = "" }: { title: string; children: React.ReactNode; className?: string }) {
  return (
    <section className={`teacher-action-panel learner-student-panel ${className}`}>
      <header><i aria-hidden="true">◌</i><h3>{title}</h3></header>
      <div className="learner-list">
        {children || <p className="learner-empty">Nothing to show yet.</p>}
      </div>
    </section>
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
function TodayClasses({ sessions }: { sessions: D["schedule"] }) {
  const today = indiaDateKey(new Date());
  const items = sessions.filter(session => indiaDateKey(session.startUtc) === today);
  return <>{items.length ? items.map(session => <SessionRow key={session.id} x={session} actionLabel="Start class" />) : <p className="learner-empty">No classes today.</p>}</>;
}
function SessionRow({ x, actionLabel = "Open class" }: { x: D["schedule"][number]; actionLabel?: string }) {
  const canJoin = (x.deliveryMode === "Online" || x.deliveryMode === "Hybrid") && x.meetingLink;
  return <article className="learner-row learner-session-row"><div><b>{x.batchName}</b><small>{dt(x.startUtc)} · {x.deliveryMode}{x.roomName ? ` · ${x.roomName}` : ""}</small></div>{canJoin && <a className="learner-join-link" href={x.meetingLink} target="_blank" rel="noreferrer" aria-label={`${actionLabel} for ${x.batchName}`}><span>{actionLabel}</span><span aria-hidden="true">↗</span></a>}</article>;
}
function ClassCalendar({ sessions }: { sessions: D["schedule"] }) {
  const [month, setMonth] = useState(() => { const now = new Date(); return new Date(now.getFullYear(), now.getMonth(), 1); });
  const year = month.getFullYear(); const monthIndex = month.getMonth(); const firstDay = new Date(year, monthIndex, 1).getDay(); const days = new Date(year, monthIndex + 1, 0).getDate();
  const monthKey = `${year}-${String(monthIndex + 1).padStart(2, "0")}`;
  const byDay = new Map<string, D["schedule"]>();
  sessions.filter(session => indiaDateKey(session.startUtc).startsWith(monthKey)).forEach(session => { const key = indiaDateKey(session.startUtc); byDay.set(key, [...(byDay.get(key) ?? []), session]); });
  const cells = Array.from({ length: firstDay + days }, (_, index) => index < firstDay ? null : index - firstDay + 1);
  return <div className="learner-calendar"><header><button type="button" aria-label="Previous month" onClick={() => setMonth(new Date(year, monthIndex - 1, 1))}>Previous</button><strong>{month.toLocaleDateString("en-IN", { month: "long", year: "numeric" })}</strong><button type="button" aria-label="Next month" onClick={() => setMonth(new Date(year, monthIndex + 1, 1))}>Next</button></header><div className="learner-calendar-weekdays">{["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"].map(day => <span key={day}>{day}</span>)}</div><div className="learner-calendar-grid">{cells.map((day, index) => { if (!day) return <div key={`empty-${index}`} className="learner-calendar-empty" />; const key = `${monthKey}-${String(day).padStart(2, "0")}`; const daySessions = byDay.get(key) ?? []; return <div className="learner-calendar-day" key={key}><b>{day}</b>{daySessions.map(session => <CalendarEvent key={session.id} session={session} />)}</div>; })}</div></div>;
}
function CalendarEvent({ session }: { session: D["schedule"][number] }) {
  const canJoin = (session.deliveryMode === "Online" || session.deliveryMode === "Hybrid") && session.meetingLink;
  if (canJoin) return <a className="learner-calendar-event learner-calendar-join" href={session.meetingLink} target="_blank" rel="noreferrer" aria-label={`Open class ${session.batchName}`}><span>{session.batchName}</span><strong>Open class</strong></a>;
  return <div className="learner-calendar-event"><span>{session.batchName}</span><small>{session.deliveryMode}</small></div>;
}
function ResourceRow({ x }: { x: { title: string; type: string; url: string; description?: string } }) {
  const downloadable = x.url.startsWith("/") || x.url.startsWith("http");
  return <article className="learner-row learner-resource-row"><b>{x.title}</b><small>{x.type}{x.description ? ` · ${x.description}` : ""}</small>{downloadable && <a href={x.url.startsWith("/") ? `${apiUrl}${x.url}` : x.url} target="_blank" rel="noreferrer">Open / download</a>}</article>;
}
function ClassHistoryRow({ item }: { item: D["classHistory"][number] }) {
  const [expanded, setExpanded] = useState(false);
  return <article className="learner-history-row"><div className="learner-history-summary"><div><b>{item.batchName}</b><small>{dt(item.startUtc)} · {item.deliveryMode} · {item.attendanceStatus}</small></div><button type="button" onClick={() => setExpanded(value => !value)} aria-expanded={expanded}>{expanded ? "Hide details" : "View details"}</button></div>{expanded && <div className="learner-history-resources">{item.resources.length ? item.resources.map((resource, index) => <ResourceRow key={index} x={resource} />) : <span>No notes, attachments, or recordings were shared for this class.</span>}</div>}</article>;
}
function ClassHistoryFilter({ items }: { items: D["classHistory"] }) {
  const [from, setFrom] = useState(""); const [to, setTo] = useState("");
  const filtered = items.filter(item => (!from || item.startUtc.slice(0, 10) >= from) && (!to || item.startUtc.slice(0, 10) <= to));
  return <><div className="learner-filter-bar"><input aria-label="History from date" type="date" value={from} onChange={event => setFrom(event.target.value)} /><input aria-label="History to date" type="date" value={to} min={from || undefined} onChange={event => setTo(event.target.value)} /><button type="button" onClick={() => { setFrom(""); setTo(""); }}>Clear</button></div>{filtered.length ? filtered.map(item => <ClassHistoryRow key={item.sessionId} item={item} />) : <p className="learner-empty">No classes match this date range.</p>}</>;
}
function CycleRow({ cycle }: { cycle: D["cycleProgress"][number] }) {
  const [expanded, setExpanded] = useState(false);
  const percent = Math.round((cycle.completedInCycle / cycle.cycleTotal) * 100);
  return <article className="learner-cycle-row"><div><b>{cycle.batchName}</b><small>{cycle.sessionMinutes} min sessions · Current 4-week cycle</small></div><div className="learner-cycle-count"><strong>{cycle.completedInCycle}/{cycle.cycleTotal}</strong><small>{cycle.remainingInCycle} class{cycle.remainingInCycle === 1 ? "" : "es"} remaining</small></div><div className="learner-cycle-bar" aria-label={`${percent}% completed`}><i style={{ width: `${percent}%` }} /></div><button type="button" className="learner-cycle-toggle" onClick={() => setExpanded(value => !value)} aria-expanded={expanded}>{expanded ? "Hide class dates" : "View class dates"}</button>{expanded && <div className="learner-cycle-dates"><section><b>Covered</b>{cycle.coveredDates.length ? cycle.coveredDates.map(date => <span key={date}>{dt(date)}</span>) : <span>No classes completed</span>}</section><section><b>Upcoming</b>{cycle.upcomingDates.length ? cycle.upcomingDates.map(date => <span key={date}>{dt(date)}</span>) : <span>No sessions scheduled</span>}</section></div>}</article>;
}
async function downloadPortalFile(path: string, filename: string) {
  const response = await academyApi(path);
  if (!response.ok) throw Error("Download could not be prepared.");
  const blob = await response.blob(); const url = URL.createObjectURL(blob); const anchor = document.createElement("a"); anchor.href = url; anchor.download = filename; document.body.appendChild(anchor); anchor.click(); anchor.remove(); URL.revokeObjectURL(url);
}
function DownloadRow({ a, b, label, path, filename }: { a: string; b: string; label: string; path: string; filename: string }) {
  const [message, setMessage] = useState("");
  return <article className="learner-row learner-download-row"><div><b>{a}</b><small>{b}</small></div><button type="button" onClick={() => void downloadPortalFile(path, filename).catch(() => setMessage("Download could not be prepared."))}>{label}</button>{message && <small>{message}</small>}</article>;
}
function InvoicePaymentFilter({ studentId, invoices }: { studentId: string; invoices: D["invoices"] }) {
  const [cycle, setCycle] = useState(""); const [status, setStatus] = useState("All");
  const filtered = invoices.filter(invoice => (!cycle || invoice.dueDate.slice(0, 7) === cycle) && (status === "All" || (status === "Paid" ? invoice.balance <= 0 : invoice.balance > 0)));
  return <><div className="learner-filter-bar"><input aria-label="Payment cycle" type="month" value={cycle} onChange={event => setCycle(event.target.value)} /><select aria-label="Payment status" value={status} onChange={event => setStatus(event.target.value)}><option>All</option><option>Paid</option><option>Outstanding</option></select><button type="button" onClick={() => { setCycle(""); setStatus("All"); }}>Clear</button></div>{filtered.length ? filtered.map(invoice => <DownloadRow key={invoice.invoiceNumber} a={invoice.invoiceNumber + " · " + cash(invoice.balance, invoice.currency)} b={`Due ${invoice.dueDate} · ${invoice.status}`} label="View / download invoice" path={`/api/portal/students/${studentId}/invoices/${invoice.id}/download`} filename={`${invoice.invoiceNumber}.html`} />) : <p className="learner-empty">No invoices match this filter.</p>}</>;
}
function Assignment({
  id,
  x,
  refresh,
}: {
  id: string;
  x: { id: string; title: string; type: string };
  refresh: () => Promise<void>;
}) {
  const [v, setV] = useState("");
  const [file, setFile] = useState<File>();
  const [m, setM] = useState("");
  async function go(e: FormEvent) {
    e.preventDefault();
    const r = await academyApi(
      `/api/portal/students/${id}/assignments/${x.id}/submit`,
      {
        method: "POST",
        body: (() => { const body = new FormData(); body.append("responseText", v); if (file) body.append("file", file); return body; })(),
      },
    );
    setM(r.ok ? "Submitted for review." : "Submission could not be saved.");
    if (r.ok) {
      setV("");
      setFile(undefined);
      void refresh();
    }
  }
  return (
    <article className="learner-assignment">
      <b>{x.title}</b>
      <small>{x.type}</small>
      <form onSubmit={go}>
        <textarea
          value={v}
          onChange={(e) => setV(e.target.value)}
          placeholder="Add a note about your practice (optional when uploading a file)…"
        />
        <label className="learner-upload-label">Practice recording, photo, video, or file<input type="file" accept="image/*,audio/*,video/*,.pdf,.doc,.docx" capture="environment" onChange={(event) => setFile(event.target.files?.[0])} /></label>
        {file && <small>{file.name} ready to upload</small>}
        <button>Submit</button>
      </form>
      <small>{m}</small>
    </article>
  );
}
function Practice({
  id,
  refresh,
}: {
  id: string;
  refresh: () => Promise<void>;
}) {
  return (
    <Action
      title="Log practice"
      url={`/api/portal/students/${id}/practice-logs`}
      fields={
        <>
          <input required name="practiceDate" type="date" />
          <input
            required
            name="minutesPracticed"
            type="number"
            min="1"
            placeholder="Minutes practised"
          />
          <input name="focusArea" placeholder="Focus area" />
        </>
      }
      refresh={refresh}
    />
  );
}
function Leave({ id, items }: { id: string; items: Leave[] }) {
  return (
    <Action
      title="Request leave"
      url={`/api/portal/students/${id}/leave-requests`}
      fields={
        <>
          <input required name="startDate" type="date" />
          <input required name="endDate" type="date" />
          <textarea required name="reason" placeholder="Reason for leave" />
        </>
      }
      extra={items.map((x) => (
        <R
          key={x.id}
          a={x.startDate + " to " + x.endDate}
          b={x.status + " · " + x.reason}
        />
      ))}
    />
  );
}
function Profile({ id, d }: { id: string; d: D }) {
  return (
    <Action
      title="My profile"
      url={`/api/portal/students/${id}/profile`}
      method="PUT"
      fields={
        <>
          <div className="learner-form-grid"><input required name="firstName" defaultValue={d.firstName || ""} placeholder="First name" /><input required name="lastName" defaultValue={d.lastName || ""} placeholder="Last name" /></div>
          <div className="learner-form-grid"><input name="preferredName" defaultValue={d.preferredName || ""} placeholder="Preferred name" /><select name="gender" defaultValue={d.gender || ""}><option value="">Gender (optional)</option><option>Female</option><option>Male</option><option>Non-binary</option><option>Prefer not to say</option></select></div>
          <input name="dateOfBirth" type="date" defaultValue={d.dateOfBirth || ""} />
          <input
            name="email"
            type="email"
            defaultValue={d.email || ""}
            placeholder="Email address"
          />
          <input
            name="phone"
            defaultValue={d.phone || ""}
            placeholder="Phone number"
          />
          <input name="addressLine1" defaultValue={d.addressLine1 || ""} placeholder="Address" />
          <div className="learner-form-grid"><input name="city" defaultValue={d.city || ""} placeholder="City" /><input name="state" defaultValue={d.state || ""} placeholder="State" /></div>
          <input name="postalCode" defaultValue={d.postalCode || ""} placeholder="Postal / PIN code" />
          <div className="learner-form-grid"><input name="emergencyContactName" defaultValue={d.emergencyContactName || ""} placeholder="Emergency contact name" /><input name="emergencyContactPhone" defaultValue={d.emergencyContactPhone || ""} placeholder="Emergency contact phone" /></div>
        </>
      }
    />
  );
}
function ParentProfile({
  parentId,
  email,
  phone,
}: {
  parentId?: string;
  email?: string;
  phone?: string;
}) {
  if (!parentId) return null;
  return (
    <Action
      title="My Parent contact details"
      url={`/api/portal/guardians/${parentId}/profile`}
      method="PUT"
      fields={
        <>
          <input
            name="email"
            type="email"
            defaultValue={email || ""}
            placeholder="Email address"
          />
          <input
            name="phone"
            defaultValue={phone || ""}
            placeholder="Phone number"
          />
        </>
      }
    />
  );
}
function Action({
  title,
  url,
  fields,
  method = "POST",
  refresh,
  extra,
}: {
  title: string;
  url: string;
  fields: React.ReactNode;
  method?: string;
  refresh?: () => Promise<void>;
  extra?: React.ReactNode;
}) {
  const [m, setM] = useState("");
  async function go(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    const body: Record<string, unknown> = Object.fromEntries(f);
    if ("minutesPracticed" in body)
      body.minutesPracticed = Number(body.minutesPracticed);
    const r = await academyApi(url, {
      method,
      headers: apiHeaders(true),
      body: JSON.stringify(body),
    });
    setM(r.ok ? "Saved successfully." : "Could not be saved.");
    if (r.ok) {
      e.currentTarget.reset();
      if (refresh) void refresh();
    }
  }
  return (
    <section className="teacher-action-panel learner-student-panel">
      <header><i aria-hidden="true">◌</i><h3>{title}</h3></header>
      <form className="learner-form" onSubmit={go}>
        {fields}
        <button>Save</button>
        <small>{m}</small>
      </form>
      {extra}
    </section>
  );
}
