"use client";

import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import { expenseAmountJson, expenseCents, formatExpenseCents } from "@/lib/expense-money";
import styles from "./expenses.module.css";

type Academy = { id: string };
type Branch = { id: string; name: string };
type Expense = { id: string; description: string; amount: number; amountExact?: string; currency: string; category: string; branchId?: string | null; expenseDate: string; status: string };
type Draft = { description: string; amount: string; category: string; branchId: string; expenseDate: string };
const emptyDraft: Draft = { description: "", amount: "", category: "General", branchId: "", expenseDate: "" };
const money = (expense: Expense) => {
  const cents = expenseCents(expense.amountExact ?? expense.amount);
  return cents === null ? "Amount unavailable" : formatExpenseCents(cents, expense.currency);
};
// Never add unlike currencies or silently imply a currency conversion.
const totals = (rows: Expense[]) => {
  const amounts = new Map<string, bigint>();
  for (const row of rows) {
    const cents = expenseCents(row.amountExact ?? row.amount);
    if (cents === null) return "Totals unavailable";
    const currency = row.currency.trim().toUpperCase();
    amounts.set(currency, (amounts.get(currency) ?? BigInt(0)) + cents);
  }
  return amounts.size ? [...amounts].map(([currency, cents]) => formatExpenseCents(cents, currency)).join(" · ") : formatExpenseCents(BigInt(0), "INR");
};

export default function ExpensesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [branches, setBranches] = useState<Branch[]>([]);
  const [branchAvailable, setBranchAvailable] = useState(false);
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [message, setMessage] = useState("Loading expenses…");
  const [filter, setFilter] = useState("All");
  const [ready, setReady] = useState(false);
  const [saving, setSaving] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const pending = useRef(false);
  const load = useCallback(async (id: string) => {
    const [expenseResponse, branchResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/expenses`, { cache: "no-store" }),
      academyApi(`/api/academies/${id}/branches`, { cache: "no-store" }),
    ]);
    if (!expenseResponse.ok || (!branchResponse.ok && branchResponse.status !== 403)) throw new Error("Expense workspace unavailable.");
    const [rows, branchRows] = await Promise.all([expenseResponse.json(), branchResponse.ok ? branchResponse.json() : Promise.resolve([])]);
    if (!Array.isArray(rows) || !Array.isArray(branchRows)) throw new Error("Invalid expense workspace.");
    setExpenses(rows); setBranches(branchRows); setBranchAvailable(branchResponse.ok);
  }, []);
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (response.status === 401) { if (active) setMessage("Please sign in before recording expenses."); return; }
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) { if (active) setMessage("Create your academy first."); return; }
        if (!active) return;
        setAcademy(academies[0]);
        await load(academies[0].id);
        if (active) { setReady(true); setMessage(""); }
      } catch { if (active) setMessage("Expenses could not be loaded. Please refresh the page or try again later."); }
    })();
    return () => { active = false; };
  }, [load]);

  async function refreshRegister() {
    if (!academy || pending.current) return;
    pending.current = true; setSaving(true);
    try { await load(academy.id); setReady(true); setNeedsReview(false); setMessage("Expense register refreshed. Check for the previous expense before recording it again."); }
    catch { setMessage("The expense register could not be refreshed. Please try again later; do not repeat an unconfirmed expense."); }
    finally { pending.current = false; setSaving(false); }
  }
  async function createExpense(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !ready || needsReview || pending.current) return;
    const captured = { ...draft };
    const amount = expenseAmountJson(captured.amount);
    if (!captured.description.trim() || amount === null) { setMessage("Enter a description and positive expense amount with up to 16 whole digits and two decimal places."); return; }
    pending.current = true; setSaving(true); setMessage("Recording expense…");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/expenses`, {
        method: "POST", headers: apiHeaders(true),
        body: `${JSON.stringify({ description: captured.description, currency: "INR", category: captured.category, branchId: branchAvailable ? captured.branchId || null : null, expenseDate: captured.expenseDate || null }).slice(0, -1)},"amount":${amount}}`,
      });
      if (!response.ok) {
        if (response.status >= 500) { setNeedsReview(true); setMessage("Expense save could not be confirmed. Refresh and check the register before retrying."); }
        else {
          let detail = "";
          try { const body: unknown = await response.json(); if (body && typeof body === "object" && "message" in body && typeof body.message === "string") detail = body.message; } catch { /* Empty error bodies use the safe fallback. */ }
          setMessage(detail || "Expense could not be recorded. Check the details and your access, then try again.");
        }
        return;
      }
      setDraft(current => Object.fromEntries(Object.entries(current).map(([key, value]) => [key, value === captured[key as keyof Draft] ? emptyDraft[key as keyof Draft] : value])) as Draft);
      setMessage("Expense recorded.");
      try { await load(academy.id); }
      catch { setNeedsReview(true); setMessage("Expense recorded. The register could not be refreshed; do not repeat this expense. Refresh the register to continue."); }
    } catch { setNeedsReview(true); setMessage("Expense save could not be confirmed. Refresh and check the register before retrying."); }
    finally { pending.current = false; setSaving(false); }
  }
  const change = (key: keyof Draft, value: string) => setDraft(current => ({ ...current, [key]: value }));
  const branchName = (id?: string | null) => branches.find(branch => branch.id === id)?.name ?? (id ? "Branch unavailable" : "All academy");
  const filtered = useMemo(() => filter === "All" ? expenses : expenses.filter(expense => expense.category === filter), [expenses, filter]);
  const categories = ["General", "TeacherPayout", "Rent", "Utilities", "Marketing", "Supplies", "Equipment"];
  const blocked = !ready || saving || needsReview;
  return <main className={`enterprise-settings finance-module expenses-standard ${styles.content}`}>
    <header className="expenses-heading"><span className="expenses-title-icon" aria-hidden="true">₹</span><div><p>Finance</p><h1>Expenses</h1></div></header>
    {message && <p className="expenses-notice" role="status" aria-live="polite">{message}</p>}
    {academy && <button type="button" disabled={saving} onClick={refreshRegister} className="enterprise-action-button">Refresh expense register</button>}
    <section className="expenses-kpis">
      <article><span>Total recorded</span><b>{totals(expenses)}</b><small>Totals shown separately for each currency.</small></article>
      <article><span>Expenses logged</span><b>{expenses.length}</b><small>Records ready for finance reporting.</small></article>
      <article><span>Teacher payouts</span><b>{totals(expenses.filter(expense => expense.category === "TeacherPayout"))}</b><small>Recorded costs, grouped by currency.</small></article>
    </section>
    <section className="expenses-layout">
      <form onSubmit={createExpense} className="expenses-panel" aria-busy={saving}>
        <header className="expenses-panel-header"><div><p>New expense</p><h2>Record expense</h2></div></header>
        <fieldset className="expenses-fields" disabled={blocked} style={{ border: 0, margin: 0, minWidth: 0 }}>
          <label className="expenses-wide"><span>Description</span><input value={draft.description} onChange={event => change("description", event.target.value)} placeholder="e.g. August piano instructor payment" className="field" maxLength={300} required disabled={blocked} /></label>
          <label><span>Amount</span><input type="number" min="0.01" step="0.01" value={draft.amount} onChange={event => change("amount", event.target.value)} placeholder="Amount in INR" className="field" required disabled={blocked} /></label>
          <label><span>Category</span><StandardSelectField name="expenseCategory" value={draft.category} onChange={value => change("category", value)} placeholder="Choose category" options={categories.map(value => ({ value, label: value === "TeacherPayout" ? "Teacher payout" : value }))} disabled={blocked} /></label>
          <label><span>Branch</span><StandardSelectField name="branchId" value={branchAvailable ? draft.branchId : ""} onChange={value => change("branchId", value)} placeholder="All academy / no branch" options={branches.map(branch => ({ value: branch.id, label: branch.name }))} disabled={blocked || !branchAvailable} />{!branchAvailable && ready && <small>Branch selection is unavailable for this account. You can record an expense without a branch.</small>}</label>
          <StandardDateField name="expenseDate" value={draft.expenseDate} onChange={value => change("expenseDate", value)} label="Expense date" />
          <button disabled={blocked || !academy || !draft.description.trim() || !draft.amount} className="enterprise-action-button expenses-action expenses-wide">{saving ? "Saving…" : "Record expense"}</button>
        </fieldset>
      </form>
      <section className="expenses-panel expenses-register">
        <header className="expenses-panel-header"><div><p>Expense register</p><h2>Recorded expenses</h2></div><div className="expenses-filter"><StandardSelectField name="expenseFilter" value={filter} onChange={setFilter} placeholder="Filter category" options={[{ value: "All", label: `All expenses (${expenses.length})` }, ...categories.map(value => ({ value, label: value === "TeacherPayout" ? "Teacher payout" : value }))]} /></div></header>
        {filtered.length === 0 ? <p className="expenses-empty">No expenses match this view.</p> : <ul className="expenses-list">{filtered.map(expense => <li key={expense.id}><div><b>{expense.description}</b><small>{expense.category === "TeacherPayout" ? "Teacher payout" : expense.category} · {branchName(expense.branchId)} · {expense.expenseDate || "No date recorded"}</small></div><strong>{money(expense)}</strong></li>)}</ul>}
      </section>
    </section>
  </main>;
}
