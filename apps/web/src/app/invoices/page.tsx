"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string; legalName?: string };
type Student = { id: string; firstName: string; lastName: string; email?: string; phone?: string };
type FeePlan = { id: string; name: string; amount: number; currency: string; frequency: string };
type Invoice = { id: string; invoiceNumber: string; studentId: string; feePlanId?: string | null; totalAmount: number; adjustedAmount?: number; paidAmount?: number; balance?: number; currency: string; issuedDate: string; dueDate: string; status: string };
type Settings = { taxRegistrationNumber: string; taxLabel: string; invoiceLogoUrl: string | null; invoiceAuthorityName: string | null; invoiceAuthorityTitle: string | null; invoiceSignatureUrl: string | null; invoiceTemplateKey: string };
// Preserve the established document currency formatting, with honest legacy metadata.
const unavailable = "Currency details unavailable";
const money = (amount: number | undefined, currency?: string) => {
  if (typeof amount !== "number" || !Number.isFinite(amount) || typeof currency !== "string") return unavailable;
  const code = currency.trim().toUpperCase();
  try { return new Intl.NumberFormat("en-IN", { style: "currency", currency: code, minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount); }
  catch { return `${code || "Unspecified currency"} ${amount.toFixed(2)}`; }
};
const currencyTotal = (rows: { amount: number; currency: string }[]) => {
  const totals = new Map<string, number>();
  for (const row of rows) {
    if (!Number.isFinite(row.amount) || typeof row.currency !== "string") return unavailable;
    const code = row.currency.trim().toUpperCase(), total = (totals.get(code) ?? 0) + row.amount;
    if (!Number.isFinite(total)) return unavailable;
    totals.set(code, total);
  }
  return [...totals].sort(([a], [b]) => a.localeCompare(b)).map(([code, amount]) => `${totals.size > 1 || code !== "INR" ? code + " " : ""}${money(amount, code)}`).join(" · ") || "0.00";
};
const cancelled = (invoice: Invoice) => invoice.status.toLowerCase() === "cancelled";
const invoiceBalance = (invoice: Invoice) => typeof invoice.balance === "number" && Number.isFinite(invoice.balance) && invoice.balance >= 0 ? money(invoice.balance, invoice.currency) : "Balance unavailable";
class InvoiceReadError extends Error { constructor(readonly status: number) { super("Invoices could not be loaded."); } }
const readMessage = (error: unknown) => "Invoices could not be loaded." + (error instanceof InvoiceReadError
  ? error.status === 401 ? " Sign in again." : error.status === 403 ? " This account does not have access to the required invoice records." : error.status === 429 ? " Please wait before trying again." : " Try again to reload the records." : " Try again to reload the records.");

export default function InvoicesPage() {
  const [academy, setAcademy] = useState<Academy>(); const [students, setStudents] = useState<Student[]>([]); const [plans, setPlans] = useState<FeePlan[]>([]); const [invoices, setInvoices] = useState<Invoice[]>([]); const [settings, setSettings] = useState<Settings>(); const [studentId, setStudentId] = useState(""); const [feePlanId, setFeePlanId] = useState(""); const [amount, setAmount] = useState(""); const [dueDate, setDueDate] = useState(""); const [selected, setSelected] = useState<Invoice>(); const [message, setMessage] = useState("Loading invoices…");
  const [busy, setBusy] = useState(false);
  const [review, setReview] = useState(false);
  const pending = useRef(false);
  const reviewRequired = useRef(false);
  const alive = useRef(true);
  const [ready, setReady] = useState(false);
  const [loading, setLoading] = useState(true);
  const readReady = useRef(false);
  const reading = useRef(false);
  const readVersion = useRef(0);
  async function saveFinancial(action: () => Promise<Response>, reset: () => void, success: string, failure: string) {
    if (!alive.current || pending.current || reviewRequired.current || !readReady.current) return;
    pending.current = true; setBusy(true);
    let confirmed = false;
    try {
      const response = await action();
      if (!alive.current) return;
      if (!response.ok) { if (response.status >= 500) throw Error(); setMessage(failure); return; }
      confirmed = true; reset(); await load();
      if (alive.current) setMessage(success);
    } catch {
      if (alive.current) {
        reviewRequired.current = true; setReview(true);
        setMessage(confirmed ? `${success} The list could not be refreshed. Reload financial records before recording again.`
          : "Save could not be confirmed. Review the latest financial records before submitting again.");
      }
    } finally { pending.current = false; if (alive.current) setBusy(false); }
  }
  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id || !alive.current) return;
    const version = ++readVersion.current;
    readReady.current = false; setReady(false); setLoading(true); setSelected(undefined);
    try {
      const responses = await Promise.all([academyApi(`/api/academies/${id}/invoices/student-options`, { cache: "no-store" }), academyApi(`/api/academies/${id}/fee-plans`, { cache: "no-store" }), academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/invoices/document-settings`, { cache: "no-store" })]);
      const failure = responses.find(response => !response.ok);
      if (failure) throw new InvoiceReadError(failure.status);
      const [studentData, planData, invoiceData, settingData]: [Student[], FeePlan[], Invoice[], Settings] = await Promise.all([responses[0].json(), responses[1].json(), responses[2].json(), responses[3].json()]);
      if (!Array.isArray(studentData) || !studentData.every(row => row && typeof row.id === "string" && typeof row.firstName === "string" && typeof row.lastName === "string")
        || !Array.isArray(planData) || !planData.every(row => row && typeof row.id === "string" && typeof row.name === "string")
        || !Array.isArray(invoiceData) || !invoiceData.every(row => row && typeof row.id === "string" && typeof row.invoiceNumber === "string" && typeof row.studentId === "string" && typeof row.status === "string")
        || !settingData || typeof settingData !== "object" || Array.isArray(settingData)) throw Error("Invalid invoice response");
      if (!alive.current || readVersion.current !== version) return;
      setStudents(studentData); setPlans(planData); setInvoices(invoiceData); setSettings(settingData);
      if (!studentId && studentData[0]) setStudentId(studentData[0].id);
      readReady.current = true; setReady(true); setMessage("");
    } finally { if (alive.current && readVersion.current === version) setLoading(false); }
  }
  async function readInvoices(initialResponse?: Response) {
    if (!alive.current || reading.current || pending.current || reviewRequired.current) return;
    reading.current = true;
    try {
      const response = initialResponse ?? await academyApi("/api/academies", { cache: "no-store" });
      if (!response.ok) throw new InvoiceReadError(response.status);
      const academies: Academy[] = await response.json();
      if (!Array.isArray(academies) || !academies.every(row => row && typeof row.id === "string" && typeof row.name === "string")) throw Error("Invalid academy response");
      if (!alive.current) return;
      if (!academies[0]) { setMessage("Create your academy first."); return; }
      const selectedAcademy = academies[0]; setAcademy(selectedAcademy);
      await load(selectedAcademy.id);
    } catch (error) { if (alive.current) setMessage(readMessage(error)); }
    finally { reading.current = false; if (alive.current) setLoading(false); }
  }
  function reloadInvoices() {
    if (!alive.current || reading.current || pending.current || reviewRequired.current) return;
    readReady.current = false; setReady(false); setLoading(true); setSelected(undefined); setMessage("Loading invoices…");
    return readInvoices();
  }
  useEffect(() => {
    void academyApi("/api/academies", { cache: "no-store" }).then(response => readInvoices(response)).catch(error => {
      if (alive.current) { setMessage(readMessage(error)); setLoading(false); }
    });
  }, []);
  useEffect(() => { alive.current = true; return () => { alive.current = false; readReady.current = false; }; }, []);

  function choosePlan(id: string) { setFeePlanId(id); const plan = plans.find((item) => item.id === id); if (plan) setAmount(plan.amount.toString()); }
  async function createInvoice(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy || !studentId) return;
    await saveFinancial(() => academyApi(`/api/academies/${academy.id}/invoices`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, feePlanId: feePlanId || null, amount: amount ? Number(amount) : null, dueDate: dueDate || null }) }),
      () => { setFeePlanId(""); setAmount(""); setDueDate(""); }, "Invoice issued.", "The invoice could not be saved. Select a fee plan or enter a positive amount.");
  }
  const student = (id: string) => students.find((item) => item.id === id); const studentName = (id: string) => { const item = student(id); return item ? `${item.firstName} ${item.lastName}` : "Unknown student"; };
  const invoiceStats = useMemo(() => ({ issued: invoices.length, paid: invoices.filter((invoice) => invoice.status.toLowerCase() === "paid").length, open: invoices.filter((invoice) => !["paid", "cancelled"].includes(invoice.status.toLowerCase())).length, value: currencyTotal(invoices.map(invoice => ({ amount: invoice.totalAmount, currency: invoice.currency }))) }), [invoices]);
  return <main className="enterprise-settings finance-module invoices-standard"><style>{`@media screen and (max-width: 640px) {
      .invoices-standard .invoice-paper { min-width: 0; max-width: 100%; }
      .invoices-standard .invoice-paper .invoice-paper-table { min-width: 0; width: 100%; table-layout: fixed; margin-inline: 0; }
      .invoices-standard .invoice-paper .invoice-paper-table :is(th, td) { white-space: normal; overflow-wrap: anywhere; padding-inline: 4px; }
      .invoices-standard .invoice-paper :is(p, h3, b, span) { overflow-wrap: anywhere; }
      .invoices-standard .invoice-paper .invoice-paper-total { min-width: 0; gap: 12px; flex-wrap: wrap; }
      .invoices-standard :is(.invoice-theme-modern, .invoice-theme-minimal) :is(.invoice-paper-header, .invoice-paper-bill, .invoice-paper-total, .invoice-paper-footer) { margin-inline: 0; }
      .invoices-standard .invoice-theme-modern .invoice-paper-header { padding: 16px; }
    }
    @media print {
      html:has(.invoices-standard .invoice-paper), body:has(.invoices-standard .invoice-paper) { display: block !important; height: auto !important; min-height: 0 !important; overflow: visible !important; margin: 0 !important; background: #fff !important; }
      body:has(.invoices-standard .invoice-paper) > :not(:has(.invoice-paper)) { display: none !important; }
      .enterprise-app-shell:has(.invoices-standard .invoice-paper), .enterprise-workspace:has(.invoices-standard .invoice-paper), .invoices-standard:has(.invoice-paper) { display: block !important; position: static !important; width: 100% !important; height: auto !important; min-height: 0 !important; max-height: none !important; overflow: visible !important; margin: 0 !important; padding: 0 !important; border: 0 !important; background: #fff !important; }
      .enterprise-app-shell:has(.invoices-standard .invoice-paper) > :not(:has(.invoice-paper)), .enterprise-workspace:has(.invoices-standard .invoice-paper) > :not(:has(.invoice-paper)), .invoices-standard > :not(.invoice-paper) { display: none !important; }
      .invoices-standard .invoice-paper { position: static !important; width: 100%; max-width: none; margin: 0 !important; box-shadow: none; }
    }`}</style><header className="enterprise-page-header"><p>Finance / invoices</p><h2>Invoices</h2><span>Issue fees, preview the academy-branded invoice, then print or save it as a PDF.</span></header>{message && <p className="enterprise-settings-empty invoices-notice" role={!ready && !loading || review ? "alert" : "status"}>{message}</p>}
    {!ready && !loading && !review && <button type="button" className="enterprise-action-button" onClick={() => void reloadInvoices()}>Try again</button>}{review && <button type="button" className="enterprise-action-button" onClick={() => window.location.reload()}>Reload financial records</button>}{ready && !review && <section className="invoices-kpis"><article><span>Issued invoices</span><strong>{invoiceStats.issued}</strong></article><article><span>Paid invoices</span><strong>{invoiceStats.paid}</strong></article><article><span>Open invoices</span><strong>{invoiceStats.open}</strong></article><article><span>Total billed</span><strong>{invoiceStats.value}</strong></article></section>}<section className="invoices-layout"><form onSubmit={createInvoice} className="surface-panel invoices-create"><fieldset disabled={busy || review || !ready} className="contents"><header><div><p>Create</p><h3>New invoice</h3></div></header><div className="invoices-fields"><label><span>Student</span><select value={studentId} onChange={(event) => setStudentId(event.target.value)} className="field" required><option value="">Select student</option>{students.map((item) => <option key={item.id} value={item.id}>{item.firstName} {item.lastName}</option>)}</select></label><label><span>Fee plan</span><select value={feePlanId} onChange={(event) => choosePlan(event.target.value)} className="field"><option value="">Custom amount</option>{plans.map((plan) => <option key={plan.id} value={plan.id}>{plan.name} · {money(plan.amount, plan.currency)}</option>)}</select></label><label><span>Invoice amount</span><input type="number" min="1" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} placeholder="Amount in INR" className="field"/></label><label><span>Due date</span><input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} className="field"/></label><button disabled={!academy || !students.length} className="enterprise-action-button">Issue invoice</button></div></fieldset></form>{ready && !review && <section className="surface-panel invoices-directory"><header><div><p>Register</p><h3>Issued invoices</h3></div><span>{invoices.length} total</span></header>{invoices.length === 0 ? <p className="invoices-empty">No invoices issued.</p> : <div className="invoices-table-wrap"><table><thead><tr><th>Student</th><th>Invoice</th><th>Due date</th><th>Amount</th><th>Status</th><th>Action</th></tr></thead><tbody>{invoices.map((invoice) => <tr key={invoice.id}><td>{studentName(invoice.studentId)}</td><td>{invoice.invoiceNumber}</td><td>{invoice.dueDate}</td><td>{money(invoice.totalAmount, invoice.currency)}</td><td><span className="invoice-status">{invoice.status}</span></td><td><button type="button" onClick={() => setSelected(invoice)} className="enterprise-action-button enterprise-action-button-secondary">Preview / Print</button></td></tr>)}</tbody></table></div>}</section>}</section>{ready && !review && selected && <section className={`invoice-paper invoice-theme-${(settings?.invoiceTemplateKey || "Classic").toLowerCase()} mt-5`}><div className="invoice-paper-actions"><button type="button" onClick={() => window.print()} className="enterprise-action-button">Print / Save PDF</button><button type="button" onClick={() => setSelected(undefined)} className="enterprise-action-button enterprise-action-button-secondary">Close preview</button></div><div className="invoice-paper-header"><div>{settings?.invoiceLogoUrl ? <img src={settings.invoiceLogoUrl} alt="Academy logo" className="invoice-paper-logo"/> : <div className="invoice-paper-mark">₹</div>}<div><p>Tax invoice</p><h3>{academy?.legalName || academy?.name}</h3>{settings?.taxRegistrationNumber && <span>{settings.taxLabel}: {settings.taxRegistrationNumber}</span>}</div></div><div className="text-right"><b>{selected.invoiceNumber}</b><span>Issued: {selected.issuedDate}</span><span>Due: {selected.dueDate}</span></div></div><div className="invoice-paper-bill"><div><p>Bill to</p><b>{studentName(selected.studentId)}</b><span>{student(selected.studentId)?.email || student(selected.studentId)?.phone || "Student account"}</span></div><div><p>Payment status</p><b>{selected.status}</b><span>Currency: {selected.currency}</span></div></div><table className="invoice-paper-table"><thead><tr><th>Description</th><th>Due date</th><th className="text-right">Amount</th></tr></thead><tbody><tr><td>{plans.find((plan) => plan.id === selected.feePlanId)?.name || "Academy fee"}</td><td>{selected.dueDate}</td><td className="text-right">{money(selected.totalAmount, selected.currency)}</td></tr></tbody></table><div className="invoice-paper-total"><span>Approved adjustments</span><b>{money(selected.adjustedAmount, selected.currency)}</b></div><div className="invoice-paper-total"><span>Collected</span><b>{money(selected.paidAmount, selected.currency)}</b></div>{cancelled(selected) && <div className="invoice-paper-total"><span>Balance before cancellation</span><b>{invoiceBalance(selected)}</b></div>}<div className="invoice-paper-total"><span>Amount due</span><b>{cancelled(selected) ? money(0, selected.currency) : invoiceBalance(selected)}</b></div><div className="invoice-paper-footer"><div><p>Thank you for learning with {academy?.name}.</p><span>Generated by AcademyDesk</span></div><div className="invoice-paper-signature">{settings?.invoiceSignatureUrl && <img src={settings.invoiceSignatureUrl} alt="Authorised signature"/>}<b>{settings?.invoiceAuthorityName || "Authorised signatory"}</b><span>{settings?.invoiceAuthorityTitle || "Finance"}</span></div></div></section>}</main>;
}
