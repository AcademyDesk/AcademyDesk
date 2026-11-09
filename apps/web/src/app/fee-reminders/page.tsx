"use client";
import { useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { reportAmount, reportMoney } from "@/lib/reports-finance";
type A = { id: string };
type S = { id: string; firstName: string; lastName: string };
type I = { id: string; invoiceNumber: string; studentId: string; balance: number; status: string; currency: string; dueDate: string };
type N = { id: string; recipientId?: string; title: string; channel: string; status: string; sentAtUtc?: string };
type Recovery = "" | "unconfirmed" | "confirmed" | "reviewed";
const cancelled = (invoice: I) => invoice.status.toLowerCase() === "cancelled";
const hasMissingMoney = (invoices: I[]) => invoices.some(invoice => !cancelled(invoice) && (!Number.isFinite(invoice.balance) || invoice.balance > 0 && (typeof invoice.currency !== "string" || !invoice.currency.trim())));
class ReminderReadError extends Error { constructor(readonly status: number) { super("Fee reminders could not be loaded."); } }
const guidance = (status: number) => status === 401 ? " Sign in again." : status === 403 ? " This account does not have access to the required records." : status === 429 ? " Please wait before trying again." : " Try again to reload the records.";

export default function FeeRemindersPage() {
  const [a, setA] = useState<A>(), [s, setS] = useState<S[]>([]), [i, setI] = useState<I[]>([]), [n, setN] = useState<N[]>([]);
  const [m, setM] = useState("Loading fee reminders…"), [busy, setBusy] = useState(""), [filter, setFilter] = useState("All");
  const [ready, setReady] = useState(false), [loading, setLoading] = useState(true), [recovery, setRecovery] = useState<Recovery>(""), [problem, setProblem] = useState(false);
  const [intent, setIntent] = useState(0);
  const alive = useRef(true), readReady = useRef(false), reading = useRef(false), pending = useRef(false);
  const recoveryState = useRef<Recovery>(""), intentVersion = useRef(0), readVersion = useRef(0);
  const eligible = useRef(new Set<string>()), unavailableDebt = useRef(false);
  const due = i.filter(invoice => !cancelled(invoice) && Number.isFinite(invoice.balance) && invoice.balance > 0);
  const missingMoney = hasMissingMoney(i);
  const totals = missingMoney ? "Currency details unavailable" : reportMoney(due.map(invoice => ({ amount: invoice.balance, currency: invoice.currency })));
  const shown = filter === "All" ? due : due.filter(invoice => filter === "Overdue" ? new Date(invoice.dueDate) < new Date() : new Date(invoice.dueDate) >= new Date());
  const canQueue = ready && !loading && !busy && !recovery && !missingMoney, visibleVersion = intent;
  function invalidateIntent() { ++intentVersion.current; setIntent(intentVersion.current); }
  function message(value: string, failed = false) { if (alive.current) { setM(value); setProblem(failed); } }
  function recoverAs(value: Recovery) { recoveryState.current = value; if (alive.current) setRecovery(value); }
  async function load(id: string) {
    if (!alive.current) return;
    const version = ++readVersion.current;
    readReady.current = false; setReady(false); setLoading(true);
    try {
      const responses = await Promise.all([academyApi(`/api/academies/${id}/invoices/student-options`, { cache: "no-store" }), academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/fee-reminders/history`, { cache: "no-store" })]);
      const failure = responses.find(response => !response.ok);
      if (failure) throw new ReminderReadError(failure.status);
      const [students, invoices, notices]: [S[], I[], N[]] = await Promise.all([responses[0].json(), responses[1].json(), responses[2].json()]);
      if (!Array.isArray(students) || !students.every(row => row && typeof row.id === "string" && typeof row.firstName === "string" && typeof row.lastName === "string")
        || !Array.isArray(invoices) || !invoices.every(row => row && typeof row.id === "string" && typeof row.invoiceNumber === "string" && typeof row.studentId === "string" && typeof row.status === "string" && typeof row.dueDate === "string")
        || !Array.isArray(notices) || !notices.every(row => row && typeof row.id === "string" && typeof row.title === "string" && typeof row.channel === "string" && typeof row.status === "string")) throw Error("Invalid reminder response");
      if (!alive.current || readVersion.current !== version) return;
      eligible.current = new Set(invoices.filter(invoice => !cancelled(invoice) && Number.isFinite(invoice.balance) && invoice.balance > 0).map(invoice => invoice.id));
      unavailableDebt.current = hasMissingMoney(invoices);
      setS(students); setI(invoices); setN(notices.filter(notice => notice.title === "Fee payment reminder"));
      invalidateIntent(); readReady.current = true; setReady(true);
    } finally { if (alive.current && readVersion.current === version) setLoading(false); }
  }
  async function readRecords(initialResponse?: Response) {
    if (!alive.current || reading.current || pending.current) return;
    reading.current = true;
    try {
      const response = initialResponse ?? await academyApi("/api/academies", { cache: "no-store" });
      if (!response.ok) throw new ReminderReadError(response.status);
      const academies: A[] = await response.json();
      if (!Array.isArray(academies) || !academies[0] || !academies.every(row => row && typeof row.id === "string")) throw Error("No academy context");
      if (!alive.current) return;
      setA(academies[0]); await load(academies[0].id);
      if (!alive.current) return;
      if (recoveryState.current === "unconfirmed" || recoveryState.current === "reviewed") {
        recoverAs("reviewed"); message("Records refreshed. The earlier queue result is still unconfirmed. Review reminder history, then start a new reminder if needed.", true);
      } else if (recoveryState.current === "confirmed") { recoverAs(""); message("Reminder queue was confirmed. Records refreshed."); }
      else message("");
    } catch (error) {
      const prefix = recoveryState.current === "confirmed" ? "The earlier reminder queue was confirmed. " : recoveryState.current ? "The earlier queue result remains unconfirmed. " : "";
      message(prefix + "Fee reminders could not be loaded." + guidance(error instanceof ReminderReadError ? error.status : 503), true);
    } finally { reading.current = false; if (alive.current) setLoading(false); }
  }
  function reloadRecords() {
    if (!alive.current || reading.current || pending.current) return;
    readReady.current = false; setReady(false); setLoading(true); message("Loading fee reminders…"); return readRecords();
  }
  useEffect(() => {
    void academyApi("/api/academies", { cache: "no-store" }).then(response => readRecords(response)).catch(() => {
      message("Fee reminders could not be loaded. Try again to reload the records.", true); if (alive.current) setLoading(false);
    });
  }, []);
  useEffect(() => { alive.current = true; return () => { alive.current = false; readReady.current = false; }; }, []);
  async function queue(id: string | undefined, version: number) {
    if (!alive.current || !a || !readReady.current || reading.current || pending.current || recoveryState.current || unavailableDebt.current || intentVersion.current !== version || !eligible.current.size || id && !eligible.current.has(id)) return;
    pending.current = true; invalidateIntent(); setBusy(id ?? "all");
    let confirmation = "";
    try {
      const response = await academyApi(`/api/academies/${a.id}/fee-reminders`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ invoiceId: id ?? null, channel: "InApp" }) });
      if (!alive.current) return;
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408) throw Error("Unconfirmed queue");
        if (response.status === 401 || response.status === 403) { readReady.current = false; setReady(false); }
        message("Reminder was not queued." + guidance(response.status), true); return;
      }
      const receipt = await response.json();
      if (!receipt || !Number.isInteger(receipt.queuedCount) || receipt.queuedCount < 0 || typeof receipt.message !== "string" || !receipt.message.trim()) throw Error("Invalid queue confirmation");
      if (!alive.current) return;
      confirmation = receipt.message; await load(a.id);
      if (alive.current) message(confirmation);
    } catch {
      if (alive.current) {
        readReady.current = false; setReady(false); recoverAs(confirmation ? "confirmed" : "unconfirmed");
        message(confirmation ? `${confirmation} The queue was confirmed, but reminder records could not be refreshed. Reload reminder records before queueing again.`
          : "Reminder queue could not be confirmed. Reload reminder records and review the history before starting a new reminder.", true);
      }
    } finally { pending.current = false; if (alive.current) setBusy(""); }
  }
  function startNewReminder() {
    if (!alive.current || !readReady.current || reading.current || pending.current || recoveryState.current !== "reviewed") return;
    invalidateIntent(); recoverAs(""); message("Ready to queue a new reminder.");
  }
  const name = (id: string) => { const student = s.find(student => student.id === id); return student ? `${student.firstName} ${student.lastName}` : "Unknown student"; };
  return <main className="enterprise-settings finance-module reminders-rebuilt">
    <header><span>₹</span><div><p>Billing &amp; collections</p><h1>Fee reminders</h1></div></header>
    {m && <p className="reminders-message" role={problem ? "alert" : "status"}>{m}</p>}
    {!ready && !loading && !recovery && <button className="enterprise-action-button" type="button" onClick={() => void reloadRecords()}>Try again</button>}
    {recovery && <button className="enterprise-action-button" type="button" disabled={loading || !!busy} onClick={() => void reloadRecords()}>Reload reminder records</button>}
    {recovery === "reviewed" && ready && <button className="enterprise-action-button" type="button" onClick={startNewReminder}>Start a new reminder</button>}
    {ready && !loading && <>
      <section className="reminders-kpis"><article><span>Open balances</span><b>{missingMoney ? "Unavailable" : due.length}</b></article><article><span>Outstanding</span><b>{totals}</b></article><article><span>Reminders queued</span><b>{n.length}</b></article></section>
      <section className="reminders-panel"><header><div><p>Collection queue</p><h2>Outstanding invoices</h2></div><button disabled={!canQueue || !due.length} onClick={() => void queue(undefined, visibleVersion)}>{busy === "all" ? "Queueing…" : "Queue all reminders"}</button></header>
        <div className="reminders-filters">{["All", "Overdue", "Upcoming"].map(value => <button key={value} onClick={() => setFilter(value)} data-active={filter === value}>{value}</button>)}</div>
        {shown.length ? <ul>{shown.map(invoice => <li key={invoice.id}><div><b>{name(invoice.studentId)}</b><small>{invoice.invoiceNumber} · Due {invoice.dueDate}</small></div><strong>{reportAmount(invoice.balance, invoice.currency)}</strong><button disabled={!canQueue} onClick={() => void queue(invoice.id, visibleVersion)}>{busy === invoice.id ? "Queueing…" : "Send reminder"}</button></li>)}</ul>
          : <p className="reminders-empty">{missingMoney ? "Some invoice balances could not be verified. Reload records before queueing reminders." : "No invoices match this filter."}</p>}
      </section>
      <section className="reminders-history"><h2>Recent reminders</h2>{n.length ? <ul>{n.slice(0, 6).map(notice => <li key={notice.id}><b>{notice.channel}</b><span>{notice.status}</span></li>)}</ul> : <p>No reminders sent yet.</p>}</section>
    </>}
  </main>;
}
