"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { useTheme } from "@/components/theme-provider";
import { isPortalSignOutEvent, portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent } from "@/lib/api";
import { loadOnboardingReview, OnboardingReviewUnavailable, watchOnboardingReviewExpiry, type OnboardingReviewRecovery } from "@/lib/penta-onboarding-review";
import type { Projection } from "@/vendor/penta-operational/frontend/operational-ui/src";
import OnboardingDialogueWorkspace from "./onboarding-dialogue-workspace";
import { Foundation, Button, Notice } from "@/vendor/penta-operational/frontend/shared-foundation/src";
import "@/vendor/penta-operational/frontend/operational-ui/src/workspace.css";
import styles from "./onboarding-review.module.css";

type State = { kind: "loading" } | { kind: "ready"; projection: Projection } |
  { kind: "blocked"; reason: OnboardingReviewUnavailable["reason"]; recovery?: OnboardingReviewRecovery;
    checking?: boolean; stillPending?: boolean };
const copy = {
  binding: "Open this review from an existing onboarding conversation. No conversation or draft is created by visiting this page.",
  access: "Current Academy Owner/Admin access could not be verified. Sign in to the correct workspace, or use Manual.",
  pending: "This conversation has a pending or unconfirmed request. No draft details are displayed and no request is repeated. Recheck the current host state before continuing.",
  unavailable: "The protected onboarding review is not enabled or connected here. No AI request was sent. Your Manual workspace remains available.",
  changed: "The draft changed, expired or could not be verified. Private details have been withheld. Recheck the current host state or use Manual; do not resubmit an unknown request.",
};

export default function OnboardingReviewWorkspace({ conversation, draft }: { conversation: string; draft: string }) {
  const router = useRouter();
  const { theme, setTheme } = useTheme();
  const [accent, setAccent] = useState<"orbit" | "aura">("orbit");
  const [attempt, setAttempt] = useState(1);
  const [state, setState] = useState<State>({ kind: "loading" });
  const recovery = useRef<OnboardingReviewRecovery | undefined>(undefined);
  const recovering = useRef(false);
  const appearance = theme === "light" ? "prism" : accent;
  useEffect(() => {
    const controller = new AbortController();
    const initialToken = portalAccessToken("AcademyAdmin");
    const loginEpoch = portalSessionEpoch("AcademyAdmin");
    let alive = true;
    recovery.current = undefined; recovering.current = false;
    const boot = async () => {
      try {
        const projection = await loadOnboardingReview(conversation, draft, crypto.randomUUID(), attempt, controller.signal);
        if (alive && !controller.signal.aborted) setState({ kind: "ready", projection });
      } catch (error) {
        if (alive && !controller.signal.aborted) {
          const unavailable = error instanceof OnboardingReviewUnavailable ? error : new OnboardingReviewUnavailable("changed");
          recovery.current = unavailable.recovery;
          setState({ kind: "blocked", reason: unavailable.reason, recovery: unavailable.recovery });
        }
      }
    };
    function clear() { controller.abort(); recovery.current = undefined; recovering.current = false; setState({ kind: "blocked", reason: "access" }); }
    function local(event: Event) { if ((event as CustomEvent).detail === "AcademyAdmin") clear(); }
    function changedToken() { if (portalAccessToken("AcademyAdmin") !== initialToken || portalSessionEpoch("AcademyAdmin") !== loginEpoch) clear(); }
    function storage(event: StorageEvent) {
      if (isPortalSignOutEvent(event, "AcademyAdmin")) clear(); else changedToken();
    }
    window.addEventListener(portalSignOutEvent, local);
    window.addEventListener(portalSessionChangedEvent, local);
    window.addEventListener("storage", storage);
    window.addEventListener("focus", changedToken);
    void boot();
    return () => { alive = false; controller.abort(); recovery.current = undefined; recovering.current = false; window.removeEventListener(portalSignOutEvent, local); window.removeEventListener(portalSessionChangedEvent, local); window.removeEventListener("storage", storage); window.removeEventListener("focus", changedToken); };
  }, [conversation, draft, attempt]);
  useEffect(() => {
    if (state.kind !== "ready") return;
    const projection = state.projection;
    return watchOnboardingReviewExpiry(projection, () => {
      recovery.current = undefined; recovering.current = false;
      // A disposed timer must never hide a newer independently verified view.
      setState(current => current.kind === "ready" && current.projection === projection ?
        { kind: "blocked", reason: "changed" } : current);
    });
  }, [state]);
  const checkPending = async () => {
    if (state.kind !== "blocked" || !state.recovery || recovery.current !== state.recovery || recovering.current) return;
    const handle = state.recovery; recovering.current = true;
    setState({ ...state, checking: true, stillPending: false });
    try {
      const result = await handle.check();
      if (recovery.current !== handle) return;
      if (result.kind === "pending") setState({ kind: "blocked", reason: "pending", recovery: handle, stillPending: true });
      else { recovery.current = undefined; setState({ kind: "ready", projection: result.projection }); }
    } catch (error) {
      if (recovery.current !== handle) return;
      recovery.current = undefined;
      setState({ kind: "blocked", reason: error instanceof OnboardingReviewUnavailable ? error.reason : "changed" });
    } finally { if (recovery.current === handle || recovery.current === undefined) recovering.current = false; }
  };
  const recheck = () => { setState({ kind: "loading" }); setAttempt(previous => previous + 1); };
  return <Foundation theme={appearance} className={styles.screen}>
    <header className={styles.header}><div><strong>Academy Desk PENTA AI</strong><span>Protected onboarding · {state.kind === "ready" &&
      state.projection.host.draft?.status === "succeeded" ? "Saved result verification" : state.kind === "ready" &&
      ["awaiting_approval", "approved"].includes(state.projection.host.draft?.status ?? "") ? "Host operation review" : "Read-only"}</span></div>
      <nav aria-label="Workspace controls"><Link href="/penta">Read chat</Link><Link href="/dashboard">Manual workspace</Link>
        <Button onClick={() => setTheme(theme === "dark" ? "light" : "dark")}>{theme === "dark" ? "Light theme" : "Dark theme"}</Button>
        <Button disabled={state.kind === "loading" || state.kind === "blocked" && state.checking} onClick={recheck}>Recheck host state</Button></nav></header>
    <section className={styles.canvas} aria-label="Protected onboarding workspace" aria-busy={state.kind === "loading"}>
      {state.kind === "ready" ? <OnboardingDialogueWorkspace key={state.projection.context_key} projection={state.projection} theme={appearance}
        onManual={() => router.push("/dashboard")} onNew={() => router.push("/penta")}
        onTheme={next => { if (next !== "prism") setAccent(next); setTheme(next === "prism" ? "light" : "dark"); }} /> : <div className={styles.blocked}><Notice title={state.kind === "loading" ? "Checking the current host context…" : "Onboarding actions are unavailable"}
        tone={state.kind === "loading" ? "info" : "warning"}>{state.kind === "loading" ? "Private draft details stay hidden until account, ownership, revision and payload checks pass." : copy[state.reason]}</Notice>
        {state.kind === "blocked" && state.reason === "access" && <Link href="/login">Sign in to your academy</Link>}
        {state.kind === "blocked" && state.recovery && <div>
          <Button disabled={state.checking} onClick={() => void checkPending()}>{state.checking ? "Checking the same request…" : "Check pending request"}</Button>
          {state.stillPending && <p role="status">The request is still pending. Nothing has been resubmitted and no student creation is confirmed.</p>}
        </div>}
        <p>Conversational sending, editing and confirmation remain disabled until their complete host adapters and model qualification are accepted.</p>
        <Link href="/student-onboarding">Open Manual student onboarding</Link></div>}
    </section>
  </Foundation>;
}
