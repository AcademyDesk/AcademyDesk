// Authenticated passive review only. Never derives a grant from a model or UI.
import { apiUrl, portalAccessToken, portalSessionEpoch, portalWorkspaceForPath } from "./api";
import { validPentaAccount } from "./penta-chat-contract";
import { readPentaJson, verifyOnboardingTurnSummary, type OnboardingBinding } from "./penta-onboarding-channel";
import { validateProjection } from "../vendor/penta-operational/frontend/operational-ui/src/validate";
import type { Projection, Host, Value, FieldSpec } from "../vendor/penta-operational/frontend/operational-ui/src/types";

type Request = (input: string, init: RequestInit) => Promise<Response>;
export type OnboardingReviewRecovery = {
  check: () => Promise<{ kind: "pending" } | { kind: "review"; projection: Projection }>;
};
export class OnboardingReviewUnavailable extends Error {
  constructor(public readonly reason: "binding" | "access" | "pending" | "unavailable" | "changed",
    public readonly recovery?: OnboardingReviewRecovery) {
    super("The current onboarding review could not be verified. No action was submitted.");
  }
}
function fail(reason: OnboardingReviewUnavailable["reason"] = "changed"): never { throw new OnboardingReviewUnavailable(reason); }
const object = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
const integer = (v: unknown, min: number, max: number): v is number => Number.isSafeInteger(v) && Number(v) >= min && Number(v) <= max;
const canonical = (v: unknown): string => JSON.stringify(v === null || typeof v !== "object" ? v : Array.isArray(v) ?
  v.map(x => JSON.parse(canonical(x))) : Object.fromEntries(Object.keys(v).sort().map(k => [k, JSON.parse(canonical((v as Record<string, unknown>)[k]))])));
const tool = "AcademyDesk.StudentOnboarding.Create";
const capabilities = ["Pulse", "Executor", "Navigator", "Twin", "Autopilot"] as const;
// Ephemeral provenance for a host-loaded object. Not persisted and not an
// authorization mechanism: every actual action is independently authorized by API.
const verifiedBindings = new WeakMap<Projection, { binding: OnboardingBinding; content: string; expiresAt: number; sessionCurrent: () => boolean }>();
export function onboardingReviewExpiry(projection: Projection): number {
  const verified = verifiedBindings.get(projection);
  if (!verified || verified.content !== canonical(projection)) fail();
  return verified.expiresAt;
}
export function verifiedOnboardingReviewBinding(projection: Projection): OnboardingBinding {
  const expiresAt = onboardingReviewExpiry(projection);
  if (!Number.isFinite(Date.now()) || expiresAt <= Date.now()) fail();
  const verified = verifiedBindings.get(projection)!;
  if (!verified.sessionCurrent()) fail("access");
  return { ...verified.binding };
}
type ExpiryEnvironment = {
  now: () => number; schedule: (callback: () => void, delay: number) => number;
  cancel: (id: number) => void; focus: EventTarget; visibility: EventTarget;
};
/** Presentation-only expiry. No polling, refresh, approval or permission grant.
 * Focus/visibility checks cover background-tab timer suspension. API authorization
 * remains authoritative even if the client clock is wrong or this code is bypassed.
 */
export function watchOnboardingReviewExpiry(projection: Projection, expired: () => void,
  environment: ExpiryEnvironment = { now: Date.now, schedule: (callback, delay) => window.setTimeout(callback, delay),
    cancel: id => window.clearTimeout(id), focus: window, visibility: document }): () => void {
  const deadline = onboardingReviewExpiry(projection);
  let active = true, timer: number | undefined;
  function dispose() {
    active = false;
    if (timer !== undefined) environment.cancel(timer);
    environment.focus.removeEventListener("focus", check);
    environment.visibility.removeEventListener("visibilitychange", check);
  }
  function check() {
    if (!active) return;
    if (timer !== undefined) environment.cancel(timer);
    const remaining = deadline - environment.now();
    if (!Number.isFinite(remaining) || remaining <= 0) { dispose(); expired(); return; }
    timer = environment.schedule(check, Math.min(remaining, 2147483647));
  }
  environment.focus.addEventListener("focus", check);
  environment.visibility.addEventListener("visibilitychange", check);
  check();
  return dispose;
}
// Presentation-only composition of an independently verified host projection.
// It cannot add a tool, model, operation, approval, field or execution grant.
export function presentOnboardingDialogue(base: Projection, messages: Projection["messages"], send: boolean): Projection {
  const binding = verifiedOnboardingReviewBinding(base);
  const projection = validateProjection({ ...base, messages,
    capabilities: base.capabilities.map(c => ({ ...c, available: send && c.key === "Executor",
      reason: send && c.key === "Executor" ? "Disposable testing interpreter only; not qualified AI." : c.reason })),
    controls: { ...base.controls, send } });
  verifiedBindings.set(projection, { binding, content: canonical(projection), expiresAt: onboardingReviewExpiry(base), sessionCurrent: verifiedBindings.get(base)!.sessionCurrent });
  return projection;
}

function fieldValid(spec: FieldSpec, value: Value): boolean {
  if (spec.kind !== value.kind) return false;
  if (value.kind === "boolean") return typeof value.value === "boolean";
  if (value.kind !== "text" && value.kind !== "date" && value.kind !== "reference") return false;
  if (typeof value.value !== "string" || !value.value.trim() || value.value.length > spec.max_length || /[\u0000-\u0008\u000b-\u001f\u007f-\u009f]/.test(value.value)) return false;
  if (value.kind === "date") return /^\d{4}-\d{2}-\d{2}$/.test(value.value) && Number.isFinite(Date.parse(value.value)) && new Date(value.value).toISOString().slice(0, 10) === value.value;
  if (value.kind === "reference") return spec.choices.some(c => c.id === value.value);
  return true;
}

function current(value: unknown, conversation: string, draft: string): Record<string, unknown> {
  if (!object(value) || value.conversationId !== conversation || value.canExecute !== false ||
      !integer(value.version, 1, 100) || value.currentDraftId !== draft || !integer(value.draftRevision, 1, 100)) fail();
  if (value.status === "pending" && id(value.pendingRequestId) && value.snapshot === null) fail("pending");
  if (value.status !== "selected" || value.pendingRequestId !== null || !object(value.snapshot)) fail();
  return value;
}

/** Explicit refresh, four GETs only: account -> current -> draft -> current.
 * Untrusted URL IDs are lookup keys, never actor/tenant authority. No creation,
 * selection, inference, confirmation, polling or execution occurs on mount.
 */
export async function loadOnboardingReview(conversation: string, draft: string, epoch: string,
  revision: number, signal: AbortSignal, request: Request = fetch): Promise<Projection> {
  if (!id(conversation) || !id(draft) || !id(epoch) || !integer(revision, 1, Number.MAX_SAFE_INTEGER)) fail("binding");
  const bearer = portalAccessToken("AcademyAdmin");
  const loginEpoch = portalSessionEpoch("AcademyAdmin");
  const sessionCurrent = () => !signal.aborted && portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === loginEpoch &&
    typeof window !== "undefined" && portalWorkspaceForPath(window.location.pathname) === "AcademyAdmin";
  if (!bearer || !sessionCurrent()) fail("access");
  async function read(path: string, statuses: readonly number[] = [200]): Promise<{ status: number; value: unknown }> {
    if (!sessionCurrent()) fail("access");
    try {
      const response = await request(`${apiUrl}${path}`, { method: "GET", signal, credentials: "omit", cache: "no-store",
        redirect: "error", referrerPolicy: "no-referrer", headers: { Authorization: `Bearer ${bearer}`, Accept: "application/json" } });
      if (!sessionCurrent()) { await response.body?.cancel().catch(() => {}); fail("access"); }
      if (!statuses.includes(response.status)) {
        await response.body?.cancel().catch(() => {});
        fail(response.status === 401 || response.status === 403 ? "access" : response.status === 404 || response.status === 503 ? "unavailable" : "changed");
      }
      const value = await readPentaJson(response, signal);
      if (!sessionCurrent()) fail("access");
      return { status: response.status, value };
    } catch (error) { if (error instanceof OnboardingReviewUnavailable) throw error; fail("unavailable"); }
  }
  const get = async (path: string) => (await read(path)).value;
  const account = await get("/api/auth/session");
  if (!object(account)) fail("access");
  const displayName = account.displayName, workspace = account.workspace;
  if (!validPentaAccount(account) || workspace !== "AcademyAdmin" ||
      typeof displayName !== "string" || !displayName.trim() || displayName.length > 200) fail("access");
  // The academy comes exclusively from the current server session response.
  const base = `/api/academies/${account.academyId}/penta/onboarding/conversations/${conversation}`;
  const initial = await get(`${base}/current`);
  let before: Record<string, unknown>;
  try { before = current(initial, conversation, draft); }
  catch (error) {
    if (!(error instanceof OnboardingReviewUnavailable) || error.reason !== "pending" || !object(initial)) throw error;
    // Capture only an authenticated current cursor, not URL/model/localStorage
    // request IDs. The closure retains this session/abort fence and never POSTs.
    const pendingId = initial.pendingRequestId as string, pendingVersion = initial.version as number;
    const pendingRevision = initial.draftRevision as number;
    let busy = false;
    const recovery: OnboardingReviewRecovery = { async check() {
      if (busy || !sessionCurrent()) fail("access");
      busy = true;
      try {
        const cursor = await get(`${base}/current`);
        if (!object(cursor) || cursor.conversationId !== conversation || cursor.currentDraftId !== draft ||
            cursor.version !== pendingVersion || cursor.canExecute !== false ||
            !integer(cursor.draftRevision, pendingRevision, pendingRevision + 1)) fail();
        const stillPending = cursor.status === "pending" && cursor.pendingRequestId === pendingId && cursor.snapshot === null;
        const completed = cursor.status === "selected" && cursor.pendingRequestId === null && object(cursor.snapshot);
        if (!stillPending && !completed || stillPending && cursor.draftRevision !== pendingRevision) fail();
        const result = await read(`${base}/claims/${pendingId}/result`, [200, 202]);
        if (result.status === 202) {
          if (!stillPending) fail();
          const v = result.value;
          if (!object(v) || v.conversationId !== conversation || v.requestId !== pendingId || v.version !== pendingVersion ||
              v.status !== "pending" || v.canExecute !== false ||
              Object.keys(v).sort().join(",") !== "canExecute,conversationId,requestId,status,version") fail();
          return { kind: "pending" };
        }
        // A result receipt alone cannot paint private fields or report a domain
        // success. Fetch and verify a fresh current/draft/digest/current projection.
        const projection = await loadOnboardingReview(conversation, draft, epoch, revision + 1, signal, request);
        if (!sessionCurrent() || !object(result.value) || projection.host.draft?.revision === undefined) fail("access");
        const host = projection.host, v = result.value;
        verifyOnboardingTurnSummary(v, 200, { academyId: account.academyId as string, actorId: host.scope.actor_id,
          conversationId: conversation, draftId: draft, expectedVersion: pendingVersion - 1, draftRevision: pendingRevision,
          authorityVersion: host.scope.authority_version, contextVersion: host.scope.context_version }, pendingId);
        if (!object(v.snapshot) || ["version", "scope", "catalog_revision", "tools", "draft", "evidence", "availability"]
          .some(key => canonical(v.snapshot && (v.snapshot as Record<string, unknown>)[key]) !== canonical(host[key as keyof Host]))) fail();
        // The final current cursor must still identify this same completed turn.
        const final = current(await get(`${base}/current`), conversation, draft);
        if (final.version !== pendingVersion || final.draftRevision !== projection.host.draft.revision ||
            ["version", "scope", "catalog_revision", "tools", "draft", "evidence", "availability"]
              .some(key => canonical((final.snapshot as Record<string, unknown>)[key]) !== canonical(host[key as keyof Host]))) fail();
        if (!sessionCurrent()) fail("access");
        return { kind: "review", projection };
      } finally { busy = false; }
    } };
    throw new OnboardingReviewUnavailable("pending", recovery);
  }
  const view = await get(`${base}/drafts/${draft}`);
  if (!object(view) || view.id !== draft || view.sessionId !== conversation || view.canExecute !== false ||
      !integer(view.contextVersion, 0, Number.MAX_SAFE_INTEGER - 1) || view.revision !== before.draftRevision ||
      typeof view.expiresAtUtc !== "string" || !/(Z|[+-]\d{2}:\d{2})$/.test(view.expiresAtUtc) ||
      !Number.isFinite(Date.parse(view.expiresAtUtc)) || Date.parse(view.expiresAtUtc) <= Date.now() ||
      typeof view.canonicalPayload !== "string" || view.canonicalPayload.length > 6000 ||
      typeof view.payloadHash !== "string" || !/^[0-9a-f]{64}$/.test(view.payloadHash)) fail();
  const digest = Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(view.canonicalPayload))))
    .map(x => x.toString(16).padStart(2, "0")).join("");
  if (digest !== view.payloadHash) fail();
  let payload: unknown;
  try { payload = JSON.parse(view.canonicalPayload); } catch { fail(); }
  if (!object(payload) || Object.keys(payload).sort().join(",") !== "fields,tool_key,tool_revision" ||
      payload.tool_key !== tool || payload.tool_revision !== 1 || !object(payload.fields) ||
      Object.hasOwn(payload.fields, "MedicalOrAccessibilityNotes")) fail();
  const after = current(await get(`${base}/current`), conversation, draft);
  if (after.version !== before.version || after.draftRevision !== before.draftRevision) fail();
  const previous = before.snapshot as Record<string, unknown>, latest = after.snapshot as Record<string, unknown>;
  if (["version", "scope", "catalog_revision", "tools", "draft", "evidence", "availability"]
    .some(key => canonical(previous[key]) !== canonical(latest[key]))) fail();
  const host = after.snapshot as Host;
  // The snapshot actor is supplied by the authenticated current/Owned API, not URL
  // or JWT decoding. The public account summary intentionally has no actor ID.
  const projection = validateProjection({ version: "operational-ui-0.3.0", context_key: `review:${epoch}`,
    projection_revision: revision, workspace_label: "Academy Desk · Onboarding review", actor_label: displayName,
    role_label: "Academy Owner / Admin", surface: "academy", host,
    capabilities: capabilities.map(key => ({ key, available: false,
      reason: "Read-only review. Conversational actions require a qualified runtime and completed host integration." })),
    selected_capability: "Executor", models: [], selected_model_ref: null, history: [],
    messages: [{ ref: "review-status", kind: "status", text: view.status === "Succeeded" ?
      "This host draft is marked completed. Verify the saved operation, receipt and audit before displaying its result. No creation will be repeated." :
      "Review the current host draft in the work canvas. This view cannot create, approve or change a student.", source_refs: [] }],
    observation: null, validation: null, operation: null, receipt: null,
    controls: { send: false, prepare: false, confirm: false, cancel: false, pause: false, resume: false,
      reconcile: false, history: false, feedback: false, secure_media: false } });
  const h = projection.host, d = h.draft;
  const statuses: Record<string, string> = { AwaitingReview: "collecting", Confirmed: "collecting", Prepared: "awaiting_approval",
    Authorized: "approved", Paused: "paused", Cancelled: "cancelled", Succeeded: "succeeded" };
  if (h.availability !== "ready" || h.evidence.length !== 0 || !id(h.scope.actor_id) || h.scope.tenant_id !== account.academyId ||
      h.scope.conversation_id !== conversation || h.scope.connection_id !== `academydesk-onboarding:${draft.replaceAll("-", "")}` ||
      h.scope.authority_version !== 1 || h.scope.context_version !== view.contextVersion + 1 || h.catalog_revision !== 1 ||
      !d || d.ref !== draft || d.revision !== view.revision || d.tool_key !== tool ||
      typeof view.status !== "string" || !Object.hasOwn(statuses, view.status) || d.status !== statuses[view.status] ||
      h.tools.length !== 1 || h.tools[0].key !== tool || h.tools[0].revision !== 1 || h.tools[0].family !== "AI-04" ||
      h.tools[0].effect !== "write" || !h.tools[0].approval_required || !h.tools[0].preparation_required || h.tools[0].max_items !== 1 ||
      h.tools[0].capability.join(",") !== "Executor" || canonical(d.fields) !== canonical(payload.fields)) fail();
  const specs = h.tools[0].fields;
  if (Object.entries(d.fields).some(([key, value]) => {
    const spec = specs.find(s => s.key === key); return !spec || !fieldValid(spec, value);
  })) fail();
  // Check ownership/version/content again after hashing and schema validation.
  if (!sessionCurrent()) fail("access");
  // Hashing and the final HTTP fence may outlast the original checked lifetime.
  if (!Number.isFinite(Date.now()) || Date.parse(view.expiresAtUtc) <= Date.now()) fail();
  verifiedBindings.set(projection, { binding: { academyId: account.academyId as string, actorId: h.scope.actor_id,
    conversationId: conversation, draftId: draft, contextVersion: h.scope.context_version, authorityVersion: h.scope.authority_version,
    expectedVersion: after.version as number, draftRevision: view.revision as number, payloadHash: view.payloadHash },
    content: canonical(projection), expiresAt: Date.parse(view.expiresAtUtc), sessionCurrent });
  return projection;
}
