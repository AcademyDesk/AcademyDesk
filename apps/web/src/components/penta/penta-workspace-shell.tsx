"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type ReactNode } from "react";
import type { PortalWorkspace } from "@/lib/api";
import { workspaceHomes } from "@/lib/workspace-entry";
import { ThemeToggle } from "@/components/theme-toggle";
import { usePentaManualTheme } from "@/components/penta-manual-theme";
import { Modal } from "@/vendor/penta-operational/frontend/shared-foundation/src";
import { PentaGlyph, type PentaGlyphName } from "./penta-glyph";
import s from "./penta-workspace-shell.module.css";

const capabilities: { name: string; icon: PentaGlyphName; tagline: string; description: string }[] = [
  { name: "Pulse", icon: "pulse", tagline: "See what matters", description: "Source-backed briefings, priorities and alerts. This capability is not connected in the current workspace." },
  { name: "Executor", icon: "chat", tagline: "Bring work into focus", description: "Work through a conversation using currently permitted host tools. The local Mini evaluation supports learner search and follow-up only. Guided workflows have their own host validation and approvals." },
  { name: "Navigator", icon: "compass", tagline: "Find the next opportunity", description: "Admissions, capacity and growth guidance. This capability is not connected in the current workspace." },
  { name: "Twin", icon: "twin", tagline: "Understand your academy", description: "A source-backed view across your academy. This capability is not connected in the current workspace." },
  { name: "Autopilot", icon: "play", tagline: "Approved work, in motion", description: "Bounded recurring work under explicit policies and approvals. Automation is not enabled in the current workspace." },
];
const labels: Record<PortalWorkspace, string> = { AcademyAdmin: "Academy administration", Platform: "Platform Owner", Teacher: "Teacher workspace", Portal: "Student / Guardian" };

/** The shell never selects a tenant, grants a tool, or invents host history/KPIs. */
export default function PentaWorkspaceShell({ workspace, verified, miniEvaluation, children, workflows }: {
  workspace: PortalWorkspace; verified: boolean; miniEvaluation: boolean; children: ReactNode; workflows?: ReactNode;
}) {
  const theme = usePentaManualTheme();
  const [overlay, setOverlay] = useState<"navigation" | "context" | null>(null);
  const [help, setHelp] = useState<string | null>(null);
  const helpTrigger = useRef<HTMLButtonElement | null>(null);
  const conversation = useRef<HTMLDivElement>(null);
  useEffect(() => { if (help === null) helpTrigger.current?.focus(); }, [help]);
  const focusConversation = () => {
    setOverlay(null);
    requestAnimationFrame(() => {
      const target = conversation.current?.querySelector<HTMLElement>("textarea:not(:disabled), input:not(:disabled), button:not(:disabled), a[href]");
      (target ?? conversation.current)?.focus();
    });
  };
  const navigation = <>
    <p className={s.eyebrow}>YOUR WORKSPACE</p>
    <button type="button" className={s.navActive} onClick={focusConversation}><PentaGlyph name="chat" />Conversation</button>
    <Link href={workspaceHomes[workspace].manual}><PentaGlyph name="grid" />Manual workspace</Link>
    <button type="button" onClick={() => setOverlay("context")}><PentaGlyph name="panel" />Workspace context</button>
    <div className={s.sidebarNote}><span className={s.sessionDot} />Conversation recovery<p>Saved responses and history are available when supported by your connected workspace.</p></div>
    {workflows && <div className={s.workflowLinks}><p className={s.eyebrow}>PROTECTED WORKFLOWS</p>{workflows}</div>}
  </>;
  const context = <>
    <div className={s.contextHeading}><PentaGlyph name="panel" /><h2>Workspace context</h2></div>
    <div className={s.contextCard}><span className={s.contextIcon}><PentaGlyph name="chat" /></span><h3>Executor</h3><p>Start with a question. Review the source. Take the next step.</p></div>
    <div className={s.contextSection}><p className={s.eyebrow}>CURRENT CONNECTION</p><strong>{verified ? labels[workspace] : "Checking your workspace"}</strong><p>{miniEvaluation ? "Private Mini · local read-only evaluation" : "Host-connected guided workspace"}</p><small>Availability and permissions are checked by Academy Desk for every request.</small></div>
    <div className={s.contextSection}><p className={s.eyebrow}>BEFORE AN ACTION</p><p>Review the exact details and required approval. A saved host receipt confirms the result.</p><Link href={workspaceHomes[workspace].manual}>Continue in Manual <span aria-hidden="true">↗</span></Link></div>
    <div className={s.contextFooter}>Your academy data stays within its authorized workspace.</div>
  </>;
  return <main {...theme} className={`${theme.className} ${s.workspace}`}>
    <a className={s.skip} href="#penta-conversation">Skip to conversation</a>
    <header className={s.header}>
      <div className={s.brand}><span className={s.mark}><PentaGlyph name="spark" /></span><div><strong>PENTA AI</strong><small>Academy Desk</small></div></div>
      <button className={s.mobileMenu} type="button" onClick={() => setOverlay("navigation")} aria-label="Open workspace navigation"><PentaGlyph name="grid" /></button>
      <div className={s.workspaceIdentity}><span className={s.sessionDot} /><span>{labels[workspace]}<small>{verified ? "Workspace verified" : "Verifying access"}</small></span></div>
      <nav className={s.mode} aria-label="Work view"><span aria-current="page">PENTA AI</span><Link href={workspaceHomes[workspace].manual}>Manual workspace</Link></nav>
      <ThemeToggle />
      <button className={s.contextToggle} type="button" onClick={() => setOverlay("context")} aria-label="Open workspace context"><PentaGlyph name="panel" /></button>
    </header>
    <aside className={s.sidebar} aria-label="Workspace navigation">{navigation}<div className={s.sidebarFooter}><span className={s.avatar}>{workspace === "Platform" ? "PO" : workspace === "Teacher" ? "T" : workspace === "Portal" ? "SG" : "AD"}</span><span>{labels[workspace]}<small>Role-scoped access</small></span></div></aside>
    <div className={s.main}>
      <nav className={s.capabilities} aria-label="PENTA capabilities">{capabilities.map(capability => <div className={s.capability} key={capability.name} data-selected={capability.name === "Executor"}>
        <button type="button" className={s.capabilitySelect} disabled={capability.name !== "Executor" || !verified} aria-pressed={capability.name === "Executor"} onClick={focusConversation}>
          <PentaGlyph name={capability.icon} /><span><strong>{capability.name}</strong><small>{capability.tagline}</small></span>
        </button><button type="button" className={s.info} aria-label={`About ${capability.name}`} onClick={event => { helpTrigger.current = event.currentTarget; setHelp(capability.name); }}>!</button>
      </div>)}</nav>
      <div className={s.canvas}>
        <section className={s.conversation} aria-label="PENTA workspace conversation"><div className={s.conversationHeading}><span className={s.eyebrow}>CONVERSATION FIRST</span><h1>{workspace === "Platform" ? "Your platform, in focus." : workspace === "Teacher" ? "Your teaching, in focus." : workspace === "Portal" ? "Your learning, in focus." : "Your academy, in focus."}</h1><p>Ask a question. Bring the next step into focus.</p></div><div id="penta-conversation" ref={conversation} tabIndex={-1} className={s.conversationBody}>{children}</div></section>
        <aside className={s.context} aria-label="Workspace context">{context}</aside>
      </div>
    </div>
    <Modal open={overlay === "navigation"} title="Your workspace" onClose={() => setOverlay(null)} drawer><nav className={s.mobileNavigation}>{navigation}</nav></Modal>
    <Modal open={overlay === "context"} title="Workspace context" onClose={() => setOverlay(null)} drawer><div className={s.contextDrawer}>{context}</div></Modal>
    <Modal open={help !== null} title={help ?? "PENTA capability"} onClose={() => setHelp(null)}><p>{capabilities.find(capability => capability.name === help)?.description}</p><p>Capability names do not grant permissions or enable an unavailable tool.</p></Modal>
  </main>;
}
