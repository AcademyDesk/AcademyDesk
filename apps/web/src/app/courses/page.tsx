"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import { courseUpdatePayload, type CourseUpdateSource } from "@/lib/course-update";

type Academy = { id: string; name: string };
type Course = CourseUpdateSource;
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
  const saveInFlight = useRef(false);
  function startSaving(id: string) {
    if (saveInFlight.current) return false;
    saveInFlight.current = true;
    setSavingId(id);
    return true;
  }
  function finishSaving() { saveInFlight.current = false; setSavingId(null); }
  async function refreshAfterSave(id: string, notice: string) {
    setMessage(notice);
    try { await loadCourses(id); }
    catch { setMessage(`${notice} The course list could not be refreshed. Refresh the page to see the latest courses.`); }
  }
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
    if (saveInFlight.current) return;
    if (!academyId) return setMessage("Select an academy before creating the course.");
    if (!name.trim()) return setMessage("Course name is required.");
    if (!startSaving("create")) return;
    setMessage("Saving course…");
    const unconfirmed = "The course save could not be confirmed. Your details are still here. Check the course list before trying again.";
    try {
      const response = await academyApi(`/api/academies/${academyId}/courses`, {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({ name, academyType: type, level }),
      });
      if (!response.ok) return setMessage(response.status >= 500 ? unconfirmed : "The course could not be saved.");
      setName("");
      setType("Music");
      setLevel("");
      await refreshAfterSave(academyId, "Course created.");
    } catch { setMessage(unconfirmed); }
    finally { finishSaving(); }
  }
  function beginEdit(course: Course) {
    if (saveInFlight.current) return;
    setEditingId(course.id);
    setEditName(course.name);
    setEditType(course.academyType);
    setEditLevel(course.level ?? "");
  }
  async function saveCourse(course: Course) {
    if (saveInFlight.current || !academyId) return;
    if (!editName.trim()) return setMessage("Course name is required.");
    if (!startSaving(course.id)) return;
    setMessage("Updating course…");
    const unconfirmed = "The course update could not be confirmed. Your details are still here. Check the course list before trying again.";
    try {
      const response = await academyApi(
        `/api/academies/${academyId}/courses/${course.id}`,
        {
          method: "PUT",
          headers: apiHeaders(true),
          body: JSON.stringify(courseUpdatePayload(course, {
            name: editName,
            academyType: editType,
            level: editLevel || null,
          })),
        },
      );
      if (!response.ok) return setMessage(response.status >= 500 ? unconfirmed : "The course could not be updated.");
      setEditingId(null);
      await refreshAfterSave(academyId, "Course updated.");
    } catch { setMessage(unconfirmed); }
    finally { finishSaving(); }
  }
  async function toggleActive(course: Course) {
    if (!academyId || !startSaving(course.id)) return;
    setMessage("Updating course status…");
    const unconfirmed = "The course status update could not be confirmed. Check the course list before trying again.";
    try {
      const response = await academyApi(
        `/api/academies/${academyId}/courses/${course.id}`,
        {
          method: "PUT",
          headers: apiHeaders(true),
          body: JSON.stringify(courseUpdatePayload(course, {
            isActive: !course.isActive,
          })),
        },
      );
      if (!response.ok)
        return setMessage(response.status >= 500 ? unconfirmed : "The course status could not be updated.");
      await refreshAfterSave(academyId,
        course.isActive ? "Course marked inactive." : "Course reactivated.",
      );
    } catch { setMessage(unconfirmed); }
    finally { finishSaving(); }
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
          <p role="status" className="enterprise-page-state courses-message">{message}</p>
        )}
        {academies.length === 0 ? (
          <p className="courses-empty">Create an academy first.</p>
        ) : (
          <>
            <section className="courses-academy">
              <span>Academy</span>
              <StandardSelectField
                disabled={savingId !== null}
                name="academy"
                value={academyId}
                onChange={(id) => { if (!saveInFlight.current) setAcademyId(id); }}
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
                      disabled={savingId !== null}
                      value={name}
                      onChange={(event) => setName(event.target.value)}
                      placeholder="Course or subject name"
                      required
                    />
                  </label>
                  <StandardSelectField
                    disabled={savingId !== null}
                    name="course-type"
                    value={type}
                    onChange={setType}
                    placeholder="Course type"
                    options={typeOptions}
                  />
                  <label>
                    <span>Level</span>
                    <input
                      disabled={savingId !== null}
                      value={level}
                      onChange={(event) => setLevel(event.target.value)}
                      placeholder="Optional level"
                    />
                  </label>
                  <button disabled={savingId !== null} className="enterprise-action-button courses-create-button">
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
                                disabled={savingId !== null}
                                value={editName}
                                onChange={(event) =>
                                  setEditName(event.target.value)
                                }
                              />
                            </label>
                            <StandardSelectField
                              disabled={savingId !== null}
                              name={`course-type-${course.id}`}
                              value={editType}
                              onChange={setEditType}
                              placeholder="Course type"
                              options={typeOptions}
                            />
                            <label>
                              <span>Level</span>
                              <input
                                disabled={savingId !== null}
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
                              disabled={savingId !== null}
                            >
                              Save
                            </button>
                            <button
                              type="button"
                              onClick={() => { if (!saveInFlight.current) setEditingId(null); }}
                              disabled={savingId !== null}
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
                              disabled={savingId !== null}
                            >
                              Edit
                            </button>
                            <button
                              type="button"
                              onClick={() => void toggleActive(course)}
                              disabled={savingId !== null}
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
