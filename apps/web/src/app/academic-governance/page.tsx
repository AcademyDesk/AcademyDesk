"use client";

import Link from "next/link";
import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Course = { id: string; name: string };
type Scheme = { id: string; name: string; passingPercent: number; bandsJson: string; isActive: boolean };
type Prerequisite = { id: string; courseId: string; requiredCourseId: string };
type Recovery = "" | "unconfirmed" | "confirmed" | "reviewed";
class GovernanceReadError extends Error { constructor(readonly status: number) { super("Academic governance could not be loaded."); } }
const guidance = (status: number) => status === 401 ? " Sign in again." : status === 403 ? " This account does not have academic administration access to the required records." : status === 429 ? " Please wait before trying again." : " Reload governance records before trying again.";

export default function AcademicGovernancePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [courses, setCourses] = useState<Course[]>([]), [schemes, setSchemes] = useState<Scheme[]>([]), [prerequisites, setPrerequisites] = useState<Prerequisite[]>([]);
  const [notice, setNotice] = useState("Loading academic governance…"), [problem, setProblem] = useState(false);
  const [saving, setSaving] = useState(false), [ready, setReady] = useState(false), [loading, setLoading] = useState(true);
  const [courseId, setCourseId] = useState(""), [requiredCourseId, setRequiredCourseId] = useState("");
  const [recovery, setRecovery] = useState<Recovery>(""), [newRule, setNewRule] = useState(true), [intent, setIntent] = useState(0);
  const alive = useRef(true), reading = useRef(false), pending = useRef(false), readReady = useRef(false);
  const readVersion = useRef(0), intentVersion = useRef(0), recoveryState = useRef<Recovery>("");
  const currentCourses = useRef<Course[]>([]), currentSchemes = useRef<Scheme[]>([]);
  const canWrite = ready && !loading && !saving && !recovery, visibleIntent = intent;
  function notify(value: string, failed = false) { if (alive.current) { setNotice(value); setProblem(failed); } }
  function invalidateIntent() { ++intentVersion.current; setIntent(intentVersion.current); }
  function recoverAs(value: Recovery) { recoveryState.current = value; if (alive.current) setRecovery(value); }
  async function load(id?: string) {
    if (!alive.current) return false;
    const version = ++readVersion.current;
    readReady.current = false; setReady(false); setLoading(true);
    try {
      if (!id) {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new GovernanceReadError(response.status);
        const rows: Academy[] = await response.json();
        if (!Array.isArray(rows) || !rows.length || !rows.every(row => row && typeof row.id === "string")) throw Error("No academy context");
        if (!alive.current || readVersion.current !== version) return false;
        id = rows[0].id; setAcademy(rows[0]);
      }
      const responses = await Promise.all([academyApi(`/api/academies/${id}/courses`, { cache: "no-store" }), academyApi(`/api/academies/${id}/academic-governance/grading-schemes`, { cache: "no-store" }), academyApi(`/api/academies/${id}/academic-governance/prerequisites`, { cache: "no-store" })]);
      const failure = responses.find(response => !response.ok);
      if (failure) throw new GovernanceReadError(failure.status);
      const [cs, ss, ps]: [Course[], Scheme[], Prerequisite[]] = await Promise.all([responses[0].json(), responses[1].json(), responses[2].json()]);
      if (!Array.isArray(cs) || !cs.every(row => row && typeof row.id === "string" && typeof row.name === "string")
        || !Array.isArray(ss) || !ss.every(row => row && typeof row.id === "string" && typeof row.name === "string" && typeof row.passingPercent === "number" && Number.isFinite(row.passingPercent) && typeof row.bandsJson === "string" && typeof row.isActive === "boolean")
        || !Array.isArray(ps) || !ps.every(row => row && typeof row.id === "string" && typeof row.courseId === "string" && typeof row.requiredCourseId === "string")) throw Error("Invalid governance records");
      if (!alive.current || readVersion.current !== version) return false;
      currentCourses.current = cs; currentSchemes.current = ss;
      setCourses(cs); setSchemes(ss); setPrerequisites(ps); invalidateIntent(); readReady.current = true; setReady(true); return true;
    } catch (error) { if (!alive.current || readVersion.current !== version) return false; throw error; }
    finally { if (alive.current && readVersion.current === version) setLoading(false); }
  }
  async function reload() {
    if (!alive.current || reading.current || pending.current) return;
    reading.current = true; const version = readVersion.current + 1;
    try {
      if (!await load() || !alive.current || readVersion.current !== version) return;
      if (recoveryState.current === "unconfirmed" || recoveryState.current === "reviewed") { recoverAs("reviewed"); notify("Records refreshed. The earlier result remains unconfirmed. Review the registers before starting a new action.", true); }
      else if (recoveryState.current === "confirmed") { recoverAs(""); notify("The earlier change was confirmed. Governance records refreshed."); }
      else notify("");
    } catch (error) { notify((recoveryState.current === "confirmed" ? "The earlier change was confirmed. " : recoveryState.current ? "The earlier result remains unconfirmed. " : "") + "Academic governance could not be loaded." + guidance(error instanceof GovernanceReadError ? error.status : 503), true); }
    finally { if (readVersion.current === version) reading.current = false; }
  }
  const initialLoad = useRef(reload);
  useEffect(() => { alive.current = true; void initialLoad.current(); return () => { alive.current = false; readReady.current = false; ++readVersion.current; reading.current = false; }; }, []);
  function eligible(version: number) { return alive.current && !!academy && readReady.current && !reading.current && !pending.current && !recoveryState.current && intentVersion.current === version; }
  function begin(version: number) { if (!eligible(version)) return false; pending.current = true; invalidateIntent(); setSaving(true); return true; }
  async function mutate(url: string, method: "POST" | "PATCH", body: object, confirmation: string, afterConfirmation?: () => void, expected?: { id: string; isActive: boolean }) {
    let confirmed = false;
    try {
      const response = await academyApi(url, { method, headers: apiHeaders(true), body: JSON.stringify(body) });
      if (!alive.current) return;
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408) throw Error("Unconfirmed change");
        if ([401, 403, 409].includes(response.status)) { readReady.current = false; setReady(false); }
        const detail = await response.json().catch(() => null);
        notify((typeof detail?.message === "string" ? detail.message : "Governance change was not saved.") + guidance(response.status), true); return;
      }
      // Existing POST contracts acknowledge with an empty200 body. Lifecycle
      // PATCH has an exact id/state receipt and must match the reviewed action.
      if (expected) { const receipt = await response.json(); if (!receipt || receipt.id !== expected.id || receipt.isActive !== expected.isActive) throw Error("Invalid confirmation"); }
      if (!alive.current) return;
      confirmed = true; afterConfirmation?.();
      await load(academy!.id);
      if (alive.current) notify(confirmation);
    } catch {
      if (alive.current) {
        readReady.current = false; setReady(false); setNewRule(method === "POST"); recoverAs(confirmed ? "confirmed" : "unconfirmed");
        notify(confirmed ? `${confirmation} The change was confirmed, but governance records could not be refreshed. Reload governance records before making another change.` : "Governance change could not be confirmed. Reload governance records and review the registers before starting a new action.", true);
      }
    } finally { pending.current = false; if (alive.current) setSaving(false); }
  }
  async function submit(event: FormEvent<HTMLFormElement>, path: "grading-schemes" | "prerequisites", version: number) {
    event.preventDefault();
    if (!eligible(version) || !academy) return;
    const submittedForm = event.currentTarget, data = new FormData(submittedForm);
    let body: object;
    if (path === "grading-schemes") {
      const name = String(data.get("name") ?? "").trim(), rawPercent = String(data.get("passingPercent") ?? "").trim(), passingPercent = Number(rawPercent), bandsJson = String(data.get("bandsJson") || "[]");
      if (!name || !rawPercent || !Number.isFinite(passingPercent) || passingPercent < 0 || passingPercent > 100) return notify("Provide a scheme name and passing percentage from0 to100.", true);
      try { JSON.parse(bandsJson); } catch { return notify("Grade bands must be valid JSON.", true); }
      body = { name, passingPercent, bandsJson };
    } else {
      if (!courseId || !requiredCourseId || courseId === requiredCourseId || ![courseId, requiredCourseId].every(id => currentCourses.current.some(row => row.id === id))) return notify("Select two different available courses for the prerequisite.", true);
      body = { courseId, requiredCourseId };
    }
    if (!begin(version)) return;
    return mutate(`/api/academies/${academy.id}/academic-governance/${path}`, "POST", body, path === "grading-schemes" ? "Grading scheme created." : "Course prerequisite saved.", () => { submittedForm.reset(); if (path === "prerequisites") { setCourseId(""); setRequiredCourseId(""); } });
  }
  async function toggleScheme(item: Scheme, version: number) {
    if (!currentSchemes.current.some(row => row.id === item.id && row.isActive === item.isActive) || !begin(version) || !academy) return;
    const expected = { id: item.id, isActive: !item.isActive };
    return mutate(`/api/academies/${academy.id}/grading-schemes/${item.id}/status`, "PATCH", { isActive: expected.isActive }, item.isActive ? "Grading scheme made inactive." : "Grading scheme activated.", undefined, expected);
  }
  function startNewAction() { if (!alive.current || !readReady.current || reading.current || pending.current || recoveryState.current !== "reviewed") return; invalidateIntent(); recoverAs(""); notify("Ready for a new action. Review retained form values before saving."); }
  const courseName = (id: string) => courses.find(item => item.id === id)?.name ?? "Course unavailable";
  const courseOptions = courses.map(item => ({ value: item.id, label: item.name }));
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
          <p className="enterprise-page-state governance-message" role={problem ? "alert" : "status"}>
            {notice}
          </p>
        )}
        {!ready && !loading && !recovery && <button type="button" className="enterprise-action-button" onClick={() => void reload()}>Try again</button>}
        {recovery && <button type="button" className="enterprise-action-button" disabled={loading || saving} onClick={() => void reload()}>Reload governance records</button>}
        {ready && recovery === "reviewed" && <button type="button" className="enterprise-action-button" onClick={startNewAction}>{newRule ? "Start a new rule" : "Continue after review"}</button>}
        {ready && !loading && <section className="governance-tiles">
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
        </section>}
        <section className="governance-form-grid">
          <form
            onSubmit={(event) => void submit(event, "grading-schemes", visibleIntent)}
            className="governance-panel"
          >
            <header className="governance-panel-header">
              <div>
                <p>Assessment policy</p>
                <h2>Create grading scheme</h2>
              </div>
            </header>
            <fieldset className="governance-fields" disabled={!canWrite} style={{ minWidth: 0, margin: 0, border: 0 }}>
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
                disabled={!canWrite}
                className="enterprise-action-button governance-action"
              >
                Create scheme
              </button>
            </fieldset>
          </form>
          <form
            onSubmit={(event) => void submit(event, "prerequisites", visibleIntent)}
            className="governance-panel"
          >
            <header className="governance-panel-header">
              <div>
                <p>Course rules</p>
                <h2>Add course prerequisite</h2>
              </div>
            </header>
            <fieldset className="governance-fields" disabled={!canWrite} style={{ minWidth: 0, margin: 0, border: 0 }}>
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
                disabled={!canWrite}
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
            {!ready || loading ? <p className="governance-empty">Grading schemes are unavailable until records load.</p> : schemes.length === 0 ? (
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
                      disabled={!canWrite}
                      type="button"
                      onClick={() => void toggleScheme(item, visibleIntent)}
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
            {!ready || loading ? <p className="governance-empty">Prerequisites are unavailable until records load.</p> : prerequisites.length === 0 ? (
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
