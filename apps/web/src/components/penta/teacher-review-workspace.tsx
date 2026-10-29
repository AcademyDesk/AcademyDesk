"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { useTheme } from "@/components/theme-provider";
import { portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent, isPortalSignOutEvent } from "@/lib/api";
import { loadTeacherCreationChannel, TeacherCreationUnavailable, type TeacherCreationView } from "@/lib/penta-teacher-operation";
import { Foundation, Button, Notice, Modal, Icon } from "@/vendor/penta-operational/frontend/shared-foundation/src";
import styles from "./teacher-review.module.css";

type Channel = Awaited<ReturnType<typeof loadTeacherCreationChannel>>;
type Phase = "loading" | "ready" | "unavailable" | "unknown" | "access";
const labels: Record<TeacherCreationView["status"], string> = {
  AwaitingReview: "Draft saved", Reviewed: "Ready for approval", Authorized: "Approved · not created", Succeeded: "Teacher created", Cancelled: "Cancelled",
};
export default function TeacherReviewWorkspace({ conversation, draft }: { conversation: string; draft: string }) {
  const { theme, setTheme } = useTheme();
  const [phase, setPhase] = useState<Phase>("loading"), [value, setValue] = useState<TeacherCreationView | null>(null);
  const [busy, setBusy] = useState(false), [confirm, setConfirm] = useState(false), [now, setNow] = useState(0);
  const channel = useRef<Channel | null>(null), alive = useRef(false), lock = useRef(false);
  useEffect(() => {
    const abort = new AbortController(), bearer = portalAccessToken("AcademyAdmin"), epoch = portalSessionEpoch("AcademyAdmin"); alive.current = true;
    function hide() { abort.abort(); channel.current?.dispose(); channel.current = null; if (alive.current) { setValue(null); setConfirm(false); setPhase("access"); } }
    function changed() { if (portalAccessToken("AcademyAdmin") !== bearer || portalSessionEpoch("AcademyAdmin") !== epoch) hide(); }
    function local(event: Event) { if ((event as CustomEvent).detail === "AcademyAdmin") hide(); }
    function storage(event: StorageEvent) { if (isPortalSignOutEvent(event, "AcademyAdmin")) hide(); else changed(); }
    const timer = window.setInterval(() => { changed(); if (alive.current) setNow(Date.now()); }, 1000);
    window.addEventListener(portalSignOutEvent, local); window.addEventListener(portalSessionChangedEvent, local);
    window.addEventListener("storage", storage); window.addEventListener("focus", changed);
    void loadTeacherCreationChannel(conversation, draft, abort.signal).then(result => {
      if (!alive.current || abort.signal.aborted) { result.dispose(); return; }
      channel.current = result; setValue(result.current); setPhase("ready"); setNow(Date.now());
    }).catch(error => { if (alive.current && !abort.signal.aborted) { setValue(null); setPhase(error instanceof TeacherCreationUnavailable && error.access ? "access" : "unavailable"); } });
    return () => { alive.current = false; abort.abort(); channel.current?.dispose(); channel.current = null; window.clearInterval(timer);
      window.removeEventListener(portalSignOutEvent, local); window.removeEventListener(portalSessionChangedEvent, local); window.removeEventListener("storage", storage); window.removeEventListener("focus", changed); };
  }, [conversation, draft]);
  async function act(action: "review" | "approve" | "execute" | "cancel" | "refresh") {
    if (lock.current || !alive.current || !channel.current || phase === "access") return;
    lock.current = true; setBusy(true); setConfirm(false);
    try {
      const result = await channel.current[action]();
      if (alive.current) { setValue(result); setPhase(channel.current.outcomeUnconfirmed && result.status !== "Succeeded" && result.status !== "Cancelled" ? "unknown" : "ready"); setNow(Date.now()); }
    } catch (error) {
      if (alive.current) { setConfirm(false); setValue(null); setPhase(error instanceof TeacherCreationUnavailable && error.access ? "access" : "unknown"); }
    } finally { lock.current = false; if (alive.current) setBusy(false); }
  }
  const fields = value ? (JSON.parse(value.canonicalPayload).fields as Record<string, { kind: string; value: string }>) : {};
  const expired = !!value && value.status !== "Succeeded" && (Date.parse(value.expiresAtUtc) <= now ||
    !!value.approvalExpiresAtUtc && Date.parse(value.approvalExpiresAtUtc) <= now);
  const launch = (action: Parameters<typeof act>[0]) => { void act(action); };
  return <Foundation theme={theme === "light" ? "prism" : "orbit"} className={styles.screen}>
    <header className={styles.header}>
      <div className={styles.brand}><span className={styles.mark}><Icon name="spark" /></span><div><strong>Academy Desk PENTA AI</strong><span>Executor · Protected teacher review</span></div></div>
      <nav aria-label="Workspace controls"><Link href="/penta">PENTA AI</Link><Link href="/teacher-onboarding">Manual workspace</Link>
        <Button onClick={() => setTheme(theme === "dark" ? "light" : "dark")}>{theme === "dark" ? "Light theme" : "Dark theme"}</Button></nav>
    </header>
    <main className={styles.canvas} aria-busy={busy || phase === "loading"}>
      <div className={styles.heading}><span className={styles.eyebrow}>HOST-VERIFIED WORKFLOW</span><h1>Review once. Create with confidence.</h1>
        <p>Check the exact teacher details, approve this draft, then create. Your academy verifies every step.</p></div>
      <section aria-label="Teacher workflow status" aria-live="polite" className={styles.status}>
        {phase === "loading" ? <Notice title="Checking the protected draft…" tone="info">No creation request is sent when this page opens.</Notice> :
          phase === "access" ? <Notice title="Current academy access could not be verified" tone="warning">Private teacher details are hidden. Sign in to the correct academy or use Manual.</Notice> :
            phase === "unavailable" ? <Notice title="Protected teacher review is unavailable" tone="warning">Open an existing teacher draft from its conversation. This feature must be enabled by the host; Manual remains available.</Notice> :
              phase === "unknown" ? <Notice title="Teacher outcome is unconfirmed" tone="warning">Do not create another request. Check this saved draft to recover the authoritative result.</Notice> :
                value?.status === "Succeeded" ? <Notice title="Teacher created" tone="success">The academy verified the saved teacher, receipt and audit. No portal account was created.</Notice> :
                  value?.status === "Cancelled" ? <Notice title="Teacher draft cancelled" tone="info">No teacher was created by this draft.</Notice> :
                    expired ? <Notice title="This review expired" tone="warning">Return to the conversation for a fresh review. No automatic approval or creation will be attempted.</Notice> :
                      <Notice title={value ? labels[value.status] : "Teacher review"} tone="info">{value?.status === "Authorized" ? "Approval is saved. Creation is a separate explicit step." : "Review is not permission to create. Only the exact approved draft can be saved."}</Notice>}
      </section>
      {value && phase === "ready" && <div className={styles.grid}>
        <section className={styles.card} aria-label="Canonical teacher details"><div className={styles.cardHeading}><h2>Teacher details</h2><span className={styles.chip}>Revision {value.revision}</span></div>
          <dl className={styles.details}>{Object.entries(fields).map(([key, field]) => <div key={key}><dt>{key.replace(/([a-z])([A-Z])/g, "$1 $2")}</dt>
            <dd>{field.value.trim() || "Not provided"}{field.kind === "reference" && <span className={styles.reference}>Verified record reference</span>}</dd></div>)}</dl>
          {!Object.keys(fields).length && <p>No teacher details have been provided yet.</p>}
          <p className={styles.footnote}>Optional details may remain blank. Credentials and complex compensation or availability settings stay in Manual.</p>
        </section>
        <aside className={styles.card} aria-label="Teacher creation controls"><h2>{value.status === "Succeeded" ? "Saved result" : "Your controls"}</h2>
          <ol className={styles.steps}><li>Review the current details</li><li>Approve the exact draft</li><li>Create and verify the result</li></ol>
          <p className={styles.footnote}>Possible duplicates are held for Manual review. Nothing here sends a message, creates an account or invokes a model.</p>
          {value.receipt && <div className={styles.receipt}><span>Verified at</span><strong>{new Date(value.receipt.completedAtUtc).toLocaleString()}</strong>
            <Link href={`/teacher-profile?teacherId=${value.receipt.teacherId}`}>Open teacher profile</Link><code>Receipt {value.receipt.receiptId}</code></div>}
          {phase === "ready" && !expired && <div className={styles.actions}>
            {value.status === "AwaitingReview" && <Button variant="primary" disabled={busy} onClick={() => launch("review")}>Review teacher draft</Button>}
            {value.status === "Reviewed" && <Button variant="primary" disabled={busy} onClick={() => setConfirm(true)}>Approve exact details</Button>}
            {value.status === "Authorized" && <Button variant="primary" disabled={busy} onClick={() => launch("execute")}>Create approved teacher</Button>}
            {!["Succeeded", "Cancelled"].includes(value.status) && <Button disabled={busy} onClick={() => launch("cancel")}>Cancel this draft</Button>}
          </div>}
        </aside>
      </div>}
      {phase === "unknown" && <div className={styles.recovery}><Button disabled={busy} onClick={() => launch("refresh")}>Check saved teacher result</Button><Link href="/teachers">Review teachers in Manual</Link></div>}
      <footer className={styles.footer}>Protected host workflow · Conversational interpretation remains separately gated.</footer>
    </main>
    <Modal open={confirm && phase === "ready" && !expired && value?.status === "Reviewed"} title="Approve this exact teacher draft" onClose={() => setConfirm(false)}>
      <p>Approve revision {value?.revision} with the details shown. This records permission for this draft only. It does not create a teacher yet.</p>
      <p>To change details, return to the conversation or Manual and prepare a fresh review.</p>
      <div className={styles.actions}><Button disabled={busy} onClick={() => setConfirm(false)}>Back to review</Button>
        <Button variant="primary" disabled={busy} onClick={() => launch("approve")}>Approve this teacher draft</Button></div>
    </Modal>
  </Foundation>;
}
