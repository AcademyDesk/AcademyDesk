"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { normalizeFinanceAmount, type FinanceDraftChannel } from "@/lib/penta-finance-draft";
import {
  adjustmentDraft, adjustmentPayload, adjustmentTypes, isAdjustmentReason, isAdjustmentType, sameAdjustmentReview, type AdjustmentDraft,
} from "@/lib/penta-invoice-adjustment";
import { useFinanceDraft, type FinanceDraftOperation } from "./use-finance-draft";
import styles from "./role-penta-entry.module.css";

const operation: FinanceDraftOperation<AdjustmentDraft> = {
  noun: "adjustment proposal", manual: "Manual Finance Adjustments", storageKey: "academydesk.penta.adjustment-recovery",
  intent: "ApproveSubmitInvoiceAdjustment", parse: adjustmentDraft, same: sameAdjustmentReview,
  conflicts: {
    "A matching adjustment is already pending. Review it in Manual Finance Adjustments.": "A matching adjustment is already pending. Review it in Manual Finance Adjustments.",
    "The invoice balance changed after review. Open a fresh adjustment draft.": "The invoice balance changed after review. Review the current details again.",
  },
  unknownOutcome: "The adjustment outcome could not be verified. Check this draft before submitting another proposal.",
};

export default function InvoiceAdjustment({ academyId, invoice, channel, onClose }: {
  academyId: string; invoice: { id: string; title: string } | null; channel: FinanceDraftChannel; onClose(): void;
}) {
  const { request, pending, notice, setNotice, run, prepare, approve, execute, close: clear, check } =
    useFinanceDraft(academyId, channel, operation);
  const [type, setType] = useState("Discount");
  const [amount, setAmount] = useState("");
  const [reason, setReason] = useState("");
  const amountField = useRef<HTMLInputElement>(null);
  const editing = !!invoice && !request;

  useEffect(() => { if (editing) amountField.current?.focus(); }, [editing]);

  async function review() {
    const exact = normalizeFinanceAmount(amount.trim());
    const explanation = reason.trim();
    if (!invoice || !isAdjustmentType(type) || !exact || !isAdjustmentReason(explanation)) {
      setNotice("Enter a positive amount with up to two decimal places and a single-line reason of up to 500 characters.");
      return;
    }
    await prepare(adjustmentPayload(invoice.id, type, exact, explanation), draft =>
      draft.plan.invoiceId === invoice.id && draft.plan.type === type &&
      draft.plan.amountExact === exact && draft.plan.reason === explanation);
  }

  function close() {
    if (!clear()) return;
    setType("Discount");
    setAmount("");
    setReason("");
    onClose();
  }

  if (!invoice && !request && !notice) return null;
  const draft = request?.draft;
  return <section className={styles.financeTurn} aria-labelledby="invoice-adjustment-heading" aria-busy={pending}>
    <h3 id="invoice-adjustment-heading">Propose an invoice adjustment</h3>
    <p>Manual Finance approval is required to apply this reduction.</p>
    {editing && <form className={styles.financeComposer} onSubmit={event => { event.preventDefault(); void run(review); }}>
      <p><strong>{invoice.title}</strong></p>
      <label htmlFor="penta-adjustment-type">Adjustment type</label>
      <select id="penta-adjustment-type" className={styles.paymentField} value={type} disabled={pending}
        onChange={event => setType(event.currentTarget.value)}>
        {adjustmentTypes.map(value => <option key={value}>{value}</option>)}
      </select>
      <label htmlFor="penta-adjustment-amount">Reduction amount</label>
      <input ref={amountField} id="penta-adjustment-amount" className={styles.paymentField} inputMode="decimal" maxLength={19}
        value={amount} onChange={event => setAmount(event.currentTarget.value)} required disabled={pending} />
      <label htmlFor="penta-adjustment-reason">Reason for the reduction</label>
      <input id="penta-adjustment-reason" className={styles.paymentField} maxLength={500} autoComplete="off"
        value={reason} onChange={event => setReason(event.currentTarget.value)} required disabled={pending} />
      <div className={styles.financeActions}>
        <button type="submit" disabled={pending || !amount.trim() || !reason.trim()}>Review adjustment</button>
        <button type="button" disabled={pending} onClick={close}>Cancel</button>
      </div>
    </form>}
    {draft && <dl className={styles.paymentDetails}>
      <div><dt>Invoice</dt><dd>{draft.plan.invoiceNumber}</dd></div>
      <div><dt>Adjustment type</dt><dd>{draft.plan.type}</dd></div>
      <div><dt>Reduction</dt><dd>{draft.plan.currency} {draft.plan.amountExact}</dd></div>
      <div><dt>Balance at review</dt><dd>{draft.plan.currency} {draft.plan.currentBalanceExact}</dd></div>
      <div><dt>Balance if approved</dt><dd>{draft.plan.currency} {draft.plan.balanceIfApprovedExact}</dd></div>
      <div><dt>Reason</dt><dd className={styles.adjustmentReason}>{draft.plan.reason}</dd></div>
    </dl>}
    {pending && <p role="status">Checking adjustment status…</p>}
    {notice && <p role="status">{notice}</p>}
    <div className={styles.financeActions}>
      {draft?.status === "Prepared" && !request?.attempted && <>
        <button type="button" disabled={pending} onClick={() => void run(approve)}>Authorize submission</button>
        <button type="button" disabled={pending} onClick={close}>Cancel</button>
      </>}
      {draft?.status === "Authorized" && <>
        <button type="button" disabled={pending} onClick={() => void run(execute)}>
          {request?.attempted ? "Retry this authorized proposal" : "Submit for Manual approval"}
        </button>
        {!request?.attempted && <button type="button" disabled={pending} onClick={close}>Cancel</button>}
      </>}
      {request?.attempted && !request.expired && draft?.status !== "Succeeded" &&
        <button type="button" disabled={pending} onClick={() => void run(check)}>Check proposal status</button>}
      {request?.expired && <button type="button" disabled={pending} onClick={close}>Close after checking Manual Finance Adjustments</button>}
      {!invoice && !request && notice && <button type="button" disabled={pending} onClick={close}>Close</button>}
    </div>
    {draft?.status === "Succeeded" && <>
      <p role="status">Proposal submitted for Manual approval.</p>
      <p>Receipt ID <code>{draft.receipt.receiptId}</code></p>
      <div className={styles.financeActions}><button type="button" disabled={pending} onClick={close}>Done</button></div>
    </>}
    <p><Link href="/finance-adjustments">Open Manual Finance Adjustments</Link></p>
  </section>;
}
