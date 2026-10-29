// Host workflow transport only. No model/tool registration, prompt interpretation,
// implicit approval, automatic POST, account creation or production activation.
import { apiUrl, portalAccessToken, portalSessionEpoch, portalWorkspaceForPath, portalSessionChangedEvent,
  portalSignOutEvent, isPortalSignOutEvent } from "./api";
import { validPentaAccount } from "./penta-chat-contract";
import { readPentaJson } from "./penta-onboarding-channel";

type Request = (url: string, init: RequestInit) => Promise<Response>;
export type TeacherCreationReceipt = { receiptId: string; teacherId: string; auditId: string; completedAtUtc: string };
export type TeacherCreationView = { id: string; sessionId: string; contextVersion: number; revision: number;
  status: "AwaitingReview" | "Reviewed" | "Authorized" | "Succeeded" | "Cancelled";
  expiresAtUtc: string; canonicalPayload: string; payloadHash: string; approvalId: string | null;
  approvalExpiresAtUtc: string | null; receipt: TeacherCreationReceipt | null };
export class TeacherCreationUnavailable extends Error {
  constructor(public readonly access = false) {
    super(access ? "Current academy access could not be verified." :
      "The teacher outcome is unconfirmed. Check this saved draft; do not submit another creation request.");
  }
}
const object = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const id = (v: unknown): v is string => typeof v === "string" &&
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
const instant = (v: unknown): v is string => typeof v === "string" &&
  /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$/.test(v) && Number.isFinite(Date.parse(v));
const keys = (v: Record<string, unknown>, expected: string[]) => Object.keys(v).sort().join(",") === [...expected].sort().join(",");
function fail(access = false): never { throw new TeacherCreationUnavailable(access); }
const fields: Record<string, [string, number]> = {
  FirstName: ["text", 120], LastName: ["text", 120], Email: ["text", 320], Phone: ["text", 30], Specialties: ["text", 500],
  Qualifications: ["text", 1000], EmploymentType: ["text", 50], DateOfBirth: ["date", 10], JoiningDate: ["date", 10],
  AddressLine1: ["text", 240], City: ["text", 100], State: ["text", 100], PostalCode: ["text", 30], BranchId: ["reference", 36],
};
async function view(value: unknown, session: string, draft: string): Promise<TeacherCreationView> {
  if (!object(value) || !keys(value, ["id", "sessionId", "contextVersion", "revision", "status", "expiresAtUtc", "canonicalPayload",
    "payloadHash", "approvalId", "approvalExpiresAtUtc", "receipt"]) || value.id !== draft || value.sessionId !== session ||
    !Number.isSafeInteger(value.contextVersion) || Number(value.contextVersion) < 0 || Number(value.contextVersion) > 9007199254740991 ||
    !Number.isSafeInteger(value.revision) || Number(value.revision) < 1 || Number(value.revision) > 100 ||
    !["AwaitingReview", "Reviewed", "Authorized", "Succeeded", "Cancelled"].includes(String(value.status)) ||
    !instant(value.expiresAtUtc) || typeof value.canonicalPayload !== "string" || value.canonicalPayload.length > 6000 ||
    typeof value.payloadHash !== "string" || !/^[0-9a-f]{64}$/.test(value.payloadHash)) fail();
  const digest = Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value.canonicalPayload))))
    .map(x => x.toString(16).padStart(2, "0")).join("");
  if (digest !== value.payloadHash) fail();
  let payload: unknown;
  try { payload = JSON.parse(value.canonicalPayload); } catch { fail(); }
  if (!object(payload) || !keys(payload, ["tool_key", "tool_revision", "fields"]) ||
    payload.tool_key !== "AcademyDesk.Teacher.Create" || payload.tool_revision !== 1 || !object(payload.fields)) fail();
  for (const [key, field] of Object.entries(payload.fields)) {
    const spec = Object.hasOwn(fields, key) ? fields[key] : null;
    if (!spec || !object(field) || !keys(field, ["kind", "value"]) || field.kind !== spec[0] ||
      typeof field.value !== "string" || field.value.length > spec[1] || /[\u0000-\u001f\u007f-\u009f]/.test(field.value)) fail();
    if (spec[0] === "reference" && !id(field.value)) fail();
    if (spec[0] === "date" && (!/^\d{4}-\d{2}-\d{2}$/.test(field.value) ||
      !Number.isFinite(Date.parse(field.value)) || new Date(field.value).toISOString().slice(0, 10) !== field.value)) fail();
  }
  const approved = ["Reviewed", "Authorized", "Succeeded"].includes(String(value.status));
  if (approved ? !id(value.approvalId) || !instant(value.approvalExpiresAtUtc) || Date.parse(value.approvalExpiresAtUtc) > Date.parse(value.expiresAtUtc)
    : value.approvalId !== null || value.approvalExpiresAtUtc !== null) fail();
  if (value.status === "Succeeded") {
    const receipt = value.receipt;
    if (!object(receipt) || !keys(receipt, ["receiptId", "teacherId", "auditId", "completedAtUtc"]) ||
      !id(receipt.receiptId) || !id(receipt.teacherId) || !id(receipt.auditId) || !instant(receipt.completedAtUtc) ||
      !instant(value.approvalExpiresAtUtc) || Date.parse(receipt.completedAtUtc) > Date.parse(value.approvalExpiresAtUtc)) fail();
  } else if (value.receipt !== null) fail();
  return value as unknown as TeacherCreationView;
}

// URL values are lookup keys only. Academy comes from the current authenticated
// account. Every operation is independently reauthorized by the backend.
export async function loadTeacherCreationChannel(session: string, draft: string, signal: AbortSignal, request: Request = fetch) {
  if (!id(session) || !id(draft) || session === draft || typeof window === "undefined") fail();
  const bearer = portalAccessToken("AcademyAdmin"), epoch = portalSessionEpoch("AcademyAdmin");
  const location = () => window.location.pathname + (window.location.search ?? ""), route = location();
  const controller = new AbortController(); let closed = false, busy = false, uncertain = false, attemptedExecution = false;
  const current = () => !closed && !signal.aborted && !controller.signal.aborted && !!bearer &&
    portalAccessToken("AcademyAdmin") === bearer && portalSessionEpoch("AcademyAdmin") === epoch && location() === route &&
    portalWorkspaceForPath(window.location.pathname) === "AcademyAdmin";
  const check = () => { if (!current()) fail(true); };
  async function call(path: string, body?: unknown, statuses = [200]) {
    check();
    const response = await request(apiUrl + path, { method: body === undefined ? "GET" : "POST", cache: "no-store", redirect: "error",
      credentials: "omit", signal: controller.signal, headers: { Authorization: `Bearer ${bearer}`, ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
      body: body === undefined ? undefined : JSON.stringify(body) });
    check();
    if (response.status === 401 || response.status === 403) { dispose(); fail(true); }
    if (!statuses.includes(response.status)) fail();
    const result = await readPentaJson(response, controller.signal); check(); return result;
  }
  check();
  const account = await call("/api/auth/session");
  const workspace = object(account) ? account.workspace : null;
  if (!validPentaAccount(account) || workspace !== "AcademyAdmin") fail(true);
  const base = `/api/academies/${account.academyId}/penta/teachers/conversations/${session}/drafts/${draft}`;
  let state = await view(await call(base), session, draft); check();
  const again = await call("/api/auth/session");
  const currentWorkspace = object(again) ? again.workspace : null;
  if (!validPentaAccount(again) || again.academyId !== account.academyId || currentWorkspace !== "AcademyAdmin") fail(true);
  function dispose() {
    closed = true; controller.abort(); signal.removeEventListener("abort", dispose);
    window.removeEventListener(portalSignOutEvent, local); window.removeEventListener(portalSessionChangedEvent, local);
    window.removeEventListener("storage", storage);
  }
  function local(event: Event) { if ((event as CustomEvent).detail === "AcademyAdmin") dispose(); }
  function storage(event: StorageEvent) { if (isPortalSignOutEvent(event, "AcademyAdmin") || !current()) dispose(); }
  signal.addEventListener("abort", dispose, { once: true });
  window.addEventListener(portalSignOutEvent, local); window.addEventListener(portalSessionChangedEvent, local); window.addEventListener("storage", storage);
  const snapshot = () => JSON.parse(JSON.stringify(state)) as TeacherCreationView;
  async function operation(action: "review" | "approve" | "execute" | "cancel") {
    check(); if (busy || uncertain || state.status === "Succeeded" || state.status === "Cancelled" ||
      Date.parse(state.expiresAtUtc) <= Date.now()) fail();
    if (action === "review" && !["AwaitingReview", "Reviewed"].includes(state.status)) fail();
    if ((action === "approve" || action === "execute") && (!state.approvalId || !state.approvalExpiresAtUtc ||
      Date.parse(state.approvalExpiresAtUtc) <= Date.now() || state.status !== (action === "approve" ? "Reviewed" : "Authorized"))) fail();
    if (action === "execute" && attemptedExecution) fail();
    busy = true; const before = snapshot();
    if (action === "execute") attemptedExecution = true;
    try {
      const body = { revision: before.revision, payloadHash: before.payloadHash,
        ...(["approve", "execute"].includes(action) ? { approvalId: before.approvalId } : {}),
        ...(action === "approve" ? { intent: "CreateTeacherWithoutAccount" } : {}) };
      const after = await view(await call(`${base}/${action}`, body, action === "execute" ? [200, 201] : [200]), session, draft); check();
      const expected = { review: "Reviewed", approve: "Authorized", execute: "Succeeded", cancel: "Cancelled" }[action];
      if (after.status !== expected || after.contextVersion !== before.contextVersion || after.revision !== before.revision ||
        after.canonicalPayload !== before.canonicalPayload || after.payloadHash !== before.payloadHash || after.expiresAtUtc !== before.expiresAtUtc ||
        (["approve", "execute"].includes(action) && (after.approvalId !== before.approvalId || after.approvalExpiresAtUtc !== before.approvalExpiresAtUtc))) fail();
      state = after; return snapshot();
    } catch (error) { uncertain = true; throw error; } finally { busy = false; }
  }
  return {
    get current() { check(); return snapshot(); },
    get outcomeUnconfirmed() { return uncertain; },
    review: () => operation("review"), approve: () => operation("approve"), execute: () => operation("execute"), cancel: () => operation("cancel"),
    async refresh() {
      check(); if (busy) fail(); busy = true;
      try { const recovered = await view(await call(base), session, draft); check(); state = recovered; return snapshot(); }
      finally { busy = false; }
      // Deliberately does not reset uncertain/attemptedExecution. GET recovery
      // cannot silently re-arm any mutation after an interrupted request.
    }, dispose,
  };
}
