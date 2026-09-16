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
};

const cards = [
  ["Students", "students", "/students", "Add learners and maintain the roster."],
  ["Teachers", "teachers", "/teachers", "Maintain the instructors who deliver classes."],
  ["Courses", "courses", "/courses", "Create music, tuition, and coaching programs."],
  ["Batches", "batches", "/batches", "Organize classes, capacity, and enrolment."],
] as const;

export default function DashboardPage() {
  const [data, setData] = useState<DashboardData>({ students: 0, teachers: 0, courses: 0, batches: 0 });
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
          setData({ students: 0, teachers: 0, courses: 0, batches: 0 });
          setMessage("Create your academy first to unlock the workspace.");
          return;
        }

        const [students, teachers, courses, batches] = await Promise.all([
          academyApi(`/api/academies/${academy.id}/students`, { cache: "no-store" }),
          academyApi(`/api/academies/${academy.id}/teachers`, { cache: "no-store" }),
          academyApi(`/api/academies/${academy.id}/courses`, { cache: "no-store" }),
          academyApi(`/api/academies/${academy.id}/batches`, { cache: "no-store" }),
        ]);
        if (![students, teachers, courses, batches].every((response) => response.ok)) throw new Error();

        setData({
          academy,
          students: (await students.json()).length,
          teachers: (await teachers.json()).length,
          courses: (await courses.json()).length,
          batches: (await batches.json()).length,
        });
        setMessage("");
      } catch {
        setMessage("The dashboard could not reach AcademyDesk. Confirm that the API is running on port 5092.");
      }
    }

    void loadDashboard();
  }, []);

  return <main className="min-h-screen bg-slate-950 text-slate-100">
    <WorkspaceNav />
    <div className="mx-auto max-w-6xl px-6 py-10">
      <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Owner workspace</p>
      <h1 className="mt-3 text-4xl font-semibold tracking-tight">{data.academy ? data.academy.name : "Academy dashboard"}</h1>
      <p className="mt-3 max-w-2xl text-slate-300">Your starting point for a music academy today, with the same structure ready for tuition and coaching operations.</p>
      {message && <p className="mt-7 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}

      <section className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map(([title, key, href, description]) => <Link key={key} href={href} className="rounded-2xl border border-slate-800 bg-slate-900 p-6 transition hover:border-cyan-500/70">
          <p className="text-sm text-slate-400">{title}</p><p className="mt-2 text-4xl font-semibold">{data[key]}</p><p className="mt-4 text-sm text-slate-300">{description}</p>
        </Link>)}
      </section>

      <section className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-6">
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
