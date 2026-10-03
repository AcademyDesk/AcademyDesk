# Reconciled payment confirmation — BUG-DATA-0001

Date: 2026-10-03. Branch: `cursor/certificate-validation`. Model: Grok 4.7 in Cursor. No product source change. No Azure deploy. No merge to `main`.

## Result

The original reconciled-payment case still passes on the current source. BUG-DATA-0001 stays **OPEN**. This run does not replace the 2026-10-01 evidence and does not close browser, device, or concurrent collection.

## Run

Disposable SQL Server 2022, run `9a85332f6e1148d1a45900c6c5e0a9e8`. `Run-ReconciledPayment.ps1 -Module Reconciliation` exit 0 in 30.6 seconds. Application migrations 82. Identity migrations 7. Runtime inventory 313 routes. The owned database, login, and container were removed.

## Observed

Invoice 1000. Payment 600 stays collected after reconciliation. Admin and student balances stay 400. An extra payment of 500 is rejected before and after reconciliation, and the ledger stays one row of 600. A blank reconciliation reference is rejected with no SQL change.

The bounded regression then counts Reconciled 600 plus Completed 100, excludes Voided 75, settles the invoice, rejects one extra cent, and queues no reminder on a settled invoice.

## Still open

Two payments racing for the same remaining balance were not run. A live Payments browser and a physical device were not run. BUG-DATA-0002, BUG-DATA-0003, BUG-DATA-0010, and BUG-SEC-0001 stay open.
