"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
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

  return <main className="min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="mx-auto max-w-6xl px-6 py-10">
    <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Finance</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Expenses</h1><p className="mt-3 text-slate-300">Record operating costs such as teacher payouts, rent, instruments, marketing, and supplies.</p>
    {message && <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className="mt-8 grid gap-6 lg:grid-cols-[0.85fr_1.15fr]"><form onSubmit={createExpense} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Record expense</h2><input value={description} onChange={(event) => setDescription(event.target.value)} placeholder="e.g. August piano instructor payment" className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><input type="number" min="0.01" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Amount in INR" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><select value={category} onChange={(event) => setCategory(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option>General</option><option>TeacherPayout</option><option>Rent</option><option>Utilities</option><option>Marketing</option><option>Supplies</option><option>Equipment</option></select><select value={branchId} onChange={(event) => setBranchId(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option value="">All academy / no branch</option>{branches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name}</option>)}</select><label className="mt-4 block text-sm text-slate-300">Expense date</label><input type="date" value={expenseDate} onChange={(event) => setExpenseDate(event.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><button disabled={!academy} className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60">Record expense</button></form>
    <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><div className="flex items-center justify-between"><h2 className="text-xl font-semibold">Recorded expenses</h2><span className="font-semibold text-cyan-200">{money(total)}</span></div>{expenses.length === 0 ? <p className="mt-6 text-slate-400">No expenses yet.</p> : <ul className="mt-5 space-y-3">{expenses.map((expense) => <li key={expense.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="flex items-start justify-between gap-4"><div><div className="font-medium">{expense.description}</div><div className="mt-1 text-sm text-slate-400">{expense.category} · {branchName(expense.branchId)} · {expense.expenseDate}</div></div><div className="font-semibold text-cyan-200">{money(expense.amount, expense.currency)}</div></div></li>)}</ul>}</section>
    </section>
  </div></main>;
}
