"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Adjustment = { id: string; type: string; amount: number; reason: string; status: string };
type Collection = { id: string; invoiceNumber: string; totalAmount: number; balance: number; currency: string; dueDate: string; daysOverdue: number };
type WorkItem = { id: string; type: string; title: string; description?: string | null; priority: string; status: string; entityId?: string | null; dueAtUtc?: string | null; escalationStage?: string | null; promisedPaymentDate?: string | null };
type DocumentSettings = { taxRegistrationNumber: string; taxLabel: string; taxRatePercent: number; defaultPaymentTermsDays: number; taxInclusivePricing: boolean; invoiceLogoUrl?: string; invoiceAuthorityName?: string; invoiceAuthorityTitle?: string; invoiceSignatureUrl?: string; invoiceTemplateKey: string; payslipTemplateKey: string };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency }).format(amount);

export default function FinanceGovernancePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [adjustments, setAdjustments] = useState<Adjustment[]>([]);
  const [collections, setCollections] = useState<Collection[]>([]);
  const [workItems, setWorkItems] = useState<WorkItem[]>([]);
  const [documentSettings, setDocumentSettings] = useState<DocumentSettings>();
  const [notice, setNotice] = useState("Loading finance controls…");
  const [noticeIsError, setNoticeIsError] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState<Collection>();
  const [saving, setSaving] = useState(false);
  const [decision, setDecision] = useState<{ id: string; approve: boolean; notes: string }>();
  const [documentKind, setDocumentKind] = useState<"Invoice" | "Payslip">("Invoice");
  const [editingDocuments, setEditingDocuments] = useState(false);

  async function load(academyId?: string, successNotice = "") {
    try {
      const id = academyId ?? academy?.id;
      if (!id) return;
      const [adjustmentResponse, collectionsResponse, workResponse, settingsResponse] = await Promise.all([
        academyApi(`/api/academies/${id}/finance-adjustments`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/finance-governance/collections`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/finance-governance/collection-tasks`, { cache: "no-store" }), academyApi(`/api/academies/${id}/finance-governance/settings`, { cache: "no-store" }),
      ]);
      if (!adjustmentResponse.ok || !collectionsResponse.ok || !workResponse.ok || !settingsResponse.ok) throw new Error();
      setAdjustments(await adjustmentResponse.json()); setCollections(await collectionsResponse.json()); setWorkItems(await workResponse.json()); setDocumentSettings(await settingsResponse.json()); setNoticeIsError(false); setNotice(successNotice);
    } catch { setNoticeIsError(true); setNotice(successNotice ? `${successNotice} The latest list could not be refreshed. Please refresh this page.` : "Finance controls could not be loaded. Confirm that the API is running and that your account can access this academy."); }
  }

  useEffect(() => { void (async () => { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setNotice("Create an academy before managing finance governance."); setAcademy(academies[0]); await load(academies[0].id); } catch { setNoticeIsError(true); setNotice("Finance controls could not be loaded. Please sign in and restart the API if needed."); } })(); }, []);

  async function decide(id: string, approve: boolean, notes: string) {
    if (!academy || saving) return;
    if (!approve && !notes.trim()) { setNoticeIsError(true); return setNotice("A rejection reason is required."); }
    setSaving(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/finance-adjustments/${id}/approval`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ approve, notes }) });
      if (!response.ok) {
        const problem = await response.json().catch(() => null);
        const reason = typeof problem?.message === "string" && problem.message.trim() ? problem.message : "The decision could not be saved.";
        await load(undefined, `${reason} Review the invoice before trying again.`);
        setNoticeIsError(true);
        return;
      }
      setDecision(undefined);
      await load(undefined, `Adjustment ${approve ? "approved" : "rejected"} and recorded in the control register.`);
    } catch { setNoticeIsError(true); setNotice("The decision could not be confirmed. Refresh the approvals before trying again to avoid a duplicate decision."); } finally { setSaving(false); }
  }

  async function createFollowUp(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !selectedInvoice) return;
    const data = new FormData(event.currentTarget); setSaving(true);
    try { const response = await academyApi(`/api/academies/${academy.id}/finance-governance/collections/${selectedInvoice.id}/follow-up`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ note: data.get("note"), priority: data.get("priority"), assignedUserId: null, dueAtUtc: data.get("dueAtUtc") || null }) }); if (!response.ok) throw new Error(); setSelectedInvoice(undefined); await load(undefined, "Collections follow-up created in the operational queue."); } catch { setNotice("The collections follow-up could not be created. Please try again."); } finally { setSaving(false); }
  }

  async function updateCollection(item: WorkItem, event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy) return; const data = new FormData(event.currentTarget); setSaving(true);
    try { const response = await academyApi(`/api/academies/${academy.id}/finance-governance/collection-tasks/${item.id}`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ escalationStage: data.get("stage"), promisedPaymentDate: data.get("promiseDate") || null }) }); if (!response.ok) throw new Error(); await load(undefined, "Collections escalation state updated."); } catch { setNotice("The escalation state could not be saved. Select a stage and try again."); } finally { setSaving(false); }
  }
  async function saveDocumentSettings(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!academy || !documentSettings) return; const data = new FormData(event.currentTarget); const response = await academyApi(`/api/academies/${academy.id}/finance-governance/settings`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ ...documentSettings, invoiceLogoUrl: data.get("invoiceLogoUrl"), invoiceAuthorityName: data.get("invoiceAuthorityName"), invoiceAuthorityTitle: data.get("invoiceAuthorityTitle"), invoiceSignatureUrl: data.get("invoiceSignatureUrl"), invoiceTemplateKey: data.get("invoiceTemplateKey"), payslipTemplateKey: data.get("payslipTemplateKey") }) }); if (!response.ok) return setNotice("Document settings could not be saved."); setDocumentSettings(await response.json()); setEditingDocuments(false); setNotice("Invoice and payslip settings saved."); }

  const openCollections = workItems.filter((item) => item.type === "Collections" && item.status !== "Completed" && item.status !== "Cancelled");
  const pendingAdjustments = adjustments.filter((item) => item.status === "PendingApproval");

  return <main className="enterprise-settings finance-module finance-control-standard">
    <header className="enterprise-page-header"><p>Finance / governance</p><div className="flex flex-wrap items-end justify-between gap-4"><div><h2>Finance control centre</h2><span>Approve financial adjustments, turn overdue exposure into accountable follow-up, and document collection commitments.</span></div><Link href="/finance" className="text-sm font-medium text-cyan-300">Finance workspace →</Link></div></header>
    {notice && <p className="enterprise-settings-empty mt-5" role={noticeIsError ? "alert" : "status"}>{notice}</p>}
    <section className="finance-control-kpis"><section className="surface-panel"><p>Pending adjustments</p><b>{pendingAdjustments.length}</b></section><section className="surface-panel"><p>Overdue invoices</p><b>{collections.length}</b></section><section className="surface-panel"><p>Open collection tasks</p><b>{openCollections.length}</b></section></section>
    <section className="surface-panel finance-control-documents"><div className="flex flex-wrap items-center justify-between gap-3"><div><p className="finance-control-eyebrow">Documents</p><h3>Invoice & payslip templates</h3></div><button type="button" onClick={() => setEditingDocuments((value) => !value)} className="enterprise-action-button enterprise-action-button-secondary">{editingDocuments ? "Close edit" : "Edit document setup"}</button></div>{documentSettings && <><div className="mt-5 grid gap-3 md:grid-cols-2"><label className="field-label">Document<select value={documentKind} onChange={(event) => setDocumentKind(event.target.value as "Invoice" | "Payslip")} className="field"><option>Invoice</option><option>Payslip</option></select></label><label className="field-label">Theme<select value={documentKind === "Invoice" ? documentSettings.invoiceTemplateKey : documentSettings.payslipTemplateKey} onChange={(event) => setDocumentSettings({ ...documentSettings, [documentKind === "Invoice" ? "invoiceTemplateKey" : "payslipTemplateKey"]: event.target.value })} className="field">{documentKind === "Invoice" ? <><option>Classic</option><option>Modern</option><option>Minimal</option><option>Formal</option></> : <><option>Standard</option><option>Compact</option><option>Professional</option></>}</select></label></div><DocumentPreview kind={documentKind} theme={documentKind === "Invoice" ? documentSettings.invoiceTemplateKey : documentSettings.payslipTemplateKey} settings={documentSettings}/>{editingDocuments && <form onSubmit={saveDocumentSettings} className="mt-5 border-t border-slate-700 pt-5"><h3>Document branding</h3><div className="mt-4 grid gap-3 md:grid-cols-2"><label className="field-label md:col-span-2">Academy logo URL<input name="invoiceLogoUrl" type="url" defaultValue={documentSettings.invoiceLogoUrl ?? ""} className="field"/></label><label className="field-label">Authorised signatory<input name="invoiceAuthorityName" defaultValue={documentSettings.invoiceAuthorityName ?? ""} className="field"/></label><label className="field-label">Designation<input name="invoiceAuthorityTitle" defaultValue={documentSettings.invoiceAuthorityTitle ?? ""} className="field"/></label><label className="field-label md:col-span-2">Signature image URL<input name="invoiceSignatureUrl" type="url" defaultValue={documentSettings.invoiceSignatureUrl ?? ""} className="field"/></label><input name="invoiceTemplateKey" type="hidden" value={documentSettings.invoiceTemplateKey}/><input name="payslipTemplateKey" type="hidden" value={documentSettings.payslipTemplateKey}/></div><button className="enterprise-action-button mt-5">Save document setup</button></form>}</>}</section>
    {selectedInvoice && <form onSubmit={createFollowUp} className="surface-panel mt-5 rounded-xl p-5"><div className="flex flex-wrap items-end justify-between gap-4"><div><h3 className="font-semibold">Create follow-up for {selectedInvoice.invoiceNumber}</h3><p className="mt-1 text-sm text-slate-400">Remaining due: {money(selectedInvoice.balance, selectedInvoice.currency)} · {selectedInvoice.daysOverdue} days overdue</p></div><button type="button" className="text-sm text-slate-400" onClick={() => setSelectedInvoice(undefined)}>Cancel</button></div><div className="mt-4 grid gap-3 md:grid-cols-3"><label className="text-sm text-slate-300">Priority<select name="priority" className="field mt-2"><option>High</option><option>Critical</option><option>Normal</option></select></label><label className="text-sm text-slate-300">Follow-up due<input type="datetime-local" name="dueAtUtc" className="field mt-2" /></label><label className="text-sm text-slate-300 md:col-span-1">Follow-up note<input required name="note" className="field mt-2" placeholder="Next collection action" defaultValue={`Follow up overdue invoice ${selectedInvoice.invoiceNumber}.`} /></label></div><button disabled={saving} className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950 disabled:opacity-60">{saving ? "Creating…" : "Create collection task"}</button></form>}
    <section className="finance-control-workspace">
      <section className="surface-panel rounded-xl p-5"><div className="flex items-center justify-between"><h3 className="font-semibold">Overdue collections</h3><Link href="/fee-reminders" className="text-sm text-cyan-300">Reminders →</Link></div>{collections.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No overdue invoices.</p> : <div className="mt-4 space-y-3">{collections.map((item) => <div key={item.id} className="rounded border border-slate-700 p-3"><div className="flex items-start justify-between gap-3"><div><b>{item.invoiceNumber}</b><p className="mt-1 text-sm text-slate-400">Due {item.dueDate} · {item.daysOverdue} days overdue</p></div><span className="font-medium text-amber-200">Remaining due: {money(item.balance, item.currency)}</span></div><button type="button" onClick={() => setSelectedInvoice(item)} className="mt-3 text-sm font-medium text-cyan-300">Create follow-up →</button></div>)}</div>}</section>
      <section className="surface-panel rounded-xl p-5"><header className="finance-control-panel-header"><div><p className="finance-control-eyebrow">Approvals</p><h3>Adjustment approvals</h3></div></header>{pendingAdjustments.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No adjustments require approval.</p> : <div className="mt-4 space-y-3">{pendingAdjustments.map((item) => <div key={item.id} className="rounded border border-slate-700 p-3"><div className="flex items-start justify-between gap-3"><div><b>{item.type}</b><p className="mt-1 text-sm text-slate-400">{item.reason}</p></div><span className="font-medium text-amber-200">{money(item.amount)}</span></div>{decision?.id === item.id ? <form className="mt-4 space-y-3" onSubmit={(event) => { event.preventDefault(); void decide(item.id, decision.approve, decision.notes); }}><label className="field-label">{decision.approve ? "Approval note (optional)" : "Rejection reason"}<textarea className="field mt-2" value={decision.notes} required={!decision.approve} onChange={(event) => setDecision({ ...decision, notes: event.target.value })} /></label><div className="flex flex-wrap gap-3"><button type="submit" disabled={saving} className="enterprise-action-button disabled:opacity-60">{saving ? "Saving…" : decision.approve ? "Confirm approval" : "Confirm rejection"}</button><button type="button" disabled={saving} onClick={() => setDecision(undefined)} className="enterprise-action-button enterprise-action-button-secondary disabled:opacity-60">Cancel</button></div></form> : <div className="mt-3 flex gap-3"><button type="button" disabled={saving} onClick={() => setDecision({ id: item.id, approve: true, notes: "" })} className="text-sm font-medium text-emerald-300 disabled:opacity-60">Approve</button><button type="button" disabled={saving} onClick={() => setDecision({ id: item.id, approve: false, notes: "" })} className="text-sm font-medium text-rose-300 disabled:opacity-60">Reject</button></div>}</div>)}</div>}</section>
    </section>
    <section className="surface-panel finance-control-register"><div><p className="finance-control-eyebrow">Collections</p><h3>Escalation register</h3></div>{openCollections.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No active collections tasks. Create a follow-up from the overdue collection queue.</p> : <div className="mt-5 grid gap-3">{openCollections.map((item) => <form key={item.id} onSubmit={(event) => void updateCollection(item, event)} className="rounded border border-slate-700 p-4"><div className="flex flex-wrap items-start justify-between gap-3"><div><b>{item.title}</b><p className="mt-1 text-sm text-slate-400">{item.description || "No collection note"}</p></div><span className="text-sm text-slate-400">{item.priority} priority</span></div><div className="mt-3 grid gap-3 md:grid-cols-[1fr_1fr_auto]"><select name="stage" defaultValue={item.escalationStage || "Initial"} className="field"><option>Initial</option><option>Reminder</option><option>ManagerReview</option><option>FinalNotice</option></select><input type="date" name="promiseDate" defaultValue={item.promisedPaymentDate || ""} className="field" /><button disabled={saving} className="rounded border border-cyan-400 px-3 py-2 text-sm font-medium text-cyan-200 disabled:opacity-60">Save control</button></div></form>)}</div>}</section>
  </main>;
}

function DocumentPreview({ kind, theme, settings }: { kind: "Invoice" | "Payslip"; theme: string; settings?: DocumentSettings }) {
  return <article className={`document-full-preview document-template-${theme.toLowerCase()}`}><header>{settings?.invoiceLogoUrl ? <img src={settings.invoiceLogoUrl} alt="Academy logo"/> : <i>₹</i>}<div><p>{kind === "Invoice" ? "Tax invoice" : "Payroll payslip"}</p><h4>AcademyDesk Music Academy</h4></div><b>{kind === "Invoice" ? "INV-2026-001" : "PS-2026-001"}</b></header><div className="document-full-preview-body"><div><p>{kind === "Invoice" ? "Bill to" : "Paid to"}</p><strong>{kind === "Invoice" ? "Student name" : "Teacher or staff name"}</strong><span>{kind === "Invoice" ? "Tuition fee · September 2026" : "September 2026 · Monthly salary"}</span></div><div className="document-full-preview-total"><p>{kind === "Invoice" ? "Total payable" : "Net payout"}</p><strong>{kind === "Invoice" ? "₹2,500" : "₹18,000"}</strong></div></div><footer>{settings?.invoiceSignatureUrl && <img src={settings.invoiceSignatureUrl} alt="Signature"/>}<strong>{settings?.invoiceAuthorityName || "Authorised signatory"}</strong><span>{settings?.invoiceAuthorityTitle || "Finance"}</span></footer></article>;
}
