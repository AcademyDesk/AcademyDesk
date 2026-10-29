"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import visual from "@/components/design-system/finance-controls.module.css";
import { StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string };
type Invoice = {
  id: string;
  invoiceNumber: string;
  totalAmount: number;
  adjustedAmount?: number;
};
type Adjustment = {
  id: string;
  invoiceId: string;
  type: string;
  amount: number;
  currency: string;
  reason: string;
  status: string;
  approvedAtUtc?: string | null;
  approvalNotes?: string | null;
};
const money = (amount: number, currency = "INR") =>
  new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
  }).format(amount);

export default function AdjustmentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [adjustments, setAdjustments] = useState<Adjustment[]>([]);
  const [notice, setNotice] = useState("Loading adjustment register…");
  const [submitting, setSubmitting] = useState(false);
  const [filter, setFilter] = useState("All");
  const [requestInvoiceId, setRequestInvoiceId] = useState("");
  const [requestType, setRequestType] = useState("Discount");

  const [ready, setReady] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const [noticeIsError, setNoticeIsError] = useState(false);
  const guard = useRef({ pending: false, ready: false, review: false });
  const blocked = !ready || submitting || needsReview;
  function requireReview() { guard.current.review = true; setNeedsReview(true); }
  async function refreshRegister() {
    if (!academy || guard.current.pending) return;
    guard.current.pending = true; setSubmitting(true);
    try { await load(academy.id); guard.current.review = false; setNeedsReview(false); setNoticeIsError(false); setNotice("Adjustment register refreshed. Check the previous action before trying again."); }
    catch { requireReview(); setNoticeIsError(true); setNotice("Adjustment register could not be refreshed. Do not repeat an unconfirmed action."); }
    finally { guard.current.pending = false; setSubmitting(false); }
  }

  const load = useCallback(async (id: string) => {
    const responses = await Promise.all([academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }), academyApi(`/api/academies/${id}/finance-adjustments`, { cache: "no-store" })]);
    if (responses.some(response => !response.ok)) throw Error();
    const [invoiceRows, adjustmentRows] = await Promise.all(responses.map(response => response.json()));
    if (![invoiceRows, adjustmentRows].every(Array.isArray)) throw Error();
    setInvoices(invoiceRows); setAdjustments(adjustmentRows); guard.current.ready = true; setReady(true);
  }, []);

  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0])
          return setNotice(
            "Create an academy before submitting an adjustment.",
          );
        setAcademy(academies[0]);
        await load(academies[0].id); setNotice("");
      } catch {
        setNoticeIsError(true);
        setNotice(
          "Adjustment data could not be loaded. Please sign in and restart the API if needed.",
        );
      }
    })();
  }, [load]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !guard.current.ready || guard.current.pending || guard.current.review) return;
    const form = event.currentTarget, data = new FormData(form);
    const captured = { invoiceId: requestInvoiceId, type: requestType, amount: String(data.get("amount") ?? ""), reason: String(data.get("reason") ?? "") };
    if (!invoices.some(invoice => invoice.id === captured.invoiceId) || !["Discount", "Scholarship", "Concession"].includes(captured.type) || !Number.isFinite(Number(captured.amount)) || Number(captured.amount) <= 0 || !captured.reason.trim()) {
      setNoticeIsError(true); setNotice("Select a valid invoice, positive amount, reduction type and business reason."); return;
    }
    guard.current.pending = true; setSubmitting(true); setNoticeIsError(false); setNotice("Submitting adjustment…");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/finance-adjustments`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ invoiceId: captured.invoiceId, type: captured.type, amount: Number(captured.amount), reason: captured.reason }) });
      if (!response.ok) {
        setNoticeIsError(true);
        if (response.status >= 500 || response.status === 408) { requireReview(); setNotice("Adjustment submission could not be confirmed. Refresh and check the register before retrying."); }
        else { const problem = await response.json().catch(() => null); setNotice(typeof problem?.message === "string" ? problem.message : "Adjustment was rejected. Check the details and your access."); }
        return;
      }
      // Uncontrolled native fields: do not erase a later draft changed outside disabled UI.
      // Disabled fields are omitted by FormData during the pending state.
      const currentAmount = form.elements.namedItem("amount") as HTMLInputElement | null;
      const currentReason = form.elements.namedItem("reason") as HTMLTextAreaElement | null;
      if (currentAmount?.value === captured.amount && currentReason?.value === captured.reason) form.reset();
      setRequestInvoiceId(value => value === captured.invoiceId ? "" : value);
      setRequestType(value => value === captured.type ? "Discount" : value);
      setNotice("Adjustment submitted for approval and recorded in the finance register.");
      try { await load(academy.id); }
      catch { requireReview(); setNoticeIsError(true); setNotice("Adjustment submitted for approval. The register could not be refreshed; do not submit it again. Refresh the adjustment register to continue."); }
    } catch { requireReview(); setNoticeIsError(true); setNotice("Adjustment submission could not be confirmed. Refresh and check the register before retrying."); }
    finally { guard.current.pending = false; setSubmitting(false); }
  }

  const visible =
    filter === "All"
      ? adjustments
      : adjustments.filter((item) => item.status === filter);
  const invoiceNumber = (invoiceId: string) =>
    invoices.find((invoice) => invoice.id === invoiceId)?.invoiceNumber ??
    "Invoice unavailable";
  const counts = {
    All: adjustments.length,
    PendingApproval: adjustments.filter(
      (item) => item.status === "PendingApproval",
    ).length,
    Approved: adjustments.filter((item) => item.status === "Approved").length,
    Rejected: adjustments.filter((item) => item.status === "Rejected").length,
  };

  return (
    <main className={`enterprise-settings finance-module adjustments-standard ${visual.page}`}>
      <header className="enterprise-page-header">
        <p>Finance / adjustments</p>
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <h1>Adjustment request register</h1>
            <span>
              Submit discounts, scholarships and concessions as invoice reductions.
              Refunds and credit notes require separate reviewed workflows and are not available here.
            </span>
          </div>
          <Link
            href="/finance-governance"
            className="text-sm font-medium text-cyan-300"
          >
          Approval queue
          </Link>
        </div>
      </header>
      {notice && (
        <p
          className="mt-5 rounded border border-amber-700/50 bg-amber-950/30 p-3 text-sm text-amber-100"
          role={noticeIsError ? "alert" : "status"}
        >
          {notice}
        </p>
      )}
      {academy && <button type="button" disabled={submitting} onClick={refreshRegister} className="enterprise-action-button">Refresh adjustment register</button>}
      <section className="mt-5 grid gap-5 xl:grid-cols-[0.75fr_1.25fr]">
        <form
          aria-busy={submitting}
          onSubmit={save}
          className="finance-adjustment-form surface-panel rounded-xl p-5"
        >
          <h3 className="font-semibold">New adjustment request</h3>
          <p className="mt-1 text-sm text-slate-400">
            An approved request updates the invoice balance. Rejected requests
            remain in the register for audit.
          </p>
          <div className="finance-adjustment-field finance-adjustment-field-first">
            <StandardSelectField
              name="invoiceId"
              label="Invoice"
              required
              disabled={blocked}
              value={requestInvoiceId}
              onChange={setRequestInvoiceId}
              placeholder="Select invoice…"
              options={invoices.map((invoice) => ({ value: invoice.id, label: `${invoice.invoiceNumber} · ${money(invoice.totalAmount)}` }))}
            />
          </div>
          <div className="finance-adjustment-field">
            <StandardSelectField
              name="type"
              label="Adjustment type"
              disabled={blocked}
              value={requestType}
              onChange={setRequestType}
              placeholder="Choose adjustment type"
              options={[
                { value: "Discount", label: "Discount" },
                { value: "Scholarship", label: "Scholarship" },
                { value: "Concession", label: "Concession" },
              ]}
            />
          </div>
          <label className="finance-adjustment-field">
            <span>Amount</span>
            <input
              disabled={blocked}
              required
              name="amount"
              type="number"
              min="0.01"
              step="0.01"
              className="field mt-2"
              placeholder="0.00"
            />
          </label>
          <label className="finance-adjustment-field finance-adjustment-reason">
            <span>Business reason</span>
            <textarea
              disabled={blocked}
              required
              name="reason"
              className="field mt-2 min-h-24"
              placeholder="State the evidence, policy reference, and reason for approval."
            />
          </label>
          <button
            disabled={!academy || !requestInvoiceId || blocked}
            className="enterprise-action-button mt-5 disabled:opacity-60"
          >
            {submitting ? "Submitting…" : "Submit for approval"}
          </button>
        </form>
        <section className="surface-panel rounded-xl p-5">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 className="font-semibold">Request history</h3>
              <p className="mt-1 text-sm text-slate-400">
                Finance control evidence for every decision.
              </p>
            </div>
            <div className="adjustments-filter">
              <StandardSelectField
                name="adjustmentFilter"
                label="Request status"
                value={filter}
                onChange={setFilter}
                placeholder="Filter requests"
                options={[
                  { value: "All", label: `All (${counts.All})` },
                  { value: "PendingApproval", label: `Pending (${counts.PendingApproval})` },
                  { value: "Approved", label: `Approved (${counts.Approved})` },
                  { value: "Rejected", label: `Rejected (${counts.Rejected})` },
                ]}
              />
            </div>
          </div>
          {!ready ? <p className="mt-5">Adjustment register unavailable until loading succeeds.</p> : visible.length === 0 ? (
            <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">
              No{" "}
              {filter === "All"
                ? "adjustment requests"
                : filter.toLowerCase().replace("approval", "")}{" "}
              requests yet.
            </p>
          ) : (
            <div className={`mt-5 ${visual.scroll}`} role="region" aria-label="Adjustment history — scroll horizontally to view all columns" tabIndex={0}>
              <table className="min-w-full text-left text-sm">
                <thead className="text-slate-400">
                  <tr>
                    <th className="pb-3 pr-4">Invoice / type</th>
                    <th className="pb-3 pr-4">Reason</th>
                    <th className="pb-3 pr-4">Amount</th>
                    <th className="pb-3">Decision</th>
                  </tr>
                </thead>
                <tbody>
                  {visible.map((item) => (
                    <tr
                      key={item.id}
                      className="border-t border-slate-800 align-top"
                    >
                      <td className="py-3 pr-4">
                        <b className="block">{invoiceNumber(item.invoiceId)}</b>
                        <span className="text-slate-400">{item.type}</span>
                        {["refund", "creditnote"].includes(item.type.toLowerCase()) && <small className="block">Legacy invoice-reduction label, not proof of refund settlement or an issued credit note. Pending records require reviewed disposition.</small>}
                      </td>
                      <td className="py-3 pr-4 text-slate-300">
                        {item.reason}
                      </td>
                      <td className="py-3 pr-4 font-medium">
                        {money(item.amount, item.currency)}
                      </td>
                      <td className="py-3">
                        <b
                          className={
                            item.status === "Approved"
                              ? "text-emerald-300"
                              : item.status === "Rejected"
                                ? "text-rose-300"
                                : "text-amber-200"
                          }
                        >
                          {item.status === "PendingApproval"
                            ? "Pending approval"
                            : item.status}
                        </b>
                        {item.approvalNotes && (
                          <span className="mt-1 block text-xs text-slate-400">
                            {item.approvalNotes}
                          </span>
                        )}
                        {item.approvedAtUtc && (
                          <span className="mt-1 block text-xs text-slate-500">
                            {new Date(item.approvedAtUtc).toLocaleDateString(
                              "en-IN",
                            )}
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </section>
    </main>
  );
}
