"use client";
import { useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { ThemeToggle } from "@/components/theme-toggle";
type P = {
  firstName: string;
  lastName: string;
  batches: { id: string; name: string; capacity: number }[];
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
type T = "today" | "classes" | "learners" | "tasks" | "more";
const tabs: [T, string, string][] = [
  ["today", "Today", "⌂"],
  ["classes", "Classes", "◷"],
  ["learners", "Learners", "♙"],
  ["tasks", "Teaching", "✓"],
  ["more", "More", "•••"],
];
const statuses = ["Present", "Absent", "Late", "Excused", "Online"];
const dt = (x: string) =>
  new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
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
    <main className="learner-shell">
      <header className="learner-topbar">
        <a href="/teacher" className="learner-brand">
          <span>A</span>
          <b>AcademyDesk</b>
          <small>Teacher</small>
        </a>
        <ThemeToggle />
      </header>
      <div className="learner-layout">
        <aside className="learner-sidebar">
          {tabs.map(([k, x, i]) => (
            <button key={k} data-active={t === k} onClick={() => setT(k)}>
              <i>{i}</i>
              {x}
            </button>
          ))}
        </aside>
        <section className="learner-content">
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
                        }}
                      >
                        <b>{name(x.batchId)}</b>
                        <small>
                          {dt(x.startUtc) +
                            " · " +
                            x.deliveryMode +
                            (x.roomName ? " · " + x.roomName : "")}
                        </small>
                      </button>
                    ))}
                  </Panel>
                )}
                {(t === "today" || t === "learners") && (
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
                {t === "tasks" && <TeacherTasks batches={p.batches} />}{" "}
                {t === "more" && <TeacherSelfService />}
              </>
            )
          )}
        </section>
      </div>
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
        <h3>Publish assignment</h3>
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
          <button>Publish assignment</button>
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
