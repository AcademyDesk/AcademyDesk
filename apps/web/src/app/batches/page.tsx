"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string };
type Course = { id: string; name: string; academyType: string };
type Teacher = { id: string; firstName: string; lastName: string };
type Branch = { id: string; name: string };
type Batch = { id: string; name: string; courseId: string; teacherId?: string | null; branchId?: string | null; capacity: number; startDate?: string | null };

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
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [message, setMessage] = useState("");

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
      body: JSON.stringify({ name, courseId, teacherId: teacherId || null, branchId: branchId || null, capacity: Number(capacity), startDate: startDate || null, endDate: endDate || null }),
    });
    if (!response.ok) return setMessage("The batch could not be saved. Check the course, teacher, and date fields.");
    setName(""); setTeacherId(""); setBranchId(""); setStartDate(""); setEndDate(""); setMessage("");
    await loadWorkspace(academyId);
  }

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
            <select value={teacherId} onChange={(event) => setTeacherId(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option value="">No teacher assigned yet</option>{teachers.map((teacher) => <option key={teacher.id} value={teacher.id}>{teacher.firstName} {teacher.lastName}</option>)}</select>
            <select value={branchId} onChange={(event) => setBranchId(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option value="">No branch assigned</option>{branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}</select>
            <input type="number" min="1" max="1000" value={capacity} onChange={(event) => setCapacity(event.target.value)} placeholder="Capacity" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required />
            <div className="mt-3 grid gap-3 sm:grid-cols-2"><input type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><input type="date" value={endDate} onChange={(event) => setEndDate(event.target.value)} className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /></div>
            <button className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300">Create batch</button>
          </form>
          <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Active batches</h2>{batches.length === 0 ? <p className="mt-6 text-slate-400">No batches yet.</p> : <ul className="mt-4 space-y-3">{batches.map((batch) => <li key={batch.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="font-medium">{batch.name}</div><div className="mt-2 text-sm text-slate-300">{teacherName(batch.teacherId)} · {branchName(batch.branchId)}</div><div className="mt-1 text-sm text-slate-400">Capacity: {batch.capacity}{batch.startDate ? ` · Starts ${batch.startDate}` : ""}</div></li>)}</ul>}</section>
        </section>
      </>}
    </div>
  </main>;
}
