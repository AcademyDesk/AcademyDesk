// Defensive rendering boundary, not authorization or proof of model accuracy.
// Keep aligned with MiniReceipt/MiniState and OutstandingFeesService in the API.
export type PentaContext = { academyId: string; name: string; timeZone: string };
export type PentaSession = { conversationId: string; version: number; expiresAtUtc: string };
type PentaRow = { sourceId: string; displayName: string; recordCode?: string | null; subjects: string[]; balances: { currency: string; outstanding: number }[]; sourcePath: string };
export type PentaReceipt = {
  conversationId: string; requestId: string; version: number; kind: string; message: string;
  capability: string; provider: string; protocol: string;
  context: { filters: Record<string, string>; sort_by: string | null; current_learner_id: string | null; current_result_ids: string[] };
  result: null | { count: number; hasMore: boolean; rows: PentaRow[]; asOfUtc: string; source: string; balanceScope: string };
};
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const object = (value: unknown): value is Record<string, unknown> => value !== null && typeof value === "object" && !Array.isArray(value);
const text = (value: unknown): value is string => typeof value === "string" && value.trim().length > 0;
const id = (value: unknown): value is string => typeof value === "string" && guid.test(value);
const timestamp = (value: unknown): value is string => typeof value === "string" &&
  /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})$/.test(value) && Number.isFinite(Date.parse(value));

export function validPentaAccount(value: unknown): value is { academyId: string; roles: string[]; isPlatformOwner: false } {
  return object(value) && id(value.academyId) && value.isPlatformOwner === false && Array.isArray(value.roles) &&
    value.roles.every(role => typeof role === "string") && value.roles.some(role => role === "Owner" || role === "AcademyAdmin");
}

export function validPentaContext(value: unknown, academyId: string): value is PentaContext {
  if (!object(value) || value.academyId !== academyId || !text(value.name) || !text(value.timeZone)) return false;
  // A bad timezone otherwise throws inside rendering after the read completes.
  try { new Intl.DateTimeFormat("en", { timeZone: value.timeZone }).format(0); return true; }
  catch { return false; }
}

export function validPentaSession(value: unknown): value is PentaSession {
  return object(value) && id(value.conversationId) && value.version === 0 && timestamp(value.expiresAtUtc);
}

export function validPentaHealth(value: unknown): value is { status: "Available" | "Degraded" | "Unavailable" } {
  return object(value) && ["Available", "Degraded", "Unavailable"].includes(value.status as string) &&
    value.provider === "PENTA Mini" && value.protocol === "0.1" && value.readOnly === true;
}

function validState(value: unknown): value is PentaReceipt["context"] {
  if (!object(value) || !object(value.filters) || !Array.isArray(value.current_result_ids) ||
      value.current_result_ids.length > 10 || !value.current_result_ids.every(id) ||
      new Set(value.current_result_ids).size !== value.current_result_ids.length ||
      (value.current_learner_id !== null && (!id(value.current_learner_id) || !value.current_result_ids.includes(value.current_learner_id))) ||
      (value.sort_by !== null && value.sort_by !== "outstanding_desc")) return false;
  const entries = Object.entries(value.filters);
  return entries.length <= 3 && entries.every(([key, field]) => ["name", "subject", "balance_status"].includes(key) &&
    text(field) && field.length <= 128 && !/[\u0000-\u001f\u007f-\u009f]/.test(field) &&
    (key !== "balance_status" || field === "Pending" || field === "Clear"));
}

function validRow(value: unknown): value is PentaRow {
  return object(value) && id(value.sourceId) && text(value.displayName) &&
    (value.recordCode == null || typeof value.recordCode === "string") &&
    value.sourcePath === `/student-management?studentId=${value.sourceId}` &&
    Array.isArray(value.subjects) && value.subjects.length <= 5 && value.subjects.every(subject => typeof subject === "string") &&
    Array.isArray(value.balances) && value.balances.every(balance => object(balance) &&
      typeof balance.currency === "string" && /^[A-Z]{3}$/.test(balance.currency) &&
      typeof balance.outstanding === "number" && Number.isFinite(balance.outstanding) && balance.outstanding >= 0) &&
    new Set(value.balances.map(balance => balance.currency)).size === value.balances.length;
}

export function validPentaReceipt(value: unknown, session: PentaSession, requestId: string, capability: string): value is PentaReceipt {
  if (!object(value) || value.conversationId !== session.conversationId || value.requestId !== requestId ||
      !Number.isSafeInteger(value.version) || value.version !== session.version + 1 || value.capability !== capability ||
      value.protocol !== "0.1" || value.provider !== "PENTA Mini" || !text(value.message) || !validState(value.context) ||
      !["RESULT", "ERROR", "REFUSAL", "UNSUPPORTED", "TEXT_RESPONSE", "CLARIFICATION_REQUIRED", "APPROVAL_REQUIRED"].includes(value.kind as string)) return false;
  if (value.result === null) return value.kind !== "RESULT";
  // Only a successful source read or source-backed disambiguation carries rows.
  if (!["RESULT", "CLARIFICATION_REQUIRED"].includes(value.kind as string) || !object(value.result)) return false;
  const result = value.result;
  if (typeof result.count !== "number" || !Number.isSafeInteger(result.count) || result.count < 0 ||
      typeof result.hasMore !== "boolean" || !timestamp(result.asOfUtc) || !text(result.source) || !text(result.balanceScope) ||
      !Array.isArray(result.rows) || result.rows.length !== Math.min(result.count, 10) || !result.rows.every(validRow) ||
      new Set(result.rows.map(row => row.sourceId)).size !== result.rows.length ||
      result.hasMore !== (result.count > result.rows.length)) return false;
  const state = value.context;
  return result.rows.length === state.current_result_ids.length &&
    result.rows.every((row, index) => row.sourceId === state.current_result_ids[index]);
}
