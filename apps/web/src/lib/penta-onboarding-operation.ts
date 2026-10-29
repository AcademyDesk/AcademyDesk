// Explicit host approval/creation only. No interpretation, tool selection, prompt
// dispatch, token refresh, automatic retry or production feature activation.
import { apiUrl, portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalWorkspaceForPath, portalSignOutEvent, isPortalSignOutEvent } from "./api";
import { validPentaAccount } from "./penta-chat-contract";
import { readPentaJson } from "./penta-onboarding-channel";
import { verifiedOnboardingReviewBinding } from "./penta-onboarding-review";
import { validateProjection } from "../vendor/penta-operational/frontend/operational-ui/src/validate";
import type { Intent, Projection, Operation, Receipt, Host } from "../vendor/penta-operational/frontend/operational-ui/src/types";

type Request = (url: string, init: RequestInit) => Promise<Response>;
export type OnboardingOperationState = { phase: "review" | "approved" | "succeeded" | "unknown";
  projection: Projection; studentId: string | null };
export class OnboardingOperationUnavailable extends Error {
  constructor(public readonly access = false) { super(access ? "Current academy access could not be verified." :
    "The outcome is unconfirmed. Check the saved result; do not repeat creation."); }
}
const obj = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
const canonical = (v: unknown): string => JSON.stringify(v === null || typeof v !== "object" ? v : Array.isArray(v) ?
  v.map(x => JSON.parse(canonical(x))) : Object.fromEntries(Object.keys(v).sort().map(k => [k, JSON.parse(canonical((v as Record<string, unknown>)[k]))])));
function fail(access = false): never { throw new OnboardingOperationUnavailable(access); }

export function createOnboardingOperationChannel(initial: Projection, request: Request = fetch) {
  const b = verifiedOnboardingReviewBinding(initial);
  const p = validateProjection(initial), original = p.host;
  if (!original.draft || !["awaiting_approval", "approved", "succeeded"].includes(original.draft.status) || !original.draft.operation_ref) fail();
  // A re-entered completed draft can only inspect its existing operation/status.
  // Terminal latches are set before any request; neither approval nor execution
  // can be resurrected by a stale/malformed response or a recovered connection.
  const terminal = original.draft.status === "succeeded";
  const bearer = portalAccessToken("AcademyAdmin"), location = () => typeof window === "undefined" ? "" : window.location.pathname + (window.location.search ?? "");
  const loginEpoch = portalSessionEpoch("AcademyAdmin");
  const route = location(), controller = new AbortController();
  let closed = false, busy = false, started = false, confirmed = terminal, executed = terminal, uncertain = false;
  let flight: AbortController | null = null;
  let operation: Operation | null = null, revision = p.projection_revision;
  const currentSession = () => !closed && !controller.signal.aborted && !!bearer && portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === loginEpoch &&
    typeof window !== "undefined" && portalWorkspaceForPath(window.location.pathname) === "AcademyAdmin" && location() === route;
  if (!currentSession()) fail(true);
  const base = `/api/academies/${b.academyId}/penta/onboarding/conversations/${b.conversationId}`;
  const draft = `${base}/drafts/${b.draftId}`, operationRef = `onboarding:${b.draftId.replaceAll("-", "")}:${b.draftRevision}`;
  if (original.draft.operation_ref !== operationRef) fail();
  const binding = (approval = false) => ({ operation_ref: operationRef, revision: b.draftRevision, payload_hash: b.payloadHash,
    ...(approval ? { approval_ref: operation?.approval_ref } : {}) });
  function close() { closed = true; controller.abort(); }
  function local(event: Event) { if ((event as CustomEvent).detail === "AcademyAdmin") close(); }
  function storage(event: StorageEvent) { if (isPortalSignOutEvent(event, "AcademyAdmin") || !currentSession()) close(); }
  window.addEventListener(portalSignOutEvent, local); window.addEventListener(portalSessionChangedEvent, local); window.addEventListener("storage", storage);
  async function call(path: string, body?: unknown): Promise<unknown> {
    if (!currentSession()) fail(true);
    if (flight?.signal.aborted) fail();
    try {
      const response = await request(apiUrl + path, { method: body === undefined ? "GET" : "POST", body: body === undefined ? undefined : JSON.stringify(body),
        credentials: "omit", redirect: "error", cache: "no-store", referrerPolicy: "no-referrer", signal: flight?.signal ?? controller.signal,
        headers: { Authorization: `Bearer ${bearer}`, Accept: "application/json", ...(body === undefined ? {} : { "Content-Type": "application/json" }) } });
      if (!currentSession() || flight?.signal.aborted || !(path.endsWith("/core/execute") ? [200, 201] : [200]).includes(response.status)) {
        await response.body?.cancel().catch(() => {}); fail(!currentSession() || response.status === 401 || response.status === 403);
      }
      const value = await readPentaJson(response, flight?.signal ?? controller.signal);
      if (!currentSession()) fail(true);
      if (flight?.signal.aborted) fail();
      return value;
    } catch (error) { if (error instanceof OnboardingOperationUnavailable) throw error; fail(!currentSession()); }
  }
  function checkedOperation(value: unknown): Operation {
    if (!obj(value) || value.ref !== operationRef || value.draft_ref !== b.draftId || value.draft_revision !== b.draftRevision ||
        value.payload_hash !== b.payloadHash || !id(value.request_ref) || value.tool_key !== "AcademyDesk.StudentOnboarding.Create" || value.tool_revision !== 1 ||
        canonical(value.scope) !== canonical(original.scope) || canonical(value.canonical_fields) !== canonical(original.draft!.fields) ||
        !["awaiting_approval", "approved", "succeeded"].includes(String(value.status)) || terminal && value.status !== "succeeded" ||
        value.approval_ref !== null && !id(value.approval_ref)) fail();
    if (operation && (value.request_ref !== operation.request_ref || value.approval_ref !== operation.approval_ref ||
        value.approval_expires_at !== operation.approval_expires_at)) fail();
    if (value.approval_ref !== null && (typeof value.approval_expires_at !== "string" ||
        !/(Z|[+-]\d{2}:\d{2})$/.test(value.approval_expires_at) || !Number.isFinite(Date.parse(value.approval_expires_at)))) fail();
    if (["approved", "succeeded"].includes(String(value.status)) && value.approval_ref === null) fail();
    if (value.status !== "succeeded" && value.approval_ref !== null && Date.parse(value.approval_expires_at as string) <= Date.now()) fail();
    return value as Operation; // Full schema validation happens in fresh Projection below.
  }
  async function refresh(): Promise<OnboardingOperationState> {
    const op = checkedOperation(await call(draft + "/core/operation", binding()));
    const receipt = await call(draft + "/core/status", binding());
    const account = await call("/api/auth/session");
    if (!obj(account)) fail(true);
    const displayName = account.displayName, workspace = account.workspace;
    if (!validPentaAccount(account) || workspace !== "AcademyAdmin" || account.academyId !== b.academyId ||
        typeof displayName !== "string" || !displayName.trim() || displayName.length > 200) fail(true);
    const cursor = await call(base + "/current");
    if (!obj(cursor) || cursor.conversationId !== b.conversationId || cursor.currentDraftId !== b.draftId ||
        cursor.version !== b.expectedVersion || cursor.draftRevision !== b.draftRevision || cursor.pendingRequestId !== null ||
        cursor.status !== "selected" || cursor.canExecute !== false || !obj(cursor.snapshot)) fail();
    const h = cursor.snapshot as Host;
    if (!h.draft || h.draft.status !== op.status || canonical(h.draft.fields) !== canonical(op.canonical_fields) ||
        ["version", "scope", "catalog_revision", "tools", "evidence", "availability"].some(k => canonical(h[k as keyof Host]) !== canonical(original[k as keyof Host]))) fail();
    if (!obj(receipt) || receipt.operation_ref !== operationRef || receipt.payload_hash !== b.payloadHash ||
        receipt.draft_revision !== b.draftRevision || receipt.status !== op.status || receipt.error_code !== "none" ||
        !Array.isArray(receipt.source_refs) || new Set(receipt.source_refs).size !== receipt.source_refs.length ||
        (receipt.status === "succeeded" ? !id(receipt.receipt_ref) || !id(receipt.audit_ref) || receipt.source_refs.length < 1 || receipt.source_refs.length > 2 || !receipt.source_refs.every(id) :
          receipt.receipt_ref !== null || receipt.audit_ref !== null || receipt.source_refs.length !== 0)) fail();
    const allowConfirm = !uncertain && !confirmed && !executed && op.status === "awaiting_approval" && !!op.approval_ref;
    const projection = validateProjection({ ...p, projection_revision: ++revision, actor_label: displayName,
      host: h, operation: op, receipt, controls: { ...p.controls, confirm: allowConfirm },
      messages: [...p.messages.filter(message => message.ref !== "operation-state"), { ref: "operation-state", kind: "status", source_refs: [], text: receipt.status === "succeeded" ?
        "Student creation is verified against the saved host receipt and audit. No portal accounts were created." :
        op.status === "approved" ? "This exact operation is approved. A student has not yet been confirmed as created. Select Create approved student to continue." :
        "Review the exact canonical operation. Confirmation approves these details; creation is a separate explicit step." }] });
    if (!currentSession()) fail(true);
    operation = projection.operation;
    return { phase: op.status === "succeeded" ? "succeeded" : uncertain ? "unknown" : op.status === "approved" ? "approved" : "review",
      projection, studentId: op.status === "succeeded" ? (receipt as Receipt).source_refs[0] : null };
  }
  async function run(work: () => Promise<OnboardingOperationState>, signal?: AbortSignal) {
    if (busy || !currentSession() || signal?.aborted) fail(!currentSession());
    busy = true;
    const active = new AbortController(); flight = active;
    const abort = () => active.abort();
    controller.signal.addEventListener("abort", abort, { once: true }); signal?.addEventListener("abort", abort, { once: true });
    try { return await work(); }
    catch (error) { uncertain = true; throw error; }
    finally { busy = false; flight = null; controller.signal.removeEventListener("abort", abort); signal?.removeEventListener("abort", abort); }
  }
  return {
    review: () => run(async () => {
      if (started) fail(); started = true;
      // Explicit review gesture, not mount/prefetch. Issue a proof only for a
      // host-prepared draft. An existing approved draft is inspected, not approved again.
      if (original.draft!.status === "awaiting_approval") checkedOperation(await call(draft + "/core/challenge", binding()));
      return refresh();
    }),
    confirm: (intent: Extract<Intent, { kind: "confirm" }>, signal?: AbortSignal) => run(async () => {
      if (!started || confirmed || executed || uncertain || !operation || operation.status !== "awaiting_approval" ||
          intent.operation_ref !== operation.ref || intent.approval_ref !== operation.approval_ref || intent.revision !== operation.draft_revision || intent.payload_hash !== operation.payload_hash) fail();
      confirmed = true;
      const proof = operation.approval_ref;
      const op = checkedOperation(await call(draft + "/core/confirm", binding(true)));
      if (op.status !== "approved" || op.approval_ref !== proof) fail();
      return refresh();
    }, signal),
    execute: () => run(async () => {
      if (!started || executed || uncertain || !operation || operation.status !== "approved" || !operation.approval_ref ||
          !operation.approval_expires_at || Date.parse(operation.approval_expires_at) <= Date.now()) fail();
      executed = true; // Never clear after abort/outage/malformed response.
      const result = await call(draft + "/core/execute", binding(true));
      if (!obj(result) || result.status !== "succeeded" || result.operation_ref !== operationRef || result.payload_hash !== b.payloadHash ||
          result.draft_revision !== b.draftRevision || canonical(result.scope) !== canonical(original.scope) || !id(result.receipt_ref) || !id(result.audit_ref)) fail();
      return refresh();
    }),
    recover: () => run(async () => {
      if (!started || !uncertain) fail();
      // Read commands only. An uncertain creation/confirmation never clears its
      // one-shot latch or becomes permission to execute again.
      return refresh();
    }),
    dispose() { close(); window.removeEventListener(portalSignOutEvent, local); window.removeEventListener(portalSessionChangedEvent, local); window.removeEventListener("storage", storage); },
  };
}
