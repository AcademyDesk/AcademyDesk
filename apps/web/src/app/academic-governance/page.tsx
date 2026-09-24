"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Course = { id: string; name: string };
type Scheme = {
  id: string;
  name: string;
  passingPercent: number;
  bandsJson: string;
  isActive: boolean;
};
type Prerequisite = { id: string; courseId: string; requiredCourseId: string };

export default function AcademicGovernancePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [courses, setCourses] = useState<Course[]>([]);
  const [schemes, setSchemes] = useState<Scheme[]>([]);
  const [prerequisites, setPrerequisites] = useState<Prerequisite[]>([]);
  const [notice, setNotice] = useState("Loading academic governance…");
  const [saving, setSaving] = useState(false);
  const [courseId, setCourseId] = useState("");
  const [requiredCourseId, setRequiredCourseId] = useState("");
  async function load(id?: string) {
    try {
      const academyId = id ?? academy?.id;
      if (!academyId) return;
      const [courseResponse, schemeResponse, prerequisiteResponse] =
        await Promise.all([
          academyApi(`/api/academies/${academyId}/courses`),
          academyApi(
            `/api/academies/${academyId}/academic-governance/grading-schemes`,
          ),
          academyApi(
            `/api/academies/${academyId}/academic-governance/prerequisites`,
          ),
        ]);
      if (
        ![courseResponse, schemeResponse, prerequisiteResponse].every(
          (response) => response.ok,
        )
      )
        throw new Error();
      setCourses(await courseResponse.json());
      setSchemes(await schemeResponse.json());
      setPrerequisites(await prerequisiteResponse.json());
      setNotice("");
    } catch {
      setNotice(
        "Academic governance could not be loaded. Confirm that the API is running and that you have academic administration access.",
      );
    }
  }
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0])
          return setNotice(
            "Create an academy and courses before configuring governance.",
          );
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setNotice(
          "Academic governance could not be loaded. Please sign in and restart the API if needed.",
        );
      }
    })();
  }, []);
  async function submit(
    event: FormEvent<HTMLFormElement>,
    path: "grading-schemes" | "prerequisites",
  ) {
    event.preventDefault();
    if (!academy) return;
    const data = new FormData(event.currentTarget);
    if (path === "prerequisites" && (!courseId || !requiredCourseId))
      return setNotice("Select both courses for the prerequisite.");
    const body =
      path === "grading-schemes"
        ? {
            name: data.get("name"),
            passingPercent: Number(data.get("passingPercent")),
            bandsJson: data.get("bandsJson") || "[]",
          }
        : { courseId, requiredCourseId };
    setSaving(true);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/academic-governance/${path}`,
        {
          method: "POST",
          headers: apiHeaders(true),
          body: JSON.stringify(body),
        },
      );
      if (!response.ok) {
        const error = await response.json().catch(() => null);
        throw new Error(
          error?.message || "Governance rule could not be saved.",
        );
      }
      event.currentTarget.reset();
      if (path === "prerequisites") {
        setCourseId("");
        setRequiredCourseId("");
      }
      setNotice(
        path === "grading-schemes"
          ? "Grading scheme created."
          : "Course prerequisite saved.",
      );
      await load();
    } catch (error) {
      setNotice(
        error instanceof Error
          ? error.message
          : "Governance rule could not be saved.",
      );
    } finally {
      setSaving(false);
    }
  }
  async function toggleScheme(item: Scheme) {
    if (!academy) return;
    setSaving(true);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/grading-schemes/${item.id}/status`,
        {
          method: "PATCH",
          headers: apiHeaders(true),
          body: JSON.stringify({ isActive: !item.isActive }),
        },
      );
      if (!response.ok) throw new Error();
      setNotice(
        item.isActive
          ? "Grading scheme made inactive."
          : "Grading scheme activated.",
      );
      await load();
    } catch {
      setNotice("Scheme status could not be updated.");
    } finally {
      setSaving(false);
    }
  }
  const courseName = (id: string) =>
    courses.find((item) => item.id === id)?.name ?? "Course unavailable";
  const courseOptions = courses.map((item) => ({
    value: item.id,
    label: item.name,
  }));
  return (
    <main className="enterprise-settings governance-standard min-h-screen">
      <WorkspaceNav />
      <div className="governance-content mx-auto max-w-6xl px-6 py-10">
        <header className="governance-heading">
          <div className="governance-title">
            <span className="governance-title-icon" aria-hidden="true">
              ♫
            </span>
            <div>
              <p>Academics</p>
              <h1>Academic Governance</h1>
            </div>
          </div>
          <nav>
            <Link href="/academic-periods">Periods</Link>
            <Link href="/curriculum">Curriculum</Link>
            <Link href="/batch-promotions">Promotions</Link>
          </nav>
        </header>
        {notice && (
          <p className="enterprise-page-state governance-message" role="status">
            {notice}
          </p>
        )}
        <section className="governance-tiles">
          <article>
            <span>Active grading schemes</span>
            <b>{schemes.filter((item) => item.isActive).length}</b>
          </article>
          <article>
            <span>Course prerequisites</span>
            <b>{prerequisites.length}</b>
          </article>
          <article>
            <span>Governed courses</span>
            <b>{courses.length}</b>
          </article>
        </section>
        <section className="governance-form-grid">
          <form
            onSubmit={(event) => void submit(event, "grading-schemes")}
            className="governance-panel"
          >
            <header className="governance-panel-header">
              <div>
                <p>Assessment policy</p>
                <h2>Create grading scheme</h2>
              </div>
            </header>
            <div className="governance-fields">
              <label>
                <span>Scheme name</span>
                <input
                  required
                  name="name"
                  placeholder="Performance grade bands"
                />
              </label>
              <label>
                <span>Passing percentage</span>
                <input
                  required
                  name="passingPercent"
                  type="number"
                  min="0"
                  max="100"
                  placeholder="Passing percentage"
                />
              </label>
              <label>
                <span>Grade bands</span>
                <textarea name="bandsJson" defaultValue="[]" />
              </label>
              <button
                disabled={saving}
                className="enterprise-action-button governance-action"
              >
                Create scheme
              </button>
            </div>
          </form>
          <form
            onSubmit={(event) => void submit(event, "prerequisites")}
            className="governance-panel"
          >
            <header className="governance-panel-header">
              <div>
                <p>Course rules</p>
                <h2>Add course prerequisite</h2>
              </div>
            </header>
            <div className="governance-fields">
              <StandardSelectField
                name="courseId"
                value={courseId}
                onChange={setCourseId}
                placeholder="Course to unlock"
                options={courseOptions}
              />
              <StandardSelectField
                name="requiredCourseId"
                value={requiredCourseId}
                onChange={setRequiredCourseId}
                placeholder="Required completed course"
                options={courseOptions}
              />
              <button
                disabled={saving}
                className="enterprise-action-button governance-action"
              >
                Save prerequisite
              </button>
            </div>
          </form>
        </section>
        <section className="governance-register-grid">
          <section className="governance-panel">
            <header className="governance-panel-header">
              <div>
                <p>Assessment policy</p>
                <h2>Grading-scheme register</h2>
              </div>
            </header>
            {schemes.length === 0 ? (
              <p className="governance-empty">No grading schemes configured.</p>
            ) : (
              <ul>
                {schemes.map((item) => (
                  <li key={item.id}>
                    <div>
                      <b>{item.name}</b>
                      <small>
                        Pass threshold: {item.passingPercent}% ·{" "}
                        {item.isActive ? "Active" : "Inactive"}
                      </small>
                      <pre>{item.bandsJson}</pre>
                    </div>
                    <button
                      disabled={saving}
                      type="button"
                      onClick={() => void toggleScheme(item)}
                    >
                      {item.isActive ? "Deactivate" : "Activate"}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </section>
          <section className="governance-panel">
            <header className="governance-panel-header">
              <div>
                <p>Course rules</p>
                <h2>Prerequisite register</h2>
              </div>
            </header>
            {prerequisites.length === 0 ? (
              <p className="governance-empty">
                No course prerequisites configured.
              </p>
            ) : (
              <ul>
                {prerequisites.map((item) => (
                  <li key={item.id}>
                    <div>
                      <b>{courseName(item.courseId)}</b>
                      <small>
                        Requires completed course:{" "}
                        {courseName(item.requiredCourseId)}
                      </small>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
