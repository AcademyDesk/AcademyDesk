import {
  financeDraftHeader, financeObject, isExactMoney, isFinanceId, type FinanceDraft, type FinanceDraftKey,
} from "./penta-finance-draft";

export const adjustmentTypes = ["Discount", "Scholarship", "Concession"] as const;
type AdjustmentType = typeof adjustmentTypes[number];
export function isAdjustmentType(value: unknown): value is AdjustmentType {
  return adjustmentTypes.some(type => type === value);
}

type Plan = {
  invoiceId: string; invoiceNumber: string; invoiceStatus: string; type: AdjustmentType;
  amountExact: string; currency: string; reason: string; currentBalanceExact: string; balanceIfApprovedExact: string;
};
type Receipt = { receiptId: string; auditId: string; adjustmentId: string; invoiceId: string;
  amountExact: string; currency: string; statusAtSubmission: "PendingApproval" };
export type AdjustmentDraft = FinanceDraft<Plan, Receipt>;

export function isAdjustmentReason(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0 && value.length <= 500 && !/\p{Cc}/u.test(value);
}

export function adjustmentPayload(invoiceId: string, type: AdjustmentType, amount: string, reason: string) {
  return { contextVersion: 0, payload: {
    tool_key: "AcademyDesk.InvoiceAdjustment.Propose", tool_revision: 1, fields: {
      InvoiceId: { kind: "reference", value: invoiceId },
      Type: { kind: "text", value: type },
      Amount: { kind: "decimal", value: amount },
      Reason: { kind: "text", value: reason },
    },
  } };
}

export function adjustmentDraft(raw: unknown, key: FinanceDraftKey): AdjustmentDraft {
  const value = financeDraftHeader(raw, key);
  const plan = value.plan as Record<string, unknown>;
  if (!isFinanceId(plan.invoiceId) || typeof plan.invoiceNumber !== "string" ||
      plan.invoiceNumber.trim().length === 0 || plan.invoiceNumber.length > 40 ||
      typeof plan.invoiceStatus !== "string" || !isAdjustmentType(plan.type) || !isAdjustmentReason(plan.reason) ||
      typeof plan.currency !== "string" || !/^[A-Za-z]{3}$/.test(plan.currency) ||
      !isExactMoney(plan.amountExact) || plan.amountExact === "0.00" ||
      !isExactMoney(plan.currentBalanceExact) || !isExactMoney(plan.balanceIfApprovedExact))
    throw new Error("Invalid adjustment review.");
  const cents = (amount: string) => BigInt(amount.replace(".", ""));
  if (cents(plan.currentBalanceExact) - cents(plan.amountExact) !== cents(plan.balanceIfApprovedExact))
    throw new Error("Invalid adjustment balance.");

  if (value.status === "Prepared" && value.approvalId === null && value.receipt === null ||
      value.status === "Authorized" && isFinanceId(value.approvalId) && value.receipt === null) return value as AdjustmentDraft;
  const receipt = value.receipt;
  if (value.status !== "Succeeded" || !isFinanceId(value.approvalId) || !financeObject(receipt) ||
      !isFinanceId(receipt.receiptId) || !isFinanceId(receipt.auditId) || !isFinanceId(receipt.adjustmentId) ||
      receipt.invoiceId !== plan.invoiceId || receipt.amountExact !== plan.amountExact ||
      receipt.currency !== plan.currency || receipt.statusAtSubmission !== "PendingApproval")
    throw new Error("Invalid adjustment receipt.");
  return value as AdjustmentDraft;
}

export function sameAdjustmentReview(expected: AdjustmentDraft, actual: AdjustmentDraft): boolean {
  const left = expected.plan;
  const right = actual.plan;
  return expected.payloadHash === actual.payloadHash &&
    left.invoiceId === right.invoiceId && left.invoiceNumber === right.invoiceNumber &&
    left.invoiceStatus === right.invoiceStatus && left.type === right.type && left.amountExact === right.amountExact &&
    left.currency === right.currency && left.reason === right.reason && left.currentBalanceExact === right.currentBalanceExact &&
    left.balanceIfApprovedExact === right.balanceIfApprovedExact;
}
