# Phase 2B — Promotion decision terminal integrity

2026-10-01. Sol High. This bounded local repair accepts the existing [BUG-DATA-0032](../ISSUES/BUG-DATA-0032.md) finding without repeating the broader Astra audit. The issue and Phase 2B remain OPEN; this is not production or release acceptance.

## Repair and test result

[BatchPromotionsController](../../apps/api/Controllers/BatchPromotionsController.cs) now treats `Approved` and `Rejected` as terminal decisions. Only a Pending record can be decided. Approval requires its exact active source enrollment and no active target enrollment, then completes the source and creates one target enrollment. Rejection only finalizes the promotion. Both decisions use the established domain save/audit boundary; a parameterized per-promotion SQL application lock serializes competing decisions. Platform-owner bypass takes its own SQL transaction while retaining the existing no-audit bypass policy.

The [baseline log](../EVIDENCE/logs/phase-2b-promotion-baseline.log) records 6 product failures plus one then-expected atomic-boundary assertion among 13 focused tests. The final [backend log](../EVIDENCE/logs/phase-2b-promotion-suite.log) and [TRX](../EVIDENCE/promotions/promotion-suite.trx) show 376/376 passed with no skips, including [13 new focused tests](../../tests/AcademyDesk.Api.Tests/BatchPromotionDecisionTests.cs). In-memory tests do not by themselves certify auth, SQL transaction or concurrency behaviour.

## Real HTTP/SQL evidence

The [isolated harness](../tools/SqlHarness/BatchPromotionDecisionRegression.cs) passed 26 exact cases in each independent run: [run 1](../EVIDENCE/logs/phase-2b-promotion-sql-run1.log) and [run 2](../EVIDENCE/logs/phase-2b-promotion-sql-run2.log).

- Valid Approve and Reject, all four terminal replay combinations, invalid decisions, stale source and already-active target rows have full promotion/enrollment/audit row-boundary checks.
- Foreign-ID and foreign-route hiding, foreign Admin, Teacher, anonymous and scoped-list controls use real Identity and MVC authorization.
- AuditLogs INSERT denial produces 500 with no domain write; a subsequent request confirms recovery.
- Three two-request races hold the exact application-lock resource until SQL DMV evidence proves two waiting requests. Approve/Reject, Reject/Approve and duplicate Approve each yield exactly one 200 terminal write and one 409 conflict. A held-lock timeout yields 503 and no write.
- Platform-owner success and terminal replay confirm the owned transaction fallback. The final logs prove generated loopback-only SQL database/login cleanup after every run; normal dev database, Azure, Blob and customer data were untouched.

## Limits and next

No browser/device interaction, full promotion creation workflow, broader enrollment/transfer lifecycle, existing-record remediation, load testing, critical suite or release certification was performed. No commit, GitHub push or Azure deployment occurred. The source includes a dedicated wrapper mode in [Run-ReconciledPayment.ps1](../tools/SqlHarness/Run-ReconciledPayment.ps1) and harness dispatcher entry for repeatable local verification.
