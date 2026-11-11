"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { financeDraftJson, normalizeFinanceAmount } from "@/lib/penta-finance-draft";
import {
  invoiceDraft, invoiceOptions, invoicePayload, sameInvoiceReview, type ChoiceKind, type InvoiceChannel, type InvoiceChoice, type InvoiceDraft,
} from "@/lib/penta-invoice-issuance";
import { useFinanceDraft, type FinanceDraftOperation } from "./use-finance-draft";
import styles from "./role-penta-entry.module.css";

const operation: FinanceDraftOperation<InvoiceDraft> = {
  noun: "invoice", manual: "Manual Invoices", storageKey: "academydesk.penta.invoice-recovery",
  intent: "ApproveIssueInvoice", parse: invoiceDraft, same: sameInvoiceReview,
  conflicts: {
    "A matching invoice already exists. Review it in Manual Invoices.": "A matching invoice already exists. Review it in Manual Invoices.",
    "The invoice terms changed after review. Open a fresh invoice draft.": "The invoice terms changed after review. Review the current details again.",
  },
  unknownOutcome: "The invoice outcome could not be verified. Check this draft before issuing another invoice.",
};

function InvoiceChoices({ kind, channel, selected, select, disabled }: {
  kind: ChoiceKind; channel: InvoiceChannel; selected: InvoiceChoice | null; select(option: InvoiceChoice | null): void; disabled: boolean;
}) {
  const [query, setQuery] = useState("");
  const [result, setResult] = useState<{ options: InvoiceChoice[]; hasMore: boolean } | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const searchInput = useRef<HTMLInputElement>(null);
  const lock = useRef(false);
  const name = kind === "student" ? "student" : "fee plan";
  const searchId = "invoice-option-" + kind;
  useEffect(() => { if (kind === "student") searchInput.current?.focus(); }, [kind]);

  async function search() {
    if (disabled || lock.current || !channel.current()) return;
    const term = query.trim();
    if (kind === "student" && term.length < 2) {
      setError("Enter at least two characters of a name or student number.");
      return;
    }
    lock.current = true;
    setBusy(true);
    setError("");
    setResult(null);
    try {
      const response = await channel.options(kind, term);
      if (!response.ok) {
        await response.body?.cancel().catch(() => {});
        throw new Error("Invoice lookup unavailable.");
      }
      const options = invoiceOptions(await financeDraftJson(response, channel.signal), kind);
      if (channel.current()) setResult(options);
    } catch {
      if (channel.current()) setError(`The ${name} lookup could not be verified. Try again or open Manual Invoices.`);
    } finally {
      lock.current = false;
      if (channel.current()) setBusy(false);
    }
  }

  if (selected) return <p>
    <strong>{kind === "student" ? "Student" : "Fee plan"}</strong> {selected.label}{selected.detail && ` · ${selected.detail}`}{" "}
    <button type="button" disabled={disabled} className={styles.retry} onClick={() => select(null)}>Change {name}</button>
  </p>;

  return <div className={styles.invoiceChoices} aria-busy={busy}>
    <label htmlFor={searchId}>{kind === "student" ? "Student name or number" : "Fee plan name"}</label>
    <div className={styles.financeActions}>
      <input ref={searchInput} id={searchId} value={query} maxLength={80} disabled={disabled || busy}
        onChange={event => { setQuery(event.currentTarget.value); setResult(null); }}
        onKeyDown={event => { if (event.key === "Enter") { event.preventDefault(); void search(); } }} />
      <button type="button" disabled={disabled || busy} onClick={() => void search()}>{kind === "student" ? "Find students" : "Find fee plans"}</button>
    </div>
    {busy && <p role="status">Searching academy records…</p>}
    {error && <p role="status">{error}</p>}
    {result && (result.options.length === 0 ? <p role="status">No matching {kind === "student" ? "students" : "fee plans"}.</p> :
      <>
        <ul className={styles.invoiceOptions} aria-label={kind === "student" ? "Matching students" : "Matching fee plans"}>
          {result.options.map(option => <li key={option.id}>
            <button type="button" disabled={disabled} onClick={() => select(option)}>
              {option.label}{option.detail && ` · ${option.detail}`}
            </button>
          </li>)}
        </ul>
        {result.hasMore && <p role="status">More matches found. Refine your search.</p>}
      </>)}
  </div>;
}

export default function InvoiceIssuance({ academyId, open, channel, onClose }: {
  academyId: string; open: boolean; channel: InvoiceChannel; onClose(): void;
}) {
  const { request, pending, notice, setNotice, run, prepare, approve, execute, close: clear, check } =
    useFinanceDraft(academyId, channel, operation);
  const [student, setStudent] = useState<InvoiceChoice | null>(null);
  const [plan, setPlan] = useState<InvoiceChoice | null>(null);
  const [billing, setBilling] = useState("oneTime");
  const [amount, setAmount] = useState("");
  const [due, setDue] = useState("");
  const usePlan = billing === "feePlan";

  async function review() {
    const exact = normalizeFinanceAmount(amount.trim());
    const feePlan = usePlan ? plan : null;
    if (!student || !due || usePlan && !feePlan || (!usePlan || amount.trim()) && !exact) {
      setNotice("Select a student, enter a due date and provide a fee plan or a positive amount with up to two decimal places.");
      return;
    }
    await prepare(invoicePayload(student.id, feePlan?.id ?? null, exact, due), draft =>
      draft.plan.studentId === student.id && draft.plan.feePlanId === (feePlan?.id ?? null) &&
      draft.plan.dueDate === due && (exact === null || draft.plan.amountExact === exact));
  }

  function close() {
    if (!clear()) return;
    setStudent(null);
    setPlan(null);
    setBilling("oneTime");
    setAmount("");
    setDue("");
    onClose();
  }

  if (!open && !request && !notice) return null;
  const draft = request?.draft;
  return <section className={styles.financeTurn} aria-labelledby="invoice-issuance-heading" aria-busy={pending}>
    <h3 id="invoice-issuance-heading">Issue an invoice</h3>
    {open && !request && <form className={styles.financeComposer} onSubmit={event => { event.preventDefault(); void run(review); }}>
      <InvoiceChoices kind="student" channel={channel} selected={student} select={setStudent} disabled={pending} />
      <label htmlFor="penta-invoice-billing">Billing type</label>
      <select id="penta-invoice-billing" className={styles.paymentField} value={billing} disabled={pending}
        onChange={event => { setBilling(event.currentTarget.value); setPlan(null); setAmount(""); }}>
        <option value="oneTime">One-time amount</option>
        <option value="feePlan">Fee plan</option>
      </select>
      {usePlan && <InvoiceChoices kind="feePlan" channel={channel} selected={plan} select={setPlan} disabled={pending} />}
      <label htmlFor="penta-invoice-amount">{usePlan ? "Amount override (optional)" : "Invoice amount"}</label>
      <input id="penta-invoice-amount" className={styles.paymentField} inputMode="decimal" maxLength={19} required={!usePlan}
        value={amount} onChange={event => setAmount(event.currentTarget.value)} disabled={pending} />
      <label htmlFor="penta-invoice-due">Due date</label>
      <input id="penta-invoice-due" className={styles.paymentField} type="date" value={due} required disabled={pending}
        onChange={event => setDue(event.currentTarget.value)} />
      <div className={styles.financeActions}>
        <button type="submit" disabled={pending || !student || usePlan && !plan}>Review invoice</button>
        <button type="button" disabled={pending} onClick={close}>Cancel</button>
      </div>
    </form>}
    {draft && <dl className={styles.paymentDetails}>
      <div><dt>Student</dt><dd>{draft.plan.studentName}{draft.plan.studentNumber && ` · ${draft.plan.studentNumber}`}</dd></div>
      <div><dt>Billing</dt><dd>{draft.plan.feePlanName ?? "One-time amount"}</dd></div>
      <div><dt>Invoice amount</dt><dd>{draft.plan.currency} {draft.plan.amountExact}</dd></div>
      <div><dt>Issue date</dt><dd>{draft.plan.issuedDate}</dd></div>
      <div><dt>Due date</dt><dd>{draft.plan.dueDate}</dd></div>
    </dl>}
    {pending && <p role="status">Checking invoice status…</p>}
    {notice && <p role="status">{notice}</p>}
    <div className={styles.financeActions}>
      {draft?.status === "Prepared" && !request?.attempted && <>
        <button type="button" disabled={pending} onClick={() => void run(approve)}>Approve exact invoice</button>
        <button type="button" disabled={pending} onClick={close}>Cancel</button>
      </>}
      {draft?.status === "Authorized" && <>
        <button type="button" disabled={pending} onClick={() => void run(execute)}>
          {request?.attempted ? "Retry this approved invoice" : "Issue approved invoice"}
        </button>
        {!request?.attempted && <button type="button" disabled={pending} onClick={close}>Cancel</button>}
      </>}
      {request?.attempted && !request.expired && draft?.status !== "Succeeded" &&
        <button type="button" disabled={pending} onClick={() => void run(check)}>Check invoice status</button>}
      {request?.expired && <button type="button" disabled={pending} onClick={close}>Close after checking Manual Invoices</button>}
    </div>
    {draft?.status === "Succeeded" && <>
      <p role="status">Invoice issued. <strong>{draft.receipt.invoiceNumber}</strong></p>
      <p>Receipt ID <code>{draft.receipt.receiptId}</code></p>
      <div className={styles.financeActions}><button type="button" disabled={pending} onClick={close}>Done</button></div>
    </>}
    <p><Link href="/invoices">Open Manual Invoices</Link></p>
  </section>;
}
