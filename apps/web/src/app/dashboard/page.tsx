"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";

type Academy = { id: string; name: string };
type ScheduleItem = {
  id: string;
  batchName: string;
  teacherName?: string | null;
  startUtc: string;
  endUtc: string;
  status: string;
  roomName?: string | null;
  deliveryMode: string;
};
type ActivityItem = {
  id: string;
  action: string;
  entityType: string;
  occurredAtUtc: string;
};
type DashboardData = {
  academy?: Academy;
  students: number;
  teachers: number;
  courses: number;
  batches: number;
  openLeads: number;
  attendanceLast30Days: number;
  presentAttendanceLast30Days: number;
  outstandingBalance: number;
  classesToday: number;
  todaySchedule: ScheduleItem[];
  recentActivity: ActivityItem[];
};
type Session = {
  displayName: string;
  roles: string[];
  academyId?: string | null;
};

const emptyDashboard: DashboardData = {
  students: 0,
  teachers: 0,
  courses: 0,
  batches: 0,
  openLeads: 0,
  attendanceLast30Days: 0,
  presentAttendanceLast30Days: 0,
  outstandingBalance: 0,
  classesToday: 0,
  todaySchedule: [],
  recentActivity: [],
};

function greeting() {
  const hour = new Date().getHours();
  return hour < 12
    ? "Good morning"
    : hour < 17
      ? "Good afternoon"
      : "Good evening";
}
function formatRupees(amount: number) {
  return Math.abs(amount) >= 100000
    ? `₹${(amount / 100000).toFixed(2)}L`
    : `₹${amount.toLocaleString("en-IN")}`;
}
function formatAction(action: string) {
  return action
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/^./, (letter) => letter.toUpperCase());
}
function relativeTime(value: string) {
  const minutes = Math.max(
    0,
    Math.floor((Date.now() - new Date(value).getTime()) / 60000),
  );
  return minutes < 1
    ? "Just now"
    : minutes < 60
      ? `${minutes} min ago`
      : minutes < 1440
        ? `${Math.floor(minutes / 60)} hr ago`
        : `${Math.floor(minutes / 1440)} days ago`;
}

export default function DashboardPage() {
  const [data, setData] = useState<DashboardData>(emptyDashboard);
  const [session, setSession] = useState<Session>();
  const [message, setMessage] = useState("Loading your workspace…");

  useEffect(() => {
    async function loadDashboard() {
      try {
        const academyResponse = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (academyResponse.status === 401) {
          setMessage("Please sign in to open your workspace.");
          return;
        }
        if (!academyResponse.ok) throw new Error();
        const academies: Academy[] = await academyResponse.json();
        const academy = academies[0];
        if (!academy) {
          setData(emptyDashboard);
          setMessage("Create your academy first to unlock the workspace.");
          return;
        }
        const [dashboardResponse, sessionResponse] = await Promise.all([
          academyApi(`/api/academies/${academy.id}/dashboard`, {
            cache: "no-store",
          }),
          academyApi("/api/auth/session", { cache: "no-store" }),
        ]);
        if (!dashboardResponse.ok) throw new Error();
        const summary = await dashboardResponse.json();
        setData({
          academy,
          students: summary.activeStudents,
          teachers: summary.activeTeachers,
          courses: summary.activeCourses,
          batches: summary.activeBatches,
          openLeads: summary.openLeads,
          attendanceLast30Days: summary.attendanceRecordsLast30Days,
          presentAttendanceLast30Days: summary.presentAttendanceLast30Days,
          outstandingBalance: summary.outstandingBalance,
          classesToday: summary.classesToday ?? 0,
          todaySchedule: summary.todaySchedule ?? [],
          recentActivity: summary.recentActivity ?? [],
        });
        if (sessionResponse.ok) setSession(await sessionResponse.json());
        setMessage("");
      } catch {
        setMessage(
          "The dashboard could not reach AcademyDesk. Confirm that the API is running on port 5092.",
        );
      }
    }
    void loadDashboard();
  }, []);

  const attendanceRate = data.attendanceLast30Days
    ? Math.round(
        (data.presentAttendanceLast30Days / data.attendanceLast30Days) * 100,
      )
    : 0;
  return (
    <main className="enterprise-dashboard">
      <section className="enterprise-dashboard-heading">
        <div>
          <h2>
            {greeting()}, {session?.displayName || "there"}
          </h2>
        </div>
        <Link href="/students" className="enterprise-primary-action">
          <span aria-hidden="true">+</span> Add student
        </Link>
      </section>
      {message && (
        <p className="mt-7 rounded-xl border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">
          {message}
        </p>
      )}
      <section
        className="enterprise-kpi-grid"
        aria-label="Academy operating indicators"
      >
        <Link href="/students" className="enterprise-kpi">
          <span>Active students</span>
          <strong>{data.students}</strong>
          <small>
            {data.openLeads
              ? `${data.openLeads} open leads`
              : "View student records"}
          </small>
        </Link>
        <Link href="/schedule" className="enterprise-kpi">
          <span>Classes today</span>
          <strong>{data.classesToday}</strong>
          <small>
            {
              data.todaySchedule.filter((item) => item.status === "Scheduled")
                .length
            }{" "}
            scheduled or in progress
          </small>
        </Link>
        <Link href="/attendance" className="enterprise-kpi">
          <span>Attendance rate</span>
          <strong>{attendanceRate}%</strong>
          <small>
            {data.presentAttendanceLast30Days} present in the last 30 days
          </small>
        </Link>
        <Link href="/invoices" className="enterprise-kpi">
          <span>Outstanding fees</span>
          <strong>{formatRupees(data.outstandingBalance)}</strong>
          <small>Review invoices and payment follow-up</small>
        </Link>
      </section>
      <section className="enterprise-dashboard-panels">
        <section className="enterprise-data-panel">
          <header className="enterprise-panel-header">
            <h3>Today&apos;s schedule</h3>
            <Link href="/calendar">Calendar</Link>
          </header>
          {data.todaySchedule.length === 0 ? (
            <p className="enterprise-empty-row">
              No classes are scheduled today.
            </p>
          ) : (
            <table className="enterprise-schedule-table">
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Class / batch</th>
                  <th>Teacher</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {data.todaySchedule.map((item) => (
                  <tr key={item.id}>
                    <td>
                      {new Intl.DateTimeFormat("en-IN", {
                        hour: "2-digit",
                        minute: "2-digit",
                        hour12: false,
                      }).format(new Date(item.startUtc))}
                    </td>
                    <td>
                      {item.batchName}
                      {item.roomName ? ` · ${item.roomName}` : ""}
                    </td>
                    <td>{item.teacherName || "Unassigned"}</td>
                    <td>
                      <span className="enterprise-session-status">
                        {item.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
        <section className="enterprise-data-panel">
          <header className="enterprise-panel-header">
            <h3>Recent activity</h3>
            <Link href="/activity">Activity log</Link>
          </header>
          <div className="enterprise-activity-list">
            <Link href="/leads" className="enterprise-activity-item">
              <span className="enterprise-activity-dot" />
              <div>
                <strong>Admissions follow-up</strong>
                <small>
                  {data.openLeads
                    ? `${data.openLeads} open leads need a next step`
                    : "Review the admissions pipeline"}
                </small>
              </div>
            </Link>
            <Link href="/staff" className="enterprise-activity-item">
              <span className="enterprise-activity-dot" />
              <div>
                <strong>Team and access</strong>
                <small>
                  Add staff, link teachers, or manage academy accounts
                </small>
              </div>
            </Link>
            <Link
              href="/communication-settings"
              className="enterprise-activity-item"
            >
              <span className="enterprise-activity-dot" />
              <div>
                <strong>Communication readiness</strong>
                <small>Check channel setup before sending reminders</small>
              </div>
            </Link>
            {data.recentActivity.slice(0, 1).map((item) => (
              <Link
                href="/activity"
                key={item.id}
                className="enterprise-activity-item"
              >
                <span className="enterprise-activity-dot" />
                <div>
                  <strong>{formatAction(item.action)}</strong>
                  <small>
                    {item.entityType} · {relativeTime(item.occurredAtUtc)}
                  </small>
                </div>
              </Link>
            ))}
          </div>
        </section>
      </section>
    </main>
  );
}
