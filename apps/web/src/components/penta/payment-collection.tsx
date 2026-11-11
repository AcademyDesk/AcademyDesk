"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import {
  approvalIntent, isPaymentMethod, normalizePaymentAmount, paymentConversation, paymentDraft, paymentDraftPath,
  paymentJson, paymentMethods, paymentPayload, paymentRecoveryKey, readPaymentRecovery, samePaymentReview,
  savePaymentRecovery, type PaymentChannel, type PaymentDraft, type PaymentDraftKey,
} from "@/lib/penta-payment-collection";
import styles from "./role-penta-entry.module.css";

type RequestState = {
  key: PaymentDraftKey;
  draft?: PaymentDraft;
  expected?: PaymentDraft;
  attempted: boolean;
  expired?: boolean;
};
class PaymentHttpError extends Error {
  constructor(readonly status: number, readonly invoiceChanged = false) {
    super("Payment request could not be verified.");
  }
}

export default function PaymentCollection({ academyId, invoiceId, channel, onClose }: {
  academyId: string; invoiceId: string | null; channel: PaymentChannel; onClose(): void;
}) {
  const [request, setRequest] = useState<RequestState | null>(null);
  const [amount, setAmount] = useState("");
  const [method, setMethod] = useState("Cash");
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState("");
  const busy = useRef(false);
  const amountField = useRef<HTMLInputElement>(null);

  const read = useCallback(async (path: string, verb: "GET" | "PUT" | "POST", body?: unknown) => {
    const response = await channel.payment(path, verb, body);
    if (!channel.current()) throw new Error("Current browser session changed.");
    if (response.status !== 200 && response.status !== 201) {
      if (response.status === 409) {
        const problem = await paymentJson(response, channel.signal).catch(() => null);
        throw new PaymentHttpError(409, problem !== null && typeof problem === "object" &&
          "message" in problem && problem.message === "The invoice changed after review. Open a fresh payment draft.");
      }
      await response.body?.cancel().catch(() => {});
      throw new PaymentHttpError(response.status);
    }
    const value = await paymentJson(response, channel.signal);
    if (!channel.current()) throw new Error("Current browser session changed.");
    return value;
  }, [channel]);

  const recover = useCallback(async (key: PaymentDraftKey, expected?: PaymentDraft) => {
    setRequest({ key, expected, attempted: true });
    try {
      const value = paymentDraft(await read(paymentDraftPath(key), "GET"), key);
      if (expected && (!samePaymentReview(expected, value) || expected.approvalId !== value.approvalId))
        throw new Error("Payment recovery returned a different draft.");
      if (channel.current()) {
        setRequest({ key, draft: value, attempted: true });
        if (value.status === "Authorized")
          setNotice("No receipt is available yet. Check again or retry this same approved payment.");
        else if (value.status === "Prepared")
          setNotice("Approval could not be verified. Check this draft or open Manual Payments.");
      }
    } catch (error) {
      if (!channel.current()) return;
      if (error instanceof PaymentHttpError && error.status === 410) {
        setRequest({ key, attempted: true, expired: true });
        setNotice("This payment draft expired. Verify the payment in Manual Payments.");
      } else setNotice("PENTA could not verify the receipt. Check this draft or open Manual Payments.");
    }
  }, [channel, read]);

  async function run(action: () => Promise<void>) {
    if (busy.current || !channel.current()) return;
    busy.current = true;
    setPending(true);
    setNotice("");
    try { await action(); }
    catch (error) {
      if (!channel.current()) return;
      if (error instanceof PaymentHttpError && (error.status === 401 || error.status === 403)) {
        setRequest(null);
        onClose();
        return;
      }
      const message = error instanceof PaymentHttpError && error.status === 410
        ? "This payment draft expired. Check Manual Payments for its current status."
        : "PENTA could not verify this step. Check the invoice or open Manual Payments.";
      setNotice(message);
    } finally {
      busy.current = false;
      if (channel.current()) setPending(false);
    }
  }

  useEffect(() => {
    let mounted = true;
    void Promise.resolve().then(() => {
      if (!mounted || !channel.current()) return;
      let key: PaymentDraftKey | null;
      try { key = readPaymentRecovery(window.sessionStorage, academyId, channel.epoch); }
      catch { return; }
      if (!key || busy.current) return;
      busy.current = true;
      setPending(true);
      void recover(key).finally(() => {
        busy.current = false;
        if (channel.current()) setPending(false);
      });
    });
    return () => { mounted = false; };
  }, [academyId, channel, recover]);

  useEffect(() => {
    if (invoiceId && !request) amountField.current?.focus();
  }, [invoiceId, request]);

  async function prepare() {
    const exact = normalizePaymentAmount(amount.trim());
    if (!invoiceId || !exact || !isPaymentMethod(method)) {
      setNotice("Enter a positive amount with up to two decimal places and select a payment method.");
      return;
    }
    const conversationId = paymentConversation(await read("/" + crypto.randomUUID(), "PUT"));
    const key = { conversationId, requestId: crypto.randomUUID() };
    const draft = paymentDraft(await read(paymentDraftPath(key), "PUT", paymentPayload(invoiceId, exact, method)), key);
    if (draft.status !== "Prepared" || draft.plan.invoiceId !== invoiceId ||
        draft.plan.amountExact !== exact || draft.plan.method !== method) throw new Error("Reviewed payment differs from the request.");
    setRequest({ key, draft, attempted: false });
    setAmount("");
  }

  async function approve(key: PaymentDraftKey, reviewed: PaymentDraft) {
    const result = paymentDraft(await read(paymentDraftPath(key) + "/approve", "POST", {
      revision: reviewed.revision, payloadHash: reviewed.payloadHash, intent: approvalIntent,
    }), key);
    if (result.status !== "Authorized" || !samePaymentReview(reviewed, result)) throw new Error("Approval differs from the review.");
    setRequest({ key, draft: result, attempted: false });
  }

  async function execute(key: PaymentDraftKey, approved: Extract<PaymentDraft, { status: "Authorized" }>) {
    try { savePaymentRecovery(window.sessionStorage, academyId, channel.epoch, key); }
    catch {
      setNotice("Receipt recovery is unavailable in this tab. Open Manual Payments to record the payment.");
      return;
    }
    setRequest({ key, expected: approved, attempted: true });
    try {
      const result = paymentDraft(await read(paymentDraftPath(key) + "/execute", "POST", {
        revision: approved.revision, payloadHash: approved.payloadHash, approvalId: approved.approvalId,
      }), key);
      if (result.status !== "Succeeded" || !samePaymentReview(approved, result) || result.approvalId !== approved.approvalId)
        throw new Error("Receipt does not match the approved payment.");
      setRequest({ key, draft: result, attempted: true });
    } catch (error) {
      if (!channel.current()) return;
      if (error instanceof PaymentHttpError && error.invoiceChanged) {
        try {
          window.sessionStorage.removeItem(paymentRecoveryKey);
          setRequest(null);
          setNotice("The invoice changed after review. Enter the payment again for a fresh review.");
          return;
        } catch { /* Keep recovery available if the pointer cannot be cleared. */ }
      }
      setNotice("The payment outcome could not be verified. Check this draft before recording another payment.");
    }
  }

  function close() {
    if (request?.attempted) {
      try { window.sessionStorage.removeItem(paymentRecoveryKey); }
      catch {
        setNotice("This tab could not clear its receipt recovery details.");
        return;
      }
    }
    setRequest(null);
    setAmount("");
    setNotice("");
    onClose();
  }

  if (!invoiceId && !request) return null;
  const draft = request?.draft;
  const plan = draft?.plan;
  return <section className={styles.financeTurn} aria-labelledby="payment-collection-heading" aria-busy={pending}>
    <h3 id="payment-collection-heading">Record a received payment</h3>
    {!request && <form className={styles.financeComposer} onSubmit={event => { event.preventDefault(); void run(prepare); }}>
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
    {pending && <p role="status">Checking payment status…</p>}
    {notice && <p role="status">{notice}</p>}
    {request && <div className={styles.financeActions}>
      {draft?.status === "Prepared" && !request.attempted && <>
        <button type="button" disabled={pending} onClick={() => void run(() => approve(request.key, draft))}>Approve exact payment</button>
        <button type="button" disabled={pending} onClick={close}>Cancel</button>
      </>}
      {draft?.status === "Authorized" && <>
        <button type="button" disabled={pending} onClick={() => void run(() => execute(request.key, draft))}>
          {request.attempted ? "Retry this approved payment" : "Record received payment"}
        </button>
        {!request.attempted && <button type="button" disabled={pending} onClick={close}>Cancel</button>}
      </>}
      {request.attempted && !request.expired && draft?.status !== "Succeeded" &&
        <button type="button" disabled={pending} onClick={() => void run(() => recover(request.key, draft ?? request.expected))}>Check payment status</button>}
      {request.expired && <button type="button" disabled={pending} onClick={close}>Close after checking Manual Payments</button>}
    </div>}
    {draft?.status === "Succeeded" && <>
      <p role="status">Payment recorded. <strong>Receipt ID</strong> <code>{draft.receipt.receiptId}</code></p>
      <div className={styles.financeActions}><button type="button" disabled={pending} onClick={close}>Done</button></div>
    </>}
    <p><Link href="/payments">Open Manual Payments</Link></p>
  </section>;
}
