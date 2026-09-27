"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
import { StandardDetailModal, StandardInteractiveTile } from "@/components/design-system/controls";

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
type Student = { id: string; firstName: string; lastName: string; isActive: boolean };
type Invoice = { id: string; studentId: string; invoiceNumber: string; totalAmount: number; paidAmount: number; dueDate: string; status: string };
type PayrollPayout = { id: string; workerName: string; periodLabel: string; netAmount: number; status: string };
type Attendance = { studentId: string; status: string };
type DetailData = { students: Student[]; invoices: Invoice[]; payroll: PayrollPayout[]; attendance: Attendance[] };

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
const emptyDetails: DetailData = { students: [], invoices: [], payroll: [], attendance: [] };

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
  const [details, setDetails] = useState<DetailData>(emptyDetails);
  const [detail, setDetail] = useState<"students" | "classes" | "attendance" | "outstanding" | null>(null);

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
        const [studentResponse, invoiceResponse, payrollResponse, allSessionsResponse] = await Promise.all([
          academyApi(`/api/academies/${academy.id}/students`),
          academyApi(`/api/academies/${academy.id}/invoices`),
          academyApi(`/api/academies/${academy.id}/payroll/payouts`),
          academyApi(`/api/academies/${academy.id}/sessions`),
        ]);
        const allSessions: { id: string }[] = allSessionsResponse.ok ? await allSessionsResponse.json() : [];
        const attendanceRows = await Promise.all(allSessions.map(async (item) => {
          const response = await academyApi(`/api/academies/${academy.id}/sessions/${item.id}/attendance`);
          return response.ok ? await response.json() as Attendance[] : [];
        }));
        setDetails({ students: studentResponse.ok ? await studentResponse.json() : [], invoices: invoiceResponse.ok ? await invoiceResponse.json() : [], payroll: payrollResponse.ok ? await payrollResponse.json() : [], attendance: attendanceRows.flat() });
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
  const activeStudents = details.students.filter((student) => student.isActive);
  const outstandingInvoices = details.invoices.filter((invoice) => invoice.totalAmount - invoice.paidAmount > 0);
  const outstandingPayroll = details.payroll.filter((payout) => payout.status !== "Paid");
  const payrollDue = outstandingPayroll.reduce((sum, payout) => sum + payout.netAmount, 0);
  return (
    <main className="enterprise-dashboard">
      <section className="enterprise-dashboard-heading">
        <div>
          <h2>
            {greeting()}, {session?.displayName || "there"}
          </h2>
        </div>
        <Link href="/student-onboarding" className="enterprise-primary-action">
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
        <StandardInteractiveTile label="Active students" value={data.students} detail="View active student names" onClick={() => setDetail("students")} className="enterprise-kpi" />
        <StandardInteractiveTile label="Classes today" value={data.classesToday} detail="View scheduled classes" onClick={() => setDetail("classes")} className="enterprise-kpi" />
        <StandardInteractiveTile label="Attendance rate" value={`${attendanceRate}%`} detail="View attendance by student" onClick={() => setDetail("attendance")} className="enterprise-kpi" />
        <StandardInteractiveTile label="Outstanding fees" value={formatRupees(data.outstandingBalance + payrollDue)} detail="View student fees and teacher salary due" onClick={() => setDetail("outstanding")} className="enterprise-kpi" />
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
                        timeZone: "Asia/Kolkata",
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
      {detail === "students" && <StandardDetailModal eyebrow="Workspace overview" title="Active students" onClose={() => setDetail(null)}><DetailList rows={activeStudents.map((student) => [fullName(student), "Active student"])} empty="No active students." /></StandardDetailModal>}
      {detail === "classes" && <StandardDetailModal eyebrow="Workspace overview" title="Classes today" onClose={() => setDetail(null)}><DetailList rows={data.todaySchedule.map((item) => [item.batchName, `${formatTime(item.startUtc)} · ${item.teacherName || "Unassigned"} · ${item.status}`])} empty="No classes are scheduled today." /></StandardDetailModal>}
      {detail === "attendance" && <StandardDetailModal eyebrow="Workspace overview" title="Attendance by student" onClose={() => setDetail(null)}><DetailList rows={activeStudents.map((student) => { const records = details.attendance.filter((record) => record.studentId === student.id); const present = records.filter((record) => ["Present", "Late", "Online"].includes(record.status)).length; return [fullName(student), records.length ? `${Math.round(present * 100 / records.length)}% · ${present} of ${records.length} classes` : "No attendance recorded"]; })} empty="No active students." /></StandardDetailModal>}
      {detail === "outstanding" && <StandardDetailModal eyebrow="Workspace overview" title="Outstanding fees and salary" onClose={() => setDetail(null)}><div className="standard-detail-group"><h3>Student fee payments</h3><DetailList rows={outstandingInvoices.map((invoice) => [fullName(details.students.find((student) => student.id === invoice.studentId)), `${invoice.invoiceNumber} · ${formatRupees(invoice.totalAmount - invoice.paidAmount)} due`])} empty="No student fees are outstanding." /><h3>Teacher salary payments</h3><DetailList rows={outstandingPayroll.map((payout) => [payout.workerName, `${payout.periodLabel} · ${formatRupees(payout.netAmount)} · ${payout.status}`])} empty="No teacher salary payments are outstanding." /></div></StandardDetailModal>}
    </main>
  );
}
function DetailList({ rows, empty }: { rows: [string, string][]; empty: string }) { return rows.length ? <div className="standard-detail-list">{rows.map(([title, detail], index) => <article key={`${title}-${index}`}><b>{title}</b><small>{detail}</small></article>)}</div> : <p className="standard-detail-empty">{empty}</p>; }
function fullName(student?: Student) { return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; }
function formatTime(value: string) { return new Intl.DateTimeFormat("en-IN", { hour: "numeric", minute: "2-digit", timeZone: "Asia/Kolkata" }).format(new Date(value)); }
