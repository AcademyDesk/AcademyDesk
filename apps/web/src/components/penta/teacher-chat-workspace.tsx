"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { useTheme } from "@/components/theme-provider";
import { portalSessionChangedEvent, portalSignOutEvent, isPortalSignOutEvent } from "@/lib/api";
import { loadTeacherDialogue, TeacherDialogueUnavailable, type TeacherDialogueState } from "@/lib/penta-teacher-dialogue";
import { Foundation, Button, Icon, Notice } from "@/vendor/penta-operational/frontend/shared-foundation/src";
import styles from "./teacher-chat.module.css";

type Channel = Awaited<ReturnType<typeof loadTeacherDialogue>>;
type Message = { who: "You" | "PENTA AI"; text: string };
export default function TeacherChatWorkspace({ conversation, draft }: { conversation: string; draft: string }) {
  const { theme, setTheme } = useTheme();
  const [phase, setPhase] = useState<"loading" | "ready" | "pending" | "unavailable" | "access">("loading");
  const [value, setValue] = useState<TeacherDialogueState | null>(null), [messages, setMessages] = useState<Message[]>([]);
  const [prompt, setPrompt] = useState(""), [busy, setBusy] = useState(false);
  const channel = useRef<Channel | null>(null), alive = useRef(false), lock = useRef(false), input = useRef<HTMLTextAreaElement | null>(null);
  const conversationEnd = useRef<HTMLDivElement | null>(null);
  useEffect(() => {
    alive.current = true; const abort = new AbortController();
    function clear() { abort.abort(); channel.current?.dispose(); channel.current = null; if (alive.current) { setValue(null); setMessages([]); setPrompt(""); setPhase("access"); } }
    function local(event: Event) { if ((event as CustomEvent).detail === "AcademyAdmin") clear(); }
    function current() { if (channel.current && !channel.current.isCurrent) clear(); }
    function storage(event: StorageEvent) { if (isPortalSignOutEvent(event, "AcademyAdmin")) clear(); else current(); }
    window.addEventListener(portalSessionChangedEvent, local); window.addEventListener(portalSignOutEvent, local);
    window.addEventListener("storage", storage); window.addEventListener("focus", current); window.addEventListener("popstate", current);
    const timer = window.setInterval(current, 1000);
    void loadTeacherDialogue(conversation, draft, abort.signal).then(result => {
      if (!alive.current || abort.signal.aborted) { result.dispose(); return; }
      channel.current = result; const next = result.current; setValue(next); setPhase(next.phase); setMessages([{ who: "PENTA AI", text: next.message }]);
    }).catch(error => { if (alive.current && !abort.signal.aborted) { abort.abort(); setPhase(error instanceof TeacherDialogueUnavailable && error.access ? "access" : "unavailable"); } });
    return () => { alive.current = false; abort.abort(); channel.current?.dispose(); channel.current = null; window.clearInterval(timer);
      window.removeEventListener(portalSessionChangedEvent, local); window.removeEventListener(portalSignOutEvent, local); window.removeEventListener("storage", storage); window.removeEventListener("focus", current); window.removeEventListener("popstate", current); };
  }, [conversation, draft]);
  useEffect(() => { const pane = conversationEnd.current?.parentElement; if (!busy && pane) pane.scrollTo({ top: pane.scrollHeight, behavior: "smooth" }); }, [messages, busy]);
  async function act(recover: boolean) {
    if (lock.current || !channel.current || !alive.current || phase === "access" || (!recover && !prompt.trim())) return;
    lock.current = true; setBusy(true); const text = prompt;
    try {
      const next = await (recover ? channel.current.recover() : channel.current.send(text));
      if (alive.current) {
        setValue(next); setPhase(next.phase); setPrompt("");
        setMessages(previous => [...previous.slice(-16), ...(!recover ? [{ who: "You" as const, text }] : []), { who: "PENTA AI", text: next.message }]);
        if (next.available) input.current?.focus();
      }
    } catch (error) {
      if (alive.current) { setValue(null); setMessages([]); setPrompt(""); setPhase(error instanceof TeacherDialogueUnavailable && error.access ? "access" : "pending"); }
    } finally { lock.current = false; if (alive.current) setBusy(false); }
  }
  const ready = phase === "ready" && !!value?.available && !busy;
  const fields = value?.draft && phase === "ready" ? JSON.parse(value.draft.canonicalPayload).fields as Record<string, { value: string }> : {};
  return <Foundation theme={theme === "light" ? "prism" : "orbit"} className={styles.screen}>
    <header className={styles.header}><div className={styles.brand}><span className={styles.mark}><Icon name="spark" /></span><div><strong>Academy Desk PENTA AI</strong><small>Executor · Teacher conversation</small></div></div>
      <nav aria-label="Workspace controls"><Link href="/penta">PENTA AI</Link><Link href="/teacher-onboarding" aria-label="Manual workspace">Manual</Link><Button onClick={() => setTheme(theme === "dark" ? "light" : "dark")}>{theme === "dark" ? "Light theme" : "Dark theme"}</Button></nav></header>
    <main className={styles.canvas}>
      <div className={styles.heading}><span>CONVERSATION WITH CONTROL</span><h1>Build the teacher draft together.</h1><p>Describe the details naturally. Review and approve the exact draft before anything is created.</p></div>
      <Notice title="Local validation · Test interpreter" tone="info">No qualified Mini is connected. Your academy independently verifies every action.</Notice>
      <div className={styles.grid}>
        <section className={styles.chat} aria-label="Teacher conversation" aria-busy={busy}>
          <div className={styles.chatHeader}><strong>Teacher onboarding</strong><span>{phase === "ready" ? "Private academy draft" : "Protected access"}</span></div>
          {phase === "loading" && <p role="status" className={styles.message}>Checking your current academy and saved draft…</p>}
          {phase === "access" && <Notice title="Current academy access could not be verified" tone="warning">Private details are hidden. Sign in to the correct academy or use Manual.</Notice>}
          {phase === "unavailable" && <Notice title="Teacher conversation is unavailable" tone="warning">No usable interpreter or protected draft is connected. Manual remains available.</Notice>}
          <div className={styles.messages} role="log" aria-label="Conversation messages" aria-live="polite" aria-relevant="additions">{messages.map((message, index) => <article key={index} className={message.who === "You" ? styles.user : styles.assistant}><strong>{message.who}</strong><p>{message.text}</p></article>)}<div ref={conversationEnd} /></div>
          {phase === "pending" && <div className={styles.pending}><Notice title="Teacher request is unconfirmed" tone="warning">Check the same request. The prompt will not be sent again, including after refresh.</Notice><Button disabled={busy} onClick={() => void act(true)}>Check saved teacher request</Button></div>}
          <form className={styles.composer} onSubmit={event => { event.preventDefault(); void act(false); }}><label htmlFor="teacher-prompt">Message PENTA AI about this teacher</label>
            <textarea id="teacher-prompt" ref={input} rows={3} maxLength={2000} value={prompt} disabled={!ready} placeholder="Tell me the teacher’s name, specialties or contact details…" onChange={event => setPrompt(event.target.value)} onKeyDown={event => { if (event.key === "Enter" && !event.shiftKey && !event.nativeEvent.isComposing) { event.preventDefault(); event.currentTarget.form?.requestSubmit(); } }} />
            <div><small>Enter to send · Shift + Enter for a new line</small><Button type="submit" variant="primary" disabled={!ready || !prompt.trim()}>{busy ? "Checking…" : "Send message"}</Button></div>
            <p>Prompts stay out of URL and saved browser storage. Sending never grants approval or creates a teacher.</p></form>
        </section>
        <aside className={styles.rail} aria-label="Verified teacher draft"><div className={styles.card}><h2>Current draft</h2>
          {value?.draft && phase === "ready" && !busy ? <><span className={styles.chip}>Revision {value.draft.revision} · {value.draft.status === "Reviewed" ? "Ready for review" : value.draft.status}</span>
            <dl>{Object.entries(fields).map(([key, field]) => <div key={key}><dt>{key.replace(/([a-z])([A-Z])/g, "$1 $2")}</dt><dd>{field.value || "Not provided"}</dd></div>)}</dl>
            {!Object.keys(fields).length && <p>The draft is empty. Start with the teacher’s name.</p>}
            <Link className={styles.review} href={`/penta/teachers?conversationId=${conversation}&draftId=${draft}`}>Open protected teacher review</Link></> : <p>Private draft details appear only after the host verifies the current result.</p>}
          <p>Approval and creation are separate actions in the protected review screen. Possible duplicates require Manual review.</p></div>
          <div className={styles.card}><h2>You stay in control</h2><p>Optional details may remain blank. Accounts, credentials, complex availability and compensation stay in Manual.</p><Link href="/teacher-onboarding">Open Manual teacher onboarding</Link></div></aside>
      </div>
    </main>
  </Foundation>;
}
