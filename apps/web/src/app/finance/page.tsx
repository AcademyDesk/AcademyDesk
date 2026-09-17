"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi } from "@/lib/api";

type Academy = { id: string };
type Invoice = { id: string; invoiceNumber: string; studentId: string; totalAmount: number; currency: string; dueDate: string; status: string };
type Payment = { id: string; invoiceId: string; amount: number; currency: string; method: string; status: string; paidAtUtc: string };
type Expense = { id: string; description: string; amount: number; currency: string; category: string; expenseDate: string };
type Student = { id: string; firstName: string; lastName: string };
const money = (amount: number) => new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR" }).format(amount);

export default function FinancePage() {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [message, setMessage] = useState("Loading financial summary…");

  useEffect(() => { async function load() { try { const academyResponse = await academyApi("/api/academies", { cache: "no-store" }); if (academyResponse.status === 401) return setMessage("Please sign in before opening the finance dashboard."); if (!academyResponse.ok) throw new Error(); const academies: Academy[] = await academyResponse.json(); if (!academies[0]) return setMessage("Create your academy first."); const id = academies[0].id; const [invoiceResponse, paymentResponse, expenseResponse, studentResponse] = await Promise.all([academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/payments`, { cache: "no-store" }), academyApi(`/api/academies/${id}/expenses`, { cache: "no-store" }), academyApi(`/api/academies/${id}/students`, { cache: "no-store" })]); if (![invoiceResponse, paymentResponse, expenseResponse, studentResponse].every((response) => response.ok)) throw new Error(); setInvoices(await invoiceResponse.json()); setPayments(await paymentResponse.json()); setExpenses(await expenseResponse.json()); setStudents(await studentResponse.json()); setMessage(""); } catch { setMessage("Finance data could not be loaded. Confirm the API is running on port 5092."); } } void load(); }, []);

  const paidByInvoice = useMemo(() => payments.filter((payment) => payment.status === "Completed").reduce<Record<string, number>>((total, payment) => ({ ...total, [payment.invoiceId]: (total[payment.invoiceId] ?? 0) + payment.amount }), {}), [payments]);
  const invoiced = invoices.reduce((total, invoice) => total + invoice.totalAmount, 0);
  const collected = payments.filter((payment) => payment.status === "Completed").reduce((total, payment) => total + payment.amount, 0);
  const spent = expenses.reduce((total, expense) => total + expense.amount, 0);
  const outstanding = Math.max(0, invoiced - collected);
  const overdue = invoices.filter((invoice) => invoice.dueDate < new Date().toISOString().slice(0, 10) && invoice.totalAmount > (paidByInvoice[invoice.id] ?? 0));
  const nameFor = (studentId: string) => { const student = students.find((item) => item.id === studentId); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };

  const metrics = [["Invoiced", invoiced, "Total issued invoices"], ["Collected", collected, "Completed payments"], ["Outstanding", outstanding, `${overdue.length} overdue invoice${overdue.length === 1 ? "" : "s"}`], ["Net cash", collected - spent, `Expenses: ${money(spent)}`]] as const;

  return <main className="min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="mx-auto max-w-6xl px-6 py-10">
    <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Finance</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Finance dashboard</h1><p className="mt-3 text-slate-300">A clear owner view of fees, collections, unpaid balances, expenses, and cash position.</p>
    {message && <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">{metrics.map(([label, amount, detail]) => <div key={label} className="rounded-2xl border border-slate-800 bg-slate-900 p-5"><p className="text-sm text-slate-400">{label}</p><p className="mt-2 text-3xl font-semibold">{money(amount)}</p><p className="mt-2 text-sm text-slate-300">{detail}</p></div>)}</section>
    <section className="mt-8 grid gap-6 lg:grid-cols-2"><section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><div className="flex items-center justify-between"><h2 className="text-xl font-semibold">Outstanding invoices</h2><Link href="/payments" className="text-sm text-cyan-300">Record payment →</Link></div>{invoices.filter((invoice) => invoice.totalAmount > (paidByInvoice[invoice.id] ?? 0)).length === 0 ? <p className="mt-6 text-slate-400">No outstanding invoices.</p> : <ul className="mt-5 space-y-3">{invoices.filter((invoice) => invoice.totalAmount > (paidByInvoice[invoice.id] ?? 0)).slice(0, 6).map((invoice) => <li key={invoice.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="flex justify-between gap-3"><div><div className="font-medium">{nameFor(invoice.studentId)}</div><div className="mt-1 text-sm text-slate-400">{invoice.invoiceNumber} · Due {invoice.dueDate}</div></div><div className="font-semibold text-amber-200">{money(invoice.totalAmount - (paidByInvoice[invoice.id] ?? 0))}</div></div></li>)}</ul>}</section>
    <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><div className="flex items-center justify-between"><h2 className="text-xl font-semibold">Recent expenses</h2><Link href="/expenses" className="text-sm text-cyan-300">Add expense →</Link></div>{expenses.length === 0 ? <p className="mt-6 text-slate-400">No expenses recorded.</p> : <ul className="mt-5 space-y-3">{expenses.slice(0, 6).map((expense) => <li key={expense.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="flex justify-between gap-3"><div><div className="font-medium">{expense.description}</div><div className="mt-1 text-sm text-slate-400">{expense.category} · {expense.expenseDate}</div></div><div className="font-semibold text-rose-200">{money(expense.amount)}</div></div></li>)}</ul>}</section></section>
  </div></main>;
}
