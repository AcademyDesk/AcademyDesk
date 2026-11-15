"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { ThemeToggle } from "@/components/theme-toggle";
import { usePentaManualTheme } from "@/components/penta-manual-theme";
import { apiUrl, isPortalSignOutEvent, portalAccessToken, portalSessionChangedEvent, portalSessionEpoch, portalSignOutEvent } from "@/lib/api";
import { readPentaJson } from "@/lib/penta-onboarding-channel";
import { validWorkspaceAccount } from "@/lib/workspace-entry";
import { validPentaAccount } from "@/lib/penta-chat-contract";
import { choiceName, createClassScheduling, deliveryModes, initialSchedulingState, schedulingPrompt, schedulingSlot, type DeliveryMode, type ScheduleChoice, type SlotInput } from "@/lib/penta-class-scheduling";
import type { EnrollmentTransport } from "@/lib/penta-enrollment-creation";
import styles from "./enrollment-assistant.module.css";

const initialSlot: SlotInput = { start: "", end: "", mode: "InPerson", location: "" };
const modeLabel = (mode: DeliveryMode) => mode === "InPerson" ? "Offline" : mode;
const clock = (value: string) => value.slice(0, 10) + " " + value.slice(11, 16) + " UTC";

function SchedulingConversation({ transport }: { transport: EnrollmentTransport }) {
  const driver = useRef<ReturnType<typeof createClassScheduling> | null>(null);
  const [view, setView] = useState(initialSchedulingState);
  const [prompt, setPrompt] = useState("");
  const [sent, setSent] = useState("");
  const [selected, setSelected] = useState<ScheduleChoice | null>(null);
  const [slot, setSlot] = useState(initialSlot);
  const [inputError, setInputError] = useState("");
  const [manualChecked, setManualChecked] = useState(false);
  useEffect(() => {
    const operation = createClassScheduling(transport, () => window.sessionStorage, setView);
    driver.current = operation;
    void operation.initialise();
    return () => { operation.dispose(); driver.current = null; };
  }, [transport]);

  const ready = view.ready && !view.pending && !view.key;
  const draft = view.draft;
  const hasSelection = selected && view.matches?.rows.includes(selected);
  const verifyInManual = view.phase === "missing" || view.phase === "expired";
  return <section aria-label="Class scheduling conversation" aria-busy={view.pending}>
    {view.notice && <p role="status">{view.notice}</p>}
    {view.phase === "unavailable" && <p><Link href="/sessions">Open Manual Sessions</Link></p>}
    {view.ready && !view.key && <>
      <div role="log" aria-label="Scheduling messages">
        <p><strong>Executor</strong> · Which batch would you like to schedule or reschedule?</p>
        {sent && <p><strong>You</strong> · {sent}</p>}
      </div>
      <form onSubmit={e => {
        e.preventDefault();
        if (!ready) return;
        try {
          const request = schedulingPrompt(prompt);
          setInputError(""); setSelected(null); setSlot(initialSlot); setSent(prompt.trim()); setPrompt("");
          void driver.current?.search(request.kind, request.query);
        } catch (error) { setInputError((error as Error).message); }
      }}>
        <label htmlFor="scheduling-prompt">Message Executor about a class</label>
        <div className={styles.actions}>
          <input id="scheduling-prompt" maxLength={110} value={prompt} disabled={!ready}
            placeholder="Schedule a class for Piano" onChange={e => setPrompt(e.currentTarget.value)} />
          <button type="submit" disabled={!ready || !prompt.trim()}>Send message</button>
        </div>
        <p>Try “Schedule a class for Piano” or “Reschedule a class for Piano”.</p>
      </form>
      {view.matches && <section className={styles.result} aria-label="Matching academy records">
        <h2>{view.matches.kind === "create" ? "Choose a batch" : "Choose the class to move"}</h2>
        {!view.matches.rows.length && <p>No matching records. Try a different batch name.</p>}
        <ul>{view.matches.rows.map(row => <li key={row.sourceId}>
          <span>{choiceName(row)}{"startUtc" in row ? " · " + clock(row.startUtc) : ""}</span>
          <button type="button" disabled={!ready} aria-pressed={selected === row} onClick={() => {
            setSelected(row); setInputError(""); setSlot(initialSlot);
          }}>Select {choiceName(row)}{"startUtc" in row ? " at " + clock(row.startUtc) : ""}</button>
        </li>)}</ul>
        {view.matches.hasMore && <p>More matches exist. Refine the batch name to find the right record.</p>}
      </section>}
      {hasSelection && selected && <form className={styles.result} aria-label="Proposed class slot" onSubmit={e => {
        e.preventDefault();
        if (!ready) return;
        try {
          schedulingSlot(slot, Date.now());
          setInputError("");
          void driver.current?.prepare(selected, slot);
        } catch (error) { setInputError((error as Error).message); }
      }}>
        <h2>When should {choiceName(selected)} meet?</h2>
        <div className={styles.details}>
          <div><label htmlFor="scheduling-start">Start (UTC)</label><input id="scheduling-start" type="datetime-local" required value={slot.start}
            disabled={!ready} onChange={e => setSlot({ ...slot, start: e.currentTarget.value })} /></div>
          <div><label htmlFor="scheduling-end">End (UTC)</label><input id="scheduling-end" type="datetime-local" required value={slot.end}
            disabled={!ready} onChange={e => setSlot({ ...slot, end: e.currentTarget.value })} /></div>
        </div>
        <label htmlFor="scheduling-mode">Delivery mode</label>
        <select id="scheduling-mode" value={slot.mode} disabled={!ready} onChange={e => {
          const mode = e.currentTarget.value as DeliveryMode;
          if (deliveryModes.includes(mode)) setSlot({ ...slot, mode, location: "" });
        }}>{deliveryModes.map(mode => <option key={mode} value={mode}>{modeLabel(mode)}</option>)}</select>
        <label htmlFor="scheduling-location">{slot.mode === "InPerson" ? "Room (optional)" : "HTTPS meeting link"}</label>
        <input id="scheduling-location" value={slot.location} maxLength={120} required={slot.mode !== "InPerson"}
          disabled={!ready} onChange={e => setSlot({ ...slot, location: e.currentTarget.value })} />
        <div className={styles.actions}><button type="submit" disabled={!ready}>Review class schedule</button></div>
      </form>}
    </>}
    {inputError && <p role="alert">{inputError}</p>}
    {draft && <section className={styles.result} aria-label="Verified scheduling review">
      <h2>{draft.status === "Succeeded" ? (draft.source ? "Class rescheduled" : "Class scheduled") : draft.source ? "Review class reschedule" : "Review new class"}</h2>
      <dl className={styles.details}>
        <div><dt>Batch reference</dt><dd><code>{draft.plan.batchId}</code></dd></div>
        {draft.source && <div><dt>Original slot</dt><dd>{clock(draft.source.startUtc)} to {clock(draft.source.endUtc)}</dd>
          <dt>Original delivery and location</dt><dd>{modeLabel(draft.source.deliveryMode)} · {draft.source.roomName ?? "Unassigned"}</dd></div>}
        <div><dt>Proposed slot</dt><dd>{clock(draft.plan.startUtc)} to {clock(draft.plan.endUtc)}</dd></div>
        <div><dt>Delivery mode</dt><dd>{modeLabel(draft.plan.deliveryMode)}</dd></div>
        <div><dt>{draft.plan.deliveryMode === "InPerson" ? "Room" : "Meeting link"}</dt><dd>{draft.plan.roomName ?? "Unassigned"}</dd></div>
        <div><dt>Assigned teacher</dt><dd><code>{draft.plan.teacherId ?? "Unassigned"}</code></dd></div>
        <div><dt>Branch</dt><dd><code>{draft.plan.branchId ?? "Unassigned"}</code></dd></div>
      </dl>
      {draft.status === "Succeeded" ? <p role="status">Verified receipt <code>{draft.receipt.receiptId}</code> Class reference <code>{draft.receipt.classSessionId}</code></p>
        : <p>{draft.status === "Prepared" ? "Approval is required before the class is changed." : "Approved. Confirm execution to save the change."}</p>}
      <div className={styles.actions}>
        {draft.status === "Prepared" && <button type="button" disabled={view.pending} onClick={() => void driver.current?.advance(draft, "approve")}>Approve exact schedule</button>}
        {draft.status === "Authorized" && <button type="button" disabled={view.pending} onClick={() => void driver.current?.advance(draft, "execute")}>{draft.source ? "Save reschedule" : "Schedule class"}</button>}
      </div>
    </section>}
    {view.key && <div className={styles.actions}>
      <button type="button" disabled={!view.ready || view.pending} onClick={() => void driver.current?.recover()}>Check saved scheduling request</button>
      {verifyInManual && <label><input type="checkbox" checked={manualChecked} onChange={e => setManualChecked(e.currentTarget.checked)} /> I checked Manual Sessions for this request</label>}
      {view.phase !== "unknown" && <button type="button" disabled={!view.ready || view.pending || (verifyInManual && !manualChecked)} onClick={() => {
        setSent(""); setSelected(null); setInputError(""); setManualChecked(false); void driver.current?.close();
      }}>{draft?.status === "Succeeded" ? "Start a new request" : "Dismiss this review"}</button>}
    </div>}
  </section>;
}

export default function ClassScheduling() {
  const theme = usePentaManualTheme();
  const [transport, setTransport] = useState<EnrollmentTransport | null>(null);
  const [notice, setNotice] = useState("Checking your academy access…");
  useEffect(() => {
    const workspace = "AcademyAdmin";
    const controller = new AbortController(), bearer = portalAccessToken(workspace), epoch = portalSessionEpoch(workspace);
    const current = () => !controller.signal.aborted && !!bearer && !!epoch &&
      portalAccessToken(workspace) === bearer && portalSessionEpoch(workspace) === epoch;
    function clear() {
      if (controller.signal.aborted) return;
      controller.abort();
      setTransport(null);
      setNotice("Your academy access could not be verified. Sign in again or open Manual Sessions.");
    }
    async function send(path: string, method: "GET" | "PUT" | "POST", body?: unknown) {
      if (!current()) { clear(); throw Error("Session changed."); }
      const response = await fetch(apiUrl + path, {
        method, cache: "no-store", credentials: "omit", redirect: "error", referrerPolicy: "no-referrer", signal: controller.signal,
        headers: { Authorization: `Bearer ${bearer}`, Accept: "application/json", ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
        body: body === undefined ? undefined : JSON.stringify(body)
      });
      if (!current() || response.status === 401 || response.status === 403) { clear(); throw Error("Access changed."); }
      return response;
    }
    function changed(e: Event) { if ((e as CustomEvent).detail === workspace) clear(); }
    function recheck() { if (!current()) clear(); }
    function storage(e: StorageEvent) { if (isPortalSignOutEvent(e, workspace)) clear(); else recheck(); }
    window.addEventListener(portalSessionChangedEvent, changed);
    window.addEventListener(portalSignOutEvent, changed);
    window.addEventListener("storage", storage);
    window.addEventListener("focus", recheck);
    void (async () => {
      try {
        const response = await send("/api/auth/session", "GET");
        if (response.status !== 200) throw Error("Session unavailable.");
        const account = await readPentaJson(response, controller.signal);
        if (!validWorkspaceAccount(account) || account.workspace !== workspace || !validPentaAccount(account)) throw Error("Unsupported account.");
        const root = `/api/academies/${account.academyId}/penta/class-scheduling/browser`;
        if (current()) { setTransport({ academyId: account.academyId, epoch, signal: controller.signal, current, invalidate: clear,
          send: (path, method, body) => send(root + path, method, body) }); setNotice(""); }
      } catch { clear(); }
    })();
    return () => {
      controller.abort();
      window.removeEventListener(portalSessionChangedEvent, changed);
      window.removeEventListener(portalSignOutEvent, changed);
      window.removeEventListener("storage", storage);
      window.removeEventListener("focus", recheck);
    };
  }, []);
  return <main {...theme} className={`${theme.className} ${styles.page}`}>
    <header className={styles.top}><Link href="/penta">Academy Desk PENTA AI</Link><ThemeToggle /><Link href="/sessions">Manual Sessions</Link></header>
    <section className={styles.panel} aria-labelledby="scheduling-heading">
      <h1 id="scheduling-heading">Class scheduling</h1>
      <p className={styles.boundary}>Executor preview · Guided class scheduling</p>
      {notice && <p role="status">{notice}</p>}
      {transport && <SchedulingConversation transport={transport} />}
    </section>
  </main>;
}
