"use client";

import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardSelectField } from "@/components/standard-select-field";
import { OnboardingField } from "@/components/design-system/onboarding-field";
import styles from "@/components/design-system/finance-workspace.module.css";

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
  const [ready, setReady] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const guard = useRef({ pending: false, review: false, ready: false });
  const blocked = !ready || saving || needsReview;
  function requireReview() { guard.current.review = true; setNeedsReview(true); }

  async function refreshRegister() {
    if (!academy || guard.current.pending) return;
    guard.current.pending = true; setSaving(true);
    try { await load(academy.id); guard.current.review = false; setNeedsReview(false); setMessageIsError(false); setMessage("Payment balances refreshed. Check for the previous payment in the payment ledger before recording it again."); }
    catch { requireReview(); setMessageIsError(true); setMessage("Payment balances could not be refreshed. Do not repeat an unconfirmed payment. Please try again later."); }
    finally { guard.current.pending = false; setSaving(false); }
  }

  const load = useCallback(async (id: string | undefined) => {
    if (!id) return;
    const [studentResponse, invoiceResponse, paymentResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/invoices/student-options`, { cache: "no-store" }), academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/payments`, { cache: "no-store" }),
    ]);
    if (![studentResponse, invoiceResponse, paymentResponse].every((response) => response.ok)) throw new Error();
    const [studentData, invoiceData, paymentData] = await Promise.all([studentResponse.json(), invoiceResponse.json(), paymentResponse.json()]);
    if (![studentData, invoiceData, paymentData].every(Array.isArray)) throw new Error();
    setStudents(studentData); setInvoices(invoiceData); setPayments(paymentData); guard.current.ready = true; setReady(true);
  }, []);

  useEffect(() => { async function initialise() { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (response.status === 401) return setMessage("Please sign in before recording payments."); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create your academy first."); setAcademy(academies[0]); await load(academies[0].id); setMessage(""); } catch { setMessageIsError(true); setMessage("Payments could not be loaded. Please try again."); } } void initialise(); }, [load]);

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
    if (!academy || !invoiceId || !selectedInvoice || selectedBalance <= 0 || !guard.current.ready || guard.current.pending || guard.current.review) return;
    const captured = { invoiceId, amount, method, reference };
    guard.current.pending = true; setSaving(true); setMessageIsError(false); setMessage("Recording payment…");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/payments`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ invoiceId: captured.invoiceId, amount: Number(captured.amount), method: captured.method, reference: captured.reference || null }) });
      if (!response.ok) {
        setMessageIsError(true);
        if (response.status >= 500 || response.status === 408) { requireReview(); setMessage("Payment save could not be confirmed. Refresh and check balances and the payment ledger before retrying."); return; }
        const problem = await response.json().catch(() => null);
        setMessage(typeof problem?.message === "string" ? problem.message : "The payment could not be saved. Please check the invoice balance and try again.");
        try { await load(academy.id); } catch { requireReview(); setMessage("Payment rejected, but balances could not be refreshed. Refresh balances before trying again."); }
        if (response.status === 400 && problem?.message === "Payment exceeds the invoice balance.") {
          setInvoiceId(current => current === captured.invoiceId ? "" : current); setAmount(current => current === captured.amount ? "" : current);
        }
        return;
      }
      setInvoiceId(current => current === captured.invoiceId ? "" : current); setAmount(current => current === captured.amount ? "" : current); setReference(current => current === captured.reference ? "" : current);
      try { await load(academy.id); setMessage("Payment recorded successfully. Invoice balances are up to date."); }
      catch { requireReview(); setMessageIsError(true); setMessage("Payment recorded successfully. Balances could not be refreshed; do not repeat this payment. Refresh balances to continue."); }
    } catch {
      requireReview(); setMessageIsError(true);
      setMessage("Payment save could not be confirmed. Refresh and check balances and the payment ledger before retrying.");
    } finally {
      guard.current.pending = false; setSaving(false);
    }
  }

  const studentName = (id: string) => { const student = students.find((item) => item.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };

  return <main className={`enterprise-settings finance-module payments-standard ${styles.page}`}>
    <header className="enterprise-page-header"><p>Finance / payments</p><h1>Payments and balances</h1><span>Record payments against invoices while keeping balances and invoice status up to date.</span></header>
    {message && <p role={messageIsError ? "alert" : "status"} className="enterprise-settings-empty mt-5">{message}</p>}
    {academy && <button type="button" disabled={saving} onClick={refreshRegister} className="enterprise-action-button">Refresh payment balances</button>}<section className={`payments-kpis ${styles.kpis}`}><article><span>Payments recorded</span><strong>{payments.length}</strong></article><article><span>Collected</span><strong>{money(paymentTotals.collected)}</strong></article><article><span>Open balances</span><strong>{paymentTotals.open}</strong></article><article><span>Outstanding</span><strong>{money(paymentTotals.outstanding)}</strong></article></section>
    <section className={`payments-layout ${styles.layout}`}><form aria-busy={saving} onSubmit={recordPayment} className="surface-panel payments-create"><header><p>Record</p><h3>Payment received</h3></header><div className="payments-fields">
      <StandardSelectField name="invoiceId" label="Invoice" value={invoiceId} onChange={selectInvoice} placeholder="Select invoice" required disabled={blocked} options={invoices.filter((invoice) => balance(invoice) > 0).map((invoice) => ({ value: invoice.id, label: `${invoice.invoiceNumber} · ${studentName(invoice.studentId)} · Balance ${money(balance(invoice), invoice.currency)}` }))}/>
      {selectedInvoice && <article className="payments-selected"><span>Selected invoice</span><b>{selectedInvoice.invoiceNumber}</b><small>{studentName(selectedInvoice.studentId)} · Balance {money(selectedBalance, selectedInvoice.currency)}</small></article>}
      <OnboardingField label="Payment amount"><input disabled={blocked} type="number" min="0.01" max={selectedBalance || undefined} step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Payment amount" className="field" required /></OnboardingField>
      <StandardSelectField name="method" label="Payment method" value={method} onChange={(value) => { if (value) setMethod(value); }} placeholder="Select payment method" disabled={blocked} options={["UPI", "Cash", "BankTransfer", "Card", "Cheque", "Offline"].map(value => ({ value, label: value === "BankTransfer" ? "Bank transfer" : value }))}/>
      <OnboardingField label="Reference (optional)"><input disabled={blocked} value={reference} onChange={(event) => setReference(event.target.value)} placeholder="UPI, cheque, or receipt reference" className="field" /></OnboardingField>
    </div><button disabled={blocked || !academy || !invoiceId || !amount || selectedBalance <= 0} className="enterprise-action-button">{saving ? "Recording…" : "Record payment"}</button></form>
    <section className="surface-panel payments-directory"><header><p>Balances</p><h3>Invoice balances</h3></header>{!ready ? <p className="enterprise-settings-empty mt-4">Invoice balances unavailable until loading succeeds.</p> : invoices.length === 0 ? <p className="enterprise-settings-empty mt-4">No invoices yet. Issue an invoice before recording a payment.</p> : <ul>{invoices.map((invoice) => <li key={invoice.id}><div className="flex flex-wrap items-start justify-between gap-3"><div><b>{studentName(invoice.studentId)}</b><p className="mt-1 text-sm text-slate-400">{invoice.invoiceNumber} · Due {invoice.dueDate}</p></div><div className="text-right"><b className="text-cyan-200">Balance {money(balance(invoice), invoice.currency)}</b><p className="mt-1 text-sm text-slate-400">of {money(invoice.totalAmount - invoice.adjustedAmount, invoice.currency)} due{invoice.adjustedAmount > 0 ? ` after ${money(invoice.adjustedAmount, invoice.currency)} adjustment` : ""} · {invoice.status}</p></div></div></li>)}</ul>}</section></section>
  </main>;
}
