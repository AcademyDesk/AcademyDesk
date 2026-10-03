# Phase 2B — Concurrent payment-void invoice status repair

Date: 2026-10-03. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`, starting at `0c893fa`. No main checkout, Azure, production database, credentials or unrelated container was changed.

## Reproduction and root cause

`BUG-DATA-0010` had a sequential void/evidence repair, but two requests voiding different collected payments on the same invoice could each sum the *other* still-collected payment before either write committed. Each then wrote `PartiallyPaid`. A real Identity/HTTP/disposable-SQL five-pair baseline proved this in all five pairs (`08d4f5b7d6b34ef3b7fa07ebc64d9973`, exit 1): both PATCH requests returned 200; the Reconciled 600 and Completed 400 rows both ended Voided; SQL invoice status remained `PartiallyPaid`, while the Admin view showed paid 0/balance 1000. The invoice was seven days past due. This is not a synthetic in-memory-only assertion.

## Bounded repair

`PaymentsController.UpdateStatus` now obtains the academy-scoped invoice row with SQL Server `UPDLOCK, HOLDLOCK` for a Voided transition. The normal finance access filter already owns the transaction; direct/platform calls open one if needed. It then *re-reads* the payment after acquiring the invoice lock, calculates remaining Completed/Reconciled money, saves the Voided row and invoice status, and commits the owned transaction. This matches Create's invoice-first lock order and serializes voids across app instances without a process-local mutex. Invalid-status and generic Reconciled responses, repeated-void idempotence, Cancelled preservation, historical reconciliation evidence and non-SQL unit behavior remain unchanged. No restoration/over-adjustment policy was introduced.

`TransitionRace` is a separate strict harness module. It uses the existing real SQL/Identity/tenant fixture and five simultaneous two-void pairs: three gross invoices and two invoices with an approved 200 adjustment. Each starts Paid with Reconciled 600 plus Completed 400 or 200, then voids both rows concurrently. It requires two HTTP 200s, no 429, exactly two Voided rows, intact reconciliation evidence/approved adjustment, SQL invoice `Overdue`, Admin paid 0 and balance 1000 or 800, and overdue collections inclusion. The existing `Transition` and `CollectionRace` modules were not weakened.

The same module now also runs five concurrent create-versus-void pairs on gross/approved-adjustment invoices. A prior Reconciled 600 is voided while a 400 or 200 payment is posted. It requires HTTP 200/201, old row Voided with historical evidence, exactly one new Completed row, collected 400 or 200, SQL/Admin `PartiallyPaid`, balance 600 and collections inclusion. This exercises another ordering against the shared invoice lock without deciding the distinct Voided-restoration policy.

## Verification

| Check | Fresh result |
| --- | --- |
| SQL harness build | PASS, 0 warnings/errors |
| `TransitionRace` unadjusted post-fix | `42dde8d5a7e040718b96a7043e316f77`, 5/5 PASS |
| `TransitionRace` final mixed gross/adjusted | `09080e6c2b9c40aa86dc2c82a16866a2`, 5/5 PASS |
| Existing `Transition` | `b0ff1b3939294c6b9c69b4d26f5d4867`, PASS |
| Existing `CollectionRace` | `f437a282eb614893be48609b2b28219f`, 5/5 PASS |
| API tests | 1,042/1,042 PASS, 0 failed/skipped |
| Extended `TransitionRace` (two-void plus create/void) | `839ffd89254449deb7de71493e29dd60`, both matrices 5/5 PASS |

The SQL runner used a process-only PowerShell execution-policy override on this host. Its exact run-owned database/login/container cleanup completed on passing runs. The failing baseline container was verified by exact name, run label and loopback binding before removal. No broad Docker prune was used.

## Still open

`BUG-DATA-0010` remains OPEN: Voided→Completed or Voided→Reconciled restoration policy is not decided; reconcile/void and other untested interleavings, payment-specific role/tenant coverage, live browser/physical-device checks and the full critical/pre-Azure gates remain unaccepted. This report does not certify all possible finance transitions or authorize an Azure deployment.
