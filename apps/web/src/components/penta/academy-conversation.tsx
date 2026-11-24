"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { apiUrl, isPortalSignOutEvent, portalAccessToken, portalSessionChangedEvent, portalSessionEpoch, portalSignOutEvent } from "@/lib/api";
import { createQueryConversation, initialQueryView } from "@/lib/penta-query-conversation";
import MiniConversation from "./mini-conversation";
import { PentaGlyph } from "./penta-glyph";
import styles from "./role-penta-entry.module.css";

/** One entry point negotiates the actual host protocol. A missing query route
 * may retain the explicitly labelled legacy pilot, never claim query coverage. */
export default function AcademyConversation({ academyId }: { academyId: string }) {
  const [route, setRoute] = useState<"checking" | "query" | "legacy">("checking");
  useEffect(() => {
    const abort = new AbortController(), bearer = portalAccessToken("AcademyAdmin");
    const timer = setTimeout(() => { setRoute("query"); abort.abort(); }, 6000);
    void fetch(`${apiUrl}/api/academies/${academyId}/penta/queries/health`, {
      headers: { Authorization: "Bearer " + bearer, Accept: "application/json" }, cache: "no-store",
      credentials: "omit", redirect: "error", referrerPolicy: "no-referrer", signal: abort.signal,
    }).then(async response => { await response.body?.cancel(); if (!abort.signal.aborted) setRoute(response.status === 404 ? "legacy" : "query"); })
      .catch(() => { /* uncertain availability must not downgrade the protocol */ if (!abort.signal.aborted) setRoute("query"); })
      .finally(() => clearTimeout(timer));
    return () => { clearTimeout(timer); abort.abort(); };
  }, [academyId]);
  return route === "checking" ? <p role="status">Checking the connected conversation protocol…</p>
    : route === "legacy" ? <><p className={styles.miniNotice}>Only the legacy student-fee pilot is connected here. Teacher, batch and other operations are unavailable in this pilot.</p><MiniConversation academyId={academyId} /></>
    : <QueryConversation academyId={academyId} />;
}

export function QueryConversation({ academyId }: { academyId: string }) {
  const [view, setView] = useState(initialQueryView), [prompt, setPrompt] = useState("");
  const driver = useRef<ReturnType<typeof createQueryConversation> | null>(null);
  const transcript = useRef<HTMLDivElement>(null);
  useEffect(() => { const node = transcript.current; if (node) node.scrollTop = node.scrollHeight; }, [view.turns.length, view.pending]);
  useEffect(() => {
    const bearer = portalAccessToken("AcademyAdmin"), epoch = portalSessionEpoch("AcademyAdmin");
    if (!bearer || !epoch) return;
    const abort = new AbortController();
    const current = () => !abort.signal.aborted && portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === epoch;
    const clear = () => { abort.abort(); setPrompt(""); setView({ ...initialQueryView, phase: "unavailable", notice: "This account session changed. Sign in again or use Manual." }); };
    const conversation = createQueryConversation({ academyId, epoch, current, invalidate: clear, signal: abort.signal,
      send: (path, method, body) => fetch(`${apiUrl}/api/academies/${academyId}/penta/queries${path}`, {
        method, cache: "no-store", credentials: "omit", redirect: "error", referrerPolicy: "no-referrer", signal: abort.signal,
        headers: { Authorization: "Bearer " + bearer, Accept: "application/json", ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
        body: body === undefined ? undefined : JSON.stringify(body),
      }),
    }, () => window.sessionStorage, setView);
    driver.current = conversation;
    function changed(event: Event) { if ((event as CustomEvent).detail === "AcademyAdmin") clear(); }
    function check() { if (!current()) clear(); }
    function storage(event: StorageEvent) { if (isPortalSignOutEvent(event, "AcademyAdmin")) clear(); else check(); }
    window.addEventListener(portalSessionChangedEvent, changed); window.addEventListener(portalSignOutEvent, changed);
    window.addEventListener("focus", check); window.addEventListener("storage", storage);
    void conversation.initialise();
    return () => { abort.abort(); conversation.dispose(); driver.current = null;
      window.removeEventListener(portalSessionChangedEvent, changed); window.removeEventListener(portalSignOutEvent, changed);
      window.removeEventListener("focus", check); window.removeEventListener("storage", storage); };
  }, [academyId]);
  return <section className={`${styles.panel} ${styles.miniWorkspace} ${styles.queryWorkspace}`} aria-label="Academy conversation">
    <div className={styles.queryHeader}>
    <div className={styles.miniIdentity}><PentaGlyph name="spark" /><strong>PENTA AI</strong><span>Connected read evaluation</span></div>
    {view.entities.length > 0 && <p className={styles.miniNotice}>Connected records: {view.entities.map(x => x === "batch" ? "batches" : x + "s").join(", ")}. Other actions remain in guided workflows and Manual.</p>}
    {view.notice && <p role="status">{view.notice}</p>}
    {view.phase === "ready" && <button className="pui-button" type="button" disabled={view.pending} onClick={() => void driver.current?.start()}>Start conversation</button>}
    </div>
    <div ref={transcript} className={`${styles.miniTranscript} ${styles.queryTranscript}`} aria-label="Conversation messages" aria-live="polite" aria-busy={view.pending}>
      {view.hasMore && <p>Showing the latest 20 responses. Earlier responses remain in the protected conversation history.</p>}
      {view.turns.map(turn => <div key={turn.requestId}>
        <div className={styles.miniUser}><span>You</span><p>{turn.inputText}</p></div>
        <section className={styles.miniAnswer} aria-label="PENTA response">
          <div className={styles.miniAnswerIdentity}><PentaGlyph name="spark" /><strong>PENTA AI</strong><span>{turn.kind === "RESULT" ? "Verified academy records" : "No action completed"}</span></div>
          <p>{turn.message}</p>
          {turn.result && <><ol>{turn.result.records.map((record, index) => <li key={record.id}>
            {turn.result!.entity === "batch" ? <><strong>{record.label}</strong> · <Link href={record.sourcePath}>Open Manual batch directory</Link></> : <Link href={record.sourcePath}>{record.label}</Link>}{record.code && <> · {record.code}</>}
            <p>{record.active ? "Active" : "Inactive"}{record.subjects.length > 0 && <> · {record.subjects.join(", ")}</>}</p>
            {turn.version === view.session?.version && <button className="pui-button pui-button-quiet" type="button" disabled={view.pending || !view.available || view.phase !== "idle"}
              onClick={() => void driver.current?.send(`Open result ${index + 1}`)}>Open result {index + 1} in conversation</button>}
          </li>)}</ol><small>Source: academy {turn.result.entity} records · Checked <time dateTime={turn.result.asOfUtc}>{new Date(turn.result.asOfUtc).toLocaleString()}</time></small></>}
          <small>Response reference <code>{turn.requestId}</code></small>
        </section>
      </div>)}
    </div>
    <div className={styles.queryFooter}>
    {view.phase === "idle" && view.available && <form className={styles.financeComposer} onSubmit={event => {
      event.preventDefault(); const value = prompt; void driver.current?.send(value).then(sent => { if (sent) setPrompt(current => current === value ? "" : current); });
    }}>
      <label htmlFor="penta-academy-query">Message PENTA AI</label>
      <div className={styles.financeActions}><textarea id="penta-academy-query" rows={2} maxLength={2000} value={prompt} autoComplete="off" disabled={view.pending}
        placeholder="Ask about students, teachers or batches…" onChange={event => setPrompt(event.currentTarget.value)} />
        <button type="submit" disabled={view.pending || !prompt.trim()}><PentaGlyph name="arrow" /><span>Send</span></button></div>
    </form>}
    {view.pending && <p role="status">Checking your academy records…</p>}
    {view.pointer && (view.phase === "unknown" || view.phase === "closed") && <button className="pui-button" type="button" disabled={view.pending} onClick={() => void driver.current?.recover()}>Recover saved response</button>}
    {view.available && view.pointer && !view.pending && <button className="pui-button pui-button-quiet" type="button" onClick={() => void driver.current?.start()}>Start a separate conversation</button>}
    <Link href="/dashboard">Open Manual workspace</Link>
    </div>
  </section>;
}
