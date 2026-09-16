"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string; legalName?: string | null; timeZone: string };

export default function Home() {
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [name, setName] = useState("");
  const [legalName, setLegalName] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");

  async function loadAcademies() {
    setLoading(true);
    try {
      const response = await academyApi("/api/academies", { cache: "no-store" });
      if (!response.ok) throw new Error();
      setAcademies(await response.json());
      setMessage("");
    } catch { setMessage("Sign in first, then create or view your academy."); }
    finally { setLoading(false); }
  }

  useEffect(() => { void loadAcademies(); }, []);

  async function createAcademy(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!name.trim()) return;
    setSaving(true); setMessage("");
    try {
      const response = await academyApi("/api/academies", { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ name, legalName, countryCode: "IN", timeZone: "Asia/Kolkata" }) });
      if (!response.ok) throw new Error();
      setName(""); setLegalName(""); await loadAcademies();
    } catch { setMessage("We could not save the academy. Check that the API and database are running."); }
    finally { setSaving(false); }
  }

  return <main className="min-h-screen bg-slate-950 px-6 py-12 text-slate-100"><div className="mx-auto max-w-5xl">
    <div className="flex items-center justify-between gap-4"><p className="text-sm font-semibold uppercase tracking-[0.25em] text-cyan-300">AcademyDesk</p><Link href="/dashboard" className="text-sm text-cyan-300 hover:text-cyan-200">Open dashboard →</Link></div>
    <h1 className="mt-3 text-4xl font-semibold tracking-tight sm:text-5xl">Academy setup</h1>
    <p className="mt-4 max-w-2xl text-slate-300">Create the academies that will use your music, tuition, and coaching operations platform.</p>
    <section className="mt-10 grid gap-6 lg:grid-cols-[1fr_1.2fr]">
      <form onSubmit={createAcademy} className="rounded-2xl border border-slate-800 bg-slate-900 p-6 shadow-xl"><h2 className="text-xl font-semibold">Add an academy</h2>
        <label className="mt-6 block text-sm text-slate-300" htmlFor="academy-name">Display name</label><input id="academy-name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Academy name" className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 outline-none ring-cyan-400 focus:ring-2" required />
        <label className="mt-4 block text-sm text-slate-300" htmlFor="legal-name">Legal name (optional)</label><input id="legal-name" value={legalName} onChange={(e) => setLegalName(e.target.value)} placeholder="Registered organization name" className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 outline-none ring-cyan-400 focus:ring-2" />
        <button disabled={saving} className="mt-6 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60">{saving ? "Saving…" : "Create academy"}</button>
      </form>
      <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6 shadow-xl"><div className="flex items-center justify-between"><h2 className="text-xl font-semibold">Academies</h2><button onClick={() => void loadAcademies()} className="text-sm text-cyan-300">Refresh</button></div>
        {message && <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-200">{message}</p>}
        {loading ? <p className="mt-8 text-slate-400">Loading…</p> : academies.length === 0 ? <p className="mt-8 rounded-lg border border-dashed border-slate-700 p-8 text-center text-slate-400">No academies yet. Create the first one to begin setup.</p> : <ul className="mt-5 space-y-3">{academies.map((academy) => <li key={academy.id} className="rounded-lg border border-slate-700 p-4"><div className="font-medium">{academy.name}</div><div className="mt-1 text-sm text-slate-400">{academy.legalName || "No legal name"} · {academy.timeZone}</div></li>)}</ul>}
      </section>
    </section>
  </div></main>;
}
