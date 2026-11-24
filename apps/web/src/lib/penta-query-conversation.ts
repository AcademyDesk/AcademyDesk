import { readPentaJson } from "./penta-onboarding-channel";
import { validPentaSession, type PentaSession } from "./penta-chat-contract";
import type { MiniChannel, MiniPointer } from "./penta-mini-conversation";
import { validMiniMoneyAmount } from "./penta-mini-conversation";

export const queryPointerKey = "academydesk.penta.query-recovery.v1";
export const queryProtocol = "academy-query.v1";
export type QueryEntity = "teacher" | "student" | "batch" | "student-fees";
export const queryBalanceScope = "All outstanding fees for each student; subject filters select students, not course-specific invoices.";
export const queryEntityLabel = (entity: QueryEntity) => entity === "student-fees" ? "student balances" : entity === "batch" ? "batches" : entity + "s";
type QueryContext = { entity: QueryEntity | null; filters: Record<string, string>; resultIds: string[] };
export type QueryReceipt = { conversationId: string; requestId: string; version: number; kind: string; message: string;
  context: QueryContext; result: { entity: QueryEntity; records: { id: string; label: string; code: string | null; active: boolean; subjects: string[]; sourcePath: string;
    balances?: { currency: string; outstandingExact: string }[] }[];
    hasMore: boolean; asOfUtc: string; balanceScope?: string } | null; inputText: string; protocol: typeof queryProtocol };
export type QueryView = { phase: "checking" | "unavailable" | "ready" | "idle" | "unknown" | "closed";
  available: boolean; pending: boolean; session: PentaSession | null; pointer: MiniPointer | null;
  turns: QueryReceipt[]; notice: string; entities: QueryEntity[]; hasMore: boolean };
export const initialQueryView: QueryView = { phase: "checking", available: false, pending: false,
  session: null, pointer: null, turns: [], notice: "Checking connected academy tools…", entities: [], hasMore: false };
const object = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const exact = (v: Record<string, unknown>, keys: string[]) => Object.keys(v).length === keys.length && keys.every(k => Object.hasOwn(v, k));
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
const entity = (v: unknown): v is QueryEntity => v === "teacher" || v === "student" || v === "batch" || v === "student-fees";
const text = (v: unknown, max: number): v is string => typeof v === "string" && v.length > 0 && v.length <= max && !/[\u0000-\u001f\u007f-\u009f]/.test(v);
const version = (v: unknown): v is number => Number.isSafeInteger(v) && Number(v) >= 0 && Number(v) <= 50;
const date = (v: unknown): v is string => typeof v === "string" && /^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d{1,7})?Z$/.test(v) &&
  Number.isFinite(Date.parse(v)) && new Date(v).toISOString().slice(0, 19) === v.slice(0, 19);
const path = (e: QueryEntity, value: string) => e === "teacher" ? `/teacher-profile?teacherId=${value}` : e === "student" || e === "student-fees" ? `/student-management?studentId=${value}` : "/batch-setup";
const fields = (e: QueryEntity) => e === "teacher" ? ["name", "subject", "status"] : e === "student-fees" ? ["name", "subject", "balance_status"] : ["name", "status"];

export function validQueryReceipt(v: unknown, session: string): v is QueryReceipt {
  if (!object(v) || !exact(v, ["conversationId", "requestId", "version", "kind", "message", "context", "result", "inputText", "protocol"]) ||
    v.protocol !== queryProtocol || v.conversationId !== session || !id(v.requestId) || !version(v.version) || v.version < 1 ||
    !text(v.message, 500) || typeof v.inputText !== "string" || !v.inputText.trim() || v.inputText.length > 2000 ||
    /[\u0000-\u0008\u000b-\u001f\u007f-\u009f]/.test(v.inputText) ||
    !["RESULT", "ERROR", "UNSUPPORTED", "CLARIFICATION_REQUIRED"].includes(String(v.kind)) || !object(v.context)) return false;
  const c = v.context;
  if (!exact(c, ["entity", "filters", "resultIds"]) || c.entity !== null && !entity(c.entity) || !object(c.filters) ||
    Object.keys(c.filters).length > 3 || !Object.entries(c.filters).every(([k, value]) => text(value, 100) &&
      (k === "name" || k === "subject" && (c.entity === "teacher" || c.entity === "student-fees") ||
        k === "balance_status" && c.entity === "student-fees" && ["Pending", "Clear"].includes(value) ||
        k === "status" && c.entity !== "student-fees" && ["Active", "Inactive"].includes(value))) ||
    !Array.isArray(c.resultIds) || c.resultIds.length > 10 || !c.resultIds.every(id) || new Set(c.resultIds).size !== c.resultIds.length ||
    c.entity === null && (c.resultIds.length !== 0 || Object.keys(c.filters).length !== 0)) return false;
  if (v.result === null) return v.kind !== "RESULT";
  const r = v.result;
  if (v.kind !== "RESULT" || !object(r) || !exact(r, ["entity", "records", "hasMore", "asOfUtc", ...(r.entity === "student-fees" ? ["balanceScope"] : [])]) || !entity(r.entity) ||
    r.entity === "student-fees" && r.balanceScope !== queryBalanceScope ||
    r.entity !== c.entity || typeof r.hasMore !== "boolean" || !date(r.asOfUtc) || !Array.isArray(r.records) || r.records.length > 10 ||
    r.records.length !== c.resultIds.length) return false;
  return r.records.every((row: unknown, index: number) => object(row) && exact(row, ["id", "label", "code", "active", "subjects", "sourcePath", ...(r.entity === "student-fees" ? ["balances"] : [])]) &&
    id(row.id) && row.id === (c.resultIds as string[])[index] && text(row.label, 240) && (row.code === null || text(row.code, 100)) &&
    typeof row.active === "boolean" && Array.isArray(row.subjects) && row.subjects.length <= (r.entity === "student-fees" ? 5 : 1) && row.subjects.every(x => text(x, 200)) &&
    (r.entity !== "student-fees" || Array.isArray(row.balances) && row.balances.length <= 20 && row.balances.every(b => object(b) &&
      exact(b, ["currency", "outstandingExact"]) && typeof b.currency === "string" && /^[A-Z]{3}$/.test(b.currency) && validMiniMoneyAmount(b.outstandingExact)) &&
      new Set(row.balances.map(b => b.currency)).size === row.balances.length) &&
    row.sourcePath === path(r.entity as QueryEntity, row.id));
}

function pointer(value: unknown, channel: MiniChannel): value is MiniPointer {
  return object(value) && exact(value, ["epoch", "academyId", "conversationId", "requestId", "expectedVersion", "expiresAtUtc"]) &&
    value.epoch === channel.epoch && value.academyId === channel.academyId && id(value.conversationId) && id(value.requestId) &&
    version(value.expectedVersion) && date(value.expiresAtUtc);
}
class Status extends Error { constructor(readonly code: number) { super("Query response unavailable"); } }

export function createQueryConversation(channel: MiniChannel, storage: () => Storage, changed: (view: QueryView) => void,
  uuid = () => crypto.randomUUID(), now = Date.now) {
  let view = initialQueryView, busy = false, disposed = false;
  const current = () => !disposed && !channel.signal.aborted && channel.current();
  const patch = (v: Partial<QueryView>) => { if (current()) { view = { ...view, ...v }; changed(view); } };
  const stored = (): MiniPointer | null => { const raw = storage().getItem(queryPointerKey); if (!raw) return null;
    try { const v: unknown = JSON.parse(raw); if (pointer(v, channel)) return v; } catch { /* malformed private pointer */ }
    storage().removeItem(queryPointerKey); return null; };
  const same = (a: unknown, b: unknown) => JSON.stringify(a) === JSON.stringify(b);
  async function call(url: string, method: "GET" | "POST", body?: unknown) {
    if (!current()) throw Error("Session changed");
    const response = await channel.send(url, method, body);
    if (!current()) throw Error("Session changed");
    if (response.status === 401 || response.status === 403) { await response.body?.cancel(); channel.invalidate(); throw new Status(response.status); }
    if (response.status !== (method === "GET" ? 200 : 201) || !response.headers.get("cache-control")?.includes("no-store")) {
      await response.body?.cancel(); throw new Status(response.status); }
    const value = await readPentaJson(response, channel.signal);
    if (!current()) throw Error("Session changed");
    return value;
  }
  function accept(value: unknown, saved: MiniPointer) {
    if (!validQueryReceipt(value, saved.conversationId) || value.requestId !== saved.requestId || value.version !== saved.expectedVersion + 1) throw Error("Unverified receipt");
    const turns = [...view.turns.filter(x => x.version < value.version), value];
    patch({ phase: "idle", pointer: saved, turns, session: { conversationId: saved.conversationId, version: value.version, expiresAtUtc: saved.expiresAtUtc }, notice: "" });
  }
  async function history(saved: MiniPointer) {
    const value = await call(`/conversations/${saved.conversationId}`, "GET");
    if (!object(value) || !exact(value, ["conversationId", "version", "expiresAtUtc", "turns", "pendingRequestId", "hasMore"]) ||
      value.conversationId !== saved.conversationId || value.version !== saved.expectedVersion + 1 || value.expiresAtUtc !== saved.expiresAtUtc ||
      value.pendingRequestId !== null || typeof value.hasMore !== "boolean" || !Array.isArray(value.turns) || value.turns.length > 20 ||
      !value.turns.every(x => validQueryReceipt(x, saved.conversationId)) ||
      value.turns.some((x, i, turns) => i > 0 && x.version !== turns[i - 1].version + 1) ||
      new Set(value.turns.map(x => x.requestId)).size !== value.turns.length ||
      value.turns.at(-1)?.requestId !== saved.requestId) throw Error("Unverified conversation history");
    patch({ turns: value.turns, hasMore: value.hasMore });
  }
  async function recover() {
    const saved = view.pointer; if (!saved) return;
    patch({ phase: "unknown", turns: [] });
    accept(await call(`/conversations/${saved.conversationId}/turns/${saved.requestId}`, "GET"), saved);
    await history(saved);
  }
  async function run(action: () => Promise<void>) {
    if (busy || !current()) return;
    busy = true; patch({ pending: true, notice: "" });
    try { await action(); }
    catch (error) { if (current()) patch(view.pointer ? { phase: error instanceof Status && error.code === 410 ? "closed" : "unknown", turns: [],
        notice: "The response is unconfirmed. Recover the saved request; do not repeat the prompt. Manual remains available." }
      : { phase: "unavailable", notice: "The academy conversation is unavailable. Your guided workflows and Manual remain available." }); }
    finally { busy = false; patch({ pending: false }); }
  }
  return {
    initialise: () => run(async () => {
      const saved = stored(); if (saved) patch({ pointer: saved, phase: "unknown" });
      try {
        const health = await call("/health", "GET");
        if (!object(health) || !exact(health, ["protocol", "readOnly", "tools", "status"]) || health.protocol !== queryProtocol || health.readOnly !== true ||
          !["Available", "Unavailable"].includes(String(health.status)) || !Array.isArray(health.tools) || health.tools.length > 8 ||
          !health.tools.every(x => object(x) && exact(x, ["key", "entity", "kind", "readOnly", "fields"]) && entity(x.entity) && ["search", "open"].includes(String(x.kind)) &&
            x.key === `AcademyDesk.${x.entity}.${x.kind}` && x.readOnly === true && Array.isArray(x.fields) &&
            x.fields.length === fields(x.entity).length && new Set(x.fields).size === x.fields.length &&
            x.fields.every(f => fields(x.entity as QueryEntity).includes(f))) ||
          new Set(health.tools.map(x => x.key)).size !== health.tools.length) throw Error("Unverified tool catalog");
        patch({ available: health.status === "Available", entities: [...new Set(health.tools.map(x => x.entity as QueryEntity))] });
      } catch { if (!current()) return; patch({ available: false }); }
      if (saved) await recover(); else patch({ phase: view.available ? "ready" : "unavailable" });
    }),
    start: () => run(async () => {
      if (!view.available) return;
      const old = stored(); if (!same(old, view.pointer)) throw Error("Another recovery request exists");
      const value = await call("/conversations", "POST", {});
      if (!validPentaSession(value) || value.version !== 0 || !date(value.expiresAtUtc) || Date.parse(value.expiresAtUtc) <= now()) throw Error("Unverified session");
      if (!same(stored(), old)) throw Error("Another recovery request exists");
      storage().removeItem(queryPointerKey); patch({ session: value, pointer: null, turns: [], hasMore: false, phase: "idle" });
    }),
    send: async (input: string) => { let sent = false; await run(async () => {
      const session = view.session, text = input.trim();
      if (!view.available || view.phase !== "idle" || !session || !text || text.length > 2000 || /[\u0000-\u0008\u000b-\u001f\u007f-\u009f]/.test(text)) return;
      if (session.version >= 50 || Date.parse(session.expiresAtUtc) <= now()) { patch({ phase: "closed", turns: [], notice: "Start a separate conversation; this session expired or reached its turn limit." }); return; }
      const saved: MiniPointer = { epoch: channel.epoch, academyId: channel.academyId, conversationId: session.conversationId,
        requestId: uuid(), expectedVersion: session.version, expiresAtUtc: session.expiresAtUtc };
      if (!id(saved.requestId) || !same(stored(), view.pointer)) throw Error("Recovery scope changed");
      storage().setItem(queryPointerKey, JSON.stringify(saved));
      patch({ pointer: saved, phase: "unknown" }); sent = true;
      accept(await call(`/conversations/${session.conversationId}/turns`, "POST", { requestId: saved.requestId, expectedVersion: saved.expectedVersion, text }), saved);
    }); return sent; },
    recover: () => run(recover), dispose: () => { disposed = true; view = initialQueryView; },
  };
}
