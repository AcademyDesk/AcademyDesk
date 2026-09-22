"use client";

import { useEffect, useMemo, useState } from "react";
import { academyApi } from "@/lib/api";

type Academy = { id: string };
type Batch = { id: string; name: string };
type Session = {
  id: string;
  batchId: string;
  startUtc: string;
  deliveryMode: string;
  roomName?: string;
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
  venue?: string;
};
type Student = { id: string; firstName: string; lastName: string };
type CalendarItem = {
  id: string;
  type: "Class" | "Make-up" | "Event";
  title: string;
  detail: string;
  start: Date;
  href: string;
};
const names = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
const monthName = (date: Date) =>
  new Intl.DateTimeFormat("en-IN", { month: "long", year: "numeric", timeZone: "Asia/Kolkata" }).format(
    date,
  );
const time = (date: Date) =>
  new Intl.DateTimeFormat("en-IN", {
    hour: "numeric",
    minute: "2-digit",
    timeZone: "Asia/Kolkata",
  }).format(date);

export default function CalendarPage() {
  const [batches, setBatches] = useState<Batch[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [events, setEvents] = useState<Event[]>([]);
  const [makeups, setMakeups] = useState<Makeup[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const today = new Date();
  const [month, setMonth] = useState(
    () => new Date(today.getFullYear(), today.getMonth(), 1),
  );
  const [filter, setFilter] = useState<"All" | CalendarItem["type"]>("All");
  const [message, setMessage] = useState("Loading calendar…");
  useEffect(() => {
    void (async () => {
      try {
        const academyResponse = await academyApi("/api/academies");
        const academies: Academy[] = await academyResponse.json();
        if (!academyResponse.ok || !academies[0]) throw new Error();
        const id = academies[0].id;
        const responses = await Promise.all(
          ["batches", "sessions", "events", "makeup-classes", "students"].map(
            (path) => academyApi(`/api/academies/${id}/${path}`),
          ),
        );
        if (responses.some((response) => !response.ok)) throw new Error();
        setBatches(await responses[0].json());
        setSessions(await responses[1].json());
        setEvents(await responses[2].json());
        setMakeups(await responses[3].json());
        setStudents(await responses[4].json());
        setMessage("");
      } catch {
        setMessage("Calendar could not be loaded.");
      }
    })();
  }, []);
  const items = useMemo<CalendarItem[]>(() => {
    const batchName = (id: string) =>
      batches.find((batch) => batch.id === id)?.name ?? "Class";
    const studentName = (id: string) => {
      const student = students.find((row) => row.id === id);
      return student ? `${student.firstName} ${student.lastName}` : "Student";
    };
    return [
      ...sessions.map((row) => ({
        id: row.id,
        type: "Class" as const,
        title: batchName(row.batchId),
        detail: `${row.deliveryMode}${row.roomName ? ` · ${row.roomName}` : ""}`,
        start: new Date(row.startUtc),
        href: `/schedule?session=${row.id}`,
      })),
      ...makeups.map((row) => ({
        id: row.id,
        type: "Make-up" as const,
        title: `${studentName(row.studentId)} · ${batchName(row.batchId)}`,
        detail: row.venue ?? "Make-up class",
        start: new Date(row.startUtc),
        href: "/makeup",
      })),
      ...events.map((row) => ({
        id: row.id,
        type: "Event" as const,
        title: row.title,
        detail: `${row.type}${row.venue ? ` · ${row.venue}` : ""}`,
        start: new Date(row.startUtc),
        href: "/events",
      })),
    ].filter((item) => filter === "All" || item.type === filter);
  }, [batches, sessions, events, makeups, students, filter]);
  const days = useMemo(() => {
    const offset = (month.getDay() + 6) % 7;
    const start = new Date(month);
    start.setDate(1 - offset);
    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(start);
      date.setDate(start.getDate() + index);
      return date;
    });
  }, [month]);
  const byDay = (date: Date) =>
    items
      .filter((item) => item.start.toDateString() === date.toDateString())
      .sort((a, b) => +a.start - +b.start);
  const agenda = items
    .filter(
      (item) =>
        item.start.getFullYear() === month.getFullYear() &&
        item.start.getMonth() === month.getMonth(),
    )
    .sort((a, b) => +a.start - +b.start);
  return (
    <main className="enterprise-settings workspace-calendar">
        <header className="workspace-calendar-heading">
          <div className="workspace-calendar-title">
            <span className="workspace-calendar-title-icon" aria-hidden="true">▦</span>
            <div><p>Workspace</p><h1>Calendar</h1><span>Classes, make-ups and academy events in one view.</span></div>
          </div>
          <div className="workspace-calendar-month-actions" aria-label="Calendar navigation">
            <button
              onClick={() =>
                setMonth(new Date(month.getFullYear(), month.getMonth() - 1, 1))
              }
              className="calendar-nav"
              aria-label="Previous month"
            >
              ‹
            </button>
            <button
              onClick={() =>
                setMonth(new Date(today.getFullYear(), today.getMonth(), 1))
              }
              className="calendar-today"
            >
              Today
            </button>
            <button
              onClick={() =>
                setMonth(new Date(month.getFullYear(), month.getMonth() + 1, 1))
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
            <div><p>Month view</p><h2>{monthName(month)}</h2></div>
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
            const inMonth = date.getMonth() === month.getMonth();
            const isToday = date.toDateString() === today.toDateString();
            return (
              <div
                key={date.toISOString()}
                className={`calendar-day ${inMonth ? "" : "outside"} ${isToday ? "today" : ""}`}
              >
                <span className="calendar-date">{date.getDate()}</span>
                <div className="calendar-events">
                  {dayItems.slice(0, 3).map((item) => (
                    <a
                      key={`${item.type}-${item.id}`}
                      href={item.href}
                      className={`calendar-event ${item.type.toLowerCase().replace("-", "")}`}
                      title={`${item.title} · ${item.detail}`}
                    >
                      <time>{time(item.start)}</time>
                      {item.title}
                    </a>
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
            <span>
              {agenda.length} items
            </span>
          </header>
          {agenda.length ? (
            <ul className="mt-4 divide-y divide-slate-800">
              {agenda.map((item) => (
                <li
                  key={`agenda-${item.type}-${item.id}`}
                  className="flex flex-wrap items-center gap-x-5 gap-y-1 py-3 text-sm"
                >
                  <time className="w-32 text-slate-400">
                    {new Intl.DateTimeFormat("en-IN", {
                      day: "2-digit",
                      month: "short",
                      hour: "numeric",
                      minute: "2-digit",
                    }).format(item.start)}
                  </time>
                  <span className="font-medium">{item.title}</span>
                  <span className="text-slate-400">{item.detail}</span>
                  <span className="calendar-kind">{item.type}</span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="py-8 text-center text-sm text-slate-400">
              No calendar items for this view.
            </p>
          )}
        </section>
    </main>
  );
}
