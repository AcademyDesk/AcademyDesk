import { readPentaJson } from "./penta-onboarding-channel";
import { academicSearch, validAdmissionDate, type AcademicSearch, type AcademicChoice } from "./penta-enrollment-assistant";
import { enrollmentConversation, type EnrollmentTransport, type EnrollmentKey } from "./penta-enrollment-creation";

export const lifecycleStatuses = ["Active", "Waitlisted", "Completed", "Withdrawn", "Cancelled", "Paused"] as const;
export type LifecycleStatus = typeof lifecycleStatuses[number];
export type LifecycleSource = { sourceId: string; studentId: string; studentName: string; batchId: string; batchName: string; status: string; startDate: string; endDate: string | null; sourcePath: "/enrollments" };
export type LifecycleSearch = { rows: LifecycleSource[]; hasMore: boolean; asOfUtc: string };
export type LifecycleEdit = { kind: "update"; status: LifecycleStatus; endDate: string | null; reason: string } | { kind: "transfer"; target: AcademicChoice; date: string };
export type LifecycleChange = { tool: "AcademyDesk.Enrollment.Update" | "AcademyDesk.Enrollment.Transfer"; enrollmentId: string; studentId: string; sourceBatchId: string; currentStatus: string; status: LifecycleStatus; endDate: string | null; lifecycleReason: string | null; targetBatchId: string | null; transferDate: string | null };
type Summary = { id: string; studentId: string; batchId: string; startDate: string; endDate: string | null; status: LifecycleStatus };
type Receipt = { receiptId: string; auditId: string; completedAtUtc: string; enrollment: Summary };
export type LifecycleDraft = { requestId: string; revision: 1; contextVersion: 0; expiresAtUtc: string; payloadHash: string; admission: null; change: LifecycleChange } & (
  { status: "Prepared"; approvalId: null; receipt: null } | { status: "Authorized"; approvalId: string; receipt: null } | { status: "Succeeded"; approvalId: string; receipt: Receipt });
export type LifecycleState = {
  availability: "loading" | "ready" | "unavailable"; pending: boolean;
  phase: "idle" | "unknown" | "review" | "approved" | "succeeded" | "expired" | "missing";
  key: EnrollmentKey | null; draft: LifecycleDraft | null; sources: LifecycleSearch | null; batches: AcademicSearch | null;
  notice: string;
};
const object = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
const date = (v: unknown): v is string => typeof v === "string" && validAdmissionDate(v);
const nullableDate = (v: unknown): v is string | null => v === null || date(v);
const utc = (v: unknown): v is string => typeof v === "string" && /^\d{4}-\d{2}-\d{2}T(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d{1,7})?Z$/.test(v) && date(v.slice(0, 10));
const text = (v: unknown, max = 500): v is string => typeof v === "string" && v.trim().length > 0 && v.length <= max && !/[\u0000-\u001f\u007f-\u009f]/.test(v);
const status = (v: unknown): v is LifecycleStatus => lifecycleStatuses.includes(v as LifecycleStatus);
const sourceStatus = (v: unknown) => status(v) || v === "Transferred";
export function lifecycleQuery(v: string): string { const q = v.trim(); if (!text(q, 80) || q.length < 2) throw Error("Enter 2–80 search characters."); return q; }
export function lifecycleSearch(v: unknown, academy: string): LifecycleSearch {
  if (!object(v) || v.tool !== "enrollment.search.v1" || v.capability !== "executor" || v.academyId !== academy || !id(academy) || !utc(v.asOfUtc) || v.maxRows !== 10 || typeof v.hasMore !== "boolean" || !Array.isArray(v.rows) || v.rows.length > 10) throw Error("Unverified enrollment search.");
  const rows = v.rows.map(row => {
    if (!object(row) || !id(row.sourceId) || !id(row.studentId) || !id(row.batchId) || !text(row.studentName) || !text(row.batchName) || !sourceStatus(row.status) || !date(row.startDate) || !nullableDate(row.endDate) || row.sourcePath !== "/enrollments" || Object.keys(row).length !== 9) throw Error("Unverified enrollment source.");
    return row as LifecycleSource;
  });
  if (new Set(rows.map(row => row.sourceId)).size !== rows.length) throw Error("Duplicate enrollment sources.");
  return { rows, hasMore: v.hasMore, asOfUtc: v.asOfUtc };
}
export function lifecycleIntent(source: LifecycleSource, edit: LifecycleEdit): LifecycleChange {
  if (!id(source.sourceId) || !id(source.studentId) || !id(source.batchId) || !sourceStatus(source.status)) throw Error("Select a verified source enrollment.");
  if (edit.kind === "update") {
    const reason = edit.reason.trim();
    if (!status(edit.status) || !nullableDate(edit.endDate) || reason && !text(reason, 1000) || edit.status !== "Active" && !reason) throw Error("Enter valid status, end date and lifecycle reason.");
    return { tool: "AcademyDesk.Enrollment.Update", enrollmentId: source.sourceId, studentId: source.studentId, sourceBatchId: source.batchId, currentStatus: source.status, status: edit.status, endDate: edit.endDate, lifecycleReason: reason || null, targetBatchId: null, transferDate: null };
  }
  if (source.status !== "Active" || !id(edit.target.id) || edit.target.id === source.batchId || !date(edit.date)) throw Error("Select a different verified target batch and transfer date for an Active enrollment.");
  return { tool: "AcademyDesk.Enrollment.Transfer", enrollmentId: source.sourceId, studentId: source.studentId, sourceBatchId: source.batchId, currentStatus: source.status, status: "Active", endDate: null, lifecycleReason: null, targetBatchId: edit.target.id, transferDate: edit.date };
}
export function lifecyclePayload(change: LifecycleChange) {
  return { tool_key: change.tool, tool_revision: 1, fields: change.tool === "AcademyDesk.Enrollment.Update" ? {
    EnrollmentId: { kind: "reference", value: change.enrollmentId }, Status: { kind: "text", value: change.status },
    ...(change.endDate ? { EndDate: { kind: "date", value: change.endDate } } : {}),
    ...(change.lifecycleReason ? { LifecycleReason: { kind: "text", value: change.lifecycleReason } } : {}),
  } : { EnrollmentId: { kind: "reference", value: change.enrollmentId }, TargetBatchId: { kind: "reference", value: change.targetBatchId }, TransferDate: { kind: "date", value: change.transferDate } } };
}
const changeKeys = ["tool", "enrollmentId", "studentId", "sourceBatchId", "currentStatus", "status", "endDate", "lifecycleReason", "targetBatchId", "transferDate"] as const;
export const sameLifecycleChange = (a: LifecycleChange, b: LifecycleChange) => changeKeys.every(k => a[k] === b[k]);
function validChange(v: unknown): v is LifecycleChange {
  if (!object(v) || !id(v.enrollmentId) || !id(v.studentId) || !id(v.sourceBatchId) || !sourceStatus(v.currentStatus) || !status(v.status) || !nullableDate(v.endDate) || !(v.lifecycleReason === null || text(v.lifecycleReason, 1000) && v.lifecycleReason === v.lifecycleReason.trim()) || Object.keys(v).length !== changeKeys.length) return false;
  return v.tool === "AcademyDesk.Enrollment.Update" ? v.targetBatchId === null && v.transferDate === null && (v.status === "Active" || v.lifecycleReason !== null) :
    v.tool === "AcademyDesk.Enrollment.Transfer" && v.currentStatus === "Active" && v.status === "Active" && id(v.targetBatchId) && v.targetBatchId !== v.sourceBatchId && date(v.transferDate) && v.endDate === null && v.lifecycleReason === null;
}
export function lifecycleDraft(v: unknown, key: EnrollmentKey): LifecycleDraft {
  if (!object(v) || !id(key.conversationId) || v.requestId !== key.requestId || v.revision !== 1 || v.contextVersion !== 0 || !utc(v.expiresAtUtc) || typeof v.payloadHash !== "string" || !/^[0-9a-f]{64}$/.test(v.payloadHash) || v.admission !== null || !validChange(v.change)) throw Error("Unverified lifecycle draft.");
  if (v.status === "Prepared" && v.approvalId === null && v.receipt === null || v.status === "Authorized" && id(v.approvalId) && v.receipt === null) return v as LifecycleDraft;
  const r = v.receipt, c = v.change;
  if (v.status !== "Succeeded" || !id(v.approvalId) || !object(r) || !id(r.receiptId) || !id(r.auditId) || !utc(r.completedAtUtc) || !object(r.enrollment)) throw Error("Unverified lifecycle receipt.");
  const e = r.enrollment;
  if (!id(e.id) || e.studentId !== c.studentId || !date(e.startDate) || !nullableDate(e.endDate) || e.status !== c.status || (c.tool === "AcademyDesk.Enrollment.Update" ? e.id !== c.enrollmentId || e.batchId !== c.sourceBatchId || e.endDate !== c.endDate : e.id === c.enrollmentId || e.batchId !== c.targetBatchId || e.startDate !== c.transferDate || e.endDate !== null)) throw Error("Receipt differs from the reviewed lifecycle change.");
  return v as LifecycleDraft;
}
const recoveryKey = "academydesk.penta.enrollment-lifecycle-recovery.v1";
export function readLifecycleRecovery(storage: Storage, academyId: string, epoch: string | null): EnrollmentKey | null {
  const raw = storage.getItem(recoveryKey); if (!raw) return null;
  try { const v: unknown = JSON.parse(raw); if (epoch && object(v) && v.academyId === academyId && v.epoch === epoch && id(v.startRequestId) && id(v.requestId) && (v.conversationId === null || id(v.conversationId)) && Object.keys(v).length === 5) return { startRequestId: v.startRequestId, requestId: v.requestId, conversationId: v.conversationId }; } catch { /* Pointers never grant authority. */ }
  storage.removeItem(recoveryKey); return null;
}
export function saveLifecycleRecovery(storage: Storage, academyId: string, epoch: string | null, key: EnrollmentKey) {
  if (!id(academyId) || !epoch || !id(key.startRequestId) || !id(key.requestId) || key.conversationId !== null && !id(key.conversationId)) throw Error("A current scoped pointer is required.");
  storage.setItem(recoveryKey, JSON.stringify({ academyId, epoch, ...key }));
}
class RecoveryUnavailable extends Error {}
class HostError extends Error { constructor(readonly status: number) { super("Lifecycle host request failed."); } }
export const initialLifecycleState: LifecycleState = { availability: "loading", pending: false, phase: "idle", key: null, draft: null, sources: null, batches: null, notice: "Checking enrollment change availability…" };

/** No mutation is sent by initialisation, search, recovery or reload. */
export function createEnrollmentLifecycle(transport: EnrollmentTransport, storage: () => Storage, changed: (v: LifecycleState) => void, uuid: () => string = () => crypto.randomUUID(), now: () => number = Date.now) {
  let state: LifecycleState = { ...initialLifecycleState }, disposed = false, busy = false;
  let expected: LifecycleDraft | null = null, intent: LifecycleChange | null = null, sourceStart: string | null = null;
  const current = () => !disposed && !transport.signal.aborted && transport.current();
  function update(v: Partial<LifecycleState>) { if (current()) { state = { ...state, ...v }; changed(state); } }
  async function call(path: string, method: "GET" | "PUT" | "POST", body?: unknown, accepted = [200]) {
    if (!current()) throw Error("Obsolete session.");
    const response = await transport.send(path, method, body);
    if (!current()) throw Error("Obsolete response.");
    if (response.status === 401 || response.status === 403) { update({ ...initialLifecycleState, availability: "unavailable", notice: "Current access could not be verified. Open Manual Enrollments." }); transport.invalidate(); throw new HostError(response.status); }
    if (!accepted.includes(response.status)) { await response.body?.cancel().catch(() => {}); throw new HostError(response.status); }
    if (!response.headers.get("cache-control")?.includes("no-store")) throw Error("Response may be cached.");
    const value = await readPentaJson(response, transport.signal);
    if (!current()) throw Error("Obsolete response.");
    return value;
  }
  const path = (key: EnrollmentKey) => { if (!id(key.conversationId) || !id(key.requestId)) throw Error("Verified conversation required."); return `/conversations/${key.conversationId}/drafts/${key.requestId}`; };
  function accept(v: LifecycleDraft) {
    if (intent && !sameLifecycleChange(intent, v.change) || expected && (expected.requestId !== v.requestId || expected.payloadHash !== v.payloadHash || expected.expiresAtUtc !== v.expiresAtUtc || !sameLifecycleChange(expected.change, v.change) || expected.approvalId !== null && expected.approvalId !== v.approvalId)) throw Error("The saved operation differs from the exact review.");
    if (v.status === "Succeeded" && sourceStart && v.change.tool === "AcademyDesk.Enrollment.Update" && v.receipt.enrollment.startDate !== sourceStart) throw Error("Source date changed.");
    if (expected?.status === "Succeeded" && (v.status !== "Succeeded" || JSON.stringify(v.receipt) !== JSON.stringify(expected.receipt))) throw Error("The saved receipt changed.");
    expected = v;
    update({ draft: v, phase: v.status === "Prepared" ? "review" : v.status === "Authorized" ? "approved" : "succeeded", notice: "" });
  }
  function failure(error: unknown) {
    if (!current()) return;
    if (error instanceof RecoveryUnavailable) { update({ notice: "The next step was not sent because recovery details could not be saved. Check this operation or open Manual Enrollments." }); return; }
    if (error instanceof HostError && error.status === 410) update({ phase: "expired", draft: null, notice: "This enrollment change expired. Check Manual Enrollments before starting again." });
    else if (error instanceof HostError && error.status === 404 && state.key) update({ phase: "missing", draft: null, notice: "No saved enrollment change could be verified. Check Manual Enrollments before starting again." });
    else update({ notice: state.key ? "The change outcome could not be confirmed. Check this same operation; do not submit another change." : "Enrollment changes are unavailable or the request was refused. Check the fields or open Manual Enrollments." });
  }
  async function run(action: () => Promise<void>) { if (busy || !current()) return; busy = true; update({ pending: true, notice: "" }); try { await action(); } catch (e) { failure(e); } finally { busy = false; update({ pending: false }); } }
  function persist(key: EnrollmentKey) {
    try { const saved = readLifecycleRecovery(storage(), transport.academyId, transport.epoch); if (saved && (saved.startRequestId !== key.startRequestId || saved.requestId !== key.requestId || saved.conversationId !== null && saved.conversationId !== key.conversationId)) throw new RecoveryUnavailable(); saveLifecycleRecovery(storage(), transport.academyId, transport.epoch, key); } catch { throw new RecoveryUnavailable(); }
  }
  async function recoverSaved() {
    let key = state.key; if (!key || state.availability !== "ready") return;
    update({ phase: "unknown", draft: null });
    if (!key.conversationId) { const conversationId = enrollmentConversation(await call(`/starts/${key.startRequestId}`, "GET")); if (!current()) return; key = { ...key, conversationId }; persist(key); update({ key }); }
    accept(lifecycleDraft(await call(path(key), "GET"), key));
  }
  return {
    snapshot: () => state,
    initialise: () => run(async () => {
      update({ availability: "loading" }); const key = state.key ?? readLifecycleRecovery(storage(), transport.academyId, transport.epoch); if (key) update({ key, phase: "unknown" });
      const r = await call("/readiness", "GET");
      if (!object(r) || r.protocol !== "enrollment-lifecycle-browser.v1" || r.requiresApproval !== true || !Array.isArray(r.tools) || r.tools.length !== 2 || !r.tools.includes("AcademyDesk.Enrollment.Update") || !r.tools.includes("AcademyDesk.Enrollment.Transfer")) throw Error("Unverified lifecycle availability.");
      update({ availability: "ready" }); if (key) await recoverSaved();
    }).then(() => { if (current() && state.availability === "loading") update({ availability: "unavailable" }); }),
    search: (query: string, kind: "source" | "batch") => run(async () => {
      if (state.availability !== "ready" || state.key) return;
      const q = lifecycleQuery(query);
      if (kind === "source") { update({ sources: null }); update({ sources: lifecycleSearch(await call(`/search?q=${encodeURIComponent(q)}`, "GET"), transport.academyId) }); }
      else { update({ batches: null }); update({ batches: academicSearch(await call(`/batch-search?q=${encodeURIComponent(q)}`, "GET"), "batch", transport.academyId) }); }
    }),
    prepare: (source: LifecycleSource, edit: LifecycleEdit) => run(async () => {
      if (state.availability !== "ready" || state.key || !state.sources?.rows.includes(source) || edit.kind === "transfer" && !state.batches?.rows.includes(edit.target)) return;
      const requested = lifecycleIntent(source, edit), payload = lifecyclePayload(requested);
      const existing = readLifecycleRecovery(storage(), transport.academyId, transport.epoch);
      if (existing) { update({ key: existing, phase: "unknown", notice: "Check the saved operation first." }); return; }
      let key: EnrollmentKey = { startRequestId: uuid(), requestId: uuid(), conversationId: null }; persist(key);
      intent = requested; sourceStart = source.startDate; expected = null;
      update({ key, phase: "unknown", sources: null, batches: null });
      const conversationId = enrollmentConversation(await call(`/conversations/${key.startRequestId}`, "PUT", {}, [200, 201]));
      if (!current()) return; key = { ...key, conversationId }; persist(key); update({ key });
      const v = lifecycleDraft(await call(path(key), "PUT", { contextVersion: 0, payload }, [200, 201]), key);
      if (v.status !== "Prepared") throw Error("Fresh review required."); accept(v);
    }),
    approve: (reviewed: LifecycleDraft) => run(async () => {
      const key = state.key, d = state.draft;
      if (state.availability !== "ready" || state.phase !== "review" || !key || d !== reviewed || d?.status !== "Prepared") return;
      if (Date.parse(d.expiresAtUtc) <= now()) throw new HostError(410); persist(key); update({ phase: "unknown", draft: null });
      const v = lifecycleDraft(await call(path(key) + "/approve", "POST", { revision: d.revision, payloadHash: d.payloadHash, intent: d.change.tool === "AcademyDesk.Enrollment.Update" ? "ApproveEnrollmentUpdate" : "ApproveEnrollmentTransfer" }), key);
      if (v.status !== "Authorized") throw Error("Unverified approval."); accept(v);
    }),
    execute: (approved: LifecycleDraft) => run(async () => {
      const key = state.key, d = state.draft;
      if (state.availability !== "ready" || state.phase !== "approved" || !key || d !== approved || d?.status !== "Authorized") return;
      if (Date.parse(d.expiresAtUtc) <= now()) throw new HostError(410); persist(key); update({ phase: "unknown", draft: null });
      const v = lifecycleDraft(await call(path(key) + "/execute", "POST", { revision: d.revision, payloadHash: d.payloadHash, approvalId: d.approvalId }, [200, 201]), key);
      if (v.status !== "Succeeded") throw Error("Committed receipt required."); accept(v);
    }),
    recover: (key: EnrollmentKey) => run(async () => { if (key === state.key) await recoverSaved(); }),
    close: (key: EnrollmentKey) => {
      if (busy || !current() || key !== state.key) return;
      try { const saved = readLifecycleRecovery(storage(), transport.academyId, transport.epoch); if (!saved || saved.startRequestId === key.startRequestId && saved.requestId === key.requestId && saved.conversationId === key.conversationId) storage().removeItem(recoveryKey); } catch { update({ notice: "Recovery details could not be cleared. Keep this operation open." }); return; }
      expected = null; intent = null; sourceStart = null; update({ key: null, draft: null, sources: null, batches: null, phase: "idle", notice: "" });
    },
    dispose: () => { disposed = true; expected = null; intent = null; sourceStart = null; state = { ...initialLifecycleState, availability: "unavailable", notice: "" }; },
  };
}
