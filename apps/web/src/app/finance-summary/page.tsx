"use client";
import { useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi } from "@/lib/api";
type Academy = { id: string };
type CurrencyTotal = {
  currency: string;
  grossBilled: number;
  approvedAdjustments: number;
  collected: number;
  outstanding: number;
  reconciled: number;
};
type Summary = { currencyTotals: CurrencyTotal[]; hasMixedCurrencies: boolean; overdueInvoices: number };
const money = (amount: number, currency: string) => {
  const unit = currency.trim().toUpperCase();
  if (!/^[A-Z]{3}$/.test(unit))
    return `${amount.toLocaleString("en-IN", { minimumFractionDigits: 2 })} (currency unavailable)`;
  return new Intl.NumberFormat("en-IN", { style: "currency", currency: unit, currencyDisplay: "code", minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
};
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
  const currencyTotals = summary?.currencyTotals ?? [];
  const cards = summary
    ? [...currencyTotals.flatMap((total) => {
        const netBillable = Math.max(0, total.grossBilled - total.approvedAdjustments);
        const collectionRate = netBillable ? Math.min(100, Math.round((total.collected / netBillable) * 100)) : 0;
        const unit = total.currency || "Currency unavailable";
        return [
          { label: `Gross billed · ${unit}`, value: money(total.grossBilled, total.currency) },
          { label: `Approved adjustments · ${unit}`, value: money(total.approvedAdjustments, total.currency) },
          { label: `Collected · ${unit}`, value: money(total.collected, total.currency) },
          { label: `Outstanding · ${unit}`, value: money(total.outstanding, total.currency) },
          { label: `Reconciled · ${unit}`, value: money(total.reconciled, total.currency) },
          { label: `Net billable · ${unit}`, value: money(netBillable, total.currency) },
          { label: `Collection rate · ${unit}`, value: `${collectionRate}%` },
          { label: `Awaiting reconciliation · ${unit}`, value: money(Math.max(0, total.collected - total.reconciled), total.currency) },
        ];
      }), { label: "Overdue invoices", value: String(summary.overdueInvoices) }]
    : [];
  return (
    <main className="enterprise-settings finance-summary-standard min-h-screen">
      <WorkspaceNav />
      <div className="finance-summary-content">
        <header className="finance-summary-heading">
          <span className="finance-summary-title-icon" aria-hidden="true">
            ¤
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
        {summary?.hasMixedCurrencies && <p className="finance-summary-notice">Totals are shown separately by currency. No exchange rate conversion is applied.</p>}
        {summary && currencyTotals.length === 0 && <p className="finance-summary-notice">{summary.currencyTotals ? "No financial activity to summarize." : "Currency totals are unavailable. Refresh the page to try again."}</p>}
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
