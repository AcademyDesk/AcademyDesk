import { readPentaJson } from "./penta-onboarding-channel";

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
export const isFinanceId = (value: unknown): value is string => typeof value === "string" && uuid.test(value);
export const financeObject = (value: unknown): value is Record<string, unknown> =>
  value !== null && typeof value === "object" && !Array.isArray(value);
export const financeTimestamp = (value: unknown): value is string =>
  typeof value === "string" && value.endsWith("Z") && Number.isFinite(Date.parse(value));
export const isExactMoney = (value: unknown): value is string =>
  typeof value === "string" && /^(0|[1-9][0-9]{0,15})\.[0-9]{2}$/.test(value);

export function normalizeFinanceAmount(value: string): string | null {
  if (!/^(0|[1-9][0-9]{0,15})(\.[0-9]{1,2})?$/.test(value)) return null;
  const [units, fractional = ""] = value.split(".");
  const amount = units + "." + fractional.padEnd(2, "0");
  return amount === "0.00" ? null : amount;
}

export type FinanceDraftKey = { conversationId: string; requestId: string };
export type FinanceDraft<Plan, Receipt> = {
  requestId: string; revision: 1; contextVersion: 0; expiresAtUtc: string; payloadHash: string; plan: Plan;
} & ({ status: "Prepared"; approvalId: null; receipt: null } |
     { status: "Authorized"; approvalId: string; receipt: null } |
     { status: "Succeeded"; approvalId: string; receipt: Receipt });

export type FinanceDraftChannel = {
  epoch: string | null; signal: AbortSignal; current(): boolean;
  send(path: string, method: "GET" | "PUT" | "POST", body?: unknown): Promise<Response>;
};

export function financeDraftPath(key: FinanceDraftKey): string {
  return `/${key.conversationId}/drafts/${key.requestId}`;
}

export function financeDraftHeader(value: unknown, key: FinanceDraftKey): Record<string, unknown> {
  if (!financeObject(value) || value.requestId !== key.requestId || value.revision !== 1 || value.contextVersion !== 0 ||
      typeof value.payloadHash !== "string" || !/^[0-9a-f]{64}$/.test(value.payloadHash) ||
      !financeTimestamp(value.expiresAtUtc) || !financeObject(value.plan)) throw new Error("Invalid finance draft.");
  return value;
}

export async function financeDraftJson(response: Response, signal: AbortSignal) {
  if (!response.headers.get("cache-control")?.includes("no-store")) throw new Error("Finance response can be cached.");
  return readPentaJson(response, signal);
}

export function financeConversation(value: unknown): string {
  if (!financeObject(value) || !isFinanceId(value.conversationId) || value.version !== 0 || !financeTimestamp(value.expiresAtUtc))
    throw new Error("Invalid finance conversation.");
  return value.conversationId;
}

export function readFinanceRecovery(storage: Storage, storageKey: string, academyId: string, epoch: string | null): FinanceDraftKey | null {
  const saved = storage.getItem(storageKey);
  if (!saved) return null;
  try {
    const value: unknown = JSON.parse(saved);
    if (epoch && financeObject(value) && value.academyId === academyId && value.epoch === epoch &&
        isFinanceId(value.conversationId) && isFinanceId(value.requestId))
      return { conversationId: value.conversationId, requestId: value.requestId };
  } catch { /* Discard an invalid recovery pointer. */ }
  storage.removeItem(storageKey);
  return null;
}

export function saveFinanceRecovery(storage: Storage, storageKey: string, academyId: string, epoch: string | null, key: FinanceDraftKey) {
  if (!epoch) throw new Error("Finance recovery requires a current browser session.");
  storage.setItem(storageKey, JSON.stringify({ academyId, epoch, ...key }));
}
