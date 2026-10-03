# Phase 2B — Adjustment, refund and credit-note policy review

Date: 2026-10-04 (Asia/Calcutta). Branch `codex/enterprise-p0-continuation`. Static review only; no application, production-data, Azure or GitHub change. `BUG-DATA-0002` remains OPEN.

## Verified current behavior

- The request UI and API offer five types: Discount, Scholarship, Concession, Refund and CreditNote. All create `PendingApproval` records.
- Approval takes the invoice lock, totals Completed/Reconciled payments, and rejects any type whose amount exceeds the unpaid collectible balance. Every approved type increments the same `Invoice.AdjustedAmount` field. A fully collected invoice therefore rejects a Refund or CreditNote of any positive amount.
- No separate refund payable, disbursement, credit-note number, tax adjustment, or link to a reversed payment was found in the adjustment entity/controller. The existing `Refund` label is **not** evidence that money is returned. The adjustment page's generic approval-language can be read that way.
- The guard correctly prevents collected money plus approved reductions from exceeding gross invoice amount. It does not establish what a refund or credit note should mean after collection, or whether a post-payment price reduction can create an amount owed to a customer.
- Finance governance and invoice views use `Status != "Voided"` to count payments; transaction guards use Completed/Reconciled. The current payment API creates Completed and permits transitions only among Completed, Reconciled and Voided. These filters agree for supported states; no live reporting defect is claimed here. An unexpected legacy/status value would require separate data-quality review.

## Decision needed before changing financial behavior

### Product-owner direction — 2026-10-04

The owner confirmed that Academy Desk should follow IFRS principles and applicable Indian GST rules. Treat price reductions, refund obligations/settlement, and credit-note documents as separate concepts. This approves the **design direction**, not the current implementation or an assertion that a particular academy has a GST registration or that IFRS is its statutory reporting framework. The applicable tax treatment, dates, document fields and accounting entries require qualified local review before release.

The next bounded implementation should first prevent the present `Refund`/`CreditNote` labels from implying a payout or legally issued note, then introduce separate persisted workflows with migration, approval, ledger, tax-document and regression evidence. Existing pending records must be reviewed/migrated without silently changing their meaning. Do not alter the financial ledger or claim compliance from this documentation update.

Recommend separating **price reductions** from **money owed/returned**:

| Case | Interim safe rule | Future governed workflow |
| --- | --- | --- |
| Discount / Scholarship / Concession | Reduce only the unpaid collectible amount, with current approval guard and audit trail. | Any post-payment exception needs an explicit payable/refund policy. |
| Refund | Do not present approval as a payout. A true refund requires a separately approved liability/disbursement record, payment reference, status and reconciliation. | Define eligible payer, refundable cap, partial refunds, failed payout/reversal, duplicate prevention, permissions and immutable history. |
| Credit note | Do not silently equate it with a cash refund or generic discount. | Define invoice/tax-document effect, numbering, links to original invoice, jurisdiction and reporting requirements. |

This is a product recommendation, **not** a decision that IFRS applies to this academy or legal/tax advice. IFRS 15 distinguishes consideration expected to be refunded as a refund liability; Indian GST law has distinct credit-note conditions and particulars. Applicability requires the product owner and qualified accountant/tax adviser. Primary references: [IFRS Foundation IFRS 15](https://www.ifrs.org/content/dam/ifrs/publications/pdf-standards/english/2022/issued/part-a/ifrs-15-revenue-from-contracts-with-customers.pdf?bypass=on) (paragraph 55); [CBIC CGST Act](https://cbic-gst.gov.in/hindi/CGST-bill-e.html) (section 34); [CBIC invoice and credit-note rules](https://cbic-gst.gov.in/gst-invoice-rules.html).

## Acceptance tests to add after policy approval

1. Partial/fully paid invoice: each reduction type cannot silently create a negative receivable; rejected decision leaves invoice, adjustment and payments unchanged.
2. Refund proposal/approval/settlement: no cash payout is inferred from `AdjustedAmount`; paid and payable are separately visible; repeated requests and concurrent settlement cannot double-refund.
3. Credit note: original-invoice linkage, unique number, tax fields where applicable, audit/role/tenant controls and exports agree with its defined meaning.
4. Cancellation/failure: payout reversal or failed payout preserves a correct liability and a complete audit trail.
5. Desktop/mobile browser and physical-device checks show unambiguous labels and balances; SQL and API snapshots match.

The current race and linked browser tests establish only the existing balance guard. They do not close this policy gate or authorize merge/push/deployment.
