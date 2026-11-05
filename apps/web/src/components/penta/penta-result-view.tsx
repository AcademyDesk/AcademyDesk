"use client";

import { useId, useState, type ReactNode } from "react";
import styles from "./penta-result-view.module.css";

// Presentation only. The host supplies already-verified cells and actions;
// this component does not parse model output, sort, total or fetch records.
export type PentaTableRow = { key: string; cells: readonly ReactNode[] };

export default function PentaResultView({ children, columns, rows, caption }: {
  children: ReactNode;
  columns: readonly string[];
  rows: readonly PentaTableRow[];
  caption: string;
}) {
  const [view, setView] = useState<"cards" | "table">("cards");
  const contentId = useId();
  if (!rows.length) return children;

  return <div className={styles.result}>
    <div className={styles.toolbar}>
      <span>{rows.length} displayed · Source order</span>
      <div role="group" aria-label="Result display">
        <button type="button" aria-pressed={view === "cards"} aria-controls={contentId} onClick={() => setView("cards")}>Cards</button>
        <button type="button" aria-pressed={view === "table"} aria-controls={contentId} onClick={() => setView("table")}>Table</button>
      </div>
    </div>
    <div id={contentId}>
      {view === "cards" ? children : <>
        <p className={styles.hint}>Same verified records and order. Scroll within the table to see every column.</p>
        <div className={styles.scroll} role="region" aria-label="Scrollable verified results" tabIndex={0}>
          <table className={styles.table}>
            <caption>{caption}</caption>
            <thead><tr>{columns.map(column => <th scope="col" key={column}>{column}</th>)}</tr></thead>
            <tbody>{rows.map(row => <tr key={row.key}>{row.cells.map((cell, index) => index === 0
              ? <th scope="row" key={index}>{cell}</th>
              : <td key={index}>{cell}</td>)}</tr>)}</tbody>
          </table>
        </div>
      </>}
    </div>
  </div>;
}
