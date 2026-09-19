"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

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
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [specialties, setSpecialties] = useState("");
  const [branchId, setBranchId] = useState("");
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

  async function createTeacher(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    setSaving(true);
    setMessage("");
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/teachers`,
        {
          method: "POST",
          headers: apiHeaders(true),
          body: JSON.stringify({
            firstName,
            lastName,
            email: email || null,
            phone: phone || null,
            specialties: specialties || null,
            branchId: branchId || null,
          }),
        },
      );
      if (!response.ok) throw new Error();
      setFirstName("");
      setLastName("");
      setEmail("");
      setPhone("");
      setSpecialties("");
      setBranchId("");
      await load();
    } catch {
      setMessage(
        "The teacher could not be saved. Check the required details and try again.",
      );
    } finally {
      setSaving(false);
    }
  }
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
    <main className="enterprise-settings">
      <div className="mx-auto max-w-6xl py-6">
        <header className="enterprise-page-header">
          <p>Teachers / management</p>
          <h2>Teacher management</h2>
          <span>Manage core teacher records, active status, and open Teacher 360.</span>
        </header>
        {message && (
          <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">
            {message}
          </p>
        )}
        <section className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
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
                aria-label="Assign class or batch"
                title="Assign class or batch"
                disabled={saving || !assignmentTeacherId || !assignmentBatchId}
                onClick={() => void assignBatch()}
                className="primary-action flex h-9 w-9 items-center justify-center p-0"
              >
                ✓
              </button>
            </div>
            <select className="mt-2 w-full" value={assignmentTeacherId} onChange={(event) => setAssignmentTeacherId(event.target.value)}>
              <option value="">Select teacher</option>
              {activeTeachers.map((teacher) => <option key={teacher.id} value={teacher.id}>{teacher.firstName} {teacher.lastName}</option>)}
            </select>
            <select className="mt-2 w-full" value={assignmentBatchId} onChange={(event) => setAssignmentBatchId(event.target.value)}>
              <option value="">Select class or batch</option>
              {batches.filter((batch) => batch.isActive).map((batch) => <option key={batch.id} value={batch.id}>{batch.name}</option>)}
            </select>
          </article>
        </section>
        <section className="mt-6 grid gap-6 lg:grid-cols-[0.9fr_1.1fr]">
          <form onSubmit={createTeacher} className="hidden">
            <h2 className="text-xl font-semibold">Add teacher</h2>
            <div className="mt-5 grid gap-3 sm:grid-cols-2">
              <input
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                placeholder="First name"
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                required
              />
              <input
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                placeholder="Last name"
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                required
              />
            </div>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Email (optional)"
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            />
            <input
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="Phone (optional)"
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            />
            <input
              value={specialties}
              onChange={(e) => setSpecialties(e.target.value)}
              placeholder="Specialties, e.g. Piano, vocals"
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            />
            <select
              value={branchId}
              onChange={(e) => setBranchId(e.target.value)}
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            >
              <option value="">No branch assigned</option>
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </select>
            <button
              disabled={!academy || saving}
              className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60"
            >
              {saving ? "Saving…" : "Add teacher"}
            </button>
          </form>
          <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
            <h2 className="text-xl font-semibold">Teaching team</h2>
            {teachers.length === 0 ? (
              <p className="mt-6 text-slate-400">
                No teachers yet. Add the first instructor above.
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
                          className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                        />
                        <input
                          value={editLastName}
                          onChange={(e) => setEditLastName(e.target.value)}
                          className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                        />
                        <input
                          value={editEmail}
                          onChange={(e) => setEditEmail(e.target.value)}
                          placeholder="Email"
                          className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                        />
                        <input
                          value={editPhone}
                          onChange={(e) => setEditPhone(e.target.value)}
                          placeholder="Phone"
                          className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                        />
                        <input
                          value={editSpecialties}
                          onChange={(e) => setEditSpecialties(e.target.value)}
                          placeholder="Specialties"
                          className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                        />
                        <select
                          value={editBranchId}
                          onChange={(e) => setEditBranchId(e.target.value)}
                          className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                        >
                          <option value="">No branch assigned</option>
                          {branches.map((branch) => (
                            <option key={branch.id} value={branch.id}>
                              {branch.name}
                            </option>
                          ))}
                        </select>
                      </div>
                      <div className="mt-3 flex gap-2">
                        <Link
                          href={`/teacher-profile?teacherId=${teacher.id}`}
                          className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-cyan-300 hover:border-cyan-400"
                        >
                          Open record
                        </Link>
                        <button
                          onClick={() => void saveTeacher(teacher)}
                          disabled={savingId === teacher.id}
                          className="rounded-lg bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950 disabled:opacity-50"
                        >
                          {savingId === teacher.id ? "Saving…" : "Save"}
                        </button>
                        <button
                          onClick={() => setEditingId(null)}
                          className="rounded-lg border border-slate-700 px-3 py-2 text-sm"
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
                      <div className="mt-3 flex gap-2">
                        <button
                          onClick={() => beginEdit(teacher)}
                          className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm hover:border-cyan-400"
                        >
                          Edit
                        </button>
                        <button
                          onClick={() => void toggleActive(teacher)}
                          disabled={savingId === teacher.id}
                          className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm hover:border-amber-400 disabled:opacity-50"
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
