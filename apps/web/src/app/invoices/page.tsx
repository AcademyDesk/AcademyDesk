"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type FeePlan = { id: string; name: string; amount: number; currency: string; frequency: string };
type Invoice = { id: string; invoiceNumber: string; studentId: string; feePlanId?: string | null; totalAmount: number; currency: string; issuedDate: string; dueDate: string; status: string };

const money = (amount: number, currency: string) => new Intl.NumberFormat("en-IN", { style: "currency", currency }).format(amount);

export default function InvoicesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [plans, setPlans] = useState<FeePlan[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [studentId, setStudentId] = useState("");
  const [feePlanId, setFeePlanId] = useState("");
  const [amount, setAmount] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [message, setMessage] = useState("Loading invoices…");

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [studentResponse, planResponse, invoiceResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/students`, { cache: "no-store" }), academyApi(`/api/academies/${id}/fee-plans`, { cache: "no-store" }), academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }),
    ]);
    if (![studentResponse, planResponse, invoiceResponse].every((response) => response.ok)) throw new Error();
    const studentData: Student[] = await studentResponse.json();
    setStudents(studentData); setPlans(await planResponse.json()); setInvoices(await invoiceResponse.json());
    if (!studentId && studentData.length) setStudentId(studentData[0].id);
    setMessage("");
  }

  useEffect(() => { async function initialise() { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (response.status === 401) return setMessage("Please sign in before creating invoices."); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create your academy first."); setAcademy(academies[0]); await load(academies[0].id); } catch { setMessage("Invoices could not be loaded. Confirm the API is running on port 5092."); } } void initialise(); }, []);

  function choosePlan(id: string) {
    setFeePlanId(id);
    const plan = plans.find((item) => item.id === id);
    if (plan) setAmount(plan.amount.toString());
  }

  async function createInvoice(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !studentId) return;
    const response = await academyApi(`/api/academies/${academy.id}/invoices`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, feePlanId: feePlanId || null, amount: amount ? Number(amount) : null, dueDate: dueDate || null }) });
    if (!response.ok) return setMessage("The invoice could not be saved. Select a fee plan or enter a positive amount.");
    setFeePlanId(""); setAmount(""); setDueDate(""); setMessage(""); await load();
  }

  const studentName = (id: string) => { const student = students.find((item) => item.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };

  return <main className="min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="mx-auto max-w-6xl px-6 py-10">
    <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Finance</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Invoices</h1><p className="mt-3 text-slate-300">Issue a fee invoice to a student from a reusable fee plan or a custom amount.</p>
    {message && <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}
    <section className="mt-8 grid gap-6 lg:grid-cols-[0.85fr_1.15fr]"><form onSubmit={createInvoice} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Create invoice</h2>
      <select value={studentId} onChange={(event) => setStudentId(event.target.value)} className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required><option value="">Select student</option>{students.map((student) => <option key={student.id} value={student.id}>{student.firstName} {student.lastName}</option>)}</select>
      <select value={feePlanId} onChange={(event) => choosePlan(event.target.value)} className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"><option value="">No fee plan — custom amount</option>{plans.map((plan) => <option key={plan.id} value={plan.id}>{plan.name} · {money(plan.amount, plan.currency)}</option>)}</select>
      <input type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Invoice amount in INR" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" />
      <label className="mt-4 block text-sm text-slate-300">Due date</label><input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" />
      <button disabled={!academy || !students.length} className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60">Issue invoice</button>
    </form>
    <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Issued invoices</h2>{invoices.length === 0 ? <p className="mt-6 text-slate-400">No invoices yet.</p> : <ul className="mt-5 space-y-3">{invoices.map((invoice) => <li key={invoice.id} className="rounded-lg border border-slate-700 bg-slate-950 p-4"><div className="flex items-start justify-between gap-4"><div><div className="font-medium">{studentName(invoice.studentId)}</div><div className="mt-1 text-sm text-slate-400">{invoice.invoiceNumber} · Due {invoice.dueDate}</div></div><div className="text-right"><div className="font-semibold text-cyan-200">{money(invoice.totalAmount, invoice.currency)}</div><div className="mt-1 text-sm text-slate-400">{invoice.status}</div></div></div></li>)}</ul>}</section>
    </section>
  </div></main>;
}
