"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Invoice = { id: string; invoiceNumber: string; studentId: string; totalAmount: number; currency: string; dueDate: string; status: string };
type Payment = { id: string; invoiceId: string; amount: number; currency: string; method: string; status: string; reference?: string | null; paidAtUtc: string };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency }).format(amount);

export default function PaymentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [invoiceId, setInvoiceId] = useState("");
  const [amount, setAmount] = useState("");
  const [method, setMethod] = useState("UPI");
  const [reference, setReference] = useState("");
  const [message, setMessage] = useState("Loading payments…");

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [studentResponse, invoiceResponse, paymentResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/students`, { cache: "no-store" }), academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/payments`, { cache: "no-store" }),
    ]);
    if (![studentResponse, invoiceResponse, paymentResponse].every((response) => response.ok)) throw new Error();
    setStudents(await studentResponse.json()); setInvoices(await invoiceResponse.json()); setPayments(await paymentResponse.json()); setMessage("");
  }

  useEffect(() => { async function initialise() { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (response.status === 401) return setMessage("Please sign in before recording payments."); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create your academy first."); setAcademy(academies[0]); await load(academies[0].id); } catch { setMessage("Payments could not be loaded. Confirm the API is running on port 5092."); } } void initialise(); }, []);

  const paidByInvoice = useMemo(() => payments.reduce<Record<string, number>>((totals, payment) => ({ ...totals, [payment.invoiceId]: (totals[payment.invoiceId] ?? 0) + (payment.status === "Completed" ? payment.amount : 0) }), {}), [payments]);
  const selectedInvoice = invoices.find((invoice) => invoice.id === invoiceId);
  const selectedBalance = selectedInvoice ? selectedInvoice.totalAmount - (paidByInvoice[selectedInvoice.id] ?? 0) : 0;

  function selectInvoice(id: string) {
    setInvoiceId(id);
    const invoice = invoices.find((item) => item.id === id);
    if (invoice) setAmount((invoice.totalAmount - (paidByInvoice[id] ?? 0)).toString());
  }

  async function recordPayment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !invoiceId) return;
    const response = await academyApi(`/api/academies/${academy.id}/payments`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ invoiceId, amount: Number(amount), method, reference: reference || null }) });
    if (!response.ok) return setMessage("The payment could not be saved. The amount must not exceed the remaining balance.");
    setInvoiceId(""); setAmount(""); setReference(""); setMessage(""); await load();
  }

  const studentName = (id: string) => { const student = students.find((item) => item.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };
  const balance = (invoice: Invoice) => invoice.totalAmount - (paidByInvoice[invoice.id] ?? 0);

  return <main className="enterprise-settings finance-module">
    <header className="enterprise-page-header"><p>Finance / payments</p><h2>Payments and balances</h2><span>Record payments against invoices while keeping balances and invoice status up to date.</span></header>
    {message && <p className="enterprise-settings-empty mt-5">{message}</p>}
    <section className="mt-5 grid gap-5 lg:grid-cols-[minmax(0,.85fr)_minmax(0,1.15fr)]"><form onSubmit={recordPayment} className="surface-panel rounded-xl p-5"><h3>Record payment</h3><div className="mt-4 grid gap-3">
      <select value={invoiceId} onChange={(event) => selectInvoice(event.target.value)} className="field" required><option value="">Select invoice</option>{invoices.filter((invoice) => balance(invoice) > 0).map((invoice) => <option key={invoice.id} value={invoice.id}>{invoice.invoiceNumber} · {studentName(invoice.studentId)} · Balance {money(balance(invoice), invoice.currency)}</option>)}</select>
      {selectedInvoice && <p className="text-sm text-cyan-200">Remaining balance: {money(selectedBalance, selectedInvoice.currency)}</p>}
      <input type="number" min="0.01" max={selectedBalance || undefined} step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Payment amount" className="field" required />
      <select value={method} onChange={(event) => setMethod(event.target.value)} className="field"><option>UPI</option><option>Cash</option><option>BankTransfer</option><option>Card</option><option>Cheque</option><option>Offline</option></select>
      <input value={reference} onChange={(event) => setReference(event.target.value)} placeholder="UPI, cheque, or receipt reference (optional)" className="field" />
    </div><button disabled={!academy || !invoiceId || !amount} className="enterprise-action-button mt-5 w-full">Record payment</button></form>
    <section className="surface-panel rounded-xl p-5"><h3>Invoice balances</h3>{invoices.length === 0 ? <p className="enterprise-settings-empty mt-4">No invoices yet. Issue an invoice before recording a payment.</p> : <ul className="mt-4 space-y-3">{invoices.map((invoice) => <li key={invoice.id} className="rounded-lg border border-slate-700 p-4"><div className="flex flex-wrap items-start justify-between gap-3"><div><b>{studentName(invoice.studentId)}</b><p className="mt-1 text-sm text-slate-400">{invoice.invoiceNumber} · Due {invoice.dueDate}</p></div><div className="text-right"><b className="text-cyan-200">Balance {money(balance(invoice), invoice.currency)}</b><p className="mt-1 text-sm text-slate-400">of {money(invoice.totalAmount, invoice.currency)} · {invoice.status}</p></div></div></li>)}</ul>}</section></section>
  </main>;
}
