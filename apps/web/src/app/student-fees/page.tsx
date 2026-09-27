"use client";

import { FormEvent, useEffect, useState } from "react";
import { StudentFeeArrangements } from "@/components/student-fee-arrangements";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string; name: string };
type Student = { id: string; firstName: string; lastName: string; isActive: boolean };

export default function StudentFeesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [studentId, setStudentId] = useState("");
  const [message, setMessage] = useState("Loading student fee details…");
  const [admissionAmount, setAdmissionAmount] = useState("");
  const [admissionDueDate, setAdmissionDueDate] = useState("");
  const [savingAdmission, setSavingAdmission] = useState(false);

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

  useEffect(() => {
    if (!academy || !studentId) return;
    void academyApi(`/api/academies/${academy.id}/students/${studentId}/fee-arrangements/admission-fee`)
      .then(async (response) => {
        if (!response.ok) throw new Error();
        const details = await response.json() as { amount?: number; dueDate?: string };
        setAdmissionAmount(details.amount?.toString() ?? "");
        setAdmissionDueDate(details.dueDate ?? "");
      })
      .catch(() => setMessage("Admission fee details could not be loaded."));
  }, [academy, studentId]);

  async function saveAdmissionFee(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !studentId) return;
    setSavingAdmission(true);
    const response = await academyApi(`/api/academies/${academy.id}/students/${studentId}/fee-arrangements/admission-fee`, {
      method: "PUT",
      headers: apiHeaders(true),
      body: JSON.stringify({ amount: admissionAmount ? Number(admissionAmount) : null, dueDate: admissionDueDate || null }),
    });
    setSavingAdmission(false);
    setMessage(response.ok ? "Admission fee saved." : "Enter a valid admission fee and due date.");
  }

  return (
    <main className="enterprise-settings student-fees-standard">
      <header className="student-fees-heading">
        <div className="student-fees-title">
          <span className="student-fees-title-icon" aria-hidden="true">₹</span>
          <div><p>Students</p><h1>Fee Details</h1><span>Maintain subject fees, admission fees, and billing frequency for each student.</span></div>
        </div>
      </header>
      {message ? <p className="enterprise-page-state student-fees-message">{message}</p> : null}
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
              <form onSubmit={saveAdmissionFee} className="student-fees-panel">
                <header className="student-fees-panel-header">
                  <div>
                    <p>One-time fee</p><h3>Admission fee</h3>
                  </div>
                  <button disabled={savingAdmission} className="enterprise-action-button">
                    {savingAdmission ? "Saving…" : "Save admission fee"}
                  </button>
                </header>
                <div className="mt-4 grid gap-3 sm:grid-cols-2">
                  <label className="field-label">Admission fee amount<input value={admissionAmount} onChange={(event) => setAdmissionAmount(event.target.value)} type="number" min="0" step="0.01" placeholder="Optional amount" /></label>
                  <StandardDateField name="admissionDueDateDisplay" label="Due date" value={admissionDueDate} onChange={setAdmissionDueDate} />
                </div>
              </form>
              <StudentFeeArrangements academyId={academy.id} studentId={studentId} />
            </div>
          ) : null}
          </section>
        </>
      ) : null}
    </main>
  );
}
