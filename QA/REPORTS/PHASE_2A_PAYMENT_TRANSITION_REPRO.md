# Phase 2A — FINANCE-RULE-003 / BUG-DATA-0010

Date: 2026-09-30. Base commit: `20bb6047f9edf733ac8e2a226621cc582ec54b3c` plus the recorded working-tree changes. Synthetic local authenticated HTTP/SQL evidence, not a product repair or production financial observation.

## Runs and safe fixtures

| Fresh run | UTC start | Loopback SQL port | Harness duration including readiness | Result |
| --- | --- | --- | --- | --- |
| `789bc93635ec441b8436bfbc496eee02` | 2026-09-30 11:55:29 | 65330 | 78.4 seconds | FINANCE-RULE-003 FAIL; infrastructure exit 0 |
| `e9169638be7b4093a1dcaafb171c8111` | 2026-09-30 11:57:02 | 65354 | 78.5 seconds | FINANCE-RULE-003 FAIL; infrastructure exit 0 |

Both used new exact run-labelled Docker SQL Server 2022 containers and marked databases with restricted runtime credentials. Each applied 79 application and 7 Identity migrations. Real authentication, academy authorization, subscriptions and audit filters were retained in TestServer. Health, real login, anonymous denial and tenant A/B controls passed. All four earlier P0 cases also reproduced their failures in both runs. Blob access stayed disabled; no development/Azure database, customer record or financial delivery service was used.

Four independent invoices were created through the actual Admin APIs, each for 1000 and due seven UTC calendar days before fixture creation. Fresh SQL confirmed the due date, ownership, ledger and baseline status. Full/partial fixtures used payments 1000/600 respectively. The adjusted fixture collected 800 first, then approved a discount 200 through the real approval route; this established a genuinely Paid/zero-balance baseline without being contaminated by BUG-DATA-0002's adjustment-before-payment status defect. The direct-reconciliation fixture used a separate Completed payment 600.

Creation was spaced by 1.1 seconds to avoid unrelated invoice-number collisions. Source hashes, identical in both runs: `Program.cs` SHA-256 `2425E2756460F1B74E3386562FF736944F4A8DEE44BE04AD68F3047A8B83ABF9`; `PaymentTransitionReproduction.cs` SHA-256 `4219629D541A0F61E057455F71A6E6DF3C2C816BC4869F2889B313FD2F5E1F1E`.

## Results — identical in both runs

| Transition / independent fixture | Expected | Observed actual HTTP, fresh SQL and read API |
| --- | --- | --- |
| Full payment 1000 on invoice 1000, then status Voided | Payment voided; invoice reopens Issued/Overdue; paid 0, balance 1000; overdue collections include invoice | PATCH 200, payment Voided; **invoice remains Paid**, admin paid 0/balance 1000; **collections exclude invoice** |
| Partial payment 600, then Voided | No remaining collected money; invoice reopens Issued/Overdue; balance 1000; collections include it | PATCH 200, payment Voided; **invoice remains PartiallyPaid**, admin paid 0/balance 1000; collections do include it |
| Payment 800 + approved discount 200, initially Paid, then Voided | Discount retained; invoice reopens; balance 800; overdue collections include it | PATCH 200; discount/approval ledger unchanged, payment Voided; **invoice remains Paid**, admin paid 0/balance 800; **collections exclude invoice** |
| Separate Completed payment 600: generic status PATCH Reconciled without evidence | Reject or require valid reference/timestamp; do not create an evidence-free reconciled state | **PATCH 200**, SQL and payment-list GET show Reconciled with **null reference and null reconciliation timestamp** |
| Voided full payment: dedicated reconciliation with valid reference | Restoration policy must be explicitly established | PATCH 200, same payment becomes Reconciled with reference/timestamp; **POLICY-PENDING**, not a hard failure solely because restoration is allowed |

The invoice list and collections endpoints were both read with the real Admin token. Baselines prove that Paid invoices are excluded and an overdue partial invoice is included before transitions. After voiding, expected collectible balances are independently 1000 minus the retained adjustment; no amount remains collected in these one-payment fixtures. Accepting either Issued or Overdue avoids inventing an automatic overdue-status policy.

## Passing controls and assertion boundaries

- Invalid generic status returned 400 on all three void fixtures; captured invoice/payment/adjustment snapshots stayed unchanged.
- Repeating each void returned 200 and left the captured financial snapshot unchanged. This is a bounded repeat check, not proof of full idempotency or unchanged audit tables.
- Blank dedicated reconciliation reference returned 400 with an unchanged snapshot on the direct-reconciliation fixture.
- The dedicated reconcile route accepted a valid `QA-VALID-RECONCILE` reference (200), and fresh SQL confirmed reference and timestamp on the original payment. This validates the evidence control, not the separate collected-money aggregation already failing under BUG-DATA-0001.
- Every mutation was followed by a fresh no-tracking SQL read. Payment IDs/amounts/counts, adjusted amount and approval state were checked; accepted status responses were not mistaken for business PASS.

## Original sanitized failing output

Both counted runs emitted:

```text
FINANCE-RULE-003 FAIL: full void inconsistent=True; partial void inconsistent=True; adjusted void inconsistent=True; direct Reconciled missing evidence=True; voided restoration POLICY-PENDING HTTP=200; invalid status/blank reference/repeated void controls verified; independent SQL and read APIs used.
```

Observed invoice/payment identifiers at the failing transitions:

| Run | Fixture | Invoice ID | Payment ID |
| --- | --- | --- | --- |
| `789bc93635ec441b8436bfbc496eee02` | Full void | `7e104e5d-efc6-4ac3-9c26-af27a7c37eb7` | `a66253a2-6bea-4609-b2c5-09ca78fb28e1` |
| Same run | Partial void | `d2debbfa-89d2-4b6a-97d1-62179c902398` | `adce6d00-7471-4f04-98a6-6d317603edab` |
| Same run | Adjusted void | `6b683c3f-4756-4eec-a943-d0c1d61b6952` | `82dca00d-660d-4d58-a904-e8023b8616dc` |
| Same run | Direct Reconciled | `7f16496b-308e-418f-9431-374bde8a199a` | `be7e7f82-33c9-4cca-963b-b47ce812ea70` |
| `e9169638be7b4093a1dcaafb171c8111` | Full void | `15ce3f25-f9f3-46bd-babe-b7e2a4e0a561` | `c37960ef-df60-4cab-a862-e0e57834a399` |
| Same run | Partial void | `709856ea-14d5-4e4a-903a-f311f3c11be1` | `97f0c3ca-5051-41e1-a652-4c720df02373` |
| Same run | Adjusted void | `9460a925-b099-4516-8b1f-b2bc6271feb0` | `f44f3ada-c975-4d78-90c9-1efcd829e67e` |
| Same run | Direct Reconciled | `be53fc92-41e5-4b3b-8bad-87fd4a16803e` | `31b61ac1-2658-47c1-9714-534eaf428ce8` |

Both runs refused a mismatched cleanup marker before the callback; matching marker cleanup then removed the owned database, login and host folders. Exact labelled Docker containers were stopped/removed; only cached images remain. Synthetic ledger data cannot be recovered from these removed containers.

## Decision and limits

**BUG-DATA-0010: RUNTIME-REPRODUCED / OPEN P0.** Generic payment status changes do not recompute invoice state and permit Reconciled without the evidence enforced by the dedicated reconciliation route. This is distinct from the Completed-only aggregation defect.

Voided-payment restoration remains **POLICY-PENDING**. Existing acceptance is not proof of approval: Phase 2B needs an explicit restoration rule, required authority and evidence/audit expectations. Zero-net payroll policy also remains pending from the preceding case. No such policy was implemented in these reproductions.

No product controllers, existing student UI changes, schema, Azure configuration/data, commit or deployment were modified. Harness build passed with zero warnings/errors. After container cleanup, the existing API unit suite passed **45/45**, zero failures/skips; this does not override the five reproduced P0 integration failures. All five test/issue states and the pending Phase 2A handoff checkpoint passed consistency checks. Source whitespace checks and `git diff --check` passed; no run-labelled QA SQL containers remain. The historical Phase 1 fingerprint validator was not weakened or claimed to pass on this expanded Phase 2 tree. Multi-payment/concurrent changes, all state-pair combinations, cancelled invoices, repeated reconciliation, restoration authorization and browser flows remain broader regression coverage. The evidence does not imply these passed.

**5/5 P0 baseline reproductions have now completed twice, all FAIL / issues OPEN. Phase 2A is not yet formally closed:** consolidated evidence/hashes, runtime endpoint inventory and acceptance/policy handoff remain to be checked before Phase 2B repairs. Do not repeat the accepted Phase 1 audit or infer release readiness from the successful harness exit.
