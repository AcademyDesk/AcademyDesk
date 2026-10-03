# Phase 2A — FINANCE-RULE-002 / BUG-DATA-0002

Date: 2026-09-30. Base commit: `20bb6047f9edf733ac8e2a226621cc582ec54b3c` plus the recorded working-tree changes. Synthetic local real-HTTP/SQL reproduction only; no product repair or production conclusion.

## Runs and fixture safety

| Fresh run | UTC start | Loopback SQL port | Harness duration including SQL readiness | Result |
| --- | --- | --- | --- | --- |
| `cf65a51012fa4437a967cc2c1c89dfbc` | 2026-09-30 11:39:08 | 53338 | 58.2 seconds | FINANCE-RULE-002 FAIL; infrastructure exit 0 |
| `9727e62fd106437091861d3a30f3d49d` | 2026-09-30 11:40:18 | 54697 | 61.5 seconds | FINANCE-RULE-002 FAIL; infrastructure exit 0 |

Both runs used new run-labelled Docker SQL Server 2022 containers, owned databases and database-scoped runtime credentials. The real ASP.NET TestServer retained authentication, academy authorization, subscription checks and audit filters. Each applied 79 application and 7 Identity migrations. Health, real login, anonymous denial and tenant A/B controls passed. Existing private-file and reconciliation reproductions still reported FAIL. Blob access remained disabled; no development/Azure database or customer/payment data was used.

Synthetic AcademyAdmin A used actual invoice, payment and adjustment APIs. Academy A enables Certificates, Finance and FinanceControls. An initial attempt (`2e4176d2e7d34649b12643baa4fb0008`, port 53420) omitted FinanceControls and stopped on HTTP 403 before creating an adjustment. This was a fixture failure, not a product defect or counted reproduction. Only the test fixture was corrected; authorization was not bypassed or altered. The failed container was retained until its exact name, hostname, image and run label were verified, then stopped and removed, deleting its synthetic SQL data. The failed attempt did not reach normal SQL marker cleanup.

Three independent invoices avoid contaminating settlement/full-adjustment checks with the deliberately attempted overpayment. Invoice creation is spaced by 1.1 seconds to avoid unrelated collisions in the product's seconds-plus-random invoice-number generator. Source hashes, identical across the two counted runs: `Program.cs` SHA-256 `75C76C23EF4F1AEF46603A8DDC438C1ABABD0AE65F4452B19805F218EF9A7EB4`; `AdjustmentReproduction.cs` SHA-256 `792C8889FC0B1F2B8A1CCA5CB845514EF1D59E482CE2874EC42EDDEC92EE5AE9`.

## Approval controls — passed in both runs for all three invoices

1. POST invoice amount 1000 returned 201; fresh SQL showed Issued, adjustment 0, no payments or adjustments.
2. POST adjustment type Discount, amount 0 returned 400; captured invoice/payment/adjustment SQL snapshot was unchanged.
3. POST valid adjustment returned 200; fresh SQL showed PendingApproval, no approval/application timestamp and no change to the invoice's adjusted amount.
4. PATCH adjustment approval with `approve=true`, notes `QA-APPROVED` returned 200. Fresh SQL showed Approved, exact adjustment amount, approval/application timestamps and notes, applied once to the owned invoice.
5. Repeating approval returned 409. The complete captured financial snapshot stayed unchanged, including adjustment amount, timestamps and notes.

These no-change assertions cover the captured invoice, payment and adjustment rows, not every possible audit or other table. Fresh no-tracking contexts are used at every transition. API success alone is not the persistence oracle.

## Business failures — identical in both counted runs

| Independent invoice scenario | Expected | Actual HTTP + fresh SQL/admin read |
| --- | --- | --- |
| Invoice 1000, approved discount 200, collected payment 600 | Remaining balance 200 | Baseline PASS: one Completed payment 600, adjusted amount 200, PartiallyPaid, admin paid 600/balance 200 |
| Same invoice: attempt extra payment 300 | 400, unchanged financial rows | **201, second Completed payment 300 persisted; ledger 900 plus discount 200 exceeds invoice 1000; admin balance clamped to 0, status PartiallyPaid** |
| Separate invoice 1000, discount 200, collected 600: pay exact remaining 200 | 201, ledger 800, zero balance, Paid in SQL and admin read | 201 and ledger 800/balance 0, but **SQL and admin status both PartiallyPaid** |
| Separate invoice 1000, full approved discount 1000 | Paid, zero balance, no payments | Baseline PASS: SQL/admin Paid, balance 0, no payments |
| Fully adjusted invoice: attempt payment 1 | 400, unchanged financial rows and Paid state | **201, Completed payment 1 persisted; invoice changed from Paid to PartiallyPaid** |

The arithmetic expectation is fixed independently: 1000 − 200 − 600 = 200; exact settlement totals 800 collected + 200 discount = 1000; full adjustment leaves 0 collectible. The test accepts either 400 or 201 as an observation, verifies its actual persistence, and explicitly prints FAIL for a rule violation. Exit 0 means reproduction/cleanup completed, not that finance passed.

## Original sanitized failing output

This line was emitted in both counted runs:

```text
FINANCE-RULE-002 FAIL: independent invoices 1000; approved adjustment 200 + collected 600 => admin balance 200; extra 300 HTTP=201/ledger=900.00/rows=2/balance=0; exact 200 HTTP=201/ledger=800.00/balance=0/SQL status=PartiallyPaid/view status=PartiallyPaid; full adjustment 1000 starts Paid/balance=0; extra 1 HTTP=201/ledger=1.00/SQL status=PartiallyPaid; expected reject 300 unchanged, accept 200 with Paid/zero balance, reject 1 unchanged; pending/invalid/reapproval controls PASS.
```

Observed SQL invoice identifiers and final states:

| Run | Overpayment invoice | Exact-settlement invoice | Full-adjustment invoice |
| --- | --- | --- | --- |
| `cf65a51012fa4437a967cc2c1c89dfbc` | `08a4be9c-45a4-401c-a4bd-2f7313ace562`: 1000 total / 200 adjusted / 900 collected / PartiallyPaid | `199f2716-22eb-4479-9cb2-0aee7b8be7d5`: 1000 / 200 / 800 / PartiallyPaid | `8471a38e-8c04-4aed-a3ce-5b3b701c0a8b`: 1000 / 1000 / 1 / PartiallyPaid |
| `9727e62fd106437091861d3a30f3d49d` | `fdb3a35d-e9e3-45c3-b9e6-fd9298eccd58`: 1000 / 200 / 900 / PartiallyPaid | `e063d43e-e70d-49b5-8011-06e7d7624783`: 1000 / 200 / 800 / PartiallyPaid | `70fef29b-7a47-46eb-98ea-e59e5b7a7479`: 1000 / 1000 / 1 / PartiallyPaid |

For both successful runs a mismatched SQL cleanup marker was refused before the callback. Matching marker cleanup then removed the owned database, login and host folders; exact run-labelled Docker containers were stopped/removed. Only cached images remain. These synthetic SQL data cannot be recovered from the removed containers.

## Decision and remaining coverage

**BUG-DATA-0002: RUNTIME-REPRODUCED / OPEN P0.** Payment creation checks and status calculations use gross TotalAmount rather than net-of-approved-adjustment amount. The admin balance subtracts AdjustedAmount and clamps negative results, concealing the overcollection in the displayed balance.

No product controllers, existing student UI changes, schema, Azure settings/data, commits or deployments were modified. Harness build passed with zero warnings/errors. After container cleanup, the API unit suite passed **45/45**, with zero failures/skips. Adjustment JSON, test matrix, issue index/card and phase checkpoint consistency passed; `git diff --check` found no whitespace errors, and no run-labelled QA SQL containers remain. The historical Phase 1 fingerprint validator was not weakened or claimed to pass on the expanded Phase 2 tree. This bounded case does not certify Guardian/student adjustment presentation, other adjustment types, over-adjustment policy, concurrency or mixed reconciled/voided payment behavior. Those remain required targeted regression or later phase coverage.

Phase 2A is **3/5 P0 baseline reproductions completed twice**, all three failing and still OPEN. Next is PAYROLL-RULE-001, then FINANCE-RULE-003 and consolidated handoff evidence. Repairs begin in Phase 2B after that handoff, one confirmed issue at a time.
