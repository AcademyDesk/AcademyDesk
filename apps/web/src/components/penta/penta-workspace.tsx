"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { academyApi } from "@/lib/api";
import { StandardAction } from "@/components/design-system/portal-standard";
import styles from "./penta.module.css";

type Domain = "student" | "batch";
type Context = { academyId: string; name: string; timeZone: string };
type Row = { sourceId: string; displayName?: string; name?: string; batchCode?: string | null; isActive?: boolean; sourcePath: string };
type Result = { academyId: string; tool: string; source: string; asOfUtc: string; timeZone: string; hasMore: boolean; maxRows: number; rows: Row[] };
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const failure = (status: number) => status === 401 ? "Your session has ended. Sign in to continue." : status === 403 ? "PENTA is available only to an authorized Academy Owner or Admin." : status === 404 ? "PENTA is not enabled in this environment. Your usual workspace is still available." : status === 429 ? "Too many requests. Wait a moment and try again." : "The search could not be completed. Please try again.";

function validResult(value: Result, academyId: string, domain: Domain): boolean {
  return value?.academyId === academyId && value.tool === `${domain}.search.v1` && value.source === (domain === "student" ? "Students" : "Batches")
    && value.maxRows === 10 && typeof value.hasMore === "boolean" && Number.isFinite(Date.parse(value.asOfUtc)) && typeof value.timeZone === "string"
    && Array.isArray(value.rows) && value.rows.length <= 10 && new Set(value.rows.map(row => row?.sourceId)).size === value.rows.length
    && value.rows.every(row => row && guid.test(row.sourceId) && (domain === "student"
      ? typeof row.displayName === "string" && row.isActive === true && row.sourcePath === `/student-management?studentId=${row.sourceId}`
      : typeof row.name === "string" && (row.batchCode == null || typeof row.batchCode === "string") && row.sourcePath === "/batch-setup"));
}

const experiences = [
  { id: "pulse", label: "Pulse", caption: "Stay ahead", description: "Briefings and operational priorities", manual: "/dashboard", destination: "Open overview" },
  { id: "executor", label: "Executor", caption: "Get work done", description: "Find records in your academy", manual: "/students", destination: "Open students" },
  { id: "navigator", label: "Navigator", caption: "Find opportunity", description: "Admissions and growth guidance", manual: "/leads", destination: "Open leads" },
  { id: "twin", label: "Twin", caption: "Understand why", description: "Academy explanations and what-if analysis", manual: "/reports", destination: "Open reports" },
  { id: "autopilot", label: "Autopilot", caption: "Stay in control", description: "Approved recurring workflows and exceptions", manual: "/work-queue", destination: "Open manual work queue" },
] as const;

export { default } from "./penta-chat-workspace";

// Retained bounded lookup prototype; the live prompt-first surface is the default.
export function LegacyRecordLookup({ active = true }: { active?: boolean }) {
  const [experience, setExperience] = useState<string>("executor");
  const selectedExperience = experiences.find(item => item.id === experience)!;
  const [context, setContext] = useState<Context | null>(null);
  const [booting, setBooting] = useState(true);
  const [domain, setDomain] = useState<Domain>("student");
  const [query, setQuery] = useState("");
  const [submitted, setSubmitted] = useState("");
  const [result, setResult] = useState<Result | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const flight = useRef<AbortController | null>(null);
  const generation = useRef(0);

  useEffect(() => {
    if (active) return;
    flight.current?.abort();
  }, [active]);

  useEffect(() => {
    const abort = new AbortController();
    const requestGeneration = generation;
    void (async () => {
      try {
        const sessionResponse = await academyApi("/api/auth/session", { signal: abort.signal });
        if (!sessionResponse.ok) throw new Error(failure(sessionResponse.status));
        const session = await sessionResponse.json();
        if (!guid.test(session.academyId ?? "") || session.isPlatformOwner || !Array.isArray(session.roles) || !session.roles.some((role: string) => role === "Owner" || role === "AcademyAdmin")) throw new Error(failure(403));
        const response = await academyApi(`/api/academies/${session.academyId}/penta/academy-context`, { signal: abort.signal });
        if (!response.ok) throw new Error(failure(response.status));
        const own = await response.json();
        if (own.academyId !== session.academyId || typeof own.name !== "string" || typeof own.timeZone !== "string") throw new Error("Academy context could not be verified.");
        if (!abort.signal.aborted) setContext(own);
      } catch (cause) {
        if (!abort.signal.aborted) setError(cause instanceof Error ? cause.message : failure(500));
      } finally { if (!abort.signal.aborted) setBooting(false); }
    })();
    return () => { abort.abort(); flight.current?.abort(); requestGeneration.current++; };
  }, []);

  function clearSearch() {
    generation.current++;
    flight.current?.abort();
    setBusy(false); setResult(null); setSubmitted(""); setError("");
  }

  async function search(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!context || busy) return;
    const normalized = query.trim();
    if (normalized.length < 2 || normalized.length > 80 || /[\u0000-\u001f\u007f-\u009f]/.test(normalized)) { setError("Enter 2 to 80 searchable characters."); return; }
    flight.current?.abort();
    const abort = new AbortController(); flight.current = abort;
    const current = ++generation.current;
    setBusy(true); setError(""); setResult(null); setSubmitted(normalized);
    try {
      const response = await academyApi(`/api/academies/${context.academyId}/penta/${domain}-search?q=${encodeURIComponent(normalized)}`, { signal: abort.signal });
      if (!response.ok) throw new Error(failure(response.status));
      const data = await response.json();
      if (!validResult(data, context.academyId, domain)) throw new Error("The source response could not be verified. Use the usual workspace instead.");
      // Validate the server timezone before presenting an as-of timestamp.
      new Intl.DateTimeFormat("en", { timeZone: data.timeZone }).format(new Date(data.asOfUtc));
      if (current === generation.current && !abort.signal.aborted) setResult(data);
    } catch (cause) {
      if (current === generation.current && !abort.signal.aborted) setError(cause instanceof Error ? cause.message : failure(500));
    } finally { if (current === generation.current) setBusy(false); }
  }

  return <main className={styles.page}>
    <header className={styles.heading}><div><span className={styles.eyebrow}>Five systems. One academy.</span><h1>{experience === "executor" ? "What would you like to find?" : selectedExperience.label}</h1></div><span className={styles.badge}>Local read-only pilot · No live AI provider</span></header>
    <div className={styles.experiences} role="tablist" aria-label="PENTA experiences">{experiences.map((item, index) => <button key={item.id} id={`penta-tab-${item.id}`} role="tab" type="button" aria-selected={experience === item.id} aria-controls="penta-experience-panel" tabIndex={experience === item.id ? 0 : -1} onClick={() => { clearSearch(); setExperience(item.id); }} onKeyDown={event => {
      const next = event.key === "ArrowRight" ? (index + 1) % experiences.length : event.key === "ArrowLeft" ? (index + experiences.length - 1) % experiences.length : event.key === "Home" ? 0 : event.key === "End" ? experiences.length - 1 : -1;
      if (next < 0) return;
      event.preventDefault(); clearSearch(); setExperience(experiences[next].id); document.getElementById(`penta-tab-${experiences[next].id}`)?.focus();
    }}><span className={styles.experienceLetter} aria-hidden="true">{item.label[0]}</span><span><strong>{item.label}</strong><small>{item.caption}</small></span></button>)}</div>
    <div role="tabpanel" id="penta-experience-panel" aria-labelledby={`penta-tab-${experience}`} tabIndex={0}>
    {experience !== "executor" ? <section className={styles.future}><span className={styles.eyebrow}>Not enabled yet</span><h2>{selectedExperience.description}</h2><p>This experience is planned, not running. No {experience === "autopilot" ? "automations have been enabled or started" : "AI insights are being generated"}.</p><Link href={selectedExperience.manual} className={styles.open}>{selectedExperience.destination} ↗</Link><StandardAction variant="secondary" onClick={() => setExperience("executor")}>Use record lookup</StandardAction></section> : <div className={styles.operatingGrid}><div>
    <section className={styles.panel} aria-labelledby="penta-search-title">
      <div className={styles.panelHead}><div><span className={styles.eyebrow}>Executor · Record lookup</span><h2 id="penta-search-title">Search your academy</h2></div>{context && <span className={styles.academy}>{context.name}</span>}</div>
      {booting ? <p role="status">Checking your academy access…</p> : context ? <form onSubmit={search} className={styles.form}>
        <fieldset className={styles.choices}><legend>What are you looking for?</legend>{(["student", "batch"] as const).map(value => <label key={value} className={domain === value ? styles.selected : ""}><input type="radio" name="penta-domain" value={value} checked={domain === value} onChange={() => { clearSearch(); setDomain(value); setQuery(""); }} />{value === "student" ? "Students" : "Batches"}</label>)}</fieldset>
        <label htmlFor="penta-query">{domain === "student" ? "Student name" : "Batch name or code"}</label>
        <div className={styles.searchRow}><input id="penta-query" value={query} maxLength={80} placeholder={domain === "student" ? "Enter at least part of a student’s name" : "Enter a batch name or code"} onChange={event => { clearSearch(); setQuery(event.target.value); }} aria-describedby="penta-guidance" /><StandardAction type="submit" disabled={busy}>{busy ? "Searching…" : "Search"}</StandardAction>{busy && <StandardAction onClick={clearSearch} variant="secondary">Cancel</StandardAction>}</div>
        <p id="penta-guidance" className={styles.hint}>Active records only. Up to 10 matches. No records are changed by this search.</p>
      </form> : <p>PENTA could not be opened. Continue in your usual workspace or <Link href="/login">sign in</Link>.</p>}
      {error && <p role="alert" className={styles.error}>{error}</p>}
    </section>
    <section aria-live="polite" aria-busy={busy} className={styles.results}>
      {busy && <p role="status">Searching your academy’s {domain === "student" ? "students" : "batches"}…</p>}
      {result && <><div className={styles.resultHead}><h2>{result.rows.length === 0 ? "No matches found" : `${result.rows.length} matching ${domain}${result.rows.length === 1 ? "" : domain === "batch" ? "es" : "s"}`}</h2><p>For “{submitted}” · As of {new Intl.DateTimeFormat("en", { dateStyle: "medium", timeStyle: "short", timeZone: result.timeZone }).format(new Date(result.asOfUtc))} ({result.timeZone})</p></div>
        {result.rows.length === 0 && <p>Try another part of the name{domain === "batch" ? " or the batch code" : ""}. Inactive records are not included.</p>}
        {result.hasMore && <p className={styles.notice}>More matches exist. These are the first 10; refine your search to narrow the results.</p>}
        <div className={styles.cards}>{result.rows.map(row => <article key={row.sourceId} className={styles.card}><div><span className={styles.badge}>Active {domain}</span><h3>{domain === "student" ? row.displayName : row.name}</h3>{domain === "batch" && <p>Batch code: {row.batchCode || "Not provided"}</p>}<details><summary>Source details</summary><p>{result.source} · Record ID: {row.sourceId}</p></details></div><Link className={styles.open} href={domain === "student" ? `/student-management?studentId=${row.sourceId}` : "/batch-setup"}>{domain === "student" ? "Open Student 360" : "Open batch list"} <span aria-hidden="true">↗</span></Link></article>)}</div>
        {domain === "batch" && result.rows.length > 0 && <p className={styles.hint}>Batch links open the existing list, not a specific batch’s details.</p>}
      </>}
    </section>
    </div><aside className={styles.trust} aria-label="PENTA scope and controls"><span className={styles.eyebrow}>Your control centre</span><h2>Grounded. Bounded. Yours.</h2><dl><dt>Academy context</dt><dd>{context?.name || "Checking access"}</dd><dt>Available now</dt><dd>Active student and batch lookup</dd><dt>Action authority</dt><dd>Read-only. No create, update, send or schedule.</dd><dt>Model connection</dt><dd>No external provider connected</dd></dl><div><strong>Direct workspace</strong><Link href="/students">Manage students ↗</Link><Link href="/batch-setup">Classes & batches ↗</Link></div></aside></div>}
    </div>
    <footer className={styles.footer}><span>Prefer the usual workspace?</span><Link href="/students">Student management</Link><Link href="/batch-setup">Classes & batches</Link></footer>
  </main>;
}
