"use client";

import { useState } from "react";
import { AREAS, INBOX_TASKS, type AreaKey, type InboxTask } from "@/components/penta/penta-data";

type Decision = "approved" | "dismissed";
type Filter = "all" | AreaKey;

const PRIORITY_LABEL: Record<InboxTask["priority"], string> = { high: "High", medium: "Medium", low: "Low" };

export default function PentaInboxPage() {
  const [filter, setFilter] = useState<Filter>("all");
  const [decisions, setDecisions] = useState<Record<string, Decision>>({});

  const tasks = INBOX_TASKS.filter((task) => filter === "all" || task.area === filter);
  const pendingCount = INBOX_TASKS.filter((task) => !decisions[task.id]).length;

  function decide(id: string, decision: Decision) {
    setDecisions((prev) => ({ ...prev, [id]: decision }));
  }

  return (
    <div className="penta-standard">
      <header className="penta-ai-hero">
        <span className="penta-ai-badge">
          <span aria-hidden="true">✦</span> AI Task Inbox
        </span>
        <h1>{pendingCount} tasks waiting for your approval</h1>
        <p className="penta-subtitle">
          Each task shows why it was raised and PENTA&apos;s recommendation. Approve what you agree with, edit the
          detail, or dismiss it — nothing runs without your decision.
        </p>
      </header>

      <div className="penta-inbox-filters" role="group" aria-label="Filter by area">
        <button type="button" className={filter === "all" ? "penta-ai-domain-pill is-active" : "penta-ai-domain-pill"} onClick={() => setFilter("all")}>
          All
        </button>
        {AREAS.map((area) => (
          <button
            key={area.key}
            type="button"
            className={filter === area.key ? "penta-ai-domain-pill is-active" : "penta-ai-domain-pill"}
            onClick={() => setFilter(area.key)}
          >
            {area.label}
          </button>
        ))}
      </div>

      <ul className="penta-inbox-list">
        {tasks.map((task) => (
          <InboxRow key={task.id} task={task} decision={decisions[task.id]} onDecide={(decision) => decide(task.id, decision)} />
        ))}
        {tasks.length === 0 && <li className="penta-pulse-empty">No tasks in this area right now.</li>}
      </ul>
    </div>
  );
}

function InboxRow({
  task,
  decision,
  onDecide,
}: {
  task: InboxTask;
  decision?: Decision;
  onDecide: (decision: Decision) => void;
}) {
  return (
    <li className={`penta-inbox-row is-${task.priority}${decision ? ` is-${decision}` : ""}`}>
      <div className="penta-inbox-priority">{PRIORITY_LABEL[task.priority]}</div>
      <div className="penta-inbox-body">
        <strong>{task.title}</strong>
        <p className="penta-inbox-reason">{task.reason}</p>
        <p className="penta-inbox-context">{task.context}</p>
        <p className="penta-inbox-recommendation">
          <span aria-hidden="true">✦</span> {task.recommendation}
        </p>
        {decision ? (
          <p className="penta-inbox-decision">{decision === "approved" ? "Approved" : "Dismissed"}</p>
        ) : (
          <div className="penta-confirm-row">
            <button type="button" className="penta-primary-action" onClick={() => onDecide("approved")}>Approve</button>
            <button type="button" className="penta-secondary-action">Edit</button>
            <button type="button" className="penta-secondary-action" onClick={() => onDecide("dismissed")}>Dismiss</button>
          </div>
        )}
      </div>
    </li>
  );
}
