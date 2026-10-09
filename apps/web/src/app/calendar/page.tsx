"use client";

import { useEffect, useMemo, useState } from "react";
import { academyApi } from "@/lib/api";

type Academy = { id: string };
type Batch = {
  id: string;
  name: string;
  courseId: string;
  teacherId?: string | null;
  meetingPattern?: string | null;
  meetingLink?: string | null;
};
type Session = {
  id: string;
  batchId: string;
  teacherId?: string | null;
  startUtc: string;
  deliveryMode: string;
  roomName?: string | null;
};
type Event = {
  id: string;
  title: string;
  type: string;
  startUtc: string;
  venue?: string;
};
type Makeup = {
  id: string;
  studentId: string;
  batchId: string;
  startUtc: string;
  deliveryMode?: string | null;
  venue?: string | null;
  meetingLink?: string | null;
};
type Student = { id: string; firstName: string; lastName: string };
type Teacher = { id: string; firstName: string; lastName: string };
type Course = { id: string; name: string };
type Enrollment = { studentId: string; batchId: string; status: string };
type CalendarItem = {
  id: string;
  type: "Class" | "Make-up" | "Event";
  title: string;
  detail: string;
  start: Date;
  href?: string;
  actionLabel?: string;
  opensExternally?: boolean;
  subject?: string;
  teacher?: string;
  students?: string[];
  meetingPattern?: string;
  deliveryMode?: string;
  location?: string;
};
const names = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
// Grid dates are civil-day markers, not instants. UTC arithmetic on these
// markers avoids the viewer's timezone/DST; real item instants stay unchanged.
const indiaDayFormat = new Intl.DateTimeFormat("en-CA", {
  year: "numeric", month: "2-digit", day: "2-digit", timeZone: "Asia/Kolkata",
});
function calendarDay(instant: Date): Date {
  const parts = indiaDayFormat.formatToParts(instant);
  const number = (type: string) => Number(parts.find((part) => part.type === type)?.value);
  return new Date(Date.UTC(number("year"), number("month") - 1, number("day")));
}
function calendarMonth(instant: Date): Date {
  const day = calendarDay(instant);
  return new Date(Date.UTC(day.getUTCFullYear(), day.getUTCMonth(), 1));
}
const monthName = (date: Date) =>
  new Intl.DateTimeFormat("en-IN", { month: "long", year: "numeric", timeZone: "UTC" }).format(
    date,
  );
const time = (date: Date) =>
  new Intl.DateTimeFormat("en-IN", {
    hour: "numeric",
    minute: "2-digit",
    timeZone: "Asia/Kolkata",
  }).format(date);

function meetingUrl(value?: string | null): string | undefined {
  const candidate = value?.trim();
  if (!candidate || !/^https?:\/\//i.test(candidate) || /\s/.test(candidate)) return undefined;
  try {
    const url = new URL(candidate);
    return ["http:", "https:"].includes(url.protocol) && url.hostname && !url.username && !url.password
      ? candidate
      : undefined;
  } catch { return undefined; }
}

function AgendaItem({ item }: { item: CalendarItem }) {
  const [studentsExpanded, setStudentsExpanded] = useState(false);
  const isClass = item.type === "Class";
  const students = item.students ?? [];
  const schedule = new Intl.DateTimeFormat("en-IN", {
    weekday: "short",
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "Asia/Kolkata",
  }).format(item.start);

  return (
    <li className="workspace-calendar-agenda-item">
      <div className="workspace-calendar-agenda-item-heading">
        <div>
          <span className="calendar-kind">{item.type}</span>
          <h3>{item.title}</h3>
        </div>
        {item.href && item.actionLabel && <a
          href={item.href}
          className="workspace-calendar-agenda-action"
          target={item.opensExternally ? "_blank" : undefined}
          rel={item.opensExternally ? "noreferrer" : undefined}
        >
          {item.actionLabel}
        </a>}
      </div>
      {isClass ? (
        <>
          <dl className="workspace-calendar-agenda-details">
            <div><dt>Subject</dt><dd>{item.subject ?? "Not assigned"}</dd></div>
            <div><dt>Time</dt><dd>{time(item.start)}</dd></div>
            <div><dt>Schedule</dt><dd>{item.meetingPattern || schedule}</dd></div>
            <div><dt>Teacher</dt><dd>{item.teacher ?? "Unassigned"}</dd></div>
            <div><dt>Delivery</dt><dd>{item.deliveryMode}</dd></div>
            <div><dt>Location</dt><dd title={item.location} style={{ whiteSpace: "normal", overflowWrap: "anywhere" }}>{item.location || "Not specified"}</dd></div>
          </dl>
          <div className="workspace-calendar-agenda-students">
            <div>
              <span>Students</span>
              <strong>{students.length ? `${students.length} active student${students.length === 1 ? "" : "s"}` : "No active students"}</strong>
            </div>
            {students.length > 1 && (
              <button type="button" onClick={() => setStudentsExpanded((value) => !value)}>
                {studentsExpanded ? "Hide students" : `Show ${students.length} students`}
              </button>
            )}
          </div>
          {(students.length === 1 || studentsExpanded) && (
            <ul className="workspace-calendar-student-list">
              {students.map((student) => <li key={student}>{student}</li>)}
            </ul>
          )}
        </>
      ) : (
        <p className="workspace-calendar-agenda-summary"><time>{schedule} · {time(item.start)}</time>{item.detail}</p>
      )}
    </li>
  );
}

export default function CalendarPage() {
  const [batches, setBatches] = useState<Batch[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [events, setEvents] = useState<Event[]>([]);
  const [makeups, setMakeups] = useState<Makeup[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const today = calendarDay(new Date());
  // Static-export HTML must not bake the build machine's current month into
  // hydration. Resolve the India month only after mounting in the browser.
  const [month, setMonth] = useState<Date | null>(null);
  const [filter, setFilter] = useState<"All" | CalendarItem["type"]>("All");
  const [message, setMessage] = useState("Loading calendar…");
  const [agendaExpanded, setAgendaExpanded] = useState(true);
  useEffect(() => {
    const monthTimer = setTimeout(() => setMonth(calendarMonth(new Date())), 0);
    void (async () => {
      try {
        const academyResponse = await academyApi("/api/academies");
        const academies: Academy[] = await academyResponse.json();
        if (!academyResponse.ok || !academies[0]) throw new Error();
        const id = academies[0].id;
        const responses = await Promise.all(
          ["batches", "sessions", "events", "makeup-classes", "students", "teachers", "courses", "enrollments"].map(
            (path) => academyApi(`/api/academies/${id}/${path}`),
          ),
        );
        // Sessions and batches are the core calendar. Optional records must
        // not hide the whole month if one supporting endpoint is unavailable.
        if (!responses[0].ok || !responses[1].ok) throw new Error();
        const readRows = async <T,>(response: Response): Promise<T[]> => response.ok ? response.json() : [];
        setBatches(await readRows<Batch>(responses[0]));
        setSessions(await readRows<Session>(responses[1]));
        setEvents(await readRows<Event>(responses[2]));
        setMakeups(await readRows<Makeup>(responses[3]));
        setStudents(await readRows<Student>(responses[4]));
        setTeachers(await readRows<Teacher>(responses[5]));
        setCourses(await readRows<Course>(responses[6]));
        setEnrollments(await readRows<Enrollment>(responses[7]));
        setMessage("");
      } catch {
        setMessage("Calendar could not be loaded.");
      }
    })();
    return () => clearTimeout(monthTimer);
  }, []);
  const items = useMemo<CalendarItem[]>(() => {
    const batchName = (id: string) =>
      batches.find((batch) => batch.id === id)?.name ?? "Class";
    const studentName = (id: string) => {
      const student = students.find((row) => row.id === id);
      return student ? `${student.firstName} ${student.lastName}` : "Student";
    };
    const batch = (id: string) => batches.find((row) => row.id === id);
    const classStudents = (batchId: string) =>
      enrollments
        .filter((row) => row.batchId === batchId && row.status.toLowerCase() === "active")
        .map((row) => studentName(row.studentId));
    const courseName = (courseId: string) =>
      courses.find((course) => course.id === courseId)?.name ?? "Not assigned";
    const teacherName = (teacherId?: string | null) => {
      const teacher = teachers.find((row) => row.id === teacherId);
      return teacher ? `${teacher.firstName} ${teacher.lastName}` : "Unassigned";
    };
    return [
      ...sessions.map((row) => {
        const assignedBatch = batch(row.batchId);
        const isVirtualClass = ["online", "hybrid"].includes(row.deliveryMode.trim().toLowerCase());
        // A stored session teacher is authoritative (including null/unassigned).
        // Only an absent session location uses a legacy batch link; never
        // replace a supplied location with a different meeting destination.
        const location = row.roomName?.trim() || (isVirtualClass ? assignedBatch?.meetingLink?.trim() : undefined);
        const meetingLink = isVirtualClass
          ? meetingUrl(location)
          : undefined;
        return ({
        id: row.id,
        type: "Class" as const,
        title: batchName(row.batchId),
        detail: `${row.deliveryMode}${location ? ` · ${location}` : ""}`,
        start: new Date(row.startUtc),
        href: meetingLink,
        actionLabel: meetingLink ? "Open" : undefined,
        opensExternally: Boolean(meetingLink),
        subject: assignedBatch ? courseName(assignedBatch.courseId) : "Not assigned",
        teacher: teacherName(row.teacherId),
        deliveryMode: row.deliveryMode,
        location,
        students: classStudents(row.batchId),
        meetingPattern: assignedBatch?.meetingPattern ?? undefined,
      });
      }),
      ...makeups.map((row) => {
        const mode = row.deliveryMode?.trim().toLowerCase();
        const location = mode === "online" || mode === "hybrid" ? row.meetingLink?.trim() : row.venue?.trim();
        return ({
        id: row.id,
        type: "Make-up" as const,
        title: `${studentName(row.studentId)} · ${batchName(row.batchId)}`,
        detail: location || "Make-up class",
        deliveryMode: row.deliveryMode ?? undefined,
        location: location || undefined,
        start: new Date(row.startUtc),
        href: "/makeup",
        actionLabel: "Open make-up",
      });
      }),
      ...events.map((row) => ({
        id: row.id,
        type: "Event" as const,
        title: row.title,
        detail: `${row.type}${row.venue ? ` · ${row.venue}` : ""}`,
        start: new Date(row.startUtc),
        href: "/events",
        actionLabel: "View event",
      })),
    ].filter((item) => filter === "All" || item.type === filter);
  }, [batches, sessions, events, makeups, students, teachers, courses, enrollments, filter]);
  const days = useMemo(() => {
    if (!month) return [];
    const offset = (month.getUTCDay() + 6) % 7;
    const start = new Date(month);
    start.setUTCDate(1 - offset);
    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(start);
      date.setUTCDate(start.getUTCDate() + index);
      return date;
    });
  }, [month]);
  const byDay = (date: Date) =>
    items
      .filter((item) => +calendarDay(item.start) === +date)
      .sort((a, b) => +a.start - +b.start);
  const agenda = items
    .filter(
      (item) => month && +calendarMonth(item.start) === +month,
    )
    .sort((a, b) => +a.start - +b.start);
  if (!month) return <main className="enterprise-settings workspace-calendar" aria-busy="true"><p>Loading calendar…</p></main>;
  return (
    <main className="enterprise-settings workspace-calendar">
        <header className="workspace-calendar-heading">
          <div className="workspace-calendar-title">
            <span className="workspace-calendar-title-icon" aria-hidden="true">▦</span>
            <div><p>Workspace</p><h1>Calendar</h1><span>Classes, make-ups and academy events in one view. All dates and times use India time (IST).</span></div>
          </div>
          <div className="workspace-calendar-month-actions" aria-label="Calendar navigation">
            <button
              onClick={() =>
                setMonth(new Date(Date.UTC(month.getUTCFullYear(), month.getUTCMonth() - 1, 1)))
              }
              className="calendar-nav"
              aria-label="Previous month"
            >
              ‹
            </button>
            <button
              onClick={() =>
                setMonth(calendarMonth(new Date()))
              }
              className="calendar-today"
            >
              Today
            </button>
            <button
              onClick={() =>
                setMonth(new Date(Date.UTC(month.getUTCFullYear(), month.getUTCMonth() + 1, 1)))
              }
              className="calendar-nav"
              aria-label="Next month"
            >
              ›
            </button>
          </div>
        </header>
        <section className="workspace-calendar-panel">
          <header className="workspace-calendar-panel-header">
            <div><p>Month view · IST</p><h2>{monthName(month)}</h2></div>
            <div className="workspace-calendar-filters" aria-label="Filter calendar items">
              {(["All", "Class", "Make-up", "Event"] as const).map((value) => (
                <button
                  key={value}
                  onClick={() => setFilter(value)}
                  className={filter === value ? "calendar-filter active" : "calendar-filter"}
                  aria-pressed={filter === value}
                >
                  {value}
                </button>
              ))}
            </div>
          </header>
          {message && <p className="enterprise-page-state enterprise-page-state-loading workspace-calendar-message">{message}</p>}
          <section className="calendar-grid">
          {names.map((name) => (
            <div key={name} className="calendar-weekday">
              {name}
            </div>
          ))}
          {days.map((date) => {
            const dayItems = byDay(date);
            const inMonth = date.getUTCMonth() === month.getUTCMonth();
            const isToday = +date === +today;
            return (
              <div
                key={date.toISOString()}
                className={`calendar-day ${inMonth ? "" : "outside"} ${isToday ? "today" : ""}`}
              >
                <span className="calendar-date">{date.getUTCDate()}</span>
                <div className="calendar-events">
                  {dayItems.slice(0, 3).map((item) => (
                    item.href ? <a
                      key={`${item.type}-${item.id}`}
                      href={item.href}
                      className={`calendar-event ${item.type.toLowerCase().replace("-", "")}`}
                      title={`${item.title} · ${item.detail}`}
                      target={item.opensExternally ? "_blank" : undefined}
                      rel={item.opensExternally ? "noreferrer" : undefined}
                    >
                      <span><time>{time(item.start)}</time>{item.title}</span>
                      {item.actionLabel && <strong>{item.actionLabel}</strong>}
                    </a>
                    : <div
                      key={`${item.type}-${item.id}`}
                      className={`calendar-event ${item.type.toLowerCase().replace("-", "")}`}
                      title={`${item.title} · ${item.detail}`}
                    >
                      <span><time>{time(item.start)}</time>{item.title}</span>
                    </div>
                  ))}
                  {dayItems.length > 3 && (
                    <span className="calendar-more">
                      +{dayItems.length - 3} more
                    </span>
                  )}
                </div>
              </div>
            );
          })}
          </section>
        </section>
        <section className="workspace-calendar-agenda">
          <header className="workspace-calendar-agenda-header">
            <div><p>Schedule list</p><h2>Month agenda</h2></div>
            <div className="workspace-calendar-agenda-header-actions">
              <span>{agenda.length} items</span>
              <button
                type="button"
                className="workspace-calendar-agenda-toggle"
                aria-expanded={agendaExpanded}
                aria-controls="calendar-month-agenda-list"
                onClick={() => setAgendaExpanded((expanded) => !expanded)}
              >
                {agendaExpanded ? "Collapse" : "Expand"}
                <i aria-hidden="true">{agendaExpanded ? "⌃" : "⌄"}</i>
              </button>
            </div>
          </header>
          {agendaExpanded && (agenda.length ? (
            <ul id="calendar-month-agenda-list" className="workspace-calendar-agenda-list">
              {agenda.map((item) => (
                <AgendaItem
                  key={`agenda-${item.type}-${item.id}`}
                  item={item}
                />
              ))}
            </ul>
          ) : (
            <p id="calendar-month-agenda-list" className="py-8 text-center text-sm text-slate-400">
              No calendar items for this view.
            </p>
          ))}
        </section>
    </main>
  );
}
