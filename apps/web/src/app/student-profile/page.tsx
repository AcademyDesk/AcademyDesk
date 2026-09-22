"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
import { StudentAdminProfile } from "@/components/student-admin-profile";
import { StudentFeeArrangements } from "@/components/student-fee-arrangements";

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
  guardians?: unknown[];
  enrollments?: unknown[];
  attendance?: unknown[];
  invoices?: { totalAmount?: number; status?: string; dueDate?: string }[];
  musicProgress?: unknown[];
  practiceLogs?: unknown[];
  communications?: unknown[];
};

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

function StudentSummaryTile({
  title,
  href,
  children,
}: {
  title: string;
  href: string;
  children: React.ReactNode;
}) {
  return (
    <Link href={href} className="student-summary-tile" aria-label={`Open ${title}`}>
      <h3>{title}</h3>
      <div>{children}</div>
    </Link>
  );
}

export default function StudentProfilePage() {
  const requested =
    typeof window === "undefined"
      ? ""
      : (new URLSearchParams(window.location.search).get("studentId") ?? "");
  const [academyId, setAcademyId] = useState("");
  const [students, setStudents] = useState<Student[]>([]);
  const [studentId, setStudentId] = useState(requested);
  const [profile, setProfile] = useState<Profile>();
  const [message, setMessage] = useState("Loading student records…");
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
    if (!academyId || !id) return;
    const response = await academyApi(
      `/api/academies/${academyId}/students/${id}/profile`,
    );
    if (response.ok) setProfile(await response.json());
  }
  const student = students.find((item) => item.id === studentId);
  const outstanding = (profile?.invoices ?? [])
    .filter((item) => item.status !== "Paid")
    .reduce((sum, item) => sum + (item.totalAmount ?? 0), 0);
  const today = new Date().toISOString().slice(0, 10);
  const upcoming = (profile?.invoices ?? [])
    .filter((item) => item.status !== "Paid" && (item.dueDate ?? "") >= today)
    .reduce((sum, item) => sum + (item.totalAmount ?? 0), 0);
  const overdue = (profile?.invoices ?? [])
    .filter((item) => item.status !== "Paid" && (item.dueDate ?? "") < today)
    .reduce((sum, item) => sum + (item.totalAmount ?? 0), 0);
  return (
    <main className="enterprise-settings student-360-standard">
      <header className="student-360-heading">
        <div className="student-360-title">
          <span className="student-360-title-icon" aria-hidden="true">◎</span>
          <div><p>Students</p><h1>
          {student
            ? `${student.firstName} ${student.lastName}`
            : "Student record"}
          </h1><span>A connected record for family, enrolment, attendance, finance, learning, and communication.</span></div>
        </div>
      </header>
      <section className="student-360-toolbar">
        <select
          value={studentId}
          onChange={(event) => void select(event.target.value)}
          className="rounded border border-slate-700 bg-slate-950 p-2"
        >
          <option value="">Select student</option>
          {students.map((item) => (
            <option key={item.id} value={item.id}>
              {item.firstName} {item.lastName}
            </option>
          ))}
        </select>
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
      {message && <p className="enterprise-page-state student-360-message">{message}</p>}
      {student && profile && (
        <>
          <section className="student-360-summary-grid">
            <StudentSummaryTile
              title="Enrolments"
              href={`/enrollments?studentId=${studentId}`}
            >
              <strong className="text-3xl">{count(profile.enrollments)}</strong>
              <p className="mt-2 text-slate-400">Active learning placements</p>
            </StudentSummaryTile>
            <StudentSummaryTile title="Attendance" href="/attendance">
              <strong className="text-3xl">{count(profile.attendance)}</strong>
              <p className="mt-2 text-slate-400">Recorded class attendance</p>
            </StudentSummaryTile>
            <StudentSummaryTile title="Fees" href="/invoices">
              <strong className="text-3xl">
                ₹{outstanding.toLocaleString("en-IN")}
              </strong>
              <p className="mt-2 text-slate-400">
                ₹{upcoming.toLocaleString("en-IN")} upcoming · ₹
                {overdue.toLocaleString("en-IN")} overdue
              </p>
            </StudentSummaryTile>
            <StudentSummaryTile title="Family" href="/guardians">
              <strong className="text-3xl">{count(profile.guardians)}</strong>
              <p className="mt-2 text-slate-400">Linked parent records</p>
            </StudentSummaryTile>
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
        </>
      )}
    </main>
  );
}
