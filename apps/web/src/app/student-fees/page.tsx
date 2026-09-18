"use client";

import { useEffect, useState } from "react";
import { StudentFeeArrangements } from "@/components/student-fee-arrangements";
import { academyApi } from "@/lib/api";

type Academy = { id: string; name: string };
type Student = { id: string; firstName: string; lastName: string; isActive: boolean };

export default function StudentFeesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [studentId, setStudentId] = useState("");
  const [message, setMessage] = useState("Loading student fee details…");

  useEffect(() => {
    void (async () => {
      try {
        const academies = await academyApi("/api/academies", { cache: "no-store" });
        const current = (await academies.json())[0] as Academy | undefined;
        if (!current) return setMessage("Create an academy before managing student fees.");
        setAcademy(current);
        const response = await academyApi(`/api/academies/${current.id}/students`, { cache: "no-store" });
        if (!response.ok) throw new Error();
        const rows = (await response.json()) as Student[];
        setStudents(rows);
        setStudentId(rows.find((student) => student.isActive)?.id ?? rows[0]?.id ?? "");
        setMessage("");
      } catch {
        setMessage("Student fee details could not be loaded.");
      }
    })();
  }, []);

  return (
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <p>Students / finance</p>
        <h2>Student fee details</h2>
        <span>Maintain each student’s subject-wise fee arrangement and billing frequency.</span>
      </header>
      {message ? <p className="enterprise-page-state">{message}</p> : null}
      {academy ? (
        <section className="mt-5 grid gap-5 xl:grid-cols-[320px_minmax(0,1fr)]">
          <section className="surface-panel rounded-xl p-5">
            <h3 className="font-semibold">Student</h3>
            <label className="field-label mt-4">
              Select student
              <select value={studentId} onChange={(event) => setStudentId(event.target.value)}>
                {students.map((student) => (
                  <option key={student.id} value={student.id}>
                    {student.firstName} {student.lastName}{student.isActive ? "" : " (Inactive)"}
                  </option>
                ))}
              </select>
            </label>
            {!students.length ? <p className="enterprise-settings-empty mt-4">No students are available yet.</p> : null}
          </section>
          {studentId ? <StudentFeeArrangements academyId={academy.id} studentId={studentId} /> : null}
        </section>
      ) : null}
    </main>
  );
}
