"use client";
import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
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
return <main className="enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav/><div className="mx-auto max-w-5xl px-6 py-10"><p className="text-sm font-semibold uppercase tracking-[.22em] text-cyan-300">Teaching</p><h1 className="mt-3 text-4xl font-semibold">Lesson plans</h1><p className="mt-3 text-slate-300">Plan what each batch will learn before class, then connect it to modules and sessions in the next refinement.</p>{m&&<p role="status" aria-live="polite" className="mt-6 rounded-lg bg-amber-950/40 p-4 text-amber-100">{m}</p>}<section className="mt-8 grid gap-6 lg:grid-cols-2"><form onSubmit={add} className="rounded-2xl bg-slate-900 p-6"><fieldset disabled={saving} className="m-0 min-w-0 border-0 p-0"><select value={batch} onChange={e=>setBatch(e.target.value)} className="w-full rounded-lg bg-slate-950 px-3 py-2">{b.map(x=><option key={x.id} value={x.id}>{x.name}</option>)}</select><input value={title} onChange={e=>setTitle(e.target.value)} placeholder="Lesson title" className="mt-3 w-full rounded-lg bg-slate-950 px-3 py-2" required/><textarea value={obj} onChange={e=>setObj(e.target.value)} placeholder="Learning objectives" className="mt-3 min-h-28 w-full rounded-lg bg-slate-950 px-3 py-2"/><button disabled={saving || !a || !batch} className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2 font-semibold text-slate-950">{saving ? "Saving…" : "Create plan"}</button></fieldset></form><section className="rounded-2xl bg-slate-900 p-6"><h2 className="text-xl font-semibold">Planned lessons</h2><ul className="mt-4 space-y-3">{p.map(x=><li key={x.id} className="rounded-lg bg-slate-950 p-4"><b>{x.title}</b><div className="mt-1 text-sm text-cyan-200">{n(x.batchId)} · {x.status}</div>{x.objectives&&<div className="mt-2 text-sm text-slate-400">{x.objectives}</div>}</li>)}</ul></section></section></div></main>}
