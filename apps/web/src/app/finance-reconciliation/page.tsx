"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders, subscribePortalSessionChanges } from "@/lib/api";

type Academy = { id: string };
type Invoice = { id: string; invoiceNumber: string };
type Payment = { id: string; invoiceId: string; amount: number; currency: string; method: string; status: string; reference?: string | null; paidAtUtc: string; reconciliationReference?: string | null; reconciledAtUtc?: string | null };
const money = (amount: number, currency: string) => new Intl.NumberFormat("en-IN", { style: "currency", currency, minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
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

  const [reviewRequired, setReviewRequired] = useState(false);
  const pendingWrite = useRef(false);
  const needsReview = useRef(false);
  const mounted = useRef(false);
  const sessionRetired = useRef(false);
  const isCurrent = () => mounted.current && !sessionRetired.current;
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  useEffect(() => subscribePortalSessionChanges("AcademyAdmin", () => {
    sessionRetired.current = true; needsReview.current = true;
    if (!mounted.current) return;
    setAcademy(undefined); setInvoices([]); setPayments([]); setSelectedId(undefined); setReference("");
    setSaving(false); setReviewRequired(true);
    setNotice("Your session changed. Reload and check the payment ledger before continuing.");
  }), []);

  async function load(academyId?: string) {
      const id = academyId ?? academy?.id;
      if (!id) return;
      const [invoiceResponse, paymentResponse] = await Promise.all([academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/payments`, { cache: "no-store" })]);
      if (!invoiceResponse.ok || !paymentResponse.ok) throw new Error();
      const [invoiceData, paymentData] = await Promise.all([invoiceResponse.json(), paymentResponse.json()]);
      if (!isCurrent()) return;
      setInvoices(invoiceData); setPayments(paymentData);
  }

  useEffect(() => { void (async () => {
    try {
      const response = await academyApi("/api/academies", { cache: "no-store" });
      if (!response.ok) throw new Error();
      const academies: Academy[] = await response.json();
      if (!isCurrent()) return;
      if (!academies[0]) return setNotice("Create an academy before reconciling payments.");
      setAcademy(academies[0]); await load(academies[0].id); if (isCurrent()) setNotice("");
    } catch { if (isCurrent()) setNotice("The payment ledger could not be loaded. Please sign in and restart the API if needed."); }
  })(); }, []);

  function requireReview() { needsReview.current = true; setReviewRequired(true); }
  async function reconcile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pendingWrite.current || needsReview.current || !isCurrent() || !academy || !selectedId || !reference.trim()) return;
    pendingWrite.current = true; setSaving(true); setNotice("Saving reconciliation evidence…");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/payments/${selectedId}/reconcile`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ reference: reference.trim() }) });
      if (!isCurrent()) return;
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408 || response.status === 0) {
          requireReview(); setNotice("The reconciliation outcome is unconfirmed. Reload and check the payment ledger before submitting evidence again.");
        } else setNotice("Reconciliation could not be saved. Check the payment and required bank or cash evidence reference.");
        return;
      }
      setSelectedId(undefined); setReference("");
      try { await load(); }
      catch {
        if (isCurrent()) { requireReview(); setNotice("Payment reconciled. The ledger could not be refreshed. Reload and check the saved evidence before submitting again."); }
        return;
      }
      if (isCurrent()) setNotice("Payment reconciled. Bank or cash evidence is now recorded in the ledger.");
    } catch {
      if (isCurrent()) { requireReview(); setNotice("Could not confirm whether reconciliation completed. Reload and check the payment ledger before submitting evidence again."); }
    } finally { pendingWrite.current = false; if (isCurrent()) setSaving(false); }
  }

  const unreconciled = payments.filter(item => item.status !== "Reconciled" && item.status !== "Voided");
  const reconciled = payments.filter(item => item.status === "Reconciled");
  const visible = useMemo(() => payments.filter(item => {
    const statusMatch = filter === "All" || (filter === "Reconciled" ? item.status === "Reconciled" : item.status !== "Reconciled" && item.status !== "Voided");
    return statusMatch && (!recordedOn || localDate(item.paidAtUtc) === recordedOn);
  }), [payments, filter, recordedOn]);
  const selected = payments.find(item => item.id === selectedId);
  const invoiceNumber = (id: string) => invoices.find(invoice => invoice.id === id)?.invoiceNumber ?? "Invoice unavailable";
  const outstandingByCurrency = Array.from(unreconciled.reduce((totals, payment) => totals.set(payment.currency, (totals.get(payment.currency) ?? 0) + payment.amount), new Map<string, number>())).sort(([a], [b]) => a.localeCompare(b));
  const writeDisabled = saving || reviewRequired;

  return <main className="enterprise-settings finance-module reconciliation-standard">
    <header className="reconciliation-heading"><span className="reconciliation-title-icon" aria-hidden="true">✓</span><div><p>Billing &amp; collections</p><h1>Payment reconciliation</h1></div><Link href="/finance" className="enterprise-action-button enterprise-action-button-secondary">Finance overview</Link></header>
    {notice && <p className="reconciliation-notice" role="status">{notice}</p>}
    <section className="reconciliation-kpis" aria-label="Payment reconciliation summary"><article><span>Awaiting evidence</span><b>{unreconciled.length}</b><small>Recorded payments not yet matched.</small></article><article><span>Value awaiting match</span><b>{outstandingByCurrency.length ? outstandingByCurrency.map(([currency, amount]) => <span key={currency} style={{ display: "block", fontSize: "inherit", color: "inherit", letterSpacing: "inherit", textTransform: "none", fontWeight: "inherit" }}>{currency} {money(amount, currency)}</span>) : "No payments awaiting match"}</b><small>Collection value requiring evidence.</small></article><article><span>Reconciled</span><b>{reconciled.length}</b><small>Payments with captured evidence.</small></article><article><span>Control rule</span><b>Required</b><small>Every confirmed match needs a reference.</small></article></section>
    {selected && <form onSubmit={reconcile} className="reconciliation-panel reconciliation-form"><header className="reconciliation-panel-header"><div><p>Evidence capture</p><h2>Reconcile {invoiceNumber(selected.invoiceId)}</h2><small>{money(selected.amount, selected.currency)} via {selected.method} · recorded reference: {selected.reference || "none"}</small></div><button type="button" disabled={writeDisabled} onClick={() => { if (pendingWrite.current || needsReview.current) return; setSelectedId(undefined); setReference(""); }} className="enterprise-action-button enterprise-action-button-secondary">Cancel</button></header><fieldset className="reconciliation-fields" disabled={writeDisabled} style={{ border: 0, margin: 0, minWidth: 0 }}><label><span>Bank, gateway, UPI, cash-book or cheque reference</span><input autoFocus required value={reference} onChange={event => setReference(event.target.value)} className="field" placeholder="e.g. HDFC-20260918-001 or UPI/UTR reference" /></label><button disabled={writeDisabled} className="enterprise-action-button reconciliation-submit">{saving ? "Saving evidence…" : "Confirm reconciliation"}</button></fieldset></form>}
    <section className="reconciliation-panel reconciliation-ledger"><header className="reconciliation-panel-header"><div><p>Control ledger</p><h2>Payment ledger</h2><small>Filter recorded collections, then capture evidence for eligible payments.</small></div><div className="reconciliation-filters"><StandardSelectField name="reconciliationStatus" value={filter} onChange={setFilter} placeholder="Filter status" options={[{ value: "Unreconciled", label: `Unreconciled (${unreconciled.length})` }, { value: "Reconciled", label: `Reconciled (${reconciled.length})` }, { value: "All", label: "All payments" }]} /><StandardDateField name="reconciliationRecordedOn" value={recordedOn} onChange={setRecordedOn} label="Recorded on" /></div></header>{visible.length === 0 ? <p className="reconciliation-empty">No payments match this view.</p> : <div className="reconciliation-table-wrap"><table><thead><tr><th>Invoice / amount</th><th>Method / recorded reference</th><th>Status</th><th>Evidence / action</th></tr></thead><tbody>{visible.map(item => <tr key={item.id}><td><b>{invoiceNumber(item.invoiceId)}</b><small>{money(item.amount, item.currency)} · {new Date(item.paidAtUtc).toLocaleDateString("en-IN")}</small></td><td><span>{item.method}</span><small>{item.reference || "No recorded reference"}</small></td><td><b className={`reconciliation-status reconciliation-status-${item.status.toLowerCase()}`}>{item.status}</b></td><td>{item.status === "Reconciled" ? <span className="reconciliation-evidence">{item.reconciliationReference || "Evidence captured"}<small>{item.reconciledAtUtc ? new Date(item.reconciledAtUtc).toLocaleString("en-IN") : ""}</small></span> : item.status === "Voided" ? <span className="reconciliation-muted">Voided</span> : <button type="button" disabled={writeDisabled} onClick={() => { if (pendingWrite.current || needsReview.current) return; setSelectedId(item.id); setReference(""); }} className="reconciliation-link">Reconcile <span aria-hidden="true">→</span></button>}</td></tr>)}</tbody></table></div>}</section>
  </main>;
}
