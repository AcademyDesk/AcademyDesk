"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { ThemeToggle } from "@/components/theme-toggle";
import { usePentaManualTheme } from "@/components/penta-manual-theme";
import { apiUrl, isPortalSignOutEvent, portalAccessToken, portalSessionChangedEvent, portalSessionEpoch, portalSignOutEvent } from "@/lib/api";
import { readPentaJson } from "@/lib/penta-onboarding-channel";
import { platformAnswer, platformPrompt, platformReady, type PlatformRead } from "@/lib/penta-platform-reads";
import styles from "./role-penta-entry.module.css";

const workspace = "Platform";
const headings: Record<PlatformRead, string> = { health: "Platform health", onboarding: "Academy onboarding", support: "Support queue" };
const examples = ["Show system status", "Show onboarding status", "Show support queue"];
type Channel = { current(): boolean; get(kind: PlatformRead | "readiness"): Promise<unknown> };
type Turn = { question: string; notice?: string; result?: ReturnType<typeof platformAnswer> };

export default function PlatformConversation() {
  const theme = usePentaManualTheme();
  const [channel, setChannel] = useState<Channel | null>(null);
  const [prompt, setPrompt] = useState("");
  const [turn, setTurn] = useState<Turn | null>(null);
  const [pending, setPending] = useState(false);
  const [status, setStatus] = useState("Checking Platform Owner access…");
  const busy = useRef(false);

  useEffect(() => {
    const bearer = portalAccessToken(workspace), epoch = portalSessionEpoch(workspace);
    const abort = new AbortController();
    const current = () => !abort.signal.aborted && !!bearer && !!epoch &&
      portalAccessToken(workspace) === bearer && portalSessionEpoch(workspace) === epoch;
    function clear() {
      if (abort.signal.aborted) return;
      abort.abort();
      setChannel(null); setTurn(null); setPrompt(""); setPending(false);
      setStatus("Platform conversation is unavailable in this session. Sign in or use Manual Platform Control.");
    }
    async function get(kind: PlatformRead | "readiness") {
      if (!current()) throw Error("Platform session changed.");
      const response = await fetch(apiUrl + "/api/platform/penta/" + kind, { cache: "no-store", credentials: "omit",
        redirect: "error", referrerPolicy: "no-referrer", signal: abort.signal,
        headers: { Authorization: "Bearer " + bearer, Accept: "application/json" } });
      if (!current()) throw Error("Platform session changed.");
      if (response.status !== 200 || !response.headers.get("cache-control")?.includes("no-store")) {
        await response.body?.cancel().catch(() => {});
        if (response.status === 401 || response.status === 403) clear();
        throw Error("Platform response could not be verified.");
      }
      const value = await readPentaJson(response, abort.signal);
      if (!current()) throw Error("Platform session changed.");
      return value;
    }
    function changed(event: Event) { if ((event as CustomEvent).detail === workspace) clear(); }
    function check() { if (!current()) clear(); }
    function storage(event: StorageEvent) { if (isPortalSignOutEvent(event, workspace)) clear(); else check(); }
    window.addEventListener(portalSessionChangedEvent, changed);
    window.addEventListener(portalSignOutEvent, changed);
    window.addEventListener("storage", storage);
    window.addEventListener("focus", check);
    void get("readiness").then(value => {
      if (!platformReady(value)) throw Error("Platform contract unavailable.");
      if (current()) { setChannel({ current, get }); setStatus(""); }
    }).catch(clear);
    return () => {
      abort.abort();
      window.removeEventListener(portalSessionChangedEvent, changed);
      window.removeEventListener(portalSignOutEvent, changed);
      window.removeEventListener("storage", storage);
      window.removeEventListener("focus", check);
    };
  }, []);

  async function ask(question: string) {
    const source = channel, text = question.trim();
    if (busy.current || !source?.current() || !text) return;
    const kind = platformPrompt(text);
    setPrompt("");
    if (!kind) { setTurn({ question: text, notice: "Try system status, academy onboarding, or the support queue." }); return; }
    busy.current = true;
    setPending(true); setTurn({ question: text });
    try {
      const result = platformAnswer(await source.get(kind), kind);
      if (source.current()) setTurn({ question: text, result });
    } catch {
      if (source.current()) setTurn({ question: text, notice: "PENTA could not verify the response. Open Manual Platform Control." });
    } finally {
      busy.current = false;
      if (source.current()) setPending(false);
    }
  }

  return <main {...theme} className={theme.className + " " + styles.page}>
    <header className={styles.top}>
      <div className={styles.brand}><span aria-hidden="true">P</span><div><strong>Academy Desk PENTA AI</strong><small>Platform Owner workspace</small></div></div>
      <ThemeToggle />
      <nav className={styles.switcher} aria-label="Work view"><span aria-current="page">PENTA AI</span><Link href="/platform">Manual workspace</Link></nav>
    </header>
    <section className={styles.content}>
      <section className={styles.panel} aria-labelledby="platform-penta-title" aria-busy={pending}>
        <h1 id="platform-penta-title">Platform conversation</h1>
        {status && <p role="status">{status}</p>}
        {!channel && <p><Link href="/login">Sign in</Link></p>}
        {channel && <>
          <p>Ask for a verified platform summary.</p>
          {turn && <div className={styles.financeTurn}>
            <p><strong>You</strong> {turn.question}</p>
            {pending && <p role="status">Checking platform records…</p>}
            {turn.notice && <p role="status">{turn.notice}</p>}
            {turn.result && <section aria-label={headings[turn.result.kind]}>
              <h2>{headings[turn.result.kind]}</h2>
              <dl>{turn.result.items.map(([name, value]) => <div key={name}><dt>{name}</dt><dd>{value}</dd></div>)}</dl>
              <small>Checked <time dateTime={turn.result.asOfUtc}>{new Date(turn.result.asOfUtc).toLocaleString()}</time>.</small>
            </section>}
          </div>}
          <form className={styles.financeComposer} onSubmit={e => { e.preventDefault(); void ask(prompt); }}>
            <label htmlFor="platform-penta-prompt">Message PENTA</label>
            <div className={styles.financeActions}>
              <input id="platform-penta-prompt" autoComplete="off" maxLength={120} value={prompt} disabled={pending}
                onChange={e => setPrompt(e.currentTarget.value)} placeholder="Show system status" />
              <button type="submit" disabled={pending || !prompt.trim()}>Send</button>
            </div>
          </form>
          <div className={styles.financeActions} aria-label="Example platform requests">{examples.map(example =>
            <button type="button" key={example} disabled={pending} onClick={() => void ask(example)}>{example}</button>)}</div>
        </>}
        <p><Link href="/platform/control">Open Manual Platform Control</Link></p>
      </section>
    </section>
  </main>;
}
