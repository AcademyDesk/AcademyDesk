"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { WorkspaceNav } from "@/components/workspace-nav";

type Academy = { id: string; name: string };
type Branch = { id: string; name: string; city?: string | null; state?: string | null; postalCode?: string | null; isActive: boolean };

export default function BranchesPage() {
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [academyId, setAcademyId] = useState("");
  const [branches, setBranches] = useState<Branch[]>([]);
  const [name, setName] = useState("");
  const [city, setCity] = useState("");
  const [state, setState] = useState("");
  const [message, setMessage] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null); const [editName, setEditName] = useState(""); const [editCity, setEditCity] = useState(""); const [editState, setEditState] = useState(""); const [savingId, setSavingId] = useState<string | null>(null);

  async function loadAcademies() {
    const response = await academyApi("/api/academies", { cache: "no-store" });
    if (!response.ok) throw new Error();
    const data: Academy[] = await response.json();
    setAcademies(data);
    if (!academyId && data.length) setAcademyId(data[0].id);
  }

  async function loadBranches(id: string) {
    if (!id) return setBranches([]);
    const response = await academyApi(`/api/academies/${id}/branches`, { cache: "no-store" });
    if (!response.ok) throw new Error();
    setBranches(await response.json());
  }

  useEffect(() => { void loadAcademies().catch(() => setMessage("The AcademyDesk API is not reachable.")); }, []);
  useEffect(() => { void loadBranches(academyId).catch(() => setMessage("Branches could not be loaded.")); }, [academyId]);

  async function createBranch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academyId || !name.trim()) return;
    const response = await academyApi(`/api/academies/${academyId}/branches`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ name, city, state }) });
    if (!response.ok) return setMessage("The branch could not be saved.");
    setName(""); setCity(""); setState(""); setMessage(""); await loadBranches(academyId);
  }
  function beginEdit(branch: Branch) { setEditingId(branch.id); setEditName(branch.name); setEditCity(branch.city ?? ""); setEditState(branch.state ?? ""); }
  async function saveBranch(branch: Branch) { if (!editName.trim()) return setMessage("Branch name is required."); setSavingId(branch.id); const response = await academyApi(`/api/academies/${academyId}/branches/${branch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: editName, city: editCity || null, state: editState || null, isActive: branch.isActive }) }); setSavingId(null); if (!response.ok) return setMessage("The branch could not be updated."); setEditingId(null); setMessage("Branch updated."); await loadBranches(academyId); }
  async function toggleActive(branch: Branch) { setSavingId(branch.id); const response = await academyApi(`/api/academies/${academyId}/branches/${branch.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ name: branch.name, city: branch.city, state: branch.state, isActive: !branch.isActive }) }); setSavingId(null); if (!response.ok) return setMessage("The branch status could not be updated."); setMessage(branch.isActive ? "Branch marked inactive." : "Branch reactivated."); await loadBranches(academyId); }

  return <><WorkspaceNav /><main className="min-h-screen bg-slate-950 px-6 py-12 text-slate-100"><div className="mx-auto max-w-4xl">
    <Link href="/" className="text-sm text-cyan-300 hover:text-cyan-200">← Academy setup</Link>
    <h1 className="mt-4 text-4xl font-semibold">Branch setup</h1>
    <p className="mt-3 text-slate-300">Add the locations where your academy teaches music, tuition, or coaching.</p>
    {message && <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-200">{message}</p>}
    {academies.length === 0 ? <p className="mt-8 rounded-lg border border-dashed border-slate-700 p-8 text-center text-slate-400">Create an academy first, then add its branches.</p> : <>
      <label className="mt-8 block text-sm text-slate-300" htmlFor="academy">Academy</label><select id="academy" value={academyId} onChange={(e) => setAcademyId(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2">{academies.map((academy) => <option key={academy.id} value={academy.id}>{academy.name}</option>)}</select>
      <section className="mt-6 grid gap-6 md:grid-cols-2"><form onSubmit={createBranch} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Add a branch</h2><input value={name} onChange={(e) => setName(e.target.value)} placeholder="Branch name" className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><input value={city} onChange={(e) => setCity(e.target.value)} placeholder="City" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><input value={state} onChange={(e) => setState(e.target.value)} placeholder="State" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><button className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300">Create branch</button></form><section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Branches</h2>{branches.length === 0 ? <p className="mt-6 text-slate-400">No branches yet.</p> : <ul className="mt-4 space-y-3">{branches.map((branch) => editingId === branch.id ? <li key={branch.id} className="rounded-lg border border-cyan-700/60 p-4"><div className="grid gap-2"><input value={editName} onChange={(e) => setEditName(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><input value={editCity} onChange={(e) => setEditCity(e.target.value)} placeholder="City" className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><input value={editState} onChange={(e) => setEditState(e.target.value)} placeholder="State" className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /></div><div className="mt-3 flex gap-2"><button onClick={() => void saveBranch(branch)} disabled={savingId === branch.id} className="rounded-lg bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950">Save</button><button onClick={() => setEditingId(null)} className="rounded-lg border border-slate-700 px-3 py-2 text-sm">Cancel</button></div></li> : <li key={branch.id} className="rounded-lg border border-slate-700 p-4"><div className="flex items-start justify-between"><div><div className="font-medium">{branch.name}</div><div className="mt-1 text-sm text-slate-400">{[branch.city, branch.state].filter(Boolean).join(", ") || "Location not set"}</div></div><span className={`rounded-full px-2 py-1 text-xs ${branch.isActive ? "bg-emerald-950 text-emerald-300" : "bg-slate-800 text-slate-400"}`}>{branch.isActive ? "Active" : "Inactive"}</span></div><div className="mt-3 flex gap-2"><button onClick={() => beginEdit(branch)} className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm">Edit</button><button onClick={() => void toggleActive(branch)} disabled={savingId === branch.id} className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm">{branch.isActive ? "Deactivate" : "Reactivate"}</button></div></li>)}</ul>}</section></section>
    </>}
  </div></main></>;
}
