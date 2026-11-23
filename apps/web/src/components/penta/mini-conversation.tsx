"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { apiUrl, isPortalSignOutEvent, portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent } from "@/lib/api";
import { createMiniConversation, initialMiniView } from "@/lib/penta-mini-conversation";
import styles from "./role-penta-entry.module.css";

export default function MiniConversation({ academyId }: { academyId: string }) {
  const [view, setView] = useState(initialMiniView), [prompt, setPrompt] = useState("");
  const driver = useRef<ReturnType<typeof createMiniConversation> | null>(null);
  useEffect(() => {
    const bearer = portalAccessToken("AcademyAdmin"), epoch = portalSessionEpoch("AcademyAdmin");
    if (!bearer || !epoch) return;
    const abort = new AbortController();
    const current = () => !abort.signal.aborted && portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === epoch;
    function clear() { abort.abort(); setPrompt(""); setView({ ...initialMiniView, phase: "unavailable", notice: "This session changed. Sign in again or use your current Manual workspace." }); }
    const mini = createMiniConversation({ academyId, epoch, current, invalidate: clear, signal: abort.signal,
      send: (path, method, body) => fetch(apiUrl + "/api/academies/" + academyId + "/penta/chat" + path, {
        method, cache: "no-store", credentials: "omit", redirect: "error", referrerPolicy: "no-referrer", signal: abort.signal,
        headers: { Authorization: "Bearer " + bearer, Accept: "application/json", ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
        body: body === undefined ? undefined : JSON.stringify(body),
      }),
    }, () => window.sessionStorage, setView);
    driver.current = mini;
    function changed(e: Event) { if ((e as CustomEvent).detail === "AcademyAdmin") clear(); }
    function check() { if (!current()) clear(); }
    function storage(e: StorageEvent) { if (isPortalSignOutEvent(e, "AcademyAdmin")) clear(); else check(); }
    window.addEventListener(portalSessionChangedEvent, changed); window.addEventListener(portalSignOutEvent, changed);
    window.addEventListener("focus", check); window.addEventListener("storage", storage);
    void mini.initialise();
    return () => {
      abort.abort(); mini.dispose(); driver.current = null;
      window.removeEventListener(portalSessionChangedEvent, changed); window.removeEventListener(portalSignOutEvent, changed);
      window.removeEventListener("focus", check); window.removeEventListener("storage", storage);
    };
  }, [academyId]);
  const receipt = view.receipt;
  return <section className={styles.panel} aria-labelledby="mini-evaluation-title" aria-busy={view.pending} style={{ overflowWrap: "anywhere" }}>
    <h2 id="mini-evaluation-title">Private Mini conversation</h2>
    <p>Read-only evaluation for learner search and follow-up. No writes, messages or scheduling. Not production-qualified.</p>
    {view.notice && <p role="status">{view.notice}</p>}
    {view.pending && <p role="status">Checking the private conversation…</p>}
    {receipt && <section aria-label="Verified Mini response">
      <h3>{receipt.kind === "CLARIFICATION_REQUIRED" ? "Clarification needed" : receipt.kind === "RESULT" ? "Verified learner read" : "Mini response"}</h3>
      <p>{receipt.message}</p>
      {receipt.result && <>
        <ol>{receipt.result.rows.map((row, index) => <li key={row.sourceId}>
          <Link href={row.sourcePath}>{row.displayName}</Link>{row.recordCode && <> · {row.recordCode}</>}
          {row.subjects.length > 0 && <p>{row.subjects.join(", ")}</p>}
          {receipt.exactMoney && (receipt.exactMoney.rows[index].balances.length > 0
            ? <ul aria-label={"Exact outstanding balances for " + row.displayName}>
              {receipt.exactMoney.rows[index].balances.map(balance => <li key={balance.currency}>{balance.currency} {balance.outstandingExact}</li>)}
            </ul> : <p>No outstanding balances in this snapshot.</p>)}
          <button type="button" disabled={view.pending || !view.available || view.phase !== "idle"} onClick={() => void driver.current?.send("Open result " + (index + 1))}>Ask about result {index + 1}</button>
        </li>)}</ol>
        {receipt.result.rows.length === 0 && <p>No matching learners.</p>}
        {receipt.result.hasMore && <p>More matches exist. Refine the learner name.</p>}
        {receipt.exactMoney ? <p>{receipt.result.balanceScope} <Link href="/finance">Open Manual Finance</Link>.</p>
          : <p>Exact amounts are unavailable in this response. Open <Link href="/finance">Manual Finance</Link> to verify them.</p>}
        <small>Source checked <time dateTime={receipt.result.asOfUtc}>{new Date(receipt.result.asOfUtc).toLocaleString()}</time>.</small>
      </>}
      <small>Verified response reference <code>{receipt.requestId}</code></small>
    </section>}
    {view.phase === "idle" && view.available && <form className={styles.financeComposer} onSubmit={e => {
      e.preventDefault(); const text = prompt;
      void driver.current?.send(text).then(sent => { if (sent) setPrompt(current => current === text ? "" : current); });
    }}>
      <label htmlFor="private-mini-prompt">Message private Mini</label>
      <input id="private-mini-prompt" autoComplete="off" value={prompt} maxLength={2000} disabled={view.pending}
        onChange={e => setPrompt(e.currentTarget.value)} placeholder="Find learners with pending fees" />
      <button type="submit" disabled={view.pending || !prompt.trim()}>Send Mini request</button>
    </form>}
    {view.pointer && <button type="button" disabled={view.pending} onClick={() => void driver.current?.recover()}>Check saved Mini response</button>}
    <button type="button" disabled={view.pending} onClick={() => void driver.current?.initialise()}>Check Mini availability</button>
    <div className={styles.financeActions}>
      <button type="button" disabled={view.pending || !view.available} onClick={() => { setPrompt(""); void driver.current?.start(); }}>
        {view.session || view.pointer ? "Start separate Mini conversation" : "Start Mini conversation"}
      </button>
      <Link href="/dashboard">Open Manual workspace</Link>
    </div>
  </section>;
}
