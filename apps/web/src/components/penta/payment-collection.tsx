"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { isPaymentMethod, paymentDraft, paymentMethods, paymentPayload, samePaymentReview, type PaymentDraft } from "@/lib/penta-payment-collection";
import { normalizeFinanceAmount, type FinanceDraftChannel } from "@/lib/penta-finance-draft";
import { useFinanceDraft, type FinanceDraftOperation } from "./use-finance-draft";
import styles from "./role-penta-entry.module.css";

const operation: FinanceDraftOperation<PaymentDraft> = {
  noun: "payment", manual: "Manual Payments", storageKey: "academydesk.penta.payment-recovery",
  intent: "ApproveRecordReceivedPayment", parse: paymentDraft, same: samePaymentReview,
  conflicts: { "The invoice changed after review. Open a fresh payment draft.": "The invoice changed after review. Enter the payment again for a fresh review." },
  unknownOutcome: "The payment outcome could not be verified. Check this draft before recording another payment.",
};

export default function PaymentCollection({ academyId, invoiceId, channel, onClose }: {
  academyId: string; invoiceId: string | null; channel: FinanceDraftChannel; onClose(): void;
}) {
  const { request, pending, notice, setNotice, run, prepare, approve, execute, close: clear, check } =
    useFinanceDraft(academyId, channel, operation);
  const [amount, setAmount] = useState("");
  const [method, setMethod] = useState("Cash");
  const amountField = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (invoiceId && !request) amountField.current?.focus();
  }, [invoiceId, request]);

  async function review() {
    const exact = normalizeFinanceAmount(amount.trim());
    if (!invoiceId || !exact || !isPaymentMethod(method)) {
      setNotice("Enter a positive amount with up to two decimal places and select a payment method.");
      return;
    }
    await prepare(paymentPayload(invoiceId, exact, method), draft =>
      draft.plan.invoiceId === invoiceId && draft.plan.amountExact === exact && draft.plan.method === method);
    setAmount("");
  }

  function close() {
    if (!clear()) return;
    setAmount("");
    onClose();
  }

  if (!invoiceId && !request) return null;
  const draft = request?.draft;
  const plan = draft?.plan;
  return <section className={styles.financeTurn} aria-labelledby="payment-collection-heading" aria-busy={pending}>
    <h3 id="payment-collection-heading">Record a received payment</h3>
    {!request && <form className={styles.financeComposer} onSubmit={event => { event.preventDefault(); void run(review); }}>
      <label htmlFor="penta-payment-amount">Payment amount</label>
      <input ref={amountField} id="penta-payment-amount" className={styles.paymentField} inputMode="decimal" maxLength={19} required
        value={amount} onChange={event => setAmount(event.currentTarget.value)} disabled={pending} />
      <label htmlFor="penta-payment-method">Payment method</label>
      <select id="penta-payment-method" className={styles.paymentField} value={method}
        onChange={event => setMethod(event.currentTarget.value)} disabled={pending}>
        {paymentMethods.map(value => <option key={value}>{value}</option>)}
      </select>
      <div className={styles.financeActions}>
        <button type="submit" disabled={pending || !amount.trim()}>Review payment</button>
        <button type="button" disabled={pending} onClick={close}>Cancel</button>
      </div>
    </form>}
    {plan && <dl className={styles.paymentDetails}>
      <div><dt>Invoice</dt><dd>{plan.invoiceNumber}</dd></div>
      <div><dt>Amount received</dt><dd>{plan.currency} {plan.amountExact}</dd></div>
      <div><dt>Method</dt><dd>{plan.method}</dd></div>
      <div><dt>Balance before payment</dt><dd>{plan.currency} {plan.balanceBeforeExact}</dd></div>
      <div><dt>Balance after payment</dt><dd>{plan.currency} {plan.balanceAfterExact}</dd></div>
    </dl>}
    {pending && <p role="status">Checking payment statusâ€¦</p>}
    {notice && <p role="status">{notice}</p>}
    {request && <div className={styles.financeActions}>
      {draft?.status === "Prepared" && !request.attempted && <>
        <button type="button" disabled={pending} onClick={() => void run(approve)}>Approve exact payment</button>
        <button type="button" disabled={pending} onClick={close}>Cancel</button>
      </>}
      {draft?.status === "Authorized" && <>
        <button type="button" disabled={pending} onClick={() => void run(execute)}>
          {request.attempted ? "Retry this approved payment" : "Record received payment"}
        </button>
        {!request.attempted && <button type="button" disabled={pending} onClick={close}>Cancel</button>}
      </>}
      {request.attempted && !request.expired && draft?.status !== "Succeeded" &&
        <button type="button" disabled={pending} onClick={() => void run(check)}>Check payment status</button>}
      {request.expired && <button type="button" disabled={pending} onClick={close}>Close after checking Manual Payments</button>}
    </div>}
    {draft?.status === "Succeeded" && <>
      <p role="status">Payment recorded. <strong>Receipt ID</strong> <code>{draft.receipt.receiptId}</code></p>
      <div className={styles.financeActions}><button type="button" disabled={pending} onClick={close}>Done</button></div>
    </>}
    <p><Link href="/payments">Open Manual Payments</Link></p>
  </section>;
}
