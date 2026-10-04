"use client";

import Link from "next/link";
import { useId, useRef, useState, type FormEvent, type KeyboardEvent, type ReactNode } from "react";
import { timestamp } from "@/components/penta/penta-data";

type SearchItem = { id: string; name: string; code: string };
type Domain = "students" | "batches";
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
];

const BATCHES: SearchItem[] = [
  { id: "btc-gtr-b01", name: "Guitar Beginners A", code: "GTR-B01" },
  { id: "btc-gtr-b02", name: "Guitar Beginners B", code: "GTR-B02" },
  { id: "btc-pno-f01", name: "Piano Foundations A", code: "PNO-F01" },
  { id: "btc-voc-b01", name: "Vocals Beginners", code: "VOC-B01" },
  { id: "btc-vln-a02", name: "Violin Advanced", code: "VLN-ADV2" },
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

export default function PentaExecutorAskPage() {
  const [domain, setDomain] = useState<Domain>("students");
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<Status>("idle");
  const [results, setResults] = useState<SearchItem[]>([]);
  const [asOf, setAsOf] = useState("");
  const [selectedId, setSelectedId] = useState("");
  const timerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const inputRef = useRef<HTMLInputElement>(null);
  const queryFieldId = useId();
  const examplesId = useId();

  function chooseDomain(next: Domain) {
    if (next === domain) return;
    setDomain(next);
    setQuery("");
    setStatus("idle");
    setResults([]);
    setSelectedId("");
    requestAnimationFrame(() => inputRef.current?.focus());
  }

  function performSearch(trimmedQuery: string) {
    if (!trimmedQuery) return;
    setStatus("loading");
    setResults([]);
    setSelectedId("");
    timerRef.current = setTimeout(() => {
      const matches = matchesOf(domain, trimmedQuery);
      setResults(matches.slice(0, 10));
      setStatus("success");
      setAsOf(timestamp());
    }, 650);
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

  function onInputKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== "Escape") return;
    if (status === "loading") {
      if (timerRef.current) clearTimeout(timerRef.current);
      setStatus("idle");
      return;
    }
    setQuery("");
    setResults([]);
    setStatus("idle");
  }

  const noun = domain === "students" ? "student" : "batch";
  const requiresSelection = status === "success" && results.length > 1;
  const selectableAction = domain === "students" ? "Open Student 360" : "Open batch list";

  return (
    <div className="penta-standard">
      <header className="penta-ai-hero">
        <span className="penta-ai-badge">
          <span aria-hidden="true">✦</span> Executor
        </span>
        <h1>What should I do?</h1>
        <p className="penta-subtitle">
          Ask in plain language to find students and batches, or act on them. PENTA grounds every answer in academy
          records and never invents fees, attendance, or schedules.
        </p>
      </header>

      <section className="penta-ask-panel">
        <div className="penta-ai-domain-toggle" role="group" aria-label="What PENTA should search">
          <span>Ask about</span>
          <button
            type="button"
            className={domain === "students" ? "penta-ai-domain-pill is-active" : "penta-ai-domain-pill"}
            aria-pressed={domain === "students"}
            onClick={() => chooseDomain("students")}
          >
            Students
          </button>
          <button
            type="button"
            className={domain === "batches" ? "penta-ai-domain-pill is-active" : "penta-ai-domain-pill"}
            aria-pressed={domain === "batches"}
            onClick={() => chooseDomain("batches")}
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
          <button type="submit" className="penta-primary-action">
            <span aria-hidden="true">✦</span> Ask
          </button>
        </form>
        <p id={examplesId} className="penta-examples">Try a name or a {noun} code — PENTA understands either.</p>
        <div className="penta-prompt-chips">
          {PROMPT_SUGGESTIONS[domain].map((prompt) => (
            <button key={prompt} type="button" className="penta-prompt-chip" onClick={() => askSuggestion(prompt)}>
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

        <p className="penta-footer-note">
          PENTA AI only shows information already recorded in AcademyDesk. It does not calculate fees, attendance, or
          schedules.
        </p>
      </section>
    </div>
  );
}

function AiBubble({ children }: { children: ReactNode }) {
  return (
    <div className="penta-ai-bubble">
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
