"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

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
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <p>Students / management</p>
        <h2>Student management</h2>
        <span>
          Search, review and act on every student. Use Student 360 to connect
          family, enrolment, attendance, fees, learning, and communications.
        </span>
      </header>
      <section className="enterprise-settings-toolbar">
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
        <p className="mt-4 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-100">
          {message}
        </p>
      )}
      <section className="mt-5 grid gap-5 xl:grid-cols-[1fr_340px]">
        <section className="surface-panel overflow-hidden rounded-xl">
          <header className="flex items-center justify-between gap-4 border-b p-5">
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
        <aside className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">Assign batch</h3>
          <div className="mt-4 grid gap-3">
            <select value={assignmentStudentId} onChange={(event) => setAssignmentStudentId(event.target.value)}>
              <option value="">Select student</option>
              {students.filter((student) => student.isActive).map((student) => <option key={student.id} value={student.id}>{student.firstName} {student.lastName}</option>)}
            </select>
            <select value={assignmentBatchId} onChange={(event) => setAssignmentBatchId(event.target.value)}>
              <option value="">Select class or batch</option>
              {batches.filter((batch) => batch.isActive && batch.enrollmentStatus === "Open").map((batch) => <option key={batch.id} value={batch.id}>{batch.name}</option>)}
            </select>
            <button disabled={saving || !assignmentStudentId || !assignmentBatchId} onClick={() => void assignBatch()} className="primary-action w-full">
              {saving ? "Assigning…" : "Assign batch"}
            </button>
          </div>
        </aside>
      </section>
    </main>
  );
}
