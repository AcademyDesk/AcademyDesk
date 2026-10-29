import { expenseCents, formatExpenseCents } from "@/lib/expense-money";

// Payroll SQL amounts have decimal(18,2) scale. Exact API strings avoid losing
// cents in large JSON numbers; unlike currencies are never added/converted.
export type PayrollAmount = { netAmount: number; netAmountExact?: string; currency: string };
const amountKeys = new Set(["monthlyAmount", "amountPerCycle", "grossAmount", "deductions"]);
// Only allowlisted, validated decimal lexemes become unquoted JSON numbers.
// Every non-monetary value still goes through JSON.stringify.
export function payrollRequestJson(body: Record<string, unknown>): string | null {
  const entries: string[] = [];
  for (const [key, value] of Object.entries(body)) {
    if (!amountKeys.has(key) || value === null) {
      const json = JSON.stringify(value);
      if (json !== undefined) entries.push(`${JSON.stringify(key)}:${json}`);
      continue;
    }
    if (typeof value !== "string") return null;
    const cents = expenseCents(value.trim());
    if (cents === null || cents < BigInt(0) || (key !== "deductions" && cents === BigInt(0)) || cents > BigInt("999999999999999999")) return null;
    entries.push(`${JSON.stringify(key)}:${cents / BigInt(100)}.${(cents % BigInt(100)).toString().padStart(2, "0")}`);
  }
  return `{${entries.join(",")}}`;
}
export function payrollMoney(amount: number, exact?: string, currency = "INR"): string {
  const cents = expenseCents(exact ?? amount);
  return cents === null ? "Amount unavailable" : formatExpenseCents(cents, currency);
}
export function payrollTotals(rows: PayrollAmount[]): string {
  const totals = new Map<string, bigint>();
  for (const row of rows) {
    const cents = expenseCents(row.netAmountExact ?? row.netAmount);
    if (cents === null) return "Totals unavailable";
    const currency = row.currency.trim().toUpperCase();
    totals.set(currency, (totals.get(currency) ?? BigInt(0)) + cents);
  }
  return totals.size ? [...totals].map(([currency, cents]) => formatExpenseCents(cents, currency)).join(" · ") : formatExpenseCents(BigInt(0), "INR");
}
