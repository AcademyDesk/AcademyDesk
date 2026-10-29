"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import visual from "@/components/design-system/finance-controls.module.css";

type Academy = { id: string };
type Invoice = { id: string; invoiceNumber: string };
type Payment = { id: string; invoiceId: string; amount: number; currency: string; method: string; status: string; reference?: string | null; paidAtUtc: string; reconciliationReference?: string | null; reconciledAtUtc?: string | null };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency }).format(amount);
const localDate = (value: string) => new Date(value).toLocaleDateString("en-CA");

export default function ReconciliationPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [notice, setNotice] = useState("Loading payment ledger…");
  const [filter, setFilter] = useState("Unreconciled");
  const [recordedOn, setRecordedOn] = useState("");
  const [selectedId, setSelectedId] = useState<string>();
  const [reference, setReference] = useState("");
  const [saving, setSaving] = useState(false);

  const [ready, setReady] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const [noticeIsError, setNoticeIsError] = useState(false);
  const guard = useRef({ pending: false, ready: false, review: false });
  const blocked = !ready || saving || needsReview;
  function requireReview() { guard.current.review = true; setNeedsReview(true); }
  async function refreshRegister() {
    if (!academy || guard.current.pending) return;
    guard.current.pending = true; setSaving(true);
    try { await load(academy.id); guard.current.review = false; setNeedsReview(false); setNoticeIsError(false); setNotice("Payment ledger refreshed. Check the previous action before trying again."); }
    catch { requireReview(); setNoticeIsError(true); setNotice("Payment ledger could not be refreshed. Do not repeat an unconfirmed action."); }
    finally { guard.current.pending = false; setSaving(false); }
  }

  const load = useCallback(async (id: string) => {
    const responses = await Promise.all([academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/payments`, { cache: "no-store" })]);
    if (responses.some(response => !response.ok)) throw Error();
    const [invoiceRows, paymentRows] = await Promise.all(responses.map(response => response.json()));
    if (![invoiceRows, paymentRows].every(Array.isArray)) throw Error();
    setInvoices(invoiceRows); setPayments(paymentRows); guard.current.ready = true; setReady(true);
    setSelectedId(current => paymentRows.some((item: Payment) => item.id === current && item.status !== "Reconciled" && item.status !== "Voided") ? current : undefined);
  }, []);

  useEffect(() => { void (async () => {
    try {
      const response = await academyApi("/api/academies", { cache: "no-store" });
      if (!response.ok) throw new Error();
      const academies: Academy[] = await response.json();
      if (!academies[0]) return setNotice("Create an academy before reconciling payments.");
      setAcademy(academies[0]); await load(academies[0].id); setNotice("");
    } catch { setNoticeIsError(true); setNotice("The payment ledger could not be loaded. Please sign in and restart the API if needed."); }
  })(); }, [load]);

  async function reconcile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !guard.current.ready || guard.current.pending || guard.current.review) return;
    const payment = payments.find(item => item.id === selectedId);
    if (!payment || payment.status === "Reconciled" || payment.status === "Voided" || !reference.trim()) {
      setNoticeIsError(true); setNotice("Select an eligible payment and enter a bank or cash evidence reference."); return;
    }
    const captured = { id: payment.id, reference };
    guard.current.pending = true; setSaving(true); setNoticeIsError(false); setNotice("Saving reconciliation evidence…");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/payments/${captured.id}/reconcile`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ reference: captured.reference.trim() }) });
      if (!response.ok) {
        setNoticeIsError(true);
        if (response.status >= 500 || response.status === 408) { requireReview(); setNotice("Reconciliation could not be confirmed. Refresh and check the payment ledger before retrying."); }
        else { const problem = await response.json().catch(() => null); setNotice(typeof problem?.message === "string" ? problem.message : "Reconciliation was rejected. Check the reference, payment status and your access."); }
        return;
      }
      setSelectedId(current => current === captured.id ? undefined : current);
      setReference(current => current === captured.reference ? "" : current);
      setNotice("Payment reconciled. Bank or cash evidence is now recorded in the ledger.");
      try { await load(academy.id); }
      catch { requireReview(); setNoticeIsError(true); setNotice("Payment reconciled. The ledger could not be refreshed; do not repeat this action. Refresh the payment ledger to continue."); }
    } catch { requireReview(); setNoticeIsError(true); setNotice("Reconciliation could not be confirmed. Refresh and check the payment ledger before retrying."); }
    finally { guard.current.pending = false; setSaving(false); }
  }

  const unreconciled = payments.filter(item => item.status !== "Reconciled" && item.status !== "Voided");
  const reconciled = payments.filter(item => item.status === "Reconciled");
  const visible = useMemo(() => payments.filter(item => {
    const statusMatch = filter === "All" || (filter === "Reconciled" ? item.status === "Reconciled" : item.status !== "Reconciled" && item.status !== "Voided");
    return statusMatch && (!recordedOn || localDate(item.paidAtUtc) === recordedOn);
  }), [payments, filter, recordedOn]);
  const selected = payments.find(item => item.id === selectedId);
  const invoiceNumber = (id: string) => invoices.find(invoice => invoice.id === id)?.invoiceNumber ?? "Invoice unavailable";
  const pendingByCurrency = new Map<string, number>();
  for (const payment of unreconciled) pendingByCurrency.set(payment.currency, (pendingByCurrency.get(payment.currency) ?? 0) + payment.amount);
  const outstandingValue = pendingByCurrency.size ? [...pendingByCurrency].map(([currency, amount]) => money(amount, currency)).join(" · ") : money(0);

  return <main className={`enterprise-settings finance-module reconciliation-standard ${visual.page}`}>
    <header className="reconciliation-heading"><span className="reconciliation-title-icon" aria-hidden="true">✓</span><div><p>Billing &amp; collections</p><h1>Payment reconciliation</h1></div><Link href="/finance" className="enterprise-action-button enterprise-action-button-secondary">Finance overview</Link></header>
    {notice && <p className="reconciliation-notice" role={noticeIsError ? "alert" : "status"}>{notice}</p>}
    {academy && <button type="button" disabled={saving} onClick={refreshRegister} className="enterprise-action-button">Refresh payment ledger</button>}<section className="reconciliation-kpis" aria-label="Payment reconciliation summary"><article><span>Awaiting evidence</span><b>{unreconciled.length}</b><small>Recorded payments not yet matched.</small></article><article><span>Value awaiting match</span><b>{outstandingValue}</b><small>Collection value requiring evidence.</small></article><article><span>Reconciled</span><b>{reconciled.length}</b><small>Payments with captured evidence.</small></article><article><span>Control rule</span><b>Required</b><small>Every confirmed match needs a reference.</small></article></section>
    {selected && <form aria-busy={saving} onSubmit={reconcile} className="reconciliation-panel reconciliation-form"><header className="reconciliation-panel-header"><div><p>Evidence capture</p><h2>Reconcile {invoiceNumber(selected.invoiceId)}</h2><small>{money(selected.amount, selected.currency)} via {selected.method} · recorded reference: {selected.reference || "none"}</small></div><button type="button" disabled={blocked} onClick={() => { setSelectedId(undefined); setReference(""); }} className="enterprise-action-button enterprise-action-button-secondary">Cancel</button></header><div className="reconciliation-fields"><label><span>Bank, gateway, UPI, cash-book or cheque reference</span><input disabled={blocked} autoFocus required value={reference} onChange={event => setReference(event.target.value)} className="field" placeholder="e.g. HDFC-20260918-001 or UPI/UTR reference" /></label><button disabled={blocked} className="enterprise-action-button reconciliation-submit">{saving ? "Saving evidence…" : "Confirm reconciliation"}</button></div></form>}
    <section className="reconciliation-panel reconciliation-ledger"><header className="reconciliation-panel-header"><div><p>Control ledger</p><h2>Payment ledger</h2><small>Filter recorded collections, then capture evidence for eligible payments.</small></div><div className="reconciliation-filters"><StandardSelectField name="reconciliationStatus" label="Payment status" value={filter} onChange={setFilter} placeholder="Filter status" options={[{ value: "Unreconciled", label: `Unreconciled (${unreconciled.length})` }, { value: "Reconciled", label: `Reconciled (${reconciled.length})` }, { value: "All", label: "All payments" }]} /><StandardDateField name="reconciliationRecordedOn" value={recordedOn} onChange={setRecordedOn} label="Recorded on (optional)" /></div></header>{!ready ? <p className="reconciliation-empty">Payment ledger unavailable until loading succeeds.</p> : visible.length === 0 ? <p className="reconciliation-empty">No payments match this view.</p> : <div className={`reconciliation-table-wrap ${visual.scroll}`} role="region" aria-label="Payment ledger — scroll horizontally to view all columns" tabIndex={0}><table><thead><tr><th>Invoice / amount</th><th>Method / recorded reference</th><th>Status</th><th>Evidence / action</th></tr></thead><tbody>{visible.map(item => <tr key={item.id}><td><b>{invoiceNumber(item.invoiceId)}</b><small>{money(item.amount, item.currency)} · {new Date(item.paidAtUtc).toLocaleDateString("en-IN")}</small></td><td><span>{item.method}</span><small>{item.reference || "No recorded reference"}</small></td><td><b className={`reconciliation-status reconciliation-status-${item.status.toLowerCase()}`}>{item.status}</b></td><td>{item.status === "Reconciled" ? <span className="reconciliation-evidence">{item.reconciliationReference || "Evidence captured"}<small>{item.reconciledAtUtc ? new Date(item.reconciledAtUtc).toLocaleString("en-IN") : ""}</small></span> : item.status === "Voided" ? <span className="reconciliation-muted">Voided</span> : <button type="button" disabled={blocked} onClick={() => { setSelectedId(item.id); setReference(""); }} className="reconciliation-link">Reconcile <span aria-hidden="true">→</span></button>}</td></tr>)}</tbody></table></div>}</section>
  </main>;
}
