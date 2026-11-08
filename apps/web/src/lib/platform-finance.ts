import { formatFinanceMoney } from "@/lib/finance-summary";

export type PlatformMoneyKey = "totalBilled" | "collectedBilling" | "outstandingBilling";
export type PlatformBillingTotals = Record<PlatformMoneyKey, number> & { currency: string };
export type PlatformMoneySummary = {
  byCurrency?: PlatformBillingTotals[];
  currency?: string | null;
  hasMixedCurrencies?: boolean;
} & Partial<Record<PlatformMoneyKey, number | null>>;

const fields: PlatformMoneyKey[] = ["totalBilled", "collectedBilling", "outstandingBilling"];
export function platformMoney(summary: PlatformMoneySummary | undefined, key: PlatformMoneyKey): string {
  const rows = summary?.byCurrency;
  if (!Array.isArray(rows) || !rows.every(row => row && typeof row.currency === "string"
    && fields.every(field => typeof row[field] === "number" && Number.isFinite(row[field])))) return "Currency details unavailable";
  if (new Set(rows.map(row => row.currency.trim().toUpperCase())).size !== rows.length) return "Currency details unavailable";
  return rows.map(row => formatFinanceMoney(row[key], row.currency)).join(" · ") || formatFinanceMoney(0);
}
