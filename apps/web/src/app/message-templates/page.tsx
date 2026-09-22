"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Template = { id: string; channel: string; name: string; templateKey: string; category: string; templateGroup: string; status: string; language: string; providerTemplateName?: string | null; subject?: string | null; body: string; isActive: boolean };
type Starter = { id: string; channel: string; templateGroup: string; name: string; templateKey: string; category: string; subject?: string | null; body: string };
type Draft = { channel: string; name: string; templateKey: string; category: string; templateGroup: string; status: string; language: string; providerTemplateName: string; subject: string; body: string; isActive: boolean };
const initialDraft: Draft = { channel: "WhatsApp", name: "", templateKey: "", category: "Utility", templateGroup: "General", status: "Draft", language: "en", providerTemplateName: "", subject: "", body: "", isActive: true };

export default function MessageTemplatesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [items, setItems] = useState<Template[]>([]);
  const [catalogue, setCatalogue] = useState<Starter[]>([]);
  const [selectedChannel, setSelectedChannel] = useState<"WhatsApp" | "Email">("WhatsApp");
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [message, setMessage] = useState("Loading templates…");
  const [form, setForm] = useState<Draft>(initialDraft);

  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const [templateResponse, catalogueResponse] = await Promise.all([
      academyApi(`/api/academies/${academyId}/communication-templates`, { cache: "no-store" }),
      academyApi(`/api/academies/${academyId}/communication-templates/starter-templates`, { cache: "no-store" }),
    ]);
    if (!templateResponse.ok || !catalogueResponse.ok) throw new Error();
    setItems(await templateResponse.json());
    setCatalogue(await catalogueResponse.json());
  }

  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (response.status === 401) return setMessage("Please sign in as the academy owner first.");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create your academy first.");
        setAcademy(academies[0]);
        await load(academies[0].id);
        setMessage("");
      } catch { setMessage("Templates could not be loaded. Confirm the API is running on port 5092."); }
    }
    void initialise();
  }, []);

  const availableStarters = useMemo(() => catalogue.filter(item => item.channel === selectedChannel), [catalogue, selectedChannel]);
  const groupedStarters = useMemo(() => Object.entries(Object.groupBy(availableStarters, item => item.templateGroup)), [availableStarters]);
  const selectedForChannel = selectedIds.filter(id => availableStarters.some(item => item.id === id));

  function toggle(id: string) { setSelectedIds(current => current.includes(id) ? current.filter(value => value !== id) : [...current, id]); }
  function toggleGroup(groupItems: Starter[]) {
    const groupIds = groupItems.map(item => item.id);
    const allSelected = groupIds.every(id => selectedIds.includes(id));
    setSelectedIds(current => allSelected ? current.filter(id => !groupIds.includes(id)) : [...new Set([...current, ...groupIds])]);
  }
  async function addSelected() {
    if (!academy || selectedIds.length === 0) return;
    setMessage("Adding selected templates…");
    const response = await academyApi(`/api/academies/${academy.id}/communication-templates/starter-templates`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ templateIds: selectedIds }) });
    const result = await response.json().catch(() => null);
    if (!response.ok) return setMessage(result?.message ?? "Selected templates could not be added.");
    await load();
    setSelectedIds([]);
    setMessage(result.message);
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const response = await academyApi(`/api/academies/${academy.id}/communication-templates`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify(form) });
    const result = await response.json().catch(() => null);
    if (!response.ok) return setMessage(result?.message ?? "Template could not be saved.");
    setForm(initialDraft);
    await load();
    setMessage("Template saved.");
  }
  const field = "w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2";
  return <main className="enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="mx-auto max-w-6xl px-6 py-10">
    <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Communication</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Message templates</h1>
    <p className="mt-3 max-w-3xl text-slate-300">Choose only the templates your academy needs. Keep WhatsApp templates as drafts until their exact Meta template and language have been approved.</p>
    {message && <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-6">
      <div className="flex flex-wrap items-end justify-between gap-4"><div><h2 className="text-xl font-semibold">Starter template catalogue</h2><p className="mt-1 text-sm text-slate-400">Select a channel, choose individual scenarios or an entire group, then add only those selections.</p></div><button disabled={!academy || selectedIds.length === 0} onClick={addSelected} className="rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 disabled:opacity-60">Add selected ({selectedIds.length})</button></div>
      <div className="mt-5 inline-flex rounded-lg border border-slate-700 p-1"><button onClick={() => setSelectedChannel("WhatsApp")} className={`rounded-md px-4 py-2 text-sm ${selectedChannel === "WhatsApp" ? "bg-cyan-400 font-semibold text-slate-950" : "text-slate-300"}`}>WhatsApp</button><button onClick={() => setSelectedChannel("Email")} className={`rounded-md px-4 py-2 text-sm ${selectedChannel === "Email" ? "bg-cyan-400 font-semibold text-slate-950" : "text-slate-300"}`}>Email</button></div>
      <div className="mt-5 grid gap-5 lg:grid-cols-2">{groupedStarters.map(([group, groupedItems]) => { const groupItems = groupedItems ?? []; const selected = groupItems.every(item => selectedIds.includes(item.id)); return <section key={group} className="rounded-xl border border-slate-700 bg-slate-950 p-4"><div className="flex items-center justify-between gap-3"><h3 className="font-semibold text-cyan-200">{group}</h3><button onClick={() => toggleGroup(groupItems)} className="text-sm text-cyan-300 hover:text-cyan-100">{selected ? "Clear group" : "Select group"}</button></div><div className="mt-3 space-y-2">{groupItems.map(item => <label key={item.id} className="flex cursor-pointer gap-3 rounded-lg border border-slate-800 p-3 hover:border-slate-600"><input type="checkbox" checked={selectedIds.includes(item.id)} onChange={() => toggle(item.id)} className="mt-1" /><span><span className="block text-sm font-medium">{item.name}</span><span className="mt-1 block text-xs text-slate-400">{item.category} · {item.templateKey}</span><span className="mt-1 block text-xs text-slate-500">{item.subject ?? item.body}</span></span></label>)}</div></section>; })}</div>
      {selectedForChannel.length > 0 && <p className="mt-4 text-sm text-slate-400">{selectedForChannel.length} {selectedChannel} template(s) selected. Your selections stay available while you switch channels.</p>}
    </section>
    <section className="mt-6 grid gap-6 lg:grid-cols-[.9fr_1.1fr]"><form onSubmit={create} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Create a custom template</h2><div className="mt-5 grid gap-3 sm:grid-cols-2"><select value={form.channel} onChange={e => setForm({ ...form, channel: e.target.value })} className={field}><option>WhatsApp</option><option>Email</option></select><select value={form.templateGroup} onChange={e => setForm({ ...form, templateGroup: e.target.value })} className={field}><option>Finance</option><option>Admissions</option><option>Classes</option><option>Academic</option><option>Academy updates</option><option>General</option></select></div><div className="mt-3 grid gap-3 sm:grid-cols-2"><input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} placeholder="Internal template name" className={field} required /><input value={form.templateKey} onChange={e => setForm({ ...form, templateKey: e.target.value })} placeholder="Key, e.g. fee_due_reminder" className={field} required /></div><div className="mt-3 grid gap-3 sm:grid-cols-2"><select value={form.category} onChange={e => setForm({ ...form, category: e.target.value })} className={field}><option>Utility</option><option>Marketing</option><option>Authentication</option><option>Transactional</option></select><select value={form.status} onChange={e => setForm({ ...form, status: e.target.value })} className={field}><option>Draft</option><option>Approved</option><option>Disabled</option></select></div><div className="mt-3 grid gap-3 sm:grid-cols-2"><input value={form.language} onChange={e => setForm({ ...form, language: e.target.value })} placeholder="Language, e.g. en_US" className={field} /><input value={form.providerTemplateName} onChange={e => setForm({ ...form, providerTemplateName: e.target.value })} placeholder="Exact Meta/provider name" className={field} /></div><input value={form.subject} onChange={e => setForm({ ...form, subject: e.target.value })} placeholder="Email subject (optional)" className={`${field} mt-3`} /><textarea value={form.body} onChange={e => setForm({ ...form, body: e.target.value })} placeholder="Template body, e.g. Hello {{guardian_name}}" className={`${field} mt-3 min-h-36`} required /><label className="mt-3 flex gap-2 text-sm text-slate-300"><input type="checkbox" checked={form.isActive} onChange={e => setForm({ ...form, isActive: e.target.checked })} /> Template is available for use</label><button disabled={!academy} className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 disabled:opacity-60">Save custom template</button></form>
    <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Your templates</h2>{items.length === 0 ? <p className="mt-6 text-slate-400">No templates added yet.</p> : <ul className="mt-5 space-y-3">{items.map(item => <li key={item.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="flex flex-wrap justify-between gap-3"><div><div className="font-medium">{item.name}</div><div className="mt-1 text-sm text-cyan-200">{item.channel} · {item.templateGroup} · {item.status}</div></div><span className="text-sm text-slate-400">{item.isActive ? "Active" : "Inactive"}</span></div><p className="mt-3 whitespace-pre-wrap text-sm text-slate-300">{item.body}</p><div className="mt-3 text-xs text-slate-500">Key: {item.templateKey}{item.providerTemplateName ? ` · Provider: ${item.providerTemplateName}` : ""}</div></li>)}</ul>}</section></section>
  </div></main>;
}
