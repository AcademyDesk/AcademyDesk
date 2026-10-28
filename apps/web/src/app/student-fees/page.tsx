"use client";

import { FormEvent, useEffect, useState } from "react";
import { StudentFeeArrangements } from "@/components/student-fee-arrangements";
import { academyApi } from "@/lib/api";
import { useRecordEditor } from "@/lib/use-record-editor";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string; name: string };
type Student = { id: string; firstName: string; lastName: string; isActive: boolean };
type AdmissionFee = { amount?: number | null; dueDate?: string | null };
const prepare = (value: AdmissionFee) => ({ amount: value.amount?.toString() ?? "", dueDate: value.dueDate ?? "" });

export default function StudentFeesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [studentId, setStudentId] = useState("");
  const [message, setMessage] = useState("Loading student fee details…");
  const editor = useRecordEditor(academy && studentId ? `/api/academies/${academy.id}/students/${studentId}/fee-arrangements/admission-fee` : "", prepare, "Admission fee details could not be loaded.");
  const admissionAmount = editor.draft?.amount ?? "";
  const admissionDueDate = editor.draft?.dueDate ?? "";
  const savingAdmission = editor.saving;

  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const academies = await academyApi("/api/academies", { cache: "no-store" });
        if (!academies.ok) throw new Error();
        const current = (await academies.json())[0] as Academy | undefined;
        if (!active) return;
        if (!current) return setMessage("Create an academy before managing student fees.");
        setAcademy(current);
        const response = await academyApi(`/api/academies/${current.id}/students`, { cache: "no-store" });
        if (!response.ok) throw new Error();
        const rows = (await response.json()) as Student[];
        if (!active) return;
        setStudents(rows);
        setStudentId(rows.find((student) => student.isActive)?.id ?? rows[0]?.id ?? "");
        setMessage("");
      } catch {
        if (active) setMessage("Student fee details could not be loaded.");
      }
    })();
    return () => { active = false; };
  }, []);

  async function saveAdmissionFee(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await editor.save({ amount: admissionAmount ? Number(admissionAmount) : null, dueDate: admissionDueDate || null },
      "Admission fee saved.", "The admission fee could not be saved. Check the record before retrying.");
  }
  const notice = message || (editor.loading ? "Loading admission fee…" : editor.message);

  return (
    <main className="enterprise-settings student-fees-standard">
      <header className="student-fees-heading">
        <div className="student-fees-title">
          <span className="student-fees-title-icon" aria-hidden="true">₹</span>
          <div><p>Students</p><h1>Fee Details</h1><span>Maintain subject fees, admission fees, and billing frequency for each student.</span></div>
        </div>
      </header>
      {notice ? <p className="enterprise-page-state student-fees-message">{notice}</p> : null}
      {editor.failed && <button onClick={editor.reload} className="enterprise-action-button mt-3">Retry loading admission fee</button>}
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
                  <button disabled={savingAdmission || !editor.ready} className="enterprise-action-button">
                    {savingAdmission ? "Saving…" : "Save admission fee"}
                  </button>
                </header>
                <fieldset disabled={savingAdmission || !editor.ready} className="mt-4 grid gap-3 sm:grid-cols-2">
                  <label className="field-label">Admission fee amount<input value={admissionAmount} onChange={(event) => editor.update(current => ({ ...current, amount: event.target.value }))} type="number" min="0" step="0.01" placeholder="Optional amount" /></label>
                  <StandardDateField name="admissionDueDateDisplay" label="Due date" value={admissionDueDate} onChange={value => editor.update(current => ({ ...current, dueDate: value }))} />
                </fieldset>
              </form>
              <StudentFeeArrangements key={`${academy.id}/${studentId}`} academyId={academy.id} studentId={studentId} />
            </div>
          ) : null}
          </section>
        </>
      ) : null}
    </main>
  );
}
