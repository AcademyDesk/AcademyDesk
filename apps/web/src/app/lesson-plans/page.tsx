"use client";
import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardSelectField } from "@/components/design-system/controls";
import visual from "@/components/design-system/academic-workspace.module.css";
type A = { id: string };
type B = { id: string; name: string };
type P = { id: string; batchId: string; title: string; objectives?: string; status: string };

async function fetchWorkspace(id: string) {
  const [batches, plans] = await Promise.all([
    academyApi(`/api/academies/${id}/batches`),
    academyApi(`/api/academies/${id}/lesson-plans`),
  ]);
  if (!batches.ok || !plans.ok) throw new Error();
  return { batches: (await batches.json()) as B[], plans: (await plans.json()) as P[] };
}
export default function LessonPlans() {
  const [a, setA] = useState<A>();
  const [b, setB] = useState<B[]>([]);
  const [p, setP] = useState<P[]>([]);
  const [batch, setBatch] = useState("");
  const [title, setTitle] = useState("");
  const [obj, setObj] = useState("");
  const [m, setM] = useState("Loading lesson plans…");
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies");
        if (!response.ok) throw new Error();
        const academies: A[] = await response.json();
        if (!academies[0]) throw new Error();
        const workspace = await fetchWorkspace(academies[0].id);
        setA(academies[0]);
        setB(workspace.batches);
        setP(workspace.plans);
        setBatch(current => current || workspace.batches[0]?.id || "");
        setM("");
      } catch {
        setM("Lesson plans could not be loaded. Apply the migration and restart the API.");
      }
    }
    void initialise();
  }, []);
  async function add(e: FormEvent) {
    e.preventDefault();
    if (pending.current || !a || !batch) return;
    pending.current = true;
    setSaving(true);
    setM("");
    try {
      const r = await academyApi(`/api/academies/${a.id}/lesson-plans`, {
        method: "POST", headers: apiHeaders(true),
        body: JSON.stringify({ batchId: batch, courseModuleId: null, classSessionId: null, title, objectives: obj || null }),
      });
      if (!r.ok) {
        let detail = "Valid batch and title required.";
        try {
          const result = await r.json();
          if (typeof result?.message === "string" && result.message.trim()) detail = result.message.trim();
        } catch { /* Empty or non-JSON rejection retains the fallback. */ }
        return setM(r.status >= 500
          ? `Lesson plan save could not be confirmed. Check the lesson plan list before retrying. ${detail}`
          : detail);
      }
      setTitle("");
      setObj("");
      try {
        const workspace = await fetchWorkspace(a.id);
        setB(workspace.batches);
        setP(workspace.plans);
        setBatch(current => current || workspace.batches[0]?.id || "");
        setM("Lesson plan created.");
      } catch {
        setM("Lesson plan created. The lesson plan list could not be refreshed; do not repeat the save. Refresh the page to check the saved plan.");
      }
    } catch {
      setM("Lesson plan save could not be confirmed. Check the lesson plan list before retrying.");
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }
  const n = (id: string) => b.find(x => x.id === id)?.name ?? "Batch";
return <main className={`enterprise-settings min-h-screen ${visual.page}`}><WorkspaceNav/><div className={visual.body}><header className={visual.heading}><p>Teaching</p><h1>Lesson plans</h1><span>Plan what each batch will learn before class.</span></header>{m&&<p role="status" aria-live="polite" className={visual.notice}>{m}</p>}<section className={visual.layout}><form onSubmit={add} className={visual.panel}><h2>Create lesson plan</h2><fieldset disabled={saving || !a} className={visual.fields}><StandardSelectField name="batchId" label="Batch" value={batch} onChange={setBatch} disabled={saving || !a} placeholder="Select batch" options={b.map(x=>({value:x.id,label:x.name}))}/><label><span>Lesson title</span><input value={title} onChange={e=>setTitle(e.target.value)} placeholder="Lesson title" required/></label><label><span>Learning objectives (optional)</span><textarea value={obj} onChange={e=>setObj(e.target.value)} placeholder="Learning objectives"/></label><button disabled={saving || !a || !batch} className="enterprise-action-button">{saving ? "Saving…" : "Create plan"}</button></fieldset></form><section className={visual.panel}><h2>Planned lessons</h2>{!a?<p>Lesson register unavailable until loading succeeds.</p>:p.length===0?<p>No lesson plans yet.</p>:<ul className={visual.records}>{p.map(x=><li key={x.id} className={visual.record}><b>{x.title}</b><small>{n(x.batchId)} · {x.status}</small>{x.objectives&&<p>{x.objectives}</p>}</li>)}</ul>}</section></section></div></main>}
