"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { apiUrl, portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent, isPortalSignOutEvent } from "@/lib/api";
import { validWorkspaceAccount } from "@/lib/workspace-entry";
import { validPentaAccount } from "@/lib/penta-chat-contract";
import { readPentaJson } from "@/lib/penta-onboarding-channel";
import { createEnrollmentLifecycle, initialLifecycleState, lifecycleStatuses, type LifecycleSource, type LifecycleStatus } from "@/lib/penta-enrollment-lifecycle";
import type { AcademicChoice } from "@/lib/penta-enrollment-assistant";
import type { EnrollmentTransport } from "@/lib/penta-enrollment-creation";
import { ThemeToggle } from "@/components/theme-toggle";
import { usePentaManualTheme } from "@/components/penta-manual-theme";
import styles from "./enrollment-assistant.module.css";

type Driver = ReturnType<typeof createEnrollmentLifecycle>;
function LifecyclePanel({ transport }: { transport: EnrollmentTransport }) {
  const driver = useRef<Driver | null>(null);
  const [view, setView] = useState(initialLifecycleState);
  const [query, setQuery] = useState("");
  const [batchQuery, setBatchQuery] = useState("");
  const [source, setSource] = useState<LifecycleSource | null>(null);
  const [target, setTarget] = useState<AcademicChoice | null>(null);
  const [kind, setKind] = useState<"update" | "transfer">("update");
  const [status, setStatus] = useState<LifecycleStatus>("Paused");
  const [endDate, setEndDate] = useState("");
  const [reason, setReason] = useState("");
  const [transferDate, setTransferDate] = useState("");
  useEffect(() => {
    let mounted = true;
    const operation = createEnrollmentLifecycle(transport, () => window.sessionStorage, value => { if (mounted) setView(value); });
    driver.current = operation;
    void Promise.resolve().then(() => operation.initialise());
    return () => { mounted = false; operation.dispose(); driver.current = null; };
  }, [transport]);
  const enabled = view.availability === "ready" && !view.pending && !view.key;
  const d = view.draft, c = d?.change, key = view.key;
  return <section aria-label="Protected enrollment changes" aria-busy={view.pending}>
    {view.notice && <p role="status">{view.notice}</p>}
    {view.pending && <p role="status">Checking this enrollment change…</p>}
    {view.availability === "unavailable" && <button type="button" disabled={view.pending} onClick={() => void driver.current?.initialise()}>Check change availability</button>}
    {!key && <>
      <form onSubmit={e => { e.preventDefault(); setSource(null); void driver.current?.search(query, "source"); }}>
        <label htmlFor="lifecycle-query">Find an enrollment by learner or batch name</label>
        <div className={styles.actions}><input id="lifecycle-query" value={query} maxLength={80} minLength={2} required disabled={!enabled} onChange={e => setQuery(e.currentTarget.value)} /><button type="submit" disabled={!enabled || query.trim().length < 2}>Find enrollments</button></div>
      </form>
      {view.sources && <section className={styles.result} aria-label="Verified source enrollments">
        <h2>Matching enrollments</h2><p>Checked {new Date(view.sources.asOfUtc).toLocaleString()} · Academy records</p>
        {!view.sources.rows.length && <p>No matching enrollments found.</p>}
        <ul>{view.sources.rows.map(row => <li key={row.sourceId}><span>{row.studentName} · {row.batchName} · {row.status} · {row.startDate}</span><button type="button" disabled={!enabled} onClick={() => { setSource(row); setTarget(null); }}>Select {row.studentName} in {row.batchName}</button></li>)}</ul>
        {view.sources.hasMore && <p>More matches exist. Refine your search before selecting.</p>}
      </section>}
      {source && view.sources?.rows.includes(source) && <section className={styles.result} aria-label="Requested enrollment change">
        <h2>Choose a change</h2><p>{source.studentName} · {source.batchName} · Current status {source.status}</p><p>Source enrollment <code>{source.sourceId}</code></p>
        <label htmlFor="lifecycle-kind">Change type</label><select id="lifecycle-kind" disabled={!enabled} value={kind} onChange={e => setKind(e.currentTarget.value === "transfer" ? "transfer" : "update")}><option value="update">Update status</option><option value="transfer">Transfer to another batch</option></select>
        {kind === "update" ? <>
          <label htmlFor="lifecycle-status">Requested status</label><select id="lifecycle-status" value={status} disabled={!enabled} onChange={e => { const value = e.currentTarget.value as LifecycleStatus; if (lifecycleStatuses.includes(value)) setStatus(value); }}>{lifecycleStatuses.map(s => <option key={s}>{s}</option>)}</select>
          <label htmlFor="lifecycle-end">End date (optional; blank clears it)</label><input id="lifecycle-end" type="date" value={endDate} disabled={!enabled} onChange={e => setEndDate(e.currentTarget.value)} />
          <label htmlFor="lifecycle-reason">Lifecycle reason {status === "Active" ? "(optional)" : "(required)"}</label><input id="lifecycle-reason" value={reason} maxLength={1000} disabled={!enabled} onChange={e => setReason(e.currentTarget.value)} />
        </> : <>
          <p>Only an Active enrollment can transfer. The host rechecks target availability and admission rules.</p>
          <form onSubmit={e => { e.preventDefault(); setTarget(null); void driver.current?.search(batchQuery, "batch"); }}>
            <label htmlFor="lifecycle-batch-query">Find a target batch</label><div className={styles.actions}><input id="lifecycle-batch-query" value={batchQuery} minLength={2} maxLength={80} required disabled={!enabled} onChange={e => setBatchQuery(e.currentTarget.value)} /><button type="submit" disabled={!enabled || batchQuery.trim().length < 2}>Find target batches</button></div>
          </form>
          {view.batches && <div aria-label="Verified target batches"><ul>{view.batches.rows.map(row => <li key={row.id}><span>{row.label}</span><button type="button" disabled={!enabled || row.id === source.batchId} onClick={() => setTarget(row)}>Choose {row.label}</button></li>)}</ul>{!view.batches.rows.length && <p>No matching target batches found.</p>}{view.batches.hasMore && <p>More target batches exist. Refine the search.</p>}</div>}
          <p>Target batch: {target?.label ?? "Select from verified results"}</p>
          <label htmlFor="lifecycle-transfer-date">Transfer date</label><input id="lifecycle-transfer-date" type="date" value={transferDate} disabled={!enabled} onChange={e => setTransferDate(e.currentTarget.value)} />
        </>}
        <div className={styles.actions}><button type="button" disabled={!enabled || (kind === "update" ? status !== "Active" && !reason.trim() : !target || !transferDate || source.status !== "Active")} onClick={() => { if (kind === "update") void driver.current?.prepare(source, { kind, status, endDate: endDate || null, reason }); else if (target) void driver.current?.prepare(source, { kind, target, date: transferDate }); }}>Review exact change</button></div>
      </section>}
    </>}
    {c && <section className={styles.result} aria-label="Exact enrollment change review">
      <h2>{c.tool === "AcademyDesk.Enrollment.Update" ? "Review status update" : "Review batch transfer"}</h2>
      <dl className={styles.details}>
        <div><dt>Student ID</dt><dd><code>{c.studentId}</code></dd></div><div><dt>Source enrollment ID</dt><dd><code>{c.enrollmentId}</code></dd></div><div><dt>Source batch ID</dt><dd><code>{c.sourceBatchId}</code></dd></div>
        <div><dt>Current status</dt><dd>{c.currentStatus}</dd></div><div><dt>Requested status</dt><dd>{c.status}</dd></div>
        {c.tool === "AcademyDesk.Enrollment.Update" ? <><div><dt>End date</dt><dd>{c.endDate ?? "None — clears any existing end date"}</dd></div><div><dt>Lifecycle reason</dt><dd>{c.lifecycleReason ?? "None"}</dd></div></> : <><div><dt>Target batch ID</dt><dd><code>{c.targetBatchId}</code></dd></div><div><dt>Transfer date</dt><dd>{c.transferDate}</dd></div></>}
      </dl><p>The host rechecks current access, the source record and domain rules when this exact approved change is executed.</p>
    </section>}
    {key && <div className={styles.actions}>
      {view.availability === "ready" && view.phase === "review" && d?.status === "Prepared" && <button type="button" disabled={view.pending} onClick={() => void driver.current?.approve(d)}>Approve exact change</button>}
      {view.availability === "ready" && view.phase === "approved" && d?.status === "Authorized" && <><p>Approved; the enrollment change is not yet confirmed.</p><button type="button" disabled={view.pending} onClick={() => void driver.current?.execute(d)}>Execute approved change</button></>}
      {view.availability === "ready" && view.phase !== "expired" && <button type="button" disabled={view.pending} onClick={() => void driver.current?.recover(key)}>Check change status</button>}
      <button type="button" disabled={view.pending} onClick={() => { driver.current?.close(key); setSource(null); setTarget(null); setReason(""); setEndDate(""); setTransferDate(""); }}>{view.phase === "succeeded" ? "Done" : view.phase === "review" ? "Cancel review" : "Close after checking Manual Enrollments"}</button>
    </div>}
    {view.phase === "succeeded" && d?.status === "Succeeded" && <section className={styles.result} role="status"><h2>Enrollment change completed</h2><p>The host verified the committed enrollment and its audit.</p><dl className={styles.details}><div><dt>Result enrollment ID</dt><dd><code>{d.receipt.enrollment.id}</code></dd></div><div><dt>Result batch ID</dt><dd><code>{d.receipt.enrollment.batchId}</code></dd></div><div><dt>Result status</dt><dd>{d.receipt.enrollment.status}</dd></div><div><dt>Start date</dt><dd>{d.receipt.enrollment.startDate}</dd></div><div><dt>End date</dt><dd>{d.receipt.enrollment.endDate ?? "None"}</dd></div></dl><p>Receipt ID <code>{d.receipt.receiptId}</code></p><p>Audit ID <code>{d.receipt.auditId}</code></p><p>Completed {new Date(d.receipt.completedAtUtc).toLocaleString()}</p></section>}
    <p><Link href="/enrollments">Check Manual Enrollments</Link></p>
  </section>;
}

export default function EnrollmentLifecycle() {
  const theme = usePentaManualTheme();
  const [transport, setTransport] = useState<EnrollmentTransport | null>(null);
  const [notice, setNotice] = useState("Checking your academy access…");
  useEffect(() => {
    const controller = new AbortController(), bearer = portalAccessToken("AcademyAdmin"), epoch = portalSessionEpoch("AcademyAdmin");
    const current = () => !controller.signal.aborted && !!bearer && !!epoch && portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === epoch;
    function clear() { if (controller.signal.aborted) return; controller.abort(); setTransport(null); setNotice("Your academy access changed or could not be verified. Sign in again or open Manual Enrollments."); }
    async function send(path: string, method: "GET" | "PUT" | "POST", body?: unknown) {
      if (!current()) throw Error("Session changed.");
      const response = await fetch(apiUrl + path, { method, cache: "no-store", credentials: "omit", redirect: "error", referrerPolicy: "no-referrer", signal: controller.signal, headers: { Authorization: `Bearer ${bearer}`, Accept: "application/json", ...(body === undefined ? {} : { "Content-Type": "application/json" }) }, body: body === undefined ? undefined : JSON.stringify(body) });
      if (!current()) { clear(); throw Error("Session changed."); } if (response.status === 401 || response.status === 403) { clear(); throw Error("Access changed."); } return response;
    }
    function changed(e: Event) { if ((e as CustomEvent).detail === "AcademyAdmin") clear(); }
    function recheck() { if (!current()) clear(); }
    function storage(e: StorageEvent) { if (isPortalSignOutEvent(e, "AcademyAdmin")) clear(); else recheck(); }
    window.addEventListener(portalSessionChangedEvent, changed); window.addEventListener(portalSignOutEvent, changed); window.addEventListener("storage", storage); window.addEventListener("focus", recheck);
    void (async () => {
      try {
        const response = await send("/api/auth/session", "GET"); if (response.status !== 200) throw Error("Session unavailable.");
        const account = await readPentaJson(response, controller.signal);
        if (!validWorkspaceAccount(account) || account.workspace !== "AcademyAdmin" || !validPentaAccount(account)) throw Error("Unsupported account.");
        const root = `/api/academies/${account.academyId}/penta`;
        if (current()) { setTransport({ academyId: account.academyId, epoch, signal: controller.signal, current, invalidate: clear, send: (path, method, body) => send(root + (path.startsWith("/batch-search?") ? path : "/enrollments/lifecycle-browser" + path), method, body) }); setNotice(""); }
      } catch { clear(); }
    })();
    return () => { controller.abort(); window.removeEventListener(portalSessionChangedEvent, changed); window.removeEventListener(portalSignOutEvent, changed); window.removeEventListener("storage", storage); window.removeEventListener("focus", recheck); };
  }, []);
  return <main {...theme} className={`${theme.className} ${styles.page}`}>
    <header className={styles.top}><Link href="/penta">Academy Desk PENTA AI</Link><ThemeToggle /><Link href="/enrollments">Manual Enrollments</Link></header>
    <section className={styles.panel} aria-labelledby="lifecycle-heading"><h1 id="lifecycle-heading">Enrollment status and transfers</h1><p>Find an existing enrollment, review the exact change, then approve and execute it separately.</p><p className={styles.boundary}>Guided protected tools. A qualified AI model is not connected. Availability depends on the academy host.</p>{notice && <p role="status">{notice}</p>}{transport && <LifecyclePanel transport={transport} />}<p><Link href="/penta/enrollments">Enrollment lookup and creation</Link> · <Link href="/penta">Back to PENTA</Link></p></section>
  </main>;
}
