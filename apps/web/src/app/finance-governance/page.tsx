"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Adjustment = { id: string; type: string; amount: number; reason: string; status: string };
type Collection = { id: string; invoiceNumber: string; totalAmount: number; currency: string; dueDate: string; daysOverdue: number };
type WorkItem = { id: string; type: string; title: string; description?: string | null; priority: string; status: string; entityId?: string | null; dueAtUtc?: string | null; escalationStage?: string | null; promisedPaymentDate?: string | null };
type DocumentSettings = { invoiceLogoUrl?: string; invoiceAuthorityName?: string; invoiceAuthorityTitle?: string; invoiceSignatureUrl?: string; invoiceTemplateKey: string };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency, maximumFractionDigits: 0 }).format(amount);

export default function FinanceGovernancePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [adjustments, setAdjustments] = useState<Adjustment[]>([]);
  const [collections, setCollections] = useState<Collection[]>([]);
  const [workItems, setWorkItems] = useState<WorkItem[]>([]);
  const [documentSettings, setDocumentSettings] = useState<DocumentSettings>();
  const [notice, setNotice] = useState("Loading finance controls…");
  const [selectedInvoice, setSelectedInvoice] = useState<Collection>();
  const [saving, setSaving] = useState(false);

  async function load(academyId?: string) {
    try {
      const id = academyId ?? academy?.id;
      if (!id) return;
      const [adjustmentResponse, collectionsResponse, workResponse, settingsResponse] = await Promise.all([
        academyApi(`/api/academies/${id}/finance-adjustments`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/finance-governance/collections`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/admin-work-items?type=Collections`, { cache: "no-store" }), academyApi(`/api/academies/${id}/finance-governance/settings`, { cache: "no-store" }),
      ]);
      if (!adjustmentResponse.ok || !collectionsResponse.ok || !workResponse.ok || !settingsResponse.ok) throw new Error();
      setAdjustments(await adjustmentResponse.json()); setCollections(await collectionsResponse.json()); setWorkItems(await workResponse.json()); setDocumentSettings(await settingsResponse.json()); setNotice("");
    } catch { setNotice("Finance controls could not be loaded. Confirm that the API is running and that your account can access this academy."); }
  }

  useEffect(() => { void (async () => { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setNotice("Create an academy before managing finance governance."); setAcademy(academies[0]); await load(academies[0].id); } catch { setNotice("Finance controls could not be loaded. Please sign in and restart the API if needed."); } })(); }, []);

  async function decide(id: string, approve: boolean) {
    if (!academy) return;
    const notes = window.prompt(approve ? "Approval note (optional)" : "Rejection reason") ?? "";
    if (!approve && !notes.trim()) return setNotice("A rejection reason is required.");
    setSaving(true);
    try { const response = await academyApi(`/api/academies/${academy.id}/finance-adjustments/${id}/approval`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ approve, notes }) }); if (!response.ok) throw new Error(); setNotice(`Adjustment ${approve ? "approved" : "rejected"} and recorded in the control register.`); await load(); } catch { setNotice("The decision could not be saved. Refresh the page and try again."); } finally { setSaving(false); }
  }

  async function createFollowUp(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !selectedInvoice) return;
    const data = new FormData(event.currentTarget); setSaving(true);
    try { const response = await academyApi(`/api/academies/${academy.id}/finance-governance/collections/${selectedInvoice.id}/follow-up`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ note: data.get("note"), priority: data.get("priority"), assignedUserId: null, dueAtUtc: data.get("dueAtUtc") || null }) }); if (!response.ok) throw new Error(); setSelectedInvoice(undefined); setNotice("Collections follow-up created in the operational queue."); await load(); } catch { setNotice("The collections follow-up could not be created. Please try again."); } finally { setSaving(false); }
  }

  async function updateCollection(item: WorkItem, event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy) return; const data = new FormData(event.currentTarget); setSaving(true);
    try { const response = await academyApi(`/api/academies/${academy.id}/admin-work-items/${item.id}/collections`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ escalationStage: data.get("stage"), promisedPaymentDate: data.get("promiseDate") || null }) }); if (!response.ok) throw new Error(); setNotice("Collections escalation state updated."); await load(); } catch { setNotice("The escalation state could not be saved. Select a stage and try again."); } finally { setSaving(false); }
  }

  const openCollections = workItems.filter((item) => item.type === "Collections" && item.status !== "Completed" && item.status !== "Cancelled");
  const pendingAdjustments = adjustments.filter((item) => item.status === "PendingApproval");

  return <main className="enterprise-settings finance-module">
    <header className="enterprise-page-header"><p>Finance / governance</p><div className="flex flex-wrap items-end justify-between gap-4"><div><h2>Finance control centre</h2><span>Approve financial adjustments, turn overdue exposure into accountable follow-up, and document collection commitments.</span></div><Link href="/finance" className="text-sm font-medium text-cyan-300">Finance workspace →</Link></div></header>
    {notice && <p className="enterprise-settings-empty mt-5" role="status">{notice}</p>}
    <section className="mt-5 grid gap-4 sm:grid-cols-3"><section className="surface-panel rounded-xl p-4"><p className="text-sm text-slate-400">Pending adjustments</p><b className="mt-1 block text-2xl text-amber-200">{pendingAdjustments.length}</b></section><section className="surface-panel rounded-xl p-4"><p className="text-sm text-slate-400">Overdue invoices</p><b className="mt-1 block text-2xl text-rose-300">{collections.length}</b></section><section className="surface-panel rounded-xl p-4"><p className="text-sm text-slate-400">Open collection tasks</p><b className="mt-1 block text-2xl text-cyan-200">{openCollections.length}</b></section></section>
    <section className="surface-panel mt-5 rounded-xl p-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><h3>Invoice and payslip templates</h3><p className="mt-1 text-sm text-slate-400">Preview document layouts before issuing an invoice or recording a payout. All use the saved academy logo and authorised signature.</p></div><Link href="/finance-policy" className="enterprise-action-button enterprise-action-button-secondary">Edit branding</Link></div><div className="mt-5 grid gap-4 lg:grid-cols-2"><div><p className="mb-2 text-xs font-bold uppercase tracking-wider text-slate-400">Invoice themes</p><div className="grid grid-cols-2 gap-3">{["Classic", "Modern", "Minimal", "Formal"].map((theme) => <DocumentPreview key={theme} kind="Invoice" theme={theme} settings={documentSettings}/>)}</div></div><div><p className="mb-2 text-xs font-bold uppercase tracking-wider text-slate-400">Payslip themes</p><div className="grid grid-cols-3 gap-3">{["Standard", "Compact", "Professional"].map((theme) => <DocumentPreview key={theme} kind="Payslip" theme={theme} settings={documentSettings}/>)}</div></div></div></section>
    {selectedInvoice && <form onSubmit={createFollowUp} className="surface-panel mt-5 rounded-xl p-5"><div className="flex flex-wrap items-end justify-between gap-4"><div><h3 className="font-semibold">Create follow-up for {selectedInvoice.invoiceNumber}</h3><p className="mt-1 text-sm text-slate-400">{money(selectedInvoice.totalAmount, selectedInvoice.currency)} · {selectedInvoice.daysOverdue} days overdue</p></div><button type="button" className="text-sm text-slate-400" onClick={() => setSelectedInvoice(undefined)}>Cancel</button></div><div className="mt-4 grid gap-3 md:grid-cols-3"><label className="text-sm text-slate-300">Priority<select name="priority" className="field mt-2"><option>High</option><option>Critical</option><option>Normal</option></select></label><label className="text-sm text-slate-300">Follow-up due<input type="datetime-local" name="dueAtUtc" className="field mt-2" /></label><label className="text-sm text-slate-300 md:col-span-1">Follow-up note<input required name="note" className="field mt-2" placeholder="Next collection action" defaultValue={`Follow up overdue invoice ${selectedInvoice.invoiceNumber}.`} /></label></div><button disabled={saving} className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950 disabled:opacity-60">{saving ? "Creating…" : "Create collection task"}</button></form>}
    <section className="mt-5 grid gap-5 xl:grid-cols-2">
      <section className="surface-panel rounded-xl p-5"><div className="flex items-center justify-between"><div><h3 className="font-semibold">Overdue collections</h3><p className="mt-1 text-sm text-slate-400">Create a traceable task before escalating a collection.</p></div><Link href="/fee-reminders" className="text-sm text-cyan-300">Reminders →</Link></div>{collections.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No overdue invoices.</p> : <div className="mt-4 space-y-3">{collections.map((item) => <div key={item.id} className="rounded border border-slate-700 p-3"><div className="flex items-start justify-between gap-3"><div><b>{item.invoiceNumber}</b><p className="mt-1 text-sm text-slate-400">Due {item.dueDate} · {item.daysOverdue} days overdue</p></div><span className="font-medium text-amber-200">{money(item.totalAmount, item.currency)}</span></div><button type="button" onClick={() => setSelectedInvoice(item)} className="mt-3 text-sm font-medium text-cyan-300">Create follow-up →</button></div>)}</div>}</section>
      <section className="surface-panel rounded-xl p-5"><h3 className="font-semibold">Adjustment approvals</h3><p className="mt-1 text-sm text-slate-400">Decisions change financial balances and remain visible in the adjustment register.</p>{pendingAdjustments.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No adjustments require approval.</p> : <div className="mt-4 space-y-3">{pendingAdjustments.map((item) => <div key={item.id} className="rounded border border-slate-700 p-3"><div className="flex items-start justify-between gap-3"><div><b>{item.type}</b><p className="mt-1 text-sm text-slate-400">{item.reason}</p></div><span className="font-medium text-amber-200">{money(item.amount)}</span></div><div className="mt-3 flex gap-3"><button disabled={saving} onClick={() => void decide(item.id, true)} className="text-sm font-medium text-emerald-300 disabled:opacity-60">Approve</button><button disabled={saving} onClick={() => void decide(item.id, false)} className="text-sm font-medium text-rose-300 disabled:opacity-60">Reject</button></div></div>)}</div>}</section>
    </section>
    <section className="surface-panel mt-5 rounded-xl p-5"><div><h3 className="font-semibold">Collections escalation register</h3><p className="mt-1 text-sm text-slate-400">Record the current contact stage and a promised payment date for each open collection task.</p></div>{openCollections.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No active collections tasks. Create a follow-up from the overdue collection queue.</p> : <div className="mt-5 grid gap-3">{openCollections.map((item) => <form key={item.id} onSubmit={(event) => void updateCollection(item, event)} className="rounded border border-slate-700 p-4"><div className="flex flex-wrap items-start justify-between gap-3"><div><b>{item.title}</b><p className="mt-1 text-sm text-slate-400">{item.description || "No collection note"}</p></div><span className="text-sm text-slate-400">{item.priority} priority</span></div><div className="mt-3 grid gap-3 md:grid-cols-[1fr_1fr_auto]"><select name="stage" defaultValue={item.escalationStage || "Initial"} className="field"><option>Initial</option><option>Reminder</option><option>ManagerReview</option><option>FinalNotice</option></select><input type="date" name="promiseDate" defaultValue={item.promisedPaymentDate || ""} className="field" /><button disabled={saving} className="rounded border border-cyan-400 px-3 py-2 text-sm font-medium text-cyan-200 disabled:opacity-60">Save control</button></div></form>)}</div>}</section>
  </main>;
}

function DocumentPreview({ kind, theme, settings }: { kind: "Invoice" | "Payslip"; theme: string; settings?: DocumentSettings }) {
  return <article className={`document-template-preview document-template-${theme.toLowerCase()}`}>{settings?.invoiceLogoUrl ? <img src={settings.invoiceLogoUrl} alt="Academy logo"/> : <i>₹</i>}<strong>{kind}</strong><span>{theme}</span><b>{kind === "Invoice" ? "INV-2026-001" : "PS-2026-001"}</b><small>{kind === "Invoice" ? "Student fee · ₹2,500" : "Monthly salary · ₹18,000"}</small><em>{settings?.invoiceAuthorityName || "Authorised signatory"}</em></article>;
}
