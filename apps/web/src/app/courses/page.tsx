"use client";

import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { WorkspaceNav } from "@/components/workspace-nav";

type Academy = { id: string; name: string };
type Course = {
  id: string;
  name: string;
  academyType: string;
  level?: string | null;
  description?: string | null;
  isActive: boolean;
};

export default function CoursesPage() {
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [academyId, setAcademyId] = useState("");
  const [courses, setCourses] = useState<Course[]>([]);
  const [name, setName] = useState("");
  const [type, setType] = useState("Music");
  const [level, setLevel] = useState("");
  const [message, setMessage] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editType, setEditType] = useState("Music");
  const [editLevel, setEditLevel] = useState("");
  const [savingId, setSavingId] = useState<string | null>(null);
  async function loadAcademies() {
    const r = await academyApi("/api/academies", { cache: "no-store" });
    if (!r.ok) throw new Error();
    const data: Academy[] = await r.json();
    setAcademies(data);
    if (!academyId && data.length) setAcademyId(data[0].id);
  }
  async function loadCourses(id: string) {
    if (!id) return setCourses([]);
    const r = await academyApi(`/api/academies/${id}/courses`, {
      cache: "no-store",
    });
    if (!r.ok) throw new Error();
    setCourses(await r.json());
  }
  useEffect(() => {
    void loadAcademies().catch(() =>
      setMessage("The AcademyDesk API is not reachable."),
    );
  }, []);
  useEffect(() => {
    void loadCourses(academyId).catch(() =>
      setMessage("Courses could not be loaded."),
    );
  }, [academyId]);
  async function createCourse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const r = await academyApi(`/api/academies/${academyId}/courses`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify({ name, academyType: type, level }),
    });
    if (!r.ok) return setMessage("The course could not be saved.");
    setName("");
    setLevel("");
    setMessage("");
    await loadCourses(academyId);
  }
  function beginEdit(course: Course) {
    setEditingId(course.id);
    setEditName(course.name);
    setEditType(course.academyType);
    setEditLevel(course.level ?? "");
  }
  async function saveCourse(course: Course) {
    if (!editName.trim()) return setMessage("Course name is required.");
    setSavingId(course.id);
    const r = await academyApi(
      `/api/academies/${academyId}/courses/${course.id}`,
      {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          name: editName,
          academyType: editType,
          level: editLevel || null,
          description: course.description,
          durationMonths: null,
          isActive: course.isActive,
        }),
      },
    );
    setSavingId(null);
    if (!r.ok) return setMessage("The course could not be updated.");
    setEditingId(null);
    setMessage("Course updated.");
    await loadCourses(academyId);
  }
  async function toggleActive(course: Course) {
    setSavingId(course.id);
    const r = await academyApi(
      `/api/academies/${academyId}/courses/${course.id}`,
      {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          name: course.name,
          academyType: course.academyType,
          level: course.level,
          description: course.description,
          durationMonths: null,
          isActive: !course.isActive,
        }),
      },
    );
    setSavingId(null);
    if (!r.ok) return setMessage("The course status could not be updated.");
    setMessage(
      course.isActive ? "Course marked inactive." : "Course reactivated.",
    );
    await loadCourses(academyId);
  }
  return (
    <>
      <WorkspaceNav />
      <main className="min-h-screen bg-slate-950 px-6 py-12 text-slate-100">
        <div className="mx-auto max-w-5xl">
        <h1 className="text-4xl font-semibold">Course catalogue</h1>
          <p className="mt-3 text-slate-300">
            Configure music programs, tuition subjects, and coaching courses in
            one catalogue.
          </p>
          {message && (
            <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-200">
              {message}
            </p>
          )}
          {academies.length === 0 ? (
            <p className="mt-8 rounded-lg border border-dashed border-slate-700 p-8 text-center text-slate-400">
              Create an academy first.
            </p>
          ) : (
            <>
              <label
                className="mt-8 block text-sm text-slate-300"
                htmlFor="academy"
              >
                Academy
              </label>
              <select
                id="academy"
                value={academyId}
                onChange={(e) => setAcademyId(e.target.value)}
                className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
              >
                {academies.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.name}
                  </option>
                ))}
              </select>
              <section className="mt-6 grid gap-6 md:grid-cols-2">
                <form
                  onSubmit={createCourse}
                  className="rounded-2xl border border-slate-800 bg-slate-900 p-6"
                >
                  <h2 className="text-xl font-semibold">Add course</h2>
                  <input
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="Course or subject name"
                    className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                    required
                  />
                  <select
                    value={type}
                    onChange={(e) => setType(e.target.value)}
                    className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                  >
                    <option>Music</option>
                    <option>Tuition</option>
                    <option>Coaching</option>
                  </select>
                  <input
                    value={level}
                    onChange={(e) => setLevel(e.target.value)}
                    placeholder="Level (optional)"
                    className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                  />
                  <button className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300">
                    Create course
                  </button>
                </form>
                <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
                  <h2 className="text-xl font-semibold">Courses</h2>
                  {courses.length === 0 ? (
                    <p className="mt-6 text-slate-400">No courses yet.</p>
                  ) : (
                    <ul className="mt-4 space-y-3">
                      {courses.map((c) =>
                        editingId === c.id ? (
                          <li
                            key={c.id}
                            className="rounded-lg border border-cyan-700/60 p-4"
                          >
                            <div className="grid gap-2">
                              <input
                                value={editName}
                                onChange={(e) => setEditName(e.target.value)}
                                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                              />
                              <select
                                value={editType}
                                onChange={(e) => setEditType(e.target.value)}
                                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                              >
                                <option>Music</option>
                                <option>Tuition</option>
                                <option>Coaching</option>
                              </select>
                              <input
                                value={editLevel}
                                onChange={(e) => setEditLevel(e.target.value)}
                                placeholder="Level"
                                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                              />
                            </div>
                            <div className="mt-3 flex gap-2">
                              <button
                                onClick={() => void saveCourse(c)}
                                disabled={savingId === c.id}
                                className="rounded-lg bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950"
                              >
                                Save
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
                            key={c.id}
                            className="rounded-lg border border-slate-700 p-4"
                          >
                            <div className="flex items-start justify-between">
                              <div>
                                <div className="font-medium">{c.name}</div>
                                <div className="mt-1 text-sm text-slate-400">
                                  {c.academyType}
                                  {c.level ? ` · ${c.level}` : ""}
                                </div>
                              </div>
                              <span
                                className={`rounded-full px-2 py-1 text-xs ${c.isActive ? "bg-emerald-950 text-emerald-300" : "bg-slate-800 text-slate-400"}`}
                              >
                                {c.isActive ? "Active" : "Inactive"}
                              </span>
                            </div>
                            <div className="mt-3 flex gap-2">
                              <button
                                onClick={() => beginEdit(c)}
                                className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm"
                              >
                                Edit
                              </button>
                              <button
                                onClick={() => void toggleActive(c)}
                                disabled={savingId === c.id}
                                className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm"
                              >
                                {c.isActive ? "Deactivate" : "Reactivate"}
                              </button>
                            </div>
                          </li>
                        ),
                      )}
                    </ul>
                  )}
                </section>
              </section>
            </>
          )}
        </div>
      </main>
    </>
  );
}
