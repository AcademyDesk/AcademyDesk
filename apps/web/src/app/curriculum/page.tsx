"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Course = { id: string; name: string };
type Module = {
  id: string;
  courseId: string;
  title: string;
  description?: string;
  sequence: number;
  isPublished: boolean;
};

async function fetchCurriculum(id: string) {
  const [courseResponse, moduleResponse] = await Promise.all([
    academyApi(`/api/academies/${id}/courses`, { cache: "no-store" }),
    academyApi(`/api/academies/${id}/course-modules`, { cache: "no-store" }),
  ]);
  if (!courseResponse.ok || !moduleResponse.ok) throw new Error();
  const courses: unknown = await courseResponse.json();
  const modules: unknown = await moduleResponse.json();
  if (!Array.isArray(courses) || !Array.isArray(modules)) throw new Error();
  return { courses: courses as Course[], modules: modules as Module[] };
}

export default function Curriculum() {
  const [academy, setAcademy] = useState<Academy>();
  const [courses, setCourses] = useState<Course[]>([]);
  const [modules, setModules] = useState<Module[]>([]);
  const [courseId, setCourseId] = useState("");
  const [title, setTitle] = useState("");
  const [sequence, setSequence] = useState("1");
  const [message, setMessage] = useState("Loading curriculum governance…");
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) throw Error();
        const data = await fetchCurriculum(academies[0].id);
        if (!active) return;
        setAcademy(academies[0]);
        setCourses(data.courses);
        setModules(data.modules);
        if (data.courses.length) setCourseId(data.courses[0].id);
        setMessage("");
      } catch {
        if (active) setMessage("Curriculum could not be loaded. Please refresh or contact your administrator.");
      }
    })();
    return () => { active = false; };
  }, []);
  async function mutate(action: () => Promise<Response>, success: string, failure: string, reset?: () => void) {
    if (!academy || pending.current) return;
    pending.current = true;
    setSaving(true);
    setMessage("");
    const uncertain = "The curriculum change could not be confirmed. Your draft has been retained. Check the syllabus register before retrying.";
    try {
      const response = await action();
      if (!response.ok) {
        setMessage(response.status >= 500 ? uncertain : failure);
        return;
      }
      reset?.();
      setMessage(success);
      try {
        const data = await fetchCurriculum(academy.id);
        setCourses(data.courses);
        setModules(data.modules);
        if (!courseId && data.courses.length) setCourseId(data.courses[0].id);
      } catch {
        setMessage(`${success} The syllabus register could not be refreshed; do not repeat the action. Refresh the page to see the latest data.`);
      }
    } catch {
      setMessage(uncertain);
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }
  async function add(event: FormEvent) {
    event.preventDefault();
    if (!academy || pending.current) return;
    if (!courseId)
      return setMessage("Select a course and module title.");
    await mutate(() => academyApi(
      `/api/academies/${academy.id}/course-modules`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          courseId,
          title,
          description: null,
          sequence: Number(sequence),
        }),
      },
    ), "Module saved as draft.", "Module could not be saved. A valid course and module title are required. Your draft has been retained.", () => {
      setTitle("");
      setSequence(String(Number(sequence) + 1));
    });
  }
  async function publish(module: Module) {
    if (!academy) return;
    await mutate(() => academyApi(
      `/api/academies/${academy.id}/course-modules/${module.id}/publication`,
      {
        method: "PATCH",
        headers: apiHeaders(true),
        body: JSON.stringify({ isPublished: !module.isPublished }),
      },
    ), module.isPublished ? "Module returned to draft." : "Module published.", "Publication state could not be updated. Your draft has been retained.");
  }
  const courseName = (id: string) =>
    courses.find((course) => course.id === id)?.name ?? "Course";
  return (
    <main className="enterprise-settings curriculum-standard min-h-screen">
      <WorkspaceNav />
      <div className="curriculum-content mx-auto max-w-6xl px-6 py-10">
        <header className="curriculum-heading">
          <div className="curriculum-title">
            <span className="curriculum-title-icon" aria-hidden="true">
              ♫
            </span>
            <div>
              <p>Academics</p>
              <h1>Curriculum</h1>
            </div>
          </div>
        </header>
        {message && (
          <p role="status" aria-live="polite" className="enterprise-page-state curriculum-message">{message}</p>
        )}
        <section className="curriculum-layout">
          <form onSubmit={add} className="curriculum-panel">
            <header className="curriculum-panel-header">
              <div>
                <p>Curriculum planning</p>
                <h2>Create module draft</h2>
              </div>
            </header>
            <fieldset disabled={!academy || saving} className="curriculum-fields min-w-0 border-0 m-0">
              <StandardSelectField
                name="course"
                disabled={!academy || saving}
                value={courseId}
                onChange={setCourseId}
                placeholder="Select course"
                options={courses.map((course) => ({
                  value: course.id,
                  label: course.name,
                }))}
              />
              <label>
                <span>Module or chapter</span>
                <input
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                  placeholder="Module or chapter"
                  required
                />
              </label>
              <label>
                <span>Sequence</span>
                <input
                  type="number"
                  value={sequence}
                  onChange={(event) => setSequence(event.target.value)}
                  min="1"
                />
              </label>
              <button disabled={!academy || saving} className="enterprise-action-button curriculum-action">
                {saving ? "Saving…" : "Save draft"}
              </button>
            </fieldset>
          </form>
          <section className="curriculum-panel curriculum-summary-panel">
            <header className="curriculum-panel-header">
              <div>
                <p>Approval controls</p>
                <h2>Curriculum review</h2>
              </div>
            </header>
            <div>
              <b>{modules.filter((module) => !module.isPublished).length}</b>
              <span>drafts awaiting review</span>
            </div>
            <div>
              <b>{modules.filter((module) => module.isPublished).length}</b>
              <span>published modules</span>
            </div>
          </section>
        </section>
        <section className="curriculum-panel curriculum-register-panel">
          <header className="curriculum-panel-header">
            <div>
              <p>Curriculum planning</p>
              <h2>Syllabus register</h2>
            </div>
            <span>{modules.length} modules</span>
          </header>
          {modules.length === 0 ? (
            <p className="curriculum-empty">No curriculum modules yet.</p>
          ) : (
            <div className="curriculum-table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Sequence</th>
                    <th>Module</th>
                    <th>Course</th>
                    <th>State</th>
                    <th>Control</th>
                  </tr>
                </thead>
                <tbody>
                  {modules.map((module) => (
                    <tr key={module.id}>
                      <td>{module.sequence}</td>
                      <td>
                        <b>{module.title}</b>
                      </td>
                      <td>{courseName(module.courseId)}</td>
                      <td>
                        <span data-published={module.isPublished}>
                          {module.isPublished ? "Published" : "Draft"}
                        </span>
                      </td>
                      <td>
                        <button
                          type="button"
                          disabled={!academy || saving}
                          onClick={() => void publish(module)}
                        >
                          {module.isPublished
                            ? "Return to draft"
                            : "Approve & publish"}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    </main>
  );
}
