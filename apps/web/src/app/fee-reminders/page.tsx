"use client";
import { useCallback, useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import styles from "@/components/design-system/finance-workspace.module.css";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Invoice = { id: string; invoiceNumber: string; studentId: string; balance: number; currency: string; dueDate: string };
type Notification = { id: string; title: string; channel: string; status: string };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency }).format(amount);

export default function FeeRemindersPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [message, setMessage] = useState("Loading fee reminders…");
  const [error, setError] = useState(false);
  const [busy, setBusy] = useState("");
  const [filter, setFilter] = useState("All");
  const [ready, setReady] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const guard = useRef({ pending: false, review: false, ready: false });
  const load = useCallback(async (id: string) => {
    // Preserve the existing student-management permission requirement.
    const responses = await Promise.all(["students", "invoices", "notifications"].map(resource => academyApi(`/api/academies/${id}/${resource}`, { cache: "no-store" })));
    if (responses.some(response => !response.ok)) throw Error();
    const [studentRows, invoiceRows, notificationRows] = await Promise.all(responses.map(response => response.json()));
    if (![studentRows, invoiceRows, notificationRows].every(Array.isArray) || invoiceRows.some((invoice: Invoice) => !Number.isFinite(invoice.balance) || !invoice.currency)) throw Error();
    setStudents(studentRows); setInvoices(invoiceRows); setNotifications(notificationRows.filter((row: Notification) => row.title === "Fee payment reminder"));
    guard.current.ready = true; setReady(true);
  }, []);
  useEffect(() => {
    let active = true;
    void (async () => { try {
      const response = await academyApi("/api/academies", { cache: "no-store" });
      if (!response.ok) throw Error(); const rows: Academy[] = await response.json();
      if (!rows[0]) { if (active) setMessage("Create your academy first."); return; }
      if (!active) return; setAcademy(rows[0]); await load(rows[0].id);
      if (active) setMessage("");
    } catch { if (active) { setError(true); setMessage("Fee reminders could not be loaded."); } } })();
    return () => { active = false; };
  }, [load]);
  const requireReview = () => { guard.current.review = true; setNeedsReview(true); };
  async function refreshRegister() {
    if (!academy || guard.current.pending) return;
    guard.current.pending = true; setBusy("refresh");
    try { await load(academy.id); guard.current.review = false; setNeedsReview(false); setError(false); setMessage("Reminder register refreshed. Check the recent reminders before queueing again."); }
    catch { requireReview(); setError(true); setMessage("Reminder register could not be refreshed. Do not repeat an unconfirmed request."); }
    finally { guard.current.pending = false; setBusy(""); }
  }
  // Use the invoice API ledger, never reconstruct gross minus payments.
  const due = invoices.filter(invoice => invoice.balance > 0);
  const shown = filter === "All" ? due : due.filter(invoice => filter === "Overdue" ? new Date(invoice.dueDate) < new Date() : new Date(invoice.dueDate) >= new Date());
  const totals = new Map<string, number>();
  for (const invoice of due) totals.set(invoice.currency, (totals.get(invoice.currency) ?? 0) + invoice.balance);
  const outstanding = totals.size ? [...totals].map(([currency, value]) => money(value, currency)).join(" · ") : money(0);
  const studentName = (id: string) => { const student = students.find(row => row.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };
  const blocked = !ready || !!busy || needsReview;
  async function queue(id?: string) {
    if (!academy || !guard.current.ready || guard.current.pending || guard.current.review || !due.length || id && !due.some(invoice => invoice.id === id)) return;
    guard.current.pending = true; setBusy(id ?? "all"); setError(false); setMessage("Queueing reminders…");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/fee-reminders`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ invoiceId: id ?? null, channel: "InApp" }) });
      const result = await response.json().catch(() => null);
      if (!response.ok) {
        setError(true);
        if (response.status >= 500 || response.status === 408) { requireReview(); setMessage("Reminder request could not be confirmed. Refresh and check recent reminders before retrying."); }
        else setMessage(typeof result?.message === "string" ? result.message : "Reminders could not be queued. Check your access and try again.");
        return;
      }
      if (!Number.isInteger(result?.queuedCount) || result.queuedCount < 0) { requireReview(); setError(true); setMessage("Reminder request was accepted, but its result could not be confirmed. Refresh and check recent reminders before retrying."); return; }
      setMessage(typeof result.message === "string" ? result.message : `Queued ${result.queuedCount} fee reminder(s).`);
      try { await load(academy.id); }
      catch { requireReview(); setError(true); setMessage(`Queued ${result.queuedCount} fee reminder(s). The register could not be refreshed; do not repeat this request. Refresh the register to continue.`); }
    } catch { requireReview(); setError(true); setMessage("Reminder request could not be confirmed. Refresh and check recent reminders before retrying."); }
    finally { guard.current.pending = false; setBusy(""); }
  }
  return <main className={`enterprise-settings finance-module reminders-rebuilt ${styles.page}`}>
    <header><span aria-hidden="true">₹</span><div><p>Billing &amp; collections</p><h1>Fee reminders</h1></div></header>
    {message && <p className="reminders-message" role={error ? "alert" : "status"}>{message}</p>}
    {academy && <button type="button" disabled={!!busy} onClick={refreshRegister} className="enterprise-action-button">Refresh reminder register</button>}
    <section className="reminders-kpis" aria-label="Reminder summary"><article><span>Open balances</span><b>{due.length}</b></article><article><span>Outstanding</span><b>{outstanding}</b></article><article><span>Reminder records</span><b>{notifications.length}</b></article></section>
    <section className="reminders-panel" aria-busy={!!busy}><header><div><p>Collection queue</p><h2>Outstanding invoices</h2></div><button type="button" disabled={blocked || !due.length} onClick={() => void queue()}>{busy === "all" ? "Queueing…" : "Queue due reminders"}</button></header>
      <p className={styles.queueHint}>Bulk queue uses the server’s due-date rules. Queueing does not confirm delivery.</p>
      <div className="reminders-filters" role="group" aria-label="Invoice due-date filter">{["All", "Overdue", "Upcoming"].map(value => <button type="button" key={value} onClick={() => setFilter(value)} data-active={filter === value} aria-pressed={filter === value}>{value}</button>)}</div>
      {!ready ? <p className="reminders-empty">Reminder register unavailable until loading succeeds.</p> : shown.length ? <ul>{shown.map(invoice => <li key={invoice.id}><div><b>{studentName(invoice.studentId)}</b><small>{invoice.invoiceNumber} · Due {invoice.dueDate}</small></div><strong>{money(invoice.balance, invoice.currency)}</strong><button type="button" disabled={blocked} onClick={() => void queue(invoice.id)}>{busy === invoice.id ? "Queueing…" : "Queue reminder"}</button></li>)}</ul> : <p className="reminders-empty">No invoices match this filter.</p>}
    </section>
    <section className="reminders-history"><h2>Recent reminders</h2>{!ready ? <p>Reminder history unavailable.</p> : notifications.length ? <ul>{notifications.slice(0, 6).map(notification => <li key={notification.id}><b>{notification.channel}</b><span>{notification.status}</span></li>)}</ul> : <p>No reminder records yet.</p>}</section>
  </main>;
}
