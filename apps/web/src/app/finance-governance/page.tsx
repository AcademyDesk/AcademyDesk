"use client";
import Image from "next/image";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import visual from "@/components/design-system/finance-controls.module.css";
import { StandardFormDate, StandardFormSelect } from "@/components/design-system/form-fields";
import { StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string };
type Adjustment = { id: string; type: string; amount: number; reason: string; status: string };
const needsLegacyReview = (item: Adjustment) => ["refund", "creditnote"].includes(item.type.toLowerCase());
type Collection = { id: string; invoiceNumber: string; totalAmount: number; balance: number; currency: string; dueDate: string; daysOverdue: number };
type WorkItem = { id: string; type: string; title: string; description?: string | null; priority: string; status: string; entityId?: string | null; dueAtUtc?: string | null; escalationStage?: string | null; promisedPaymentDate?: string | null };
type DocumentSettings = { taxRegistrationNumber: string; taxLabel: string; taxRatePercent: number; defaultPaymentTermsDays: number; taxInclusivePricing: boolean; invoiceLogoUrl?: string; invoiceAuthorityName?: string; invoiceAuthorityTitle?: string; invoiceSignatureUrl?: string; invoiceTemplateKey: string; payslipTemplateKey: string; settingsVersion?: string };
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
  const documentEditorOpen = useRef(false);
  const documentSavePending = useRef(false);
  const [savingDocuments, setSavingDocuments] = useState(false);
  const [ready, setReady] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const actionGuard = useRef({ ready: false, pending: false, review: false, snapshot: 0 });
  const snapshot = actionGuard.current.snapshot;
  const blocked = !ready || saving || needsReview || savingDocuments;

  const load = useCallback(async (id: string) => {
      const [adjustmentResponse, collectionsResponse, workResponse, settingsResponse] = await Promise.all([
        academyApi(`/api/academies/${id}/finance-adjustments`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/finance-governance/collections`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/finance-governance/collection-tasks`, { cache: "no-store" }), academyApi(`/api/academies/${id}/finance-governance/settings`, { cache: "no-store" }),
      ]);
      if (!adjustmentResponse.ok || !collectionsResponse.ok || !workResponse.ok || !settingsResponse.ok) throw new Error();
      const [adjustmentRows, collectionRows, taskRows, loadedSettings] = await Promise.all([adjustmentResponse.json(), collectionsResponse.json(), workResponse.json(), settingsResponse.json()]);
      if (![adjustmentRows, collectionRows, taskRows].every(Array.isArray) || !loadedSettings || typeof loadedSettings !== "object" || Array.isArray(loadedSettings)) throw new Error();
      // Publish one checked snapshot. Never rebase an open document draft onto a newer version.
      setAdjustments(adjustmentRows); setCollections(collectionRows); setWorkItems(taskRows);
      if (!documentEditorOpen.current) setDocumentSettings(loadedSettings);
      actionGuard.current.snapshot++; actionGuard.current.ready = true; setReady(true);
  }, []);

  function requireReview() { actionGuard.current.review = true; setNeedsReview(true); }
  async function refreshRegister() {
    if (!academy || actionGuard.current.pending || documentSavePending.current) return;
    actionGuard.current.pending = true; setSaving(true);
    try { await load(academy.id); actionGuard.current.review = false; setNeedsReview(false); setNoticeIsError(false); setNotice("Finance controls refreshed. Check the previous action before trying again."); }
    catch { requireReview(); setNoticeIsError(true); setNotice("Finance controls could not be refreshed. Do not repeat an unconfirmed action."); }
    finally { actionGuard.current.pending = false; setSaving(false); }
  }

  useEffect(() => { void (async () => { try { const response = await academyApi("/api/academies", { cache: "no-store" }); if (!response.ok) throw new Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setNotice("Create an academy before managing finance governance."); setAcademy(academies[0]); await load(academies[0].id); setNoticeIsError(false); setNotice(""); } catch { setNoticeIsError(true); setNotice("Finance controls could not be loaded. Please sign in and restart the API if needed."); } })(); }, [load]);

  function canAct() { return academy && snapshot === actionGuard.current.snapshot && actionGuard.current.ready && !actionGuard.current.pending && !actionGuard.current.review && !documentSavePending.current; }
  async function performAction(path: string, method: "POST" | "PATCH", body: object, success: string, rejected: string, confirmed?: () => void) {
    if (!canAct() || !academy) return;
    actionGuard.current.pending = true; setSaving(true); setNoticeIsError(false); setNotice("Saving finance action…");
    const uncertain = () => { requireReview(); setNoticeIsError(true); setNotice("The action could not be confirmed. Refresh finance controls and check the previous action before retrying; your draft is retained."); };
    try {
      const response = await academyApi(`/api/academies/${academy.id}/${path}`, { method, headers: apiHeaders(true), body: JSON.stringify(body) });
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408) uncertain();
        else { const problem = await response.json().catch(() => null); setNoticeIsError(true); setNotice(typeof problem?.message === "string" && problem.message.trim() ? problem.message : rejected); }
        return;
      }
      confirmed?.(); setNotice(success);
      try { await load(academy.id); }
      catch { requireReview(); setNoticeIsError(true); setNotice(`${success} The latest list could not be refreshed; do not repeat this action. Refresh finance controls to continue.`); }
    } catch { uncertain(); }
    finally { actionGuard.current.pending = false; setSaving(false); }
  }

  async function decide(id: string, approve: boolean, notes: string) {
    if (!canAct()) return;
    const item = adjustments.find(item => item.id === id && item.status === "PendingApproval");
    if (!item || needsLegacyReview(item)) { setNoticeIsError(true); return setNotice("Select a pending adjustment eligible for this decision. Legacy refund and credit-note labels require reviewed disposition."); }
    if (!approve && !notes.trim()) { setNoticeIsError(true); return setNotice("A rejection reason is required."); }
    await performAction(`finance-adjustments/${id}/approval`, "PATCH", { approve, notes }, `Adjustment ${approve ? "approved" : "rejected"} and recorded in the control register.`, "The decision could not be saved. Review the invoice and your access before trying again.", () => setDecision(current => current?.id === id && current.approve === approve && current.notes === notes ? undefined : current));
  }

  async function createFollowUp(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canAct() || !selectedInvoice) return;
    const data = new FormData(event.currentTarget), invoice = collections.find(item => item.id === selectedInvoice.id);
    if (!invoice || invoice.balance <= 0 || !String(data.get("note") ?? "").trim() || !["High", "Critical", "Normal"].includes(String(data.get("priority")))) { setNoticeIsError(true); return setNotice("Select an overdue invoice, priority and follow-up note."); }
    await performAction(`finance-governance/collections/${invoice.id}/follow-up`, "POST", { note: data.get("note"), priority: data.get("priority"), assignedUserId: null, dueAtUtc: data.get("dueAtUtc") || null }, "Collections follow-up created in the operational queue.", "The collections follow-up could not be created. Check the invoice and your access.", () => setSelectedInvoice(current => current?.id === invoice.id ? undefined : current));
  }

  async function updateCollection(item: WorkItem, event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!canAct()) return;
    const data = new FormData(event.currentTarget), current = workItems.find(row => row.id === item.id);
    if (!current || current.type !== "Collections" || ["Completed", "Cancelled"].includes(current.status) || !["Initial", "Reminder", "ManagerReview", "FinalNotice"].includes(String(data.get("stage")))) { setNoticeIsError(true); return setNotice("Select an active collection task and a valid escalation stage."); }
    await performAction(`finance-governance/collection-tasks/${item.id}`, "PATCH", { escalationStage: data.get("stage"), promisedPaymentDate: data.get("promiseDate") || null }, "Collections escalation state updated.", "The escalation state could not be saved. Check the task and your access.");
  }
  async function saveDocumentSettings(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy || !documentSettings || documentSavePending.current) return;
    if (!documentSettings.settingsVersion) { setNoticeIsError(true); return setNotice("Refresh settings before saving. Your draft has not been saved."); }
    const data = new FormData(event.currentTarget); documentSavePending.current = true; setSavingDocuments(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/finance-governance/settings`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ ...documentSettings, invoiceLogoUrl: data.get("invoiceLogoUrl"), invoiceAuthorityName: data.get("invoiceAuthorityName"), invoiceAuthorityTitle: data.get("invoiceAuthorityTitle"), invoiceSignatureUrl: data.get("invoiceSignatureUrl"), invoiceTemplateKey: data.get("invoiceTemplateKey"), payslipTemplateKey: data.get("payslipTemplateKey") }) });
      if (response.status === 409) { setNoticeIsError(true); return setNotice("Settings changed in another window. Your draft has not been saved. Refresh this page and review the latest settings before saving again."); }
      if (response.status === 428) { setNoticeIsError(true); return setNotice("Refresh settings before saving. Your draft has not been saved."); }
      if (response.status === 400) {
        const rejected: unknown = await response.json().catch(() => null);
        const reason = rejected && typeof rejected === "object" && "message" in rejected ? rejected.message : null;
        if (reason === "Tax label must not exceed 30 characters. Your draft has not been saved." || reason === "Tax registration number must not exceed 50 characters. Your draft has not been saved.") { setNoticeIsError(true); return setNotice(reason); }
      }
      if (!response.ok) { setNoticeIsError(true); return setNotice("Document settings could not be saved."); }
      setDocumentSettings(await response.json()); documentEditorOpen.current = false; setEditingDocuments(false); setNoticeIsError(false); setNotice("Invoice and payslip settings saved.");
    } catch { setNoticeIsError(true); setNotice("The save could not be confirmed. Check the latest settings before retrying; your draft is retained."); }
    finally { documentSavePending.current = false; setSavingDocuments(false); }
  }

  const openCollections = workItems.filter((item) => item.type === "Collections" && item.status !== "Completed" && item.status !== "Cancelled");
  const pendingAdjustments = adjustments.filter((item) => item.status === "PendingApproval");

  return <main className={`enterprise-settings finance-module finance-control-standard ${visual.page}`}>
    <header className="enterprise-page-header"><p>Finance / governance</p><div className="flex flex-wrap items-end justify-between gap-4"><div><h1>Finance control centre</h1><span>Approve financial adjustments, turn overdue exposure into accountable follow-up, and document collection commitments.</span></div><Link href="/finance" className="text-sm font-medium text-cyan-300">Finance workspace →</Link></div></header>
    {notice && <p className="enterprise-settings-empty mt-5" role={noticeIsError ? "alert" : "status"}>{notice}</p>}
    {academy && <button type="button" disabled={saving || savingDocuments} onClick={refreshRegister} className="enterprise-action-button">Refresh finance controls</button>}
    <section className="finance-control-kpis"><section className="surface-panel"><p>Pending adjustments</p><b>{ready ? pendingAdjustments.length : "—"}</b></section><section className="surface-panel"><p>Overdue invoices</p><b>{ready ? collections.length : "—"}</b></section><section className="surface-panel"><p>Open collection tasks</p><b>{ready ? openCollections.length : "—"}</b></section></section>
    <section className="surface-panel finance-control-documents"><div className="flex flex-wrap items-center justify-between gap-3"><div><p className="finance-control-eyebrow">Documents</p><h3>Invoice & payslip templates</h3></div><button type="button" disabled={blocked || !documentSettings} onClick={() => { if (!documentSettings) return; documentEditorOpen.current = !editingDocuments; setEditingDocuments(!editingDocuments); }} className="enterprise-action-button enterprise-action-button-secondary">{editingDocuments ? "Close edit" : "Edit document setup"}</button></div>{documentSettings && <><div className="mt-5 grid gap-3 md:grid-cols-2"><StandardSelectField name="documentKind" label="Document" disabled={blocked} value={documentKind} onChange={value => { if (value === "Invoice" || value === "Payslip") setDocumentKind(value); }} placeholder="Choose document" options={[{value:"Invoice",label:"Invoice"},{value:"Payslip",label:"Payslip"}]}/><StandardSelectField name="documentTheme" label="Theme" disabled={blocked} value={documentKind === "Invoice" ? documentSettings.invoiceTemplateKey : documentSettings.payslipTemplateKey} onChange={value => { if (value) setDocumentSettings({ ...documentSettings, [documentKind === "Invoice" ? "invoiceTemplateKey" : "payslipTemplateKey"]: value }); }} placeholder="Choose theme" options={(documentKind === "Invoice" ? ["Classic","Modern","Minimal","Formal"] : ["Standard","Compact","Professional"]).map(value => ({value,label:value}))}/></div><p data-template-preview="true">Illustrative template preview — not an issued invoice or payslip.</p><DocumentPreview kind={documentKind} theme={documentKind === "Invoice" ? documentSettings.invoiceTemplateKey : documentSettings.payslipTemplateKey} settings={documentSettings}/>{editingDocuments && <form onSubmit={saveDocumentSettings} className="mt-5 border-t border-slate-700 pt-5"><fieldset disabled={blocked}><h3>Document branding</h3><div className="mt-4 grid gap-3 md:grid-cols-2"><label className="field-label md:col-span-2">Academy logo URL<input name="invoiceLogoUrl" type="url" defaultValue={documentSettings.invoiceLogoUrl ?? ""} className="field"/></label><label className="field-label">Authorised signatory<input name="invoiceAuthorityName" defaultValue={documentSettings.invoiceAuthorityName ?? ""} className="field"/></label><label className="field-label">Designation<input name="invoiceAuthorityTitle" defaultValue={documentSettings.invoiceAuthorityTitle ?? ""} className="field"/></label><label className="field-label md:col-span-2">Signature image URL<input name="invoiceSignatureUrl" type="url" defaultValue={documentSettings.invoiceSignatureUrl ?? ""} className="field"/></label><input name="invoiceTemplateKey" type="hidden" value={documentSettings.invoiceTemplateKey}/><input name="payslipTemplateKey" type="hidden" value={documentSettings.payslipTemplateKey}/></div><button disabled={blocked} className="enterprise-action-button mt-5">{savingDocuments ? "Saving…" : "Save document setup"}</button></fieldset></form>}</>}</section>
    {selectedInvoice && <form onSubmit={createFollowUp} className="surface-panel mt-5 rounded-xl p-5"><div className="flex flex-wrap items-end justify-between gap-4"><div><h3 className="font-semibold">Create follow-up for {selectedInvoice.invoiceNumber}</h3><p className="mt-1 text-sm text-slate-400">Remaining due: {money(selectedInvoice.balance, selectedInvoice.currency)} · {selectedInvoice.daysOverdue} days overdue</p></div><button type="button" disabled={blocked} className="text-sm text-slate-400" onClick={() => setSelectedInvoice(undefined)}>Cancel</button></div><div className="mt-4 grid gap-3 md:grid-cols-3"><StandardFormSelect name="priority" label="Priority" defaultValue="High" options={["High","Critical","Normal"]} disabled={blocked}/><label className="text-sm text-slate-300">Follow-up due (local time, optional)<input disabled={blocked} type="datetime-local" name="dueAtUtc" className="field mt-2" /></label><label className="text-sm text-slate-300 md:col-span-1">Follow-up note<input disabled={blocked} required name="note" className="field mt-2" placeholder="Next collection action" defaultValue={`Follow up overdue invoice ${selectedInvoice.invoiceNumber}.`} /></label></div><button disabled={blocked} className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950 disabled:opacity-60">{saving ? "Creating…" : "Create collection task"}</button></form>}
    <section className="finance-control-workspace">
      <section className="surface-panel rounded-xl p-5"><div className="flex items-center justify-between"><h3 className="font-semibold">Overdue collections</h3><Link href="/fee-reminders" className="text-sm text-cyan-300">Reminders →</Link></div>{!ready ? <p className="mt-5">Finance controls unavailable until loading succeeds.</p> : collections.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No overdue invoices.</p> : <div className="mt-4 space-y-3">{collections.map((item) => <div key={item.id} className="rounded border border-slate-700 p-3"><div className="flex items-start justify-between gap-3"><div><b>{item.invoiceNumber}</b><p className="mt-1 text-sm text-slate-400">Due {item.dueDate} · {item.daysOverdue} days overdue</p></div><span className="font-medium text-amber-200">Remaining due: {money(item.balance, item.currency)}</span></div><button type="button" disabled={blocked} onClick={() => setSelectedInvoice(item)} className="mt-3 text-sm font-medium text-cyan-300">Create follow-up →</button></div>)}</div>}</section>
      <section className="surface-panel rounded-xl p-5"><header className="finance-control-panel-header"><div><p className="finance-control-eyebrow">Approvals</p><h3>Adjustment approvals</h3></div></header>{!ready ? <p className="mt-5">Finance controls unavailable until loading succeeds.</p> : pendingAdjustments.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No adjustments require approval.</p> : <div className="mt-4 space-y-3">{pendingAdjustments.map((item) => <div key={item.id} className="rounded border border-slate-700 p-3"><div className="flex items-start justify-between gap-3"><div><b>{item.type}</b><p className="mt-1 text-sm text-slate-400">{item.reason}</p></div><span className="font-medium text-amber-200">{money(item.amount)}</span></div>{needsLegacyReview(item) ? <p className="mt-3 text-sm">Legacy reduction label: not a refund settlement or issued credit note. Awaiting reviewed disposition; approve/reject is unavailable here.</p> : decision?.id === item.id ? <form className="mt-4 space-y-3" onSubmit={(event) => { event.preventDefault(); void decide(item.id, decision.approve, decision.notes); }}><label className="field-label">{decision.approve ? "Approval note (optional)" : "Rejection reason"}<textarea disabled={blocked} className="field mt-2" value={decision.notes} required={!decision.approve} onChange={(event) => setDecision({ ...decision, notes: event.target.value })} /></label><div className="flex flex-wrap gap-3"><button type="submit" disabled={blocked} className="enterprise-action-button disabled:opacity-60">{saving ? "Saving…" : decision.approve ? "Confirm approval" : "Confirm rejection"}</button><button type="button" disabled={blocked} onClick={() => setDecision(undefined)} className="enterprise-action-button enterprise-action-button-secondary disabled:opacity-60">Cancel</button></div></form> : <div className="mt-3 flex gap-3"><button type="button" disabled={blocked} onClick={() => setDecision({ id: item.id, approve: true, notes: "" })} className="text-sm font-medium text-emerald-300 disabled:opacity-60">Approve</button><button type="button" disabled={blocked} onClick={() => setDecision({ id: item.id, approve: false, notes: "" })} className="text-sm font-medium text-rose-300 disabled:opacity-60">Reject</button></div>}</div>)}</div>}</section>
    </section>
    <section className="surface-panel finance-control-register"><div><p className="finance-control-eyebrow">Collections</p><h3>Escalation register</h3></div>{!ready ? <p className="mt-5">Finance controls unavailable until loading succeeds.</p> : openCollections.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No active collections tasks. Create a follow-up from the overdue collection queue.</p> : <div className="mt-5 grid gap-3">{openCollections.map((item) => <form key={item.id} onSubmit={(event) => void updateCollection(item, event)} className="rounded border border-slate-700 p-4"><div className="flex flex-wrap items-start justify-between gap-3"><div><b>{item.title}</b><p className="mt-1 text-sm text-slate-400">{item.description || "No collection note"}</p></div><span className="text-sm text-slate-400">{item.priority} priority</span></div><div className="mt-3 grid gap-3 md:grid-cols-[1fr_1fr_auto]"><StandardFormSelect name="stage" label="Escalation stage" defaultValue={item.escalationStage || "Initial"} options={["Initial","Reminder","ManagerReview","FinalNotice"]} disabled={blocked}/><StandardFormDate name="promiseDate" label="Promised payment date (optional)" defaultValue={item.promisedPaymentDate || ""} disabled={blocked}/><button disabled={blocked} className="rounded border border-cyan-400 px-3 py-2 text-sm font-medium text-cyan-200 disabled:opacity-60">Save control</button></div></form>)}</div>}</section>
  </main>;
}

function DocumentPreview({ kind, theme, settings }: { kind: "Invoice" | "Payslip"; theme: string; settings?: DocumentSettings }) {
  return <article className={`document-full-preview document-template-${theme.toLowerCase()}`}><header>{settings?.invoiceLogoUrl ? <Image unoptimized loading="eager" width={48} height={48} src={settings.invoiceLogoUrl} alt="Academy logo"/> : <i>₹</i>}<div><p>{kind === "Invoice" ? "Tax invoice" : "Payroll payslip"}</p><h4>AcademyDesk Music Academy</h4></div><b>{kind === "Invoice" ? "INV-2026-001" : "PS-2026-001"}</b></header><div className="document-full-preview-body"><div><p>{kind === "Invoice" ? "Bill to" : "Paid to"}</p><strong>{kind === "Invoice" ? "Student name" : "Teacher or staff name"}</strong><span>{kind === "Invoice" ? "Tuition fee · September 2026" : "September 2026 · Monthly salary"}</span></div><div className="document-full-preview-total"><p>{kind === "Invoice" ? "Total payable" : "Net payout"}</p><strong>{kind === "Invoice" ? "₹2,500" : "₹18,000"}</strong></div></div><footer>{settings?.invoiceSignatureUrl && <Image unoptimized loading="eager" width={110} height={42} style={{ height: "auto" }} src={settings.invoiceSignatureUrl} alt="Signature"/>}<strong>{settings?.invoiceAuthorityName || "Authorised signatory"}</strong><span>{settings?.invoiceAuthorityTitle || "Finance"}</span></footer></article>;
}
