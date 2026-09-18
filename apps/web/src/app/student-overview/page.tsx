"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";

type Academy = { id: string; name: string };
type Summary = {
  totalStudents: number;
  activeStudents: number;
  inactiveStudents: number;
  totalAdmissionFees: number;
  totalSubjectFees: number;
  outstandingFees: number;
  overdueFees: number;
  activeSubjectFeeArrangements: number;
};

const money = (value: number) => `₹${value.toLocaleString("en-IN")}`;

export default function StudentOverviewPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [summary, setSummary] = useState<Summary>();
  const [message, setMessage] = useState("Loading student overview…");

  useEffect(() => {
    void (async () => {
      try {
        const academies = await academyApi("/api/academies", { cache: "no-store" });
        const current = (await academies.json())[0] as Academy | undefined;
        if (!current) return setMessage("Create an academy before viewing student operations.");
        setAcademy(current);
        const response = await academyApi(`/api/academies/${current.id}/students/overview`, { cache: "no-store" });
        if (!response.ok) throw new Error();
        setSummary(await response.json());
        setMessage("");
      } catch {
        setMessage("Student overview could not be loaded.");
      }
    })();
  }, []);

  return (
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <p>Students / overview</p>
            <h2>Student overview</h2>
            <span>{academy?.name ?? "Academy"} student, admission and fee position.</span>
          </div>
          <div className="flex gap-3">
            <Link href="/student-onboarding" className="rounded bg-cyan-400 px-4 py-2.5 text-sm font-semibold text-slate-950">Add student</Link>
            <Link href="/student-management" className="rounded border border-slate-700 px-4 py-2.5 text-sm">Open Student 360</Link>
          </div>
        </div>
      </header>
      {message ? <p className="enterprise-page-state">{message}</p> : null}
      {summary ? (
        <>
          <section className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <Metric label="Total students" value={summary.totalStudents.toLocaleString("en-IN")} note={`${summary.activeStudents} active · ${summary.inactiveStudents} inactive`} />
            <Metric label="Total fees" value={money(summary.totalSubjectFees)} note={`${summary.activeSubjectFeeArrangements} active subject fees`} />
            <Metric label="Total admission fees" value={money(summary.totalAdmissionFees)} note="One-time fees configured" />
            <Metric label="Outstanding fees" value={money(summary.outstandingFees)} note={`${money(summary.overdueFees)} overdue`} emphasis={summary.overdueFees > 0 ? "warning" : undefined} />
          </section>
          <section className="mt-5 grid gap-5 xl:grid-cols-3">
            <Action href="/student-onboarding" title="Student onboarding" text="Create the governed student and parent record." />
            <Action href="/students" title="Student management" text="Edit status and open any student record." />
            <Action href="/student-fees" title="Student fee details" text="Set admission and subject-wise fees." />
          </section>
        </>
      ) : null}
    </main>
  );
}

function Metric({ label, value, note, emphasis }: { label: string; value: string; note: string; emphasis?: "warning" }) {
  return <article className="surface-panel rounded-xl p-5"><p className="text-sm text-slate-400">{label}</p><strong className={emphasis ? "mt-3 block text-3xl text-amber-300" : "mt-3 block text-3xl"}>{value}</strong><small className="mt-2 block text-sm text-slate-400">{note}</small></article>;
}
function Action({ href, title, text }: { href: string; title: string; text: string }) {
  return <Link href={href} className="enterprise-setting-card"><span>Students</span><h3>{title}</h3><p>{text}</p><footer><small>Open workspace</small><b>→</b></footer></Link>;
}
