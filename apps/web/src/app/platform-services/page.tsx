"use client";

import { FormEvent, useEffect, useState } from "react";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string };
type PlatformInvoice = { id: string; invoiceNumber: string; amount: number; currency: string; status: string; periodStart: string; periodEnd: string; dueDate: string; paymentReference?: string | null; paymentSubmittedAtUtc?: string | null };
type SupportCase = { id: string; subject: string; priority: string; status: string; description?: string | null; academyResponse?: string | null; createdAtUtc: string; academyRespondedAtUtc?: string | null };

const date = (value: string) => new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(`${value}T12:00:00`));

export default function PlatformServicesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [invoices, setInvoices] = useState<PlatformInvoice[]>([]);
  const [cases, setCases] = useState<SupportCase[]>([]);
  const [message, setMessage] = useState("Loading platform services…");
  const [busy, setBusy] = useState(false);
  const [priority, setPriority] = useState("Normal");
  const [references, setReferences] = useState<Record<string, string>>({});
  const [responses, setResponses] = useState<Record<string, string>>({});

  async function load() {
    try {
      const academyResponse = await academyApi("/api/academies", { cache: "no-store" });
      if (!academyResponse.ok) throw new Error();
      const academies: Academy[] = await academyResponse.json();
      if (!academies[0]) throw new Error();
      const selected = academies[0]; setAcademy(selected);
      const [invoiceResponse, caseResponse] = await Promise.all([
        academyApi(`/api/academies/${selected.id}/platform-services/billing-invoices`, { cache: "no-store" }),
        academyApi(`/api/academies/${selected.id}/platform-services/support-cases`, { cache: "no-store" }),
      ]);
      if (!invoiceResponse.ok || !caseResponse.ok) throw new Error();
      setInvoices(await invoiceResponse.json()); setCases(await caseResponse.json()); setMessage("");
    } catch { setMessage("Platform services could not be loaded."); }
  }
  useEffect(() => { void load(); }, []);

  async function submitPayment(invoice: PlatformInvoice) {
    if (!academy) return; setBusy(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/platform-services/billing-invoices/${invoice.id}/payment-submission`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ reference: references[invoice.id] || null }) });
      if (!response.ok) throw new Error(); setMessage("Payment submitted for Platform Owner review."); await load();
    } catch { setMessage("Payment submission could not be sent."); } finally { setBusy(false); }
  }
  async function createCase(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy) return; const form = new FormData(event.currentTarget); setBusy(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/platform-services/support-cases`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ subject: form.get("subject"), priority, description: form.get("description") }) });
      if (!response.ok) throw new Error(); event.currentTarget.reset(); setPriority("Normal"); setMessage("Support request sent to the Platform Owner."); await load();
    } catch { setMessage("Support request could not be sent."); } finally { setBusy(false); }
  }
  async function respond(item: SupportCase) {
    if (!academy || !responses[item.id]?.trim()) return; setBusy(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/platform-services/support-cases/${item.id}/response`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ message: responses[item.id] }) });
      if (!response.ok) throw new Error(); setResponses(current => ({ ...current, [item.id]: "" })); setMessage("Response sent to the Platform Owner."); await load();
    } catch { setMessage("Response could not be sent."); } finally { setBusy(false); }
  }

  return <main className="enterprise-settings platform-services-standard">
    <header className="platform-services-heading"><span aria-hidden="true">⇄</span><div><p>Administration</p><h1>Platform billing &amp; support</h1></div></header>
    {message && <p className="platform-services-notice" role="status">{message}</p>}
    <section className="platform-services-kpis"><article><span>Platform invoices</span><b>{invoices.length}</b></article><article><span>Payment review</span><b>{invoices.filter(item => item.status === "Payment submitted").length}</b></article><article><span>Open support</span><b>{cases.filter(item => !["Resolved", "Closed"].includes(item.status)).length}</b></article></section>
    <section className="platform-services-grid">
      <section className="platform-services-panel"><header><p>Billing register</p><h2>Platform invoices</h2></header>{invoices.length ? <ul className="platform-invoice-list">{invoices.map(invoice => <li key={invoice.id}><div><b>{invoice.invoiceNumber}</b><small>{date(invoice.periodStart)} – {date(invoice.periodEnd)} · due {date(invoice.dueDate)}</small></div><strong>{invoice.currency} {invoice.amount.toLocaleString("en-IN", { minimumFractionDigits: 2 })}</strong><em data-status={invoice.status.toLowerCase().replaceAll(" ", "-")}>{invoice.status}</em>{invoice.status === "Payment submitted" ? <small className="platform-submitted">Submitted{invoice.paymentReference ? ` · ${invoice.paymentReference}` : ""}</small> : !["Paid", "Void"].includes(invoice.status) && <div className="platform-payment-form"><input className="field" value={references[invoice.id] ?? ""} onChange={event => setReferences(current => ({ ...current, [invoice.id]: event.target.value }))} placeholder="Payment reference (optional)" /><button type="button" disabled={busy} className="enterprise-action-button" onClick={() => void submitPayment(invoice)}>Submit payment</button></div>}</li>)}</ul> : <p className="platform-services-empty">No platform invoices are available.</p>}</section>
      <form onSubmit={createCase} className="platform-services-panel platform-support-form"><header><p>Contact Platform Owner</p><h2>New support request</h2></header><div><label><span>Subject</span><input className="field" name="subject" required placeholder="What do you need help with?" /></label><label><span>Priority</span><StandardSelectField name="priority" value={priority} onChange={setPriority} placeholder="Select priority" options={["Low", "Normal", "High", "Critical"].map(value => ({ value, label: value }))} /></label><label className="platform-services-wide"><span>Message</span><textarea className="field" name="description" required placeholder="Describe the issue, context, and expected outcome." /></label><button disabled={busy} className="enterprise-action-button platform-services-wide">Send support request</button></div></form>
    </section>
    <section className="platform-services-panel platform-support-history"><header><p>Conversation register</p><h2>Support requests</h2></header>{cases.length ? <ul>{cases.map(item => <li key={item.id}><div className="platform-case-summary"><div><b>{item.subject}</b><small>{item.priority} priority · {new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(item.createdAtUtc))}</small></div><em data-status={item.status.toLowerCase()}>{item.status}</em></div><p>{item.description}</p>{item.academyResponse && <div className="platform-case-response"><span>Your latest response</span><p>{item.academyResponse}</p></div>} {!['Resolved', 'Closed'].includes(item.status) && <div className="platform-case-reply"><textarea className="field" value={responses[item.id] ?? ""} onChange={event => setResponses(current => ({ ...current, [item.id]: event.target.value }))} placeholder="Send an update to the Platform Owner" /><button type="button" disabled={busy || !responses[item.id]?.trim()} className="enterprise-action-button enterprise-action-button-secondary" onClick={() => void respond(item)}>Send update</button></div>}</li>)}</ul> : <p className="platform-services-empty">No support requests have been created.</p>}</section>
  </main>;
}
