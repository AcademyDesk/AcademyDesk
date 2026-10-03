# Phase 2B — Reconcile/void ledger guard

Date: 2026-10-03. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`, starting at `a3a03d2fb42e3fe06017bac2b8071bebd23e8e52`. Only local branch code, QA runner and documentation changed. Main, GitHub, Azure, normal development database, production credentials and unrelated containers were untouched.

## Gap and bounded repair

`PaymentsController.Reconcile` previously read a payment and changed it to `Reconciled` without the invoice-first lock used by Create and Voided. If another request voided the same payment first, reconciliation could leave a collected `Reconciled` row with a stale `Overdue` invoice. A separate sequential path could reconcile a previously Voided payment after another payment filled the balance, overcollecting the invoice. The old dedicated route accepted explicit restoration; its business authorization is not approved or changed by this repair.

Reconcile now starts an owned SQL transaction only when the finance filter has not already supplied one, takes the academy-scoped invoice `UPDLOCK,HOLDLOCK`, then re-reads the payment. When the current row is Voided, it checks other Completed/Reconciled money against the adjusted collectible amount. It returns 400 with the existing “Payment exceeds the invoice balance.” message if restoration would overcollect, without mutating payment, evidence or invoice. Otherwise it updates invoice status to `Paid` or `PartiallyPaid` in the same transaction as the payment/evidence change. A Cancelled invoice keeps its status. The existing Completed-to-Reconciled evidence path is preserved. This is a financial consistency guard, **not** approval of a Voided restoration business policy.

The dedicated `ReconcileVoidRace` harness uses authenticated real HTTP and a fresh owned SQL database. Five pairs concurrently reconcile and void the same paid payment (three gross 1000, two with approved 200 adjustment). Each requires two 200s, no 429, intact reconciliation evidence, and exact agreement among persisted payment, invoice, Admin paid/balance view and collections. Either serialized winner is valid: Reconciled/Paid or Voided/Overdue. It also tests void-then-explicit-reconcile restoring a Paid invoice and void/replacement/reconcile returning the exact 400 message with the original Voided row unchanged.

## Verification

| Check | Result |
| --- | --- |
| SQL harness build | PASS, 0 warnings/errors |
| `ReconcileVoidRace` real Identity/HTTP/SQL | `355c861d031440398dbd53cb5d983dd9`: 5/5 races, restore and replacement controls PASS |
| `TransitionRace` existing two-void and create/void | `3d79572fe9d446f1993d2838663cc11a`: both matrices 5/5 PASS |
| `Transition` sequential/evidence | `01a2ef0f2655450fbdde6f54f944d5fa`: PASS |
| `CollectionRace` reconciled 600 plus two concurrent 400s | `7fc77e56e95f4cada40238da291460fe`: 5/5 PASS |
| API unit/integration suite | 1,042/1,042 PASS, 0 skipped |

The first combined transition run `62d2375ba6874ee1a132e30f1600d1d5` passed the pre-existing race checks but hit HTTP 429 in the newly appended fixture. The new cases were isolated into their own module. The first isolated run `9c8dbd99009f41cba75659a8335339e4` failed argument validation because the switch was missing from the harness allow-list; this QA-only dispatch error was corrected. Neither run counts as product failure or acceptance evidence. Their exact owned containers were inspected for name, run label and loopback port before being stopped/removed; successful runners removed their own SQL database, login and container. No broad Docker prune was used.

The API test fixture that previously stored a payment with a nonexistent invoice was corrected to create a real invoice; all 1,042 tests then passed. No test expectations or race assertions were weakened.

## Still open

`BUG-DATA-0010` remains OPEN. Product owner must decide whether a Voided payment may ever be restored through explicit reconciliation (and what role/audit rules apply). Generic Voided-to-Completed, other transitions and cancelled-invoice policy require separate coverage. The live Payments browser, physical-device, broader authorization/isolation, fault and full pre-Azure release gates are not certified by this bounded SQL check. No merge, push or deployment is authorized by this report.
