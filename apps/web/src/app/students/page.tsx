"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string; name: string };
type Student = {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
};
type Batch = { id: string; name: string; isActive: boolean; enrollmentStatus?: string };

export default function StudentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [assignmentStudentId, setAssignmentStudentId] = useState("");
  const [assignmentBatchId, setAssignmentBatchId] = useState("");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("Active");
  const [message, setMessage] = useState("Loading student register…");
  const [saving, setSaving] = useState(false);
  const filtered = useMemo(
    () =>
      students.filter(
        (student) =>
          (status === "Active") === student.isActive &&
          `${student.firstName} ${student.lastName} ${student.email ?? ""} ${student.phone ?? ""}`
            .toLowerCase()
            .includes(query.toLowerCase()),
      ),
    [students, query, status],
  );
  async function load() {
    const response = await academyApi("/api/academies", { cache: "no-store" });
    if (!response.ok) throw new Error();
    const academies: Academy[] = await response.json();
    const current = academies[0];
    if (!current) {
      setMessage("Create an academy before managing students.");
      return;
    }
    setAcademy(current);
    const [roster, batchResponse] = await Promise.all([
      academyApi(`/api/academies/${current.id}/students`, { cache: "no-store" }),
      academyApi(`/api/academies/${current.id}/batches`, { cache: "no-store" }),
    ]);
    if (!roster.ok || !batchResponse.ok) throw new Error();
    setStudents(await roster.json());
    setBatches(await batchResponse.json());
    setMessage("");
  }
  useEffect(() => {
    void load().catch(() =>
      setMessage("The student register could not be loaded."),
    );
  }, []);
  async function toggle(student: Student) {
    if (!academy) return;
    setSaving(true);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/students/${student.id}`,
        {
          method: "PUT",
          headers: apiHeaders(true),
          body: JSON.stringify({
            firstName: student.firstName,
            lastName: student.lastName,
            email: student.email,
            phone: student.phone,
            branchId: null,
            isActive: !student.isActive,
          }),
        },
      );
      if (!response.ok) throw new Error();
      await load();
      setMessage(
        `${student.firstName} ${student.lastName} ${student.isActive ? "made inactive" : "reactivated"}.`,
      );
    } catch {
      setMessage("The student status could not be changed.");
    } finally {
      setSaving(false);
    }
  }
  async function assignBatch() {
    if (!academy || !assignmentStudentId || !assignmentBatchId)
      return setMessage("Select a student and batch.");
    setSaving(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/enrollments`, {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({ studentId: assignmentStudentId, batchId: assignmentBatchId, startDate: null, status: "Active" }),
      });
      const result = await response.json().catch(() => null);
      if (!response.ok) throw new Error(result?.message ?? "The batch could not be assigned.");
      setAssignmentStudentId("");
      setAssignmentBatchId("");
      setMessage("Batch assigned to student.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "The batch could not be assigned.");
    } finally {
      setSaving(false);
    }
  }
  return (
    <main className="enterprise-settings student-management-standard">
      <header className="student-management-heading">
        <div className="student-management-title">
          <span className="student-management-title-icon" aria-hidden="true">♙</span>
          <div><p>Students</p><h1>Management</h1><span>Search, review, and act on every student record.</span></div>
        </div>
      </header>
      <section className="student-management-toolbar">
        <div className="enterprise-settings-tabs">
          <button
            onClick={() => setStatus("Active")}
            aria-selected={status === "Active"}
          >
            Active
          </button>
          <button
            onClick={() => setStatus("Inactive")}
            aria-selected={status === "Inactive"}
          >
            Inactive
          </button>
        </div>
        <label className="enterprise-settings-search">
          ⌕
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search name, email or phone"
          />
        </label>
      </section>
      {message && (
        <p className="enterprise-page-state student-management-message">
          {message}
        </p>
      )}
      <section className="student-management-layout">
        <section className="student-management-panel">
          <header className="student-management-panel-header">
            <div>
          <h3 className="font-semibold">Students</h3>
              <p className="mt-1 text-sm text-slate-400">
                {filtered.length} of {students.length} records ·{" "}
                {academy?.name ?? "Academy"}
              </p>
            </div>
          </header>
          <div className="overflow-auto">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead className="bg-slate-900 text-slate-400">
                <tr>
                  <th className="p-4 font-medium">Student</th>
                  <th className="p-4 font-medium">Contact</th>
                  <th className="p-4 font-medium">Status</th>
                  <th className="p-4 font-medium">Next action</th>
                </tr>
              </thead>
              <tbody>
                {filtered.length ? (
                  filtered.map((student) => (
                    <tr
                      key={student.id}
                      className="border-t hover:bg-slate-900/50"
                    >
                      <td className="p-4">
                        <b>
                          {student.firstName} {student.lastName}
                        </b>
                        <small className="mt-1 block text-slate-400">
                          Student record
                        </small>
                      </td>
                      <td className="p-4 text-slate-300">
                        {student.email || student.phone || "No contact details"}
                      </td>
                      <td className="p-4">
                        <span
                          className={
                            student.isActive
                              ? "platform-status active"
                              : "platform-status"
                          }
                        >
                          {student.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td className="p-4">
                        <div className="flex gap-3">
                          <Link
                            className="text-cyan-300"
                            href={`/student-profile?studentId=${student.id}`}
                          >
                            Open record
                          </Link>
                          <button
                            disabled={saving}
                            onClick={() => void toggle(student)}
                            className="text-slate-400 hover:text-slate-100"
                          >
                            {student.isActive ? "Deactivate" : "Reactivate"}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={4} className="p-10 text-center text-slate-400">
                      No students match this view.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
        <aside className="student-management-assignment">
          <header className="student-management-panel-header">
            <div><p>Enrolment</p><h3>Assign class / batch</h3></div>
            <button
              disabled={saving || !assignmentStudentId || !assignmentBatchId}
              onClick={() => void assignBatch()}
              className="student-management-assign-button"
            >
              Assign batch
            </button>
          </header>
          <div className="student-management-assignment-fields">
            <StandardSelectField
              name="assignment-student"
              value={assignmentStudentId}
              onChange={setAssignmentStudentId}
              placeholder="Select student"
              options={students.filter((student) => student.isActive).map((student) => ({ value: student.id, label: `${student.firstName} ${student.lastName}` }))}
            />
            <StandardSelectField
              name="assignment-batch"
              value={assignmentBatchId}
              onChange={setAssignmentBatchId}
              placeholder="Select class or batch"
              options={batches.filter((batch) => batch.isActive && batch.enrollmentStatus === "Open").map((batch) => ({ value: batch.id, label: batch.name }))}
            />
          </div>
        </aside>
      </section>
    </main>
  );
}
