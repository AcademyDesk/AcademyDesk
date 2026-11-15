import { readPentaJson } from "./penta-onboarding-channel";
import { admissionPayload, validAdmissionDate, type Admission } from "./penta-enrollment-assistant";

export type EnrollmentTransport = {
  academyId: string; epoch: string | null; signal: AbortSignal; current(): boolean; invalidate(): void;
  send(path: string, method: "GET" | "PUT" | "POST", body?: unknown): Promise<Response>;
};
export type EnrollmentKey = { startRequestId: string; requestId: string; conversationId: string | null };
export type EnrollmentCandidate = Admission & { studentLabel: string; batchLabel: string };
type Receipt = { receiptId: string; auditId: string; completedAtUtc: string; enrollment: Admission & { id: string; endDate: null } };
export type EnrollmentDraft = { requestId: string; revision: 1; contextVersion: 0; expiresAtUtc: string;
  payloadHash: string; admission: Admission } & (
  { status: "Prepared"; approvalId: null; receipt: null } |
  { status: "Authorized"; approvalId: string; receipt: null } |
  { status: "Succeeded"; approvalId: string; receipt: Receipt });
export type EnrollmentCreationState = {
  availability: "loading" | "ready" | "unavailable"; pending: boolean;
  phase: "idle" | "unknown" | "review" | "approved" | "succeeded" | "expired" | "missing";
  key: EnrollmentKey | null; draft: EnrollmentDraft | null; labels: { student: string; batch: string } | null; notice: string;
};
const object = (value: unknown): value is Record<string, unknown> => value !== null && typeof value === "object" && !Array.isArray(value);
const id = (value: unknown): value is string => typeof value === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(value) && value !== "00000000-0000-0000-0000-000000000000";
const utc = (value: unknown): value is string => typeof value === "string" && /^\d{4}-\d{2}-\d{2}T(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d{1,7})?Z$/.test(value) && validAdmissionDate(value.slice(0, 10)) && Number.isFinite(Date.parse(value));
const admission = (value: unknown): value is Admission => object(value) && id(value.studentId) && id(value.batchId) &&
  typeof value.startDate === "string" && validAdmissionDate(value.startDate) && (value.status === "Active" || value.status === "Waitlisted");
export const sameAdmission = (left: Admission, right: Admission) => left.studentId === right.studentId && left.batchId === right.batchId && left.startDate === right.startDate && left.status === right.status;
export function enrollmentConversation(value: unknown): string {
  if (!object(value) || !id(value.conversationId) || value.version !== 0 || !utc(value.expiresAtUtc)) throw Error("Unverified enrollment conversation.");
  return value.conversationId;
}
export function enrollmentDraft(value: unknown, key: EnrollmentKey): EnrollmentDraft {
  if (!object(value) || !id(key.conversationId) || value.requestId !== key.requestId || value.revision !== 1 || value.contextVersion !== 0 ||
      !utc(value.expiresAtUtc) || typeof value.payloadHash !== "string" || !/^[0-9a-f]{64}$/.test(value.payloadHash) ||
      !admission(value.admission) || value.change != null) throw Error("Unverified enrollment draft.");
  if (value.status === "Prepared" && value.approvalId === null && value.receipt === null ||
      value.status === "Authorized" && id(value.approvalId) && value.receipt === null) return value as EnrollmentDraft;
  const receipt = value.receipt;
  if (value.status !== "Succeeded" || !id(value.approvalId) || !object(receipt) || !id(receipt.receiptId) || !id(receipt.auditId) ||
      !utc(receipt.completedAtUtc) || !object(receipt.enrollment) || !id(receipt.enrollment.id) ||
      receipt.enrollment.endDate !== null || !admission(receipt.enrollment) || !sameAdmission(value.admission, receipt.enrollment)) throw Error("Unverified enrollment receipt.");
  return value as EnrollmentDraft;
}
export function sameEnrollmentReview(expected: EnrollmentDraft, actual: EnrollmentDraft): boolean {
  return expected.requestId === actual.requestId && expected.payloadHash === actual.payloadHash &&
    expected.expiresAtUtc === actual.expiresAtUtc && sameAdmission(expected.admission, actual.admission);
}
const recoveryKey = "academydesk.penta.enrollment-creation-recovery.v1";
export function readEnrollmentRecovery(storage: Storage, academyId: string, epoch: string | null): EnrollmentKey | null {
  const raw = storage.getItem(recoveryKey);
  if (!raw) return null;
  try {
    const value: unknown = JSON.parse(raw);
    if (epoch && object(value) && value.academyId === academyId && value.epoch === epoch && id(value.startRequestId) && id(value.requestId) &&
        (value.conversationId === null || id(value.conversationId)) && Object.keys(value).length === 5)
      return { startRequestId: value.startRequestId, requestId: value.requestId, conversationId: value.conversationId };
  } catch { /* Untrusted pointers never become operations. */ }
  storage.removeItem(recoveryKey);
  return null;
}
export function saveEnrollmentRecovery(storage: Storage, academyId: string, epoch: string | null, key: EnrollmentKey) {
  if (!id(academyId) || !epoch || !id(key.startRequestId) || !id(key.requestId) || key.conversationId !== null && !id(key.conversationId)) throw Error("A current scoped recovery pointer is required.");
  storage.setItem(recoveryKey, JSON.stringify({ academyId, epoch, ...key }));
}
class RecoveryUnavailable extends Error {}
class EnrollmentHttpError extends Error { constructor(readonly status: number) { super("Enrollment host request failed."); } }
export const initialEnrollmentCreationState: EnrollmentCreationState = { availability: "loading", pending: false, phase: "idle", key: null, draft: null, labels: null, notice: "Checking enrollment creation availability…" };

/** Every domain write is an explicit call. Initialisation/recovery issue GET only. */
export function createEnrollmentCreation(transport: EnrollmentTransport, storage: () => Storage,
  changed: (value: EnrollmentCreationState) => void, uuid: () => string = () => crypto.randomUUID(), now: () => number = Date.now) {
  let state: EnrollmentCreationState = { ...initialEnrollmentCreationState }, disposed = false, busy = false;
  let expected: EnrollmentDraft | null = null, intent: Admission | null = null, latestCandidate: EnrollmentCandidate | null = null;
  const current = () => !disposed && !transport.signal.aborted && transport.current();
  function update(value: Partial<EnrollmentCreationState>) { if (current()) { state = { ...state, ...value }; changed(state); } }
  async function call(path: string, method: "GET" | "PUT" | "POST", body?: unknown, accepted = [200]) {
    if (!current()) throw Error("Obsolete enrollment session.");
    const response = await transport.send(path, method, body);
    if (!current()) throw Error("Obsolete enrollment response.");
    if (response.status === 401 || response.status === 403) { update({ ...initialEnrollmentCreationState, availability: "unavailable", notice: "Current academy access could not be verified. Open Manual Enrollments." }); transport.invalidate(); throw new EnrollmentHttpError(response.status); }
    if (!accepted.includes(response.status)) { await response.body?.cancel().catch(() => {}); throw new EnrollmentHttpError(response.status); }
    if (!response.headers.get("cache-control")?.includes("no-store")) throw Error("Enrollment response may be cached.");
    const value = await readPentaJson(response, transport.signal);
    if (!current()) throw Error("Obsolete enrollment response.");
    return value;
  }
  const path = (key: EnrollmentKey) => {
    if (!id(key.conversationId) || !id(key.requestId)) throw Error("A verified conversation is required.");
    return `/browser-conversations/${key.conversationId}/drafts/${key.requestId}`;
  };
  function accept(value: EnrollmentDraft) {
    if (intent && !sameAdmission(intent, value.admission) || expected && (!sameEnrollmentReview(expected, value) || expected.approvalId !== null && expected.approvalId !== value.approvalId))
      throw Error("The saved enrollment differs from the reviewed operation.");
    if (expected?.status === "Succeeded" && (value.status !== "Succeeded" || value.receipt.receiptId !== expected.receipt.receiptId || value.receipt.auditId !== expected.receipt.auditId || value.receipt.enrollment.id !== expected.receipt.enrollment.id || value.receipt.completedAtUtc !== expected.receipt.completedAtUtc))
      throw Error("The saved receipt changed.");
    expected = value;
    update({ draft: value, phase: value.status === "Prepared" ? "review" : value.status === "Authorized" ? "approved" : "succeeded", notice: "" });
  }
  function failure(error: unknown) {
    if (!current()) return;
    if (error instanceof RecoveryUnavailable) { update({ notice: "The next step was not sent because recovery details could not be saved. Check this operation or open Manual Enrollments." }); return; }
    if (error instanceof EnrollmentHttpError && error.status === 410) update({ phase: "expired", draft: null, notice: "This enrollment operation expired. Check Manual Enrollments before starting again." });
    else if (error instanceof EnrollmentHttpError && error.status === 404 && state.key) update({ phase: "missing", draft: null, notice: "No saved enrollment operation could be verified. Check Manual Enrollments before starting again." });
    else update({ notice: state.key ? "The enrollment outcome could not be confirmed. Check this same operation; do not start another enrollment." : "Enrollment creation is unavailable here. Manual Enrollments remains available." });
  }
  async function run(action: () => Promise<void>) {
    if (busy || !current()) return;
    busy = true; update({ pending: true, notice: "" });
    try { await action(); } catch (error) { failure(error); }
    finally { busy = false; update({ pending: false }); }
  }
  function persist(key: EnrollmentKey) {
    try {
      const saved = readEnrollmentRecovery(storage(), transport.academyId, transport.epoch);
      if (saved && (saved.startRequestId !== key.startRequestId || saved.requestId !== key.requestId || saved.conversationId !== null && saved.conversationId !== key.conversationId)) throw new RecoveryUnavailable();
      saveEnrollmentRecovery(storage(), transport.academyId, transport.epoch, key);
    } catch { throw new RecoveryUnavailable(); }
  }
  async function recoverSaved() {
    let key = state.key;
    if (!key || state.availability !== "ready") return;
    update({ phase: "unknown", draft: null });
    if (!key.conversationId) {
      const conversationId = enrollmentConversation(await call(`/browser-starts/${key.startRequestId}`, "GET"));
      if (!current()) return;
      key = { ...key, conversationId }; persist(key); update({ key });
    }
    accept(enrollmentDraft(await call(path(key), "GET"), key));
  }
  return {
    snapshot: () => state,
    initialise: () => run(async () => {
      update({ availability: "loading" });
      const key = state.key ?? readEnrollmentRecovery(storage(), transport.academyId, transport.epoch);
      if (key) update({ key, phase: "unknown" });
      const readiness = await call("/browser-readiness", "GET");
      if (!object(readiness) || readiness.protocol !== "enrollment-create-browser.v1" || readiness.requiresApproval !== true) throw Error("Unverified enrollment availability.");
      update({ availability: "ready" });
      if (key) await recoverSaved();
    }).then(() => { if (current() && state.availability === "loading") update({ availability: "unavailable" }); }),
    prepare: (candidate: EnrollmentCandidate) => run(async () => {
      if (state.availability !== "ready" || state.key || candidate !== latestCandidate) return;
      const requested: Admission = { studentId: candidate.studentId, batchId: candidate.batchId, startDate: candidate.startDate, status: candidate.status };
      const payload = admissionPayload(requested);
      const existing = readEnrollmentRecovery(storage(), transport.academyId, transport.epoch);
      if (existing) { update({ key: existing, phase: "unknown", notice: "A saved operation must be checked first." }); return; }
      let key: EnrollmentKey = { startRequestId: uuid(), requestId: uuid(), conversationId: null };
      persist(key);
      intent = requested; expected = null;
      update({ key, phase: "unknown", labels: { student: candidate.studentLabel, batch: candidate.batchLabel } });
      const conversationId = enrollmentConversation(await call(`/browser-conversations/${key.startRequestId}`, "PUT", {}, [200, 201]));
      if (!current()) return;
      key = { ...key, conversationId }; persist(key); update({ key });
      const draft = enrollmentDraft(await call(path(key), "PUT", { contextVersion: 0, payload }, [200, 201]), key);
      if (draft.status !== "Prepared") throw Error("A fresh review is required.");
      accept(draft);
    }),
    setCandidate: (candidate: EnrollmentCandidate | null) => { latestCandidate = candidate; },
    approve: (reviewed: EnrollmentDraft) => run(async () => {
      const key = state.key, draft = state.draft;
      if (state.availability !== "ready" || state.phase !== "review" || !key || draft !== reviewed || draft?.status !== "Prepared") return;
      if (Date.parse(draft.expiresAtUtc) <= now()) throw new EnrollmentHttpError(410);
      persist(key);
      update({ phase: "unknown", draft: null });
      const result = enrollmentDraft(await call(path(key) + "/approve", "POST", { revision: draft.revision, payloadHash: draft.payloadHash, intent: "ApproveEnrollmentCreate" }), key);
      if (result.status !== "Authorized") throw Error("Approval could not be verified.");
      accept(result);
    }),
    execute: (approved: EnrollmentDraft) => run(async () => {
      const key = state.key, draft = state.draft;
      if (state.availability !== "ready" || state.phase !== "approved" || !key || draft !== approved || draft?.status !== "Authorized") return;
      if (Date.parse(draft.expiresAtUtc) <= now()) throw new EnrollmentHttpError(410);
      persist(key);
      update({ phase: "unknown", draft: null });
      const result = enrollmentDraft(await call(path(key) + "/execute", "POST", { revision: draft.revision, payloadHash: draft.payloadHash, approvalId: draft.approvalId }, [200, 201]), key);
      if (result.status !== "Succeeded") throw Error("A committed receipt is required.");
      accept(result);
    }),
    recover: (key: EnrollmentKey) => run(async () => { if (key === state.key) await recoverSaved(); }),
    close: (key: EnrollmentKey) => {
      if (busy || !current() || key !== state.key) return;
      try { const saved = readEnrollmentRecovery(storage(), transport.academyId, transport.epoch); if (!saved || saved.startRequestId === key.startRequestId && saved.requestId === key.requestId && saved.conversationId === key.conversationId) storage().removeItem(recoveryKey); } catch { update({ notice: "Recovery details could not be cleared. Keep this operation open." }); return; }
      expected = null; intent = null; latestCandidate = null; update({ key: null, draft: null, labels: null, phase: "idle", notice: "" });
    },
    dispose: () => { disposed = true; expected = null; intent = null; latestCandidate = null; state = { ...initialEnrollmentCreationState, availability: "unavailable", notice: "" }; },
  };
}
