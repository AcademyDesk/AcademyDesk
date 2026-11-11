import {
  financeDraftHeader, financeObject as object, financeTimestamp as timestamp, isExactMoney as isMoney, isFinanceId as id,
  type FinanceDraft, type FinanceDraftKey,
} from "./penta-finance-draft";

export const paymentMethods = ["Cash", "UPI", "BankTransfer", "Card", "Cheque", "Offline"] as const;
type Method = typeof paymentMethods[number];
const cents = (value: string) => BigInt(value.replace(".", ""));
export const isPaymentMethod = (value: unknown): value is Method => paymentMethods.some(method => method === value);

export type PaymentReview = {
  invoiceId: string; invoiceNumber: string; currency: string; method: Method;
  amountExact: string; balanceBeforeExact: string; balanceAfterExact: string;
};
type Receipt = { receiptId: string; auditId: string; paymentId: string; invoiceId: string;
  amountExact: string; currency: string; remainingBalanceExact: string; recordedAtUtc: string };
export type PaymentDraft = FinanceDraft<PaymentReview, Receipt>;

export function paymentPayload(invoiceId: string, amount: string, method: Method) {
  return { contextVersion: 0, payload: {
    tool_key: "AcademyDesk.Payment.Record", tool_revision: 1, fields: {
      InvoiceId: { kind: "reference", value: invoiceId },
      Amount: { kind: "decimal", value: amount },
      Method: { kind: "text", value: method },
    },
  } };
}

export function paymentDraft(raw: unknown, key: FinanceDraftKey): PaymentDraft {
  const value = financeDraftHeader(raw, key);
  const plan = value.plan as Record<string, unknown>;
  if (!id(plan.invoiceId) || typeof plan.invoiceNumber !== "string" || !plan.invoiceNumber.trim() ||
      plan.invoiceNumber.length > 40 || typeof plan.currency !== "string" || !/^[A-Za-z]{3}$/.test(plan.currency) ||
      !isPaymentMethod(plan.method) || !isMoney(plan.amountExact) || !isMoney(plan.balanceBeforeExact) ||
      !isMoney(plan.balanceAfterExact) || plan.amountExact === "0.00" ||
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
