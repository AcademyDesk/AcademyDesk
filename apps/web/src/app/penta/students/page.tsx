"use client";

import Link from "next/link";
import { useState } from "react";
import { STUDENTS } from "@/components/penta/penta-data";

type View = "ai" | "manual";

export default function PentaStudentsPage() {
  const [view, setView] = useState<View>("ai");
  const overdue = STUDENTS.filter((student) => student.feesStatus === "Overdue");

  return (
    <div className="penta-standard">
      <header className="penta-ai-hero penta-module-header">
        <div>
          <span className="penta-ai-badge">
            <span aria-hidden="true">✦</span> Students
          </span>
          <h1>Students</h1>
          <p className="penta-subtitle">
            PENTA opens here with a briefing by default. Switch to the manual view for the full table, filters, and
            bulk actions — your choice is remembered for this module.
          </p>
        </div>
        <div className="penta-view-switch" role="group" aria-label="Students view">
          <button
            type="button"
            className={view === "ai" ? "penta-view-pill is-active" : "penta-view-pill"}
            aria-pressed={view === "ai"}
            onClick={() => setView("ai")}
          >
            <span aria-hidden="true">✦</span> AI view
          </button>
          <button
            type="button"
            className={view === "manual" ? "penta-view-pill is-active" : "penta-view-pill"}
            aria-pressed={view === "manual"}
            onClick={() => setView("manual")}
          >
            Manual view
          </button>
        </div>
      </header>

      {view === "ai" ? (
        <section className="penta-ask-panel">
          <div className="penta-ai-bubble">
            <span className="penta-ai-avatar" aria-hidden="true">✦</span>
            <div className="penta-ai-bubble-body">
              <span className="penta-ai-bubble-name">PENTA AI</span>
              <p>
                {STUDENTS.length} students recorded. {overdue.length} are overdue on fees, and 1 is on hold pending
                review.
              </p>
            </div>
          </div>

          <article className="penta-centre-card">
            <header>
              <h2>Overdue on fees</h2>
              <span>{overdue.length} students</span>
            </header>
            <ul>
              {overdue.map((student) => (
                <li key={student.id}>
                  {student.name} · {student.batch}
                </li>
              ))}
            </ul>
            <Link href="/fee-reminders" className="penta-primary-action">Draft fee reminders</Link>
          </article>

          <div className="penta-prompt-chips">
            <button type="button" className="penta-prompt-chip" onClick={() => setView("manual")}>
              <span aria-hidden="true">✦</span> Ask PENTA about this list
            </button>
            <Link href="/penta/ask" className="penta-prompt-chip">
              <span aria-hidden="true">✦</span> Find a specific student
            </Link>
          </div>
        </section>
      ) : (
        <section className="penta-manual-table-wrap">
          <div className="penta-manual-toolbar">
            <p>{STUDENTS.length} students</p>
            <button type="button" className="penta-secondary-action" onClick={() => setView("ai")}>
              <span aria-hidden="true">✦</span> Ask PENTA about this list
            </button>
          </div>
          <table className="penta-manual-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Code</th>
                <th>Batch</th>
                <th>Status</th>
                <th>Fees</th>
              </tr>
            </thead>
            <tbody>
              {STUDENTS.map((student) => (
                <tr key={student.id}>
                  <td>
                    <Link href={`/student-profile?id=${student.id}`}>{student.name}</Link>
                  </td>
                  <td>{student.code}</td>
                  <td>{student.batch}</td>
                  <td>
                    <span className="penta-status-chip">{student.status}</span>
                  </td>
                  <td>
                    <span className={student.feesStatus === "Overdue" ? "penta-status-chip is-overdue" : "penta-status-chip"}>
                      {student.feesStatus}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
    </div>
  );
}
