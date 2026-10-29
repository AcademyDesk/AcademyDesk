"use client";

import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import styles from "@/components/design-system/finance-workspace.module.css";

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

  const [ready, setReady] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const guard = useRef({ pending: false, review: false, ready: false });
  const blocked = !ready || !!savingId || needsReview;
  function requireReview() { guard.current.review = true; setNeedsReview(true); }
  async function refreshRegister() {
    if (!academy || guard.current.pending) return;
    guard.current.pending = true; setSavingId("refresh");
    try { await load(academy.id); guard.current.review = false; setNeedsReview(false); setMessage("Fee plan register refreshed. Check the previous action before trying again."); }
    catch { requireReview(); setMessage("Fee plan register could not be refreshed. Do not repeat an unconfirmed action."); }
    finally { guard.current.pending = false; setSavingId(undefined); }
  }
  const load = useCallback(async (id: string) => {
    const response = await academyApi(`/api/academies/${id}/fee-plans`, { cache: "no-store" });
    if (!response.ok) throw new Error();
    const rows = await response.json(); if (!Array.isArray(rows)) throw Error();
    setPlans(rows); guard.current.ready = true; setReady(true);
  }, []);

  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (response.status === 401) return setMessage("Please sign in before setting up fees.");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create your academy first.");
        setAcademy(academies[0]);
        await load(academies[0].id); setMessage("");
      } catch {
        setMessage("Fee plans could not be loaded. Please refresh and try again.");
      }
    }
    void initialise();
  }, [load]);

  const planStats = useMemo(() => ({
    total: plans.length,
    monthly: plans.filter((plan) => plan.frequency === "Monthly").length,
    cycle: plans.filter((plan) => ["Quarterly", "Term"].includes(plan.frequency)).length,
    oneTime: plans.filter((plan) => plan.frequency === "OneTime").length,
  }), [plans]);

  async function createPlan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !guard.current.ready || guard.current.pending || guard.current.review) return;
    const captured = { name, amount, frequency };
    guard.current.pending = true; setSavingId("new");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/fee-plans`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ name: captured.name, amount: Number(captured.amount), currency: "INR", frequency: captured.frequency }) });
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408) { requireReview(); setMessage("Fee plan save could not be confirmed. Refresh and check the register before retrying."); }
        else { const problem = await response.json().catch(() => null); setMessage(typeof problem?.message === "string" ? problem.message : "Fee plan could not be created. Check the details and your access."); }
        return;
      }
      setName(current => current === captured.name ? "" : current); setAmount(current => current === captured.amount ? "" : current); setFrequency(current => current === captured.frequency ? "Monthly" : current);
      setMessage("Fee plan created.");
      try { await load(academy.id); } catch { requireReview(); setMessage("Fee plan created. The register could not be refreshed; do not create it again. Refresh the register to continue."); }
    } catch { requireReview(); setMessage("Fee plan save could not be confirmed. Refresh and check the register before retrying."); }
    finally { guard.current.pending = false; setSavingId(undefined); }
  }

  async function savePlan(plan: FeePlan, next: Pick<FeePlan, "name" | "amount" | "frequency">, isActive = true) {
    if (!academy || !guard.current.ready || guard.current.pending || guard.current.review) return;
    if (!next.name.trim() || next.amount <= 0) return setMessage("Enter a plan name and a positive fee amount.");
    guard.current.pending = true; setSavingId(plan.id);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/fee-plans/${plan.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ ...next, isActive }) });
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408) { requireReview(); setMessage("Fee plan change could not be confirmed. Refresh and check the register before retrying."); }
        else { const problem = await response.json().catch(() => null); setMessage(typeof problem?.message === "string" ? problem.message : "The fee plan could not be saved. Check the details and your access."); }
        return;
      }
      setEditingId(undefined);
      if (isActive) {
        setMessage("Fee plan updated.");
        try { await load(academy.id); } catch { requireReview(); setMessage("Fee plan updated. The register could not be refreshed; do not repeat the change. Refresh the register to continue."); }
      } else { setPlans(current => current.filter(item => item.id !== plan.id)); setMessage("Fee plan deactivated."); }
    } catch { requireReview(); setMessage("Fee plan change could not be confirmed. Refresh and check the register before retrying."); }
    finally { guard.current.pending = false; setSavingId(undefined); }
  }

  return <main className={`enterprise-settings finance-module fee-plans-standard ${styles.page}`}>
    <header className="fee-plans-heading"><div className="fee-plans-title"><span className="fee-plans-title-icon" aria-hidden="true">₹</span><div><p>Billing & collections</p><h1>Fee plans</h1></div></div></header>
    {message && <p className="fee-plans-notice" role="status">{message}</p>}
    {academy && <button type="button" disabled={!!savingId} onClick={refreshRegister} className="enterprise-action-button">Refresh fee plan register</button>}
    <section className={`fee-plans-kpis ${styles.kpis}`} aria-label="Fee plan summary">
      <article><span>Active plans</span><strong>{planStats.total}</strong></article><article><span>Monthly plans</span><strong>{planStats.monthly}</strong></article><article><span>Cycle plans</span><strong>{planStats.cycle}</strong></article><article><span>One-time plans</span><strong>{planStats.oneTime}</strong></article>
    </section>
    <section className={`fee-plans-layout ${styles.layout}`}>
      <form aria-busy={!!savingId} onSubmit={createPlan} className="fee-plans-panel fee-plans-create"><header className="fee-plans-panel-header"><div><p>Create</p><h2>New fee plan</h2></div></header><div className="fee-plans-fields"><label><span>Plan name</span><input disabled={blocked} value={name} onChange={(event) => setName(event.target.value)} placeholder="e.g. Piano monthly tuition" required /></label><label><span>Fee amount</span><input disabled={blocked} type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Amount in INR" required /></label><StandardSelectField name="frequency" label="Billing cycle" value={frequency} onChange={value => { if (value) setFrequency(value); }} placeholder="Choose billing cycle" options={frequencies} disabled={blocked} /><button disabled={!academy || blocked} className="enterprise-action-button">{savingId === "new" ? "Creating…" : "Create fee plan"}</button></div></form>
      <section className="fee-plans-panel fee-plans-directory"><header className="fee-plans-panel-header"><div><p>Directory</p><h2>Active fee plans</h2></div><span>{plans.length} active</span></header>{!ready ? <p className="fee-plans-empty">Fee plan register unavailable until loading succeeds.</p> : plans.length === 0 ? <p className="fee-plans-empty">No fee plans created.</p> : <ul>{plans.map((plan) => <li key={plan.id}>{editingId === plan.id ? <FeePlanEditor plan={plan} saving={blocked} onCancel={() => setEditingId(undefined)} onSave={(next) => void savePlan(plan, next)} /> : <div className="fee-plan-row"><div><b>{plan.name}</b><small>{frequencies.find((item) => item.value === plan.frequency)?.label ?? plan.frequency}</small></div><strong>{money(plan.amount, plan.currency)}</strong><div className="fee-plan-actions"><button type="button" disabled={blocked} onClick={() => setEditingId(plan.id)} className="enterprise-action-button enterprise-action-button-secondary">Edit</button><button type="button" disabled={blocked} onClick={() => void savePlan(plan, plan, false)} className="fee-plan-deactivate">Deactivate</button></div></div>}</li>)}</ul>}</section>
    </section>
  </main>;
}

function FeePlanEditor({ plan, saving, onCancel, onSave }: { plan: FeePlan; saving: boolean; onCancel: () => void; onSave: (next: Pick<FeePlan, "name" | "amount" | "frequency">) => void }) {
  const [name, setName] = useState(plan.name);
  const [amount, setAmount] = useState(String(plan.amount));
  const [frequency, setFrequency] = useState(plan.frequency);
  return <form onSubmit={(event) => { event.preventDefault(); onSave({ name, amount: Number(amount), frequency }); }} className="fee-plan-edit-form"><label><span>Plan name</span><input disabled={saving} value={name} onChange={(event) => setName(event.target.value)} required /></label><label><span>Amount</span><input disabled={saving} type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} required /></label><StandardSelectField name="editFrequency" label="Billing cycle" value={frequency} onChange={value => { if (value) setFrequency(value); }} placeholder="Choose billing cycle" options={frequencies} disabled={saving} /><div className="fee-plan-actions"><button disabled={saving} className="enterprise-action-button">Save</button><button type="button" disabled={saving} onClick={onCancel} className="enterprise-action-button enterprise-action-button-secondary">Cancel</button></div></form>;
}
