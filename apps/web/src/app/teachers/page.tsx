"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string; name: string };
type Branch = { id: string; name: string };
type Batch = { id: string; name: string; batchCode?: string | null; courseId: string; teacherId?: string | null; branchId?: string | null; capacity: number; waitlistCapacity: number; deliveryMode?: string; meetingPattern?: string | null; roomName?: string | null; enrollmentStatus?: string; adminNotes?: string | null; startDate?: string | null; endDate?: string | null; isActive: boolean };
type Teacher = {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phone?: string | null;
  specialties?: string | null;
  branchId?: string | null;
  isActive: boolean;
};

export default function TeachersPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [branches, setBranches] = useState<Branch[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [assignmentTeacherId, setAssignmentTeacherId] = useState("");
  const [assignmentBatchId, setAssignmentBatchId] = useState("");
  const [message, setMessage] = useState("Loading teachers…");
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editFirstName, setEditFirstName] = useState("");
  const [editLastName, setEditLastName] = useState("");
  const [editEmail, setEditEmail] = useState("");
  const [editPhone, setEditPhone] = useState("");
  const [editSpecialties, setEditSpecialties] = useState("");
  const [editBranchId, setEditBranchId] = useState("");
  const [savingId, setSavingId] = useState<string | null>(null);
  const activeTeachers = teachers.filter((teacher) => teacher.isActive);
  const nextPayCycle = new Date(
    new Date().getFullYear(),
    new Date().getMonth() + 1,
    1,
  );

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [teacherResponse, branchResponse, batchResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/teachers`, { cache: "no-store" }),
      academyApi(`/api/academies/${id}/branches`, { cache: "no-store" }),
      academyApi(`/api/academies/${id}/batches`, { cache: "no-store" }),
    ]);
    if (!teacherResponse.ok || !batchResponse.ok) {
      const failedResponse = [teacherResponse, batchResponse].find((response) => !response.ok);
      throw new Error(failedResponse?.status === 401 ? "SESSION_EXPIRED" : "LOAD_FAILED");
    }
    setTeachers(await teacherResponse.json());
    setBranches(branchResponse.ok ? await branchResponse.json() : []);
    setBatches(await batchResponse.json());
    setMessage("");
  }

  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (response.status === 401)
          return setMessage("Please sign in before managing teachers.");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0])
          return setMessage("Create your academy first, then add teachers.");
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch (error) {
        setMessage(
          error instanceof Error && error.message === "SESSION_EXPIRED"
            ? "Your sign-in session has expired. Please sign in again."
            : "Teachers could not be loaded. Please refresh the page and try again.",
        );
      }
    }
    void initialise();
  }, []);

  function beginEdit(teacher: Teacher) {
    setEditingId(teacher.id);
    setEditFirstName(teacher.firstName);
    setEditLastName(teacher.lastName);
    setEditEmail(teacher.email ?? "");
    setEditPhone(teacher.phone ?? "");
    setEditSpecialties(teacher.specialties ?? "");
    setEditBranchId(teacher.branchId ?? "");
  }
  async function saveTeacher(teacher: Teacher) {
    if (!academy || !editFirstName.trim() || !editLastName.trim())
      return setMessage("First name and last name are required.");
    setSavingId(teacher.id);
    const response = await academyApi(
      `/api/academies/${academy.id}/teachers/${teacher.id}`,
      {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          firstName: editFirstName,
          lastName: editLastName,
          email: editEmail || null,
          phone: editPhone || null,
          specialties: editSpecialties || null,
          branchId: editBranchId || null,
          isActive: teacher.isActive,
        }),
      },
    );
    setSavingId(null);
    if (!response.ok) return setMessage("The teacher could not be updated.");
    setEditingId(null);
    setMessage("Teacher updated.");
    await load();
  }
  async function toggleActive(teacher: Teacher) {
    if (!academy) return;
    setSavingId(teacher.id);
    const response = await academyApi(
      `/api/academies/${academy.id}/teachers/${teacher.id}`,
      {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          firstName: teacher.firstName,
          lastName: teacher.lastName,
          email: teacher.email,
          phone: teacher.phone,
          specialties: teacher.specialties,
          branchId: teacher.branchId,
          isActive: !teacher.isActive,
        }),
      },
    );
    setSavingId(null);
    if (!response.ok)
      return setMessage("The teacher status could not be updated.");
    setMessage(
      teacher.isActive ? "Teacher marked inactive." : "Teacher reactivated.",
    );
    await load();
  }
  async function assignBatch() {
    if (!academy || !assignmentTeacherId || !assignmentBatchId)
      return setMessage("Select a teacher and class or batch.");
    const batch = batches.find((item) => item.id === assignmentBatchId);
    if (!batch) return;
    setSaving(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/batches/${batch.id}`, {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          name: batch.name, batchCode: batch.batchCode ?? null, courseId: batch.courseId,
          teacherId: assignmentTeacherId, branchId: batch.branchId ?? null,
          capacity: batch.capacity, waitlistCapacity: batch.waitlistCapacity ?? 0,
          deliveryMode: batch.deliveryMode ?? "InPerson", meetingPattern: batch.meetingPattern ?? null,
          roomName: batch.roomName ?? null, enrollmentStatus: batch.enrollmentStatus ?? "Open",
          adminNotes: batch.adminNotes ?? null, startDate: batch.startDate ?? null,
          endDate: batch.endDate ?? null, isActive: batch.isActive,
        }),
      });
      const result = await response.json().catch(() => null);
      if (!response.ok) throw new Error(result?.message ?? "The teacher could not be assigned.");
      setAssignmentTeacherId("");
      setAssignmentBatchId("");
      setMessage("Teacher assigned to class or batch.");
      await load();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "The teacher could not be assigned.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <main className="enterprise-settings teacher-standard teacher-management-standard">
      <div className="teacher-management-content">
        <header className="teacher-management-heading">
          <div className="teacher-management-title">
            <span className="teacher-management-title-icon" aria-hidden="true">♜</span>
            <div><p>Teachers</p><h1>Teacher management</h1></div>
          </div>
        </header>
        {message && <p className="enterprise-page-state">{message}</p>}
        <section className="teacher-management-kpis">
          <article className="teacher-management-kpi">
            <span>Active teachers</span>
            <strong>
              {activeTeachers.length}
            </strong>
          </article>
          <article className="teacher-management-kpi">
            <span>Due this cycle</span>
            <strong>
              {activeTeachers.length}
            </strong>
          </article>
          <article className="teacher-management-kpi">
            <span>Next payment cycle</span>
            <strong className="teacher-management-date">
              {new Intl.DateTimeFormat("en-IN", {
                day: "2-digit",
                month: "short",
                timeZone: "Asia/Kolkata",
              }).format(nextPayCycle)}
            </strong>
          </article>
          <article className="teacher-management-assignment-tile">
            <div className="teacher-management-assignment-heading">
              <span>Assign class / batch</span>
              <button
                disabled={saving || !assignmentTeacherId || !assignmentBatchId}
                onClick={() => void assignBatch()}
                className="teacher-assignment-confirm"
              >
                Save
              </button>
            </div>
            <div className="teacher-assignment-control mt-2">
              <StandardSelectField
                name="assignment-teacher"
                value={assignmentTeacherId}
                onChange={setAssignmentTeacherId}
                placeholder="Select teacher"
                options={activeTeachers.map((teacher) => ({ value: teacher.id, label: `${teacher.firstName} ${teacher.lastName}` }))}
              />
            </div>
            <div className="teacher-assignment-control mt-2">
              <StandardSelectField
                name="assignment-batch"
                value={assignmentBatchId}
                onChange={setAssignmentBatchId}
                placeholder="Select class or batch"
                options={batches.filter((batch) => batch.isActive).map((batch) => ({ value: batch.id, label: batch.name }))}
              />
            </div>
          </article>
        </section>
        <section className="teacher-directory-panel">
            <header className="teacher-directory-header"><div><p>Directory</p><h2>Teaching team</h2></div><span>{teachers.length} records</span></header>
            {teachers.length === 0 ? (
              <p className="teacher-directory-empty">No teachers yet.</p>
            ) : (
              <ul className="teacher-directory-list">
                {teachers.map((teacher) =>
                  editingId === teacher.id ? (
                    <li
                      key={teacher.id}
                      className="teacher-directory-record teacher-directory-record-editing"
                    >
                      <div className="grid gap-2 sm:grid-cols-2">
                        <input
                          value={editFirstName}
                          onChange={(e) => setEditFirstName(e.target.value)}
                          className="field"
                        />
                        <input
                          value={editLastName}
                          onChange={(e) => setEditLastName(e.target.value)}
                          className="field"
                        />
                        <input
                          value={editEmail}
                          onChange={(e) => setEditEmail(e.target.value)}
                          placeholder="Email"
                          className="field"
                        />
                        <input
                          value={editPhone}
                          onChange={(e) => setEditPhone(e.target.value)}
                          placeholder="Phone"
                          className="field"
                        />
                        <input
                          value={editSpecialties}
                          onChange={(e) => setEditSpecialties(e.target.value)}
                          placeholder="Specialties"
                          className="field"
                        />
                        <StandardSelectField
                          name="teacher-branch"
                          value={editBranchId}
                          onChange={setEditBranchId}
                          placeholder="No branch assigned"
                          options={branches.map((branch) => ({ value: branch.id, label: branch.name }))}
                        />
                      </div>
                      <div className="mt-3 flex gap-2">
                        <Link
                          href={`/teacher-profile?teacherId=${teacher.id}`}
                          className="enterprise-action-button enterprise-action-button-secondary"
                        >
                          Open record
                        </Link>
                        <button
                          onClick={() => void saveTeacher(teacher)}
                          disabled={savingId === teacher.id}
                          className="enterprise-action-button"
                        >
                          {savingId === teacher.id ? "Saving…" : "Save"}
                        </button>
                        <button
                          onClick={() => setEditingId(null)}
                          className="enterprise-action-button enterprise-action-button-secondary"
                        >
                          Cancel
                        </button>
                      </div>
                    </li>
                  ) : (
                    <li
                      key={teacher.id}
                      className="teacher-directory-record"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="font-medium">
                            {teacher.firstName} {teacher.lastName}
                          </div>
                          <div className="teacher-directory-specialty">
                            {teacher.specialties || "No specialties set"}
                          </div>
                          <div className="teacher-directory-contact">
                            {teacher.email ||
                              teacher.phone ||
                              "No contact details"}
                          </div>
                        </div>
                        <span
                          className={teacher.isActive ? "teacher-directory-status active" : "teacher-directory-status"}
                        >
                          {teacher.isActive ? "Active" : "Inactive"}
                        </span>
                      </div>
                      <div className="mt-3 flex flex-wrap gap-2">
                        <Link
                          href={`/teacher-profile?teacherId=${teacher.id}`}
                          className="enterprise-action-button enterprise-action-button-secondary"
                        >
                          Open Teacher 360
                        </Link>
                        <button
                          onClick={() => beginEdit(teacher)}
                          className="enterprise-action-button enterprise-action-button-secondary"
                        >
                          Edit
                        </button>
                        <button
                          onClick={() => void toggleActive(teacher)}
                          disabled={savingId === teacher.id}
                          className="teacher-status-action"
                        >
                          {teacher.isActive ? "Deactivate" : "Reactivate"}
                        </button>
                      </div>
                    </li>
                  ),
                )}
              </ul>
            )}
        </section>
      </div>
    </main>
  );
}
