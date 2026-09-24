"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
import { StandardDetailModal, StandardInteractiveTile } from "@/components/design-system/controls";

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
type Student = { id: string; firstName: string; lastName: string; isActive: boolean };
type Batch = { id: string; name: string };
type FeeArrangement = { amount: number; subjectName: string; isActive: boolean };
type Invoice = { studentId: string; invoiceNumber: string; totalAmount: number; paidAmount: number };

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
  const [subjectFees, setSubjectFees] = useState<Record<string, FeeArrangement[]>>({});
  const [admissionFees, setAdmissionFees] = useState<Record<string, number>>({});
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [detail, setDetail] = useState<"students" | "fees" | "admission" | "outstanding" | null>(null);
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
        const studentRows: Student[] = studentResponse.ok ? await studentResponse.json() : [];
        setStudents(studentRows);
        setBatches(batchResponse.ok ? await batchResponse.json() : []);
        const [invoiceResponse, ...feeResponses] = await Promise.all([
          academyApi(`/api/academies/${current.id}/invoices`, { cache: "no-store" }),
          ...studentRows.flatMap((student) => [academyApi(`/api/academies/${current.id}/students/${student.id}/fee-arrangements`, { cache: "no-store" }), academyApi(`/api/academies/${current.id}/students/${student.id}/fee-arrangements/admission-fee`, { cache: "no-store" })]),
        ]);
        setInvoices(invoiceResponse.ok ? await invoiceResponse.json() : []);
        const nextSubjectFees: Record<string, FeeArrangement[]> = {};
        const nextAdmissionFees: Record<string, number> = {};
        await Promise.all(studentRows.map(async (student, index) => {
          const arrangements = feeResponses[index * 2]; const admission = feeResponses[index * 2 + 1];
          nextSubjectFees[student.id] = arrangements?.ok ? await arrangements.json() : [];
          const admissionData = admission?.ok ? await admission.json() as { amount?: number | null } : null;
          nextAdmissionFees[student.id] = admissionData?.amount ?? 0;
        }));
        setSubjectFees(nextSubjectFees); setAdmissionFees(nextAdmissionFees);
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
  const studentName = (student?: Student) => student ? `${student.firstName} ${student.lastName}` : "Unknown student";
  const activeStudents = students.filter((student) => student.isActive);
  const balanceFor = (studentId: string) => invoices.filter((invoice) => invoice.studentId === studentId).reduce((sum, invoice) => sum + Math.max(0, invoice.totalAmount - invoice.paidAmount), 0);

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
            <StandardInteractiveTile label="Total students" value={summary.totalStudents.toLocaleString("en-IN")} detail={`${summary.activeStudents} active · ${summary.inactiveStudents} inactive`} onClick={() => setDetail("students")} className="student-overview-kpi" />
            <StandardInteractiveTile label="Total fees" value={money(summary.totalSubjectFees)} detail={`${summary.activeSubjectFeeArrangements} active subject fees`} onClick={() => setDetail("fees")} className="student-overview-kpi" />
            <StandardInteractiveTile label="Total admission fees" value={money(summary.totalAdmissionFees)} detail="View admission fee by student" onClick={() => setDetail("admission")} className="student-overview-kpi" />
            <StandardInteractiveTile label="Outstanding fees" value={money(summary.outstandingFees)} detail={`${money(summary.overdueFees)} overdue`} onClick={() => setDetail("outstanding")} className={`student-overview-kpi ${summary.overdueFees > 0 ? "student-overview-kpi-warning" : ""}`} />
          </section>
          {detail === "students" && <StandardDetailModal eyebrow="Student overview" title="Students" onClose={() => setDetail(null)}><StudentDetails rows={students.map((student) => [studentName(student), `${student.isActive ? "Active" : "Inactive"} · Total active fees ${money((subjectFees[student.id] ?? []).filter((fee) => fee.isActive).reduce((sum, fee) => sum + fee.amount, 0))}`])} empty="No students found." /></StandardDetailModal>}
          {detail === "fees" && <StandardDetailModal eyebrow="Student overview" title="Active student fees" onClose={() => setDetail(null)}><StudentDetails rows={activeStudents.flatMap((student) => (subjectFees[student.id] ?? []).filter((fee) => fee.isActive).map((fee) => [studentName(student), `${fee.subjectName} · ${money(fee.amount)}`] as [string, string]))} empty="No active subject fees configured." /></StandardDetailModal>}
          {detail === "admission" && <StandardDetailModal eyebrow="Student overview" title="Admission fees" onClose={() => setDetail(null)}><StudentDetails rows={students.filter((student) => admissionFees[student.id] > 0).map((student) => [studentName(student), money(admissionFees[student.id])])} empty="No admission fees configured." /></StandardDetailModal>}
          {detail === "outstanding" && <StandardDetailModal eyebrow="Student overview" title="Outstanding student fees" onClose={() => setDetail(null)}><StudentDetails rows={students.filter((student) => balanceFor(student.id) > 0).map((student) => [studentName(student), money(balanceFor(student.id))])} empty="No student fees are outstanding." /></StandardDetailModal>}
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

function Insight({ title, value, details }: { title: string; value: string; details: string[] }) {
  return <article className="student-overview-insight"><span>Student delivery</span><div><h3>{title}</h3><strong>{value}</strong></div><ul>{details.map((detail) => <li key={detail}>{detail}</li>)}</ul></article>;
}
function StudentDetails({ rows, empty }: { rows: [string, string][]; empty: string }) { return rows.length ? <div className="standard-detail-list">{rows.map(([title, detail], index) => <article key={`${title}-${index}`}><b>{title}</b><small>{detail}</small></article>)}</div> : <p className="standard-detail-empty">{empty}</p>; }
