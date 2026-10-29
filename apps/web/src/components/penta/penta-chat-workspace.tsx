"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import { academyApi, isPortalSignOutEvent, portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent } from "@/lib/api";
import { validPentaAccount, validPentaContext, validPentaHealth, validPentaReceipt, validPentaSession, type PentaContext as Context, type PentaSession as Session, type PentaReceipt as Receipt } from "@/lib/penta-chat-contract";
import styles from "./penta-chat.module.css";
import design from "./penta-design.module.css";
import PentaResultView from "./penta-result-view";

type Turn = { prompt: string; receipt: Receipt };
const caps = [
  ["pulse", "P", "Pulse", "See what needs attention. This first pilot provides fee lookup; proactive briefings come later."],
  ["executor", "E", "Executor", "Ask for academy records in conversation. Creates, updates and sends are not enabled in this read-only pilot."],
  ["navigator", "N", "Navigator", "Admissions and growth guidance, planned. The current shared conversation supports fee lookup only."],
  ["twin", "T", "Twin", "Understand your academy using verified context. Deeper analysis and simulations are planned."],
  ["autopilot", "A", "Autopilot", "Controlled, approved automation, planned. No background actions are enabled."],
] as const;
const ordinals = ["first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth"];
const failure = (status: number) => status === 401 ? "Sign in again to continue." : status === 403 ? "This pilot needs current Academy Owner/Admin access and the Finance module." : status === 404 ? "PENTA Mini is not enabled here, or this private conversation is unavailable." : status === 409 ? "This conversation is busy or stale. Start a new conversation; no request will be automatically repeated." : status === 410 ? "This conversation expired. Start a new conversation; your manual workspace remains available." : status === 429 ? "The local pilot limit has been reached. Your manual workspace remains available." : "The request could not be completed. Start a new conversation or use the manual workspace.";

export default function PentaChatWorkspace({ active = true }: { active?: boolean }) {
  const [context, setContext] = useState<Context | null>(null);
  const [session, setSession] = useState<Session | null>(null);
  const [capability, setCapability] = useState("executor");
  const [help, setHelp] = useState<string | null>(null);
  const [prompt, setPrompt] = useState("");
  const [turns, setTurns] = useState<Turn[]>([]);
  const [pendingPrompt, setPendingPrompt] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [health, setHealth] = useState("Checking");
  const [blocked, setBlocked] = useState(false);
  const [readinessAttempt, setReadinessAttempt] = useState(0);
  const [followingLatest, setFollowingLatest] = useState(true);
  const flight = useRef<AbortController | null>(null);
  const generation = useRef(0);
  const input = useRef<HTMLTextAreaElement>(null);
  const messages = useRef<HTMLDivElement>(null);
  const followLatest = useRef(true);
  const helpTrigger = useRef<HTMLButtonElement | null>(null);
  const login = useRef<{ epoch: string | null } | null>(null);
  const sessionCurrent = useCallback(() => login.current !== null &&
    portalSessionEpoch("AcademyAdmin") === login.current.epoch && Boolean(portalAccessToken("AcademyAdmin")), []);
  const clearSignedOutConversation = useCallback(() => {
    followLatest.current = true; setFollowingLatest(true);
    flight.current?.abort(); generation.current++;
    setContext(null); setSession(null); setTurns([]); setPrompt(""); setPendingPrompt("");
    setBusy(false); setBlocked(true); setHealth("Unavailable"); setError("Signed out. Sign in to continue.");
  }, []);
  const latest = turns.at(-1)?.receipt;
  const ready = context !== null && health === "Available";
  useEffect(() => {
    const controller = new AbortController();
    const sessionGeneration = generation;
    const identity = { epoch: portalSessionEpoch("AcademyAdmin") }; login.current = identity;
    const current = () => !controller.signal.aborted && login.current === identity && sessionCurrent();
    const ensureCurrent = () => { if (!current()) throw new Error(failure(401)); };
    async function boot() {
      try {
        const response = await academyApi("/api/auth/session", { signal: controller.signal });
        if (!response.ok) throw new Error(failure(response.status));
        const account = await response.json();
        ensureCurrent();
        if (!validPentaAccount(account)) throw new Error(failure(403));
        const own = await academyApi(`/api/academies/${account.academyId}/penta/academy-context`, { signal: controller.signal });
        if (!own.ok) throw new Error(failure(own.status));
        const data = await own.json();
        ensureCurrent();
        if (!validPentaContext(data, account.academyId)) throw new Error("Academy context could not be verified. Use the manual workspace.");
        const status = await academyApi(`/api/academies/${data.academyId}/penta/chat/health`, { signal: controller.signal });
        if (!status.ok) throw new Error(failure(status.status));
        const readiness = await status.json();
        ensureCurrent();
        if (!validPentaHealth(readiness)) throw new Error("Mini readiness could not be verified. Use the manual workspace.");
        if (!controller.signal.aborted) { setContext(data); setHealth(readiness.status); }
      } catch (cause) { if (!controller.signal.aborted) {
        if (!current()) { controller.abort(); clearSignedOutConversation(); }
        else { setHealth("Unavailable"); setError(cause instanceof Error ? cause.message : failure(500)); }
      } }
    }
    void boot();
    function invalidate() { controller.abort(); clearSignedOutConversation(); }
    function checkSession() { if (!current()) invalidate(); }
    function visible() { if (document.visibilityState === "visible") checkSession(); }
    function signout(event: StorageEvent) {
      if (isPortalSignOutEvent(event, "AcademyAdmin")) invalidate(); else checkSession();
    }
    function localSignout(event: Event) {
      if ((event as CustomEvent).detail === "AcademyAdmin") invalidate();
    }
    window.addEventListener("storage", signout);
    window.addEventListener(portalSignOutEvent, localSignout);
    window.addEventListener(portalSessionChangedEvent, localSignout);
    window.addEventListener("focus", checkSession); window.addEventListener("pageshow", checkSession);
    document.addEventListener("visibilitychange", visible);
    return () => { controller.abort(); flight.current?.abort(); sessionGeneration.current++; window.removeEventListener("storage", signout); window.removeEventListener(portalSignOutEvent, localSignout);
      window.removeEventListener(portalSessionChangedEvent, localSignout); window.removeEventListener("focus", checkSession); window.removeEventListener("pageshow", checkSession); document.removeEventListener("visibilitychange", visible); };
  }, [readinessAttempt, sessionCurrent, clearSignedOutConversation]);
  useEffect(() => { if (active && ready) input.current?.focus(); }, [active, ready]);
  useEffect(() => {
    if (!help) return;
    function close(event: KeyboardEvent) {
      if (event.key === "Escape") { setHelp(null); helpTrigger.current?.focus(); }
    }
    window.addEventListener("keydown", close);
    return () => window.removeEventListener("keydown", close);
  }, [help]);
  useEffect(() => {
    if (active && followLatest.current && messages.current) messages.current.scrollTop = messages.current.scrollHeight;
  }, [turns, busy, active]);
  function trackReadingPosition(event: React.UIEvent<HTMLDivElement>) {
    const viewport = event.currentTarget;
    const following = viewport.scrollHeight - viewport.clientHeight - viewport.scrollTop <= 64;
    followLatest.current = following; setFollowingLatest(following);
  }
  function jumpToLatest() {
    followLatest.current = true; setFollowingLatest(true);
    if (messages.current) { messages.current.scrollTop = messages.current.scrollHeight; messages.current.focus({ preventScroll: true }); }
  }
  function recheckReadiness() {
    if (busy || health === "Checking") return;
    flight.current?.abort(); generation.current++;
    followLatest.current = true; setFollowingLatest(true);
    setContext(null); setSession(null); setTurns([]); setPrompt(""); setPendingPrompt("");
    setError(""); setBlocked(false); setHealth("Checking");
    setReadinessAttempt(previous => previous + 1);
  }
  function startNew() {
    if (busy) return;
    if (!sessionCurrent()) { clearSignedOutConversation(); return; }
    followLatest.current = true; setFollowingLatest(true);
    generation.current++; setSession(null); setTurns([]); setPrompt(""); setPendingPrompt(""); setError(""); setBlocked(false); input.current?.focus();
  }
  function stopWaiting() {
    if (!busy) return;
    flight.current?.abort(); flight.current = null; generation.current++;
    setBusy(false); setPendingPrompt(""); setBlocked(true);
    setError("Stopped waiting. Start a new conversation to continue. This request will not be sent again automatically.");
  }
  async function send(event: React.FormEvent) {
    event.preventDefault();
    if (!sessionCurrent()) { clearSignedOutConversation(); return; }
    if (!ready || !context || busy || blocked || !active || !prompt.trim()) return;
    const text = prompt.trim();
    if (text.length > 2000) { setError("Keep prompts under 2,000 characters."); return; }
    const controller = new AbortController(); flight.current = controller;
    const revision = ++generation.current;
    const identity = login.current;
    const stillCurrent = () => !controller.signal.aborted && revision === generation.current && login.current === identity && sessionCurrent();
    followLatest.current = true; setFollowingLatest(true);
    setBusy(true); setError(""); setPendingPrompt(text);
    try {
      let current = session;
      if (!current) {
        const response = await academyApi(`/api/academies/${context.academyId}/penta/chat/conversations`, { method: "POST", signal: controller.signal });
        if (!response.ok) throw new Error(failure(response.status));
        const created: unknown = await response.json();
        if (!stillCurrent()) { if (!sessionCurrent()) clearSignedOutConversation(); return; }
        if (!validPentaSession(created)) throw new Error("The conversation could not be verified. Use the manual workspace.");
        current = created;
        if (revision === generation.current) setSession(current);
      }
      if (!stillCurrent()) { if (!sessionCurrent()) clearSignedOutConversation(); return; }
      const requestId = crypto.randomUUID();
      const response = await academyApi(`/api/academies/${context.academyId}/penta/chat/conversations/${current.conversationId}/turns`, {
        method: "POST", headers: { "Content-Type": "application/json" }, signal: controller.signal,
        body: JSON.stringify({ requestId, expectedVersion: current.version, text, capability }),
      });
      if (!response.ok) throw new Error(failure(response.status));
      const receipt = await response.json();
      if (!stillCurrent()) { if (!sessionCurrent()) clearSignedOutConversation(); return; }
      if (!validPentaReceipt(receipt, current, requestId, capability)) throw new Error("The result could not be verified. Start a new conversation or use the manual workspace.");
      if (revision === generation.current && !controller.signal.aborted) {
        setTurns(previous => [...previous, { prompt: text, receipt }]); setPrompt(""); setPendingPrompt("");
        setSession({ ...current, version: receipt.version });
      }
    } catch (cause) {
      if (!sessionCurrent()) clearSignedOutConversation();
      else if (revision === generation.current && !controller.signal.aborted) { setBlocked(true); setPendingPrompt(""); setError(cause instanceof Error ? cause.message : failure(500)); }
    } finally { if (flight.current === controller) flight.current = null; if (revision === generation.current) { setBusy(false); if (followLatest.current) input.current?.focus({ preventScroll: true }); } }
  }

  return <main className={`${design.system} ${styles.page}`} data-penta-ui="0.1">
    <header className={styles.heading}><div><span className={styles.eyebrow}>Academy Desk PENTA AI</span><h1>Your academy. One conversation.</h1><p>Ask naturally. Follow up. Every fee amount comes from your academy’s ledger.</p></div><span className={styles.status}>{health === "Available" ? "● Mini connected" : `Mini · ${health}`}<small>Private · Read-only pilot</small></span></header>
    <nav className={styles.capabilities} aria-label="PENTA capabilities">{caps.map(([id, letter, title, description]) => <div key={id}><button type="button" aria-pressed={capability === id} onClick={() => { setCapability(id); input.current?.focus(); }}><span>{letter}</span>{title}</button><button type="button" className={styles.help} aria-label={`About ${title}`} aria-expanded={help === id} onClick={event => { helpTrigger.current = event.currentTarget; setHelp(help === id ? null : id); }}>!</button>{help === id && <div className={styles.helpText} role="note"><strong>{title}</strong><p>{description}</p><button type="button" onClick={() => { setHelp(null); helpTrigger.current?.focus(); }}>Close</button></div>}</div>)}</nav>
    <p className={styles.scope}><Link href="/penta/onboarding" prefetch={false}>Protected onboarding review ↗</Link> · <Link href="/penta/teachers/chat" prefetch={false}>Teacher onboarding conversation ↗</Link> · Availability checked by the host</p>
    {health !== "Available" && <section className={styles.readiness} aria-label="AI availability">
      <div><strong role="status">{health === "Checking" ? "Checking PENTA access and Mini readiness…" : "PENTA is not ready for this conversation."}</strong><p>{health === "Checking" ? "AI requests stay disabled until academy access and readiness are verified." : "Use your manual workspace while AI is unavailable. Recheck starts a clean conversation, clears any draft and verifies access again; it never resends a request."}</p></div>
      <div className={styles.readinessActions}>{health !== "Checking" && <button type="button" disabled={busy} onClick={recheckReadiness}>Recheck AI availability</button>}<Link href="/dashboard">Open manual workspace ↗</Link></div>
    </section>}
    <div className={styles.layout}><section className={styles.conversation} aria-label="PENTA conversation">
      <div className={styles.chatHead}><span>{context?.name || (health === "Checking" ? "Checking academy access…" : "Academy access unverified")}</span><button type="button" disabled={busy} onClick={startNew}>New conversation</button></div>
      <p className={styles.announcement} role="status" aria-live="polite" aria-atomic="true">{busy ? "PENTA is preparing a verified read." : error ? "" : latest ? `Reply ${turns.length} ready. ${latest.kind === "RESULT" ? `Verified read. ${latest.result?.rows.length ?? 0} students displayed.` : latest.kind === "CLARIFICATION_REQUIRED" ? "Choose a student to continue." : "Review PENTA's response."}` : ""}</p>
      <div ref={messages} className={styles.messages} role="region" aria-label="Conversation messages" tabIndex={0} onScroll={trackReadingPosition}>
        {!turns.length && !busy && <div className={styles.welcome}><span className={styles.orb}>P</span><h2>What needs your attention?</h2><p>Find a student by name or student code, check fees, then refine your request in conversation. If several students match, you choose using their record codes and references.</p><button type="button" disabled={!ready || blocked} onClick={() => { setPrompt("Show students with pending fees."); input.current?.focus(); }}>Show students with pending fees ↗</button><small>Or ask “Show students named [name]” or “Find student [code]” · Active students only · Up to 10 results</small><small>Start a new conversation for a different search. Corrections and combined filter resets are still limited in this pilot.</small></div>}
        {turns.map((turn, index) => <article key={turn.receipt.requestId} className={styles.turn}>
          <p className={styles.user}><span>You</span>{turn.prompt}</p>
          <div className={styles.assistant}>
            <span className={styles.byline}>PENTA AI · {turn.receipt.kind === "RESULT" ? "Verified read" : turn.receipt.kind.replaceAll("_", " ")}</span><p>{turn.receipt.message}</p>
            {turn.receipt.result && <>
              <p className={styles.source}>As of {new Date(turn.receipt.result.asOfUtc).toLocaleString("en-IN", { timeZone: context?.timeZone || "Asia/Kolkata" })} · {turn.receipt.result.source}</p>
              <PentaResultView caption="Verified students · Source order" columns={["Reference", "Student", "Courses", "Outstanding", "Source & follow-up"]} rows={turn.receipt.result.rows.map((row, rowIndex) => ({ key: row.sourceId, cells: [
                rowIndex + 1,
                <div key="student"><strong>{row.displayName}</strong><small>Student code: {row.recordCode || "Not assigned"} · Active</small></div>,
                row.subjects.join(" · ") || "No active course",
                <div key="balances">{row.balances.length ? row.balances.map(balance => <strong key={balance.currency}>{balance.currency} {balance.outstanding.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</strong>) : "No outstanding fees"}</div>,
                <div key="source"><Link href={row.sourcePath}>Open Student 360 ↗</Link><details><summary>Record reference</summary><code>{row.sourceId}</code></details>{index === turns.length - 1 && turn.receipt.kind === "CLARIFICATION_REQUIRED" && <button type="button" disabled={busy || blocked || !active} aria-label={`Choose student ${rowIndex + 1}: ${row.displayName}${row.recordCode ? ` (${row.recordCode})` : ""}`} onClick={() => { setPrompt(`Show the ${ordinals[rowIndex]} one.`); input.current?.focus(); }}>Choose student {rowIndex + 1}</button>}</div>,
              ] }))}>
              <ol className={styles.cards}>{turn.receipt.result.rows.map((row, rowIndex) => <li key={row.sourceId}>
                <div><span className={styles.ordinal}>{rowIndex + 1}</span><h3>{row.displayName}</h3><span className={styles.active}>Active</span></div>
                <p className={styles.recordCode}>Student code: {row.recordCode || "Not assigned"}</p>
                <p>{row.subjects.join(" · ") || "No active course"}</p>
                <div className={styles.balances}>{row.balances.length ? row.balances.map(balance => <strong key={balance.currency}>{balance.currency} {balance.outstanding.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}<small>Outstanding</small></strong>) : <strong>No outstanding fees</strong>}</div>
                <footer>
                  <details><summary>Record reference</summary><code>{row.sourceId}</code></details><Link href={row.sourcePath}>Open Student 360 ↗</Link>
                  {index === turns.length - 1 && turn.receipt.kind === "CLARIFICATION_REQUIRED" && <button type="button" disabled={busy || blocked || !active} aria-label={`Choose student ${rowIndex + 1}: ${row.displayName}${row.recordCode ? ` (${row.recordCode})` : ""}`} onClick={() => { setPrompt(`Show the ${ordinals[rowIndex]} one.`); input.current?.focus(); }}>Choose student {rowIndex + 1}</button>}
                </footer>
              </li>)}</ol>
              </PentaResultView>
              {turn.receipt.result.hasMore && <p className={styles.source}>Showing the first {turn.receipt.result.rows.length}. Refine your prompt to find fewer students.</p>}
              <p className={styles.scope}>{turn.receipt.result.balanceScope}</p>
            </>}
            {index === turns.length - 1 && latest?.result && <p className={styles.followup}>{latest.kind === "CLARIFICATION_REQUIRED" ? "Choose a student to prepare your follow-up, then press Send. Nothing is selected automatically." : "Try “Only piano”, “Highest first”, or “Show the second one”."}</p>}
          </div>
        </article>)}
        {busy && <div className={styles.turn}><p className={styles.user}><span>You</span>{pendingPrompt}</p><p className={styles.thinking}>PENTA Mini is interpreting your request. Academy Desk will authorize and verify the read…</p><button type="button" className={styles.stopWaiting} onClick={stopWaiting}>Stop waiting</button></div>}
      </div>
      {!followingLatest && <div className={styles.jumpBar}><button type="button" onClick={jumpToLatest}>Jump to latest ↓</button></div>}
      <form className={styles.composer} onSubmit={send}><label htmlFor="penta-prompt">Message PENTA AI</label><textarea ref={input} id="penta-prompt" rows={2} maxLength={2000} value={prompt} disabled={busy || !ready || blocked} placeholder="Find a student by name, or ask about fees…" onChange={event => setPrompt(event.target.value)} onKeyDown={event => { if (event.key === "Enter" && !event.shiftKey && !event.nativeEvent.isComposing) { event.preventDefault(); event.currentTarget.form?.requestSubmit(); } }} /><div><small>Enter to send · Shift + Enter for a new line</small><button type="submit" disabled={busy || !ready || blocked || !prompt.trim()}>{busy ? "Thinking…" : "Send ↑"}</button></div><p className={styles.privacy}>Prompts are not saved to SQL. This view stays in memory; reload starts a new conversation. No automatic retries or actions.</p>{error && <p role="alert" className={styles.error}>{error}</p>}</form>
    </section><aside className={styles.rail}><details open><summary>Context & control</summary><dl><dt>Academy</dt><dd>{context?.name || "Unverified"}</dd><dt>Available tools</dt><dd>{ready && !blocked ? "Search students · Read selected student" : "Unavailable · Use the manual workspace"}</dd><dt>Current filters</dt><dd>{latest ? Object.entries(latest.context.filters).map(([key, value]) => `${key.replaceAll("_", " ")}: ${value}`).join(" · ") || "None" : "None"}</dd><dt>Sort</dt><dd>{latest?.context.sort_by === "outstanding_desc" ? "Outstanding · highest first" : "Student name"}</dd><dt>Authority</dt><dd>Current Owner/Admin + Finance. Your own academy only.</dd><dt>Model</dt><dd>Private PENTA Mini · Protocol 0.1</dd></dl></details><details open><summary>Manual workspace</summary><p>Direct control remains available. AI does not change your records.</p><Link href="/invoices">Invoices ↗</Link><Link href="/payments">Payments ↗</Link><Link href="/students">Students ↗</Link><Link href="/batch-setup">Classes & batches ↗</Link></details></aside></div>
  </main>;
}
