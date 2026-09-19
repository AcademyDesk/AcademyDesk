"use client";

import { FormEvent, useEffect, useState } from "react";
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

  return <main className="enterprise-settings finance-module">
    <header className="enterprise-page-header"><p>Finance / fee plans</p><h2>Fee plans</h2><span>Create reusable pricing for monthly tuition, term fees, workshops, or one-time admission charges.</span></header>
    {message && <p className="enterprise-settings-empty mt-5">{message}</p>}
    <section className="mt-5 grid gap-5 lg:grid-cols-[minmax(0,.85fr)_minmax(0,1.15fr)]">
      <form onSubmit={createPlan} className="surface-panel rounded-xl p-5"><h3>Create fee plan</h3><div className="mt-4 grid gap-3"><input value={name} onChange={(event) => setName(event.target.value)} placeholder="e.g. Piano monthly tuition" className="field" required /><input type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Amount in INR" className="field" required /><select value={frequency} onChange={(event) => setFrequency(event.target.value)} className="field"><option>Monthly</option><option>Quarterly</option><option>Term</option><option>OneTime</option></select></div><button disabled={!academy} className="enterprise-action-button mt-5 w-full">Create fee plan</button></form>
      <section className="surface-panel rounded-xl p-5"><h3>Active fee plans</h3>{plans.length === 0 ? <p className="enterprise-settings-empty mt-4">No fee plans yet. Create a reusable price to issue consistent invoices.</p> : <ul className="mt-4 space-y-3">{plans.map((plan) => <li key={plan.id} className="rounded-lg border border-slate-700 p-4"><div className="flex items-start justify-between gap-4"><div><b>{plan.name}</b><p className="mt-1 text-sm text-slate-400">{plan.frequency}</p></div><b className="text-cyan-200">{money(plan.amount, plan.currency)}</b></div></li>)}</ul>}</section>
    </section>
  </main>;
}
