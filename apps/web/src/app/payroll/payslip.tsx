"use client";

import { createPortal } from "react-dom";
import { StandardDetailModal } from "@/components/design-system/controls";
import { payrollMoney } from "./payroll-money";
import styles from "./payslip.module.css";

export type Payout = {
  id: string; payrollProfileId: string; workerName: string; workerType: string;
  payslipNumber: string; periodLabel: string; sessionsCovered?: number | null;
  grossAmount: number; deductions: number; netAmount: number; currency: string;
  grossAmountExact?: string; deductionsExact?: string; netAmountExact?: string;
  status: string; paymentMethod: string; reference?: string | null; paidAtUtc: string | null;
  identitySource?: string; identityRecordedAtUtc?: string | null; settlementKind?: string;
};
export function PayrollPayslip({ payout, academyName, onClose }: { payout: Payout; academyName?: string; onClose: () => void }) {
  const statement = payout.status === "NoCashDue";
  const date = payout.paidAtUtc ? new Date(payout.paidAtUtc).toLocaleString("en-IN", { timeZone: "UTC" }) + " UTC" : "No cash payment date";
  return createPortal(<div className={`payroll-print-target ${styles.printTarget}`}>
    <StandardDetailModal title={statement ? "Earnings statement" : "Recorded payslip"} eyebrow="Payroll record" onClose={onClose}>
      <article className={styles.paper} aria-label="Selected payroll document" data-payout-id={payout.id}>
        <header className={styles.heading}><p>{academyName || "Academy payroll"}</p><h3>{statement ? "Earnings statement" : "Payslip"}</h3><strong>{payout.payslipNumber}</strong></header>
        <dl className={styles.details}>
          <div><dt>Worker</dt><dd>{payout.workerName}</dd></div><div><dt>Worker type</dt><dd>{payout.workerType}</dd></div>
          <div><dt>Pay period</dt><dd>{payout.periodLabel}</dd></div><div><dt>Cycle</dt><dd>{payout.sessionsCovered == null ? "Monthly" : `${payout.sessionsCovered} sessions`}</dd></div>
          <div><dt>Recorded status</dt><dd>{statement ? "No cash due" : payout.status}</dd></div><div><dt>Payment time</dt><dd>{date}</dd></div>
          <div><dt>Payment method</dt><dd>{statement ? "No cash payment" : payout.paymentMethod}</dd></div><div><dt>Reference</dt><dd>{payout.reference || "Not recorded"}</dd></div>
        </dl>
        <section className={styles.amounts} aria-label="Payroll amount breakdown">
          <div><span>Gross earnings</span><b>{payrollMoney(payout.grossAmount, payout.grossAmountExact, payout.currency)}</b></div>
          <div><span>Deductions</span><b>{payrollMoney(payout.deductions, payout.deductionsExact, payout.currency)}</b></div>
          <div><span>Net amount</span><b>{payrollMoney(payout.netAmount, payout.netAmountExact, payout.currency)}</b></div>
        </section>
        {payout.identitySource !== "PayoutSnapshot" && <p className={styles.note}>Historical worker identity is unverified. No identity has been inferred from a current profile.</p>}
        {payout.settlementKind === "LegacyZeroNet" && <p className={styles.note}>Legacy zero-net record: the saved Paid label is retained, not proof of a cash disbursement.</p>}
        <footer className={styles.note}>Source record: {payout.id}. This application record is not proof of bank settlement or statutory payroll compliance.</footer>
      </article>
      <div className={styles.actions}><button type="button" className="enterprise-action-button" onClick={() => window.print()}>Print / Save PDF</button><button type="button" className="enterprise-action-button enterprise-action-button-secondary" onClick={onClose}>Close preview</button></div>
    </StandardDetailModal>
  </div>, document.body);
}
