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
  schedule: {
    id: string;
    batchName: string;
    startUtc: string;
    deliveryMode: string;
    roomName?: string;
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
  }[];
  invoices: {
    invoiceNumber: string;
    balance: number;
    currency: string;
    dueDate: string;
    status: string;
  }[];
};
type Notice = { id: string; title: string; message: string; status: string };
type Leave = {
  id: string;
  startDate: string;
  endDate: string;
  reason: string;
  status: string;
};
type Tab = "home" | "classes" | "tasks" | "progress" | "more";
const tabs: [Tab, string, string][] = [
  ["home", "Home", "⌂"],
  ["classes", "Classes", "◷"],
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
export default function Portal() {
  const [me, setMe] = useState<Me>();
  const [id, setId] = useState("");
  const [d, setD] = useState<D>();
  const [n, setN] = useState<Notice[]>([]);
  const [l, setL] = useState<Leave[]>([]);
  const [t, setT] = useState<Tab>("home");
  const [m, setM] = useState("Loading your learning workspace…");
  async function load(student = id) {
    if (!student) return;
    const [a, b, c] = await Promise.all([
      academyApi(`/api/portal/students/${student}`),
      academyApi(`/api/portal/students/${student}/leave-requests`),
      academyApi("/api/portal/notifications"),
    ]);
    if (!a.ok) throw Error("Your student record could not be loaded.");
    setD(await a.json());
    if (b.ok) setL(await b.json());
    if (c.ok) setN(await c.json());
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
          <div className="enterprise-utilities"><ThemeToggle /><StudentPortalProfile /></div>
        </header>
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
            : tab[0].toUpperCase() + tab.slice(1)}
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
          <P title="Today and next">
            {d.schedule.slice(0, 4).map((x) => (
              <R
                key={x.id}
                a={x.batchName}
                b={dt(x.startUtc) + " · " + x.deliveryMode}
              />
            ))}
          </P>
        </>
      )}
      {tab === "classes" && (
        <>
          <P title="Upcoming timetable">
            {d.schedule.map((x) => (
              <R
                key={x.id}
                a={x.batchName}
                b={
                  dt(x.startUtc) +
                  " · " +
                  x.deliveryMode +
                  (x.roomName ? " · " + x.roomName : "")
                }
              />
            ))}
          </P>
          <P title="Learning resources">
            {d.resources.map((x, i) => (
              <a key={i} className="learner-row" href={x.url} target="_blank">
                <b>{x.title}</b>
                <small>{x.type} · Open resource →</small>
              </a>
            ))}
          </P>
        </>
      )}
      {tab === "tasks" && (
        <>
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
        </>
      )}
      {tab === "progress" && (
        <>
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
              <R
                key={x.certificateNumber}
                a={x.title}
                b={x.certificateNumber + " · " + x.issuedDate}
              />
            ))}
          </P>
        </>
      )}
      {tab === "more" && (
        <>
          <P title="Fees and payments">
            {d.invoices.map((x) => (
              <R
                key={x.invoiceNumber}
                a={x.invoiceNumber + " · " + cash(x.balance, x.currency)}
                b={"Due " + x.dueDate + " · " + x.status}
              />
            ))}
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
        </>
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
function P({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="teacher-action-panel learner-student-panel">
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
  const [m, setM] = useState("");
  async function go(e: FormEvent) {
    e.preventDefault();
    const r = await academyApi(
      `/api/portal/students/${id}/assignments/${x.id}/submit`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({ responseText: v }),
      },
    );
    setM(r.ok ? "Submitted for review." : "Submission could not be saved.");
    if (r.ok) {
      setV("");
      void refresh();
    }
  }
  return (
    <article className="learner-assignment">
      <b>{x.title}</b>
      <small>{x.type}</small>
      <form onSubmit={go}>
        <textarea
          required
          value={v}
          onChange={(e) => setV(e.target.value)}
          placeholder="Write your submission…"
        />
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
      title="My contact details"
      url={`/api/portal/students/${id}/profile`}
      method="PUT"
      fields={
        <>
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
