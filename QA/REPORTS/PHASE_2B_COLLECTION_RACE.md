# Concurrent collection — BUG-DATA-0001

Date: 2026-10-03. Branch: `cursor/certificate-validation`. Model: Grok 4.7 in Cursor. No product source change. No Azure deploy. No merge to `main`.

## Result

Two payments for the same remaining balance can both be saved. BUG-DATA-0001 stays **OPEN**. The sequential reconciled case from the same day still stands. This run does not close the live Payments browser or a physical device.

## Run

Disposable SQL Server 2022, run `45e986ef6c4b4a72a840fcf09b604e4a`, port 57918. `Run-ReconciledPayment.ps1 -Module CollectionRace` exit 1. Runtime inventory 313 routes, digest `SHA256=562F95AD3C4514C384CCC11317E91174D0BAB7203E6AA7F162FCD74E8BE81A99`. Academy `5bad2f3e-ad44-4f2f-b4c9-f518815a0d64`. The runner kept the container because the check failed. That owned container was then stopped and removed.

## Setup

Each attempt creates an invoice of 1000, records a payment of 600, and reconciles it. Two separate clients then post 400 at the same time. A guarded result is one 201, one 400, two payment rows, and a collected total of 1000. Five attempts ran. No 429 rate-limit result appeared.

## Observed

| Attempt | Statuses | Collected | Rows | Guarded |
| --- | --- | --- | --- | --- |
| 1 | 201, 400 | 1000 | 2 | yes |
| 2 | 201, 201 | 1400 | 3 | no |
| 3 | 201, 400 | 1000 | 2 | yes |
| 4 | 201, 201 | 1400 | 3 | no |
| 5 | 201, 201 | 1400 | 3 | no |

Three of five attempts stored both 400 payments on top of the reconciled 600. The invoice total stayed 1000 and the invoice status became Paid. The extra 400 is a completed payment with no reconciliation reference.

`PaymentsController.Create` reads the collected sum and then inserts the new payment. Those two steps are not one transaction and do not lock the invoice row, so both requests can see 600 already collected and both can accept 400.

## Still open

The product still needs a transaction that lets only one of those payments succeed. Live Payments browser and physical device checks were not run. BUG-DATA-0002, BUG-DATA-0003, BUG-DATA-0010, BUG-SEC-0001, and BUG-FUNC-0032 stay open.
