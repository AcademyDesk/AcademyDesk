"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import { reportAmount, reportMoney } from "@/lib/reports-finance";

type Academy = { id: string };
type Branch = { id: string; name: string };
type Expense = { id: string; description: string; amount: number; currency?: string; category: string; branchId?: string | null; expenseDate: string; status: string };
type ReviewOutcome = "none" | "saved" | "unconfirmed";
class ExpenseReadError extends Error { constructor(readonly status: number) { super("Expense records unavailable"); } }

export default function ExpensesPage() {
  const [academy, setAcademy] = useState<Academy>(); const [branches, setBranches] = useState<Branch[]>([]); const [expenses, setExpenses] = useState<Expense[]>([]); const [description, setDescription] = useState(""); const [amount, setAmount] = useState(""); const [category, setCategory] = useState("General"); const [branchId, setBranchId] = useState(""); const [expenseDate, setExpenseDate] = useState(""); const [message, setMessage] = useState("Loading expenses…"); const [filter, setFilter] = useState("All");
  const [loadState, setLoadState] = useState<"loading" | "ready" | "signin" | "denied" | "retry" | "empty">("loading");
  const [busy, setBusy] = useState(false);
  const [review, setReview] = useState<ReviewOutcome>("none");
  const [reviewRead, setReviewRead] = useState(false);
  const [reload, setReload] = useState(0);
  const [noticeRole, setNoticeRole] = useState<"status" | "alert">("status");
  const pending = useRef(false), reviewRequired = useRef<ReviewOutcome>("none"), alive = useRef(true), generation = useRef(0), draftVersion = useRef(0);
  const renderedDraftVersion = draftVersion.current;

  async function readRegister(id: string) {
    const responses = await Promise.all([
      academyApi(`/api/academies/${id}/expenses`, { cache: "no-store" }),
      academyApi(`/api/academies/${id}/expenses/branch-options`, { cache: "no-store" }),
    ]);
    for (const response of responses) if (!response.ok) throw new ExpenseReadError(response.status);
    const [expenseRows, branchRows]: unknown[] = await Promise.all(responses.map(response => response.json()));
    if (!Array.isArray(expenseRows) || !Array.isArray(branchRows)) throw new Error("Invalid register response");
    return { expenses: expenseRows as Expense[], branches: branchRows as Branch[] };
  }
  function applyRegister(rows: { expenses: Expense[]; branches: Branch[] }) { setExpenses(rows.expenses); setBranches(rows.branches); }
  function clearDraft() { draftVersion.current++; setDescription(""); setAmount(""); setCategory("General"); setBranchId(""); setExpenseDate(""); }
  function showReadFailure(error: unknown) {
    const prefix = reviewRequired.current === "saved" ? "Expense saved, but financial records could not be refreshed. "
      : reviewRequired.current === "unconfirmed" ? "Save could not be confirmed. Your entries are retained. " : "";
    setNoticeRole("alert");
    if (error instanceof ExpenseReadError && error.status === 401) { setLoadState("signin"); setMessage(prefix + "Your session has ended. Sign in again to open expenses."); }
    else if (error instanceof ExpenseReadError && error.status === 403) { setLoadState("denied"); setMessage(prefix + "You do not have access to the records required for expenses. Contact your academy administrator."); }
    else { setLoadState("retry"); setMessage(prefix + (error instanceof ExpenseReadError && error.status === 429
      ? "Too many requests. Please wait a moment, then reload financial records."
      : reviewRequired.current !== "none" ? "Reload financial records to check the latest register before recording again."
      : "Expenses could not be loaded. Check your connection and try again.")); }
  }
  function reloadRecords() {
    if (pending.current) return;
    setLoadState("loading"); setReviewRead(false); setNoticeRole("status");
    setMessage(reviewRequired.current === "saved" ? "Expense saved. Loading financial records…" : "Loading expenses…");
    setReload(value => value + 1);
  }
  useEffect(() => {
    alive.current = true; const epoch = ++generation.current; let cancelled = false;
    const current = () => alive.current && !cancelled && epoch === generation.current;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!current()) return;
        if (!response.ok) throw new ExpenseReadError(response.status);
        const academies: unknown = await response.json();
        if (!current()) return;
        if (!Array.isArray(academies)) throw new Error("Invalid academy response");
        if (!academies[0]) { setLoadState("empty"); setMessage("Create your academy first."); return; }
        const selected = academies[0] as Academy, rows = await readRegister(selected.id);
        if (!current()) return;
        setAcademy(selected); applyRegister(rows); setLoadState("ready");
        if (reviewRequired.current === "unconfirmed") { setReviewRead(true); setNoticeRole("alert"); setMessage("The register is refreshed. Check for the expense before starting a new entry. No submission was repeated."); }
        else { const saved = reviewRequired.current === "saved"; reviewRequired.current = "none"; setReview("none"); setMessage(saved ? "Expense saved. Financial records refreshed." : ""); }
      } catch (error) { if (current()) showReadFailure(error); }
    })();
    return () => { cancelled = true; alive.current = false; };
  }, [reload]);

  async function createExpense(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!alive.current || pending.current || reviewRequired.current !== "none" || loadState !== "ready" || !academy || renderedDraftVersion !== draftVersion.current) return;
    const numericAmount = Number(amount);
    if (!description.trim() || !amount.trim() || !Number.isFinite(numericAmount) || numericAmount <= 0) { setNoticeRole("alert"); setMessage("Enter a description and positive expense amount."); return; }
    const epoch = generation.current, current = () => alive.current && epoch === generation.current;
    pending.current = true; setBusy(true); setNoticeRole("status"); setMessage("Recording expense…"); let saved = false;
    try {
      const response = await academyApi(`/api/academies/${academy.id}/expenses`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ description, amount: numericAmount, currency: "INR", category, branchId: branchId || null, expenseDate: expenseDate || null }) });
      if (!current()) return;
      if (!response.ok) {
        if (response.status >= 500) throw new Error("Unconfirmed expense submission");
        setNoticeRole("alert"); setMessage(response.status === 401 ? "Your session has ended. Sign in again before recording an expense."
          : response.status === 403 ? "You do not have permission to record this expense. Contact your academy administrator."
          : response.status === 429 ? "Too many requests. Wait a moment before trying again."
          : "The expense could not be saved. Check its description, positive amount and branch, then try again.");
        return;
      }
      saved = true; clearDraft(); reviewRequired.current = "saved"; setReview("saved"); setReviewRead(false);
      setLoadState("loading"); setNoticeRole("status"); setMessage("Expense saved. Refreshing financial records…");
      const rows = await readRegister(academy.id);
      if (!current()) return;
      applyRegister(rows); reviewRequired.current = "none"; setReview("none"); setLoadState("ready"); setMessage("Expense recorded.");
    } catch (error) {
      if (current()) {
        reviewRequired.current = saved ? "saved" : "unconfirmed"; setReview(reviewRequired.current); setReviewRead(false);
        if (saved) showReadFailure(error);
        else { setLoadState("retry"); setNoticeRole("alert"); setMessage("Save could not be confirmed. Your entries are retained. Reload financial records and check for an existing record before starting a new expense."); }
      }
    } finally { pending.current = false; if (current()) setBusy(false); }
  }
  function startNewExpense() {
    if (!alive.current || pending.current || loadState !== "ready" || !reviewRead || reviewRequired.current !== "unconfirmed") return;
    clearDraft(); reviewRequired.current = "none"; setReview("none"); setReviewRead(false); setNoticeRole("status"); setMessage("New expense ready. No previous submission was repeated.");
  }
  const branchName = (id?: string | null) => branches.find(branch => branch.id === id)?.name ?? "All academy"; const total = reportMoney(expenses); const filtered = useMemo(() => filter === "All" ? expenses : expenses.filter(expense => expense.category === filter), [expenses, filter]); const categories = ["General", "TeacherPayout", "Rent", "Utilities", "Marketing", "Supplies", "Equipment"];
  if (loadState !== "ready") return <main className="enterprise-settings finance-module expenses-standard">
    <header className="expenses-heading"><div><p>Finance</p><h1>Expenses</h1></div></header>
    <p className="expenses-notice" role={noticeRole}>{message}</p>
    {loadState === "retry" && <button type="button" className="enterprise-action-button" disabled={busy} onClick={reloadRecords}>{review === "none" ? "Try again" : "Reload financial records"}</button>}
    {loadState === "signin" && <a href="/login?returnTo=/expenses">Sign in again</a>}
    {loadState === "denied" && <a href="/login">Use another account</a>}
  </main>;
  return <main className="enterprise-settings finance-module expenses-standard"><header className="expenses-heading"><span className="expenses-title-icon" aria-hidden="true">₹</span><div><p>Finance</p><h1>Expenses</h1></div></header>{message && <p className="expenses-notice" role={noticeRole}>{message}</p>}{review === "unconfirmed" && <div className="expenses-notice"><button type="button" className="enterprise-action-button" disabled={!reviewRead || busy} onClick={startNewExpense}>Start a new expense</button></div>}<section className="expenses-kpis"><article><span>Total recorded</span><b>{total}</b><small>All operating costs in this register.</small></article><article><span>Expenses logged</span><b>{expenses.length}</b><small>Records ready for finance reporting.</small></article><article><span>Teacher payouts</span><b>{reportMoney(expenses.filter(expense => expense.category === "TeacherPayout"))}</b><small>Costs recorded as teacher payouts.</small></article></section><section className="expenses-layout"><form onSubmit={createExpense} aria-busy={busy} className="expenses-panel"><fieldset className="contents" disabled={busy || review !== "none"}><header className="expenses-panel-header"><div><p>New expense</p><h2>Record expense</h2></div></header><div className="expenses-fields"><label className="expenses-wide"><span>Description</span><input value={description} onChange={event => setDescription(event.target.value)} placeholder="e.g. August piano instructor payment" className="field" required /></label><label><span>Amount</span><input type="number" min="0.01" step="0.01" value={amount} onChange={event => setAmount(event.target.value)} placeholder="Amount in INR" className="field" required /></label><label><span>Category</span><StandardSelectField name="expenseCategory" value={category} onChange={setCategory} placeholder="Choose category" options={categories.map(value => ({ value, label: value === "TeacherPayout" ? "Teacher payout" : value }))} /></label><label><span>Branch</span><StandardSelectField name="branchId" value={branchId} onChange={setBranchId} placeholder="All academy / no branch" options={branches.map(branch => ({ value: branch.id, label: branch.name }))} /></label><StandardDateField name="expenseDate" value={expenseDate} onChange={setExpenseDate} label="Expense date" /><button disabled={busy || review !== "none" || !academy || !description.trim() || !amount} className="enterprise-action-button expenses-action expenses-wide">Record expense</button></div></fieldset></form><section className="expenses-panel expenses-register"><header className="expenses-panel-header"><div><p>Expense register</p><h2>Recorded expenses</h2></div><div className="expenses-filter"><StandardSelectField name="expenseFilter" value={filter} onChange={setFilter} placeholder="Filter category" options={[{ value: "All", label: `All expenses (${expenses.length})` }, ...categories.map(value => ({ value, label: value === "TeacherPayout" ? "Teacher payout" : value }))]} /></div></header>{filtered.length === 0 ? <p className="expenses-empty">No expenses match this view.</p> : <ul className="expenses-list">{filtered.map(expense => <li key={expense.id}><div><b>{expense.description}</b><small>{expense.category === "TeacherPayout" ? "Teacher payout" : expense.category} · {branchName(expense.branchId)} · {expense.expenseDate || "No date recorded"}</small></div><strong>{reportAmount(expense.amount, expense.currency)}</strong></li>)}</ul>}</section></section></main>;
}
