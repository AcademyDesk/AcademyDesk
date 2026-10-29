"use client";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { useTheme } from "@/components/theme-provider";
import { portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent, isPortalSignOutEvent } from "@/lib/api";
import { loadTeacherStart, type TeacherStartHandle } from "@/lib/penta-teacher-start";
import { Foundation, Button, Notice } from "@/vendor/penta-operational/frontend/shared-foundation/src";
import styles from "./teacher-review.module.css";

export default function TeacherStartWorkspace() {
  const router = useRouter(), { theme, setTheme } = useTheme();
  const [state, setState] = useState<"loading" | "available" | "unavailable" | "unknown" | "access">("loading");
  const [busy, setBusy] = useState(false);
  const handle = useRef<TeacherStartHandle | null>(null), lock = useRef(false), alive = useRef(false);
  useEffect(() => {
    const controller = new AbortController(), bearer = portalAccessToken("AcademyAdmin"); alive.current = true;
    const loginEpoch = portalSessionEpoch("AcademyAdmin");
    function clear() { controller.abort(); handle.current = null; if (alive.current) setState("access"); }
    function local(event: Event) { if ((event as CustomEvent).detail === "AcademyAdmin") clear(); }
    function token() { if (portalAccessToken("AcademyAdmin") !== bearer || portalSessionEpoch("AcademyAdmin") !== loginEpoch) clear(); }
    function storage(event: StorageEvent) { if (isPortalSignOutEvent(event, "AcademyAdmin")) clear(); else token(); }
    window.addEventListener(portalSignOutEvent, local); window.addEventListener(portalSessionChangedEvent, local); window.addEventListener("storage", storage); window.addEventListener("focus", token);
    void loadTeacherStart(controller.signal).then(value => { if (alive.current && !controller.signal.aborted) { handle.current = value; setState(value.recovering ? "unknown" : value.available ? "available" : "unavailable"); } })
      .catch(() => { if (alive.current && !controller.signal.aborted) setState("unavailable"); });
    return () => { alive.current = false; controller.abort(); handle.current = null; window.removeEventListener(portalSignOutEvent, local); window.removeEventListener(portalSessionChangedEvent, local); window.removeEventListener("storage", storage); window.removeEventListener("focus", token); };
  }, []);
  async function start(readOnly: boolean) {
    if (lock.current || !handle.current || !alive.current || (readOnly ? state !== "unknown" : state !== "available")) return;
    lock.current = true; setBusy(true);
    try {
      const value = await (readOnly ? handle.current.check() : handle.current.start());
      if (alive.current) router.replace(`/penta/teachers/chat?conversationId=${value.conversationId}&draftId=${value.draftId}`);
    } catch { if (alive.current) setState(previous => previous === "access" ? "access" : "unknown"); }
    finally { lock.current = false; if (alive.current) setBusy(false); }
  }
  return <Foundation theme={theme === "light" ? "prism" : "orbit"} className={styles.screen}>
    <header className={styles.header}><div className={styles.brand}><div><strong>Academy Desk PENTA AI</strong><span>Teacher conversation · Start</span></div></div>
      <nav aria-label="Workspace controls"><Link href="/penta">PENTA AI</Link><Link href="/teacher-onboarding">Manual workspace</Link>
        <Button onClick={() => setTheme(theme === "dark" ? "light" : "dark")}>{theme === "dark" ? "Light theme" : "Dark theme"}</Button></nav></header>
    <section className={`${styles.canvas} ${styles.startEntry}`} aria-label="Teacher conversation start" aria-busy={busy || state === "loading"}>
      <h1>Start a teacher conversation</h1>
      <Notice title={state === "loading" ? "Checking current academy access…" : state === "available" ? "Protected start is available" :
        state === "unknown" ? "Conversation start is unconfirmed" : state === "access" ? "Current academy access could not be verified" : "Protected start is unavailable"}
        tone={state === "available" ? "info" : "warning"}>
        {state === "unknown" ? "Do not start another conversation. Check this same start request; nothing is resubmitted. This recovery link also works after refreshing this page while signed in to the same academy account." :
          "An explicit start creates an empty private draft, not a teacher. No AI prompt is sent and no approval is granted. This local candidate uses a test interpreter, not a qualified Mini model. The academy verifies the draft independently."}
      </Notice>
      {state === "available" && <Button disabled={busy} onClick={() => void start(false)}>Start teacher conversation</Button>}
      {state === "unknown" && <Button disabled={busy} onClick={() => void start(true)}>Check conversation start</Button>}
      {state === "access" && <p><Link href="/login">Sign in to your academy</Link></p>}
      <p>Your Manual workspace works independently of PENTA.</p><Link href="/teacher-onboarding">Open Manual teacher onboarding</Link>
    </section>
  </Foundation>;
}
