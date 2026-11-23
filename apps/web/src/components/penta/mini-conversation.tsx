"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { apiUrl, isPortalSignOutEvent, portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent } from "@/lib/api";
import { createMiniConversation, initialMiniView } from "@/lib/penta-mini-conversation";
import { PentaGlyph } from "./penta-glyph";
import styles from "./role-penta-entry.module.css";

export default function MiniConversation({ academyId }: { academyId: string }) {
  const [view, setView] = useState(initialMiniView), [prompt, setPrompt] = useState("");
  const [lastPrompt, setLastPrompt] = useState("");
  const driver = useRef<ReturnType<typeof createMiniConversation> | null>(null);
  useEffect(() => {
    const bearer = portalAccessToken("AcademyAdmin"), epoch = portalSessionEpoch("AcademyAdmin");
    if (!bearer || !epoch) return;
    const abort = new AbortController();
    const current = () => !abort.signal.aborted && portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === epoch;
    function clear() { abort.abort(); setPrompt(""); setLastPrompt(""); setView({ ...initialMiniView, phase: "unavailable", notice: "This session changed. Sign in again or use your current Manual workspace." }); }
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
  return <section className={`${styles.panel} ${styles.miniWorkspace}`} aria-labelledby="mini-evaluation-title" aria-busy={view.pending} style={{ overflowWrap: "anywhere" }}>
    <header className={styles.miniHeader}><span className={styles.miniIdentity}><PentaGlyph name="spark" /><h2 id="mini-evaluation-title">Private Mini conversation</h2></span><span className={styles.miniScope}>Local evaluation · Read only</span></header>
    <div className={styles.miniTranscript}>
    {!receipt && !lastPrompt && !view.pending && <div className={styles.miniWelcome}><span className={styles.miniOrb}><PentaGlyph name="spark" /></span><h3>What shall we work on?</h3><p>Find a learner. Explore a balance.<br />Keep the context in your conversation.</p><div className={styles.miniSuggestions}>{["Show students with pending fees", "Show Riya Menon"].map(text => <button type="button" key={text} disabled={!view.available} onClick={() => { setPrompt(text); document.getElementById("private-mini-prompt")?.focus(); }}><PentaGlyph name="chat" />{text}</button>)}</div></div>}
    {lastPrompt && <div className={styles.miniUser}><span>You</span><p>{lastPrompt}</p></div>}
    {view.notice && <p className={styles.miniNotice} role="status">{view.notice}</p>}
    {view.pending && <p className={styles.miniNotice} role="status">Checking the private conversation…</p>}
    {receipt && <section className={styles.miniAnswer} aria-label="Verified Mini response">
      <div className={styles.miniAnswerIdentity}><PentaGlyph name="spark" /><strong>PENTA AI</strong><span>Verified host response</span></div>
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
          <button className="pui-button pui-button-quiet" type="button" disabled={view.pending || !view.available || view.phase !== "idle"} onClick={() => { setLastPrompt("Open result " + (index + 1)); void driver.current?.send("Open result " + (index + 1)); }}>Ask about result {index + 1}</button>
        </li>)}</ol>
        {receipt.result.rows.length === 0 && <p>No matching learners.</p>}
        {receipt.result.hasMore && <p>More matches exist. Refine the learner name.</p>}
        {receipt.exactMoney ? <p>{receipt.result.balanceScope} <Link href="/finance">Open Manual Finance</Link>.</p>
          : <p>Exact amounts are unavailable in this response. Open <Link href="/finance">Manual Finance</Link> to verify them.</p>}
        <small>Source checked <time dateTime={receipt.result.asOfUtc}>{new Date(receipt.result.asOfUtc).toLocaleString()}</time>.</small>
      </>}
      <small>Verified response reference <code>{receipt.requestId}</code></small>
    </section>}
    </div>
    {view.phase === "idle" && view.available && <form className={styles.financeComposer} onSubmit={e => {
      e.preventDefault(); const text = prompt;
      if (!view.pending && text.trim()) setLastPrompt(text);
      void driver.current?.send(text).then(sent => { if (sent) setPrompt(current => current === text ? "" : current); });
    }}>
      <label htmlFor="private-mini-prompt">Message private Mini</label>
      <div className={styles.financeActions}>
      <textarea id="private-mini-prompt" autoComplete="off" value={prompt} maxLength={2000} disabled={view.pending} rows={2}
        onChange={e => setPrompt(e.currentTarget.value)} placeholder="Ask about your academy…" onKeyDown={e => { if (e.key === "Enter" && !e.shiftKey && !e.nativeEvent.isComposing) { e.preventDefault(); if (prompt.trim() && !view.pending) e.currentTarget.form?.requestSubmit(); } }} />
      <button type="submit" disabled={view.pending || !prompt.trim()}><PentaGlyph name="arrow" /><span>Send Mini request</span></button>
      </div>
    </form>}
    <div className={`${styles.financeActions} ${styles.miniControls}`}>
    {view.pointer && <button type="button" disabled={view.pending} onClick={() => void driver.current?.recover()}>Check saved Mini response</button>}
    <button type="button" disabled={view.pending} onClick={() => void driver.current?.initialise()}>Check Mini availability</button>
      <button type="button" disabled={view.pending || !view.available} onClick={() => { setPrompt(""); setLastPrompt(""); void driver.current?.start(); }}>
        {view.session || view.pointer ? "Start separate Mini conversation" : "Start Mini conversation"}
      </button>
      <Link href="/dashboard">Open Manual workspace</Link>
    </div>
    <p className={styles.miniFootnote}>Learner search and follow-up only. No writes, messages or scheduling. Not production-qualified.</p>
  </section>;
}
