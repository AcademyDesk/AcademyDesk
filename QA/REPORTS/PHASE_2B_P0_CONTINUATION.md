# Phase 2B — P0 continuation and approved-adjustment collection race

Date: 2026-10-03. Branch/worktree: `codex/enterprise-p0-continuation`, `D:\AcademyDesk-codex-p0`, forked from clean `cursor/certificate-validation` commit `bff83ca108b24833cc0f8d03684258337630eb80`. `D:\AcademyDesk` main and its untracked `QA/EVIDENCE` were not changed. No push, merge, Azure deployment, production database or credential operation.

## Takeover classification

- `origin/main` and primary main were both `f7af512f884ca5fe8ac3ddb287c852492592e709` at inspection/fetch. The Docker branch `cursor/docker-runtime-validation` was clean at `beb3f1c`; its documented single-container API/static-web/SQL runtime gate is **ready for review**, not automatically accepted as a full Docker, security or release gate. The report retains npm audit, DataProtection key, migration-upgrade and wider auth/browser follow-ups.
- The certificate branch was clean at `bff83ca`. Its repaired concurrent collection gate had already passed 5/5 plus sequential reconciliation and 1,042 API tests. BUG-FUNC-0032 still has the three-page official PDF, null issue-audit actor and physical-device gaps.
- BUG-DATA-0001, BUG-DATA-0002, BUG-DATA-0003, BUG-DATA-0010 and BUG-SEC-0001 remain P0/OPEN. No issue was closed in this slice.

## Fresh bounded regressions

`dotnet build QA/tools/SqlHarness/SqlHarness.csproj --verbosity quiet` passed with zero warnings/errors. On fresh run-owned Docker SQL, real Identity/HTTP/tenant controls and exact owned cleanup:

| Module | Run ID | Result |
| --- | --- | --- |
| Adjustment | `87241ab7f5834ad397f5d89ca43d7052` | PASS; adjusted guard, mixed statuses, cent boundaries, pending/rejected controls |
| Transition | `76bd09af6ebe430996b51aadac20f76b` | PASS; void/reconciliation bounded cases; restoration policy still pending |
| Payroll | `7b7e3b20b6044ffe85d5f0c36d399c4a` | PASS; negative-net rejection; zero-net policy still pending |
| New AdjustmentRace | `34bba6beae6745ada1abf22005c5ff62` | PASS 5/5; details below |
| Original CollectionRace rerun | `902ccc02aeb845a4ba774b3179ed73c9` | PASS 5/5; no weakening of original case |

The new `AdjustmentRace` module keeps the same parallel HTTP pattern as `CollectionRace` but uses a separate fresh run: invoice 1000, approved Discount 200, Reconciled payment 600, then simultaneous payments of 200 and 200. Every one of five pairs produced exactly one 201 and one 400 with `Payment exceeds the invoice balance.`; no 429. Fresh SQL showed collected 800, two payment rows, Paid status and one intact approved adjustment. The original unadjusted module still produced exactly one 201/one 400, collected 1000, two rows, Paid, no 429 in five pairs. The guards run through the real transaction/filter/SQL route. The new test changes only the QA harness, not product behavior or business policy.

`git diff --check` passed. The broader `node QA/tools/validate.cjs` could not run in this isolated worktree because the intentionally local/untracked `QA/EVIDENCE/logs/observed-checks.json` is absent here; it was not copied from or deleted in the primary checkout. This is an evidence-availability limitation, not a passing QA validation claim.

Two early test-setup failures were not product findings or counted runs: the new switch was initially omitted from harness argument validation; after that, the unadjusted-only snapshot reader rejected the intentionally adjusted fixture. Both harness wiring issues were corrected and their exact labelled disposable containers were verified and removed. No unrelated container was pruned.

## Browser and open boundaries

The computer-use browser runtime could not initialize (`failed to write kernel assets: The system cannot find the path specified`). No live Payments-page, console/network, responsive or physical-device PASS is claimed. A [browser-only Cursor handoff](../HANDOFFS/CURSOR_PAYMENTS_BROWSER.md) is prepared for Composer 2.5, while Sol High remains engineering owner.

The approved-adjustment test does **not** race approval against a payment. FinanceAdjustmentsController.Decide does not currently share the invoice-row lock used by payment creation, and over-adjustment/refund handling requires an explicit business policy. Do not infer this gate is passed. Likewise, voided-payment restoration, zero-net payroll, private media breadth and pre-Azure release gates remain open. Continue P0 before P1, preserving the historical failing evidence.
