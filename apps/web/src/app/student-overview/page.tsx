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
    <main className="enterprise-settings student-overview-standard">
      <header className="student-overview-heading">
        <div className="student-overview-title">
          <span className="student-overview-title-icon" aria-hidden="true">♙</span>
          <div><p>Students</p><h1>Student overview</h1><span>{academy?.name ?? "Academy"} student, admission and fee position.</span></div>
        </div>
        <div className="student-overview-actions">
            <Link href="/student-onboarding" className="enterprise-action-button">Add student</Link>
            <Link href="/student-management" className="enterprise-action-button enterprise-action-button-secondary">Open Student 360</Link>
        </div>
      </header>
      {message ? <p className="enterprise-page-state student-overview-message">{message}</p> : null}
      {summary ? (
        <>
          <section className="student-overview-kpis" aria-label="Student metrics">
            <Metric label="Total students" value={summary.totalStudents.toLocaleString("en-IN")} note={`${summary.activeStudents} active · ${summary.inactiveStudents} inactive`} />
            <Metric label="Total fees" value={money(summary.totalSubjectFees)} note={`${summary.activeSubjectFeeArrangements} active subject fees`} />
            <Metric label="Total admission fees" value={money(summary.totalAdmissionFees)} note="One-time fees configured" />
            <Metric label="Outstanding fees" value={money(summary.outstandingFees)} note={`${money(summary.overdueFees)} overdue`} emphasis={summary.overdueFees > 0 ? "warning" : undefined} />
          </section>
          <section className="student-overview-actions-grid">
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
  return <article className={`student-overview-kpi ${emphasis ? "student-overview-kpi-warning" : ""}`}><span>{label}</span><strong>{value}</strong><small>{note}</small></article>;
}
function Action({ href, title, text }: { href: string; title: string; text: string }) {
  return <Link href={href} className="student-overview-action"><span>Students</span><h3>{title}</h3><p>{text}</p><small>Open section →</small></Link>;
}
