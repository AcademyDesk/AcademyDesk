// Render structured explanatory text, not HTML or raw server diagnostics.
export function studentImportErrors(value: unknown): string | null {
  if (!value || typeof value !== "object" || !("errors" in value) || !Array.isArray(value.errors) || !value.errors.length) return null;
  const errors = value.errors;
  if (!errors.every(error => error && typeof error === "object" && Number.isInteger(error.row) && error.row >= 0 && typeof error.message === "string")) return null;
  return `${errors.length} validation errors: ${errors.map(error => `row ${error.row} ${error.message}`).join("; ")}`;
}
