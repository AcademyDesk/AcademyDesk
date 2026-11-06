// Teacher-specific Testing host startup adapter; no model qualification or UI activation.
// Explicit, bounded host session bootstrap. No prompt/inference/domain mutation.
import { apiUrl, portalAccessToken, portalSessionEpoch, portalWorkspaceForPath } from "./api";
import { validPentaAccount } from "./penta-chat-contract";
import { readPentaJson } from "./penta-onboarding-channel";
type Request = (url: string, init: RequestInit) => Promise<Response>;
export type TeacherStartResult = { conversationId: string; draftId: string };
export type TeacherStartHandle = { available: boolean; recovering: boolean; start: () => Promise<TeacherStartResult>; check: () => Promise<TeacherStartResult> };
const object = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
function fail(): never { throw new Error("The teacher conversation start could not be verified. Check the same start request; do not start another. Manual remains available."); }
const keys = (v: Record<string, unknown>, expected: string) => Object.keys(v).sort().join(",") === expected.split(",").sort().join(",");

export async function loadTeacherStart(signal: AbortSignal, request: Request = fetch): Promise<TeacherStartHandle> {
  const bearer = portalAccessToken("AcademyAdmin");
  const loginEpoch = portalSessionEpoch("AcademyAdmin");
  const location = () => typeof window === "undefined" ? "" : window.location.pathname + (window.location.search ?? "");
  let route = location();
  const current = () => !!bearer && !signal.aborted && portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === loginEpoch && typeof window !== "undefined" &&
    portalWorkspaceForPath(window.location.pathname) === "AcademyAdmin" && location() === route;
  if (!current()) fail();
  // A URL keeps only the opaque idempotency lookup key, never a prompt, record,
  // tenant, role, bearer or approval. It is not authority: the existing host GET
  // must resolve the current authenticated actor/academy before returning a draft.
  const parameters = new URLSearchParams(window.location.search ?? "");
  const recovered = parameters.get("teacherStartRequestId");
  if (parameters.size !== 0 && (parameters.size !== 1 || !id(recovered))) fail();
  async function call(path: string, body?: unknown): Promise<unknown> {
    if (!current()) fail();
    const response = await request(apiUrl + path, { method: body === undefined ? "GET" : "POST", signal,
      credentials: "omit", cache: "no-store", redirect: "error", referrerPolicy: "no-referrer",
      headers: { Authorization: `Bearer ${bearer}`, Accept: "application/json", ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
      body: body === undefined ? undefined : JSON.stringify(body) });
    if (!current() || !(body === undefined ? [200] : [200, 201]).includes(response.status)) { await response.body?.cancel().catch(() => {}); fail(); }
    const value = await readPentaJson(response, signal); if (!current()) fail(); return value;
  }
  async function account(academy?: string): Promise<string> {
    const value = await call("/api/auth/session");
    if (!object(value) || value.workspace !== "AcademyAdmin" || !validPentaAccount(value) || !id(value.academyId) || academy && value.academyId !== academy) fail();
    return value.academyId;
  }
  const academy = await account(), base = `/api/academies/${academy}/penta/teachers`;
  async function readiness(): Promise<boolean> {
    const value = await call(base + "/readiness");
    if (!object(value) || !keys(value, "version,academyId,available,canStart,canExecute,syntheticInterpreter") || value.version !== "teacher-start/1" ||
        value.academyId !== academy || typeof value.available !== "boolean" || value.canStart !== value.available || typeof value.syntheticInterpreter !== "boolean" || (value.available && !value.syntheticInterpreter) || value.canExecute !== false) fail();
    return value.available;
  }
  const available = await readiness() && recovered === null;
  let used = recovered !== null, busy = false, requestId: string | null = recovered;
  function retainLookupKey() {
    if (!current() || !id(requestId)) fail();
    const next = `${window.location.pathname}?teacherStartRequestId=${requestId}`;
    try { window.history.replaceState(window.history.state, "", next + (window.location.hash ?? "")); }
    catch { fail(); }
    // Only our exact self-authored history replacement may move this handle's
    // route fence. A failed replacement or concurrent navigation cannot send.
    if (location() !== next) fail();
    route = next;
    if (!current()) fail();
  }
  function result(value: unknown): TeacherStartResult {
    if (!object(value) || !keys(value, "version,academyId,requestId,conversationId,draftId,expiresAtUtc,status,canExecute,domainEffect") ||
        value.version !== "teacher-start/1" || value.academyId !== academy || value.requestId !== requestId ||
        !id(value.conversationId) || !id(value.draftId) || value.conversationId === value.draftId || value.conversationId === requestId || value.draftId === requestId ||
        value.status !== "conversation_started" || value.canExecute !== false || value.domainEffect !== false ||
        typeof value.expiresAtUtc !== "string" || !/(Z|[+-]\d{2}:\d{2})$/.test(value.expiresAtUtc) ||
        !Number.isFinite(Date.parse(value.expiresAtUtc)) || Date.parse(value.expiresAtUtc) <= Date.now()) fail();
    return { conversationId: value.conversationId, draftId: value.draftId };
  }
  async function run(readOnly: boolean): Promise<TeacherStartResult> {
    if (busy || !current() || readOnly && !requestId || !readOnly && (used || !available)) fail();
    busy = true;
    try {
      if (!readOnly) {
        used = true; requestId = crypto.randomUUID();
        await account(academy); if (!await readiness()) fail();
        retainLookupKey(); // must complete before the only start POST
      }
      const value = result(await call(readOnly ? `${base}/starts/${requestId}` : `${base}/conversations`, readOnly ? undefined : { requestId }));
      await account(academy); if (!current()) fail();
      return value;
    } finally { busy = false; }
  }
  return { available, recovering: recovered !== null, start: () => run(false), check: () => run(true) };
}
