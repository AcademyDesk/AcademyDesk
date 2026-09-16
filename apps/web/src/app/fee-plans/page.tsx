"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type FeePlan = { id: string; name: string; amount: number; currency: string; frequency: string; isActive: boolean };

function money(amount: number, currency: string) {
  return new Intl.NumberFormat("en-IN", { style: "currency", currency, maximumFractionDigits: 2 }).format(amount);
}

export default function FeePlansPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [plans, setPlans] = useState<FeePlan[]>([]);
  const [name, setName] = useState("");
  const [amount, setAmount] = useState("");
  const [frequency, setFrequency] = useState("Monthly");
  const [message, setMessage] = useState("Loading fee plans…");

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const response = await academyApi(`/api/academies/${id}/fee-plans`, { cache: "no-store" });
    if (!response.ok) throw new Error();
    setPlans(await response.json()); setMessage("");
  }

  useEffect(() => { async function initialise() { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (response.status === 401) return setMessage("Please sign in before setting up fees."); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create your academy first."); setAcademy(academies[0]); await load(academies[0].id); } catch { setMessage("Fee plans could not be loaded. Confirm the API is running on port 5092."); } } void initialise(); }, []);

  async function createPlan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const response = await academyApi(`/api/academies/${academy.id}/fee-plans`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ name, amount: Number(amount), currency: "INR", frequency }) });
    if (!response.ok) return setMessage("Enter a name and a positive fee amount.");
    setName(""); setAmount(""); setMessage(""); await load();
  }

  return <main className="min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="mx-auto max-w-6xl px-6 py-10">
    <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Finance</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Fee plans</h1><p className="mt-3 text-slate-300">Create reusable pricing for monthly tuition, term fees, workshops, or one-time admission charges.</p>
    {message && <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className="mt-8 grid gap-6 lg:grid-cols-[0.85fr_1.15fr]"><form onSubmit={createPlan} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Create fee plan</h2><input value={name} onChange={(event) => setName(event.target.value)} placeholder="e.g. Piano monthly tuition" className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><input type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Amount in INR" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><select value={frequency} onChange={(event) => setFrequency(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option>Monthly</option><option>Quarterly</option><option>Term</option><option>OneTime</option></select><button disabled={!academy} className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60">Create fee plan</button></form>
    <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Active fee plans</h2>{plans.length === 0 ? <p className="mt-6 text-slate-400">No fee plans yet.</p> : <ul className="mt-5 space-y-3">{plans.map((plan) => <li key={plan.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="flex items-start justify-between gap-4"><div><div className="font-medium">{plan.name}</div><div className="mt-1 text-sm text-slate-400">{plan.frequency}</div></div><div className="font-semibold text-cyan-200">{money(plan.amount, plan.currency)}</div></div></li>)}</ul>}</section></section>
  </div></main>;
}
