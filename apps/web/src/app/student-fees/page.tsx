"use client";

import { useEffect, useState } from "react";
import { StudentFeeArrangements } from "@/components/student-fee-arrangements";
import { AdmissionFeeEditor } from "@/components/admission-fee-editor";
import { academyApi } from "@/lib/api";
import { StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string; name: string };
type Student = { id: string; firstName: string; lastName: string; isActive: boolean };

export default function StudentFeesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [studentId, setStudentId] = useState("");
  const [message, setMessage] = useState("Loading student fee details…");

  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const academies = await academyApi("/api/academies", { cache: "no-store" });
        if (!academies.ok) throw new Error();
        const current = (await academies.json())[0] as Academy | undefined;
        if (!current) { if (active) setMessage("Create an academy before managing student fees."); return; }
        const response = await academyApi(`/api/academies/${current.id}/students`, { cache: "no-store" });
        if (!response.ok) throw new Error();
        const rows = (await response.json()) as Student[];
        if (!Array.isArray(rows)) throw new Error();
        if (!active) return;
        setAcademy(current);
        setStudents(rows);
        setStudentId(rows.find((student) => student.isActive)?.id ?? rows[0]?.id ?? "");
        setMessage("");
      } catch {
        if (active) setMessage("Student fee details could not be loaded.");
      }
    })();
    return () => { active = false; };
  }, []);

  return (
    <main className="enterprise-settings student-fees-standard">
      <header className="student-fees-heading">
        <div className="student-fees-title">
          <span className="student-fees-title-icon" aria-hidden="true">₹</span>
          <div><p>Students</p><h1>Fee Details</h1><span>Maintain subject fees, admission fees, and billing frequency for each student.</span></div>
        </div>
      </header>
      {message ? <p role="status" aria-live="polite" className="enterprise-page-state student-fees-message">{message}</p> : null}
      {academy ? (
        <>
          <section className="student-fees-toolbar">
            <StandardSelectField
              name="fee-student"
              value={studentId}
              onChange={setStudentId}
              placeholder="Select student"
              options={students.map((student) => ({ value: student.id, label: `${student.firstName} ${student.lastName}${student.isActive ? "" : " (Inactive)"}` }))}
            />
          </section>
          {!students.length ? <p className="enterprise-settings-empty student-fees-empty">No students are available yet.</p> : null}
          <section className="student-fees-layout">
          {studentId ? (
            <div className="student-fees-workspace">
              <AdmissionFeeEditor academyId={academy.id} studentId={studentId} />
              <StudentFeeArrangements academyId={academy.id} studentId={studentId} />
            </div>
          ) : null}
          </section>
        </>
      ) : null}
    </main>
  );
}
