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
  ["Students", "students", "/students", "Add learners and maintain the roster."],
  ["Teachers", "teachers", "/teachers", "Maintain the instructors who deliver classes."],
  ["Courses", "courses", "/courses", "Create music, tuition, and coaching programs."],
  ["Batches", "batches", "/batches", "Organize classes, capacity, and enrolment."],
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

  return <main className="dashboard-shell min-h-screen text-slate-100">
    <WorkspaceNav />
    <div className="mx-auto max-w-6xl px-6 py-10">
      <section className="dashboard-hero rounded-3xl p-7 md:p-9">
        <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Owner workspace</p>
        <h1 className="mt-3 text-4xl font-semibold tracking-tight md:text-5xl">{data.academy ? data.academy.name : "Academy dashboard"}</h1>
        <p className="mt-4 max-w-2xl text-slate-300">Your command centre for students, teaching, attendance, and academy finances.</p>
      </section>
      {message && <p className="mt-7 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}

      <section className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map(([title, key, href, description]) => <Link key={key} href={href} className="metric-card rounded-2xl p-6 transition duration-200">
          <p className="text-sm text-slate-400">{title}</p><p className="mt-2 text-4xl font-semibold">{data[key]}</p><p className="mt-4 text-sm text-slate-300">{description}</p>
        </Link>)}
      </section>

      <section className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
        <Link href="/leads" className="metric-card rounded-2xl p-6 transition duration-200"><p className="text-sm text-slate-400">Open leads</p><p className="mt-2 text-3xl font-semibold">{data.openLeads}</p><p className="mt-3 text-sm text-slate-300">Follow up with prospective learners.</p></Link>
        <Link href="/attendance" className="metric-card rounded-2xl p-6 transition duration-200"><p className="text-sm text-slate-400">Attendance, last 30 days</p><p className="mt-2 text-3xl font-semibold">{data.presentAttendanceLast30Days} / {data.attendanceLast30Days}</p><p className="mt-3 text-sm text-slate-300">Present or online records.</p></Link>
        <Link href="/finance" className="metric-card rounded-2xl p-6 transition duration-200"><p className="text-sm text-slate-400">Outstanding balance</p><p className="mt-2 text-3xl font-semibold">₹{data.outstandingBalance.toLocaleString("en-IN")}</p><p className="mt-3 text-sm text-slate-300">Invoices not yet covered by payments.</p></Link>
        <Link href="/reports" className="metric-card rounded-2xl p-6 transition duration-200"><p className="text-sm text-slate-400">Collection health</p><p className="mt-2 text-3xl font-semibold">{data.attendanceLast30Days ? Math.round((data.presentAttendanceLast30Days / data.attendanceLast30Days) * 100) : 0}%</p><p className="mt-3 text-sm text-slate-300">Recent attendance rate.</p></Link>
      </section>

      <section className="surface-panel mt-8 rounded-2xl p-6">
        <h2 className="text-xl font-semibold">Recommended setup order</h2>
        <ol className="mt-4 grid gap-3 text-sm text-slate-300 md:grid-cols-4">
          <li className="rounded-lg bg-slate-950 p-4">1. <Link href="/" className="text-cyan-300">Create academy</Link></li>
          <li className="rounded-lg bg-slate-950 p-4">2. <Link href="/branches" className="text-cyan-300">Add branches</Link></li>
          <li className="rounded-lg bg-slate-950 p-4">3. <Link href="/courses" className="text-cyan-300">Create courses</Link></li>
          <li className="rounded-lg bg-slate-950 p-4">4. <Link href="/students" className="text-cyan-300">Add students</Link> and batches</li>
        </ol>
      </section>
    </div>
  </main>;
}
