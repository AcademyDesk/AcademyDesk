export type FinanceMoneyKey = "grossBilled" | "approvedAdjustments" | "collected" | "outstanding" | "reconciled";
export type FinanceCurrencyTotals = Record<FinanceMoneyKey, number> & { currency: string };
export type FinanceSummary = {
  byCurrency?: FinanceCurrencyTotals[];
  overdueInvoices: number;
  currency?: string | null;
  hasMixedCurrencies?: boolean;
} & Partial<Record<FinanceMoneyKey, number | null>>;

const keys: FinanceMoneyKey[] = ["grossBilled", "approvedAdjustments", "collected", "outstanding", "reconciled"];
const unavailable = "Currency details unavailable";
function rows(summary: Pick<FinanceSummary, "byCurrency">): FinanceCurrencyTotals[] | null {
  const values = summary.byCurrency;
  if (!Array.isArray(values) || !values.every(row => row && typeof row.currency === "string"
    && keys.every(key => typeof row[key] === "number" && Number.isFinite(row[key])))) return null;
  const currencies = values.map(row => row.currency.trim().toUpperCase());
  return new Set(currencies).size === currencies.length ? values : null;
}

export function formatFinanceMoney(amount: number, currency?: string): string {
  const number = new Intl.NumberFormat("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
  if (currency === undefined) return number;
  const code = currency.trim().toUpperCase();
  try { return new Intl.NumberFormat("en-IN", { style: "currency", currency: code, currencyDisplay: "code" }).format(amount); }
  catch { return `${code || "Unspecified currency"} ${number}`; }
}

export function formatExactFinanceAmount(amount: unknown, currency: unknown): string | null {
  if (typeof amount !== "string" || !/^-?(0|[1-9][0-9]{0,15})\.[0-9]{2}$/.test(amount) ||
      typeof currency !== "string" || !/^[A-Za-z]{3}$/.test(currency.trim())) return null;
  return `${currency.trim().toUpperCase()} ${amount}`;
}

export function financeMoney(summary: Pick<FinanceSummary, "byCurrency">, key: FinanceMoneyKey): string {
  const values = rows(summary);
  if (!values) return unavailable;
  return values.map(row => formatFinanceMoney(row[key], row.currency)).join(" · ") || formatFinanceMoney(0);
}

export function financeDerived(summary: FinanceSummary, metric: "netBillable" | "collectionRate" | "awaitingReconciliation"): string {
  const values = rows(summary);
  if (!values) return unavailable;
  if (!values.length) return metric === "collectionRate" ? "0%" : formatFinanceMoney(0);
  return values.map(row => {
    const net = Math.max(0, row.grossBilled - row.approvedAdjustments);
    if (metric === "collectionRate") return `${row.currency.trim().toUpperCase() || "Unspecified currency"} ${net ? Math.min(100, Math.round(row.collected / net * 100)) : 0}%`;
    return formatFinanceMoney(metric === "netBillable" ? net : Math.max(0, row.collected - row.reconciled), row.currency);
  }).join(" · ");
}
