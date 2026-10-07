"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
import { StudentAdminProfile } from "@/components/student-admin-profile";
import { StudentFeeArrangements } from "@/components/student-fee-arrangements";
import { StandardSelectField } from "@/components/design-system/controls";
import { StandardDetailModal, StandardInteractiveTile } from "@/components/design-system/interactive";

type Student = {
  id: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  isActive: boolean;
};
type Profile = {
  studentNumber?: string;
  preferredName?: string;
  gender?: string;
  dateOfBirth?: string;
  admissionDate?: string;
  addressLine1?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  medicalOrAccessibilityNotes?: string;
  adminNotes?: string;
  guardians?: Guardian[];
  enrollments?: Enrollment[];
  attendance?: StatusCount[];
  currentMonthAttendance?: StatusCount[];
  invoices?: Invoice[];
  musicProgress?: unknown[];
  practiceLogs?: unknown[];
  communications?: unknown[];
};

type Guardian = { id: string; name: string; email?: string | null; phone?: string | null; relationship?: string | null };
type Enrollment = { batchName: string; courseName: string; status: string; startDate: string; endDate?: string | null };
type StatusCount = { status: string; count: number };
type Invoice = { invoiceNumber: string; totalAmount: number; adjustedAmount?: number; currency: string; dueDate: string; status: string; subjectName?: string | null; paidAmount?: number; lastPaidAtUtc?: string | null };
type StudentDetail = "enrolments" | "attendance" | "fees" | "family";

const count = (items?: unknown[]) => items?.length ?? 0;
function DetailPanel({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section className="surface-panel rounded-xl p-5">
      <header><h3 className="font-semibold">{title}</h3></header>
      <div className="mt-4 text-sm text-slate-300">{children}</div>
    </section>
  );
}

function formatDate(value?: string | null) {
  if (!value) return "Not recorded";
  return new Intl.DateTimeFormat("en-IN", { day: "numeric", month: "short", year: "numeric" }).format(new Date(value));
}

function formatAttendance(items?: StatusCount[]) {
  return (items ?? []).map((item) => `${item.status}: ${item.count}`).join(" · ") || "No attendance recorded";
}

function attendanceCount(items?: StatusCount[]) {
  return (items ?? []).reduce((total, item) => total + item.count, 0);
}

function StudentDetailList({ rows, empty }: { rows: [string, string][]; empty: string }) {
  if (!rows.length) return <p className="standard-detail-empty">{empty}</p>;
  return <div className="standard-detail-list">{rows.map(([title, detail], index) => <div key={`${title}-${index}`}><strong>{title}</strong><span>{detail}</span></div>)}</div>;
}

export default function StudentProfilePage() {
  const search = useSearchParams();
  const requested = search.get("studentId") ?? "";
  const createdNotice = search.get("notice") === "student-created";
  const [academyId, setAcademyId] = useState("");
  const [students, setStudents] = useState<Student[]>([]);
  const [studentId, setStudentId] = useState(requested);
  const [profile, setProfile] = useState<Profile>();
  const [message, setMessage] = useState("Loading student records…");
  const [detail, setDetail] = useState<StudentDetail | null>(null);
  useEffect(() => {
    void (async () => {
      try {
        const academies = await academyApi("/api/academies");
        const academy = (await academies.json())[0];
        if (!academy)
          return setMessage(
            "Create an academy before opening student records.",
          );
        setAcademyId(academy.id);
        const response = await academyApi(
          `/api/academies/${academy.id}/students`,
        );
        const rows: Student[] = await response.json();
        setStudents(rows);
        const selected = requested || rows[0]?.id || "";
        setStudentId(selected);
        if (selected) {
          const detail = await academyApi(
            `/api/academies/${academy.id}/students/${selected}/profile`,
          );
          if (detail.ok) setProfile(await detail.json());
        }
        setMessage("");
      } catch {
        setMessage("Student records could not be loaded.");
      }
    })();
  }, [requested]);
  async function select(id: string) {
    setStudentId(id);
    setDetail(null);
    if (!academyId || !id) return;
    const response = await academyApi(
      `/api/academies/${academyId}/students/${id}/profile`,
    );
    if (response.ok) setProfile(await response.json());
  }
  const student = students.find((item) => item.id === studentId);
  const balance = (item: Invoice) => item.status === "Cancelled" ? 0 : Math.max(0, item.totalAmount - (item.adjustedAmount ?? 0) - (item.paidAmount ?? 0));
  const outstanding = (profile?.invoices ?? [])
    .reduce((sum, item) => sum + balance(item), 0);
  const today = new Date().toISOString().slice(0, 10);
  const upcoming = (profile?.invoices ?? [])
    .filter((item) => balance(item) > 0 && item.dueDate >= today)
    .reduce((sum, item) => sum + balance(item), 0);
  const overdue = (profile?.invoices ?? [])
    .filter((item) => balance(item) > 0 && item.dueDate < today)
    .reduce((sum, item) => sum + balance(item), 0);
  return (
    <main className="enterprise-settings student-360-standard">
      <header className="student-360-heading">
        <div className="student-360-title">
          <span className="student-360-title-icon" aria-hidden="true">◎</span>
          <div><p>Students</p><h1>
          {student
            ? `${student.firstName} ${student.lastName}`
            : "360"}
          </h1><span>A connected record for family, enrolment, attendance, finance, learning, and communication.</span></div>
        </div>
      </header>
      <section className="student-360-toolbar">
        <StandardSelectField
          name="student-record"
          value={studentId}
          onChange={(value) => void select(value)}
          placeholder="Select student"
          options={students.map((item) => ({ value: item.id, label: `${item.firstName} ${item.lastName}` }))}
        />
        <div className="enterprise-page-actions">
          <Link
            href={`/enrollments?studentId=${studentId}`}
            className="enterprise-action-button"
          >
            Enrol student
          </Link>
          <Link href="/students" className="enterprise-action-button enterprise-action-button-secondary">
            Student management
          </Link>
        </div>
      </section>
      {createdNotice && <p className="enterprise-page-state student-360-message" role="status">Student created successfully. You can now enrol the student, add fees, or update their profile.</p>}
      {message && <p className="enterprise-page-state student-360-message">{message}</p>}
      {student && profile && (
        <>
          <section className="student-360-summary-grid">
            <StandardInteractiveTile className="student-summary-tile" label="Enrolments" value={count(profile.enrollments)} detail={profile.enrollments?.map((item) => item.courseName).filter((value, index, values) => values.indexOf(value) === index).join(" · ") || "No enrolled subjects"} onClick={() => setDetail("enrolments")} />
            <StandardInteractiveTile className="student-summary-tile" label="Attendance" value={attendanceCount(profile.attendance)} detail={formatAttendance(profile.currentMonthAttendance) + " this month"} onClick={() => setDetail("attendance")} />
            <StandardInteractiveTile className="student-summary-tile" label="Fees" value={`₹${outstanding.toLocaleString("en-IN")}`} detail={`₹${upcoming.toLocaleString("en-IN")} upcoming · ₹${overdue.toLocaleString("en-IN")} overdue`} onClick={() => setDetail("fees")} />
            <StandardInteractiveTile className="student-summary-tile" label="Family" value={count(profile.guardians)} detail="View parent details" onClick={() => setDetail("family")} />
          </section>
          <section className="student-360-detail-grid">
            <StudentAdminProfile
              academyId={academyId}
              studentId={studentId}
              profile={profile}
              onSaved={(value) =>
                setProfile((current) => ({ ...current, ...value }))
              }
            />
            <div className="space-y-5">
              <StudentFeeArrangements
                academyId={academyId}
                studentId={studentId}
              />
              <DetailPanel title="Learning progress">
                <p>{count(profile.musicProgress)} music progress records</p>
                <p className="mt-2">
                  {count(profile.practiceLogs)} practice or study logs
                </p>
                <Link
                  href="/assignments"
                  className="enterprise-action-button enterprise-action-button-secondary mt-4"
                >
                  Review assignments and submissions
                </Link>
              </DetailPanel>
              <DetailPanel title="Engagement">
                <p>{count(profile.communications)} communication records</p>
                <p className="mt-2 text-slate-400">
                  Use contact preferences before sending reminders or
                  announcements.
                </p>
                <Link
                  href="/communication-preferences"
                  className="enterprise-action-button enterprise-action-button-secondary mt-4"
                >
                  Contact preferences
                </Link>
              </DetailPanel>
            </div>
          </section>
          {detail === "enrolments" && <StandardDetailModal title="Enrolled subjects" eyebrow="Student 360" onClose={() => setDetail(null)}><div className="standard-detail-content"><StudentDetailList rows={(profile.enrollments ?? []).map((item) => [item.courseName, `${item.batchName} · ${item.status} · Started ${formatDate(item.startDate)}`])} empty="This student is not enrolled in any subject." /></div></StandardDetailModal>}
          {detail === "attendance" && <StandardDetailModal title="Attendance" eyebrow="Student 360" onClose={() => setDetail(null)}><div className="standard-detail-content"><section className="standard-detail-group"><h3>All recorded attendance</h3><StudentDetailList rows={(profile.attendance ?? []).map((item) => [item.status, `${item.count} class${item.count === 1 ? "" : "es"}`])} empty="No attendance has been recorded for this student." /></section><section className="standard-detail-group"><h3>This month</h3><StudentDetailList rows={(profile.currentMonthAttendance ?? []).map((item) => [item.status, `${item.count} class${item.count === 1 ? "" : "es"}`])} empty="No attendance has been recorded this month." /></section></div></StandardDetailModal>}
          {detail === "fees" && <StandardDetailModal title="Fee payments" eyebrow="Student 360" onClose={() => setDetail(null)}><div className="standard-detail-content"><StudentDetailList rows={(profile.invoices ?? []).map((item) => { const remaining = balance(item); return [item.invoiceNumber, `${item.subjectName || "General fee"} · ${item.status} · ${item.currency} ${item.paidAmount ?? 0} paid · ${remaining > 0 ? `${item.currency} ${remaining} due` : "Paid in full"} · Due ${formatDate(item.dueDate)}${item.lastPaidAtUtc ? ` · Last payment ${formatDate(item.lastPaidAtUtc)}` : ""}`]; })} empty="No fee payments or invoices are recorded for this student." /></div></StandardDetailModal>}
          {detail === "family" && <StandardDetailModal title="Family" eyebrow="Student 360" onClose={() => setDetail(null)}><div className="standard-detail-content"><StudentDetailList rows={(profile.guardians ?? []).map((guardian) => [guardian.name, `${guardian.relationship || "Parent"}${guardian.email ? ` · ${guardian.email}` : ""}${guardian.phone ? ` · ${guardian.phone}` : ""}`])} empty="No parent or guardian is linked to this student." /></div></StandardDetailModal>}
        </>
      )}
    </main>
  );
}
