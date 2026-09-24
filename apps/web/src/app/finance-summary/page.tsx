"use client";
import { useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi } from "@/lib/api";
type Academy = { id: string };
type Summary = {
  grossBilled: number;
  approvedAdjustments: number;
  collected: number;
  outstanding: number;
  reconciled: number;
  overdueInvoices: number;
};
const money = (amount: number) => `₹${amount.toLocaleString("en-IN")}`;
export default function FinanceSummary() {
  const [summary, setSummary] = useState<Summary>();
  const [message, setMessage] = useState("Loading finance summary…");
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        const academies: Academy[] = await response.json();
        if (!response.ok || !academies[0]) throw Error();
        const summaryResponse = await academyApi(
          `/api/academies/${academies[0].id}/finance-governance/summary`,
        );
        if (!summaryResponse.ok) throw Error();
        setSummary(await summaryResponse.json());
        setMessage("");
      } catch {
        setMessage("Finance summary could not be loaded.");
      }
    })();
  }, []);
  const netBillable = summary
    ? Math.max(0, summary.grossBilled - summary.approvedAdjustments)
    : 0;
  const collectionRate = netBillable
    ? Math.min(100, Math.round((summary!.collected / netBillable) * 100))
    : 0;
  const awaitingReconciliation = summary
    ? Math.max(0, summary.collected - summary.reconciled)
    : 0;
  const cards = summary
    ? [
        { label: "Gross billed", value: money(summary.grossBilled) },
        {
          label: "Approved adjustments",
          value: money(summary.approvedAdjustments),
        },
        { label: "Collected", value: money(summary.collected) },
        { label: "Outstanding", value: money(summary.outstanding) },
        { label: "Reconciled", value: money(summary.reconciled) },
        { label: "Overdue invoices", value: String(summary.overdueInvoices) },
        { label: "Net billable", value: money(netBillable) },
        { label: "Collection rate", value: `${collectionRate}%` },
        {
          label: "Awaiting reconciliation",
          value: money(awaitingReconciliation),
        },
      ]
    : [];
  return (
    <main className="enterprise-settings finance-summary-standard min-h-screen">
      <WorkspaceNav />
      <div className="finance-summary-content">
        <header className="finance-summary-heading">
          <span className="finance-summary-title-icon" aria-hidden="true">
            ₹
          </span>
          <div className="finance-summary-title">
            <p>Finance</p>
            <h1>Summary</h1>
          </div>
        </header>
        {message && (
          <p className="finance-summary-notice" role="status">
            {message}
          </p>
        )}
        <section className="finance-summary-grid">
          {cards.map((card) => (
            <article key={card.label}>
              <span>{card.label}</span>
              <strong>{card.value}</strong>
            </article>
          ))}
        </section>
      </div>
    </main>
  );
}
