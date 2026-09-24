"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type FeePlan = { id: string; name: string; amount: number; currency: string; frequency: string; isActive: boolean };

const frequencies = [
  { value: "Monthly", label: "Monthly" },
  { value: "Quarterly", label: "Quarterly" },
  { value: "Term", label: "Per term" },
  { value: "OneTime", label: "One-time" },
];
const money = (amount: number, currency: string) => new Intl.NumberFormat("en-IN", { style: "currency", currency, maximumFractionDigits: 2 }).format(amount);

export default function FeePlansPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [plans, setPlans] = useState<FeePlan[]>([]);
  const [name, setName] = useState("");
  const [amount, setAmount] = useState("");
  const [frequency, setFrequency] = useState("Monthly");
  const [message, setMessage] = useState("Loading fee plans…");
  const [editingId, setEditingId] = useState<string>();
  const [savingId, setSavingId] = useState<string>();

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const response = await academyApi(`/api/academies/${id}/fee-plans`, { cache: "no-store" });
    if (!response.ok) throw new Error();
    setPlans(await response.json());
    setMessage("");
  }

  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (response.status === 401) return setMessage("Please sign in before setting up fees.");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create your academy first.");
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setMessage("Fee plans could not be loaded. Please refresh and try again.");
      }
    }
    void initialise();
  }, []);

  const planStats = useMemo(() => ({
    total: plans.length,
    monthly: plans.filter((plan) => plan.frequency === "Monthly").length,
    cycle: plans.filter((plan) => ["Quarterly", "Term"].includes(plan.frequency)).length,
    oneTime: plans.filter((plan) => plan.frequency === "OneTime").length,
  }), [plans]);

  async function createPlan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    setSavingId("new");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/fee-plans`, {
        method: "POST", headers: apiHeaders(true), body: JSON.stringify({ name, amount: Number(amount), currency: "INR", frequency }),
      });
      if (!response.ok) throw new Error();
      setName(""); setAmount(""); setFrequency("Monthly");
      await load();
      setMessage("Fee plan created.");
    } catch {
      setMessage("Enter a plan name and a positive fee amount.");
    } finally { setSavingId(undefined); }
  }

  async function savePlan(plan: FeePlan, next: Pick<FeePlan, "name" | "amount" | "frequency">, isActive = true) {
    if (!academy || !next.name.trim() || next.amount <= 0) return setMessage("Enter a plan name and a positive fee amount.");
    setSavingId(plan.id);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/fee-plans/${plan.id}`, {
        method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ ...next, isActive }),
      });
      if (!response.ok) throw new Error();
      setEditingId(undefined);
      if (isActive) {
        await load();
        setMessage("Fee plan updated.");
      } else {
        setPlans((current) => current.filter((item) => item.id !== plan.id));
        setMessage("Fee plan deactivated.");
      }
    } catch {
      setMessage("The fee plan could not be saved. Please try again.");
    } finally { setSavingId(undefined); }
  }

  return <main className="enterprise-settings finance-module fee-plans-standard">
    <header className="fee-plans-heading"><div className="fee-plans-title"><span className="fee-plans-title-icon" aria-hidden="true">₹</span><div><p>Billing & collections</p><h1>Fee plans</h1></div></div></header>
    {message && <p className="fee-plans-notice" role="status">{message}</p>}
    <section className="fee-plans-kpis" aria-label="Fee plan summary">
      <article><span>Active plans</span><strong>{planStats.total}</strong></article><article><span>Monthly plans</span><strong>{planStats.monthly}</strong></article><article><span>Cycle plans</span><strong>{planStats.cycle}</strong></article><article><span>One-time plans</span><strong>{planStats.oneTime}</strong></article>
    </section>
    <section className="fee-plans-layout">
      <form onSubmit={createPlan} className="fee-plans-panel fee-plans-create"><header className="fee-plans-panel-header"><div><p>Create</p><h2>New fee plan</h2></div></header><div className="fee-plans-fields"><label><span>Plan name</span><input value={name} onChange={(event) => setName(event.target.value)} placeholder="e.g. Piano monthly tuition" required /></label><label><span>Fee amount</span><input type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Amount in INR" required /></label><label><span>Billing cycle</span><StandardSelectField name="frequency" value={frequency} onChange={setFrequency} placeholder="Choose billing cycle" options={frequencies} /></label><button disabled={!academy || savingId === "new"} className="enterprise-action-button">{savingId === "new" ? "Creating…" : "Create fee plan"}</button></div></form>
      <section className="fee-plans-panel fee-plans-directory"><header className="fee-plans-panel-header"><div><p>Directory</p><h2>Active fee plans</h2></div><span>{plans.length} active</span></header>{plans.length === 0 ? <p className="fee-plans-empty">No fee plans created.</p> : <ul>{plans.map((plan) => <li key={plan.id}>{editingId === plan.id ? <FeePlanEditor plan={plan} saving={savingId === plan.id} onCancel={() => setEditingId(undefined)} onSave={(next) => void savePlan(plan, next)} /> : <div className="fee-plan-row"><div><b>{plan.name}</b><small>{frequencies.find((item) => item.value === plan.frequency)?.label ?? plan.frequency}</small></div><strong>{money(plan.amount, plan.currency)}</strong><div className="fee-plan-actions"><button type="button" onClick={() => setEditingId(plan.id)} className="enterprise-action-button enterprise-action-button-secondary">Edit</button><button type="button" disabled={savingId === plan.id} onClick={() => void savePlan(plan, plan, false)} className="fee-plan-deactivate">Deactivate</button></div></div>}</li>)}</ul>}</section>
    </section>
  </main>;
}

function FeePlanEditor({ plan, saving, onCancel, onSave }: { plan: FeePlan; saving: boolean; onCancel: () => void; onSave: (next: Pick<FeePlan, "name" | "amount" | "frequency">) => void }) {
  const [name, setName] = useState(plan.name);
  const [amount, setAmount] = useState(String(plan.amount));
  const [frequency, setFrequency] = useState(plan.frequency);
  return <form onSubmit={(event) => { event.preventDefault(); onSave({ name, amount: Number(amount), frequency }); }} className="fee-plan-edit-form"><label><span>Plan name</span><input value={name} onChange={(event) => setName(event.target.value)} required /></label><label><span>Amount</span><input type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} required /></label><label><span>Cycle</span><StandardSelectField name="frequency" value={frequency} onChange={setFrequency} placeholder="Choose billing cycle" options={frequencies} /></label><div className="fee-plan-actions"><button disabled={saving} className="enterprise-action-button">Save</button><button type="button" onClick={onCancel} className="enterprise-action-button enterprise-action-button-secondary">Cancel</button></div></form>;
}
