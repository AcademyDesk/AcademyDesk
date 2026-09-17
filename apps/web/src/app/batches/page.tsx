"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string };
type Course = { id: string; name: string; academyType: string };
type Teacher = { id: string; firstName: string; lastName: string };
type Branch = { id: string; name: string };
type Batch = { id: string; name: string; batchCode?: string | null; courseId: string; teacherId?: string | null; branchId?: string | null; capacity: number; waitlistCapacity?: number; deliveryMode?: string; meetingPattern?: string | null; roomName?: string | null; enrollmentStatus?: string; activeEnrolments?: number; startDate?: string | null; endDate?: string | null; isActive: boolean };

export default function BatchesPage() {
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [academyId, setAcademyId] = useState("");
  const [courses, setCourses] = useState<Course[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [courseId, setCourseId] = useState("");
  const [teacherId, setTeacherId] = useState("");
  const [branchId, setBranchId] = useState("");
  const [name, setName] = useState("");
  const [capacity, setCapacity] = useState("10");
  const [batchCode, setBatchCode] = useState("");
  const [waitlistCapacity, setWaitlistCapacity] = useState("0");
  const [deliveryMode, setDeliveryMode] = useState("InPerson");
  const [meetingPattern, setMeetingPattern] = useState("");
  const [roomName, setRoomName] = useState("");
  const [enrollmentStatus, setEnrollmentStatus] = useState("Open");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [message, setMessage] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null); const [editName, setEditName] = useState(""); const [editCourseId, setEditCourseId] = useState(""); const [editTeacherId, setEditTeacherId] = useState(""); const [editBranchId, setEditBranchId] = useState(""); const [editCapacity, setEditCapacity] = useState("10"); const [editStartDate, setEditStartDate] = useState(""); const [editEndDate, setEditEndDate] = useState(""); const [savingId, setSavingId] = useState<string | null>(null);

  async function loadAcademies() {
    const response = await academyApi("/api/academies", { cache: "no-store" });
    if (!response.ok) throw new Error();
    const data: Academy[] = await response.json();
    setAcademies(data);
    if (!academyId && data.length) setAcademyId(data[0].id);
  }

  async function loadWorkspace(id: string) {
    if (!id) return;
    const [courseResponse, teacherResponse, branchResponse, batchResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/courses`, { cache: "no-store" }),
      academyApi(`/api/academies/${id}/teachers`, { cache: "no-store" }),
      academyApi(`/api/academies/${id}/branches`, { cache: "no-store" }),
      academyApi(`/api/academies/${id}/batches`, { cache: "no-store" }),
    ]);
    if (![courseResponse, teacherResponse, branchResponse, batchResponse].every((response) => response.ok)) throw new Error();
    const courseData: Course[] = await courseResponse.json();
    setCourses(courseData);
    setTeachers(await teacherResponse.json());
    setBranches(await branchResponse.json());
    setBatches(await batchResponse.json());
    if (!courseId && courseData.length) setCourseId(courseData[0].id);
  }

  useEffect(() => { void loadAcademies().catch(() => setMessage("Please sign in and make sure the AcademyDesk API is running.")); }, []);
  useEffect(() => { void loadWorkspace(academyId).catch(() => setMessage("Batch information could not be loaded.")); }, [academyId]);

  async function createBatch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academyId || !courseId) return;
    const response = await academyApi(`/api/academies/${academyId}/batches`, {
      method: "POST", headers: apiHeaders(true),
      body: JSON.stringify({ name, batchCode: batchCode || null, courseId, teacherId: teacherId || null, branchId: branchId || null, capacity: Number(capacity), waitlistCapacity: Number(waitlistCapacity), deliveryMode, meetingPattern: meetingPattern || null, roomName: roomName || null, enrollmentStatus, adminNotes: null, startDate: startDate || null, endDate: endDate || null }),
    });
    if (!response.ok) return setMessage("The batch could not be saved. Check the course, teacher, and date fields.");
    setName(""); setBatchCode(""); setTeacherId(""); setBranchId(""); setWaitlistCapacity("0"); setDeliveryMode("InPerson"); setMeetingPattern(""); setRoomName(""); setEnrollmentStatus("Open"); setStartDate(""); setEndDate(""); setMessage("");
    await loadWorkspace(academyId);
  }
  function beginEdit(batch: Batch) { setEditingId(batch.id); setEditName(batch.name); setEditCourseId(batch.courseId); setEditTeacherId(batch.teacherId ?? ""); setEditBranchId(batch.branchId ?? ""); setEditCapacity(String(batch.capacity)); setEditStartDate(batch.startDate ?? ""); setEditEndDate(batch.endDate ?? ""); }
  async function saveBatch(batch: Batch) { if (!editName.trim() || !editCourseId) return setMessage("Batch name and course are required."); setSavingId(batch.id); const response = await academyApi(`/api/academies/${academyId}/batches/${batch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: editName, courseId: editCourseId, teacherId: editTeacherId || null, branchId: editBranchId || null, capacity: Number(editCapacity), startDate: editStartDate || null, endDate: editEndDate || null, isActive: batch.isActive }) }); setSavingId(null); if (!response.ok) return setMessage("The batch could not be updated."); setEditingId(null); setMessage("Batch updated."); await loadWorkspace(academyId); }
  async function toggleActive(batch: Batch) { setSavingId(batch.id); const response = await academyApi(`/api/academies/${academyId}/batches/${batch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: batch.name, courseId: batch.courseId, teacherId: batch.teacherId, branchId: batch.branchId, capacity: batch.capacity, startDate: batch.startDate, endDate: batch.endDate, isActive: !batch.isActive }) }); setSavingId(null); if (!response.ok) return setMessage("The batch status could not be updated."); setMessage(batch.isActive ? "Batch marked inactive." : "Batch reactivated."); await loadWorkspace(academyId); }

  const teacherName = (id?: string | null) => {
    const teacher = teachers.find((item) => item.id === id);
    return teacher ? `${teacher.firstName} ${teacher.lastName}` : "Unassigned";
  };
  const branchName = (id?: string | null) => branches.find((branch) => branch.id === id)?.name ?? "No branch";

  return <main className="min-h-screen bg-slate-950 text-slate-100">
    <WorkspaceNav />
    <div className="mx-auto max-w-6xl px-6 py-10">
      <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Delivery</p>
      <h1 className="mt-3 text-4xl font-semibold tracking-tight">Batches and classes</h1>
      <p className="mt-3 text-slate-300">Turn each course into a teaching group with its instructor, location, capacity, and dates.</p>
      {message && <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
      {academies.length === 0 ? <p className="mt-8 rounded-lg border border-dashed border-slate-700 p-8 text-center text-slate-400">Create an academy and course first.</p> : <>
        <label className="mt-8 block text-sm text-slate-300" htmlFor="academy">Academy</label><select id="academy" value={academyId} onChange={(event) => setAcademyId(event.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2">{academies.map((academy) => <option key={academy.id} value={academy.id}>{academy.name}</option>)}</select>
        <section className="mt-6 grid gap-6 lg:grid-cols-[0.9fr_1.1fr]">
          <form onSubmit={createBatch} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Create batch</h2>
            <select value={courseId} onChange={(event) => setCourseId(event.target.value)} className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required><option value="">Select course</option>{courses.map((course) => <option key={course.id} value={course.id}>{course.name} · {course.academyType}</option>)}</select>
            <input value={name} onChange={(event) => setName(event.target.value)} placeholder="Batch name, e.g. Piano Level 1 – Evening" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required />
            <input value={batchCode} onChange={(event) => setBatchCode(event.target.value)} placeholder="Batch code, e.g. PNO-L1-EVE" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" />
            <select value={teacherId} onChange={(event) => setTeacherId(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option value="">No teacher assigned yet</option>{teachers.map((teacher) => <option key={teacher.id} value={teacher.id}>{teacher.firstName} {teacher.lastName}</option>)}</select>
            <select value={branchId} onChange={(event) => setBranchId(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option value="">No branch assigned</option>{branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}</select>
            <input type="number" min="1" max="1000" value={capacity} onChange={(event) => setCapacity(event.target.value)} placeholder="Capacity" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required />
            <div className="mt-3 grid gap-3 sm:grid-cols-2"><select value={deliveryMode} onChange={(event) => setDeliveryMode(event.target.value)} className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option value="InPerson">In person</option><option value="Online">Online</option><option value="Hybrid">Hybrid</option></select><select value={enrollmentStatus} onChange={(event) => setEnrollmentStatus(event.target.value)} className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option value="Open">Open for enrolment</option><option value="Waitlist">Waitlist only</option><option value="Closed">Closed</option></select></div>
            <div className="mt-3 grid gap-3 sm:grid-cols-2"><input value={meetingPattern} onChange={(event) => setMeetingPattern(event.target.value)} placeholder="Meeting pattern, e.g. Tue/Thu 17:00" className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><input value={roomName} onChange={(event) => setRoomName(event.target.value)} placeholder="Room / online location" className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /></div>
            <input type="number" min="0" max="1000" value={waitlistCapacity} onChange={(event) => setWaitlistCapacity(event.target.value)} placeholder="Waitlist capacity" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" />
            <div className="mt-3 grid gap-3 sm:grid-cols-2"><input type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><input type="date" value={endDate} onChange={(event) => setEndDate(event.target.value)} className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /></div>
            <button className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300">Create batch</button>
          </form>
          <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Batches</h2>{batches.length === 0 ? <p className="mt-6 text-slate-400">No batches yet.</p> : <ul className="mt-4 space-y-3">{batches.map((batch) => editingId === batch.id ? <li key={batch.id} className="rounded-lg border border-cyan-700/60 bg-slate-950 p-4"><div className="grid gap-2"><input value={editName} onChange={(e) => setEditName(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2" /><select value={editCourseId} onChange={(e) => setEditCourseId(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2">{courses.map((course) => <option key={course.id} value={course.id}>{course.name}</option>)}</select><select value={editTeacherId} onChange={(e) => setEditTeacherId(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"><option value="">No teacher</option>{teachers.map((teacher) => <option key={teacher.id} value={teacher.id}>{teacher.firstName} {teacher.lastName}</option>)}</select><select value={editBranchId} onChange={(e) => setEditBranchId(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"><option value="">No branch</option>{branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}</select><input type="number" min="1" max="1000" value={editCapacity} onChange={(e) => setEditCapacity(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2" /><div className="grid gap-2 sm:grid-cols-2"><input type="date" value={editStartDate} onChange={(e) => setEditStartDate(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2" /><input type="date" value={editEndDate} onChange={(e) => setEditEndDate(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2" /></div></div><div className="mt-3 flex gap-2"><button onClick={() => void saveBatch(batch)} disabled={savingId === batch.id} className="rounded-lg bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950">Save</button><button onClick={() => setEditingId(null)} className="rounded-lg border border-slate-700 px-3 py-2 text-sm">Cancel</button></div></li> : <li key={batch.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="flex items-start justify-between"><div><div className="font-medium">{batch.name}</div><div className="mt-2 text-sm text-slate-300">{teacherName(batch.teacherId)} · {branchName(batch.branchId)}</div><div className="mt-1 text-sm text-slate-400">Capacity: {batch.capacity}{batch.startDate ? ` · Starts ${batch.startDate}` : ""}</div></div><span className={`rounded-full px-2 py-1 text-xs ${batch.isActive ? "bg-emerald-950 text-emerald-300" : "bg-slate-800 text-slate-400"}`}>{batch.isActive ? "Active" : "Inactive"}</span></div><div className="mt-3 flex gap-2"><button onClick={() => beginEdit(batch)} className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm">Edit</button><button onClick={() => void toggleActive(batch)} disabled={savingId === batch.id} className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm">{batch.isActive ? "Deactivate" : "Reactivate"}</button></div></li>)}</ul>}</section>
        </section>
      </>}
    </div>
  </main>;
}
