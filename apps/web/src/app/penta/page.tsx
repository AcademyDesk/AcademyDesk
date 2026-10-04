"use client";

import Link from "next/link";
import { useId, useRef, useState, type FormEvent, type KeyboardEvent, type ReactNode } from "react";
import { StandardSelectField } from "@/components/design-system/controls";

type SearchItem = { id: string; name: string; code: string };
type Domain = "students" | "batches";
type Scenario = "live" | "loading" | "empty" | "capped" | "multiple" | "network-error" | "unavailable" | "unauthorized" | "expired";
type Status = "idle" | "loading" | "success" | "error";

// Fictional, recorded-only sample data. PENTA never invents fees, attendance,
// schedules, or capacity — only identity and active status are shown.
const STUDENTS: SearchItem[] = [
  { id: "stu-1042", name: "Aarav Mehta", code: "STU-1042" },
  { id: "stu-1098", name: "Aarav Nair", code: "STU-1098" },
  { id: "stu-1103", name: "Ananya Iyer", code: "STU-1103" },
  { id: "stu-1130", name: "Meera Krishnan", code: "STU-1130" },
  { id: "stu-1174", name: "Diya Shah", code: "STU-1174" },
  { id: "stu-1190", name: "Kabir Rao", code: "STU-1190" },
  { id: "stu-1205", name: "Ishaan Verma", code: "STU-1205" },
  { id: "stu-1221", name: "Riya Kapoor", code: "STU-1221" },
  { id: "stu-1239", name: "Aditya Sharma", code: "STU-1239" },
  { id: "stu-1256", name: "Sanya Malhotra", code: "STU-1256" },
  { id: "stu-1267", name: "Vihaan Joshi", code: "STU-1267" },
  { id: "stu-1289", name: "Tara Bhatt", code: "STU-1289" },
];

const BATCHES: SearchItem[] = [
  { id: "btc-gtr-b01", name: "Guitar Beginners A", code: "GTR-B01" },
  { id: "btc-gtr-b02", name: "Guitar Beginners B", code: "GTR-B02" },
  { id: "btc-pno-f01", name: "Piano Foundations A", code: "PNO-F01" },
  { id: "btc-pno-a02", name: "Piano Advanced", code: "PNO-ADV2" },
  { id: "btc-voc-b01", name: "Vocals Beginners", code: "VOC-B01" },
  { id: "btc-voc-a03", name: "Vocals Advanced", code: "VOC-ADV3" },
  { id: "btc-vln-f01", name: "Violin Foundations", code: "VLN-F01" },
  { id: "btc-vln-a02", name: "Violin Advanced", code: "VLN-ADV2" },
  { id: "btc-drm-b01", name: "Drums Beginners", code: "DRM-B01" },
  { id: "btc-drm-a02", name: "Drums Advanced", code: "DRM-ADV2" },
  { id: "btc-key-f01", name: "Keyboard Foundations", code: "KEY-F01" },
  { id: "btc-uku-b01", name: "Ukulele Beginners", code: "UKU-B01" },
  { id: "btc-flt-f01", name: "Flute Foundations", code: "FLT-F01" },
];

const scenarioOptions = [
  { value: "live", label: "Live demo (default)" },
  { value: "loading", label: "Force: loading" },
  { value: "empty", label: "Force: no matches" },
  { value: "capped", label: "Force: many matches (cap)" },
  { value: "multiple", label: "Force: multiple matches" },
  { value: "network-error", label: "Force: API / network error" },
  { value: "unavailable", label: "Force: PENTA unavailable" },
  { value: "unauthorized", label: "Force: unauthorized user" },
  { value: "expired", label: "Force: expired session" },
];

const PROMPT_SUGGESTIONS: Record<Domain, string[]> = {
  students: ["Aarav Mehta", "Meera Krishnan", "STU-1130"],
  batches: ["Guitar Beginners", "Vocals Advanced", "GTR-B02"],
};

function matchesOf(domain: Domain, query: string) {
  const pool = domain === "students" ? STUDENTS : BATCHES;
  const needle = query.trim().toLowerCase();
  if (!needle) return [];
  return pool.filter((item) => item.name.toLowerCase().includes(needle) || item.code.toLowerCase().includes(needle));
}

export default function PentaPreviewPage() {
  const [domain, setDomain] = useState<Domain>("students");
  const [scenario, setScenario] = useState<Scenario>("live");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<Status>("idle");
  const [results, setResults] = useState<SearchItem[]>([]);
  const [capped, setCapped] = useState(false);
  const [asOf, setAsOf] = useState("");
  const [selectedId, setSelectedId] = useState("");
  const [networkErrorMessage, setNetworkErrorMessage] = useState("");
  const timerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const inputRef = useRef<HTMLInputElement>(null);
  const queryFieldId = useId();
  const examplesId = useId();

  function resetSearch() {
    if (timerRef.current) clearTimeout(timerRef.current);
    setStatus("idle");
    setResults([]);
    setCapped(false);
    setSelectedId("");
  }

  function chooseDomain(next: Domain) {
    if (next === domain) return;
    setDomain(next);
    setQuery("");
    resetSearch();
    requestAnimationFrame(() => inputRef.current?.focus());
  }

  function performSearch(trimmedQuery: string) {
    if (!trimmedQuery) return;
    setStatus("loading");
    setResults([]);
    setSelectedId("");
    timerRef.current = setTimeout(
      () => {
        if (scenario === "network-error") {
          setStatus("error");
          setNetworkErrorMessage("PENTA couldn't complete this search just now. Check your connection and try again.");
          return;
        }
        if (scenario === "empty") {
          setResults([]);
          setCapped(false);
          setStatus("success");
          setAsOf(timestamp());
          return;
        }
        if (scenario === "capped") {
          const pool = domain === "students" ? STUDENTS : BATCHES;
          setResults(pool.slice(0, 10));
          setCapped(pool.length > 10);
          setStatus("success");
          setAsOf(timestamp());
          return;
        }
        if (scenario === "multiple") {
          const pool = domain === "students" ? STUDENTS : BATCHES;
          setResults(pool.slice(0, 2));
          setCapped(false);
          setStatus("success");
          setAsOf(timestamp());
          return;
        }
        const matches = matchesOf(domain, trimmedQuery);
        setResults(matches.slice(0, 10));
        setCapped(matches.length > 10);
        setStatus("success");
        setAsOf(timestamp());
      },
      scenario === "loading" ? 60000 : 650,
    );
  }

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmed = query.trim();
    if (!trimmed) {
      inputRef.current?.focus();
      return;
    }
    performSearch(trimmed);
  }

  function askSuggestion(text: string) {
    setQuery(text);
    performSearch(text);
  }

  function cancelSearch() {
    if (timerRef.current) clearTimeout(timerRef.current);
    setStatus("idle");
  }

  function onInputKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== "Escape") return;
    if (status === "loading") {
      cancelSearch();
      return;
    }
    setQuery("");
    setResults([]);
    setStatus("idle");
  }

  function onScenarioChange(value: string) {
    setScenario(value as Scenario);
    resetSearch();
  }

  let body: ReactNode;
  if (scenario === "unauthorized") {
    body = (
      <TakeoverState icon="⊘" eyebrow="Access restricted" title="This preview is limited to Academy Owner and Admin roles.">
        <p>If you need access to PENTA preview, ask your Academy Owner or Admin to enable it for your account.</p>
      </TakeoverState>
    );
  } else if (scenario === "expired") {
    body = (
      <TakeoverState icon="◍" eyebrow="Session ended" title="Your session has ended.">
        <p>Sign in again to continue using PENTA preview.</p>
        <Link href="/login" className="penta-primary-action">Sign in again</Link>
      </TakeoverState>
    );
  } else if (scenario === "unavailable") {
    body = (
      <TakeoverState icon="✦" eyebrow="Temporarily unavailable" title="PENTA AI is temporarily unavailable.">
        <p>We&apos;ve recorded this and are looking into it. In the meantime, use these pages to find what you need.</p>
        <div className="penta-fallback-links">
          <Link href="/students">Open Students</Link>
          <Link href="/batches">Open Batches</Link>
        </div>
      </TakeoverState>
    );
  } else {
    body = (
      <AskWorkspace
        domain={domain}
        onChooseDomain={chooseDomain}
        query={query}
        setQuery={setQuery}
        status={status}
        results={results}
        capped={capped}
        asOf={asOf}
        selectedId={selectedId}
        setSelectedId={setSelectedId}
        networkErrorMessage={networkErrorMessage}
        inputRef={inputRef}
        queryFieldId={queryFieldId}
        examplesId={examplesId}
        onSubmit={onSubmit}
        onAskSuggestion={askSuggestion}
        onCancel={cancelSearch}
        onRetry={() => performSearch(query.trim())}
        onInputKeyDown={onInputKeyDown}
      />
    );
  }

  return (
    <main className="enterprise-settings penta-standard">
      <header className="penta-ai-hero">
        <span className="penta-ai-badge">
          <span aria-hidden="true">✦</span> PENTA AI
        </span>
        <h1>Ask PENTA</h1>
        <p className="penta-subtitle">
          Your AI assistant for finding students and batches already recorded in AcademyDesk. Ask in plain language —
          PENTA grounds every answer in academy records and never invents fees, attendance, or schedules.
        </p>
      </header>
      <section className="penta-demo-bar" aria-label="Design preview controls">
        <div>
          <span className="penta-demo-label">Preview state</span>
          <small>For design review only — switch states without needing live data.</small>
        </div>
        <StandardSelectField
          name="penta-scenario"
          value={scenario}
          onChange={onScenarioChange}
          placeholder="Live demo"
          options={scenarioOptions}
        />
      </section>
      {body}
      <footer className="penta-manual-footer">
        Prefer to browse without asking? <Link href="/students">Open Students</Link> ·{" "}
        <Link href="/batches">Open Batches</Link>
      </footer>
    </main>
  );
}

function timestamp() {
  return new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

function TakeoverState({ icon, eyebrow, title, children }: { icon: string; eyebrow: string; title: string; children: ReactNode }) {
  return (
    <section className="penta-takeover" role="alert">
      <span className="penta-takeover-icon" aria-hidden="true">{icon}</span>
      <p>{eyebrow}</p>
      <h2>{title}</h2>
      {children}
    </section>
  );
}

function AskWorkspace({
  domain,
  onChooseDomain,
  query,
  setQuery,
  status,
  results,
  capped,
  asOf,
  selectedId,
  setSelectedId,
  networkErrorMessage,
  inputRef,
  queryFieldId,
  examplesId,
  onSubmit,
  onAskSuggestion,
  onCancel,
  onRetry,
  onInputKeyDown,
}: {
  domain: Domain;
  onChooseDomain: (domain: Domain) => void;
  query: string;
  setQuery: (value: string) => void;
  status: Status;
  results: SearchItem[];
  capped: boolean;
  asOf: string;
  selectedId: string;
  setSelectedId: (value: string) => void;
  networkErrorMessage: string;
  inputRef: React.RefObject<HTMLInputElement | null>;
  queryFieldId: string;
  examplesId: string;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onAskSuggestion: (text: string) => void;
  onCancel: () => void;
  onRetry: () => void;
  onInputKeyDown: (event: KeyboardEvent<HTMLInputElement>) => void;
}) {
  const noun = domain === "students" ? "student" : "batch";
  const requiresSelection = status === "success" && results.length > 1;
  const selectableAction = domain === "students" ? "Open Student 360" : "Open batch list";

  return (
    <section className="penta-ask-panel">
      <div className="penta-ai-domain-toggle" role="group" aria-label="What PENTA should search">
        <span>Ask about</span>
        <button
          type="button"
          className={domain === "students" ? "penta-ai-domain-pill is-active" : "penta-ai-domain-pill"}
          aria-pressed={domain === "students"}
          onClick={() => onChooseDomain("students")}
        >
          Students
        </button>
        <button
          type="button"
          className={domain === "batches" ? "penta-ai-domain-pill is-active" : "penta-ai-domain-pill"}
          aria-pressed={domain === "batches"}
          onClick={() => onChooseDomain("batches")}
        >
          Batches
        </button>
      </div>

      <form onSubmit={onSubmit} className="penta-ai-ask-row">
        <label htmlFor={queryFieldId} className="sr-only">
          {domain === "students" ? "Ask about a student" : "Ask about a batch"}
        </label>
        <span className="penta-ai-input-icon" aria-hidden="true">✦</span>
        <input
          id={queryFieldId}
          ref={inputRef}
          className="penta-ai-input"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={onInputKeyDown}
          placeholder={domain === "students" ? "Ask PENTA, e.g. “Find Aarav Mehta”" : "Ask PENTA, e.g. “Find the Guitar Beginners batch”"}
          aria-describedby={examplesId}
          autoComplete="off"
        />
        {status === "loading" ? (
          <button type="button" className="penta-secondary-action" onClick={onCancel}>Cancel</button>
        ) : (
          <button type="submit" className="penta-primary-action">
            <span aria-hidden="true">✦</span> Ask
          </button>
        )}
      </form>
      <p id={examplesId} className="penta-examples">Try a name or a {noun} code — PENTA understands either.</p>
      <div className="penta-prompt-chips">
        {PROMPT_SUGGESTIONS[domain].map((prompt) => (
          <button key={prompt} type="button" className="penta-prompt-chip" onClick={() => onAskSuggestion(prompt)}>
            <span aria-hidden="true">✦</span> {prompt}
          </button>
        ))}
      </div>

      <div role="status" aria-live="polite" className="penta-live-region">
        {status === "loading" && (
          <AiBubble>
            <p className="penta-ai-thinking">
              <span className="penta-spinner" aria-hidden="true" /> Searching recorded academy data…
            </p>
          </AiBubble>
        )}
        {status === "success" && results.length === 0 && (
          <AiBubble>
            <p>I couldn&apos;t find an active {noun} matching &ldquo;{query.trim()}&rdquo;. Try a different name or code.</p>
          </AiBubble>
        )}
        {status === "success" && results.length === 1 && (
          <AiBubble>
            <p>I found 1 active match for &ldquo;{query.trim()}&rdquo;.</p>
            <ResultCard item={results[0]} asOf={asOf} action={<ItemAction domain={domain} item={results[0]} />} />
          </AiBubble>
        )}
        {requiresSelection && (
          <AiBubble>
            <fieldset className="penta-disambiguation">
              <legend>
                I found {results.length} matching {noun === "batch" ? "batches" : `${noun}s`} — which one did you mean?
              </legend>
              <ul>
                {results.map((item) => (
                  <li key={item.id}>
                    <label className="penta-option">
                      <input
                        type="radio"
                        name="penta-selection"
                        value={item.id}
                        checked={selectedId === item.id}
                        onChange={() => setSelectedId(item.id)}
                      />
                      <span>
                        <strong>{item.name}</strong>
                        <small>{item.code} · Active</small>
                      </span>
                    </label>
                  </li>
                ))}
              </ul>
              {capped && <p className="penta-cap-note">Showing the first 10 matches. Refine your question to narrow the list.</p>}
              <div className="penta-confirm-row">
                {selectedId && domain === "students" ? (
                  <Link href={`/student-profile?id=${selectedId}`} className="penta-primary-action">{selectableAction}</Link>
                ) : selectedId ? (
                  <Link href="/batches" className="penta-primary-action">{selectableAction}</Link>
                ) : (
                  <button type="button" className="penta-primary-action" disabled aria-disabled="true">{selectableAction}</button>
                )}
              </div>
            </fieldset>
          </AiBubble>
        )}
      </div>

      {status === "error" && (
        <AiBubble tone="error">
          <p>{networkErrorMessage}</p>
          <div className="penta-confirm-row">
            <button type="button" className="penta-primary-action" onClick={onRetry}>Try again</button>
          </div>
        </AiBubble>
      )}

      <p className="penta-footer-note">
        PENTA AI only shows information already recorded in AcademyDesk. It does not calculate fees, attendance, or
        schedules.
      </p>
    </section>
  );
}

function AiBubble({ children, tone = "default" }: { children: ReactNode; tone?: "default" | "error" }) {
  return (
    <div className={tone === "error" ? "penta-ai-bubble is-error" : "penta-ai-bubble"} role={tone === "error" ? "alert" : undefined}>
      <span className="penta-ai-avatar" aria-hidden="true">✦</span>
      <div className="penta-ai-bubble-body">
        <span className="penta-ai-bubble-name">PENTA AI</span>
        {children}
      </div>
    </div>
  );
}

function ResultCard({ item, asOf, action }: { item: SearchItem; asOf: string; action: ReactNode }) {
  return (
    <article className="penta-result-card">
      <div>
        <strong>{item.name}</strong>
        <span className="penta-status-chip">Active</span>
      </div>
      <small>{item.code}</small>
      <small className="penta-result-source">Academy records{asOf ? ` · as of ${asOf}` : ""}</small>
      <div className="penta-result-action">{action}</div>
    </article>
  );
}

function ItemAction({ domain, item }: { domain: Domain; item: SearchItem }) {
  if (domain === "students") return <Link href={`/student-profile?id=${item.id}`} className="penta-primary-action">Open Student 360</Link>;
  return <Link href="/batches" className="penta-primary-action">Open batch list</Link>;
}
