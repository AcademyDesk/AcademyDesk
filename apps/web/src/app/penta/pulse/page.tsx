"use client";

import Link from "next/link";
import { useState } from "react";
import { ALERTS, type Alert, type Severity } from "@/components/penta/penta-data";

const SEVERITY_LABEL: Record<Severity, string> = { critical: "Critical", warning: "Warning", info: "Info" };

export default function PentaPulsePage() {
  const [dismissed, setDismissed] = useState<string[]>([]);
  const visible = ALERTS.filter((alert) => !dismissed.includes(alert.id));

  return (
    <div className="penta-standard">
      <header className="penta-ai-hero">
        <span className="penta-ai-badge">
          <span aria-hidden="true">✦</span> Pulse
        </span>
        <h1>What needs my attention?</h1>
        <p className="penta-subtitle">
          A ranked feed, not a dashboard. Every item is grounded in recorded academy data, ordered by what matters
          most right now.
        </p>
      </header>

      <ul className="penta-pulse-feed">
        {visible.map((alert) => (
          <PulseItem key={alert.id} alert={alert} onFix={() => setDismissed((ids) => [...ids, alert.id])} />
        ))}
        {visible.length === 0 && (
          <li className="penta-pulse-empty">Nothing needs your attention right now. PENTA will surface new items here as they come up.</li>
        )}
      </ul>
    </div>
  );
}

function PulseItem({ alert, onFix }: { alert: Alert; onFix: () => void }) {
  return (
    <li className={`penta-pulse-item is-${alert.severity}`}>
      <span className="penta-pulse-severity" aria-hidden="true" />
      <div className="penta-pulse-body">
        <div className="penta-pulse-heading">
          <strong>{alert.title}</strong>
          <span className="penta-pulse-tag">{SEVERITY_LABEL[alert.severity]}</span>
        </div>
        <p>{alert.detail}</p>
        <div className="penta-pulse-actions">
          <Link href="/penta/ask" className="penta-secondary-action">Ask about this</Link>
          <Link href={alert.manualHref} className="penta-secondary-action">Open manually</Link>
          <button type="button" className="penta-primary-action" onClick={onFix}>Fix</button>
        </div>
      </div>
    </li>
  );
}
