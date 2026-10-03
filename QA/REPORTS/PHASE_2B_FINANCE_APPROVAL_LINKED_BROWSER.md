# Phase 2B — Linked Finance Governance approval browser/SQL check (2026-10-04)

## Scope and isolation

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. QA-only `BrowserApproval` mode extended the existing Payments browser harness without changing its default mode. Run `9bf2830534a047479fba15a8061a9c53` created one named, labelled disposable SQL Server container, database and runtime login. It seeded a 1,000 invoice with Reconciled 600 plus Completed 400, then a separate PendingApproval 200 discount. A second approved-adjustment invoice preserved the existing Payments-harness ledger controls. The API was exposed only through the loopback bridge at `127.0.0.1:49402` with a source-identical Finance Governance page and login page at `127.0.0.1:49401`.

The synthetic Academy Admin signed in through the copied `/login` page. Its redirect to `/dashboard` returned 404 because the source-only copy intentionally omitted that unrelated page; direct `/finance-governance` loaded with the saved local test session. No production data, user credentials, normal development database, Azure, or GitHub write was used. The runner's tenant-isolation preflight passed: two real admin logins, same-tenant reads, foreign GET/POST 403 and no foreign SQL write.

## Browser and ledger result

| Check | Result |
| --- | --- |
| Initial pending state | One 200 Discount visible and pending count 1. |
| Inline cancel | Form closed without PATCH; pending item remained visible. |
| Confirm approval | Exactly one real `PATCH /finance-adjustments/{id}/approval` returned 400. The page announced `Adjustment exceeds the remaining invoice balance.` as an alert, refreshed the queue, and retained pending count 1. |
| Reload | The same pending item was visible after a fresh page read. |
| Final SQL assertion | Invoice status Paid, total 1,000, adjusted 0, collected 1,000 from exactly two payment rows; adjustment still PendingApproval with no approved/applied timestamp. Harness exit 0. |
| Companion ledger | Existing approved-adjustment invoice stayed Paid with 200 adjustment, 800 collected, exactly two payment rows. |
| Mobile emulation | At 390×844, document/body scroll width 379 versus viewport 390; inline decision form remained visible without horizontal overflow. Physical device not run. |
| Browser console | No warning or error in the final linked browser check. |

The host asserted one approval attempt and one BadRequest, the pending adjustment and both exact ledgers before teardown. The run-owned database/login and container were removed; exact container listing is empty. Browser tab and frontend were stopped. The frontend page was copied byte-for-byte from the application into an ignored `.build-check` fixture; no application source changed in this slice. Build of `QA/tools/SqlHarness/SqlHarness.csproj` passed with 0 warnings/0 errors.

Local untracked screenshot evidence: `QA/EVIDENCE/finance-approval-linked-20261004/rejected-paid-invoice.png` and `mobile-approval.png`. It contains only synthetic fixtures.

## Remaining gates

BUG-DATA-0002 remains OPEN. This one browser and emulated mobile viewport do not settle over-adjustment/refund policy, physical Android/iOS behavior, all role/tenant scenarios, failure-injection matrix, or release readiness. Earlier concurrency/transaction evidence remains in [the guard report](PHASE_2B_APPROVAL_PAYMENT_GUARD.md). No main merge, GitHub push, Azure deployment, or production financial write.
