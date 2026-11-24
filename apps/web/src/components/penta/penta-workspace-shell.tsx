"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import { portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent, isPortalSignOutEvent, type PortalWorkspace } from "@/lib/api";
import { workspaceHomes } from "@/lib/workspace-entry";
import { ThemeToggle } from "@/components/theme-toggle";
import { usePentaManualTheme } from "@/components/penta-manual-theme";
import { Modal } from "@/vendor/penta-operational/frontend/shared-foundation/src";
import { PentaGlyph, type PentaGlyphName } from "./penta-glyph";
import { queryEntityLabel } from "@/lib/penta-query-conversation";
import { PentaWorkspaceContext, type PentaWorkspaceSnapshot } from "./penta-workspace-context";
import s from "./penta-workspace-shell.module.css";

const capabilities: { name: string; icon: PentaGlyphName; tagline: string; description: string }[] = [
  { name: "Pulse", icon: "pulse", tagline: "See what matters", description: "Source-backed briefings, priorities and alerts. This capability is not connected in the current workspace." },
  { name: "Executor", icon: "chat", tagline: "Turn intent into action", description: "Work through a conversation using currently permitted host tools. Connected records and provider availability are checked by the host. Guided mutations retain their own validation and approvals; a capability or model selection cannot grant permission." },
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
  const [section, setSection] = useState<"connections" | "review" | "models" | "new" | null>(null);
  const [projection, setProjection] = useState<PentaWorkspaceSnapshot | null>(null);
  const projectionEpoch = useRef<string | null>(null);
  const publish = useCallback((value: PentaWorkspaceSnapshot | null) => {
    setProjection(value && projectionEpoch.current && projectionEpoch.current===portalSessionEpoch(workspace) && portalAccessToken(workspace) ? value : null);
  }, [workspace]);
  useEffect(()=>{
    projectionEpoch.current=portalSessionEpoch(workspace);
    const clear=()=>{projectionEpoch.current=null;setProjection(null);};
    const changed=(event:Event)=>{if((event as CustomEvent).detail===workspace)clear();};
    const stored=(event:StorageEvent)=>{if(isPortalSignOutEvent(event,workspace)||portalSessionEpoch(workspace)!==projectionEpoch.current)clear();};
    window.addEventListener(portalSessionChangedEvent,changed);window.addEventListener(portalSignOutEvent,changed);window.addEventListener("storage",stored);
    return()=>{projectionEpoch.current=null;window.removeEventListener(portalSessionChangedEvent,changed);window.removeEventListener(portalSignOutEvent,changed);window.removeEventListener("storage",stored);};
  },[workspace]);
  const snapshot = verified ? projection : null;
  const latest = snapshot?.turns.at(-1);
  const providerStatus = !miniEvaluation ? "Host-guided workspace" : snapshot === null ? "Provider status not reported" : snapshot.phase === "checking" ? "Checking provider" : snapshot.available ? "Read provider available" : "Model connection unavailable";
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
    <button type="button" className={s.newConversation} disabled={!snapshot?.available || snapshot.pending} onClick={() => {setOverlay(null);setSection("new");}}><span aria-hidden="true">＋</span> New conversation</button>
    <p className={s.eyebrow}>YOUR WORKSPACE</p>
    <button type="button" className={s.navActive} onClick={focusConversation}><PentaGlyph name="chat" />Conversation</button>
    <Link href={workspaceHomes[workspace].manual}><PentaGlyph name="grid" />Manual workspace</Link>
    <button type="button" onClick={() => setOverlay("context")}><PentaGlyph name="panel" />Workspace context</button>
    <button type="button" onClick={() => { setOverlay(null); setSection("connections"); }}><PentaGlyph name="connections" />Connections</button>
    <button type="button" onClick={() => { setOverlay(null); setSection("review"); }}><PentaGlyph name="receipt" />Review queue</button>
    <button type="button" className={s.mobileModel} onClick={() => {setOverlay(null);setSection("models");}}><PentaGlyph name="spark" />Model · MINI</button>
    <div className={s.recent}><p className={s.eyebrow}>CURRENT CONVERSATION</p>{snapshot?.turns.length ? snapshot.turns.slice(-6).map(turn => <button type="button" key={turn.requestId} onClick={() => {
      setOverlay(null); requestAnimationFrame(() => document.getElementById("penta-turn-" + turn.requestId)?.scrollIntoView({ block: "start", behavior: "smooth" }));
    }}><PentaGlyph name="chat" /><span>{turn.inputText}</span></button>) : <p>Verified responses will appear here as you work.</p>}{snapshot?.hasMore && <small>Showing recent responses from this conversation.</small>}</div>
    {workflows && <div className={s.workflowLinks}><p className={s.eyebrow}>PROTECTED WORKFLOWS</p>{workflows}</div>}
  </>;
  const context = <>
    <div className={s.contextHeading}><PentaGlyph name="panel" /><h2>Intelligence & context</h2></div>
    <div className={s.contextCard}><span className={s.eyebrow}>WORKING WITH YOU</span><div className={s.cardTitle}><span className={s.contextIcon}><PentaGlyph name="spark" /></span><h3>{latest?.result ? "Verified result" : "Your next step, in focus"}</h3></div><p>{latest ? latest.message : "Ask in your own words. Keep the conversation here, and review the details alongside it."}</p><span className={s.statusBadge}>{snapshot?.pending ? "Checking records" : latest?.result ? "Host-verified read" : providerStatus}</span></div>
    {latest?.result && <section className={s.artifact} aria-label="Verified response details"><div className={s.artifactHeading}><PentaGlyph name="receipt" /><strong>{queryEntityLabel(latest.result.entity)}</strong><span>{latest.result.records.length} shown</span></div><div className={s.artifactRows}>{latest.result.records.map(record => <Link href={record.sourcePath} key={record.id}><strong>{record.label}</strong>{record.code && <small>{record.code}</small>}{record.balances?.map(balance => <span key={balance.currency}>{balance.currency} {balance.outstandingExact}</span>)}{record.attendance && <span>{record.attendance.status} · {record.attendance.batch}</span>}</Link>)}</div>{latest.result.hasMore && <p>More records match. Refine your question.</p>}<footer><PentaGlyph name="connections" /><span>Academy Desk · {queryEntityLabel(latest.result.entity)}<small>As of {new Date(latest.result.asOfUtc).toLocaleString()}</small></span></footer></section>}
    <div className={s.contextSection}><p className={s.eyebrow}>CONNECTED WORKSPACE</p><strong>{verified ? labels[workspace] : "Checking your workspace"}</strong><p>{snapshot?.entities.length ? snapshot.entities.map(queryEntityLabel).join(" · ") : "Permitted tools are resolved from your current account."}</p><button type="button" className={s.textAction} onClick={() => setSection("connections")}>View connections <span aria-hidden="true">↗</span></button></div>
    <div className={s.contextSection}><p className={s.eyebrow}>ACTION REVIEW</p><p>{latest?.result ? "This response read existing records. It did not change data or send a message." : "Consequential actions keep their host validation and required approvals. Nothing is approved by choosing a model."}</p><button type="button" className={s.textAction} onClick={() => setSection("review")}>Review protected workflows <span aria-hidden="true">↗</span></button></div>
    <div className={s.contextFooter}><PentaGlyph name="shield" />Permission checked. Source attached. You stay in control.</div>
  </>;
  return <PentaWorkspaceContext.Provider value={publish}><main {...theme} className={`${theme.className} ${s.workspace}`}>
    <a className={s.skip} href="#penta-conversation">Skip to conversation</a>
    <header className={s.header}>
      <div className={s.brand}><span className={s.mark}><PentaGlyph name="spark" /></span><div><strong>PENTA AI</strong><small>Your intelligent workspace</small></div></div>
      <button className={s.mobileMenu} type="button" onClick={() => setOverlay("navigation")} aria-label="Open workspace navigation"><PentaGlyph name="grid" /></button>
      <div className={s.workspaceIdentity}><span className={s.sessionDot} /><span>{labels[workspace]}<small>{verified ? "Workspace verified" : "Verifying access"}</small></span></div>
      <button className={s.modelSelector} type="button" onClick={() => setSection("models")} aria-label="Choose PENTA model"><PentaGlyph name="spark" /><span>MINI <small>{miniEvaluation ? "Evaluation" : "Guided mode"}</small></span><span aria-hidden="true">⌄</span></button>
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
        <section className={s.conversation} aria-label="PENTA workspace conversation"><div className={s.conversationHeading}><span className={s.eyebrow}>ONE WORKSPACE. FIVE INTELLIGENCES.</span><h1>{workspace === "Platform" ? "Your platform. One intelligent space." : workspace === "Teacher" ? "Your teaching. One intelligent space." : workspace === "Portal" ? "Your learning. One intelligent space." : "Your academy. One intelligent space."}</h1><p>Think out loud. Find the answer. Move work forward.</p></div><div id="penta-conversation" ref={conversation} tabIndex={-1} className={s.conversationBody}>{children}</div></section>
        <aside className={s.context} aria-label="Workspace context">{context}</aside>
      </div>
    </div>
    <Modal open={overlay === "navigation"} title="Your workspace" onClose={() => setOverlay(null)} drawer><nav className={s.mobileNavigation}>{navigation}</nav></Modal>
    <Modal open={overlay === "context"} title="Workspace context" onClose={() => setOverlay(null)} drawer><div className={s.contextDrawer}>{context}</div></Modal>
    <Modal open={help !== null} title={help ?? "PENTA capability"} onClose={() => setHelp(null)}><p>{capabilities.find(capability => capability.name === help)?.description}</p><p>Capability names do not grant permissions or enable an unavailable tool.</p></Modal>
    <Modal open={section === "models"} title="Choose your PENTA model" onClose={() => setSection(null)}><p className={s.dialogIntro}>One workspace, the same permission boundaries. Only a configured and qualified runtime can serve a request.</p><div className={s.modelOptions}>{[
      {name:"MINI",detail:"Focused everyday work",status:miniEvaluation ? "Evaluation only" : "Guided interface; no live model",current:true},
      {name:"PLUS",detail:"Deeper workflows and analysis",status:"Not connected",current:false},
      {name:"PRO",detail:"Advanced planning and reasoning",status:"Not connected",current:false},
      {name:"ULTRA",detail:"Future high-capacity tier",status:"Deferred",current:false},
    ].map(model=><button type="button" key={model.name} disabled={!model.current} aria-pressed={model.current} onClick={()=>setSection(null)}><PentaGlyph name="spark" /><span><strong>{model.name}</strong><small>{model.detail}</small></span><em>{model.status}</em></button>)}</div><p className={s.dialogIntro}>This selector does not load weights, upgrade permissions or redirect a request to an unavailable tier. Mini is not production-qualified.</p></Modal>
    <Modal open={section === "connections"} title="Workspace connections" onClose={() => setSection(null)}><div className={s.connectionTile}><PentaGlyph name="connections" /><div><strong>Academy Desk</strong><p>{verified ? "Authenticated host workspace" : "Workspace verification pending"}</p><small>{providerStatus} · Manual remains available</small></div></div><p className={s.dialogIntro}>Only the academy and records permitted by your signed-in account can be retrieved. No external connector is presented as connected without host verification.</p>{snapshot?.entities.length ? <p>Connected record types: {snapshot.entities.map(queryEntityLabel).join(", ")}.</p> : null}<Link className={s.dialogLink} href={workspaceHomes[workspace].manual}>Open permitted Manual settings and integrations ↗</Link></Modal>
    <Modal open={section === "review"} title="Action review" onClose={() => setSection(null)}><p>Read results do not require an approval. Creates, updates, deletions and sends must use their protected host workflows and required confirmations.</p><p>No approval queue has been loaded here; an empty display does not mean the academy has no pending work.</p>{workflows ? <div className={s.workflowLinks}>{workflows}</div> : <Link className={s.dialogLink} href={workspaceHomes[workspace].manual}>Open your permitted Manual workspace ↗</Link>}</Modal>
    <Modal open={section === "new"} title="Start a new conversation?" onClose={() => setSection(null)}><p>This opens a separate conversation and replaces this page’s recovery pointer. The current responses remain protected on the server, but are not listed in the new conversation.</p><button type="button" className={s.dialogLink} disabled={!snapshot?.available || snapshot.pending} onClick={()=>{window.dispatchEvent(new Event("penta-request-new-conversation"));setSection(null);}}>Start new conversation</button></Modal>
  </main></PentaWorkspaceContext.Provider>;
}
