# Cloud/local finance reconciliation — 29 October 2026

Status: BOUNDED BACKEND RECONCILIATION VALIDATED LOCALLY. Production, combined
PENTA integration, current-source GitHub CI and issue closure remain open.

Worktree `D:\AcademyDesk-finance-sync`, branch
`codex/finance-cloud-reconcile-20261029`, base
`00d0d00f0264bb913065aa040a9f0ef0967771b6` plus this slice. No main merge,
application/customer database use, model experiment or Azure action.

## Resulting behavior

- Retain cloud's shared invoice-first transaction/lock, stale invoice refresh,
  deterministic invoice status and validated Voided-payment restoration.
- Bring forward the approved generic Refund/CreditNote restriction. Pending
  legacy documents remain visible with explicit financial meaning and reviewed-
  disposition metadata; neither approval nor rejection silently decides them.
- A reduction approval cannot reduce the invoice below Completed plus Reconciled
  collections and earlier applied reductions. Exactly remaining balance succeeds;
  one cent above fails before changing proposal, invoice, payment or audit state.
- Reject unsupported stored type, invalid stored amount/currency and new amounts
  outside exact decimal(18,2), instead of rounding or silently changing currency.
- Refresh an already tracked proposal after the invoice SQL lock. A second
  decision cannot reapply a proposal approved while the direct caller was stale.
- Keep cancelled invoice labels intact; a partially reduced overdue invoice stays
  overdue. Required domain/audit transactions and existing permissions remain.
- Payroll's positive-net, worker/period/provenance protections are unchanged.

No migration is added. Actual disposable runs apply the cloud history's84
application/7Identity migrations. The unpublished-local overlapping migration
history still requires its own qualified integration path.

## Validation actually completed

- Full unfiltered cloud-based API suite: **1,265 passed, zero failed/skipped**;
  31new reduction cases over the accepted1,234 payroll baseline.
- Rebuilt SQL harness with zero warnings/errors.
- Retained FinanceIntegrity module12cases plus **28new real SQL/HTTP cases**.
  Ten new races observe two actual invoice SQL lock waiters before release:
  five collection-versus-reduction and five competing-reduction attempts. Exactly
  one success and one400; total collected plus applied reduction stays1000;
  rejected rows/decisions do not persist; exactly one success audit.429 never
  qualifies. Stored-type/currency, precision, legacy, exact balance, overdue and
  stale tracked-proposal controls pass. Actual mandatory-audit failure rolls back.
- Retained Adjustment, Transition and Reconciliation modules pass. Transition
  retains cloud restoration's twelve fresh-SQL/readback cases, including both
  restoration paths, adjusted balance, overbalance and cancelled refusal.
- Ported original CollectionRaceRegression.cs **byte-for-byte**, SHA256
  `9b83aaea418c0abc9f61cea71b1b19ad5a5c9dc83b03b9f5164c09d7e35e034a`.
  Added explicit CollectionRace/AdjustmentRace dispatch and retained every
  assertion. **5/5 regular and5/5 adjusted pairs** pass: exactly201/400,
  exact existing rejection message, two payment rows, collected1000/800,
  invoicePaid and unchanged approved200adjustment where applicable. No429.
- Existing CI SQL module list now includes both retained race modules. This is
  implementation/static validation, not a new GitHub execution claim.
- Both windows restored the exact original PENTA container IDs/images, healthy
  services/API200 and complete starting Docker inventory. Only run-owned SQL
  resources removed; lease released. No downloads, package acquisition or model
  loading/training/inference experiment/activation. Offline cached restore does
  not establish a new online dependency-security audit.

First window: `2daf8fcc5eed4c76a3e0f4d516b801fa`.

| SQL module | Actual run |
| --- | --- |
| FinanceIntegrity | `3e96d98d58b94c39bcfa677d802be250` |
| Adjustment | `4fac902c7dba49899e136024ce107c59` |
| Transition | `228b8d1befcc489fa27ed629dd67cbf7` |
| Reconciliation | `a2cecca871c841d391eaab87b12e76f5` |

Original-race successor window: `424f7aba9d9a48d998c682d7494a88f5`.

| SQL module | Actual run |
| --- | --- |
| CollectionRace | `be5630ac39314458848110c592fb2a9b` |
| AdjustmentRace | `184444461f2344949b29e97211d3ac35` |

The successor reuses the full API TRX only after exact application/test source
hashes match the first window. It rebuilds/reruns the changed harness. First-window
SQL results belong to their recorded harness revision; no unchanged old result is
relabelled as a newly executed final-harness run. Exact-source release CI remains
required. Local linked receipt:
`.build-check/finance-reconcile/qualified-result.json`. Native new reduction proof:
`QA/EVIDENCE/finance-reconciliation-sql-3e96d98d58b94c39bcfa677d802be250/result.json`.
All raw evidence remains local and excluded from publication.

## Remaining work

BUG-DATA-0001/0002 stay OPEN: current live payment browser/physical-device and
broader critical closure gates are not established here. Other P0s stay open.
Bring forward existing PENTA/security/Manual source and tests from the published
preservation branches; reconcile controllers, services, migrations, consumers,
visuals and CI coherently. Do not replace the preserved3,183-test suite with this
cloud-based1,265 suite or add their overlapping totals. Qualified real Mini,
remaining conversational workflows, all-role/device/privacy/security, restore/
rollback/staging and release acceptance remain open. Owner stop before candidate
model loading remains active. Next: Sol High for consequential PENTA integration;
Sol Medium for settled bounded UI/documentation.
