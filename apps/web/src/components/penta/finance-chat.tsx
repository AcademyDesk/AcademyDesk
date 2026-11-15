"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { apiUrl, isPortalSignOutEvent, portalAccessToken, portalSessionChangedEvent, portalSessionEpoch, portalSignOutEvent } from "@/lib/api";
import { readPentaJson } from "@/lib/penta-onboarding-channel";
import { academicSearch, type AcademicSearch, type AcademicChoice } from "@/lib/penta-enrollment-assistant";
import { workspacePrompt } from "@/lib/penta-workspace-prompt";
import { readSchedulingPointer, schedulingReady } from "@/lib/penta-class-scheduling";
import type { EnrollmentTransport } from "@/lib/penta-enrollment-creation";
import { SchedulingConversation } from "./class-scheduling";
import type { FinanceDraftChannel } from "@/lib/penta-finance-draft";
import type { InvoiceChannel } from "@/lib/penta-invoice-issuance";
import InvoiceAdjustment from "./invoice-adjustment";
import InvoiceIssuance from "./invoice-issuance";
import PaymentCollection from "./payment-collection";
import styles from "./role-penta-entry.module.css";

const manualRoutes = {
  overview: "/finance", collections: "/finance-governance", invoice: "/invoices",
  reconciliation: "/finance-reconciliation", expenses: "/expenses", payroll: "/payroll",
} as const;

type FinanceReply = {
  kind: keyof typeof manualRoutes;
  title: string;
  columns: string[];
  rows: { values: string[]; invoiceId: string | null }[];
  hasMore: boolean;
  asOfUtc: string;
};
const object = (value: unknown): value is Record<string, unknown> =>
  value !== null && typeof value === "object" && !Array.isArray(value);
const label = (value: unknown): value is string => typeof value === "string" && value.length > 0 && value.length <= 120;
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isReply(value: unknown): value is FinanceReply {
  if (!object(value) || typeof value.kind !== "string" || !Object.hasOwn(manualRoutes, value.kind) ||
      !label(value.title) || typeof value.asOfUtc !== "string" || !Number.isFinite(Date.parse(value.asOfUtc)) ||
      typeof value.hasMore !== "boolean" || !Array.isArray(value.columns) || !value.columns.length ||
      value.columns.length > 8 || !value.columns.every(label) || !Array.isArray(value.rows) || value.rows.length > 20) return false;
  const columnCount = value.columns.length;
  const hasInvoice = value.kind === "invoice" || value.kind === "collections";
  return value.rows.every(row => object(row) && Array.isArray(row.values) && row.values.length === columnCount &&
    row.values.every(label) && (hasInvoice ? typeof row.invoiceId === "string" && guid.test(row.invoiceId) : row.invoiceId === null));
}

type Channel = {
  call(text?: string): Promise<Response>;
  current(): boolean;
  signal: AbortSignal;
  finance: boolean;
  financeControls: boolean;
  lookup: ((entity: AcademicSearch["kind"], query: string) => Promise<Response>) | null;
  schedule: EnrollmentTransport | null;
  payment: FinanceDraftChannel | null;
  issuance: InvoiceChannel | null;
  adjustment: FinanceDraftChannel | null;
};
type Turn = { prompt: string; reply?: FinanceReply; academic?: AcademicSearch; selected?: AcademicChoice; notice?: string };
type SchedulingTurn = Extract<ReturnType<typeof workspacePrompt>, { kind: "schedule" }>;

export default function FinanceChat({ academyId, fallback }: { academyId: string; fallback: ReactNode }) {
  const [available, setAvailable] = useState<Channel | null>(null);
  const [paymentInvoice, setPaymentInvoice] = useState<string | null>(null);
  const [invoiceOpen, setInvoiceOpen] = useState(false);
  const [adjustmentInvoice, setAdjustmentInvoice] = useState<{ id: string; title: string } | null>(null);
  const [prompt, setPrompt] = useState("");
  const [turn, setTurn] = useState<Turn | null>(null);
  const [pending, setPending] = useState(false);
  const active = useRef(false);
  const lookupContext = useRef<{ result: AcademicSearch; observed: number } | null>(null);
  const [scheduling, setScheduling] = useState<SchedulingTurn | null>(null);
  const [resumeSchedule, setResumeSchedule] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    const bearer = portalAccessToken("AcademyAdmin");
    const epoch = portalSessionEpoch("AcademyAdmin");
    const root = `${apiUrl}/api/academies/${academyId}/penta`;
    const current = () => !controller.signal.aborted && !!bearer &&
      portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === epoch;
    const request = (path: string, method: string, body?: unknown) => fetch(root + path, {
      method,
      credentials: "omit", cache: "no-store", redirect: "error", referrerPolicy: "no-referrer",
      signal: controller.signal,
      headers: { Authorization: `Bearer ${bearer}`, Accept: "application/json",
        ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    const call = (text?: string) => request("/chat/finance", text === undefined ? "GET" : "POST",
      text === undefined ? undefined : { text });

    function clear() {
      controller.abort();
      setAvailable(null);
      setPaymentInvoice(null);
      setInvoiceOpen(false);
      setAdjustmentInvoice(null);
      lookupContext.current = null;
      setScheduling(null);
      setResumeSchedule(false);
      setTurn(null);
      setPrompt("");
      setPending(false);
    }
    function recheck() { if (!current()) clear(); }
    function sessionChanged(event: Event) {
      if ((event as CustomEvent).detail === "AcademyAdmin") clear();
    }
    function storage(event: StorageEvent) {
      if (isPortalSignOutEvent(event, "AcademyAdmin")) clear();
      else recheck();
    }

    window.addEventListener(portalSessionChangedEvent, sessionChanged);
    window.addEventListener(portalSignOutEvent, sessionChanged);
    window.addEventListener("storage", storage);
    window.addEventListener("focus", recheck);
    if (current()) {
      void (async () => {
        try {
          async function readiness(path: string): Promise<unknown> {
            const response = await request(path, "GET");
            if (!current() || response.status !== 200 || !response.headers.get("cache-control")?.includes("no-store")) {
              await response.body?.cancel();
              return null;
            }
            return readPentaJson(response, controller.signal);
          }
          const [finance, academics, scheduling] = await Promise.allSettled([
            readiness("/chat/finance"), readiness("/lookup-readiness"), readiness("/class-scheduling/browser/readiness"),
          ]);
          if (!current()) return;
          const value = finance.status === "fulfilled" && object(finance.value) ? finance.value : {};
          const financeReady = typeof value.financeControls === "boolean";
          const lookupReady = academics.status === "fulfilled" && object(academics.value) &&
            academics.value.protocol === "academy-lookup.v1" && academics.value.academyId === academyId;
          const scheduleReady = scheduling.status === "fulfilled" && schedulingReady(scheduling.value);
          if (!financeReady && !lookupReady && !scheduleReady) return;
          if (scheduleReady) {
            try { setResumeSchedule(Boolean(readSchedulingPointer(window.sessionStorage, academyId, epoch))); }
            catch { /* A new request still checks whether recovery storage is usable. */ }
          }
          const scope = { epoch, current, signal: controller.signal };
          setAvailable({ call, current, signal: controller.signal, finance: financeReady, financeControls: value.financeControls === true,
            lookup: lookupReady ? (entity, query) => request("/" + entity + "-search?q=" + encodeURIComponent(query), "GET") : null,
            schedule: scheduleReady ? { ...scope, academyId, invalidate: clear,
              send: (path, method, body) => request("/class-scheduling/browser" + path, method, body),
            } : null,
            payment: value.paymentCollection === true ? { ...scope,
              send: (path, method, body) => request("/payment-collection/conversations" + path, method, body),
            } : null,
            issuance: value.invoiceIssuance === true ? { ...scope,
              send: (path, method, body) => request("/invoice-issuance/conversations" + path, method, body),
              options: (kind, query) => request("/invoice-issuance/options", "POST", { kind, query }),
            } : null,
            adjustment: value.financeControls === true && value.invoiceAdjustment === true ? { ...scope,
              send: (path, method, body) => request("/invoice-adjustments/conversations" + path, method, body),
            } : null,
          });
        } catch { /* The Manual link remains available. */ }
      })();
    }
    return () => {
      controller.abort();
      window.removeEventListener(portalSessionChangedEvent, sessionChanged);
      window.removeEventListener(portalSignOutEvent, sessionChanged);
      window.removeEventListener("storage", storage);
      window.removeEventListener("focus", recheck);
    };
  }, [academyId]);

  async function send(text: string, displayText = text) {
    const source = available;
    const question = text.trim();
    if (active.current || !source?.current() || !question) return;
    active.current = true;
    setPending(true);
    setPrompt("");
    setTurn({ prompt: displayText });
    try {
      const remembered = lookupContext.current;
      const context = remembered && Date.now() - remembered.observed < 300_000 ? remembered.result : null;
      const command = workspacePrompt(question, context);
      if (command.kind === "clarify") {
        setTurn({ prompt: displayText, notice: command.message });
        return;
      }
      if (command.kind === "select") {
        const selected = context?.rows[command.index];
        if (selected) setTurn({ prompt: displayText, selected });
        return;
      }
      lookupContext.current = null;
      if (command.kind === "schedule") {
        if (!source.schedule) setTurn({ prompt: displayText, notice: "Class scheduling is unavailable here. Open Manual Sessions to continue." });
        else setScheduling(command);
        return;
      }
      if (command.kind === "lookup") {
        if (!source.lookup) { setTurn({ prompt: displayText, notice: "Academy lookup is unavailable in this session." }); return; }
        const response = await source.lookup(command.entity, command.query);
        if (!source.current()) return;
        if (response.status !== 200 || !response.headers.get("cache-control")?.includes("no-store")) {
          await response.body?.cancel();
          if (source.current()) setTurn({ prompt: displayText, notice: "PENTA could not verify this lookup. Try Manual to continue." });
          return;
        }
        const value = await readPentaJson(response, source.signal);
        if (!source.current()) return;
        const result = academicSearch(value, command.entity, academyId);
        lookupContext.current = { result, observed: Date.now() };
        setTurn({ prompt: displayText, academic: result });
        return;
      }
      if (!source.finance) {
        setTurn({ prompt: displayText, notice: "That request is unavailable here. Try a supported prompt or use Manual." });
        return;
      }
      const response = await source.call(question);
      if (!source.current()) return;
      if (response.status === 200) {
        const value = await readPentaJson(response, source.signal);
        if (!source.current()) return;
        if (!isReply(value) || !response.headers.get("cache-control")?.includes("no-store"))
          throw new Error("Invalid finance result.");
        setTurn({ prompt: displayText, reply: value });
        return;
      }
      await response.body?.cancel();
      if (!source.current()) return;
      const notice = response.status === 422 ? "Try one of the finance prompts below." :
        response.status === 401 || response.status === 403 ? "Your current access does not allow that finance read." :
        response.status === 404 && /^(show invoice|show reconciliation) /i.test(question) ?
          "That invoice is unavailable. Check it in Manual Finance." :
          "PENTA could not verify this read. Open Manual Finance to continue.";
      setTurn({ prompt: displayText, notice });
    } catch {
      if (source.current()) setTurn({ prompt: displayText, notice: "PENTA could not verify this request. Try the matching Manual workspace." });
    } finally {
      active.current = false;
      if (source.current()) setPending(false);
    }
  }

  if (!available) return fallback;
  const reply = turn?.reply;
  const prompts = available.finance ? [
    ...(available.financeControls ? ["Show finance overview", "Show overdue invoices"] : []),
    "Show recent expenses", "Show recent payroll",
  ] : [];
  return <section className={styles.panel} aria-labelledby="finance-chat-title" aria-busy={pending}>
    <h2 id="finance-chat-title">PENTA conversation</h2>
    <p>Ask for verified academy records or start a supported workflow. Guided preview.</p>
    {turn && <div className={styles.financeTurn}>
      <p><strong>You</strong> {turn.prompt}</p>
      {pending && <p role="status">Checking academy records…</p>}
      {turn.notice && <p role="status">{turn.notice}</p>}
      {turn.academic && <section aria-label="Matching academy records">
        <h3>{turn.academic.kind === "student" ? "Matching students" : "Matching batches"}</h3>
        {turn.academic.rows.length ? <ol>{turn.academic.rows.map(row => <li key={row.id}><Link href={row.path}>{row.label}</Link></li>)}</ol>
          : <p>No matching records. Try a different name.</p>}
        {turn.academic.rows.length > 0 && <p>Say “Open the first result” or refine the search with “Try” and a different name.</p>}
        {turn.academic.hasMore && <p>More matches exist. Use a more specific name.</p>}
        <small>As of <time dateTime={turn.academic.asOfUtc}>{new Date(turn.academic.asOfUtc).toLocaleString()}</time></small>
      </section>}
      {turn.selected && <p>Selected record · <Link href={turn.selected.path}>{turn.selected.label}</Link></p>}
      {reply && <section aria-label={reply.title}>
        <h3>{reply.title}</h3>
        {reply.rows.length === 0 ? <p>No records to show.</p> :
          <div className={styles.financeTable} tabIndex={0} role="region" aria-label={reply.title + " results"}>
            <table aria-label={reply.title}>
              <thead><tr>{reply.columns.map(column => <th scope="col" key={column}>{column}</th>)}
                {(reply.kind === "invoice" || reply.kind === "collections") && <th scope="col">Open in chat</th>}</tr></thead>
              <tbody>{reply.rows.map((row, index) => <tr key={row.invoiceId ?? index}>
                {row.values.map((value, column) => <td key={column}>{value}</td>)}
                {row.invoiceId && <td><div className={styles.financeActions}>
                  {reply.kind === "collections" && <button type="button" disabled={pending}
                    onClick={() => void send(`show invoice ${row.invoiceId}`, `Show invoice ${row.values[0]}`)}>Invoice</button>}
                  <button type="button" disabled={pending}
                    onClick={() => void send(`show reconciliation ${row.invoiceId}`, "Show reconciliation")}>Reconciliation</button>
                  {reply.kind === "invoice" && available.payment && <button type="button" disabled={pending || paymentInvoice !== null}
                    onClick={() => setPaymentInvoice(row.invoiceId)}>Record payment</button>}
                  {reply.kind === "invoice" && available.adjustment && <button type="button" disabled={pending || adjustmentInvoice !== null}
                    onClick={() => { if (row.invoiceId) setAdjustmentInvoice({ id: row.invoiceId, title: reply.title }); }}>Propose invoice adjustment</button>}
                </div></td>}
              </tr>)}</tbody>
            </table>
          </div>}
        <p><small>As of <time dateTime={reply.asOfUtc}>{new Date(reply.asOfUtc).toLocaleString()}</time>.
          {reply.hasMore && " More records are available in Manual."}</small></p>
      </section>}
    </div>}
    {available.schedule && (scheduling || resumeSchedule) && <div className={styles.financeTurn}>
      <h3>Class scheduling</h3>
      <SchedulingConversation transport={available.schedule} command={scheduling ?? undefined} embedded />
    </div>}
    {available.payment && <PaymentCollection academyId={academyId} invoiceId={paymentInvoice}
      channel={available.payment} onClose={() => setPaymentInvoice(null)} />}
    {available.adjustment && <InvoiceAdjustment academyId={academyId} invoice={adjustmentInvoice}
      channel={available.adjustment} onClose={() => setAdjustmentInvoice(null)} />}
    {available.issuance && <>
      <div className={styles.financeActions}>
        <button type="button" disabled={invoiceOpen} onClick={() => setInvoiceOpen(true)}>Prepare an invoice</button>
      </div>
      <InvoiceIssuance academyId={academyId} open={invoiceOpen} channel={available.issuance} onClose={() => setInvoiceOpen(false)} />
    </>}
    <form onSubmit={event => { event.preventDefault(); void send(prompt); }} className={styles.financeComposer}>
      <label htmlFor="finance-chat-prompt">Message PENTA</label>
      <div className={styles.financeActions}>
        <input id="finance-chat-prompt" value={prompt} maxLength={180} autoComplete="off"
          onChange={event => setPrompt(event.currentTarget.value)} placeholder={available.lookup ? "Find student Mira" : "Ask about your academy"} />
        <button type="submit" disabled={pending || !prompt.trim()}>Send</button>
      </div>
    </form>
    {available.lookup && <p><small>Try “Find student Mira” or “Look up batch Piano”.</small></p>}
    {available.schedule && <p><small>Try “Schedule a class for Piano”.</small></p>}
    {prompts.length > 0 && <div className={styles.financeActions} aria-label="Example finance prompts">
      {prompts.map(example => <button type="button" key={example} disabled={pending}
        onClick={() => void send(example)}>{example}</button>)}
    </div>}
    {available.finance && <p><Link href={reply ? manualRoutes[reply.kind] : "/finance"}>Open Manual Finance</Link></p>}
  </section>;
}
