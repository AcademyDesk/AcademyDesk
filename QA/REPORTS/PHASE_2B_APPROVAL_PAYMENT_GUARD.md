# Phase 2B — Approval versus payment transaction guard

Date: 2026-10-04 (Asia/Calcutta). Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`, starting at `f7986f0`. No change to main, GitHub, Azure, production data or credentials.

## Retained failing baseline

`PHASE_2B_APPROVAL_RACE.md` records run `d66c4ad4118c4001a2a3e9703ea1d7e6`: 5/5 concurrent pairs returned approval 200 and payment 201, then persisted collected 1000 against gross 1000 minus approved adjustment 200. `FinanceAdjustmentsController.Decide` did not serialize against payment creation or reject an approval that over-adjusted an already collected invoice.

## Bounded repair

`Decide` first identifies the academy-scoped invoice, then obtains the same SQL Server invoice `UPDLOCK,HOLDLOCK` used by payment creation. The normal finance filter owns the transaction; direct callers open and commit an owned transaction. The adjustment is re-read after acquiring the lock, so another decision cannot reuse a stale PendingApproval state. For approval, the controller sums current Completed/Reconciled payments and rejects when `total - previously approved adjustments - proposed adjustment - collected < 0`. The 400 response says “Adjustment exceeds the remaining invoice balance.” The rejected adjustment and invoice remain unchanged; the existing finance filter does not commit or audit a 400 result. A permitted approval saves the adjustment and invoice together. Rejection decisions keep their earlier behavior.

The original five-pair QA race was strengthened to require one of two exact serial outcomes, not merely `collected <= collectible`: (1) approval 200, payment 400, Approved 200, one Reconciled 600, PartiallyPaid/200 balance; or (2) approval 400, payment 201, PendingApproval/no approval timestamps, Reconciled 600 plus Completed 400, Paid/zero balance. Both require matching SQL and Admin API views and no 429. A separate approval-first control checks that an accepted 200 adjustment makes a later 400 payment fail with the established balance message and no extra row.

## Verification

| Check | Result |
| --- | --- |
| New focused unit baseline | 1 expected failure before fix: over-adjustment approval returned 200 instead of 400 |
| SQL harness build | PASS, 0 warnings/errors |
| ApprovalRace original shape | `50a9f525d4164ba1aa09db0368c03d71`: 5/5 guarded, payment-first |
| ApprovalRace strict + forced approval-first | `811706c0ed0a45b482737284791c572d`: 5/5 guarded plus approval-first control PASS |
| ApprovalRace final code (matching decision timestamps) | `5903c5aec0f8436c89b08b24e94d02b2`: 5/5 guarded plus approval-first control PASS |
| Existing Adjustment SQL | `2fd9aa616a804c0f90c6dcfd936571dc`: PASS |
| Existing AdjustmentRace SQL | `6b529d99c38b4e44b0bb8fa65155d976`: 5/5 PASS |
| Full API tests | 1,044/1,044 PASS, 0 skipped |

The initial concurrent *build and test commands* collided while writing an `obj` file. They were rerun sequentially; build and all tests passed. This was an orchestration failure, not a product test result. The SQL runner used a process-only PowerShell execution-policy override. Passing runs removed their exact owned database, login and container; no broad Docker cleanup was used. The pre-existing untracked `QA/EVIDENCE` directory was preserved.

## Remaining gates

`BUG-DATA-0002` remains OPEN. Product policy for over-adjustment, refund/credit treatment and any exceptional post-payment discount is not decided by this safety guard. Live browser/physical-device, broader role/tenant and full pre-Azure checks remain. This bounded result does not authorize merge, push or deployment.
