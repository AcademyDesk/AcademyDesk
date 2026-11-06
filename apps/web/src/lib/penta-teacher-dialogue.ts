// Host-bound teacher conversation transport. No approval/execution dispatch.
import { apiUrl, portalAccessToken, portalSessionEpoch, portalWorkspaceForPath } from "./api";
import { validPentaAccount } from "./penta-chat-contract";
import { readPentaJson } from "./penta-onboarding-channel";
import { loadTeacherCreationChannel, TeacherCreationUnavailable, type TeacherCreationView } from "./penta-teacher-operation";

type Request = (url: string, init: RequestInit) => Promise<Response>;
export type TeacherDialogueState = { phase: "ready" | "pending"; available: boolean; draft: TeacherCreationView | null; message: string };
const obj = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
const keys = (v: Record<string, unknown>, expected: string) => Object.keys(v).sort().join(",") === expected.split(",").sort().join(",");
export class TeacherDialogueUnavailable extends Error {
  constructor(public readonly access = false) { super(access ? "Current academy access could not be verified." : "Check the same teacher request. Do not send it again. Manual remains available."); }
}
function fail(access = false): never { throw new TeacherDialogueUnavailable(access); }

export async function loadTeacherDialogue(session: string, draft: string, signal: AbortSignal, request: Request = fetch) {
  if (!id(session) || !id(draft) || session === draft || typeof window === "undefined") fail();
  const bearer = portalAccessToken("AcademyAdmin"), epoch = portalSessionEpoch("AcademyAdmin");
  const location = () => window.location.pathname + (window.location.search ?? "");
  let route = location(), closed = false, busy = false;
  const params = new URLSearchParams(window.location.search), pending = params.get("teacherRequestId");
  if (params.get("conversationId") !== session || params.get("draftId") !== draft ||
      params.size !== (pending === null ? 2 : 3) || (pending !== null && !id(pending))) fail();
  const valid = () => !closed && !signal.aborted && !!bearer && portalAccessToken("AcademyAdmin") === bearer &&
    portalSessionEpoch("AcademyAdmin") === epoch && location() === route && portalWorkspaceForPath(window.location.pathname) === "AcademyAdmin";
  const check = () => { if (!valid()) { dispose(); fail(true); } };
  let native: Awaited<ReturnType<typeof loadTeacherCreationChannel>> | null = null;
  let requestId: string | null = pending, state: TeacherDialogueState = { phase: "pending", available: false, draft: null, message: "Check the saved teacher request." };
  function dispose() { closed = true; native?.dispose(); native = null; signal.removeEventListener("abort", dispose); state = { phase: "pending", available: false, draft: null, message: "Current access changed." }; }
  signal.addEventListener("abort", dispose, { once: true });
  async function call(path: string, body?: unknown, statuses = [200]) {
    check(); const response = await request(apiUrl + path, { method: body === undefined ? "GET" : "POST", signal, credentials: "omit", cache: "no-store", redirect: "error", referrerPolicy: "no-referrer",
      headers: { Authorization: `Bearer ${bearer}`, Accept: "application/json", ...(body === undefined ? {} : { "Content-Type": "application/json" }) }, body: body === undefined ? undefined : JSON.stringify(body) });
    check(); if (response.status === 401 || response.status === 403) { dispose(); fail(true); }
    if (!statuses.includes(response.status)) { await response.body?.cancel().catch(() => {}); fail(); }
    const value = await readPentaJson(response, signal); check(); return value;
  }
  async function account(expected?: string): Promise<string> {
    const v = await call("/api/auth/session");
    const workspace = obj(v) ? v.workspace : null;
    if (!obj(v) || !validPentaAccount(v) || workspace !== "AcademyAdmin" || !id(v.academyId) || (expected && v.academyId !== expected)) { dispose(); fail(true); }
    return v.academyId;
  }
  const academy = await account(), base = `/api/academies/${academy}/penta/teachers`, resource = `${base}/conversations/${session}/drafts/${draft}`;
  async function readiness() {
    const v = await call(base + "/readiness");
    if (!obj(v) || !keys(v, "version,academyId,available,canStart,canExecute,syntheticInterpreter") || v.version !== "teacher-start/1" || v.academyId !== academy ||
        typeof v.available !== "boolean" || v.canStart !== v.available || v.canExecute !== false || typeof v.syntheticInterpreter !== "boolean" || (v.available && !v.syntheticInterpreter)) fail();
    return v.available;
  }
  function current(): TeacherDialogueState { check(); return structuredClone(state); }
  async function readDraft() {
    native?.dispose(); native = null;
    try { native = await loadTeacherCreationChannel(session, draft, signal, request); check(); return native.current; }
    catch (error) { if (error instanceof TeacherCreationUnavailable && error.access) { dispose(); fail(true); } throw error; }
  }
  async function finish(value: unknown): Promise<TeacherDialogueState> {
    if (!obj(value) || !keys(value, "version,conversationId,draftId,requestId,draftRevision,payloadHash,kind,status,canExecute,domainEffect,result") ||
        value.version !== "teacher-conversation/1" || value.conversationId !== session || value.draftId !== draft || value.requestId !== requestId ||
        !Number.isSafeInteger(value.draftRevision) || Number(value.draftRevision) < 1 || Number(value.draftRevision) > 100 || typeof value.payloadHash !== "string" || !/^[0-9a-f]{64}$/.test(value.payloadHash) ||
        !["pending", "partial", "prepared", "unavailable", "invalid_response", "needs_manual_review"].includes(String(value.kind)) ||
        value.status !== (value.kind === "pending" ? "pending" : "completed") || value.canExecute !== false || value.domainEffect !== false ||
        (["partial", "prepared"].includes(String(value.kind)) ? !obj(value.result) : value.result !== null)) fail();
    if (value.kind === "pending") { state = { phase: "pending", available: false, draft: null, message: "This request is still pending. Check it without resending." }; return current(); }
    // Ignore model/plan prose and details. Load canonical fields and hash from the
    // independently authorized native workflow, then bind it to this exact turn.
    const next = await readDraft();
    if (next.revision !== value.draftRevision || next.payloadHash !== value.payloadHash) fail();
    if (value.kind === "prepared" && next.status === "AwaitingReview") fail();
    const fields = JSON.parse(next.canonicalPayload).fields as Record<string, { value: string }>;
    const missing = ["FirstName", "LastName"].filter(key => !fields[key]?.value.trim()).map(key => key === "FirstName" ? "first name" : "last name");
    const available = ["partial", "prepared"].includes(String(value.kind)) && ["AwaitingReview", "Reviewed"].includes(next.status) && Date.parse(next.expiresAtUtc) > Date.now() && await readiness();
    await account(academy); check();
    state = { phase: "ready", available, draft: next, message: next.status === "Cancelled" ? "This teacher draft was cancelled. No teacher was created by this draft." : next.status === "Succeeded" ? "This teacher was created. Open the verified review for its receipt." :
      next.status === "Authorized" ? "This draft is already approved. Creation remains a separate step in the protected review." :
        value.kind === "partial" ? missing.length ? `Please provide the teacher’s ${missing.join(" and ")}. No teacher has been created.` : "Details saved. Provide any corrections, then request preparation for review." :
          value.kind === "prepared" ? "Your draft is ready. Review the exact details; approval and creation are separate steps." :
            value.kind === "needs_manual_review" ? "The host could not prepare these details. Review possible duplicates or validation issues in Manual." :
              "The interpreter response could not be used. No creation was authorized. Use Manual or check this saved draft." };
    return current();
  }
  async function run(work: () => Promise<TeacherDialogueState>) {
    check(); if (busy) fail(); busy = true;
    try { return await work(); }
    catch (error) { native?.dispose(); native = null; state = { phase: "pending", available: false, draft: null, message: "The teacher request is unconfirmed. Check it without resending." }; throw error; }
    finally { busy = false; }
  }
  try { if (requestId === null) {
    const value = await readDraft(); const available = ["AwaitingReview", "Reviewed"].includes(value.status) && Date.parse(value.expiresAtUtc) > Date.now() && await readiness(); await account(academy);
    state = { phase: "ready", available, draft: value, message: available ? "Tell me the teacher’s details. We’ll collect missing information before review. No teacher is created by sending a message." : "Conversational sending is unavailable for this draft. Manual and protected review remain separate." };
  }
  check(); } catch (error) { dispose(); throw error; }
  return {
    get current() { return current(); }, get isCurrent() { return valid(); },
    get pendingRequestId() { return requestId; },
    send: (text: string) => run(async () => {
      const before = state.draft;
      if (!state.available || state.phase !== "ready" || !before || !text.trim() || text.length > 2000 || /[\u0000-\u0008\u000b-\u001f\u007f-\u009f]/.test(text) || Date.parse(before.expiresAtUtc) <= Date.now()) fail();
      await account(academy); if (!await readiness()) fail();
      const nextId = crypto.randomUUID(), nextRoute = `${window.location.pathname}?conversationId=${session}&draftId=${draft}&teacherRequestId=${nextId}`;
      // Opaque request identity must survive reload before the only POST. Prompt
      // text and canonical/private fields are never placed in URL or storage.
      native?.dispose(); native = null;
      window.history.replaceState(window.history.state, "", nextRoute + (window.location.hash ?? ""));
      if (location() !== nextRoute) fail(); route = nextRoute; check(); requestId = nextId;
      state = { phase: "pending", available: false, draft: null, message: "Checking this teacher request…" };
      return finish(await call(resource + "/conversation/turns", { requestId, draftRevision: before.revision, payloadHash: before.payloadHash, text }, [200, 202]));
    }),
    recover: () => run(async () => {
      if (!requestId) fail(); await account(academy);
      return finish(await call(resource + `/conversation/turns/${requestId}/result`, undefined, [200, 202]));
    }),
    dispose,
  };
}
