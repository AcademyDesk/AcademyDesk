import {
  financeDraftHeader, financeObject, isExactMoney, isFinanceId, type FinanceDraft, type FinanceDraftChannel, type FinanceDraftKey,
} from "./penta-finance-draft";

export type ChoiceKind = "student" | "feePlan";
export type InvoiceChoice = { id: string; label: string; detail: string | null };
export type InvoiceChannel = FinanceDraftChannel & {
  options(kind: ChoiceKind, query: string): Promise<Response>;
};
type Plan = {
  studentId: string; studentName: string; studentNumber: string | null; feePlanId: string | null; feePlanName: string | null;
  amountExact: string; currency: string; issuedDate: string; dueDate: string;
};
type Receipt = { receiptId: string; auditId: string; invoiceId: string; invoiceNumber: string;
  amountExact: string; currency: string; issuedDate: string; dueDate: string };
export type InvoiceDraft = FinanceDraft<Plan, Receipt>;

const date = (value: unknown): value is string =>
  typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value) && Number.isFinite(Date.parse(value));
const label = (value: unknown): value is string => typeof value === "string" && value.trim().length > 0 && value.length <= 320;
const nullableLabel = (value: unknown): value is string | null => value === null || typeof value === "string" && value.length <= 320;

export function invoiceOptions(value: unknown, kind: ChoiceKind): { options: InvoiceChoice[]; hasMore: boolean } {
  if (!financeObject(value) || value.kind !== kind || typeof value.hasMore !== "boolean" ||
      !Array.isArray(value.options) || value.options.length > 20 ||
      !value.options.every(row => financeObject(row) && isFinanceId(row.id) && label(row.label) && nullableLabel(row.detail)))
    throw new Error("Invalid invoice options.");
  return { options: value.options, hasMore: value.hasMore };
}

export function invoicePayload(studentId: string, feePlanId: string | null, amount: string | null, dueDate: string) {
  return { contextVersion: 0, payload: {
    tool_key: "AcademyDesk.Invoice.Issue", tool_revision: 1, fields: {
      StudentId: { kind: "reference", value: studentId },
      DueDate: { kind: "date", value: dueDate },
      ...(feePlanId ? { FeePlanId: { kind: "reference", value: feePlanId } } : {}),
      ...(amount ? { Amount: { kind: "decimal", value: amount } } : {}),
    },
  } };
}

export function invoiceDraft(raw: unknown, key: FinanceDraftKey): InvoiceDraft {
  const value = financeDraftHeader(raw, key);
  const plan = value.plan as Record<string, unknown>;
  if (!isFinanceId(plan.studentId) || !label(plan.studentName) || !nullableLabel(plan.studentNumber) ||
      plan.studentActive !== true || (plan.feePlanId === null ? plan.feePlanName !== null :
        !isFinanceId(plan.feePlanId) || !label(plan.feePlanName)) ||
      !isExactMoney(plan.amountExact) || plan.amountExact === "0.00" ||
      typeof plan.currency !== "string" || !/^[A-Za-z]{3}$/.test(plan.currency) ||
      !date(plan.issuedDate) || !date(plan.dueDate)) throw new Error("Invalid invoice review.");
  if (value.status === "Prepared" && value.approvalId === null && value.receipt === null ||
      value.status === "Authorized" && isFinanceId(value.approvalId) && value.receipt === null) return value as InvoiceDraft;
  const receipt = value.receipt;
  if (value.status !== "Succeeded" || !isFinanceId(value.approvalId) || !financeObject(receipt) ||
      !isFinanceId(receipt.receiptId) || !isFinanceId(receipt.auditId) || !isFinanceId(receipt.invoiceId) ||
      !label(receipt.invoiceNumber) || receipt.invoiceNumber.length > 40 ||
      receipt.amountExact !== plan.amountExact || receipt.currency !== plan.currency ||
      receipt.issuedDate !== plan.issuedDate || receipt.dueDate !== plan.dueDate)
    throw new Error("Invalid invoice receipt.");
  return value as InvoiceDraft;
}

export function sameInvoiceReview(expected: InvoiceDraft, actual: InvoiceDraft): boolean {
  return expected.payloadHash === actual.payloadHash && expected.plan.studentId === actual.plan.studentId &&
    expected.plan.studentName === actual.plan.studentName && expected.plan.studentNumber === actual.plan.studentNumber &&
    expected.plan.feePlanId === actual.plan.feePlanId && expected.plan.feePlanName === actual.plan.feePlanName &&
    expected.plan.amountExact === actual.plan.amountExact && expected.plan.currency === actual.plan.currency &&
    expected.plan.issuedDate === actual.plan.issuedDate && expected.plan.dueDate === actual.plan.dueDate;
}
