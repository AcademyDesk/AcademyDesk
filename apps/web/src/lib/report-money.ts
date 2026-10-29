import { expenseCents, formatExpenseCents } from "@/lib/expense-money";

export type ReportAmount = { amount: string | number; currency: string };

export function reportDecimal(value: string | number): string {
  const cents = typeof value === "number" || typeof value === "string" ? expenseCents(value) : null;
  if (cents === null) return "Amount unavailable";
  const absolute = cents < BigInt(0) ? -cents : cents;
  return `${cents < BigInt(0) ? "-" : ""}${absolute / BigInt(100)}.${(absolute % BigInt(100)).toString().padStart(2, "0")}`;
}

/** Read-only presentation, never currency conversion or authoritative accounting.
 * Unsafe/missing wire amounts are unavailable, not silently rounded or set to0.
 */
export function reportMoney(rows: readonly ReportAmount[], subtract: readonly ReportAmount[] = []): string {
  const totals = new Map<string, bigint>();
  for (const [entries, sign] of [[rows, BigInt(1)], [subtract, BigInt(-1)]] as const) {
    for (const row of entries) {
      const code = typeof row.currency === "string" ? row.currency.trim().toUpperCase() : "";
      const cents = typeof row.amount === "number" || typeof row.amount === "string" ? expenseCents(row.amount) : null;
      if (!/^[A-Z]{3}$/.test(code) || cents === null) return "Amount unavailable";
      totals.set(code, (totals.get(code) ?? BigInt(0)) + sign * cents);
    }
  }
  return totals.size ? [...totals].sort(([a], [b]) => a.localeCompare(b))
    .map(([code, cents]) => `${code} ${formatExpenseCents(cents, code)}`).join(" · ") : "No amounts recorded";
}

export function reportBalanceIsOpen(balance: string | number): boolean {
  const cents = typeof balance === "number" || typeof balance === "string" ? expenseCents(balance) : null;
  // Keep an unavailable balance visible for correction rather than declare it settled.
  return cents === null || cents > BigInt(0);
}
