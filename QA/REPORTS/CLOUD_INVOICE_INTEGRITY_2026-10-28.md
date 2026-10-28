# Invoice write boundaries and authoritative status — 2026-10-28

MODEL: Session Codex model; no override/delegation.
TOOL: Codex cloud terminal and ordinary GitHub Actions web metadata.
REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/release-export-security; base52495e2a68923fd1325c8731307aedcaec195c32.
ENVIRONMENT: Local controller/InMemory regression; prepared real-SQL CI cases.
TASK: Continue critical balance/transition races and BUG-DATA-0055 invoice labels.
MAY MODIFY: Invoice/payment/adjustment writers, focused tests/SQL module/CI/docs.
MUST NOT MODIFY: Customer data, financial policy decisions, schema/RBAC/deployment.
VALIDATE: Exact ledger/status/no-write boundaries; full API; SQL harness compile;
new deterministic races must execute before concurrency acceptance.
STOP WHEN: Tested checkpoint pushed and newer CI result awaited while other work
continues.
NEXT ROUTE: Codex for real CI interpretation, routing/access and remaining gates.

## Repair

`InvoiceWriteScope` takes a tenant/invoice-scoped SQL update/hold lock before
loading mutable payment or adjustment state. All collection, void, reconciliation,
restoration, adjustment-decision and manual invoice-status actions use that order.
Normal actors reuse the existing finance/audit transaction, holding the lock until
its commit/rollback. Platform-owner/direct callers start and dispose an owned
transaction. Already tracked unchanged SQL invoices reload under the acquired
lock; unsaved tracked changes fail closed instead of being discarded. InMemory
fixtures retain their ordinary behavior and do not prove locking.

Manual invoice status changes now require the existing collected/adjusted balance
and UTC due-date rules. Paid cannot be manufactured for unpaid invoices;
contradictory PartiallyPaid/Issued/Overdue labels reject. Explicit cancellation
remains supported and is canonicalized. New collections reject cancelled invoices;
adjustment approval preserves cancellation. Invoice-list and collection sums count
same-tenant Completed/Reconciled rows only. No new adjustment cap/refund rule or
payroll policy was invented.

## Local results and prepared SQL proof

- Initial invoice suite:31 cases,22 failures/nine passes. Two failures were QA
  assertions requiring a JSON400 body although the existing invalid-status
  contract permits empty400. Corrected those assertions without changing that
  API behavior. Twenty product failures were reproduced.
- Final33 invoice/collection/cancellation cases PASS. Full API **1,141 PASS**,
  zero failed/skipped. No SQL concurrency claim follows from those tests.
- New `--audit-finance-integrity` module compiles with zero warnings/errors and
  is wired into the existing CI/owned runner. Twelve prepared cases: six real
  paired SQL races (collection/restore, duplicate/two adjustments, void/reconcile),
  three audit-fault rollbacks, false-Paid no-write, cancelled-collection no-write
  and stale tracked-invoice refresh. Both concurrent requests must be observed
  waiting on the held Invoices UPDLOCK before release; overlap alone is rejected
  as proof. Fresh ledger/audit readback is required.
- Existing transition SQL assertions now expect canonical Cancelled rather than
  the previous lower-case storage artifact. Their cancellation/no-write checks
  remain. Actionlint and PowerShell runner parsing PASS; diff checks PASS.
- CI now cancels outdated push/PR verification runs by workflow/ref. Manual
  deployment verification is not cancelled by that setting.

## Newly observed remote baseline

The ordinary GitHub Actions overview is accessible even though api.github.com
remains blocked. It reports [verification40728363572](https://github.com/AcademyDesk/AcademyDesk/actions/runs/40728363572)
**COMPLETED / SUCCESS** for exact commit52495e2. Its source workflow requires the
API/audits, guarded SQL finance/tenant/private-file/private-Blob checks, full
frontend suite with freshly generated communication SQL evidence, builds and API
container startup/failure smoke. Detailed logs still request sign-in; individual
case counts/artifact contents have not been read. This verifies the preceding
checkpoint, not the newer invoice changes in this report.

The cloud's last full frontend result remains1,109/1,112 PASS, three missing SQL-log
failures. Do not fabricate/import unreviewed evidence to change that count. All
new invoice SQL concurrency cases are **NOT RUN locally** and need their own
successful CI before acceptance. Existing P0/P1/browser/device/restore/critical
requirements remain OPEN. No issue closed, main merge, environment publication,
configured/customer DB access or Azure deployment was performed.
