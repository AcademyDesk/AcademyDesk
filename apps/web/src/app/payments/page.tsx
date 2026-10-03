"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Invoice = { id: string; invoiceNumber: string; studentId: string; totalAmount: number; adjustedAmount: number; balance: number; currency: string; dueDate: string; status: string };
type Payment = { id: string; invoiceId: string; amount: number; currency: string; method: string; status: string; reference?: string | null; paidAtUtc: string };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency }).format(amount);
// The invoice API owns applied-adjustment and collected-payment arithmetic.
const balance = (invoice: Invoice) => invoice.balance;

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
  const [messageIsError, setMessageIsError] = useState(false);
  const [saving, setSaving] = useState(false);

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [studentResponse, invoiceResponse, paymentResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/invoices/student-options`, { cache: "no-store" }), academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/payments`, { cache: "no-store" }),
    ]);
    if (![studentResponse, invoiceResponse, paymentResponse].every((response) => response.ok)) throw new Error();
    setStudents(await studentResponse.json()); setInvoices(await invoiceResponse.json()); setPayments(await paymentResponse.json());
  }

  useEffect(() => { async function initialise() { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (response.status === 401) return setMessage("Please sign in before recording payments."); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create your academy first."); setAcademy(academies[0]); await load(academies[0].id); setMessage(""); } catch { setMessageIsError(true); setMessage("Payments could not be loaded. Please try again."); } } void initialise(); }, []);

  const selectedInvoice = invoices.find((invoice) => invoice.id === invoiceId);
  const selectedBalance = selectedInvoice ? balance(selectedInvoice) : 0;
  const paymentTotals = useMemo(() => ({ collected: payments.filter((payment) => payment.status === "Completed" || payment.status === "Reconciled").reduce((total, payment) => total + payment.amount, 0), open: invoices.filter((invoice) => balance(invoice) > 0).length, outstanding: invoices.reduce((total, invoice) => total + Math.max(0, balance(invoice)), 0) }), [invoices, payments]);

  function selectInvoice(id: string) {
    setInvoiceId(id);
    const invoice = invoices.find((item) => item.id === id);
    if (invoice) setAmount(balance(invoice).toString());
  }

  async function recordPayment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !invoiceId || saving) return;
    setSaving(true);
    setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/payments`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ invoiceId, amount: Number(amount), method, reference: reference || null }) });
      if (!response.ok) {
        const problem = await response.json().catch(() => null);
        setMessageIsError(true);
        setMessage(typeof problem?.message === "string" ? problem.message : "The payment could not be saved. Please check the invoice balance and try again.");
        await load().catch(() => undefined);
        if (response.status === 400 && problem?.message === "Payment exceeds the invoice balance.") {
          setInvoiceId(""); setAmount("");
        }
        return;
      }
      await load();
      setInvoiceId(""); setAmount(""); setReference("");
      setMessageIsError(false);
      setMessage("Payment recorded successfully. Invoice balances are up to date.");
    } catch {
      setMessageIsError(true);
      setMessage("The payment could not be confirmed. Refresh balances before trying again to avoid a duplicate.");
    } finally {
      setSaving(false);
    }
  }

  const studentName = (id: string) => { const student = students.find((item) => item.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };

  return <main className="enterprise-settings finance-module payments-standard">
    <header className="enterprise-page-header"><p>Finance / payments</p><h2>Payments and balances</h2><span>Record payments against invoices while keeping balances and invoice status up to date.</span></header>
    {message && <p role={messageIsError ? "alert" : "status"} className="enterprise-settings-empty mt-5">{message}</p>}
    <section className="payments-kpis"><article><span>Payments recorded</span><strong>{payments.length}</strong></article><article><span>Collected</span><strong>{money(paymentTotals.collected)}</strong></article><article><span>Open balances</span><strong>{paymentTotals.open}</strong></article><article><span>Outstanding</span><strong>{money(paymentTotals.outstanding)}</strong></article></section>
    <section className="payments-layout"><form onSubmit={recordPayment} className="surface-panel payments-create"><header><p>Record</p><h3>Payment received</h3></header><div className="payments-fields">
      <select value={invoiceId} onChange={(event) => selectInvoice(event.target.value)} className="field" required><option value="">Select invoice</option>{invoices.filter((invoice) => balance(invoice) > 0).map((invoice) => <option key={invoice.id} value={invoice.id}>{invoice.invoiceNumber} · {studentName(invoice.studentId)} · Balance {money(balance(invoice), invoice.currency)}</option>)}</select>
      {selectedInvoice && <article className="payments-selected"><span>Selected invoice</span><b>{selectedInvoice.invoiceNumber}</b><small>{studentName(selectedInvoice.studentId)} · Balance {money(selectedBalance, selectedInvoice.currency)}</small></article>}
      <input type="number" min="0.01" max={selectedBalance || undefined} step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Payment amount" className="field" required />
      <select value={method} onChange={(event) => setMethod(event.target.value)} className="field"><option>UPI</option><option>Cash</option><option>BankTransfer</option><option>Card</option><option>Cheque</option><option>Offline</option></select>
      <input value={reference} onChange={(event) => setReference(event.target.value)} placeholder="UPI, cheque, or receipt reference (optional)" className="field" />
    </div><button disabled={!academy || !invoiceId || !amount || saving} className="enterprise-action-button">{saving ? "Recording…" : "Record payment"}</button></form>
    <section className="surface-panel payments-directory"><header><p>Balances</p><h3>Invoice balances</h3></header>{invoices.length === 0 ? <p className="enterprise-settings-empty mt-4">No invoices yet. Issue an invoice before recording a payment.</p> : <ul>{invoices.map((invoice) => <li key={invoice.id}><div className="flex flex-wrap items-start justify-between gap-3"><div><b>{studentName(invoice.studentId)}</b><p className="mt-1 text-sm text-slate-400">{invoice.invoiceNumber} · Due {invoice.dueDate}</p></div><div className="text-right"><b className="text-cyan-200">Balance {money(balance(invoice), invoice.currency)}</b><p className="mt-1 text-sm text-slate-400">of {money(invoice.totalAmount - invoice.adjustedAmount, invoice.currency)} due{invoice.adjustedAmount > 0 ? ` after ${money(invoice.adjustedAmount, invoice.currency)} adjustment` : ""} · {invoice.status}</p></div></div></li>)}</ul>}</section></section>
  </main>;
}
