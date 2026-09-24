"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

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
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const [list, batchList, courseList] = await Promise.all([
      academyApi(`/api/academies/${academyId}/resources`),
      academyApi(`/api/academies/${academyId}/batches`),
      academyApi(`/api/academies/${academyId}/courses`),
    ]);
    if (!list.ok || !batchList.ok || !courseList.ok) throw new Error();
    setResources(await list.json());
    setBatches(await batchList.json());
    setCourses(await courseList.json());
    setMessage("");
  }
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        const accounts: Academy[] = await response.json();
        if (!response.ok || !accounts[0]) throw new Error();
        setAcademy(accounts[0]);
        await load(accounts[0].id);
      } catch {
        setMessage("Resources could not be loaded.");
      }
    })();
  }, []);
  async function create(event: FormEvent) {
    event.preventDefault();
    if (!academy) return;
    setMessage("Publishing resource…");
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
      const result = await response.json().catch(() => null);
      setMessage(
        result?.message ?? "Provide a title and a link, or upload a file.",
      );
      return;
    }
    setTitle("");
    setUrl("");
    setFile(null);
    if (input.current) input.current.value = "";
    await load();
  }
  const subject = (id?: string) =>
    courses.find((value) => value.id === id)?.name ?? "All subjects";
  const audience = (id?: string) =>
    batches.find((value) => value.id === id)?.name ?? "All students";
  return (
    <main className="enterprise-settings resources-standard min-h-screen">
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
        <section className="resources-grid">
          <form onSubmit={create} className="surface-panel resources-panel">
            <div className="standard-panel-heading">
              <p>Library</p>
              <h2>Add resource</h2>
            </div>
            <label>
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
            <div className="resource-file">
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
            </div>
            <label>
              Type
              <StandardSelectField
                name="type"
                value={type}
                onChange={setType}
                placeholder="Select type"
                options={resourceTypes.map((value) => ({
                  value,
                  label: value === "SheetMusic" ? "Sheet music" : value,
                }))}
              />
            </label>
            <label>
              Subject
              <StandardSelectField
                name="course"
                value={course}
                onChange={setCourse}
                placeholder="All subjects"
                options={courses.map((value) => ({
                  value: value.id,
                  label: `${value.name}${value.subjectArea ? ` · ${value.subjectArea}` : ""}`,
                }))}
              />
            </label>
            <label>
              Audience
              <StandardSelectField
                name="batch"
                value={batch}
                onChange={setBatch}
                placeholder="All students in the subject"
                options={batches.map((value) => ({
                  value: value.id,
                  label: value.name,
                }))}
              />
            </label>
            <button className="enterprise-action-button">
              Publish resource
            </button>
          </form>
          <section className="surface-panel resources-panel">
            <div className="standard-panel-heading standard-panel-heading-row">
              <div>
                <p>Library</p>
                <h2>Published resources</h2>
              </div>
              <span className="resource-count">{resources.length}</span>
            </div>
            {resources.length === 0 ? (
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
