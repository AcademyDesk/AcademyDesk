"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { academyApi, type PortalWorkspace } from "@/lib/api";
import { validWorkspaceAccount, workspaceHomes } from "@/lib/workspace-entry";
import { ThemeToggle } from "@/components/theme-toggle";
import { usePentaManualTheme } from "@/components/penta-manual-theme";
import { StandardDetailModal } from "@/components/design-system/controls";
import styles from "./role-penta-entry.module.css";

type RoleWorkspace = Exclude<PortalWorkspace, "AcademyAdmin">;
const labels: Record<RoleWorkspace, string> = { Platform: "Platform Owner", Teacher: "Teacher", Portal: "Student / Guardian" };
const capabilities = [
  ["P", "Pulse", "Proactive briefings, alerts and priorities."],
  ["E", "Executor", "Authorized operational actions through conversation."],
  ["N", "Navigator", "Admissions, growth and opportunity guidance."],
  ["T", "Twin", "Source-backed academy context and explanations."],
  ["A", "Autopilot", "Explicitly approved, bounded recurring workflows."],
] as const;

/** Entry presentation only: does not grant a role, discover a tenant or call tools. */
export default function RolePentaEntry({ workspace }: { workspace: RoleWorkspace }) {
  const theme = usePentaManualTheme();
  const [state, setState] = useState<"checking" | "verified" | "signin" | "denied" | "unavailable">("checking");
  useEffect(() => {
    const controller = new AbortController();
    void (async () => {
      try {
        const response = await academyApi("/api/auth/session", { signal: controller.signal, cache: "no-store" });
        if (controller.signal.aborted) return;
        if (response.status === 401) { setState("signin"); return; }
        if (response.status === 403) { setState("denied"); return; }
        if (!response.ok) { setState("unavailable"); return; }
        const account: unknown = await response.json();
        if (!controller.signal.aborted) setState(validWorkspaceAccount(account) && account.workspace === workspace ? "verified" : "denied");
      } catch { if (!controller.signal.aborted) setState("unavailable"); }
    })();
    return () => controller.abort();
  }, [workspace]);
  return <main {...theme} className={`${theme.className} ${styles.page}`}>
    <header className={styles.top}>
      <div className={styles.brand}><span aria-hidden="true">P</span><div><strong>Academy Desk PENTA AI</strong><small>{labels[workspace]} workspace</small></div></div>
      <ThemeToggle />
      <nav className={styles.switcher} aria-label="Work view"><span aria-current="page">PENTA AI</span><Link href={workspaceHomes[workspace].manual}>Manual workspace</Link></nav>
    </header>
    <section className={styles.content}>
      <div className={styles.hero}><span className={styles.orb} aria-hidden="true">P</span><h1>Your workspace. AI first.</h1><p>One PENTA experience, with your Manual workspace always available. Features become usable only after their role-specific backend and safety checks are complete.</p></div>
      <section className={styles.panel} aria-labelledby="penta-role-status">
        <span className={styles.badge}>No AI actions enabled for this role</span>
        <h2 id="penta-role-status">{state === "checking" ? "Checking your workspace…" : state === "signin" ? "Sign in to continue" : state === "denied" ? "This workspace could not be verified" : state === "unavailable" ? "Workspace verification is unavailable" : `${labels[workspace]} conversation is not available yet`}</h2>
        <p role={state === "checking" || state === "verified" ? "status" : "alert"}>{state === "verified" ? "Your session is verified. This role has no released PENTA conversational tools yet. No request has been sent to a model and no records have been changed. Continue in Manual to work with your permitted data." : state === "checking" ? "Verifying the signed-in session. No academy records or AI tools are requested." : state === "signin" ? "Your session has ended or you are not signed in. Sign in again to continue." : state === "denied" ? "The signed-in account does not match this role workspace. No protected data or tools have been loaded." : "We could not check your session. Try again later, or open Manual; its normal access checks still apply."}</p>
        {state === "signin" || state === "denied" ? <Link className={styles.open} href="/login">Sign in</Link> : <Link className={styles.open} href={workspaceHomes[workspace].manual}>Open Manual workspace</Link>}
      </section>
      <section className={styles.capabilities} aria-label="PENTA capability information">{capabilities.map(([letter, name, description]) => <details key={letter}><summary><span aria-hidden="true">{letter}</span>{name} ⓘ</summary><p>{description}</p><small>Planned for this role. Not an enabled action.</small></details>)}</section>
    </section>
  </main>;
}

/** Manual remains the existing role portal. This link never enables AI tools. */
export function RoleManualModeBar({ workspace }: { workspace: RoleWorkspace }) {
  const theme = usePentaManualTheme();
  const router = useRouter();
  const edited = useRef(false);
  const [confirmLeave, setConfirmLeave] = useState(false);
  useEffect(() => {
    // Conservative mode-link guard, not a new form persistence system. Pointer
    // edits cover custom date/select controls that update hidden form values.
    const mark = (event: Event) => { if (event.target instanceof Element && event.target.closest("form")) edited.current = true; };
    document.addEventListener("input", mark, true);
    document.addEventListener("change", mark, true);
    document.addEventListener("pointerdown", mark, true);
    return () => { document.removeEventListener("input", mark, true); document.removeEventListener("change", mark, true); document.removeEventListener("pointerdown", mark, true); };
  }, []);
  return <nav className={`penta-ui pui-academy-mapping ${styles.modeBar}`} data-penta-theme={theme["data-penta-theme"]} aria-label="Work view">
    <strong>Academy Desk PENTA AI</strong><div className={styles.switcher}><Link href={workspaceHomes[workspace].ai} onClick={event => {
      if (edited.current && document.querySelector("form")) { event.preventDefault(); setConfirmLeave(true); }
    }}>PENTA AI</Link><span aria-current="page">Manual workspace</span></div>
    {confirmLeave && <StandardDetailModal title="Leave Manual workspace?" eyebrow="Unsaved form safeguard" onClose={() => setConfirmLeave(false)}>
      <div className={styles.leave}><p>This page contains a form you interacted with. Unsaved changes may be lost when you open PENTA AI. Stay in Manual to finish or save your work.</p><button type="button" onClick={() => setConfirmLeave(false)}>Stay in Manual</button><button type="button" onClick={() => router.push(workspaceHomes[workspace].ai)}>Leave and open PENTA AI</button></div>
    </StandardDetailModal>}
  </nav>;
}
