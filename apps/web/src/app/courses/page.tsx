"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string };
type Course = {
  id: string;
  name: string;
  academyType: string;
  level?: string | null;
  description?: string | null;
  isActive: boolean;
};
const courseTypes = ["Music", "Tuition", "Coaching"];

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
    const response = await academyApi("/api/academies", { cache: "no-store" });
    if (!response.ok) throw new Error();
    const data: Academy[] = await response.json();
    setAcademies(data);
    if (!academyId && data.length) setAcademyId(data[0].id);
  }
  async function loadCourses(id: string) {
    if (!id) return setCourses([]);
    const response = await academyApi(`/api/academies/${id}/courses`, {
      cache: "no-store",
    });
    if (!response.ok) throw new Error();
    setCourses(await response.json());
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
    const response = await academyApi(`/api/academies/${academyId}/courses`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify({ name, academyType: type, level }),
    });
    if (!response.ok) return setMessage("The course could not be saved.");
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
    const response = await academyApi(
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
    if (!response.ok) return setMessage("The course could not be updated.");
    setEditingId(null);
    setMessage("Course updated.");
    await loadCourses(academyId);
  }
  async function toggleActive(course: Course) {
    setSavingId(course.id);
    const response = await academyApi(
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
    if (!response.ok)
      return setMessage("The course status could not be updated.");
    setMessage(
      course.isActive ? "Course marked inactive." : "Course reactivated.",
    );
    await loadCourses(academyId);
  }
  const typeOptions = courseTypes.map((value) => ({ value, label: value }));
  return (
    <main className="enterprise-settings courses-standard min-h-screen">
      <WorkspaceNav />
      <div className="courses-content mx-auto max-w-6xl px-6 py-10">
        <header className="courses-heading">
          <div className="courses-title">
            <span className="courses-title-icon" aria-hidden="true">
              ♫
            </span>
            <div>
              <p>Academics</p>
              <h1>Courses</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state courses-message">{message}</p>
        )}
        {academies.length === 0 ? (
          <p className="courses-empty">Create an academy first.</p>
        ) : (
          <>
            <section className="courses-academy">
              <span>Academy</span>
              <StandardSelectField
                name="academy"
                value={academyId}
                onChange={setAcademyId}
                placeholder="Select academy"
                options={academies.map((academy) => ({
                  value: academy.id,
                  label: academy.name,
                }))}
              />
            </section>
            <section className="courses-layout">
              <form onSubmit={createCourse} className="courses-panel">
                <header className="courses-panel-header">
                  <div>
                    <p>Catalogue</p>
                    <h2>Add course</h2>
                  </div>
                </header>
                <div className="courses-fields">
                  <label>
                    <span>Course or subject name</span>
                    <input
                      value={name}
                      onChange={(event) => setName(event.target.value)}
                      placeholder="Course or subject name"
                      required
                    />
                  </label>
                  <StandardSelectField
                    name="course-type"
                    value={type}
                    onChange={setType}
                    placeholder="Course type"
                    options={typeOptions}
                  />
                  <label>
                    <span>Level</span>
                    <input
                      value={level}
                      onChange={(event) => setLevel(event.target.value)}
                      placeholder="Optional level"
                    />
                  </label>
                  <button className="enterprise-action-button courses-create-button">
                    Create course
                  </button>
                </div>
              </form>
              <section className="courses-panel courses-list-panel">
                <header className="courses-panel-header">
                  <div>
                    <p>Catalogue</p>
                    <h2>Courses</h2>
                  </div>
                  <span>
                    {courses.filter((course) => course.isActive).length} active
                  </span>
                </header>
                {courses.length === 0 ? (
                  <p className="courses-empty">No courses yet.</p>
                ) : (
                  <ul>
                    {courses.map((course) =>
                      editingId === course.id ? (
                        <li key={course.id} className="courses-edit-row">
                          <div className="courses-edit-fields">
                            <label>
                              <span>Course name</span>
                              <input
                                value={editName}
                                onChange={(event) =>
                                  setEditName(event.target.value)
                                }
                              />
                            </label>
                            <StandardSelectField
                              name={`course-type-${course.id}`}
                              value={editType}
                              onChange={setEditType}
                              placeholder="Course type"
                              options={typeOptions}
                            />
                            <label>
                              <span>Level</span>
                              <input
                                value={editLevel}
                                onChange={(event) =>
                                  setEditLevel(event.target.value)
                                }
                                placeholder="Level"
                              />
                            </label>
                          </div>
                          <div className="courses-actions">
                            <button
                              type="button"
                              onClick={() => void saveCourse(course)}
                              disabled={savingId === course.id}
                            >
                              Save
                            </button>
                            <button
                              type="button"
                              onClick={() => setEditingId(null)}
                            >
                              Cancel
                            </button>
                          </div>
                        </li>
                      ) : (
                        <li key={course.id}>
                          <div>
                            <b>{course.name}</b>
                            <small>
                              {course.academyType}
                              {course.level ? ` · ${course.level}` : ""}
                            </small>
                          </div>
                          <div className="courses-row-actions">
                            <span data-active={course.isActive}>
                              {course.isActive ? "Active" : "Inactive"}
                            </span>
                            <button
                              type="button"
                              onClick={() => beginEdit(course)}
                            >
                              Edit
                            </button>
                            <button
                              type="button"
                              onClick={() => void toggleActive(course)}
                              disabled={savingId === course.id}
                            >
                              {course.isActive ? "Deactivate" : "Reactivate"}
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
  );
}
