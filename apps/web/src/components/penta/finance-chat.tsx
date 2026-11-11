"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { apiUrl, isPortalSignOutEvent, portalAccessToken, portalSessionChangedEvent, portalSessionEpoch, portalSignOutEvent } from "@/lib/api";
import { readPentaJson } from "@/lib/penta-onboarding-channel";
import type { PaymentChannel } from "@/lib/penta-payment-collection";
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

type Channel = PaymentChannel & {
  call(text?: string): Promise<Response>;
  financeControls: boolean;
  paymentCollection: boolean;
};
type Turn = { prompt: string; reply?: FinanceReply; notice?: string };

export default function FinanceChat({ academyId, fallback }: { academyId: string; fallback: ReactNode }) {
  const [available, setAvailable] = useState<Channel | null>(null);
  const [paymentInvoice, setPaymentInvoice] = useState<string | null>(null);
  const [prompt, setPrompt] = useState("");
  const [turn, setTurn] = useState<Turn | null>(null);
  const [pending, setPending] = useState(false);
  const active = useRef(false);

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
          const response = await call();
          if (!current() || response.status !== 200) return;
          const value = await readPentaJson(response, controller.signal);
          if (!current() || !object(value) || typeof value.financeControls !== "boolean") return;
          setAvailable({ call, current, signal: controller.signal, epoch, financeControls: value.financeControls,
            paymentCollection: value.paymentCollection === true,
            payment: (path, method, body) => request("/payment-collection/conversations" + path, method, body),
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
      if (source.current()) setTurn({ prompt: displayText, notice: "PENTA could not verify this read. Open Manual Finance to continue." });
    } finally {
      active.current = false;
      if (source.current()) setPending(false);
    }
  }

  if (!available) return fallback;
  const reply = turn?.reply;
  const prompts = [
    ...(available.financeControls ? ["Show finance overview", "Show overdue invoices"] : []),
    "Show recent expenses", "Show recent payroll",
  ];
  return <section className={styles.panel} aria-labelledby="finance-chat-title" aria-busy={pending}>
    <h2 id="finance-chat-title">Finance chat</h2>
    <p>Ask PENTA for a finance preview. This workspace reads verified academy records.</p>
    {turn && <div className={styles.financeTurn}>
      <p><strong>You</strong> {turn.prompt}</p>
      {pending && <p role="status">Checking academy records…</p>}
      {turn.notice && <p role="status">{turn.notice}</p>}
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
                  {reply.kind === "invoice" && available.paymentCollection && <button type="button" disabled={pending || paymentInvoice !== null}
                    onClick={() => setPaymentInvoice(row.invoiceId)}>Record payment</button>}
                </div></td>}
              </tr>)}</tbody>
            </table>
          </div>}
        <p><small>As of <time dateTime={reply.asOfUtc}>{new Date(reply.asOfUtc).toLocaleString()}</time>.
          {reply.hasMore && " More records are available in Manual."}</small></p>
      </section>}
    </div>}
    {available.paymentCollection && <PaymentCollection academyId={academyId} invoiceId={paymentInvoice}
      channel={available} onClose={() => setPaymentInvoice(null)} />}
    <form onSubmit={event => { event.preventDefault(); void send(prompt); }} className={styles.financeComposer}>
      <label htmlFor="finance-chat-prompt">Ask about finance</label>
      <div className={styles.financeActions}>
        <input id="finance-chat-prompt" value={prompt} maxLength={180} autoComplete="off"
          onChange={event => setPrompt(event.currentTarget.value)} placeholder="Show recent expenses" />
        <button type="submit" disabled={pending || !prompt.trim()}>Send</button>
      </div>
    </form>
    <div className={styles.financeActions} aria-label="Example finance prompts">
      {prompts.map(example => <button type="button" key={example} disabled={pending}
        onClick={() => void send(example)}>{example}</button>)}
    </div>
    <p><Link href={reply ? manualRoutes[reply.kind] : "/finance"}>Open Manual Finance</Link></p>
  </section>;
}
