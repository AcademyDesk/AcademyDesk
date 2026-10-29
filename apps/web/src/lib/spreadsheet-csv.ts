/** Human spreadsheet-view CSV, not a lossless programmatic interchange format.
 * The tab guard is intentional data in the exported cell, never a stored edit.
 * Client-specific import/re-save behavior still requires acceptance testing.
 */
export function spreadsheetCsvCell(value: string | number | boolean | null | undefined): string {
  let text = value == null ? "" : String(value);
  if (typeof value === "string") {
    for (const character of text) {
      if (/\p{Cc}/u.test(character)) {
        text = `\t${text}`;
        break;
      }
      if (/[\p{White_Space}\p{Cf}']/u.test(character)) continue;
      if (/[=+\-@＝＋－＠]/u.test(character)) text = `\t${text}`;
      break;
    }
  }
  return `"${text.replaceAll('"', '""')}"`;
}

export function spreadsheetCsv(rows: readonly (readonly (string | number | boolean | null | undefined)[])[]): string {
  return rows.map((row) => row.map(spreadsheetCsvCell).join(",")).join("\r\n");
}
