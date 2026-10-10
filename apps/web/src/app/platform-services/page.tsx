"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string };
type PlatformInvoice = { id: string; invoiceNumber: string; amount: number; currency: string; status: string; periodStart: string; periodEnd: string; dueDate: string; paymentReference?: string | null; paymentSubmittedAtUtc?: string | null };
type SupportCase = { id: string; subject: string; priority: string; status: string; description?: string | null; academyResponse?: string | null; createdAtUtc: string; academyRespondedAtUtc?: string | null };

const date = (value: string) => new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(`${value}T12:00:00`));


async function fetchWorkspace() {
  const academyResponse = await academyApi("/api/academies", { cache: "no-store" });
  if (!academyResponse.ok) throw new Error();
  const academies: Academy[] = await academyResponse.json();
  if (!academies[0]) throw new Error();
  const selected = academies[0];
  const [invoiceResponse, caseResponse] = await Promise.all([
    academyApi(`/api/academies/${selected.id}/platform-services/billing-invoices`, { cache: "no-store" }),
    academyApi(`/api/academies/${selected.id}/platform-services/support-cases`, { cache: "no-store" }),
  ]);
  if (!invoiceResponse.ok || !caseResponse.ok) throw new Error();
  const invoices: PlatformInvoice[] = await invoiceResponse.json();
  const cases: SupportCase[] = await caseResponse.json();
  if (!Array.isArray(invoices) || !Array.isArray(cases)) throw new Error();
  return { academy: selected, invoices, cases };
}
export default function PlatformServicesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [invoices, setInvoices] = useState<PlatformInvoice[]>([]);
  const [cases, setCases] = useState<SupportCase[]>([]);
  const [message, setMessage] = useState("Loading platform services…");
  const [busy, setBusy] = useState(false);
  const [priority, setPriority] = useState("Normal");
  const [references, setReferences] = useState<Record<string, string>>({});
  const [responses, setResponses] = useState<Record<string, string>>({});
  const pending = useRef(false);
  useEffect(() => {
    let active = true;
    void fetchWorkspace().then(workspace => {
      if (!active) return;
      setAcademy(workspace.academy); setInvoices(workspace.invoices); setCases(workspace.cases); setMessage("");
    }).catch(() => {
      if (active) setMessage("Platform services could not be loaded.");
    });
    return () => { active = false; };
  }, []);

  async function mutate(action: () => Promise<Response>, success: string, failure: string, afterSave?: () => void) {
    if (!academy || pending.current) return;
    pending.current = true; setBusy(true); setMessage("");
    try {
      const response = await action();
      if (!response.ok) return setMessage(response.status >= 500
        ? `${failure} The result could not be confirmed. Check platform invoices or support history before retrying.`
        : `${failure} Your entries have been retained.`);
      afterSave?.();
      setMessage(success);
      try {
        const workspace = await fetchWorkspace();
        setAcademy(workspace.academy); setInvoices(workspace.invoices); setCases(workspace.cases);
      } catch {
        setMessage(`${success} Platform services could not be refreshed; do not repeat the submission. Refresh the page to check the saved record.`);
      }
    } catch {
      setMessage("Submission could not be confirmed. Your entries have been retained; check platform invoices or support history before retrying.");
    } finally { pending.current = false; setBusy(false); }
  }
  async function submitPayment(invoice: PlatformInvoice) {
    if (!academy) return;
    await mutate(() => academyApi(`/api/academies/${academy.id}/platform-services/billing-invoices/${invoice.id}/payment-submission`, {
      method: "POST", headers: apiHeaders(true), body: JSON.stringify({ reference: references[invoice.id] || null }),
    }), "Payment submitted for Platform Owner review.", "Payment submission could not be sent.");
  }
  async function createCase(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || pending.current) return;
    const element = event.currentTarget;
    const form = new FormData(element);
    await mutate(() => academyApi(`/api/academies/${academy.id}/platform-services/support-cases`, {
      method: "POST", headers: apiHeaders(true), body: JSON.stringify({ subject: form.get("subject"), priority, description: form.get("description") }),
    }), "Support request sent to the Platform Owner.", "Support request could not be sent.", () => {
      element.reset(); setPriority("Normal");
    });
  }
  async function respond(item: SupportCase) {
    if (!academy || !responses[item.id]?.trim()) return;
    await mutate(() => academyApi(`/api/academies/${academy.id}/platform-services/support-cases/${item.id}/response`, {
      method: "POST", headers: apiHeaders(true), body: JSON.stringify({ message: responses[item.id] }),
    }), "Response sent to the Platform Owner.", "Response could not be sent.", () => {
      setResponses(current => ({ ...current, [item.id]: "" }));
    });
  }

  return <main className="enterprise-settings platform-services-standard">
    <header className="platform-services-heading"><span aria-hidden="true">⇄</span><div><p>Administration</p><h1>Platform billing &amp; support</h1></div></header>
    {message && <p className="platform-services-notice" role="status" aria-live="polite">{message}</p>}
    <section className="platform-services-kpis"><article><span>Platform invoices</span><b>{invoices.length}</b></article><article><span>Payment review</span><b>{invoices.filter(item => item.status === "Payment submitted").length}</b></article><article><span>Open support</span><b>{cases.filter(item => !["Resolved", "Closed"].includes(item.status)).length}</b></article></section>
    <section className="platform-services-grid">
      <section className="platform-services-panel"><header><p>Billing register</p><h2>Platform invoices</h2></header>{invoices.length ? <ul className="platform-invoice-list">{invoices.map(invoice => <li key={invoice.id}><div><b>{invoice.invoiceNumber}</b><small>{date(invoice.periodStart)} – {date(invoice.periodEnd)} · due {date(invoice.dueDate)}</small></div><strong>{invoice.currency} {invoice.amount.toLocaleString("en-IN", { minimumFractionDigits: 2 })}</strong><em data-status={invoice.status.toLowerCase().replaceAll(" ", "-")}>{invoice.status}</em>{invoice.status === "Payment submitted" ? <small className="platform-submitted">Submitted{invoice.paymentReference ? ` · ${invoice.paymentReference}` : ""}</small> : !["Paid", "Void"].includes(invoice.status) && <div className="platform-payment-form"><input className="field" disabled={busy} aria-label={`Payment reference for ${invoice.invoiceNumber}`} value={references[invoice.id] ?? ""} onChange={event => setReferences(current => ({ ...current, [invoice.id]: event.target.value }))} placeholder="Payment reference (optional)" /><button type="button" disabled={busy} className="enterprise-action-button" onClick={() => void submitPayment(invoice)}>Submit payment</button></div>}</li>)}</ul> : <p className="platform-services-empty">No platform invoices are available.</p>}</section>
      <form onSubmit={createCase} aria-busy={busy} className="platform-services-panel platform-support-form"><header><p>Contact Platform Owner</p><h2>New support request</h2></header><div><label><span>Subject</span><input className="field" name="subject" disabled={busy || !academy} required placeholder="What do you need help with?" /></label><label><span>Priority</span><StandardSelectField name="priority" disabled={busy || !academy} value={priority} onChange={setPriority} placeholder="Select priority" options={["Low", "Normal", "High", "Critical"].map(value => ({ value, label: value }))} /></label><label className="platform-services-wide"><span>Message</span><textarea className="field" name="description" disabled={busy || !academy} required placeholder="Describe the issue, context, and expected outcome." /></label><button disabled={busy || !academy} className="enterprise-action-button platform-services-wide">Send support request</button></div></form>
    </section>
    <section className="platform-services-panel platform-support-history"><header><p>Conversation register</p><h2>Support requests</h2></header>{cases.length ? <ul>{cases.map(item => <li key={item.id}><div className="platform-case-summary"><div><b>{item.subject}</b><small>{item.priority} priority · {new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(item.createdAtUtc))}</small></div><em data-status={item.status.toLowerCase()}>{item.status}</em></div><p>{item.description}</p>{item.academyResponse && <div className="platform-case-response"><span>Your latest response</span><p>{item.academyResponse}</p></div>} {!['Resolved', 'Closed'].includes(item.status) && <div className="platform-case-reply"><textarea className="field" disabled={busy} aria-label={`Update for ${item.subject}`} value={responses[item.id] ?? ""} onChange={event => setResponses(current => ({ ...current, [item.id]: event.target.value }))} placeholder="Send an update to the Platform Owner" /><button type="button" disabled={busy || !responses[item.id]?.trim()} className="enterprise-action-button enterprise-action-button-secondary" onClick={() => void respond(item)}>Send update</button></div>}</li>)}</ul> : <p className="platform-services-empty">No support requests have been created.</p>}</section>
  </main>;
}
