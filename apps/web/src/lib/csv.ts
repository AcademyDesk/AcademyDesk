/** Encode untrusted text as inert spreadsheet cells without changing the source data. */
export function spreadsheetCsvCell(value: string): string {
  // Match the API policy: skip whitespace/BOM/control prefixes when finding a formula,
  // and protect leading controls even when followed by ordinary text.
  const prefix = value.match(/^[\s\p{Cc}]*/u)?.[0] ?? "";
  const formula = /^[=+\-@＝＋－＠]/u.test(value.slice(prefix.length));
  const text = formula || /\p{Cc}/u.test(prefix) ? "'" + value : value;
  return `"${text.replaceAll('"', '""')}"`;
}

export function spreadsheetCsv(rows: string[][]): string {
  return rows.map((row) => row.map(spreadsheetCsvCell).join(",")).join("\r\n");
}
