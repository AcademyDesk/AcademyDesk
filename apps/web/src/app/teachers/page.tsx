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
    if (!teacherResponse.ok || !branchResponse.ok || !batchResponse.ok) throw new Error();
    setTeachers(await teacherResponse.json());
    setBranches(await branchResponse.json());
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
      } catch {
        setMessage(
          "Teachers could not be loaded. Confirm that the API is running on port 5092.",
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
        <header className="enterprise-page-header">
          <p>Teachers / management</p>
          <h2>Teacher management</h2>
          <span>Manage core teacher records, active status, and open Teacher 360.</span>
        </header>
        {message && <p className="enterprise-page-state">{message}</p>}
        <section className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <article className="surface-panel rounded-xl p-5">
            <p className="text-sm text-slate-400">Active teachers</p>
            <p className="mt-2 text-3xl font-semibold">
              {activeTeachers.length}
            </p>
          </article>
          <article className="surface-panel rounded-xl p-5">
            <p className="text-sm text-slate-400">Due this cycle</p>
            <p className="mt-2 text-3xl font-semibold">
              {activeTeachers.length}
            </p>
            <p className="mt-1 text-sm text-slate-400">
              Active teachers pending payroll review
            </p>
          </article>
          <article className="surface-panel rounded-xl p-5">
            <p className="text-sm text-slate-400">Next payment cycle</p>
            <p className="mt-2 font-semibold">
              {new Intl.DateTimeFormat("en-IN", {
                day: "2-digit",
                month: "short",
                timeZone: "Asia/Kolkata",
              }).format(nextPayCycle)}
            </p>
            <p className="mt-1 text-sm text-slate-400">
              Monthly settlement date
            </p>
          </article>
          <article className="surface-panel rounded-xl p-5">
            <div className="flex items-center justify-between gap-3">
              <p className="text-sm text-slate-400">Assign Class / Batch</p>
              <button
                aria-label="Save class or batch assignment"
                title="Save class or batch assignment"
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
        <section className="mt-6">
          <section className="surface-panel teacher-directory-panel rounded-xl p-5">
            <h2 className="text-xl font-semibold">Teaching team</h2>
            {teachers.length === 0 ? (
              <p className="mt-6 text-slate-400">
                No teachers yet. Start with Teacher onboarding.
              </p>
            ) : (
              <ul className="mt-5 space-y-3">
                {teachers.map((teacher) =>
                  editingId === teacher.id ? (
                    <li
                      key={teacher.id}
                      className="rounded-lg border border-cyan-700/60 bg-slate-950 p-4"
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
                      className="rounded-lg border border-slate-700 bg-slate-950 p-4"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <div className="font-medium">
                            {teacher.firstName} {teacher.lastName}
                          </div>
                          <div className="mt-1 text-sm text-slate-400">
                            {teacher.specialties || "No specialties set"}
                          </div>
                          <div className="mt-2 text-sm text-slate-300">
                            {teacher.email ||
                              teacher.phone ||
                              "No contact details"}
                          </div>
                        </div>
                        <span
                          className={`rounded-full px-2 py-1 text-xs ${teacher.isActive ? "bg-emerald-950 text-emerald-300" : "bg-slate-800 text-slate-400"}`}
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
        </section>
      </div>
    </main>
  );
}
