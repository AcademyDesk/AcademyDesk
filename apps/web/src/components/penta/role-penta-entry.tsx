"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { academyApi, portalAccessToken, portalSessionEpoch, type PortalWorkspace } from "@/lib/api";
import { validWorkspaceAccount, workspaceHomes } from "@/lib/workspace-entry";
import { validPentaAccount } from "@/lib/penta-chat-contract";
import FinanceChat from "./finance-chat";
import MiniConversation from "./mini-conversation";
import TeacherAttendanceConversation from "./teacher-attendance-conversation";
import PortalConversation from "./portal-conversation";
import PentaWorkspaceShell from "./penta-workspace-shell";
import { usePentaManualTheme } from "@/components/penta-manual-theme";
import styles from "./role-penta-entry.module.css";

type RoleWorkspace = PortalWorkspace;
const labels: Record<RoleWorkspace, string> = { AcademyAdmin: "Academy administration", Platform: "Platform Owner", Teacher: "Teacher", Portal: "Student / Guardian" };

/** Entry presentation only: does not grant a role, discover a tenant or call tools. */
export default function RolePentaEntry({ workspace }: { workspace: RoleWorkspace }) {
  const [check, setCheck] = useState(0);
  const [financeAcademy, setFinanceAcademy] = useState<string | null>(null);
  const [portalAcademy, setPortalAcademy] = useState<string | null>(null);
  const [state, setState] = useState<"checking" | "verified" | "signin" | "denied" | "unavailable">("checking");
  useEffect(() => {
    const controller = new AbortController();
    const accessToken = portalAccessToken(workspace);
    const epoch = portalSessionEpoch(workspace);
    const sameLogin = () => portalSessionEpoch(workspace) === epoch && Boolean(portalAccessToken(workspace)) && (epoch !== null || portalAccessToken(workspace) === accessToken);
    void (async () => {
      try {
        const response = await academyApi("/api/auth/session", { signal: controller.signal, cache: "no-store" });
        if (controller.signal.aborted) return;
        if (!sameLogin()) { setState("unavailable"); return; }
        if (response.status === 401) { setState("signin"); return; }
        if (response.status === 403) { setState("denied"); return; }
        if (!response.ok) { setState("unavailable"); return; }
        const account: unknown = await response.json();
        if (controller.signal.aborted) return;
        if (!sameLogin()) { setState("unavailable"); return; }
        setFinanceAcademy(workspace === "AcademyAdmin" && validPentaAccount(account) ? account.academyId : null);
        setPortalAcademy((workspace === "Teacher" || workspace === "Portal") && validWorkspaceAccount(account) && account.workspace === workspace &&
          "academyId" in account && typeof account.academyId === "string" ? account.academyId : null);
        setState(validWorkspaceAccount(account) && account.workspace === workspace ? "verified" : "denied");
      } catch { if (!controller.signal.aborted) setState("unavailable"); }
    })();
    return () => controller.abort();
  }, [workspace, check]);
  const fallback = <section className={styles.panel} aria-labelledby="penta-role-status">
    <span className={styles.badge}>Conversations not available</span>
    <h2 id="penta-role-status">{state === "checking" ? "Checking your workspace…" : state === "signin" ? "Sign in to continue" : state === "denied" ? "This workspace could not be verified" : state === "unavailable" ? "Workspace verification is unavailable" : `${labels[workspace]} conversation is not available yet`}</h2>
    <p role={state === "checking" || state === "verified" ? "status" : "alert"}>{state === "verified" ? "Your workspace is verified. Open Manual to continue with your permitted work." : state === "checking" ? "Checking your signed-in workspace." : state === "signin" ? "Your session has ended or you are not signed in. Sign in again to continue." : state === "denied" ? "Sign in with an account for this workspace." : "We could not check your session. Check your workspace again, or open Manual; its normal access checks still apply."}</p>
    {state === "signin" || state === "denied" ? <Link className={styles.open} href="/login">Sign in</Link> : <Link className={styles.open} href={workspaceHomes[workspace].manual}>Open Manual workspace</Link>}
    {state === "unavailable" && <button type="button" className={styles.retry} onClick={() => { setState("checking"); setCheck(value => value + 1); }}>Check workspace again</button>}
  </section>;
  const miniEvaluation = state === "verified" && financeAcademy !== null && process.env.NEXT_PUBLIC_PENTA_MINI_EVALUATION === "true";
  return <PentaWorkspaceShell workspace={workspace} verified={state === "verified"} miniEvaluation={miniEvaluation}
    workflows={state === "verified" && financeAcademy ? <><Link href="/penta/enrollments">Enrollment assistant</Link><Link href="/penta/onboarding">Student onboarding</Link><Link href="/penta/teachers/chat">Teacher onboarding</Link><Link href="/penta/scheduling">Class scheduling</Link></> : undefined}>
      {miniEvaluation && financeAcademy && <MiniConversation key={financeAcademy + "-mini"} academyId={financeAcademy} />}
      {state === "verified" && financeAcademy
        ? miniEvaluation
          ? <details className={styles.guided}><summary>More supported workflows <small>Host-guided</small></summary><FinanceChat key={financeAcademy} academyId={financeAcademy} fallback={fallback} /></details>
          : <FinanceChat key={financeAcademy} academyId={financeAcademy} fallback={fallback} />
        : state === "verified" && portalAcademy
          ? workspace === "Teacher"
            ? <TeacherAttendanceConversation key={portalAcademy} academyId={portalAcademy} fallback={fallback} />
            : <PortalConversation key={portalAcademy} academyId={portalAcademy} fallback={fallback} />
          : fallback}
  </PentaWorkspaceShell>;
}

/** Manual remains the existing role portal. This link never enables AI tools. */
export function RoleManualModeBar({ workspace }: { workspace: RoleWorkspace }) {
  const theme = usePentaManualTheme();
  const router = useRouter();
  const edited = useRef(false);
  const modeLink = useRef<HTMLAnchorElement>(null);
  const leaveDialog = useRef<HTMLDialogElement>(null);
  const [confirmLeave, setConfirmLeave] = useState(false);
  useEffect(() => {
    const dialog = leaveDialog.current;
    if (!confirmLeave || !dialog) return;
    const modeAnchor = modeLink.current;
    dialog.showModal();
    return () => {
      if (dialog.open) dialog.close();
      modeAnchor?.focus();
    };
  }, [confirmLeave]);
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
    <strong>Academy Desk PENTA AI</strong><div className={styles.switcher}><Link ref={modeLink} href={workspaceHomes[workspace].ai} onClick={event => {
      if (edited.current && document.querySelector("form")) { event.preventDefault(); setConfirmLeave(true); }
    }}>PENTA AI</Link><span aria-current="page">Manual workspace</span></div>
    {confirmLeave && <dialog ref={leaveDialog} className={styles.leave} aria-labelledby="manual-mode-leave-title" onCancel={event => { event.preventDefault(); setConfirmLeave(false); }}>
      <h2 id="manual-mode-leave-title">Leave Manual workspace?</h2>
      <p>This page contains a form you interacted with. Unsaved changes may be lost when you open PENTA AI. Stay in Manual to finish or save your work.</p><button type="button" onClick={() => setConfirmLeave(false)}>Stay in Manual</button><button type="button" onClick={() => router.push(workspaceHomes[workspace].ai)}>Leave and open PENTA AI</button>
    </dialog>}
  </nav>;
}
