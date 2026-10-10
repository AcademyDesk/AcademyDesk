"use client";

import Link from "next/link";
import { FormEvent, useEffect, useRef, useState } from "react";
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

async function fetchGovernance(id: string) {
  const responses = await Promise.all([
    academyApi(`/api/academies/${id}/courses`, { cache: "no-store" }),
    academyApi(`/api/academies/${id}/academic-governance/grading-schemes`, { cache: "no-store" }),
    academyApi(`/api/academies/${id}/academic-governance/prerequisites`, { cache: "no-store" }),
  ]);
  if (!responses.every((response) => response.ok)) throw new Error();
  const [courses, schemes, prerequisites] = await Promise.all(responses.map((response) => response.json()));
  if (![courses, schemes, prerequisites].every(Array.isArray)) throw new Error();
  return { courses, schemes, prerequisites };
}

export default function AcademicGovernancePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [courses, setCourses] = useState<Course[]>([]);
  const [schemes, setSchemes] = useState<Scheme[]>([]);
  const [prerequisites, setPrerequisites] = useState<Prerequisite[]>([]);
  const [notice, setNotice] = useState("Loading academic governance…");
  const [saving, setSaving] = useState(false);
  const [courseId, setCourseId] = useState("");
  const [requiredCourseId, setRequiredCourseId] = useState("");
  const pending = useRef(false);
  function applyGovernance(data: Awaited<ReturnType<typeof fetchGovernance>>) {
    setCourses(data.courses);
    setSchemes(data.schemes);
    setPrerequisites(data.prerequisites);
  }
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!Array.isArray(academies) || !academies[0]?.id) throw new Error();
        const data = await fetchGovernance(academies[0].id);
        if (!active) return;
        applyGovernance(data);
        setAcademy(academies[0]);
        setNotice("");
      } catch {
        if (active) setNotice("Academic governance could not be loaded. Please refresh or check your access.");
      }
    })();
    return () => { active = false; };
  }, []);
  async function mutate(request: () => Promise<Response>, success: string, reset?: () => void) {
    if (!academy || pending.current) return;
    pending.current = true;
    setSaving(true);
    setNotice("");
    try {
      const response = await request();
      if (!response.ok) {
        if (response.status >= 500) throw new Error();
        const error = await response.json().catch(() => null);
        setNotice(typeof error?.message === "string" ? error.message : "Governance change was not accepted. Your draft has been retained.");
        return;
      }
      reset?.();
      setNotice(success);
      try { applyGovernance(await fetchGovernance(academy.id)); }
      catch { setNotice(`${success} The register could not be refreshed. Do not repeat the action; refresh to check the saved record.`); }
    } catch {
      setNotice("The result could not be confirmed. Your draft has been retained. Check the register before trying again.");
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }
  async function submit(
    event: FormEvent<HTMLFormElement>,
    path: "grading-schemes" | "prerequisites",
  ) {
    event.preventDefault();
    if (!academy || pending.current) return;
    const form = event.currentTarget;
    const data = new FormData(form);
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
    await mutate(() => academyApi(
        `/api/academies/${academy.id}/academic-governance/${path}`,
        {
          method: "POST",
          headers: apiHeaders(true),
          body: JSON.stringify(body),
        },
      ), path === "grading-schemes" ? "Grading scheme created." : "Course prerequisite saved.", () => {
      form.reset();
      if (path === "prerequisites") {
        setCourseId("");
        setRequiredCourseId("");
      }
    });
  }
  async function toggleScheme(item: Scheme) {
    if (!academy || pending.current) return;
    await mutate(() => academyApi(
        `/api/academies/${academy.id}/grading-schemes/${item.id}/status`,
        {
          method: "PATCH",
          headers: apiHeaders(true),
          body: JSON.stringify({ isActive: !item.isActive }),
        },
      ),
        item.isActive
          ? "Grading scheme made inactive."
          : "Grading scheme activated.",
      );
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
          <p className="enterprise-page-state governance-message" role="status" aria-live="polite">
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
            <fieldset disabled={saving || !academy} className="governance-fields m-0 min-w-0 border-0">
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
            </fieldset>
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
            <fieldset disabled={saving || !academy} className="governance-fields m-0 min-w-0 border-0">
              <StandardSelectField
                name="courseId"
                disabled={saving || !academy}
                value={courseId}
                onChange={setCourseId}
                placeholder="Course to unlock"
                options={courseOptions}
              />
              <StandardSelectField
                name="requiredCourseId"
                disabled={saving || !academy}
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
            </fieldset>
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
