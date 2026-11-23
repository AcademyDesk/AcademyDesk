import { readPentaJson } from "./penta-onboarding-channel";
import { validPentaHealth, validPentaReceipt, validPentaSession, type PentaReceipt, type PentaSession } from "./penta-chat-contract";

export type MiniExactMoney = { version: "mini-exact-money.v1"; rows: { sourceId: string; balances: { currency: string; outstandingExact: string }[] }[] };
export type MiniReadReceipt = PentaReceipt & { exactMoney?: MiniExactMoney | null };
export const miniPointerKey = "academydesk.penta.mini-read-recovery.v1";
export type MiniPointer = { epoch: string; academyId: string; conversationId: string; requestId: string; expectedVersion: number; expiresAtUtc: string };
export type MiniView = { phase: "checking" | "unavailable" | "ready" | "idle" | "unknown" | "closed";
  available: boolean; pending: boolean; session: PentaSession | null; pointer: MiniPointer | null; receipt: MiniReadReceipt | null; notice: string };
export const initialMiniView: MiniView = { phase: "checking", available: false, pending: false, session: null, pointer: null, receipt: null, notice: "Checking private Mini availability…" };
export type MiniChannel = { academyId: string; epoch: string; current(): boolean; invalidate(): void; signal: AbortSignal;
  send(path: string, method: "GET" | "POST", body?: unknown): Promise<Response> };
const obj = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const exact = (v: Record<string, unknown>, keys: string[]) => Object.keys(v).length === keys.length && keys.every(k => Object.hasOwn(v, k));
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
const version = (v: unknown): v is number => typeof v === "number" && Number.isSafeInteger(v) && v >= 0 && v < 50;
const expires = (v: unknown): v is string => typeof v === "string" && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$/.test(v) && Number.isFinite(Date.parse(v)) && new Date(v).toISOString().slice(0, 10) === v.slice(0, 10);
const keys = ["epoch", "academyId", "conversationId", "requestId", "expectedVersion", "expiresAtUtc"];

export function readMiniPointer(storage: Storage, academyId: string, epoch: string): MiniPointer | null {
  const raw = storage.getItem(miniPointerKey);
  if (!raw) return null;
  try {
    const v: unknown = JSON.parse(raw);
    if (obj(v) && exact(v, keys) && v.academyId === academyId && v.epoch === epoch && id(academyId) &&
      id(v.conversationId) && id(v.requestId) && version(v.expectedVersion) && expires(v.expiresAtUtc)) return v as MiniPointer;
  } catch { /* Invalid local data cannot establish a session. */ }
  storage.removeItem(miniPointerKey);
  return null;
}
// Validate .NET decimal representability lexically. Never parse/format financial
// strings with Number, floating-point arithmetic or locale-dependent conversion.
export function validMiniMoneyAmount(value: unknown): value is string {
  if (typeof value !== "string" || !/^(?:0|[1-9]\d{0,28})\.\d{2}$/.test(value)) return false;
  let coefficient = value.replace(".", ""), scale = 2;
  while (scale > 0 && coefficient.endsWith("0")) { coefficient = coefficient.slice(0, -1); scale--; }
  coefficient = coefficient.replace(/^0+(?=\d)/, "");
  const maximum = "79228162514264337593543950335";
  return coefficient.length < maximum.length || coefficient.length === maximum.length && coefficient <= maximum;
}
function validMoney(value: unknown, result: NonNullable<PentaReceipt["result"]>): value is MiniExactMoney {
  return obj(value) && exact(value, ["version", "rows"]) && value.version === "mini-exact-money.v1" && Array.isArray(value.rows) &&
    value.rows.length === result.rows.length && value.rows.every((row: unknown, index: number) => obj(row) &&
      exact(row, ["sourceId", "balances"]) && row.sourceId === result.rows[index].sourceId && Array.isArray(row.balances) &&
      row.balances.length === result.rows[index].balances.length && row.balances.every((balance: unknown, b: number) =>
        obj(balance) && exact(balance, ["currency", "outstandingExact"]) && balance.currency === result.rows[index].balances[b].currency &&
        validMiniMoneyAmount(balance.outstandingExact)));
}
function checkedReceipt(value: unknown, pointer: MiniPointer): value is MiniReadReceipt {
  const session = { conversationId: pointer.conversationId, version: pointer.expectedVersion, expiresAtUtc: pointer.expiresAtUtc };
  if (!validPentaReceipt(value, session, pointer.requestId, "executor") || !exact(value as unknown as Record<string, unknown>,
    ["conversationId", "requestId", "version", "kind", "message", "capability", "provider", "protocol", "context", "result", ...(obj(value) && Object.hasOwn(value, "exactMoney") ? ["exactMoney"] : [])])) return false;
  if (!exact(value.context, ["filters", "sort_by", "current_learner_id", "current_result_ids"])) return false;
  const money = (value as MiniReadReceipt).exactMoney;
  if (!value.result) return money == null;
  if (!exact(value.result, ["count", "hasMore", "rows", "asOfUtc", "source", "balanceScope"])) return false;
  const rowsValid = value.result.rows.every(row => exact(row, ["sourceId", "displayName", "subjects", "balances", "sourcePath", ...(Object.hasOwn(row, "recordCode") ? ["recordCode"] : [])]) &&
    row.displayName.length <= 500 && (row.recordCode == null || row.recordCode.length <= 120) &&
    row.subjects.every(s => s.length <= 500) && row.balances.length <= 20 && row.balances.every(b => exact(b, ["currency", "outstanding"])));
  return rowsValid && (money == null || validMoney(money, value.result));
}
class StatusError extends Error { constructor(readonly code: number) { super("Mini response unavailable."); } }
class StorageError extends Error {}

export function createMiniConversation(channel: MiniChannel, storage: () => Storage, changed: (view: MiniView) => void,
  uuid = () => crypto.randomUUID(), now = Date.now) {
  let view = initialMiniView, busy = false, disposed = false;
  const current = () => !disposed && !channel.signal.aborted && channel.current();
  const patch = (next: Partial<MiniView>) => { if (current()) { view = { ...view, ...next }; changed(view); } };
  const stored = () => readMiniPointer(storage(), channel.academyId, channel.epoch);
  const same = (a: MiniPointer | null, b: MiniPointer | null) => JSON.stringify(a) === JSON.stringify(b);
  function persist(pointer: MiniPointer) {
    try {
      if (!same(stored(), view.pointer)) throw Error("Another saved request exists.");
      storage().setItem(miniPointerKey, JSON.stringify(pointer));
    } catch { throw new StorageError(); }
  }
  async function call(path: string, method: "GET" | "POST", body?: unknown): Promise<unknown> {
    if (!current()) throw Error("Session changed.");
    const response = await channel.send(path, method, body);
    if (!current()) throw Error("Session changed.");
    if (response.status === 401 || response.status === 403) {
      await response.body?.cancel().catch(() => {}); channel.invalidate(); throw new StatusError(response.status);
    }
    const expected = method === "GET" ? 200 : 201;
    if (response.status !== expected || !response.headers.get("cache-control")?.includes("no-store")) {
      await response.body?.cancel().catch(() => {}); throw new StatusError(response.status);
    }
    const value = await readPentaJson(response, channel.signal);
    if (!current()) throw Error("Session changed.");
    return value;
  }
  function accept(value: unknown, pointer: MiniPointer) {
    if (!checkedReceipt(value, pointer)) throw Error("Unverified receipt.");
    patch({ phase: "idle", session: { conversationId: pointer.conversationId, version: value.version, expiresAtUtc: pointer.expiresAtUtc },
      pointer, receipt: value, notice: "" });
  }
  async function recover() {
    const pointer = view.pointer;
    if (!pointer) return;
    patch({ phase: "unknown", receipt: null });
    const value = await call("/conversations/" + pointer.conversationId + "/turns/" + pointer.requestId, "GET");
    accept(value, pointer);
  }
  async function run(action: () => Promise<void>, recovering = false) {
    if (busy || !current()) return;
    busy = true; patch({ pending: true, notice: "" });
    try { await action(); }
    catch (error) {
      if (!current()) return;
      if (error instanceof StorageError) patch({ notice: "No Mini prompt was sent because private recovery details could not be saved." });
      else if (view.pointer) patch({ phase: recovering && error instanceof StatusError && [404, 410].includes(error.code) ? "closed" : "unknown",
        receipt: null, notice: "The response is unconfirmed. Check this saved request or use Manual. Do not repeat the prompt." });
      else patch({ phase: "unavailable", notice: "Private Mini is unavailable or could not verify this response. Continue with the guided workspace or Manual." });
    } finally { busy = false; patch({ pending: false }); }
  }
  return {
    initialise: () => run(async () => {
      const pointer = stored();
      if (pointer) patch({ pointer, phase: "unknown" });
      let available = false;
      try {
        const health = await call("/health", "GET");
        available = validPentaHealth(health) && exact(health as unknown as Record<string, unknown>, ["status", "provider", "protocol", "readOnly"]) && health.status === "Available";
      } catch { if (!current()) return; }
      patch({ available });
      // A stored receipt remains recoverable even while inference is unavailable.
      if (pointer) await recover();
      else patch({ phase: available ? "ready" : "unavailable" });
      patch({ notice: available ? "Private read-only evaluation. The model is not production-qualified."
        : "Private Mini is unavailable. Saved responses may still be checked; continue with guided reads or Manual." });
    }, true),
    start: () => run(async () => {
      if (!view.available) return;
      // Only an explicit separate-conversation action can abandon an old pointer.
      const saved = stored();
      if (!same(saved, view.pointer)) throw new StorageError();
      const value = await call("/conversations", "POST", {});
      if (!validPentaSession(value) || !id(value.conversationId) || !exact(value as unknown as Record<string, unknown>, ["conversationId", "version", "expiresAtUtc"]) ||
        !expires(value.expiresAtUtc) || Date.parse(value.expiresAtUtc) <= now()) throw Error("Unverified session.");
      if (!same(stored(), saved)) throw new StorageError();
      storage().removeItem(miniPointerKey);
      patch({ phase: "idle", session: value, pointer: null, receipt: null, notice: "" });
    }),
    send: async (input: string) => {
      let sent = false;
      await run(async () => {
      const session = view.session, text = input.trim();
      if (!view.available || view.phase !== "idle" || !session || !text || text.length > 2000 || /[\u0000-\u0008\u000b-\u001f\u007f-\u009f]/.test(text)) return;
      if (!version(session.version) || Date.parse(session.expiresAtUtc) <= now()) { patch({ phase: "closed", receipt: null, notice: "This conversation has expired or reached its turn limit. Start a separate conversation." }); return; }
      const pointer: MiniPointer = { epoch: channel.epoch, academyId: channel.academyId, conversationId: session.conversationId,
        requestId: uuid(), expectedVersion: session.version, expiresAtUtc: session.expiresAtUtc };
      if (!id(pointer.requestId)) throw Error("Invalid request identity.");
      persist(pointer); patch({ pointer, phase: "unknown", receipt: null });
      sent = true;
      const value = await call("/conversations/" + session.conversationId + "/turns", "POST",
        { requestId: pointer.requestId, expectedVersion: session.version, text, capability: "executor" });
      accept(value, pointer);
      });
      return sent;
    },
    recover: () => run(recover, true),
    dispose: () => { disposed = true; view = initialMiniView; },
  };
}
