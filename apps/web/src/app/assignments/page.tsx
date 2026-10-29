"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardSelectField } from "@/components/design-system/controls";
import visual from "@/components/design-system/learning-workspace.module.css";

type Academy = { id: string };
type Batch = { id: string; name: string };
type Assignment = { id: string; batchId: string; title: string; description?: string | null; dueAtUtc?: string | null; type: string; isPublished: boolean };

async function fetchWorkspace(id: string) {
  const [batchResponse, assignmentResponse] = await Promise.all([
    academyApi(`/api/academies/${id}/batches`, { cache: "no-store" }),
    academyApi(`/api/academies/${id}/assignments`, { cache: "no-store" }),
  ]);
  if (!batchResponse.ok || !assignmentResponse.ok) throw new Error();
  return { batches: (await batchResponse.json()) as Batch[], assignments: (await assignmentResponse.json()) as Assignment[] };
}
export default function AssignmentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [batches, setBatches] = useState<Batch[]>([]);
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [batchId, setBatchId] = useState("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [dueAt, setDueAt] = useState("");
  const [type, setType] = useState("Homework");
  const [published, setPublished] = useState(true);
  const [message, setMessage] = useState("Loading assignments…");

  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (response.status === 401) return setMessage("Please sign in before managing assignments.");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create an academy and batch first.");
        setAcademy(academies[0]);
        const workspace = await fetchWorkspace(academies[0].id);
        setBatches(workspace.batches);
        setAssignments(workspace.assignments);
        setBatchId((current) => current || workspace.batches[0]?.id || "");
        setMessage("");
      } catch {
        setMessage("Assignments could not be loaded. Confirm the API is running on port 5092.");
      }
    }
    void initialise();
  }, []);
  async function createAssignment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending.current || !academy || !batchId) return;
    pending.current = true;
    setSaving(true);
    setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/assignments`, {
        method: "POST", headers: apiHeaders(true),
        body: JSON.stringify({ batchId, title, description: description || null, dueAtUtc: dueAt ? new Date(dueAt).toISOString() : null, type, isPublished: published }),
      });
      if (!response.ok) {
        let detail = "The assignment could not be saved.";
        try {
          const result = await response.json();
          if (typeof result?.message === "string" && result.message.trim()) detail = result.message.trim();
        } catch { /* Empty or non-JSON rejection retains the fallback. */ }
        return setMessage(response.status >= 500
          ? `Assignment save could not be confirmed. Check the assignment list before retrying. ${detail}`
          : detail);
      }
      setTitle("");
      setDescription("");
      setDueAt("");
      try {
        const workspace = await fetchWorkspace(academy.id);
        setBatches(workspace.batches);
        setAssignments(workspace.assignments);
        setBatchId((current) => current || workspace.batches[0]?.id || "");
        setMessage("Assignment created.");
      } catch {
        setMessage("Assignment created. The assignment list could not be refreshed; do not repeat the save. Refresh the page to check the saved assignment.");
      }
    } catch {
      setMessage("Assignment save could not be confirmed. Check the assignment list before retrying.");
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }
  const batchName = (id: string) => batches.find((batch) => batch.id === id)?.name ?? "Unknown batch";
  const date = (value?: string | null) => value ? new Intl.DateTimeFormat("en-IN", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value)) : "No due date";
  return <main className={`enterprise-settings min-h-screen ${visual.page}`}><WorkspaceNav/><div><header className={visual.heading}><p>Learning</p><h1>Homework assigned &amp; progress</h1><span>Create homework, practice tasks, theory work, or coaching assignments for each batch.</span></header>{message&&<p role="status" aria-live="polite" className={visual.notice}>{message}</p>}<section className={visual.layout}><form onSubmit={createAssignment} className={visual.panel}><h2>Create assignment</h2><fieldset className={visual.fields} disabled={saving}><StandardSelectField name="batchId" label="Batch" required disabled={saving} value={batchId} onChange={setBatchId} placeholder="Select batch" options={batches.map(batch=>({value:batch.id,label:batch.name}))}/><label><span>Assignment title</span><input value={title} onChange={event=>setTitle(event.target.value)} placeholder="Assignment title" required/></label><label><span>Instructions or practice notes (optional)</span><textarea value={description} onChange={event=>setDescription(event.target.value)} placeholder="Instructions or practice notes (optional)"/></label><StandardSelectField name="assignmentType" label="Assignment type" disabled={saving} value={type} onChange={setType} placeholder="Select type" options={["Homework","Practice","Theory","Project"].map(value=>({value,label:value}))}/><label><span>Due date and time (local, optional)</span><input type="datetime-local" value={dueAt} onChange={event=>setDueAt(event.target.value)}/></label><label className={visual.check}><input type="checkbox" checked={published} onChange={event=>setPublished(event.target.checked)}/>Publish for students</label><button disabled={saving || !academy || !batchId} className="enterprise-action-button">{saving?"Saving…":"Create assignment"}</button></fieldset></form><section className={visual.panel}><h2>Assignments</h2>{assignments.length===0?<p>No assignments yet.</p>:<ul className={visual.records}>{assignments.map(assignment=><li key={assignment.id} className={visual.record}><b>{assignment.title}</b><small>{batchName(assignment.batchId)} · {assignment.type} · {assignment.isPublished?"Published":"Draft"}</small>{assignment.description&&<p>{assignment.description}</p>}<small>Due: {date(assignment.dueAtUtc)}</small></li>)}</ul>}</section></section></div></main>;
}
