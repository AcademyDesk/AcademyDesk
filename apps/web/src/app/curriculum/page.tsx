"use client";

import { FormEvent, useEffect, useState } from "react";
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

export default function Curriculum() {
  const [academy, setAcademy] = useState<Academy>();
  const [courses, setCourses] = useState<Course[]>([]);
  const [modules, setModules] = useState<Module[]>([]);
  const [courseId, setCourseId] = useState("");
  const [title, setTitle] = useState("");
  const [sequence, setSequence] = useState("1");
  const [message, setMessage] = useState("Loading curriculum governance…");
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const [courseResponse, moduleResponse] = await Promise.all([
      academyApi(`/api/academies/${academyId}/courses`),
      academyApi(`/api/academies/${academyId}/course-modules`),
    ]);
    if (!courseResponse.ok || !moduleResponse.ok) throw Error();
    const courseRows: Course[] = await courseResponse.json();
    setCourses(courseRows);
    setModules(await moduleResponse.json());
    if (!courseId && courseRows.length) setCourseId(courseRows[0].id);
    setMessage("");
  }
  useEffect(() => {
    void (async () => {
      try {
        const academies: Academy[] = await (
          await academyApi("/api/academies")
        ).json();
        if (!academies[0]) throw Error();
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setMessage("Curriculum could not be loaded.");
      }
    })();
  }, []);
  async function add(event: FormEvent) {
    event.preventDefault();
    if (!academy || !courseId)
      return setMessage("Select a course and module title.");
    const response = await academyApi(
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
    );
    if (!response.ok)
      return setMessage("Valid course and module title required.");
    setTitle("");
    setSequence(String(Number(sequence) + 1));
    setMessage("Module saved as draft.");
    await load();
  }
  async function publish(module: Module) {
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/course-modules/${module.id}/publication`,
      {
        method: "PATCH",
        headers: apiHeaders(true),
        body: JSON.stringify({ isPublished: !module.isPublished }),
      },
    );
    if (!response.ok)
      return setMessage("Publication state could not be updated.");
    setMessage(
      module.isPublished
        ? "Module returned to draft."
        : "Module approved and published.",
    );
    await load();
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
          <p className="enterprise-page-state curriculum-message">{message}</p>
        )}
        <section className="curriculum-layout">
          <form onSubmit={add} className="curriculum-panel">
            <header className="curriculum-panel-header">
              <div>
                <p>Curriculum planning</p>
                <h2>Create module draft</h2>
              </div>
            </header>
            <div className="curriculum-fields">
              <StandardSelectField
                name="course"
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
              <button className="enterprise-action-button curriculum-action">
                Save draft
              </button>
            </div>
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
