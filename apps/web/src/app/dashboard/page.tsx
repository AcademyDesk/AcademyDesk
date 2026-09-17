"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi } from "@/lib/api";

type Academy = { id: string; name: string };

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
};

const cards = [
  ["Students", "students", "/students", "Manage learners and their records.", "ST"],
  ["Teachers", "teachers", "/teachers", "Coordinate your teaching team.", "TE"],
  ["Courses", "courses", "/courses", "Maintain your programme catalogue.", "CO"],
  ["Batches", "batches", "/batches", "Monitor active learning groups.", "BA"],
] as const;

export default function DashboardPage() {
  const [data, setData] = useState<DashboardData>({ students: 0, teachers: 0, courses: 0, batches: 0, openLeads: 0, attendanceLast30Days: 0, presentAttendanceLast30Days: 0, outstandingBalance: 0 });
  const [message, setMessage] = useState("Loading your workspace…");

  useEffect(() => {
    async function loadDashboard() {
      try {
        const academyResponse = await academyApi("/api/academies", { cache: "no-store" });
        if (academyResponse.status === 401) {
          setMessage("Please sign in to open your workspace.");
          return;
        }
        if (!academyResponse.ok) throw new Error();

        const academies: Academy[] = await academyResponse.json();
        const academy = academies[0];
        if (!academy) {
          setData({ students: 0, teachers: 0, courses: 0, batches: 0, openLeads: 0, attendanceLast30Days: 0, presentAttendanceLast30Days: 0, outstandingBalance: 0 });
          setMessage("Create your academy first to unlock the workspace.");
          return;
        }

        const dashboard = await academyApi(`/api/academies/${academy.id}/dashboard`, { cache: "no-store" });
        if (!dashboard.ok) throw new Error();
        const summary = await dashboard.json();

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
        });
        setMessage("");
      } catch {
        setMessage("The dashboard could not reach AcademyDesk. Confirm that the API is running on port 5092.");
      }
    }

    void loadDashboard();
  }, []);

  const attendanceRate = data.attendanceLast30Days ? Math.round((data.presentAttendanceLast30Days / data.attendanceLast30Days) * 100) : 0;

  return <>
    <WorkspaceNav />
    <main className="dashboard-shell min-h-screen text-slate-100">
    <div className="mx-auto max-w-6xl px-6 py-10">
      <section className="dashboard-hero rounded-3xl p-7 md:p-9">
        <div className="flex flex-col justify-between gap-7 md:flex-row md:items-end">
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">AcademyDesk · Owner workspace</p>
            <h1 className="mt-3 text-4xl font-semibold tracking-tight md:text-5xl">{data.academy ? data.academy.name : "Academy dashboard"}</h1>
            <p className="mt-4 max-w-2xl text-slate-300">A single command centre for learning delivery, academy operations, and financial health.</p>
          </div>
          <div className="enterprise-workspace-status">
            <span>Workspace status</span>
            <strong>{message ? "Refreshing data" : "Operational"}</strong>
          </div>
        </div>
      </section>
      {message && <p className="mt-7 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}

      <section className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map(([title, key, href, description, marker]) => <Link key={key} href={href} className="metric-card enterprise-metric-card rounded-2xl p-6 transition duration-200">
          <div className="flex items-start justify-between gap-4"><p className="text-sm text-slate-400">{title}</p><span className="enterprise-metric-marker">{marker}</span></div><p className="mt-3 text-4xl font-semibold">{data[key]}</p><p className="mt-4 text-sm text-slate-300">{description}</p>
        </Link>)}
      </section>

      <section className="mt-8 grid gap-5 lg:grid-cols-[1.1fr_0.9fr]">
        <section className="surface-panel rounded-2xl p-6">
          <div className="flex items-start justify-between gap-4"><div><p className="text-sm font-semibold uppercase tracking-[0.16em] text-slate-400">Operational pulse</p><h2 className="mt-2 text-xl font-semibold">Academy health at a glance</h2></div><Link href="/reports" className="enterprise-inline-link">View reports →</Link></div>
          <div className="mt-6 grid gap-3 sm:grid-cols-3">
            <Link href="/leads" className="enterprise-signal-card"><span>Open leads</span><strong>{data.openLeads}</strong><small>Prospects to follow up</small></Link>
            <Link href="/attendance" className="enterprise-signal-card"><span>Attendance rate</span><strong>{attendanceRate}%</strong><small>{data.presentAttendanceLast30Days} of {data.attendanceLast30Days} recorded</small></Link>
            <Link href="/finance" className="enterprise-signal-card"><span>Outstanding</span><strong>₹{data.outstandingBalance.toLocaleString("en-IN")}</strong><small>Pending invoice balance</small></Link>
          </div>
        </section>
        <section className="surface-panel rounded-2xl p-6">
          <p className="text-sm font-semibold uppercase tracking-[0.16em] text-slate-400">Quick actions</p>
          <h2 className="mt-2 text-xl font-semibold">Continue setting up your academy</h2>
          <div className="mt-5 grid gap-2">
            <Link href="/students" className="enterprise-quick-action"><span>01</span><div><strong>Add a student</strong><small>Create a learner record and contact profile.</small></div><b aria-hidden="true">→</b></Link>
            <Link href="/batches" className="enterprise-quick-action"><span>02</span><div><strong>Create a batch</strong><small>Set up the class, schedule, and capacity.</small></div><b aria-hidden="true">→</b></Link>
            <Link href="/fee-plans" className="enterprise-quick-action"><span>03</span><div><strong>Configure fees</strong><small>Prepare plans and invoices for collections.</small></div><b aria-hidden="true">→</b></Link>
          </div>
        </section>
      </section>
    </div>
    </main>
  </>;
}
