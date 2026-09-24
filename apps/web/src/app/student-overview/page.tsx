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
type Session = { id: string; batchId: string; startUtc: string; deliveryMode: string };
type Enrollment = { studentId: string; batchId: string; status: string };
type Student = { id: string; firstName: string; lastName: string };
type Batch = { id: string; name: string };

const money = (value: number) => `₹${value.toLocaleString("en-IN")}`;
const indiaDay = (value: Date) => new Intl.DateTimeFormat("en-CA", { timeZone: "Asia/Kolkata" }).format(value);
const indiaTime = (value: string) => new Intl.DateTimeFormat("en-IN", { hour: "numeric", minute: "2-digit", timeZone: "Asia/Kolkata" }).format(new Date(value));

export default function StudentOverviewPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [summary, setSummary] = useState<Summary>();
  const [sessions, setSessions] = useState<Session[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [message, setMessage] = useState("Loading student overview…");

  useEffect(() => {
    void (async () => {
      try {
        const academies = await academyApi("/api/academies", { cache: "no-store" });
        const current = (await academies.json())[0] as Academy | undefined;
        if (!current) return setMessage("Create an academy before viewing student operations.");
        setAcademy(current);
        const [summaryResponse, sessionResponse, enrollmentResponse, studentResponse, batchResponse] = await Promise.all([
          academyApi(`/api/academies/${current.id}/students/overview`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/sessions`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/enrollments`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/students`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/batches`, { cache: "no-store" }),
        ]);
        if (!summaryResponse.ok) throw new Error();
        setSummary(await summaryResponse.json());
        setSessions(sessionResponse.ok ? await sessionResponse.json() : []);
        setEnrollments(enrollmentResponse.ok ? await enrollmentResponse.json() : []);
        setStudents(studentResponse.ok ? await studentResponse.json() : []);
        setBatches(batchResponse.ok ? await batchResponse.json() : []);
        setMessage("");
      } catch {
        setMessage("Student overview could not be loaded.");
      }
    })();
  }, []);
  const today = indiaDay(new Date());
  const todaySessions = sessions.filter((session) => indiaDay(new Date(session.startUtc)) === today);
  const weekEnd = new Date();
  weekEnd.setDate(weekEnd.getDate() + 7);
  const upcomingSessions = sessions.filter((session) => new Date(session.startUtc) >= new Date() && new Date(session.startUtc) <= weekEnd);
  const learnersFor = (sessionRows: Session[]) => {
    const batchIds = new Set(sessionRows.map((session) => session.batchId));
    return students.filter((student) => enrollments.some((enrollment) => enrollment.studentId === student.id && enrollment.status.toLowerCase() === "active" && batchIds.has(enrollment.batchId)));
  };
  const todayLearners = learnersFor(todaySessions);
  const weekLearners = learnersFor(upcomingSessions);
  const batchName = (id: string) => batches.find((batch) => batch.id === id)?.name ?? "Class";

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
          <section className="student-overview-insights-grid" aria-label="Student delivery insights">
            <Insight title="Students with classes today" value={String(todayLearners.length)} details={todayLearners.length ? todayLearners.slice(0, 4).map((student) => `${student.firstName} ${student.lastName}`) : ["No students scheduled today."]} />
            <Insight title="Today’s class schedule" value={String(todaySessions.length)} details={todaySessions.length ? todaySessions.slice(0, 4).map((session) => `${indiaTime(session.startUtc)} · ${batchName(session.batchId)}`) : ["No classes scheduled today."]} />
            <Insight title="Students scheduled this week" value={String(weekLearners.length)} details={weekLearners.length ? weekLearners.slice(0, 4).map((student) => `${student.firstName} ${student.lastName}`) : ["No classes scheduled in the next 7 days."]} />
          </section>
        </>
      ) : null}
    </main>
  );
}

function Metric({ label, value, note, emphasis }: { label: string; value: string; note: string; emphasis?: "warning" }) {
  return <article className={`student-overview-kpi ${emphasis ? "student-overview-kpi-warning" : ""}`}><span>{label}</span><strong>{value}</strong><small>{note}</small></article>;
}
function Insight({ title, value, details }: { title: string; value: string; details: string[] }) {
  return <article className="student-overview-insight"><span>Student delivery</span><div><h3>{title}</h3><strong>{value}</strong></div><ul>{details.map((detail) => <li key={detail}>{detail}</li>)}</ul></article>;
}
