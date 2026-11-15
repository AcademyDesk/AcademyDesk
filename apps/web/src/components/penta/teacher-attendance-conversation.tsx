"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { apiUrl, isPortalSignOutEvent, portalAccessToken, portalSessionChangedEvent, portalSessionEpoch, portalSignOutEvent } from "@/lib/api";
import { attendancePrompt, attendanceQuery, attendanceRoot, createTeacherAttendance, initialAttendanceView,
  type AttendanceLearner, type AttendanceStatus } from "@/lib/penta-teacher-attendance";
import styles from "./role-penta-entry.module.css";

const workspace = "Teacher";
const when = (date: string) => new Date(date).toLocaleString();

export default function TeacherAttendanceConversation({ academyId, fallback }: { academyId: string; fallback: ReactNode }) {
  const driver = useRef<ReturnType<typeof createTeacherAttendance> | null>(null);
  const [view, setView] = useState(initialAttendanceView);
  const [prompt, setPrompt] = useState("");
  const [asked, setAsked] = useState("");
  const [query, setQuery] = useState("");
  const [learner, setLearner] = useState<AttendanceLearner | null>(null);
  const [status, setStatus] = useState<AttendanceStatus>("Present");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState("");
  const [checkedManual, setCheckedManual] = useState(false);

  useEffect(() => {
    const bearer = portalAccessToken(workspace), epoch = portalSessionEpoch(workspace);
    if (!bearer || !epoch) return;
    const controller = new AbortController();
    const current = () => !controller.signal.aborted && portalAccessToken(workspace) === bearer && portalSessionEpoch(workspace) === epoch;
    function clear() {
      controller.abort();
      setView({ ...initialAttendanceView, phase: "unavailable" });
      setPrompt(""); setAsked(""); setQuery(""); setLearner(null); setNotes(""); setError("");
    }
    const attendance = createTeacherAttendance({
      academyId, epoch, signal: controller.signal, current, invalidate: clear,
      send: (path, method, body) => fetch(apiUrl + attendanceRoot + path, {
        method, cache: "no-store", credentials: "omit", redirect: "error", referrerPolicy: "no-referrer",
        signal: controller.signal, headers: { Authorization: "Bearer " + bearer, Accept: "application/json",
          ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
        body: body === undefined ? undefined : JSON.stringify(body),
      }),
    }, () => window.sessionStorage, setView);
    driver.current = attendance;
    function recheck() { if (!current()) clear(); }
    function changed(e: Event) { if ((e as CustomEvent).detail === workspace) clear(); }
    function storage(e: StorageEvent) { if (isPortalSignOutEvent(e, workspace)) clear(); else recheck(); }
    window.addEventListener(portalSessionChangedEvent, changed);
    window.addEventListener(portalSignOutEvent, changed);
    window.addEventListener("storage", storage);
    window.addEventListener("focus", recheck);
    void attendance.initialise();
    return () => {
      controller.abort(); attendance.dispose(); driver.current = null;
      window.removeEventListener(portalSessionChangedEvent, changed);
      window.removeEventListener(portalSignOutEvent, changed);
      window.removeEventListener("storage", storage);
      window.removeEventListener("focus", recheck);
    };
  }, [academyId]);

  if (!view.ready) return fallback;
  const draft = view.draft;
  const canUse = !view.pending && !view.pointer;
  return <section className={styles.panel} aria-labelledby="teacher-attendance-title" aria-busy={view.pending}>
    <h2 id="teacher-attendance-title">PENTA attendance conversation</h2>
    <p>Find an assigned class and learner, then review a mark and its in-app notice.</p>
    {asked && <div className={styles.financeTurn}><strong>You</strong> {asked}</div>}
    {view.notice && <p role="status">{view.notice}</p>}
    {error && <p role="alert">{error}</p>}
    {!view.pointer && <>
      <form className={styles.financeComposer} onSubmit={event => {
        event.preventDefault();
        try {
          const batch = attendancePrompt(prompt);
          setError(""); setLearner(null); setQuery(""); setNotes(""); setAsked(prompt.trim()); setPrompt("");
          void driver.current?.searchClasses(batch);
        } catch (error) { setError((error as Error).message); }
      }}>
        <label htmlFor="penta-attendance-prompt">Message PENTA about attendance</label>
        <div className={styles.financeActions}>
          <input id="penta-attendance-prompt" autoComplete="off" value={prompt} maxLength={110} disabled={!canUse}
            placeholder="Mark attendance for Piano" onChange={e => setPrompt(e.currentTarget.value)} />
          <button type="submit" disabled={!canUse || !prompt.trim()}>Send</button>
        </div>
      </form>
      {view.classes && <section className={styles.financeTurn} aria-label="Assigned class matches">
        <h3>Which class?</h3>
        {view.classes.rows.length ? <div className={styles.financeActions}>{view.classes.rows.map(row =>
          <button type="button" key={row.id} disabled={!canUse} aria-pressed={row === view.selected} onClick={() => {
            setLearner(null); setQuery(""); setNotes(""); setError(""); driver.current?.selectClass(row);
          }}>Choose {row.batchName} at {when(row.startUtc)}</button>)}</div> : <p>No assigned class matched. Try another batch name.</p>}
        {view.classes.hasMore && <p>More matches exist. Use a more specific batch name or open Manual Attendance.</p>}
      </section>}
      {view.selected && <form className={styles.financeComposer} onSubmit={event => {
        event.preventDefault();
        try { const currentQuery = attendanceQuery(query); setError(""); setLearner(null); setNotes(""); void driver.current?.searchLearners(currentQuery); }
        catch (error) { setError((error as Error).message); }
      }}>
        <label htmlFor="penta-attendance-learner">Find an enrolled learner in {view.selected.batchName}</label>
        <div className={styles.financeActions}>
          <input id="penta-attendance-learner" autoComplete="off" value={query} maxLength={80} disabled={!canUse}
            onChange={e => setQuery(e.currentTarget.value)} />
          <button type="submit" disabled={!canUse || query.trim().length < 2}>Find learner</button>
        </div>
      </form>}
      {view.roster && <section className={styles.financeTurn} aria-label="Enrolled learner matches">
        <h3>Which learner?</h3>
        {view.roster.rows.length ? <div className={styles.financeActions}>{view.roster.rows.map(row =>
          <button type="button" key={row.id} disabled={!canUse} aria-pressed={row === learner} onClick={() => {
            setLearner(row); setNotes(""); setError("");
          }}>Choose {row.name}</button>)}</div> : <p>No enrolled learner matched. Try a different name.</p>}
        {view.roster.hasMore && <p>More matches exist. Narrow the learner name.</p>}
      </section>}
      {learner && view.roster?.rows.includes(learner) && <form className={styles.financeComposer} onSubmit={event => {
        event.preventDefault(); if (canUse) void driver.current?.prepare(learner, status, notes);
      }}>
        <label htmlFor="penta-attendance-mark">Attendance for {learner.name}</label>
        <select className={styles.paymentField} id="penta-attendance-mark" value={status} disabled={!canUse} onChange={event =>
          setStatus(event.currentTarget.value === "Absent" ? "Absent" : "Present")}>
          <option>Present</option><option>Absent</option>
        </select>
        <label htmlFor="penta-attendance-notes">Attendance note (optional)</label>
        <div className={styles.financeActions}>
          <input id="penta-attendance-notes" autoComplete="off" maxLength={500} value={notes} disabled={!canUse}
            onChange={e => setNotes(e.currentTarget.value)} />
          <button type="submit" disabled={!canUse}>Review attendance</button>
        </div>
      </form>}
    </>}
    {draft && <div className={styles.financeTurn}>
      <h3>{draft.status === "Succeeded" ? "Attendance saved" : "Review exact attendance"}</h3>
      <dl>
        <dt>Class</dt><dd>{view.selected?.batchName ?? draft.batchId} · {when(draft.sessionStartUtc)}</dd>
        <dt>Learner</dt><dd>{learner?.id === draft.change.studentId ? learner.name : draft.change.studentId}</dd>
        <dt>Mark</dt><dd>{draft.change.status}</dd>
        <dt>Note</dt><dd>{draft.change.notes || "None"}</dd>
        <dt>In-app notice</dt><dd>{draft.change.inAppMessage}</dd>
      </dl>
      {draft.status === "Succeeded" ? <p role="status">One in-app notice queued. Receipt <code>{draft.receipt.id}</code></p> :
        <div className={styles.financeActions}><button type="button" disabled={view.pending}
          onClick={() => void driver.current?.confirm(draft)}>Confirm mark and queue notice</button></div>}
    </div>}
    {view.pointer && <div className={styles.financeActions}>
      <button type="button" disabled={view.pending} onClick={() => void driver.current?.recover()}>Check saved attendance</button>
      {view.phase === "closed" && <label><input type="checkbox" checked={checkedManual}
        onChange={e => setCheckedManual(e.currentTarget.checked)} /> I checked Manual Attendance</label>}
      {view.phase !== "unknown" && <button type="button" disabled={view.pending || view.phase === "closed" && !checkedManual} onClick={() => {
        setAsked(""); setNotes(""); setLearner(null); setCheckedManual(false); void driver.current?.clear();
      }}>{view.phase === "succeeded" ? "Start another mark" : "Dismiss review"}</button>}
    </div>}
    <p><Link href="/teacher">Open Manual Attendance</Link></p>
  </section>;
}
