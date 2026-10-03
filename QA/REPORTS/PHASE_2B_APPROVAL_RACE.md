# Approval versus payment — BUG-DATA-0002

Date: 2026-10-03. Branch: `codex/enterprise-p0-continuation`. Model: Grok 4.7 in Cursor. No product source change. No Azure deploy. No merge to `main`.

## Result

Approving a discount and collecting the pre-approval remainder can both succeed. BUG-DATA-0002 stays **OPEN**. The earlier race, where the discount is approved before either payment starts, still stands. This run does not close physical-device or policy gates.

## Run

Disposable SQL Server 2022, run `d66c4ad4118c4001a2a3e9703ea1d7e6`, port 62260. `Run-ReconciledPayment.ps1 -Module ApprovalRace` exit 1. Academy `aa6535f4-9e64-40e6-810c-6dd74d09fb02`. The runner kept the container because the check failed. That owned container was then stopped and removed.

## Setup

Each attempt creates an invoice of 1000, records and reconciles a payment of 600, and leaves a pending discount of 200. Two clients then act together: one approves the discount, and the other posts 400. A guarded result keeps collected Completed plus Reconciled money at or below 1000 minus the stored adjustment. Five attempts ran. No 429 result appeared.

## Observed

| Attempt | Statuses | Collected | Adjusted | Invoice status | Guarded |
| --- | --- | --- | --- | --- | --- |
| 1 | 200, 201 | 1000 | 200 | Paid | no |
| 2 | 200, 201 | 1000 | 200 | Paid | no |
| 3 | 200, 201 | 1000 | 200 | Paid | no |
| 4 | 200, 201 | 1000 | 200 | Paid | no |
| 5 | 200, 201 | 1000 | 200 | Paid | no |

All five attempts stored 1000 collected against a collectible amount of 800, and marked the invoice Paid. `FinanceAdjustmentsController.Decide` applies the discount and recalculates invoice status without the invoice-row lock used by payment creation.

## Still open

The product still needs one transaction so the approval and the payment cannot both commit when together they pass the collectible amount. Physical device, over-adjustment policy, and the other open P0 items stay open.
