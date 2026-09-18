"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Invoice = { id: string; invoiceNumber: string };
type Payment = { id: string; invoiceId: string; amount: number; currency: string; method: string; status: string; reference?: string | null; paidAtUtc: string; reconciliationReference?: string | null; reconciledAtUtc?: string | null };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency, maximumFractionDigits: 0 }).format(amount);

export default function ReconciliationPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [notice, setNotice] = useState("Loading payment ledger…");
  const [filter, setFilter] = useState("Unreconciled");
  const [selectedId, setSelectedId] = useState<string>();
  const [reference, setReference] = useState("");
  const [saving, setSaving] = useState(false);

  async function load(academyId?: string) {
    try {
      const id = academyId ?? academy?.id;
      if (!id) return;
      const [invoiceResponse, paymentResponse] = await Promise.all([academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/payments`, { cache: "no-store" })]);
      if (!invoiceResponse.ok || !paymentResponse.ok) throw new Error();
      setInvoices(await invoiceResponse.json());
      setPayments(await paymentResponse.json());
      setNotice("");
    } catch { setNotice("The payment ledger could not be loaded. Confirm that the API is running and that you have finance access."); }
  }

  useEffect(() => { void (async () => { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setNotice("Create an academy before reconciling payments."); setAcademy(academies[0]); await load(academies[0].id); } catch { setNotice("The payment ledger could not be loaded. Please sign in and restart the API if needed."); } })(); }, []);

  async function reconcile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !selectedId || !reference.trim()) return;
    setSaving(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/payments/${selectedId}/reconcile`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ reference: reference.trim() }) });
      if (!response.ok) throw new Error();
      setSelectedId(undefined); setReference(""); setNotice("Payment reconciled. Bank or cash evidence is now recorded in the ledger."); await load();
    } catch { setNotice("Reconciliation could not be saved. A bank or cash evidence reference is required."); } finally { setSaving(false); }
  }

  const visible = useMemo(() => payments.filter((item) => filter === "All" || (filter === "Reconciled" ? item.status === "Reconciled" : item.status !== "Reconciled" && item.status !== "Voided")), [payments, filter]);
  const selected = payments.find((item) => item.id === selectedId);
  const invoiceNumber = (id: string) => invoices.find((invoice) => invoice.id === id)?.invoiceNumber ?? "Invoice unavailable";
  const unreconciled = payments.filter((item) => item.status !== "Reconciled" && item.status !== "Voided").length;

  return <main className="enterprise-settings">
    <header className="enterprise-page-header"><p>Finance / reconciliation</p><div className="flex flex-wrap items-end justify-between gap-4"><div><h2>Payment reconciliation</h2><span>Match recorded collections with bank, payment gateway, UPI, cash or cheque evidence before financial close.</span></div><Link href="/finance" className="text-sm font-medium text-cyan-300">Finance workspace →</Link></div></header>
    {notice && <p className="mt-5 rounded border border-amber-700/50 bg-amber-950/30 p-3 text-sm text-amber-100" role="status">{notice}</p>}
    <section className="mt-5 grid gap-4 sm:grid-cols-3"><section className="surface-panel rounded-xl p-4"><p className="text-sm text-slate-400">Unreconciled payments</p><b className="mt-1 block text-2xl text-amber-200">{unreconciled}</b></section><section className="surface-panel rounded-xl p-4"><p className="text-sm text-slate-400">Reconciled payments</p><b className="mt-1 block text-2xl text-emerald-300">{payments.filter((item) => item.status === "Reconciled").length}</b></section><section className="surface-panel rounded-xl p-4"><p className="text-sm text-slate-400">Control rule</p><b className="mt-1 block text-sm">Evidence reference required</b></section></section>
    {selected && <form onSubmit={reconcile} className="surface-panel mt-5 rounded-xl p-5"><div className="flex flex-wrap items-end justify-between gap-4"><div><h3 className="font-semibold">Reconcile {invoiceNumber(selected.invoiceId)}</h3><p className="mt-1 text-sm text-slate-400">{money(selected.amount, selected.currency)} via {selected.method} · recorded reference: {selected.reference || "none"}</p></div><button type="button" onClick={() => { setSelectedId(undefined); setReference(""); }} className="text-sm text-slate-400">Cancel</button></div><label className="mt-4 block text-sm text-slate-300">Bank, gateway, UPI, cash-book or cheque reference<input autoFocus required value={reference} onChange={(event) => setReference(event.target.value)} className="field mt-2" placeholder="e.g. HDFC-20260918-001 or UPI/UTR reference" /></label><button disabled={saving} className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950 disabled:opacity-60">{saving ? "Saving evidence…" : "Confirm reconciliation"}</button></form>}
    <section className="surface-panel mt-5 rounded-xl p-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><h3 className="font-semibold">Payment ledger</h3><p className="mt-1 text-sm text-slate-400">Filter the ledger, then record evidence only for eligible payments.</p></div><select value={filter} onChange={(event) => setFilter(event.target.value)} className="field w-auto"><option value="Unreconciled">Unreconciled ({unreconciled})</option><option value="Reconciled">Reconciled</option><option value="All">All payments</option></select></div>{visible.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No payments match this view.</p> : <div className="mt-5 overflow-x-auto"><table className="min-w-full text-left text-sm"><thead className="text-slate-400"><tr><th className="pb-3 pr-4">Invoice / amount</th><th className="pb-3 pr-4">Method / recorded ref.</th><th className="pb-3 pr-4">Status</th><th className="pb-3">Evidence / action</th></tr></thead><tbody>{visible.map((item) => <tr key={item.id} className="border-t border-slate-800 align-top"><td className="py-3 pr-4"><b className="block">{invoiceNumber(item.invoiceId)}</b><span className="text-slate-400">{money(item.amount, item.currency)} · {new Date(item.paidAtUtc).toLocaleDateString("en-IN")}</span></td><td className="py-3 pr-4"><span>{item.method}</span><span className="mt-1 block text-slate-400">{item.reference || "No recorded reference"}</span></td><td className="py-3 pr-4"><b className={item.status === "Reconciled" ? "text-emerald-300" : item.status === "Voided" ? "text-rose-300" : "text-amber-200"}>{item.status}</b></td><td className="py-3">{item.status === "Reconciled" ? <span className="text-emerald-300">{item.reconciliationReference || "Evidence captured"}<span className="mt-1 block text-xs text-slate-500">{item.reconciledAtUtc ? new Date(item.reconciledAtUtc).toLocaleString("en-IN") : ""}</span></span> : item.status === "Voided" ? <span className="text-slate-500">Voided</span> : <button type="button" onClick={() => { setSelectedId(item.id); setReference(""); }} className="text-cyan-300">Reconcile →</button>}</td></tr>)}</tbody></table></div>}</section>
  </main>;
}
