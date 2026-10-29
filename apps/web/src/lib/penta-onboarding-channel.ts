// One authenticated, one-shot draft turn. This is not a tool/approval dispatcher.
// A summary is deliberately NOT an operational-ui Projection or a domain receipt.
import { apiUrl, portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalWorkspaceForPath, isPortalSignOutEvent, portalSignOutEvent } from "./api";

export type OnboardingBinding = {
  academyId: string; actorId: string; conversationId: string; draftId: string;
  contextVersion: number; authorityVersion: number;
  expectedVersion: number; draftRevision: number; payloadHash: string;
  // Hydrate only from the authenticated host's pending cursor after reload.
  pendingRequestId?: string;
};
export type OnboardingTurnSummary = {
  conversationId: string; requestId: string; version: number; draftRevision: number | null;
  kind: "pending" | "host_prepare" | "clarify" | "unsupported" | "request_pause" |
    "request_resume" | "request_cancel" | "unavailable" | "invalid_response";
  status: string; canExecute: false; domainEffect: false; questionKeys: string[];
};
type Request = (input: string, init: RequestInit) => Promise<Response>;
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
const id = (value: unknown): value is string => typeof value === "string" && guid.test(value) && value !== "00000000-0000-0000-0000-000000000000";
const integer = (value: unknown, min: number, max: number): value is number => Number.isSafeInteger(value) && Number(value) >= min && Number(value) <= max;
const object = (value: unknown): value is Record<string, unknown> => value !== null && typeof value === "object" && !Array.isArray(value);
const failed = () => new Error("The onboarding outcome could not be verified. Check the same request status; do not resubmit. Manual access remains available.");
const maxBytes = 65536;

export async function readPentaJson(response: Response, signal: AbortSignal): Promise<unknown> {
  if (response.redirected || !/^application\/json(?:;|$)/i.test(response.headers.get("content-type") ?? "") ||
      Number(response.headers.get("content-length") ?? 0) > maxBytes || !response.body) throw failed();
  const reader = response.body.getReader();
  const chunks: Uint8Array[] = [];
  let size = 0;
  const abort = () => { void reader.cancel().catch(() => {}); };
  signal.addEventListener("abort", abort, { once: true });
  try {
    while (true) {
      if (signal.aborted) throw failed();
      const { done, value } = await reader.read();
      if (signal.aborted) throw failed();
      if (done) break;
      size += value.byteLength;
      if (size > maxBytes) throw failed();
      chunks.push(value);
    }
    const bytes = new Uint8Array(size);
    let offset = 0;
    for (const chunk of chunks) { bytes.set(chunk, offset); offset += chunk.byteLength; }
    return JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
  } finally {
    signal.removeEventListener("abort", abort);
    await reader.cancel().catch(() => {});
    reader.releaseLock();
  }
}

export type OnboardingResultBinding = Pick<OnboardingBinding, "academyId" | "actorId" | "conversationId" | "draftId" |
  "expectedVersion" | "draftRevision" | "authorityVersion" | "contextVersion">;
/** Verifies a read result only; never an execution grant or a projection. */
export function verifyOnboardingTurnSummary(value: unknown, status: number, b: OnboardingResultBinding, requestId: string): OnboardingTurnSummary {
  if (!object(value) || value.conversationId !== b.conversationId || value.requestId !== requestId ||
      value.version !== b.expectedVersion + 1 || value.canExecute !== false) throw failed();
  if (status === 202) {
    if (value.status !== "pending" || Object.keys(value).some(key => !["conversationId", "requestId", "version", "status", "canExecute"].includes(key))) throw failed();
    return { conversationId: b.conversationId, requestId, version: value.version as number,
      draftRevision: null, kind: "pending", status: "pending", canExecute: false, domainEffect: false, questionKeys: [] };
  }
  const kinds: Record<string, string> = { host_prepare: "draft_updated", clarify: "clarification_required",
    unsupported: "unsupported", request_pause: "draft_updated", request_resume: "draft_updated",
    request_cancel: "draft_updated", unavailable: "unavailable", invalid_response: "invalid_response" };
  const kind = typeof value.kind === "string" ? value.kind : "";
  if (!Object.hasOwn(kinds, kind) || value.draftId !== b.draftId ||
      !Array.isArray(value.operations) || value.operations.length !== (kind === "host_prepare" ? 1 : 0) ||
      !Array.isArray(value.questions) || value.questions.length > 3 ||
      !value.questions.every(key => typeof key === "string" && /^[A-Za-z][A-Za-z0-9_]{0,63}$/.test(key)) ||
      new Set(value.questions).size !== value.questions.length || kind !== "clarify" && value.questions.length > 0) throw failed();
  // A clarify may retain safe partial data and therefore advance the draft.
  const updated = value.status === "draft_updated";
  if (value.status !== kinds[kind] && !(kind === "clarify" && updated) ||
      value.draftRevision !== b.draftRevision + (updated ? 1 : 0)) throw failed();
  const snapshot = value.snapshot;
  if (!object(snapshot) || snapshot.version !== "operational-draft-0.3.0" || snapshot.availability !== "ready" ||
      !object(snapshot.scope) || !object(snapshot.draft) || snapshot.scope.tenant_id !== b.academyId ||
      snapshot.scope.actor_id !== b.actorId || snapshot.scope.conversation_id !== b.conversationId ||
      snapshot.scope.connection_id !== `academydesk-onboarding:${b.draftId.replaceAll("-", "")}` ||
      snapshot.scope.authority_version !== b.authorityVersion || snapshot.scope.context_version !== b.contextVersion ||
      snapshot.draft.ref !== b.draftId || snapshot.draft.revision !== value.draftRevision ||
      snapshot.draft.tool_key !== "AcademyDesk.StudentOnboarding.Create") throw failed();
  const draftStatuses: Record<string, string> = { host_prepare: "awaiting_approval", clarify: "collecting",
    request_pause: "paused", request_resume: "collecting", request_cancel: "cancelled" };
  if (draftStatuses[kind] && snapshot.draft.status !== draftStatuses[kind]) throw failed();
  if (kind === "host_prepare") {
    const op = value.operations[0];
    const ref = `onboarding:${b.draftId.replaceAll("-", "")}:${value.draftRevision}`;
    if (!object(op) || op.ref !== ref || op.request_ref !== requestId || op.draft_ref !== b.draftId ||
        op.draft_revision !== value.draftRevision || op.tool_key !== "AcademyDesk.StudentOnboarding.Create" ||
        op.tool_revision !== 1 || op.approval_ref !== null || op.status !== "awaiting_approval" ||
        snapshot.draft.operation_ref !== ref) throw failed();
  }
  // Never forward the raw snapshot, fields, operations, URLs or server message to UI.
  // A later full projection adapter must independently validate its entire contract.
  return { conversationId: b.conversationId, requestId, version: value.version as number,
    draftRevision: value.draftRevision as number, kind: kind as OnboardingTurnSummary["kind"],
    status: value.status as string, canExecute: false, domainEffect: false, questionKeys: [...value.questions] as string[] };
}

/** Binding must come from the current authenticated host, never chat/model text.
 * Browser binding checks are defensive only; the server independently authorizes.
 * One instance admits at most one POST. Recovery is an explicit GET of that ID.
 */
export function createOnboardingTurnChannel(binding: OnboardingBinding, request: Request = fetch, browserContracts = false) {
  const b = { ...binding };
  if (![b.academyId, b.actorId, b.conversationId, b.draftId].every(id) ||
      !integer(b.expectedVersion, 0, 99) || !integer(b.draftRevision, 1, 99) ||
      !integer(b.authorityVersion, 1, Number.MAX_SAFE_INTEGER) || !integer(b.contextVersion, 1, Number.MAX_SAFE_INTEGER) ||
      typeof b.payloadHash !== "string" || !/^[0-9a-f]{64}$/.test(b.payloadHash) ||
      b.pendingRequestId !== undefined && !id(b.pendingRequestId)) throw failed();
  const inAdminWorkspace = () => typeof window !== "undefined" && portalWorkspaceForPath(window.location.pathname) === "AcademyAdmin";
  const bearer = portalAccessToken("AcademyAdmin");
  const loginEpoch = portalSessionEpoch("AcademyAdmin");
  if (!bearer || !inAdminWorkspace()) throw new Error("Sign in to the academy workspace to continue.");
  const base = `/api/academies/${b.academyId}/penta/onboarding/conversations/${b.conversationId}`;
  let closed = false, used = b.pendingRequestId !== undefined, busy = false;
  let requestId: string | null = b.pendingRequestId ?? null;
  let flight: AbortController | null = null;
  const validSession = () => !closed && inAdminWorkspace() && portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === loginEpoch;
  function invalidate() { closed = true; flight?.abort(); }
  function localSignout(event: Event) { if ((event as CustomEvent).detail === "AcademyAdmin") invalidate(); }
  function signout(event: StorageEvent) { if (isPortalSignOutEvent(event, "AcademyAdmin") || !validSession()) invalidate(); }
  if (typeof window !== "undefined") {
    window.addEventListener(portalSignOutEvent, localSignout);
    window.addEventListener(portalSessionChangedEvent, localSignout);
    window.addEventListener("storage", signout);
  }
  async function run(method: "POST" | "GET", path: string, signal?: AbortSignal, payload?: string) {
    if (!validSession() || busy || signal?.aborted || !requestId) throw failed();
    busy = true;
    const controller = new AbortController(); flight = controller;
    const abort = () => controller.abort();
    signal?.addEventListener("abort", abort, { once: true });
    try {
      const response = await request(`${apiUrl}${base}${path}`, {
        method, signal: controller.signal, cache: "no-store", credentials: "omit",
        redirect: "error", referrerPolicy: "no-referrer",
        headers: { Authorization: `Bearer ${bearer}`, Accept: "application/json",
          ...(method === "POST" ? { "Content-Type": "application/json" } : {}) },
        ...(payload === undefined ? {} : { body: payload }),
      });
      if (!validSession() || controller.signal.aborted ||
          !(method === "POST" ? [200, 201] : [200, 202]).includes(response.status)) {
        await response.body?.cancel().catch(() => {}); throw failed();
      }
      const value = await readPentaJson(response, controller.signal);
      if (!validSession() || controller.signal.aborted) throw failed();
      return verifyOnboardingTurnSummary(value, response.status, b, requestId);
    } catch { throw failed(); }
    finally { signal?.removeEventListener("abort", abort); if (flight === controller) flight = null; busy = false; }
  }
  return {
    async send(text: string, signal?: AbortSignal): Promise<OnboardingTurnSummary> {
      if (used || busy || !validSession() || signal?.aborted || typeof text !== "string" ||
          !text.trim() || text.length > 2000 || /[\u0000-\u0008\u000b-\u001f\u007f-\u009f]/.test(text)) throw failed();
      requestId = crypto.randomUUID();
      used = true; // Never clear on timeout, malformed response,401,409 or outage.
      return run("POST", browserContracts ? "/browser-turns" : "/turns", signal, JSON.stringify({ requestId, expectedVersion: b.expectedVersion,
        draftRevision: b.draftRevision, payloadHash: b.payloadHash, text }));
    },
    reconcile(signal?: AbortSignal): Promise<OnboardingTurnSummary> {
      if (!used || !requestId) return Promise.reject(failed());
      return run("GET", `/claims/${requestId}/result`, signal);
    },
    get requestId() { return requestId; },
    dispose() {
      invalidate();
      if (typeof window !== "undefined") {
        window.removeEventListener(portalSignOutEvent, localSignout);
        window.removeEventListener(portalSessionChangedEvent, localSignout);
        window.removeEventListener("storage", signout);
      }
    },
  };
}
