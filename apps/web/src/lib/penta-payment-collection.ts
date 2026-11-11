import { readPentaJson } from "./penta-onboarding-channel";

export const paymentMethods = ["Cash", "UPI", "BankTransfer", "Card", "Cheque", "Offline"] as const;
export const approvalIntent = "ApproveRecordReceivedPayment";
type Method = typeof paymentMethods[number];
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
const exactMoney = /^(0|[1-9][0-9]{0,15})\.[0-9]{2}$/;
const id = (value: unknown): value is string => typeof value === "string" && uuid.test(value);
const object = (value: unknown): value is Record<string, unknown> =>
  value !== null && typeof value === "object" && !Array.isArray(value);
const timestamp = (value: unknown): value is string =>
  typeof value === "string" && value.endsWith("Z") && Number.isFinite(Date.parse(value));
const isMoney = (value: unknown): value is string => typeof value === "string" && exactMoney.test(value);
const cents = (value: string) => BigInt(value.replace(".", ""));
const zeroCents = BigInt(0);
export const isPaymentMethod = (value: unknown): value is Method =>
  paymentMethods.some(method => method === value);

export function normalizePaymentAmount(value: string): string | null {
  if (!/^(0|[1-9][0-9]{0,15})(\.[0-9]{1,2})?$/.test(value)) return null;
  const [units, fractional = ""] = value.split(".");
  const amount = units + "." + fractional.padEnd(2, "0");
  return cents(amount) > zeroCents ? amount : null;
}

export type PaymentDraftKey = { conversationId: string; requestId: string };
export type PaymentReview = {
  invoiceId: string; invoiceNumber: string; currency: string; method: Method;
  amountExact: string; balanceBeforeExact: string; balanceAfterExact: string;
};
type Receipt = { receiptId: string; auditId: string; paymentId: string; invoiceId: string;
  amountExact: string; currency: string; remainingBalanceExact: string; recordedAtUtc: string };
export type PaymentDraft = {
  requestId: string; revision: 1; contextVersion: 0; expiresAtUtc: string;
  payloadHash: string; plan: PaymentReview;
} & ({ status: "Prepared"; approvalId: null; receipt: null } |
     { status: "Authorized"; approvalId: string; receipt: null } |
     { status: "Succeeded"; approvalId: string; receipt: Receipt });

export function paymentDraftPath(key: PaymentDraftKey): string {
  return `/${key.conversationId}/drafts/${key.requestId}`;
}
export function paymentPayload(invoiceId: string, amount: string, method: Method) {
  return { contextVersion: 0, payload: {
    tool_key: "AcademyDesk.Payment.Record", tool_revision: 1, fields: {
      InvoiceId: { kind: "reference", value: invoiceId },
      Amount: { kind: "decimal", value: amount },
      Method: { kind: "text", value: method },
    },
  } };
}

export async function paymentJson(response: Response, signal: AbortSignal) {
  if (!response.headers.get("cache-control")?.includes("no-store")) throw new Error("Payment response can be cached.");
  return readPentaJson(response, signal);
}

export function paymentConversation(value: unknown): string {
  if (!object(value) || !id(value.conversationId) || value.version !== 0 || !timestamp(value.expiresAtUtc))
    throw new Error("Invalid payment conversation.");
  return value.conversationId;
}

export function paymentDraft(value: unknown, key: PaymentDraftKey): PaymentDraft {
  if (!object(value) || value.requestId !== key.requestId || value.revision !== 1 || value.contextVersion !== 0 ||
      typeof value.payloadHash !== "string" || !/^[0-9a-f]{64}$/.test(value.payloadHash) || !timestamp(value.expiresAtUtc) ||
      !object(value.plan)) throw new Error("Invalid payment draft.");
  const plan = value.plan;
  if (!id(plan.invoiceId) || typeof plan.invoiceNumber !== "string" || !plan.invoiceNumber.trim() ||
      plan.invoiceNumber.length > 40 || typeof plan.currency !== "string" || !/^[A-Za-z]{3}$/.test(plan.currency) ||
      !isPaymentMethod(plan.method) || !isMoney(plan.amountExact) || !isMoney(plan.balanceBeforeExact) ||
      !isMoney(plan.balanceAfterExact) || cents(plan.amountExact) <= zeroCents ||
      cents(plan.balanceBeforeExact) - cents(plan.amountExact) !== cents(plan.balanceAfterExact) ||
      plan.reference !== null) throw new Error("Invalid payment review.");
  if (value.status === "Prepared" && value.approvalId === null && value.receipt === null ||
      value.status === "Authorized" && id(value.approvalId) && value.receipt === null) return value as PaymentDraft;
  const receipt = value.receipt;
  if (value.status !== "Succeeded" || !id(value.approvalId) || !object(receipt) ||
      !id(receipt.receiptId) || !id(receipt.auditId) || !id(receipt.paymentId) || receipt.invoiceId !== plan.invoiceId ||
      receipt.currency !== plan.currency || receipt.amountExact !== plan.amountExact ||
      receipt.remainingBalanceExact !== plan.balanceAfterExact || !timestamp(receipt.recordedAtUtc))
    throw new Error("Invalid payment receipt.");
  return value as PaymentDraft;
}

export function samePaymentReview(expected: PaymentDraft, actual: PaymentDraft): boolean {
  return expected.payloadHash === actual.payloadHash &&
    expected.plan.invoiceId === actual.plan.invoiceId && expected.plan.invoiceNumber === actual.plan.invoiceNumber &&
    expected.plan.currency === actual.plan.currency && expected.plan.method === actual.plan.method &&
    expected.plan.amountExact === actual.plan.amountExact && expected.plan.balanceBeforeExact === actual.plan.balanceBeforeExact &&
    expected.plan.balanceAfterExact === actual.plan.balanceAfterExact;
}

export type PaymentChannel = {
  epoch: string | null; signal: AbortSignal; current(): boolean;
  payment(path: string, method: "GET" | "PUT" | "POST", body?: unknown): Promise<Response>;
};
export const paymentRecoveryKey = "academydesk.penta.payment-recovery";

export function readPaymentRecovery(storage: Storage, academyId: string, epoch: string | null): PaymentDraftKey | null {
  const saved = storage.getItem(paymentRecoveryKey);
  if (!saved) return null;
  try {
    const value: unknown = JSON.parse(saved);
    if (epoch && object(value) && value.academyId === academyId && value.epoch === epoch &&
        id(value.conversationId) && id(value.requestId)) return { conversationId: value.conversationId, requestId: value.requestId };
  } catch { /* Discard an invalid recovery pointer. */ }
  storage.removeItem(paymentRecoveryKey);
  return null;
}

export function savePaymentRecovery(storage: Storage, academyId: string, epoch: string | null, key: PaymentDraftKey) {
  if (!epoch) throw new Error("Payment recovery requires a current browser session.");
  storage.setItem(paymentRecoveryKey, JSON.stringify({ academyId, epoch, ...key }));
}
