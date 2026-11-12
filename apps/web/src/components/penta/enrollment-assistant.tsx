"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import { apiUrl, portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent, isPortalSignOutEvent } from "@/lib/api";
import { validPentaAccount } from "@/lib/penta-chat-contract";
import { validWorkspaceAccount } from "@/lib/workspace-entry";
import { readPentaJson } from "@/lib/penta-onboarding-channel";
import { academicCommand, academicSearch, admissionPayload, admissionPreview, validAdmissionDate, type AcademicChoice, type AcademicSearch, type AdmissionPreview } from "@/lib/penta-enrollment-assistant";
import EnrollmentCreation from "./enrollment-creation";
import type { EnrollmentTransport } from "@/lib/penta-enrollment-creation";
import { ThemeToggle } from "@/components/theme-toggle";
import { usePentaManualTheme } from "@/components/penta-manual-theme";
import styles from "./enrollment-assistant.module.css";

type Channel = { academyId: string; current(): boolean; request(path: string, body?: unknown): Promise<unknown>; creation: EnrollmentTransport };
class AccessLost extends Error {}
export default function EnrollmentAssistant() {
  const theme = usePentaManualTheme();
  const [channel, setChannel] = useState<Channel | null>(null);
  const [prompt, setPrompt] = useState("");
  const [result, setResult] = useState<AcademicSearch | null>(null);
  const [student, setStudent] = useState<AcademicChoice | null>(null);
  const [batch, setBatch] = useState<AcademicChoice | null>(null);
  const [startDate, setStartDate] = useState("");
  const [status, setStatus] = useState<"Active" | "Waitlisted">("Active");
  const [preview, setPreview] = useState<AdmissionPreview | null>(null);
  const [notice, setNotice] = useState("Checking your academy access…");
  const [busy, setBusy] = useState(false);
  const [creationOpen, setCreationOpen] = useState(false);
  const creationLocked = useRef(false);
  const creationActivity = useCallback((value: boolean) => { if (!value && creationLocked.current) setPreview(null); creationLocked.current = value; setCreationOpen(value); }, []);
  const active = useRef(false);
  const invalidate = useRef<() => void>(() => {});

  useEffect(() => {
    const controller = new AbortController();
    const bearer = portalAccessToken("AcademyAdmin"), epoch = portalSessionEpoch("AcademyAdmin");
    const current = () => !controller.signal.aborted && !!bearer && portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === epoch;
    function clear() {
      if (controller.signal.aborted) return;
      controller.abort();
      creationLocked.current = false; setCreationOpen(false);
      setChannel(null); setPrompt(""); setResult(null); setStudent(null); setBatch(null); setStartDate(""); setPreview(null); setBusy(false);
      setNotice("Your academy access changed or could not be verified. Sign in again or open Manual Enrollments.");
    }
    invalidate.current = clear;
    async function send(path: string, method: "GET" | "PUT" | "POST", body?: unknown) {
      if (!current()) throw new AccessLost();
      const response = await fetch(apiUrl + path, { method, cache: "no-store", credentials: "omit",
        redirect: "error", referrerPolicy: "no-referrer", signal: controller.signal,
        headers: { Authorization: `Bearer ${bearer}`, Accept: "application/json", ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
        body: body === undefined ? undefined : JSON.stringify(body) });
      if (!current()) throw new AccessLost();
      if (response.status === 401 || response.status === 403) { clear(); throw new AccessLost(); }
      return response;
    }
    async function request(path: string, body?: unknown) {
      const response = await send(path, body === undefined ? "GET" : "POST", body);
      if (response.status !== 200) { await response.body?.cancel().catch(() => {}); throw Error("This host read is unavailable or was refused."); }
      const value = await readPentaJson(response, controller.signal);
      if (!current()) throw new AccessLost();
      return value;
    }
    function changed(event: Event) { if ((event as CustomEvent).detail === "AcademyAdmin") clear(); }
    function recheck() { if (!current()) clear(); }
    function storage(event: StorageEvent) { if (isPortalSignOutEvent(event, "AcademyAdmin")) clear(); else recheck(); }
    window.addEventListener(portalSessionChangedEvent, changed); window.addEventListener(portalSignOutEvent, changed);
    window.addEventListener("storage", storage); window.addEventListener("focus", recheck);
    void (async () => {
      try {
        const account = await request("/api/auth/session");
        if (!validWorkspaceAccount(account) || account.workspace !== "AcademyAdmin" || !validPentaAccount(account)) throw new AccessLost();
        if (current()) { setChannel({ academyId: account.academyId, current, request, creation: {
          academyId: account.academyId, epoch, signal: controller.signal, current, invalidate: clear,
          send: (path, method, body) => send(`/api/academies/${account.academyId}/penta/enrollments` + path, method, body),
        } }); setNotice(""); }
      } catch { if (!controller.signal.aborted) clear(); }
    })();
    return () => {
      controller.abort(); invalidate.current = () => {};
      window.removeEventListener(portalSessionChangedEvent, changed); window.removeEventListener(portalSignOutEvent, changed);
      window.removeEventListener("storage", storage); window.removeEventListener("focus", recheck);
    };
  }, []);

  async function run(action: (source: Channel) => Promise<void>) {
    if (active.current || creationLocked.current || !channel) return;
    if (!channel.current()) { invalidate.current(); return; }
    const source = channel;
    active.current = true; setBusy(true); setNotice("");
    try { await action(source); }
    catch (error) {
      if (!source.current()) { invalidate.current(); return; }
      if (error instanceof AccessLost) { invalidate.current(); return; }
      setNotice("PENTA could not verify this read. Check access, refine the request or open Manual Enrollments. No enrollment was created or approved.");
    } finally { active.current = false; if (source.current()) setBusy(false); }
  }
  async function search(source: Channel) {
    const command = academicCommand(prompt);
    if (!command) { setNotice('Use “Find student Mira” or “Find batch Piano” with 2–80 search characters. Other requests are unavailable here.'); return; }
    setResult(null);
    const value = academicSearch(await source.request(`/api/academies/${source.academyId}/penta/${command.kind}-search?q=${encodeURIComponent(command.query)}`), command.kind, source.academyId);
    if (source.current()) { setResult(value); setPrompt(""); }
  }
  function choose(choice: AcademicChoice, kind: "student" | "batch") {
    if (busy || creationLocked.current || !channel?.current() || !result || result.kind !== kind || !result.rows.includes(choice)) return;
    if (kind === "student") setStudent(choice); else setBatch(choice);
    setPreview(null);
  }
  async function checkAdmission(source: Channel) {
    if (!student || !batch || !validAdmissionDate(startDate)) { setNotice("Select a verified student and batch, then enter a valid start date."); return; }
    setPreview(null);
    const expected = { studentId: student.id, batchId: batch.id, startDate, status };
    const value = admissionPreview(await source.request(`/api/academies/${source.academyId}/penta/enrollments/preview`, admissionPayload(expected)), expected);
    if (source.current()) setPreview(value);
  }
  const enabled = !!channel && !busy && !creationOpen;
  return <main {...theme} className={`${theme.className} ${styles.page}`}>
    <header className={styles.top}><Link href="/penta">Academy Desk PENTA AI</Link><ThemeToggle /><Link href="/enrollments">Manual Enrollments</Link></header>
    <section className={styles.panel} aria-labelledby="enrollment-assistant-heading" aria-busy={busy}>
      <h1 id="enrollment-assistant-heading">Enrollment assistant</h1>
      <p>Find a student and batch, then check enrollment eligibility with your academy’s protected tools.</p>
      <p className={styles.boundary}>Limited command support. A qualified AI model is not connected. Eligibility previews create nothing. Where enabled, enrollment creation needs separate exact review, approval and an explicit action.</p>
      {notice && <p role="status">{notice}</p>}
      {busy && <p role="status">Checking the host…</p>}
      <form onSubmit={event => { event.preventDefault(); void run(search); }}>
        <label htmlFor="academic-prompt">Ask PENTA to find a student or batch</label>
        <div className={styles.actions}><input id="academic-prompt" value={prompt} maxLength={100} disabled={!enabled} placeholder="Find student Mira" onChange={event => setPrompt(event.currentTarget.value)} required />
          <button disabled={!enabled || !prompt.trim()} type="submit">Send request</button></div>
      </form>
      {result && <section className={styles.result} aria-label="Verified search results">
        <h2>{result.kind === "student" ? "Students" : "Batches"}</h2>
        <p>Checked {new Date(result.asOfUtc).toLocaleString()} · Academy records</p>
        {!result.rows.length && <p>No matching {result.kind === "student" ? "students" : "batches"} found.</p>}
        <ul>{result.rows.map(choice => <li key={choice.id}><span>{choice.label}</span><button type="button" disabled={!enabled} onClick={() => choose(choice, result.kind)}>Select {choice.label}</button></li>)}</ul>
        {result.hasMore && <p>More matches exist. Refine your search before selecting.</p>}
      </section>}
      {channel && <section className={styles.result} aria-label="Enrollment eligibility">
        <h2>Check enrollment eligibility</h2>
        <dl><div><dt>Student</dt><dd>{student?.label ?? "Select from verified search results"}</dd></div><div><dt>Batch</dt><dd>{batch?.label ?? "Select from verified search results"}</dd></div></dl>
        <form onSubmit={event => { event.preventDefault(); void run(checkAdmission); }}>
          <label htmlFor="admission-date">Start date</label><input id="admission-date" type="date" value={startDate} disabled={!enabled} required onChange={event => { setStartDate(event.currentTarget.value); setPreview(null); }} />
          <label htmlFor="admission-status">Requested status</label><select id="admission-status" value={status} disabled={!enabled} onChange={event => { if (event.currentTarget.value === "Active" || event.currentTarget.value === "Waitlisted") { setStatus(event.currentTarget.value); setPreview(null); } }}><option>Active</option><option>Waitlisted</option></select>
          <button type="submit" disabled={!enabled || !student || !batch || !validAdmissionDate(startDate)}>Check eligibility</button>
        </form>
        {preview && !creationOpen && <div role="status"><h3>Eligibility checked — approval still required</h3><p>{student?.label} · {batch?.label} · {preview.startDate} · {preview.status}</p><p>No enrollment created. No seat reserved. Availability and permissions will be checked again when an approved operation is executed.</p><Link href="/enrollments">Continue in Manual Enrollments</Link></div>}
      </section>}
      {channel && <EnrollmentCreation transport={channel.creation} candidate={preview && student && batch ? { ...preview, studentLabel: student.label, batchLabel: batch.label } : null} onActivity={creationActivity} />}
      <p><Link href="/penta">Back to PENTA</Link> · <Link href="/enrollments">Open Manual Enrollments</Link></p>
    </section>
  </main>;
}
