// Defensive host rendering only. Search/preview is never approval or execution.
export type SearchKind = "student" | "batch";
export type AcademicChoice = { id: string; label: string; path: string };
export type AcademicSearch = { kind: SearchKind; rows: AcademicChoice[]; hasMore: boolean; asOfUtc: string };
export type Admission = { studentId: string; batchId: string; startDate: string; status: "Active" | "Waitlisted" };
export type AdmissionPreview = Admission & { asOfUtc: string };
const object = (value: unknown): value is Record<string, unknown> => value !== null && typeof value === "object" && !Array.isArray(value);
const id = (value: unknown): value is string => typeof value === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(value) && value !== "00000000-0000-0000-0000-000000000000";
const label = (value: unknown): value is string => typeof value === "string" && value.trim().length > 0 && value.length <= 500 && !/[\u0000-\u001f\u007f]/.test(value);
const timestamp = (value: unknown): value is string => typeof value === "string" && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$/.test(value) && Number.isFinite(Date.parse(value));
export const validAdmissionDate = (value: string) => /^\d{4}-\d{2}-\d{2}$/.test(value) && Number(value.slice(0, 4)) >= 1 && Number.isFinite(Date.parse(value + "T00:00:00Z")) && new Date(value + "T00:00:00Z").toISOString().slice(0, 10) === value;

/** Deliberately bounded commands, not natural-language model inference. */
export function academicCommand(prompt: string): { kind: SearchKind; query: string } | null {
  const match = /^find (student|batch) (.+)$/i.exec(prompt.trim());
  if (!match) return null;
  const query = match[2].trim();
  if (query.length < 2 || query.length > 80 || /[\u0000-\u001f\u007f]/.test(query)) return null;
  return { kind: match[1].toLowerCase() as SearchKind, query };
}

export function academicSearch(value: unknown, kind: SearchKind, academyId: string): AcademicSearch {
  if (!object(value) || value.academyId !== academyId || value.tool !== `${kind}.search.v1` || value.capability !== "executor" ||
      value.source !== (kind === "student" ? "Students" : "Batches") || value.maxRows !== 10 || typeof value.hasMore !== "boolean" ||
      !timestamp(value.asOfUtc) || typeof value.timeZone !== "string" || !Array.isArray(value.rows) || value.rows.length > 10) throw Error("Unverified search result.");
  try { new Intl.DateTimeFormat("en", { timeZone: value.timeZone }).format(0); } catch { throw Error("Unverified timezone."); }
  const rows = value.rows.map(row => {
    if (!object(row) || !id(row.sourceId)) throw Error("Unverified source.");
    const name = kind === "student" ? row.displayName : row.name;
    const path = kind === "student" ? `/student-management?studentId=${row.sourceId}` : "/batch-setup";
    if (!label(name) || row.sourcePath !== path || kind === "student" && row.isActive !== true ||
        kind === "batch" && row.batchCode != null && (typeof row.batchCode !== "string" || row.batchCode.length > 200 || /[\u0000-\u001f\u007f]/.test(row.batchCode))) throw Error("Unverified source label.");
    return { id: row.sourceId, label: name, path };
  });
  if (new Set(rows.map(row => row.id)).size !== rows.length) throw Error("Duplicate sources.");
  return { kind, rows, hasMore: value.hasMore, asOfUtc: value.asOfUtc };
}

export function admissionPayload(admission: Admission) {
  if (!id(admission.studentId) || !id(admission.batchId) || !validAdmissionDate(admission.startDate) || !["Active", "Waitlisted"].includes(admission.status)) throw Error("Invalid enrollment intent.");
  return { tool_key: "AcademyDesk.Enrollment.Create", tool_revision: 1, fields: {
    StudentId: { kind: "reference", value: admission.studentId }, BatchId: { kind: "reference", value: admission.batchId },
    StartDate: { kind: "date", value: admission.startDate }, Status: { kind: "text", value: admission.status },
  } };
}
export function admissionPreview(value: unknown, expected: Admission): AdmissionPreview {
  if (!object(value) || value.tool !== "AcademyDesk.Enrollment.Create" || value.status !== "PreviewOnly" || value.requiresApproval !== true ||
      !timestamp(value.asOfUtc) || !object(value.admission)) throw Error("Unverified admission preview.");
  const admission = value.admission;
  if (Object.keys(expected).some(key => admission[key] !== expected[key as keyof Admission])) throw Error("The host preview differs from the requested enrollment.");
  return { ...expected, asOfUtc: value.asOfUtc };
}
