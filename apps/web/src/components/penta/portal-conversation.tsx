"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { apiUrl, isPortalSignOutEvent, portalAccessToken, portalSessionChangedEvent, portalSessionEpoch, portalSignOutEvent } from "@/lib/api";
import { readPentaJson } from "@/lib/penta-onboarding-channel";
import { portalAnswer, portalLearners, portalPentaRoot, portalPrompt, resolvePortalLearner, type PortalAnswer,
  type PortalLearner, type PortalPrompt, type PortalRead } from "@/lib/penta-portal-reads";
import styles from "./role-penta-entry.module.css";

const workspace = "Portal";
const headlines: Record<PortalRead, string> = {
  "next-class": "Next class", "due-work": "Upcoming due work", "outstanding-fees": "Outstanding fees",
};
const when = (value: string) => new Date(value).toLocaleString();
type Channel = { current(): boolean; get(path: string): Promise<unknown> };
type Turn = { question: string; notice?: string; choices?: PortalLearner[]; followUp?: PortalPrompt;
  result?: { learner: string; answer: PortalAnswer } };
class PortalError extends Error { constructor(readonly status: number) { super("Portal read unavailable."); } }

export default function PortalConversation({ academyId, fallback }: { academyId: string; fallback: ReactNode }) {
  const [channel, setChannel] = useState<Channel | null>(null);
  const [prompt, setPrompt] = useState("");
  const [turn, setTurn] = useState<Turn | null>(null);
  const [pending, setPending] = useState(false);
  const locked = useRef(false), selected = useRef<string | null>(null);

  useEffect(() => {
    const bearer = portalAccessToken(workspace), epoch = portalSessionEpoch(workspace);
    if (!bearer || !epoch) return;
    const abort = new AbortController();
    const current = () => !abort.signal.aborted && portalAccessToken(workspace) === bearer && portalSessionEpoch(workspace) === epoch;
    function clear() {
      abort.abort();
      selected.current = null;
      setChannel(null); setTurn(null); setPrompt(""); setPending(false);
    }
    async function get(path: string) {
      if (!current()) throw Error("Portal session changed.");
      const response = await fetch(apiUrl + portalPentaRoot + path, { cache: "no-store", credentials: "omit",
        redirect: "error", referrerPolicy: "no-referrer", signal: abort.signal,
        headers: { Authorization: "Bearer " + bearer, Accept: "application/json" } });
      if (!current()) throw Error("Portal session changed.");
      if (response.status !== 200 || !response.headers.get("cache-control")?.includes("no-store")) {
        await response.body?.cancel().catch(() => {});
        if (response.status === 401) clear();
        throw new PortalError(response.status);
      }
      const value = await readPentaJson(response, abort.signal);
      if (!current()) throw Error("Portal session changed.");
      return value;
    }
    function changed(event: Event) { if ((event as CustomEvent).detail === workspace) clear(); }
    function check() { if (!current()) clear(); }
    function storage(event: StorageEvent) { if (isPortalSignOutEvent(event, workspace)) clear(); else check(); }
    window.addEventListener(portalSessionChangedEvent, changed);
    window.addEventListener(portalSignOutEvent, changed);
    window.addEventListener("storage", storage);
    window.addEventListener("focus", check);
    void get("/learners").then(value => {
      portalLearners(value, academyId);
      if (current()) setChannel({ current, get });
    }).catch(() => {});
    return () => {
      abort.abort();
      window.removeEventListener(portalSessionChangedEvent, changed);
      window.removeEventListener(portalSignOutEvent, changed);
      window.removeEventListener("storage", storage);
      window.removeEventListener("focus", check);
    };
  }, [academyId]);

  async function ask(question: string, request: PortalPrompt | null, chosen?: string) {
    const source = channel;
    if (!source?.current() || locked.current || !question.trim()) return;
    locked.current = true;
    setPrompt(""); setTurn({ question }); setPending(true);
    try {
      if (!request) {
        setTurn({ question, notice: "Try asking about the next class, upcoming due work, or outstanding fees." });
        return;
      }
      const { learners, hasMore } = portalLearners(await source.get("/learners"), academyId);
      if (!source.current()) return;
      const learner = chosen ? learners.find(x => x.id === chosen) :
        resolvePortalLearner(request, learners, hasMore, selected.current);
      if (!learner) {
        selected.current = null;
        setTurn({ question, notice: learners.length ? "Which linked learner should I check?" : "No learner is currently linked to this account.",
          choices: learners, followUp: request });
        return;
      }
      if (!(request.kind === "outstanding-fees" ? learner.finance : learner.academic)) {
        setTurn({ question, notice: "This account does not have access to that information for " + learner.name + "." });
        return;
      }
      const answer = portalAnswer(await source.get("/students/" + learner.id + "/" + request.kind), academyId, learner.id, request.kind);
      if (!source.current()) return;
      selected.current = learner.id;
      setTurn({ question, result: { learner: learner.name, answer } });
    } catch (error) {
      selected.current = null;
      if (source.current()) setTurn({ question, notice: error instanceof PortalError && error.status === 403
        ? "Access to this learner or information has changed. Check your current access in the Student Portal."
        : "PENTA could not verify this information. Try again or open the Student Portal." });
    } finally {
      locked.current = false;
      if (source.current()) setPending(false);
    }
  }

  if (!channel) return fallback;
  const answer = turn?.result?.answer;
  return <section className={styles.panel} aria-labelledby="portal-penta-title" aria-busy={pending}>
    <h2 id="portal-penta-title">PENTA learner conversation</h2>
    <p>Ask about classes, due work, or fees for a learner you can access.</p>
    {turn && <div className={styles.financeTurn}>
      <p><strong>You</strong> {turn.question}</p>
      {pending && <p role="status">Checking current portal records…</p>}
      {turn.notice && <p role="status">{turn.notice}</p>}
      {turn.followUp && <div className={styles.financeActions}>
        {turn.choices?.map(learner => <button type="button" key={learner.id} disabled={pending} onClick={() =>
          void ask(turn.question, turn.followUp ?? null, learner.id)}>Choose {learner.name}</button>)}
      </div>}
      {answer && <section aria-label={headlines[answer.kind]}>
        <h3>{headlines[answer.kind]} · {turn.result?.learner}</h3>
        {!answer.rows.length && <p>No matching records.</p>}
        {answer.kind === "next-class" && answer.rows.map(row =>
          <p key={row.startUtc}>{row.batchName} · {when(row.startUtc)} · {row.deliveryMode === "InPerson" ? "Offline" : row.deliveryMode}</p>)}
        {answer.kind === "due-work" && <ul>{answer.rows.map((row, index) =>
          <li key={index}>{row.title} · due {when(row.dueAtUtc)}</li>)}</ul>}
        {answer.kind === "outstanding-fees" && <ul>{answer.rows.map((row, index) =>
          <li key={index}>{row.invoiceNumber} · {row.currency} {row.balance} · due {row.dueDate}</li>)}</ul>}
        <p><small>Checked <time dateTime={answer.asOfUtc}>{when(answer.asOfUtc)}</time>. <Link href="/portal">Open Student Portal</Link></small></p>
      </section>}
    </div>}
    <form className={styles.financeComposer} onSubmit={event => {
      event.preventDefault(); void ask(prompt, portalPrompt(prompt));
    }}>
      <label htmlFor="portal-penta-prompt">Message PENTA</label>
      <div className={styles.financeActions}>
        <input id="portal-penta-prompt" autoComplete="off" value={prompt} maxLength={140} disabled={pending}
          placeholder="When is my next class?" onChange={event => setPrompt(event.currentTarget.value)} />
        <button type="submit" disabled={pending || !prompt.trim()}>Send</button>
      </div>
    </form>
    <p><Link href="/portal">Open Manual Student Portal</Link></p>
  </section>;
}
