"use client";

import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Branch = { id: string; name: string };
type Expense = { id: string; description: string; amount: number; currency: string; category: string; branchId?: string | null; expenseDate: string; status: string };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency }).format(amount);

export default function ExpensesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [branches, setBranches] = useState<Branch[]>([]);
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [description, setDescription] = useState("");
  const [amount, setAmount] = useState("");
  const [category, setCategory] = useState("General");
  const [branchId, setBranchId] = useState("");
  const [expenseDate, setExpenseDate] = useState("");
  const [message, setMessage] = useState("Loading expenses…");

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [expenseResponse, branchResponse] = await Promise.all([academyApi(`/api/academies/${id}/expenses`, { cache: "no-store" }), academyApi(`/api/academies/${id}/branches`, { cache: "no-store" })]);
    if (!expenseResponse.ok || !branchResponse.ok) throw new Error();
    setExpenses(await expenseResponse.json()); setBranches(await branchResponse.json()); setMessage("");
  }

  useEffect(() => { async function initialise() { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (response.status === 401) return setMessage("Please sign in before recording expenses."); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create your academy first."); setAcademy(academies[0]); await load(academies[0].id); } catch { setMessage("Expenses could not be loaded. Confirm the API is running on port 5092."); } } void initialise(); }, []);

  async function createExpense(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const response = await academyApi(`/api/academies/${academy.id}/expenses`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ description, amount: Number(amount), currency: "INR", category, branchId: branchId || null, expenseDate: expenseDate || null }) });
    if (!response.ok) return setMessage("Enter a description and positive expense amount.");
    setDescription(""); setAmount(""); setCategory("General"); setBranchId(""); setExpenseDate(""); setMessage(""); await load();
  }

  const branchName = (id?: string | null) => branches.find((branch) => branch.id === id)?.name ?? "All academy";
  const total = expenses.reduce((sum, expense) => sum + expense.amount, 0);

  return <main className="enterprise-settings finance-module">
    <header className="enterprise-page-header"><p>Finance / expenses</p><h2>Expenses</h2><span>Record operating costs such as teacher payouts, rent, instruments, marketing, and supplies.</span></header>
    {message && <p className="enterprise-settings-empty mt-5">{message}</p>}
    <section className="mt-5 grid gap-5 lg:grid-cols-[minmax(0,.85fr)_minmax(0,1.15fr)]"><form onSubmit={createExpense} className="surface-panel rounded-xl p-5"><h3>Record expense</h3><div className="mt-4 grid gap-3"><input value={description} onChange={(event) => setDescription(event.target.value)} placeholder="e.g. August piano instructor payment" className="field" required /><input type="number" min="0.01" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Amount in INR" className="field" required /><select value={category} onChange={(event) => setCategory(event.target.value)} className="field"><option>General</option><option>TeacherPayout</option><option>Rent</option><option>Utilities</option><option>Marketing</option><option>Supplies</option><option>Equipment</option></select><select value={branchId} onChange={(event) => setBranchId(event.target.value)} className="field"><option value="">All academy / no branch</option>{branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}</select><label className="field-label">Expense date<input type="date" value={expenseDate} onChange={(event) => setExpenseDate(event.target.value)} className="field" /></label></div><button disabled={!academy} className="enterprise-action-button mt-5 w-full">Record expense</button></form>
    <section className="surface-panel rounded-xl p-5"><div className="flex items-center justify-between gap-3"><h3>Recorded expenses</h3><b className="text-cyan-200">{money(total)}</b></div>{expenses.length === 0 ? <p className="enterprise-settings-empty mt-4">No expenses yet. Record costs here to keep the finance view complete.</p> : <ul className="mt-4 space-y-3">{expenses.map((expense) => <li key={expense.id} className="rounded-lg border border-slate-700 p-4"><div className="flex items-start justify-between gap-4"><div><b>{expense.description}</b><p className="mt-1 text-sm text-slate-400">{expense.category} · {branchName(expense.branchId)} · {expense.expenseDate}</p></div><b className="text-cyan-200">{money(expense.amount, expense.currency)}</b></div></li>)}</ul>}</section></section>
  </main>;
}
