// Mounted conversation coordinator. The only admitted runtime in this candidate
// is explicitly synthetic disposable QA; this does NOT qualify/connect Mini.
import { apiUrl, portalAccessToken, portalSessionEpoch } from "./api";
import { validPentaAccount } from "./penta-chat-contract";
import { createOnboardingTurnChannel, readPentaJson, type OnboardingTurnSummary } from "./penta-onboarding-channel";
import { loadOnboardingReview, presentOnboardingDialogue, verifiedOnboardingReviewBinding } from "./penta-onboarding-review";
import type { Projection, Intent } from "../vendor/penta-operational/frontend/operational-ui/src/types";
type Request = (url: string, init: RequestInit) => Promise<Response>;
const obj = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
export class OnboardingDialogueUnavailable extends Error {
  constructor(public readonly pending = false) { super("The conversation outcome is unconfirmed. Check the same request; do not send it again."); }
}
function fail(pending = false): never { throw new OnboardingDialogueUnavailable(pending); }

export function createOnboardingDialogue(initial: Projection, signal: AbortSignal, request: Request = fetch) {
  let current = initial, binding = verifiedOnboardingReviewBinding(initial);
  const token = portalAccessToken("AcademyAdmin"), location = () => window.location.pathname + (window.location.search ?? "");
  const loginEpoch = portalSessionEpoch("AcademyAdmin");
  const route = location();
  let closed = false, busy = false, connected = false, ready = false;
  let turn: ReturnType<typeof createOnboardingTurnChannel> | null = null;
  let prompt: string | null = null, sequence = 0;
  let flight: AbortController | null = null;
  const messages: Projection["messages"] = [];
  const valid = () => !closed && !signal.aborted && !flight?.signal.aborted && !!token && portalAccessToken("AcademyAdmin") === token && portalSessionEpoch("AcademyAdmin") === loginEpoch && location() === route;
  if (!valid() || current.host.draft?.status !== "collecting") fail();
  function dispose() { closed = true; ready = false; flight?.abort(); turn?.dispose(); signal.removeEventListener("abort", dispose); }
  signal.addEventListener("abort", dispose, { once: true });
  const base = `/api/academies/${binding.academyId}/penta/onboarding/conversations/${binding.conversationId}`;
  async function get(path: string): Promise<unknown> {
    if (!valid()) fail();
    const active = flight?.signal ?? signal;
    const response = await request(apiUrl + path, { method: "GET", signal: active, credentials: "omit", cache: "no-store", redirect: "error",
      referrerPolicy: "no-referrer", headers: { Authorization: `Bearer ${token}`, Accept: "application/json" } });
    if (!valid() || response.status !== 200) { await response.body?.cancel().catch(() => {}); fail(); }
    const value = await readPentaJson(response, active);
    if (!valid()) fail();
    return value;
  }
  async function readiness(b = binding): Promise<boolean> {
    const account = await get("/api/auth/session");
    if (!obj(account)) fail(); const workspace = account.workspace;
    if (!validPentaAccount(account) || workspace !== "AcademyAdmin" || account.academyId !== binding.academyId) fail();
    const v = await get(base + "/send-readiness");
    const keys = "actorId,academyId,authorityVersion,canExecute,canSend,contextVersion,conversationId,conversationVersion,draftId,draftRevision,payloadHash,qualification,runtime,version".split(",").sort().join(",");
    if (!obj(v) || Object.keys(v).sort().join(",") !== keys || v.version !== "onboarding-send/1" ||
        v.academyId !== binding.academyId || v.actorId !== binding.actorId || v.conversationId !== binding.conversationId || v.draftId !== binding.draftId ||
        v.conversationVersion !== b.expectedVersion || v.draftRevision !== b.draftRevision || v.payloadHash !== b.payloadHash ||
        v.contextVersion !== b.contextVersion || v.authorityVersion !== b.authorityVersion || v.canExecute !== false || v.qualification !== "not_qualified" ||
        !(v.canSend === true && v.runtime === "synthetic_test" || v.canSend === false && v.runtime === "unavailable")) fail();
    return v.canSend === true;
  }
  async function run<T>(work: () => Promise<T>, callerSignal?: AbortSignal): Promise<T> {
    if (busy || !valid() || callerSignal?.aborted) fail(!!turn?.requestId);
    busy = true;
    const active = new AbortController(); flight = active; const abort = () => active.abort();
    signal.addEventListener("abort", abort, { once: true }); callerSignal?.addEventListener("abort", abort, { once: true });
    try { return await work(); }
    catch { ready = false; return fail(!!turn?.requestId); }
    finally { busy = false; flight = null; signal.removeEventListener("abort", abort); callerSignal?.removeEventListener("abort", abort); }
  }
  async function completed(summary: OnboardingTurnSummary): Promise<Projection | null> {
    if (summary.kind === "pending") return null;
    const next = await loadOnboardingReview(binding.conversationId, binding.draftId, crypto.randomUUID(), current.projection_revision + 1, flight?.signal ?? signal, request);
    const nextBinding = verifiedOnboardingReviewBinding(next);
    if (!valid() || nextBinding.expectedVersion !== summary.version || nextBinding.draftRevision !== summary.draftRevision ||
        nextBinding.actorId !== binding.actorId || nextBinding.academyId !== binding.academyId ||
        nextBinding.contextVersion !== binding.contextVersion || nextBinding.authorityVersion !== binding.authorityVersion) fail(true);
    const fields = next.host.tools[0].fields;
    const labels = summary.questionKeys.map(key => { const field = fields.find(f => f.key === key); if (!field) fail(true); return field.label; });
    const text = summary.kind === "clarify" ? labels.length ? `Please provide: ${labels.join(", ")}. No student has been created.` :
      "Please clarify the student details. No student has been created." : summary.kind === "host_prepare" ?
      "The host prepared a draft. Review its exact details; approval and student creation are separate actions." : summary.kind === "unsupported" ?
      "This request is outside this onboarding conversation. Use the Manual workspace for other operations." :
      "This turn did not authorize or create a student. Check the host draft or use Manual.";
    if (!prompt || messages.length >= 18) fail(true);
    const mayContinue = next.host.draft?.status === "collecting" && !["unavailable", "invalid_response"].includes(summary.kind) && messages.length + 2 < 18;
    ready = mayContinue && await readiness(nextBinding);
    if (!valid()) fail(true);
    messages.push({ ref: `turn-user-${++sequence}`, kind: "user", text: prompt, source_refs: [] },
      { ref: `turn-status-${sequence}`, kind: "status", text, source_refs: [] });
    const projection = presentOnboardingDialogue(next, [...messages], ready);
    turn?.dispose(); turn = null; prompt = null; current = projection; binding = nextBinding;
    return projection;
  }
  return {
    connect: () => run(async () => {
      if (connected) fail(); connected = true;
      ready = await readiness();
      return { available: ready, projection: presentOnboardingDialogue(current, [{ ref: "testing-runtime", kind: "status", source_refs: [],
        text: ready ? "Disposable test interpreter connected. This is not qualified AI; no student is created by sending a message." :
          "Conversational sending is unavailable. No qualified runtime is connected; Manual remains available." }], ready) };
    }),
    send: (intent: Extract<Intent, { kind: "send" }>, callerSignal: AbortSignal) => run(async () => {
      if (!connected || !ready || turn || callerSignal.aborted || intent.capability !== "Executor" || intent.base_revision !== binding.draftRevision ||
          typeof intent.text !== "string" || !intent.text.trim() || intent.text.length > 2000 || /[\u0000-\u0008\u000b-\u001f\u007f-\u009f]/.test(intent.text)) fail();
      ready = false;
      if (!await readiness() || callerSignal.aborted || !valid()) fail();
      turn = createOnboardingTurnChannel(binding, request, true); prompt = intent.text;
      const summary = await turn.send(intent.text, flight!.signal);
      if (callerSignal.aborted || !valid()) fail(true);
      return completed(summary);
    }, callerSignal),
    recover: () => run(async () => {
      if (!turn?.requestId || !prompt) fail();
      // Exactly the captured request. Never generate a new ID or POST a prompt.
      return completed(await turn.reconcile(signal));
    }),
    get pending() { return !!turn?.requestId; },
    dispose,
  };
}
