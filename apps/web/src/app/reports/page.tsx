"use client";

import { useEffect, useMemo, useState } from "react";
import { academyApi } from "@/lib/api";
import { spreadsheetCsv } from "@/lib/csv";
import { reportAmount, reportCurrency, reportDifference, reportMoney } from "@/lib/reports-finance";
import { StandardDetailModal, StandardInteractiveTile } from "@/components/design-system/controls";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string; isActive: boolean };
type Teacher = { id: string; firstName: string; lastName: string; isActive: boolean };
type Batch = { id: string; name: string; capacity: number };
type Enrollment = { studentId: string; batchId: string; status: string };
type Session = { id: string; batchId: string; startUtc: string; status: string };
type Attendance = { studentId: string; status: string };
type Invoice = {
  id: string;
  studentId: string;
  invoiceNumber: string;
  totalAmount: number;
  balance: number;
  currency?: string;
  status: string;
  dueDate: string;
};
type Payment = { invoiceId: string; amount: number; currency?: string; status: string };
type Expense = { description: string; amount: number; currency?: string; category: string; expenseDate: string };
type PayrollPayout = { workerName: string; periodLabel: string; netAmount: number; currency?: string; status: string };
function downloadCsv(filename: string, rows: string[][]) {
  const content = spreadsheetCsv(rows);
  const anchor = document.createElement("a");
  anchor.href = URL.createObjectURL(
    new Blob([content], { type: "text/csv;charset=utf-8" }),
  );
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(anchor.href);
}
export default function ReportsPage() {
  const [students, setStudents] = useState<Student[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [enrolments, setEnrolments] = useState<Enrollment[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [attendance, setAttendance] = useState<Attendance[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [payroll, setPayroll] = useState<PayrollPayout[]>([]);
  const [detail, setDetail] = useState<"students" | "teachers" | "classes" | "attendance" | "collected" | "outstanding" | "expenses" | null>(null);
  const [message, setMessage] = useState("Loading operational reports…");
  const [loadState, setLoadState] = useState<"loading" | "ready" | "signin" | "denied" | "retry" | "empty">("loading");
  const [reload, setReload] = useState(0);
  useEffect(() => {
    let cancelled = false;
    function rejectResponse(response: Response) {
      if (![401, 403, 429].includes(response.status)) return false;
      if (response.status === 401) {
        setLoadState("signin"); setMessage("Your session has ended. Please sign in again to open reports.");
      } else if (response.status === 403) {
        setLoadState("denied"); setMessage("You do not have access to the data required for these reports. Contact your academy administrator.");
      } else {
        setLoadState("retry"); setMessage("Too many requests. Please wait a moment, then try again.");
      }
      return true;
    }
    async function load() {
      setLoadState("loading"); setMessage("Loading operational reports…");
      setDetail(null);
      try {
        const academyResponse = await academyApi("/api/academies", { cache: "no-store" });
        if (cancelled || rejectResponse(academyResponse)) return;
        if (!academyResponse.ok) throw new Error();
        const academies: Academy[] = await academyResponse.json();
        if (cancelled) return;
        if (!academies[0]) { setLoadState("empty"); setMessage("Create your academy first."); return; }
        const id = academies[0].id;
        const responses = await Promise.all([
          "students", "teachers", "batches", "enrollments", "sessions", "invoices", "payments", "expenses", "payroll/payouts",
        ].map(path => academyApi(`/api/academies/${id}/${path}`, { cache: "no-store" })));
        if (cancelled) return;
        for (const response of responses) {
          if (rejectResponse(response)) return;
          if (!response.ok) throw new Error();
        }
        const [studentData, teacherData, batchData, enrollmentData, sessionData, invoiceData, paymentData, expenseData, payrollData] = await Promise.all(responses.map(response => response.json()));
        if (cancelled) return;
        const attendanceResponses = await Promise.all((sessionData as Session[]).map(session =>
          academyApi(`/api/academies/${id}/sessions/${session.id}/attendance`, { cache: "no-store" })));
        if (cancelled) return;
        for (const response of attendanceResponses) {
          if (rejectResponse(response)) return;
          if (!response.ok) throw new Error();
        }
        const attendanceLists: Attendance[][] = await Promise.all(attendanceResponses.map(response => response.json()));
        if (cancelled) return;
        setStudents(studentData); setTeachers(teacherData); setBatches(batchData); setEnrolments(enrollmentData);
        setSessions(sessionData); setInvoices(invoiceData); setPayments(paymentData); setExpenses(expenseData);
        setPayroll(payrollData); setAttendance(attendanceLists.flat()); setMessage(""); setLoadState("ready");
      } catch {
        if (cancelled) return;
        setLoadState("retry"); setMessage("Reports could not be loaded. Check your connection and try again.");
      }
    }
    void load();
    return () => { cancelled = true; };
  }, [reload]);
  const activeEnrolments = enrolments.filter(
    (item) => item.status === "Active",
  );
  const activeStudents = students.filter((student) => student.isActive);
  const activeTeachers = teachers.filter((teacher) => teacher.isActive);
  const scheduledClasses = sessions.filter((session) => session.status !== "Cancelled");
  const collectedPayments = payments.filter(payment => payment.status === "Completed" || payment.status === "Reconciled");
  const collectedRows = collectedPayments.map(payment => ({ amount: payment.amount, currency: payment.currency }));
  const invoicedRows = invoices.map(invoice => ({ amount: invoice.totalAmount, currency: invoice.currency }));
  const expenseRows = expenses.map(expense => ({ amount: expense.amount, currency: expense.currency }));
  const outstandingRows = invoices.map(invoice => ({ amount: invoice.status.toLowerCase() === "cancelled" ? 0 : invoice.balance, currency: invoice.currency }));
  const outstandingInvoices = invoices.filter(invoice => invoice.status.toLowerCase() !== "cancelled" && Number.isFinite(invoice.balance) && invoice.balance > 0);
  const outstandingPayroll = payroll.filter(payout => payout.status !== "Paid");
  const attendanceRate = attendance.length
    ? Math.round(
        (attendance.filter((record) =>
          ["Present", "Late", "Online"].includes(record.status),
        ).length /
          attendance.length) *
          100,
      )
    : 0;
  const batchRows = useMemo(
    () =>
      batches.map((batch) => [
        batch.name,
        String(
          activeEnrolments.filter((item) => item.batchId === batch.id).length,
        ),
        String(batch.capacity),
      ]),
    [batches, activeEnrolments],
  );
  if (loadState !== "ready") return <main className="enterprise-settings workspace-reports">
    <h1>Reports</h1><p role={loadState === "loading" || loadState === "empty" ? "status" : "alert"}>{message}</p>
    {loadState === "retry" && <button type="button" onClick={() => setReload(value => value + 1)}>Try again</button>}
    {loadState === "signin" && <a href="/login?returnTo=/reports">Sign in again</a>}
    {loadState === "denied" && <a href="/login">Use another account</a>}
  </main>;
  const invoiceCsvRows = (): string[][] => [
    ["Invoice", "Student", "Amount", "Currency", "Due date"],
    ...invoices.map(invoice => [invoice.invoiceNumber, studentName(students.find(student => student.id === invoice.studentId)),
      String(invoice.totalAmount), reportCurrency(invoice.currency), invoice.dueDate]),
  ];
  const metrics = [
    ["Active students", activeStudents.length, "students"] as const,
    ["Teachers", activeTeachers.length, "teachers"] as const,
    ["Scheduled classes", scheduledClasses.length, "classes"] as const,
    ["Attendance rate", `${attendanceRate}%`, "attendance"] as const,
    ["Collected", reportMoney(collectedRows), "collected"] as const,
    ["Outstanding", reportMoney(outstandingRows), "outstanding"] as const,
    ["Expenses", reportMoney(expenseRows), "expenses"] as const,
  ];
  return (
    <main className="enterprise-settings workspace-reports">
      <header className="workspace-reports-heading">
        <div className="workspace-reports-title">
          <span className="workspace-reports-title-icon" aria-hidden="true">▤</span>
          <div><p>Workspace</p><h1>Reports</h1><span>Academy operations, capacity, attendance, and financial performance.</span></div>
        </div>
      </header>
        {message && (
          <p className="enterprise-page-state enterprise-page-state-loading workspace-reports-message">
            {message}
          </p>
        )}
        <section className="workspace-reports-kpis" aria-label="Operational metrics">
          {metrics.map(([label, value, key]) => <StandardInteractiveTile key={label} label={label} value={value} detail="View details" onClick={() => setDetail(key)} className="workspace-reports-kpi" />)}
          <article className="workspace-reports-kpi"><span>Collected less recorded expenses</span><strong>{reportDifference(collectedRows, expenseRows)}</strong></article>
        </section>
        {detail === "students" && <StandardDetailModal eyebrow="Workspace reports" title="Active students" onClose={() => setDetail(null)}><ReportDetailList rows={activeStudents.map((student) => [studentName(student), "Active student"])} empty="No active students." /></StandardDetailModal>}
        {detail === "teachers" && <StandardDetailModal eyebrow="Workspace reports" title="Teachers" onClose={() => setDetail(null)}><ReportDetailList rows={activeTeachers.map((teacher) => [teacherName(teacher), "Active teacher"])} empty="No active teachers." /></StandardDetailModal>}
        {detail === "classes" && <StandardDetailModal eyebrow="Workspace reports" title="Scheduled classes" onClose={() => setDetail(null)}><ReportDetailList rows={scheduledClasses.map((session) => [batches.find((batch) => batch.id === session.batchId)?.name ?? "Class", `${reportDate(session.startUtc)} · ${session.status}`])} empty="No scheduled classes." /></StandardDetailModal>}
        {detail === "attendance" && <StandardDetailModal eyebrow="Workspace reports" title="Attendance by student" onClose={() => setDetail(null)}><ReportDetailList rows={activeStudents.map((student) => { const records = attendance.filter((record) => record.studentId === student.id); const present = records.filter((record) => ["Present", "Late", "Online"].includes(record.status)).length; return [studentName(student), records.length ? `${Math.round(present * 100 / records.length)}% · ${present} of ${records.length} classes` : "No attendance recorded"]; })} empty="No active students." /></StandardDetailModal>}
        {detail === "collected" && <StandardDetailModal eyebrow="Workspace reports" title="Collected fee payments" onClose={() => setDetail(null)}><ReportDetailList rows={collectedPayments.map((payment) => { const invoice = invoices.find((item) => item.id === payment.invoiceId); return [studentName(students.find((student) => student.id === invoice?.studentId)), `${reportAmount(payment.amount, payment.currency)} paid · ${invoice?.invoiceNumber ?? "Invoice"}`]; })} empty="No completed or reconciled fee payments." /></StandardDetailModal>}
        {detail === "outstanding" && <StandardDetailModal eyebrow="Workspace reports" title="Student fees and payroll records" onClose={() => setDetail(null)}><div className="standard-detail-group"><h3>Student fee payments</h3><ReportDetailList rows={outstandingInvoices.map((invoice) => [studentName(students.find((student) => student.id === invoice.studentId)), `${invoice.invoiceNumber} · ${reportAmount(invoice.balance, invoice.currency)} due`])} empty="No student fees are outstanding." /><h3>Payroll records not marked Paid</h3><ReportDetailList rows={outstandingPayroll.map((payout) => [payout.workerName, `${payout.periodLabel} · ${reportAmount(payout.netAmount, payout.currency)} · ${payout.status}`])} empty="No payroll records are listed." /></div></StandardDetailModal>}
        {detail === "expenses" && <StandardDetailModal eyebrow="Workspace reports" title="Expenses" onClose={() => setDetail(null)}><ReportDetailList rows={expenses.map((expense) => [expense.description, `${expense.category} · ${reportAmount(expense.amount, expense.currency)} · ${expense.expenseDate}`])} empty="No expenses recorded." /></StandardDetailModal>}
        <section className="workspace-reports-grid">
          <section className="workspace-reports-panel">
            <header className="workspace-reports-panel-header">
              <div><p>Capacity</p><h2>Batch occupancy</h2></div>
              <button
                onClick={() =>
                  downloadCsv("academydesk-batch-occupancy.csv", [
                    ["Batch", "Active enrolments", "Capacity"],
                    ...batchRows,
                  ])
                }
                className="workspace-reports-secondary-action"
              >
                Download CSV
              </button>
            </header>
            {batches.length === 0 ? (
              <p className="workspace-reports-empty">No batches yet.</p>
            ) : (
              <ul className="workspace-reports-list">
                {batches.map((batch) => (
                  <li
                    key={batch.id}
                >
                    <strong>{batch.name}</strong>
                    <small>
                      {
                        activeEnrolments.filter(
                          (item) => item.batchId === batch.id,
                        ).length
                      }{" "}
                      of {batch.capacity} places filled
                    </small>
                  </li>
                ))}
              </ul>
            )}
          </section>
          <section className="workspace-reports-panel">
            <header className="workspace-reports-panel-header">
              <div><p>Finance</p><h2>Finance export</h2></div>
              <button
                onClick={() =>
                  downloadCsv("academydesk-invoices.csv", invoiceCsvRows())
                }
                className="workspace-reports-secondary-action"
              >
                Download CSV
              </button>
            </header>
            <div className="workspace-reports-finance-summary">
              {invoices.length} invoices · {reportMoney(invoicedRows)} issued ·{" "}
              {reportMoney(collectedRows)} collected
            </div>
          </section>
        </section>
        <section className="workspace-reports-export-panel">
          <header className="workspace-reports-panel-header">
            <div><p>Downloads</p><h2>Export centre</h2></div>
            <span>
              CSV downloads for operational use
            </span>
          </header>
          <div className="workspace-reports-export-grid">
            <ExportCard
              title="Students"
              detail="Student contact register"
              onClick={() =>
                downloadCsv("academydesk-students.csv", [
                  ["First name", "Last name"],
                  ...students.map((student) => [
                    student.firstName,
                    student.lastName,
                  ]),
                ])
              }
            />
            <ExportCard
              title="Batches"
              detail="Capacity and active enrolments"
              onClick={() =>
                downloadCsv("academydesk-batches.csv", [
                  ["Batch", "Active enrolments", "Capacity"],
                  ...batchRows,
                ])
              }
            />
            <ExportCard
              title="Invoices"
              detail="Issued fees and due dates"
              onClick={() =>
                downloadCsv("academydesk-invoices.csv", invoiceCsvRows())
              }
            />
            <ExportCard
              title="Expenses"
              detail="Expense ledger totals"
              onClick={() =>
                downloadCsv("academydesk-expenses.csv", [
                  ["Expense amount", "Currency"],
                  ...expenses.map((expense) => [String(expense.amount), reportCurrency(expense.currency)]),
                ])
              }
            />
          </div>
        </section>
    </main>
  );
}
function ExportCard({
  title,
  detail,
  onClick,
}: {
  title: string;
  detail: string;
  onClick: () => void;
}) {
  return (
    <article className="workspace-reports-export-card">
      <h3>{title}</h3>
      <p>{detail}</p>
      <button
        onClick={onClick}
        className="workspace-reports-secondary-action"
      >
        Download CSV →
      </button>
    </article>
  );
}
function ReportDetailList({ rows, empty }: { rows: [string, string][]; empty: string }) {
  return rows.length ? <div className="standard-detail-list">{rows.map(([title, detail], index) => <article key={`${title}-${index}`}><b>{title}</b><small>{detail}</small></article>)}</div> : <p className="standard-detail-empty">{empty}</p>;
}
function studentName(student?: Student) { return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; }
function teacherName(teacher: Teacher) { return `${teacher.firstName} ${teacher.lastName}`; }
function reportDate(value: string) { return new Intl.DateTimeFormat("en-IN", { day: "numeric", month: "short", year: "numeric", timeZone: "Asia/Kolkata" }).format(new Date(value)); }
