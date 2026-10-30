"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { academyApi, apiHeaders, subscribePortalSessionChanges } from "@/lib/api";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Invoice = { id: string; invoiceNumber: string; studentId: string; totalAmount: number; balance: number; currency: string; dueDate: string; status: string };
type Payment = { id: string; invoiceId: string; amount: number; currency: string; method: string; status: string; reference?: string | null; paidAtUtc: string };
const money = (amount: number, currency: string) => {
  const code = typeof currency === "string" ? currency.trim().toUpperCase() : "";
  return /^[A-Z]{3}$/.test(code) ? new Intl.NumberFormat("en-IN", { style: "currency", currency: code, minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount)
    : `${new Intl.NumberFormat("en-IN").format(amount)} (currency unavailable)`;
};
function currencyTotals(rows: { amount: number; currency: string }[]) {
  if (!rows.length) return "No amounts";
  if (rows.some(row => !Number.isFinite(row.amount))) return "Amounts unavailable";
  const totals = new Map<string, number>();
  for (const row of rows) {
    const code = typeof row.currency === "string" ? row.currency.trim().toUpperCase() : "";
    totals.set(code, (totals.get(code) ?? 0) + row.amount);
  }
  return [...totals].sort(([a], [b]) => a.localeCompare(b)).map(([currency, amount]) => money(amount, currency)).join(" · ");
}
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

  const [writeState, setWriteState] = useState<"idle" | "saving" | "uncertain" | "retired">("idle");
  const [loaded, setLoaded] = useState(false);
  const pending = useRef(false);
  const ready = useRef(false);
  const mounted = useRef(true);
  const retired = useRef(false);
  const revision = useRef(0);
  const controller = useRef<AbortController | null>(null);
  const current = (epoch: number) => mounted.current && !retired.current && epoch === revision.current;

  async function load(id: string, epoch: number) {
    const responses = await Promise.all([
      academyApi(`/api/academies/${id}/invoices/student-options`, { cache: "no-store", signal: controller.current?.signal }),
      academyApi(`/api/academies/${id}/invoices`, { cache: "no-store", signal: controller.current?.signal }),
      academyApi(`/api/academies/${id}/payments`, { cache: "no-store", signal: controller.current?.signal }),
    ]);
    if (!current(epoch)) return false;
    if (!responses.every(response => response.ok)) throw new Error("Payment register unavailable");
    const [nextStudents, nextInvoices, nextPayments] = await Promise.all(responses.map(response => response.json()));
    if (!current(epoch)) return false;
    setStudents(nextStudents); setInvoices(nextInvoices); setPayments(nextPayments);
    ready.current = true; setLoaded(true);
    return true;
  }

  useEffect(() => {
    mounted.current = true; retired.current = false; controller.current = new AbortController();
    const epoch = ++revision.current;
    const stop = subscribePortalSessionChanges("AcademyAdmin", () => {
      retired.current = true; revision.current++; controller.current?.abort(); ready.current = false;
      setAcademy(undefined); setStudents([]); setInvoices([]); setPayments([]);
      setInvoiceId(""); setAmount(""); setReference(""); setMethod("UPI"); setLoaded(false);
      setWriteState("retired"); setMessage("Your session changed. Reload payments after signing in.");
    });
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store", signal: controller.current?.signal });
        if (!current(epoch)) return;
        if (response.status === 401) return setMessage("Please sign in before recording payments.");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!current(epoch)) return;
        if (!academies[0]) return setMessage("Create your academy first.");
        setAcademy(academies[0]);
        if (await load(academies[0].id, epoch)) setMessage("");
      } catch { if (current(epoch)) setMessage("Payments could not be loaded. Reload to try again."); }
    }
    void initialise();
    return () => { mounted.current = false; revision.current++; controller.current?.abort(); stop(); };
    // This view owns one authenticated session; replacement retires it until reload.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selectedInvoice = invoices.find((invoice) => invoice.id === invoiceId);
  const selectedBalance = selectedInvoice ? balance(selectedInvoice) : 0;
  const paymentTotals = useMemo(() => ({
    collected: currencyTotals(payments.filter(payment => payment.status === "Completed" || payment.status === "Reconciled")),
    open: invoices.filter(invoice => balance(invoice) > 0).length,
    outstanding: currencyTotals(invoices.map(invoice => ({ amount: Math.max(0, balance(invoice)), currency: invoice.currency }))),
  }), [invoices, payments]);

  function selectInvoice(id: string) {
    if (pending.current || retired.current) return;
    setInvoiceId(id);
    const invoice = invoices.find((item) => item.id === id);
    if (invoice) setAmount(balance(invoice).toString());
  }

  async function recordPayment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !invoiceId || !ready.current || pending.current || retired.current) return;
    const numericAmount = Number(amount);
    if (!Number.isFinite(numericAmount) || numericAmount <= 0 || numericAmount > selectedBalance) {
      setMessage("Enter a positive payment within the remaining balance."); return;
    }
    pending.current = true; setWriteState("saving"); setMessage("Saving payment…");
    const epoch = revision.current;
    let confirmed = false, uncertain = false;
    try {
      const response = await academyApi(`/api/academies/${academy.id}/payments`, {
        method: "POST", headers: apiHeaders(true), signal: controller.current?.signal,
        body: JSON.stringify({ invoiceId, amount: numericAmount, method, reference: reference || null }),
      });
      if (!current(epoch)) return;
      if (!response.ok) {
        if (![400, 401, 403, 404, 409, 422].includes(response.status)) throw new Error("Uncertain write");
        setMessage("The payment was rejected. Check the amount, remaining balance and your access before trying again.");
        return;
      }
      confirmed = true; ready.current = false; setLoaded(false);
      if (!await load(academy.id, epoch)) return;
      setInvoiceId(""); setAmount(""); setReference(""); setMessage("Payment saved and balances refreshed.");
    } catch {
      if (!current(epoch)) return;
      uncertain = true; ready.current = false; setLoaded(false);
      setMessage(confirmed ? "Payment saved, but balances could not be refreshed. Reload and check the payment register before recording another payment."
        : "The payment result is uncertain. Reload and check the payment register before recording another payment.");
    } finally {
      if (current(epoch)) { pending.current = uncertain; setWriteState(uncertain ? "uncertain" : "idle"); }
    }
  }

  const studentName = (id: string) => { const student = students.find((item) => item.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };

  return <main className="enterprise-settings finance-module payments-standard">
    <header className="enterprise-page-header"><p>Finance / payments</p><h2>Payments and balances</h2><span>Record payments against invoices while keeping balances and invoice status up to date.</span></header>
    {message && <p role="status" className="enterprise-settings-empty mt-5">{message}</p>}
    <section className="payments-kpis"><article><span>Payments recorded</span><strong>{payments.length}</strong></article><article><span>Collected</span><strong>{paymentTotals.collected}</strong></article><article><span>Open balances</span><strong>{paymentTotals.open}</strong></article><article><span>Outstanding</span><strong>{paymentTotals.outstanding}</strong></article></section>
    <section className="payments-layout"><form onSubmit={recordPayment} className="surface-panel payments-create"><header><p>Record</p><h3>Payment received</h3></header><div className="payments-fields">
      <select aria-label="Invoice" disabled={writeState !== "idle" || !loaded} value={invoiceId} onChange={(event) => selectInvoice(event.target.value)} className="field" required><option value="">Select invoice</option>{invoices.filter((invoice) => balance(invoice) > 0).map((invoice) => <option key={invoice.id} value={invoice.id}>{invoice.invoiceNumber} · {studentName(invoice.studentId)} · Balance {money(balance(invoice), invoice.currency)}</option>)}</select>
      {selectedInvoice && <article className="payments-selected"><span>Selected invoice</span><b>{selectedInvoice.invoiceNumber}</b><small>{studentName(selectedInvoice.studentId)} · Balance {money(selectedBalance, selectedInvoice.currency)}</small></article>}
      <input aria-label="Payment amount" disabled={writeState !== "idle" || !loaded} type="number" min="0.01" max={selectedBalance || undefined} step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Payment amount" className="field" required />
      <select aria-label="Payment method" disabled={writeState !== "idle" || !loaded} value={method} onChange={(event) => setMethod(event.target.value)} className="field"><option>UPI</option><option>Cash</option><option>BankTransfer</option><option>Card</option><option>Cheque</option><option>Offline</option></select>
      <input aria-label="Payment reference" disabled={writeState !== "idle" || !loaded} value={reference} onChange={(event) => setReference(event.target.value)} placeholder="UPI, cheque, or receipt reference (optional)" className="field" />
    </div><button disabled={writeState !== "idle" || !loaded || !academy || !invoiceId || !amount} className="enterprise-action-button">{writeState === "saving" ? "Saving payment…" : "Record payment"}</button></form>
    <section className="surface-panel payments-directory"><header><p>Balances</p><h3>Invoice balances</h3></header>{invoices.length === 0 ? <p className="enterprise-settings-empty mt-4">No invoices yet. Issue an invoice before recording a payment.</p> : <ul>{invoices.map((invoice) => <li key={invoice.id}><div className="flex flex-wrap items-start justify-between gap-3"><div><b>{studentName(invoice.studentId)}</b><p className="mt-1 text-sm text-slate-400">{invoice.invoiceNumber} · Due {invoice.dueDate}</p></div><div className="text-right"><b className="text-cyan-200">Balance {money(balance(invoice), invoice.currency)}</b><p className="mt-1 text-sm text-slate-400">of {money(invoice.totalAmount, invoice.currency)} · {invoice.status}</p></div></div></li>)}</ul>}</section></section>
  </main>;
}
