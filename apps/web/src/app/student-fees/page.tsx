"use client";

import { FormEvent, useEffect, useState } from "react";
import { StudentFeeArrangements } from "@/components/student-fee-arrangements";
import { academyApi, apiHeaders } from "@/lib/api";

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
          {studentId ? (
            <div className="space-y-5">
              <form onSubmit={saveAdmissionFee} className="surface-panel rounded-xl p-5">
                <header className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <h3 className="font-semibold">Admission fee</h3>
                    <p className="mt-1 text-sm text-slate-400">One-time fee charged when the student joins the academy.</p>
                  </div>
                  <button disabled={savingAdmission} className="rounded bg-cyan-400 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-50">
                    {savingAdmission ? "Saving…" : "Save admission fee"}
                  </button>
                </header>
                <div className="mt-4 grid gap-3 sm:grid-cols-2">
                  <label className="field-label">Admission fee amount<input value={admissionAmount} onChange={(event) => setAdmissionAmount(event.target.value)} type="number" min="0" step="0.01" placeholder="Optional amount" /></label>
                  <label className="field-label">Due date<input value={admissionDueDate} onChange={(event) => setAdmissionDueDate(event.target.value)} type="date" /></label>
                </div>
              </form>
              <StudentFeeArrangements academyId={academy.id} studentId={studentId} />
            </div>
          ) : null}
        </section>
      ) : null}
    </main>
  );
}
