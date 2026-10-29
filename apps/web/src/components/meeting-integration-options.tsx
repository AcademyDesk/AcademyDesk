"use client";

import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
import { isUnavailableMeetingCapabilities, meetingIntegrations } from "@/lib/meeting-integration-options";
import styles from "./meeting-integration-options.module.css";

type Result = { academyId: string; attempt: number; kind: "unavailable" | "denied" | "error"; asOfUtc?: string };

export function MeetingIntegrationOptions({ academyId }: { academyId?: string }) {
  const [result, setResult] = useState<Result>();
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    if (!academyId) return;
    const controller = new AbortController();
    void (async () => {
      try {
        const response = await academyApi(`/api/academies/${encodeURIComponent(academyId)}/meetings/capabilities`, { cache: "no-store", signal: controller.signal });
        if (controller.signal.aborted) return;
        if (response.status === 401 || response.status === 403) {
          setResult({ academyId, attempt, kind: "denied" }); return;
        }
        if (!response.ok) throw new Error("Unavailable capabilities");
        const body: unknown = await response.json();
        if (!isUnavailableMeetingCapabilities(body)) throw new Error("Unrecognized capabilities");
        if (!controller.signal.aborted) setResult({ academyId, attempt, kind: "unavailable", asOfUtc: body.asOfUtc });
      } catch {
        if (!controller.signal.aborted) setResult({ academyId, attempt, kind: "error" });
      }
    })();
    return () => controller.abort();
  }, [academyId, attempt]);

  const current = result?.academyId === academyId && result?.attempt === attempt ? result : undefined;
  const loading = !!academyId && !current;
  const status = !academyId ? "Waiting for academy access" : loading ? "Checking availability…" : current?.kind === "unavailable" ? "Secure setup not enabled" : current?.kind === "denied" ? "Access unavailable" : "Status could not be verified";
  return (
    <section className={styles.hub} aria-labelledby="meeting-integrations-title" aria-busy={loading}>
      <header className={styles.header}>
        <div><p className={styles.eyebrow}>Meeting integrations</p><h2 id="meeting-integrations-title">Three providers. One academy workspace.</h2><p>Google Meet, Microsoft Teams and Zoom each have their own connection area. Saving a provider draft below does not connect an account.</p></div>
        <button type="button" className={styles.refresh} disabled={!academyId || loading} onClick={() => setAttempt((value) => value + 1)}>Refresh availability</button>
      </header>
      <p role="status" aria-live="polite" className={styles.notice}>{status}. {current?.kind === "denied" ? "Only authorized academy owners and administrators can manage connections." : "Provider sign-in, reconnect and disconnect remain unavailable until the secure connection flow is implemented and verified."}</p>
      <div className={styles.grid}>
        {meetingIntegrations.map((provider) => (
          <article className={styles.card} key={provider.id} data-meeting-integration={provider.id}>
            <div className={styles.identity}><span className={styles.mark} aria-hidden="true">{provider.mark}</span><div><h3>{provider.name}</h3><p>{provider.subtitle}</p></div></div>
            <p className={styles.description}>{provider.description}</p>
            <span className={styles.badge} id={`meeting-state-${provider.id}`}>{status}</span>
            <div className={styles.actions}>
              <button type="button" disabled aria-describedby={`meeting-state-${provider.id}`}>Connect {provider.name}</button>
              <button type="button" disabled aria-label={`Reconnect ${provider.name}`}>Reconnect</button>
              <button type="button" disabled aria-label={`Disconnect ${provider.name}`}>Disconnect</button>
            </div>
            <details className={styles.requirements}><summary>Setup requirements</summary><ol>{provider.requirements.map((step) => <li key={step}>{step}</li>)}</ol><a href={provider.documentation} target="_blank" rel="noopener noreferrer">Official {provider.name} developer guide <span aria-hidden="true">↗</span></a></details>
          </article>
        ))}
      </div>
      <footer className={styles.footer}><p>Account passwords, client secrets and access tokens must never be entered in chat or the draft fields. Each provider needs its own verified connection; one provider&apos;s status cannot enable another.</p>{current?.asOfUtc && <p>Availability checked: <time dateTime={current.asOfUtc}>{current.asOfUtc.replace("T", " ").replace("Z", " UTC")}</time></p>}</footer>
    </section>
  );
}
