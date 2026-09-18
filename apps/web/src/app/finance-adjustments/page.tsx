"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Invoice = { id: string; invoiceNumber: string; totalAmount: number; adjustedAmount?: number };
type Adjustment = { id: string; invoiceId: string; type: string; amount: number; currency: string; reason: string; status: string; approvedAtUtc?: string | null; approvalNotes?: string | null };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency, maximumFractionDigits: 0 }).format(amount);

export default function AdjustmentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [adjustments, setAdjustments] = useState<Adjustment[]>([]);
  const [notice, setNotice] = useState("Loading adjustment register…");
  const [submitting, setSubmitting] = useState(false);
  const [filter, setFilter] = useState("All");

  async function load(academyId?: string) {
    try {
      const id = academyId ?? academy?.id;
      if (!id) return;
      const [invoiceResponse, adjustmentResponse] = await Promise.all([
        academyApi(`/api/academies/${id}/invoices`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/finance-adjustments`, { cache: "no-store" }),
      ]);
      if (!invoiceResponse.ok || !adjustmentResponse.ok) throw new Error();
      setInvoices(await invoiceResponse.json());
      setAdjustments(await adjustmentResponse.json());
      setNotice("");
    } catch {
      setNotice("Adjustment data could not be loaded. Confirm that the API is running and that you have finance access.");
    }
  }

  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setNotice("Create an academy before submitting an adjustment.");
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setNotice("Adjustment data could not be loaded. Please sign in and restart the API if needed.");
      }
    })();
  }, []);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const form = event.currentTarget;
    const data = new FormData(form);
    setSubmitting(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/finance-adjustments`, {
        method: "POST", headers: apiHeaders(true),
        body: JSON.stringify({ invoiceId: data.get("invoiceId"), type: data.get("type"), amount: Number(data.get("amount")), reason: data.get("reason") }),
      });
      if (!response.ok) throw new Error();
      form.reset();
      setNotice("Adjustment submitted for approval and recorded in the finance register.");
      await load();
    } catch {
      setNotice("The adjustment could not be submitted. Select a valid invoice, positive amount, type and reason.");
    } finally { setSubmitting(false); }
  }

  const visible = filter === "All" ? adjustments : adjustments.filter((item) => item.status === filter);
  const invoiceNumber = (invoiceId: string) => invoices.find((invoice) => invoice.id === invoiceId)?.invoiceNumber ?? "Invoice unavailable";
  const counts = { All: adjustments.length, PendingApproval: adjustments.filter((item) => item.status === "PendingApproval").length, Approved: adjustments.filter((item) => item.status === "Approved").length, Rejected: adjustments.filter((item) => item.status === "Rejected").length };

  return <main className="enterprise-settings">
    <header className="enterprise-page-header"><p>Finance / adjustments</p><div className="flex flex-wrap items-end justify-between gap-4"><div><h2>Adjustment request register</h2><span>Submit and trace discounts, scholarships, concessions, refunds and credit notes through a controlled approval lifecycle.</span></div><Link href="/finance-governance" className="text-sm font-medium text-cyan-300">Open approval queue →</Link></div></header>
    {notice && <p className="mt-5 rounded border border-amber-700/50 bg-amber-950/30 p-3 text-sm text-amber-100" role="status">{notice}</p>}
    <section className="mt-5 grid gap-5 xl:grid-cols-[0.75fr_1.25fr]">
      <form onSubmit={save} className="surface-panel rounded-xl p-5"><h3 className="font-semibold">New adjustment request</h3><p className="mt-1 text-sm text-slate-400">An approved request updates the invoice balance. Rejected requests remain in the register for audit.</p><label className="mt-5 block text-sm text-slate-300">Invoice<select required name="invoiceId" className="field mt-2"><option value="">Select invoice…</option>{invoices.map((invoice) => <option key={invoice.id} value={invoice.id}>{invoice.invoiceNumber} · {money(invoice.totalAmount)}</option>)}</select></label><label className="mt-3 block text-sm text-slate-300">Adjustment type<select name="type" className="field mt-2"><option>Discount</option><option>Scholarship</option><option>Concession</option><option>Refund</option><option>CreditNote</option></select></label><label className="mt-3 block text-sm text-slate-300">Amount<input required name="amount" type="number" min="0.01" step="0.01" className="field mt-2" placeholder="0.00" /></label><label className="mt-3 block text-sm text-slate-300">Business reason<textarea required name="reason" className="field mt-2 min-h-24" placeholder="State the evidence, policy reference, and reason for approval." /></label><button disabled={!academy || submitting} className="mt-5 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950 disabled:opacity-60">{submitting ? "Submitting…" : "Submit for approval"}</button></form>
      <section className="surface-panel rounded-xl p-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><h3 className="font-semibold">Request history</h3><p className="mt-1 text-sm text-slate-400">Finance control evidence for every decision.</p></div><select value={filter} onChange={(event) => setFilter(event.target.value)} className="field w-auto"><option value="All">All ({counts.All})</option><option value="PendingApproval">Pending ({counts.PendingApproval})</option><option value="Approved">Approved ({counts.Approved})</option><option value="Rejected">Rejected ({counts.Rejected})</option></select></div>{visible.length === 0 ? <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">No {filter === "All" ? "adjustment requests" : filter.toLowerCase().replace("approval", "")} requests yet.</p> : <div className="mt-5 overflow-x-auto"><table className="min-w-full text-left text-sm"><thead className="text-slate-400"><tr><th className="pb-3 pr-4">Invoice / type</th><th className="pb-3 pr-4">Reason</th><th className="pb-3 pr-4">Amount</th><th className="pb-3">Decision</th></tr></thead><tbody>{visible.map((item) => <tr key={item.id} className="border-t border-slate-800 align-top"><td className="py-3 pr-4"><b className="block">{invoiceNumber(item.invoiceId)}</b><span className="text-slate-400">{item.type}</span></td><td className="py-3 pr-4 text-slate-300">{item.reason}</td><td className="py-3 pr-4 font-medium">{money(item.amount, item.currency)}</td><td className="py-3"><b className={item.status === "Approved" ? "text-emerald-300" : item.status === "Rejected" ? "text-rose-300" : "text-amber-200"}>{item.status === "PendingApproval" ? "Pending approval" : item.status}</b>{item.approvalNotes && <span className="mt-1 block text-xs text-slate-400">{item.approvalNotes}</span>}{item.approvedAtUtc && <span className="mt-1 block text-xs text-slate-500">{new Date(item.approvedAtUtc).toLocaleDateString("en-IN")}</span>}</td></tr>)}</tbody></table></div>}</section>
    </section>
  </main>;
}
