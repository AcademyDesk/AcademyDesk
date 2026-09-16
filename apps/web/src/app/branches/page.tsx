"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";

type Academy = { id: string; name: string };
type Branch = { id: string; name: string; city?: string | null; state?: string | null; postalCode?: string | null; isActive: boolean };
const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export default function BranchesPage() {
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [academyId, setAcademyId] = useState("");
  const [branches, setBranches] = useState<Branch[]>([]);
  const [name, setName] = useState("");
  const [city, setCity] = useState("");
  const [state, setState] = useState("");
  const [message, setMessage] = useState("");

  async function loadAcademies() {
    const response = await fetch(`${apiUrl}/api/academies`, { cache: "no-store" });
    if (!response.ok) throw new Error();
    const data: Academy[] = await response.json();
    setAcademies(data);
    if (!academyId && data.length) setAcademyId(data[0].id);
  }

  async function loadBranches(id: string) {
    if (!id) return setBranches([]);
    const response = await fetch(`${apiUrl}/api/academies/${id}/branches`, { cache: "no-store" });
    if (!response.ok) throw new Error();
    setBranches(await response.json());
  }

  useEffect(() => { void loadAcademies().catch(() => setMessage("The AcademyDesk API is not reachable.")); }, []);
  useEffect(() => { void loadBranches(academyId).catch(() => setMessage("Branches could not be loaded.")); }, [academyId]);

  async function createBranch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academyId || !name.trim()) return;
    const response = await fetch(`${apiUrl}/api/academies/${academyId}/branches`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name, city, state }) });
    if (!response.ok) return setMessage("The branch could not be saved.");
    setName(""); setCity(""); setState(""); setMessage(""); await loadBranches(academyId);
  }

  return <main className="min-h-screen bg-slate-950 px-6 py-12 text-slate-100"><div className="mx-auto max-w-4xl">
    <Link href="/" className="text-sm text-cyan-300 hover:text-cyan-200">← Academy setup</Link>
    <h1 className="mt-4 text-4xl font-semibold">Branch setup</h1>
    <p className="mt-3 text-slate-300">Add the locations where your academy teaches music, tuition, or coaching.</p>
    {message && <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-200">{message}</p>}
    {academies.length === 0 ? <p className="mt-8 rounded-lg border border-dashed border-slate-700 p-8 text-center text-slate-400">Create an academy first, then add its branches.</p> : <>
      <label className="mt-8 block text-sm text-slate-300" htmlFor="academy">Academy</label><select id="academy" value={academyId} onChange={(e) => setAcademyId(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2">{academies.map((academy) => <option key={academy.id} value={academy.id}>{academy.name}</option>)}</select>
      <section className="mt-6 grid gap-6 md:grid-cols-2"><form onSubmit={createBranch} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Add a branch</h2><input value={name} onChange={(e) => setName(e.target.value)} placeholder="Branch name" className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><input value={city} onChange={(e) => setCity(e.target.value)} placeholder="City" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><input value={state} onChange={(e) => setState(e.target.value)} placeholder="State" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><button className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300">Create branch</button></form><section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Branches</h2>{branches.length === 0 ? <p className="mt-6 text-slate-400">No branches yet.</p> : <ul className="mt-4 space-y-3">{branches.map((branch) => <li key={branch.id} className="rounded-lg border border-slate-700 p-4"><div className="font-medium">{branch.name}</div><div className="mt-1 text-sm text-slate-400">{[branch.city, branch.state].filter(Boolean).join(", ") || "Location not set"}</div></li>)}</ul>}</section></section>
    </>}
  </div></main>;
}
