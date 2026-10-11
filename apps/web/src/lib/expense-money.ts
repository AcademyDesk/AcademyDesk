// Expense decimal(18,2) amounts must not pass through binary floating point.
export function expenseCents(value: string | number): bigint | null {
  const text = typeof value === "number"
    ? Number.isFinite(value) && Math.abs(value) <= Number.MAX_SAFE_INTEGER / 100 ? String(value) : ""
    : value;
  const match = /^(-?)(\d{1,16})(?:\.(\d{1,2}))?$/.exec(text);
  if (!match) return null;
  const cents = BigInt(match[2]) * BigInt(100) + BigInt((match[3] ?? "").padEnd(2, "0"));
  return match[1] ? -cents : cents;
}

export function expenseAmountJson(value: string): string | null {
  const cents = expenseCents(value.trim());
  if (cents === null || cents <= BigInt(0) || cents > BigInt("999999999999999999")) return null;
  // This validated decimal lexeme is a JSON number, not a string or injected code.
  return `${cents / BigInt(100)}.${(cents % BigInt(100)).toString().padStart(2, "0")}`;
}

export function formatExpenseCents(cents: bigint, currency: string): string {
  const absolute = cents < BigInt(0) ? -cents : cents;
  const code = currency.trim().toUpperCase();
  const formatter = new Intl.NumberFormat("en-IN", {
    ...( /^[A-Z]{3}$/.test(code) ? { style: "currency" as const, currency: code } : {} ),
    minimumFractionDigits: 2, maximumFractionDigits: 2,
  });
  const value = formatter.formatToParts(absolute / BigInt(100))
    .map(part => part.type === "fraction" ? (absolute % BigInt(100)).toString().padStart(2, "0") : part.value).join("");
  return `${cents < BigInt(0) ? "-" : ""}${value}${/^[A-Z]{3}$/.test(code) ? "" : ` (${currency || "currency unavailable"})`}`;
}
