"use client";

import { FormEvent, useEffect, useState } from "react";
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

  return <main className="enterprise-settings finance-module">
    <header className="enterprise-page-header"><p>Finance / invoices</p><h2>Invoices</h2><span>Issue a fee invoice to a student from a reusable fee plan or a custom amount.</span></header>
    {message && <p className="enterprise-settings-empty mt-5">{message}</p>}
    <section className="mt-5 grid gap-5 lg:grid-cols-[minmax(0,.85fr)_minmax(0,1.15fr)]"><form onSubmit={createInvoice} className="surface-panel rounded-xl p-5"><h3>Create invoice</h3><div className="mt-4 grid gap-3">
      <select value={studentId} onChange={(event) => setStudentId(event.target.value)} className="field" required><option value="">Select student</option>{students.map((student) => <option key={student.id} value={student.id}>{student.firstName} {student.lastName}</option>)}</select>
      <select value={feePlanId} onChange={(event) => choosePlan(event.target.value)} className="field"><option value="">No fee plan — custom amount</option>{plans.map((plan) => <option key={plan.id} value={plan.id}>{plan.name} · {money(plan.amount, plan.currency)}</option>)}</select>
      <input type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Invoice amount in INR" className="field" />
      <label className="field-label">Due date<input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} className="field" /></label>
    </div><button disabled={!academy || !students.length} className="enterprise-action-button mt-5 w-full">Issue invoice</button></form>
    <section className="surface-panel rounded-xl p-5"><h3>Issued invoices</h3>{invoices.length === 0 ? <p className="enterprise-settings-empty mt-4">No invoices yet. Create an invoice once a student and price are ready.</p> : <ul className="mt-4 space-y-3">{invoices.map((invoice) => <li key={invoice.id} className="rounded-lg border border-slate-700 p-4"><div className="flex items-start justify-between gap-4"><div><b>{studentName(invoice.studentId)}</b><p className="mt-1 text-sm text-slate-400">{invoice.invoiceNumber} · Due {invoice.dueDate}</p></div><div className="text-right"><b className="text-cyan-200">{money(invoice.totalAmount, invoice.currency)}</b><p className="mt-1 text-sm text-slate-400">{invoice.status}</p></div></div></li>)}</ul>}</section></section>
  </main>;
}
