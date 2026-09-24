"use client";

import { useEffect, useMemo, useState } from "react";
import { academyApi } from "@/lib/api";
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
  paidAmount: number;
  dueDate: string;
};
type Payment = { invoiceId: string; amount: number; status: string };
type Expense = { description: string; amount: number; category: string; expenseDate: string };
type PayrollPayout = { workerName: string; periodLabel: string; netAmount: number; status: string };
const money = (amount: number) =>
  new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR" }).format(
    amount,
  );
function downloadCsv(filename: string, rows: string[][]) {
  const content = rows
    .map((row) =>
      row.map((value) => `"${value.replaceAll('"', '""')}"`).join(","),
    )
    .join("\r\n");
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
  useEffect(() => {
    async function load() {
      try {
        const academyResponse = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (academyResponse.status === 401)
          return setMessage("Please sign in before opening reports.");
        if (!academyResponse.ok) throw new Error();
        const academies: Academy[] = await academyResponse.json();
        if (!academies[0]) return setMessage("Create your academy first.");
        const id = academies[0].id;
        const [
          studentResponse,
          teacherResponse,
          batchResponse,
          enrollmentResponse,
          sessionResponse,
          invoiceResponse,
          paymentResponse,
          expenseResponse,
          payrollResponse,
        ] = await Promise.all([
          academyApi(`/api/academies/${id}/students`),
          academyApi(`/api/academies/${id}/teachers`),
          academyApi(`/api/academies/${id}/batches`),
          academyApi(`/api/academies/${id}/enrollments`),
          academyApi(`/api/academies/${id}/sessions`),
          academyApi(`/api/academies/${id}/invoices`),
          academyApi(`/api/academies/${id}/payments`),
          academyApi(`/api/academies/${id}/expenses`),
          academyApi(`/api/academies/${id}/payroll/payouts`),
        ]);
        if (
          ![
            studentResponse,
            teacherResponse,
            batchResponse,
            enrollmentResponse,
            sessionResponse,
            invoiceResponse,
            paymentResponse,
            expenseResponse,
            payrollResponse,
          ].every((response) => response.ok)
        )
          throw new Error();
        const sessionData: Session[] = await sessionResponse.json();
        setStudents(await studentResponse.json());
        setTeachers(await teacherResponse.json());
        setBatches(await batchResponse.json());
        setEnrolments(await enrollmentResponse.json());
        setSessions(sessionData);
        setInvoices(await invoiceResponse.json());
        setPayments(await paymentResponse.json());
        setExpenses(await expenseResponse.json());
        setPayroll(await payrollResponse.json());
        const attendanceLists = await Promise.all(
          sessionData.map(async (session) => {
            const response = await academyApi(
              `/api/academies/${id}/sessions/${session.id}/attendance`,
            );
            return response.ok ? ((await response.json()) as Attendance[]) : [];
          }),
        );
        setAttendance(attendanceLists.flat());
        setMessage("");
      } catch {
        setMessage(
          "Reports could not be loaded. Confirm the API is running on port 5092.",
        );
      }
    }
    void load();
  }, []);
  const activeEnrolments = enrolments.filter(
    (item) => item.status === "Active",
  );
  const activeStudents = students.filter((student) => student.isActive);
  const activeTeachers = teachers.filter((teacher) => teacher.isActive);
  const scheduledClasses = sessions.filter((session) => session.status !== "Cancelled");
  const collected = payments
    .filter((payment) => payment.status === "Completed")
    .reduce((sum, payment) => sum + payment.amount, 0);
  const invoiced = invoices.reduce(
    (sum, invoice) => sum + invoice.totalAmount,
    0,
  );
  const spent = expenses.reduce((sum, expense) => sum + expense.amount, 0);
  const outstandingInvoices = invoices.filter((invoice) => invoice.totalAmount - invoice.paidAmount > 0);
  const outstandingPayroll = payroll.filter((payout) => payout.status !== "Paid");
  const payrollDue = outstandingPayroll.reduce((sum, payout) => sum + payout.netAmount, 0);
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
  const metrics = [
    ["Active students", activeStudents.length, "students"] as const,
    ["Teachers", activeTeachers.length, "teachers"] as const,
    ["Scheduled classes", scheduledClasses.length, "classes"] as const,
    ["Attendance rate", `${attendanceRate}%`, "attendance"] as const,
    ["Collected", money(collected), "collected"] as const,
    ["Outstanding", money(Math.max(0, invoiced - collected) + payrollDue), "outstanding"] as const,
    ["Expenses", money(spent), "expenses"] as const,
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
          <article className="workspace-reports-kpi"><span>Net cash</span><strong>{money(collected - spent)}</strong></article>
        </section>
        {detail === "students" && <StandardDetailModal eyebrow="Workspace reports" title="Active students" onClose={() => setDetail(null)}><ReportDetailList rows={activeStudents.map((student) => [studentName(student), "Active student"])} empty="No active students." /></StandardDetailModal>}
        {detail === "teachers" && <StandardDetailModal eyebrow="Workspace reports" title="Teachers" onClose={() => setDetail(null)}><ReportDetailList rows={activeTeachers.map((teacher) => [teacherName(teacher), "Active teacher"])} empty="No active teachers." /></StandardDetailModal>}
        {detail === "classes" && <StandardDetailModal eyebrow="Workspace reports" title="Scheduled classes" onClose={() => setDetail(null)}><ReportDetailList rows={scheduledClasses.map((session) => [batches.find((batch) => batch.id === session.batchId)?.name ?? "Class", `${reportDate(session.startUtc)} · ${session.status}`])} empty="No scheduled classes." /></StandardDetailModal>}
        {detail === "attendance" && <StandardDetailModal eyebrow="Workspace reports" title="Attendance by student" onClose={() => setDetail(null)}><ReportDetailList rows={activeStudents.map((student) => { const records = attendance.filter((record) => record.studentId === student.id); const present = records.filter((record) => ["Present", "Late", "Online"].includes(record.status)).length; return [studentName(student), records.length ? `${Math.round(present * 100 / records.length)}% · ${present} of ${records.length} classes` : "No attendance recorded"]; })} empty="No active students." /></StandardDetailModal>}
        {detail === "collected" && <StandardDetailModal eyebrow="Workspace reports" title="Collected fee payments" onClose={() => setDetail(null)}><ReportDetailList rows={payments.filter((payment) => payment.status === "Completed").map((payment) => { const invoice = invoices.find((item) => item.id === payment.invoiceId); return [studentName(students.find((student) => student.id === invoice?.studentId)), `${money(payment.amount)} paid · ${invoice?.invoiceNumber ?? "Invoice"}`]; })} empty="No completed fee payments." /></StandardDetailModal>}
        {detail === "outstanding" && <StandardDetailModal eyebrow="Workspace reports" title="Outstanding fees and salary" onClose={() => setDetail(null)}><div className="standard-detail-group"><h3>Student fee payments</h3><ReportDetailList rows={outstandingInvoices.map((invoice) => [studentName(students.find((student) => student.id === invoice.studentId)), `${invoice.invoiceNumber} · ${money(invoice.totalAmount - invoice.paidAmount)} due`])} empty="No student fees are outstanding." /><h3>Teacher salary payments</h3><ReportDetailList rows={outstandingPayroll.map((payout) => [payout.workerName, `${payout.periodLabel} · ${money(payout.netAmount)} · ${payout.status}`])} empty="No teacher salary payments are outstanding." /></div></StandardDetailModal>}
        {detail === "expenses" && <StandardDetailModal eyebrow="Workspace reports" title="Expenses" onClose={() => setDetail(null)}><ReportDetailList rows={expenses.map((expense) => [expense.description, `${expense.category} · ${money(expense.amount)} · ${expense.expenseDate}`])} empty="No expenses recorded." /></StandardDetailModal>}
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
                  downloadCsv("academydesk-invoices.csv", [
                    ["Invoice", "Student", "Amount", "Due date"],
                    ...invoices.map((invoice) => {
                      const student = students.find(
                        (item) => item.id === invoice.studentId,
                      );
                      return [
                        invoice.invoiceNumber,
                        student
                          ? `${student.firstName} ${student.lastName}`
                          : "Unknown",
                        String(invoice.totalAmount),
                        invoice.dueDate,
                      ];
                    }),
                  ])
                }
                className="workspace-reports-secondary-action"
              >
                Download CSV
              </button>
            </header>
            <div className="workspace-reports-finance-summary">
              {invoices.length} invoices · {money(invoiced)} issued ·{" "}
              {money(collected)} collected
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
                downloadCsv("academydesk-invoices.csv", [
                  ["Invoice", "Student", "Amount", "Due date"],
                  ...invoices.map((invoice) => {
                    const student = students.find(
                      (item) => item.id === invoice.studentId,
                    );
                    return [
                      invoice.invoiceNumber,
                      student
                        ? `${student.firstName} ${student.lastName}`
                        : "Unknown",
                      String(invoice.totalAmount),
                      invoice.dueDate,
                    ];
                  }),
                ])
              }
            />
            <ExportCard
              title="Expenses"
              detail="Expense ledger totals"
              onClick={() =>
                downloadCsv("academydesk-expenses.csv", [
                  ["Expense amount"],
                  ...expenses.map((expense) => [String(expense.amount)]),
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
