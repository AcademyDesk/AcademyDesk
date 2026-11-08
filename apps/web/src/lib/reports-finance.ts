import { formatFinanceMoney } from "@/lib/finance-summary";

export type ReportMoneyRow = { amount: number; currency?: string };
const unavailable = "Currency details unavailable";

export function reportCurrency(currency?: string): string {
  return typeof currency === "string" ? currency.trim().toUpperCase() || "Unspecified currency" : unavailable;
}

export function reportAmount(amount: number, currency?: string): string {
  return typeof amount === "number" && Number.isFinite(amount) && typeof currency === "string"
    ? formatFinanceMoney(amount, currency) : unavailable;
}

export function reportMoney(rows: ReportMoneyRow[]): string {
  const totals = new Map<string, number>();
  for (const row of rows) {
    if (typeof row.amount !== "number" || !Number.isFinite(row.amount) || typeof row.currency !== "string") return unavailable;
    const unit = row.currency.trim().toUpperCase();
    const total = (totals.get(unit) ?? 0) + row.amount;
    if (!Number.isFinite(total)) return unavailable;
    totals.set(unit, total);
  }
  return [...totals].sort(([a], [b]) => a.localeCompare(b, "en"))
    .map(([currency, amount]) => formatFinanceMoney(amount, currency)).join(" · ") || formatFinanceMoney(0);
}

// Separate units remain separate, including an expense-only negative balance.
// Preserve the existing recorded-expense scope; this is not a cash-flow forecast.
export function reportDifference(receipts: ReportMoneyRow[], expenses: ReportMoneyRow[]): string {
  if (expenses.some(row => typeof row.amount !== "number" || !Number.isFinite(row.amount))) return unavailable;
  return reportMoney([...receipts, ...expenses.map(row => ({ ...row, amount: -row.amount }))]);
}
