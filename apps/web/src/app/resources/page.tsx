"use client";

import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import visual from "@/components/design-system/learning-workspace.module.css";

type Academy = { id: string };
type Batch = { id: string; name: string };
type Course = { id: string; name: string; subjectArea?: string };
type Resource = {
  id: string;
  title: string;
  type: string;
  url: string;
  batchId?: string;
  courseId?: string;
};
const resourceTypes = [
  "Document",
  "Book",
  "SheetMusic",
  "Audio",
  "Video",
  "Link",
];
export default function Resources() {
  const [academy, setAcademy] = useState<Academy>();
  const [batches, setBatches] = useState<Batch[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [resources, setResources] = useState<Resource[]>([]);
  const [title, setTitle] = useState("");
  const [url, setUrl] = useState("");
  const [type, setType] = useState("Document");
  const [course, setCourse] = useState("");
  const [batch, setBatch] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [message, setMessage] = useState("Loading resources…");
  const input = useRef<HTMLInputElement>(null);
  const [ready, setReady] = useState(false);
  const [saving, setSaving] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const guard = useRef({ ready: false, pending: false, review: false, snapshot: 0, draft: 0 });
  const mounted = useRef(false);
  const snapshot = guard.current.snapshot;
  const blocked = !ready || saving || needsReview;
  function changeDraft() { guard.current.draft++; }
  function requireReview() { guard.current.review = true; setNeedsReview(true); }
  const load = useCallback(async (academyId: string) => {
    const [list, batchList, courseList] = await Promise.all([
      academyApi(`/api/academies/${academyId}/resources`),
      academyApi(`/api/academies/${academyId}/batches`),
      academyApi(`/api/academies/${academyId}/courses`),
    ]);
    if (!list.ok || !batchList.ok || !courseList.ok) throw new Error();
    const [resourceRows, batchRows, courseRows] = await Promise.all([list.json(), batchList.json(), courseList.json()]);
    if (![resourceRows, batchRows, courseRows].every(Array.isArray)) throw new Error();
    if (!mounted.current) return;
    setResources(resourceRows); setBatches(batchRows); setCourses(courseRows);
    guard.current.ready = true; guard.current.snapshot++; setReady(true);
  }, []);
  useEffect(() => {
    mounted.current = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        const accounts: Academy[] = await response.json();
        if (!response.ok || !Array.isArray(accounts) || !accounts[0]?.id) throw new Error();
        if (!mounted.current) return;
        setAcademy(accounts[0]);
        await load(accounts[0].id);
        if (mounted.current) setMessage("");
      } catch {
        if (mounted.current) setMessage("Resources could not be loaded.");
      }
    })();
    return () => { mounted.current = false; };
  }, [load]);
  async function refreshRegister() {
    if (!academy || guard.current.pending) return;
    guard.current.pending = true; setSaving(true);
    try { await load(academy.id); if (mounted.current) { guard.current.review = false; setNeedsReview(false); setMessage("Resource register refreshed. Check for the previous resource before publishing again."); } }
    catch { if (mounted.current) { requireReview(); setMessage("The resource register could not be refreshed. Do not repeat an unconfirmed publish."); } }
    finally { guard.current.pending = false; if (mounted.current) setSaving(false); }
  }
  async function create(event: FormEvent) {
    event.preventDefault();
    if (!mounted.current || !academy || !guard.current.ready || guard.current.pending || guard.current.review || snapshot !== guard.current.snapshot) return;
    if (!title.trim() || (!file && !url.trim()) || !resourceTypes.includes(type) || (course && !courses.some(item => item.id === course)) || (batch && !batches.some(item => item.id === batch))) { setMessage("Provide a title, a link or file, and valid resource options."); return; }
    const captured = { title, url, file, type, course, batch, draft: guard.current.draft };
    guard.current.pending = true; setSaving(true);
    setMessage("Publishing resource…");
    let confirmed = false;
    try {
    let response: Response;
    if (file) {
      const body = new FormData();
      body.append("file", file);
      body.append("title", title);
      body.append("type", type);
      if (course) body.append("courseId", course);
      if (batch) body.append("batchId", batch);
      response = await academyApi(
        `/api/academies/${academy.id}/resources/upload`,
        { method: "POST", body },
      );
    } else
      response = await academyApi(`/api/academies/${academy.id}/resources`, {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          title,
          description: null,
          type,
          url,
          batchId: batch || null,
          courseId: course || null,
          isPublished: true,
        }),
      });
    if (!response.ok) {
      if (!mounted.current) return;
      if (response.status >= 500 || response.status === 408) { requireReview(); setMessage("Resource publish could not be confirmed. Your draft is retained. Refresh and check the register before retrying."); return; }
      const result = await response.json().catch(() => null);
      if (mounted.current) setMessage(typeof result?.message === "string" && result.message.trim() ? result.message : "The resource could not be published. Check the resource details and your access.");
      return;
    }
    confirmed = true;
    if (!mounted.current) return;
    if (guard.current.draft === captured.draft) { setTitle(current => current === captured.title ? "" : current); setUrl(current => current === captured.url ? "" : current); setFile(current => current === captured.file ? null : current); if (input.current) input.current.value = ""; }
    setMessage("Resource published.");
    await load(academy.id);
    } catch { if (mounted.current) { requireReview(); setMessage(confirmed ? "Resource published. The register could not be refreshed; do not repeat this action. Refresh the resource register to continue." : "Resource publish could not be confirmed. Your draft is retained. Refresh and check the register before retrying."); } }
    finally { guard.current.pending = false; if (mounted.current) setSaving(false); }
  }
  const subject = (id?: string) =>
    courses.find((value) => value.id === id)?.name ?? "All subjects";
  const audience = (id?: string) =>
    batches.find((value) => value.id === id)?.name ?? "All students";
  return (
    <main className={`enterprise-settings resources-standard min-h-screen ${visual.page}`}>
      <WorkspaceNav />
      <div className="resources-content">
        <header className="standard-workspace-heading">
          <span className="standard-heading-icon" aria-hidden="true">
            ▤
          </span>
          <div>
            <p>Academy experience</p>
            <h1>Learning resources</h1>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state" role="status">
            {message}
          </p>
        )}
        {academy && <button type="button" className="enterprise-action-button" disabled={saving} onClick={refreshRegister}>Refresh resource register</button>}
        <section className="resources-grid">
          <form onChangeCapture={changeDraft} onSubmit={create} aria-busy={saving} className="surface-panel resources-panel">
            <div className="standard-panel-heading">
              <p>Library</p>
              <h2>Add resource</h2>
            </div>
            <fieldset disabled={blocked} className={visual.fields}><label>
              Resource title
              <input
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                required
              />
            </label>
            <label>
              Web link
              <input
                value={url}
                disabled={!!file}
                onChange={(event) => setUrl(event.target.value)}
                placeholder="https://"
              />
            </label>
            <label className="resource-file">
              <span>Upload file</span>
              <input
                ref={input}
                type="file"
                accept=".pdf,.doc,.docx,image/*,audio/*,video/*"
                onChange={(event) => {
                  setFile(event.target.files?.[0] ?? null);
                  setUrl("");
                }}
              />
              <small>{file?.name ?? "No file selected"}</small>
            </label>
            <div>
              <StandardSelectField
                name="type"
                disabled={blocked}
                label="Type"
                value={type}
                onChange={value => { changeDraft(); setType(value); }}
                placeholder="Select type"
                options={resourceTypes.map((value) => ({
                  value,
                  label: value === "SheetMusic" ? "Sheet music" : value,
                }))}
              />
            </div>
            <div>
              <StandardSelectField
                name="course"
                disabled={blocked}
                label="Subject (optional)"
                value={course}
                onChange={value => { changeDraft(); setCourse(value); }}
                placeholder="All subjects"
                options={courses.map((value) => ({
                  value: value.id,
                  label: `${value.name}${value.subjectArea ? ` · ${value.subjectArea}` : ""}`,
                }))}
              />
            </div>
            <div>
              <StandardSelectField
                name="batch"
                disabled={blocked}
                label="Audience (optional)"
                value={batch}
                onChange={value => { changeDraft(); setBatch(value); }}
                placeholder="All students in the subject"
                options={batches.map((value) => ({
                  value: value.id,
                  label: value.name,
                }))}
              />
            </div>
            <button disabled={blocked} className="enterprise-action-button">
              {saving ? "Publishing…" : "Publish resource"}
            </button></fieldset>
          </form>
          <section className="surface-panel resources-panel">
            <div className="standard-panel-heading standard-panel-heading-row">
              <div>
                <p>Library</p>
                <h2>Published resources</h2>
              </div>
              <span className="resource-count">{ready ? resources.length : "—"}</span>
            </div>
            {!ready ? <p className="enterprise-settings-empty">Resource register unavailable until loading succeeds.</p> : resources.length === 0 ? (
              <p className="enterprise-settings-empty">
                No resources have been published yet.
              </p>
            ) : (
              <ul className="resource-list">
                {resources.map((resource) => (
                  <li key={resource.id}>
                    <div>
                      <a href={resource.url} target="_blank" rel="noreferrer">
                        {resource.title}
                      </a>
                      <span>
                        {subject(resource.courseId)} ·{" "}
                        {audience(resource.batchId)}
                      </span>
                    </div>
                    <b>
                      {resource.type === "SheetMusic"
                        ? "Sheet music"
                        : resource.type}
                    </b>
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
