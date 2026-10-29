"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardSelectField } from "@/components/design-system/controls";
import styles from "@/components/design-system/records-workspace.module.css";

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
  async function read(signal?: AbortSignal) {
    const response = await academyApi("/api/academies", { cache: "no-store", signal });
    if (!response.ok) throw new Error();
    const academies: Academy[] = await response.json();
    if (!Array.isArray(academies)) throw new Error();
    const current = academies[0];
    if (!current) return null;
    if (typeof current.id !== "string" || !current.id) throw new Error();
    const [roster, batchResponse] = await Promise.all([
      academyApi(`/api/academies/${current.id}/students`, { cache: "no-store", signal }),
      academyApi(`/api/academies/${current.id}/batches`, { cache: "no-store", signal }),
    ]);
    if (!roster.ok || !batchResponse.ok) throw new Error();
    const [people, classes]: [Student[], Batch[]] = await Promise.all([roster.json(), batchResponse.json()]);
    if (!Array.isArray(people) || !Array.isArray(classes)) throw new Error();
    return { current, people, classes };
  }
  const publish = useCallback((snapshot: Awaited<ReturnType<typeof read>>) => {
    if (!snapshot) { setMessage("Create an academy before managing students."); return; }
    setAcademy(snapshot.current);
    setStudents(snapshot.people);
    setBatches(snapshot.classes);
    setMessage("");
  }, []);
  async function load() { publish(await read()); }
  useEffect(() => {
    const controller = new AbortController();
    void read(controller.signal).then(snapshot => {
      if (!controller.signal.aborted) publish(snapshot);
    }).catch(() => { if (!controller.signal.aborted) setMessage("The student register could not be loaded."); });
    return () => controller.abort();
  }, [publish]);
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
    <main className={`enterprise-settings student-management-standard ${styles.records}`}>
      <header className="student-management-heading">
        <div className="student-management-title">
          <span className="student-management-title-icon" aria-hidden="true">♙</span>
          <div><p>Students</p><h1>Management</h1></div>
        </div>
      </header>
      <section className="student-management-toolbar">
        <div className="enterprise-settings-tabs">
          <button
            onClick={() => setStatus("Active")}
            aria-pressed={status === "Active"}
          >
            Active
          </button>
          <button
            onClick={() => setStatus("Inactive")}
            aria-pressed={status === "Inactive"}
          >
            Inactive
          </button>
        </div>
        <label className="enterprise-settings-search">
          ⌕
          <input
            aria-label="Search students by name, email or phone"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search name, email or phone"
          />
        </label>
      </section>
      {message && (
        <p role="status" aria-live="polite" className="enterprise-page-state student-management-message">
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
            <table role="table" className={`text-left text-sm ${styles.table}`}>
              <caption className={styles.srOnly}>Student register — {status.toLowerCase()} students</caption>
              <thead role="rowgroup" className="bg-slate-900 text-slate-400">
                <tr role="row">
                  <th className="p-4 font-medium">Student</th>
                  <th className="p-4 font-medium">Contact</th>
                  <th className="p-4 font-medium">Status</th>
                  <th className="p-4 font-medium">Next action</th>
                </tr>
              </thead>
              <tbody role="rowgroup">
                {filtered.length ? (
                  filtered.map((student) => (
                    <tr
                      role="row"
                      key={student.id}
                      className="border-t hover:bg-slate-900/50"
                    >
                      <td role="cell" className="p-4">
                        <span aria-hidden="true" className={styles.cellLabel}>Student</span>
                        <b>
                          {student.firstName} {student.lastName}
                        </b>
                        <small className="mt-1 block text-slate-400">
                          Student record
                        </small>
                      </td>
                      <td role="cell" className="p-4 text-slate-300">
                        <span aria-hidden="true" className={styles.cellLabel}>Contact</span>
                        {student.email || student.phone || "No contact details"}
                      </td>
                      <td role="cell" className="p-4">
                        <span aria-hidden="true" className={styles.cellLabel}>Status</span>
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
                      <td role="cell" className="p-4">
                        <span aria-hidden="true" className={styles.cellLabel}>Actions</span>
                        <div className={styles.actions}>
                          <Link
                            className="enterprise-action-button enterprise-action-button-secondary"
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
              label="Student"
              name="assignment-student"
              value={assignmentStudentId}
              onChange={setAssignmentStudentId}
              placeholder="Select student"
              options={students.filter((student) => student.isActive).map((student) => ({ value: student.id, label: `${student.firstName} ${student.lastName}` }))}
            />
            <StandardSelectField
              label="Class or batch"
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
