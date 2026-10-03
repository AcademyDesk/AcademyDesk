"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
import { EnterprisePageState } from "@/components/enterprise-page-state";

type Academy = { id: string };
type FinanceSummary = {
  grossBilled: number;
  approvedAdjustments: number;
  collected: number;
  outstanding: number;
  reconciled: number;
  overdueInvoices: number;
};
type Collection = {
  id: string;
  invoiceNumber: string;
  totalAmount: number;
  balance: number;
  currency: string;
  dueDate: string;
  status: string;
  daysOverdue: number;
};

const money = (amount: number, currency = "INR") =>
  new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
  }).format(amount);
const modules = [
  [
    "Create & generate invoices",
    "/invoices",
    "Billing",
  ],
  [
    "Payments received",
    "/payments",
    "Collections",
  ],
  [
    "Reconciliation",
    "/finance-reconciliation",
    "Control",
  ],
  [
    "Adjustments",
    "/finance-adjustments",
    "Approval",
  ],
  [
    "Control centre",
    "/finance-governance",
    "Governance",
  ],
  [
    "Finance policy",
    "/finance-policy",
    "Settings",
  ],
  [
    "Outstanding & unpaid",
    "/fee-reminders",
    "Automation",
  ],
  [
    "Expenses",
    "/expenses",
    "Operations",
  ],
  [
    "Invoice & payslip templates",
    "/finance-governance",
    "Documents",
  ],
] as const;

export default function FinancePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [summary, setSummary] = useState<FinanceSummary>();
  const [collections, setCollections] = useState<Collection[]>([]);
  const [notice, setNotice] = useState("Loading finance workspace…");
  const [exporting, setExporting] = useState<string>();

  useEffect(() => {
    async function load() {
      try {
        const academyResponse = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (academyResponse.status === 401)
          return setNotice(
            "Please sign in before opening the finance workspace.",
          );
        if (!academyResponse.ok) throw new Error();
        const academies: Academy[] = await academyResponse.json();
        if (!academies[0])
          return setNotice("Create an academy before configuring finance.");
        const currentAcademy = academies[0];
        setAcademy(currentAcademy);
        const [summaryResponse, collectionsResponse] = await Promise.all([
          academyApi(
            `/api/academies/${currentAcademy.id}/finance-governance/summary`,
            { cache: "no-store" },
          ),
          academyApi(
            `/api/academies/${currentAcademy.id}/finance-governance/collections`,
            { cache: "no-store" },
          ),
        ]);
        if (!summaryResponse.ok || !collectionsResponse.ok) throw new Error();
        setSummary(await summaryResponse.json());
        setCollections(await collectionsResponse.json());
        setNotice("");
      } catch {
        setNotice(
          "Finance data could not be loaded. Confirm that the AcademyDesk API is running and that your account can access this academy.",
        );
      }
    }
    void load();
  }, []);

  async function downloadExport(kind: "invoices" | "payments") {
    if (!academy) return;
    setExporting(kind);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/exports/${kind}`,
      );
      if (!response.ok) throw new Error();
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `academydesk-${kind}.csv`;
      link.click();
      URL.revokeObjectURL(url);
      setNotice(
        `${kind === "invoices" ? "Invoice" : "Payment"} export downloaded.`,
      );
    } catch {
      setNotice("The export could not be created. Please try again.");
    } finally {
      setExporting(undefined);
    }
  }

  const cards = summary
    ? [
        ["Gross billed", money(summary.grossBilled)],
        ["Collected", money(summary.collected)],
        ["Outstanding", money(summary.outstanding)],
        ["Reconciled", money(summary.reconciled)],
      ]
    : [];

  return (
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <p>Finance / workspace</p>
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <h2>Finance operations</h2>
          </div>
          <div className="flex flex-wrap gap-2">
            <Link href="/invoices" className="enterprise-action-button">
              Issue invoice
            </Link>
            <Link
              href="/payments"
              className="enterprise-action-button enterprise-action-button-secondary"
            >
              Record payment
            </Link>
          </div>
        </div>
      </header>
      {notice && (
        <EnterprisePageState
          tone={
            notice.includes("could not") || notice.includes("Please sign")
              ? "error"
              : notice.includes("downloaded")
                ? "success"
                : "loading"
          }
        >
          {notice}
        </EnterprisePageState>
      )}
      <section
        className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4"
        aria-label="Finance summary"
      >
        {cards.map(([label, value]) => (
          <section key={label} className="surface-panel rounded-xl p-5">
            <p className="text-sm text-slate-400">{label}</p>
            <strong className="mt-2 block text-3xl">{value}</strong>
          </section>
        ))}
      </section>
      <section className="mt-5 grid gap-5 xl:grid-cols-[1.2fr_0.8fr]">
        <section className="surface-panel rounded-xl p-5">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 className="font-semibold">Collections attention queue</h3>
            </div>
            <Link
              href="/finance-governance"
              className="text-sm font-medium text-cyan-300"
            >
              Finance controls
            </Link>
          </div>
          {collections.length === 0 ? (
            <p className="mt-5 rounded border border-dashed border-slate-700 p-4 text-sm text-slate-400">
              No overdue invoices need collections action.
            </p>
          ) : (
            <div className="mt-5 overflow-x-auto">
              <table className="min-w-full text-left text-sm">
                <thead className="text-slate-400">
                  <tr>
                    <th className="pb-3 pr-4">Invoice</th>
                    <th className="pb-3 pr-4">Due</th>
                    <th className="pb-3 pr-4">Remaining due</th>
                    <th className="pb-3">Priority</th>
                  </tr>
                </thead>
                <tbody>
                  {collections.slice(0, 5).map((item) => (
                    <tr key={item.id} className="border-t border-slate-800">
                      <td className="py-3 pr-4 font-medium">
                        {item.invoiceNumber}
                      </td>
                      <td className="py-3 pr-4">{item.dueDate}</td>
                      <td className="py-3 pr-4">
                        {money(item.balance, item.currency)}
                      </td>
                      <td className="py-3">
                        <span
                          className={
                            item.daysOverdue > 30
                              ? "text-rose-300"
                              : "text-amber-200"
                          }
                        >
                          {item.daysOverdue} days overdue
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">Finance exports</h3>
          <div className="mt-5 grid gap-3">
            <button
              type="button"
              disabled={!academy || exporting !== undefined}
              onClick={() => void downloadExport("invoices")}
              className="rounded border border-slate-700 px-4 py-3 text-left text-sm hover:border-cyan-400 disabled:opacity-60"
            >
              <b className="block">Invoice ledger CSV</b>
              {exporting === "invoices" && <span className="text-slate-400">Preparing download…</span>}
            </button>
            <button
              type="button"
              disabled={!academy || exporting !== undefined}
              onClick={() => void downloadExport("payments")}
              className="rounded border border-slate-700 px-4 py-3 text-left text-sm hover:border-cyan-400 disabled:opacity-60"
            >
              <b className="block">Payment ledger CSV</b>
              {exporting === "payments" && <span className="text-slate-400">Preparing download…</span>}
            </button>
          </div>
        </section>
      </section>
      <section className="mt-5">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h3 className="font-semibold">Finance work areas</h3>
          </div>
          <Link
            href="/finance-summary"
            className="text-sm font-medium text-cyan-300"
          >
            Finance summary
          </Link>
        </div>
        <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          {modules.map(([title, href, label]) => (
            <Link
              key={`${href}-${label}`}
              href={href}
              className="surface-panel rounded-xl p-4 transition hover:border-cyan-400"
            >
              <span className="text-xs font-semibold uppercase tracking-wider text-cyan-300">
                {label}
              </span>
              <h4 className="mt-2 font-semibold">{title}</h4>
            </Link>
          ))}
        </div>
      </section>
    </main>
  );
}
