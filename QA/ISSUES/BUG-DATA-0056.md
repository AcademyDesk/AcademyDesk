# BUG-DATA-0056 — Editing a payroll profile rewrites the worker name shown on historical payouts

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded API/schema repair verified |
| Final verification | RETEST / NOT CLOSED; browser/device/direct/platform/wider release gates OPEN; bounded measured concurrency passed |
| Severity | Major payroll history attribution integrity |
| Priority | P1 |
| Category | DATA |
| Module | PAYROLL |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payroll; payout register |
| API | GET /api/academies/{academyId}/payroll/payouts; PUT /payroll/profiles/{profileId} |
| Environment | Disposable local SQL + real Identity/TestServer HTTP; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PAYROLL-HISTORY-IDENTITY-001 |
| Evidence classification | Frozen baseline reproduction + repaired unit/native SQL/legacy migration proof; no live browser/device acceptance |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | All three proposed historical failures reproduced in one owned baseline run; guarded final run has zero historical failures |
| Source | apps/api/Controllers/PayrollController.cs:39 |
| Class/function | Payroll.Payouts / UpdateProfile |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | QA/REPORTS/PHASE_2B_PAYROLL_HISTORY_IDENTITY_REPAIR.md; private QA/EVIDENCE/payroll-history-20261012/ |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated SQL create an active profile named Worker A and record one synthetic payout. Read payout list and save its workerName/payslipNumber. Update the same profile workerName to Worker B, read payout list again and compare the existing payout. Also test deactivation, missing profile and worker-link change. |
| API response | POST201, PUT200, GET200; role/tenant/missing/inactive rejects and same-action audit500 rollback verified; see report |
| Database before/after | Fresh-context all-row/field snapshots across Profiles/Payouts/Audits/Invoices/Payments; migration preserves pre-column orphan payout and NULL identity |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Scoped payroll-history feature commit succeeding 3f40fd04d1e118d16866e0a0f1c1eb7b6364337d; see Git history/report |
| Retest result | Bounded34 native controls + migration preservation +24 measured SQL overlap/audit pairs +four linked browser journeys24 register states PASS; physical devices/wider gates NOT RUN |
| Regression result | 68 freshly built targeted tests (66 retained +two UTC),27 handler checks,14 retained SQL payroll rules/types/lint/export83/harness PASS; zero-net policy pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated SQL create an active profile named Worker A and record one synthetic payout. Read payout list and save its workerName/payslipNumber. Update the same profile workerName to Worker B, read payout list again and compare the existing payout. Also test deactivation, missing profile and worker-link change.

## Expected

Historical payout identity remains the worker at the time of disbursement, or the system maintains an explicit immutable identity/version audit that makes the change explainable; a profile edit cannot silently relabel past pay.

## Actual / evidence

PayrollPayout stores PayrollProfileId and money/period but no worker-name snapshot. Payouts performs an inner join and projects current profile.WorkerName/WorkerType for every historical row. UpdateProfile can change both, so the same saved payout is returned with different worker identity; a missing profile removes the row from the list. Static source trace; runtime NOT RUN.

Source snapshot:

```text
38:         Ok(await db.PayrollPayouts.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.PaidAtUtc)
39:             .Join(db.PayrollProfiles.AsNoTracking(), payout => payout.PayrollProfileId, profile => profile.Id, (payout, profile) => new PayrollPayoutSummary(payout.Id, payout.PayrollProfileId, profile.WorkerName, profile.WorkerType, payout.PayslipNumber, payout.PeriodLabel, payout.SessionsCovered, payout.GrossAmount, payout.Deductions, payout.NetAmount, payout.Currency, payout.Status, payout.PaymentMethod, payout.Reference, payout.PaidAtUtc)).ToListAsync(token));
40:
41:     [HttpPost("payouts")]
```

## Suspected root cause

Historical payout summary is derived from mutable current profile fields rather than payout-time identity.

## Business impact and blast radius

Payroll register and downstream payslip attribution after worker corrections/reassignments or profile removal; no claim that a real payout was executed.

## Related / required regression

PAYROLL-HISTORY-IDENTITY-001: Real SQL/HTTP create payout, edit profile name/type/link, deactivate/remove profile, then fresh list/read. Assert immutable historical identity and retained payout visibility with audit trail; no real payroll disbursement.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Runtime reproduction and bounded repair — 2026-10-12

The original source excerpt above is retained as discovery evidence, not current behavior. Run05e1ea2aca0e4bf8a40558bf17275426 against the frozen pre-repair API reproduced Worker A becoming Worker B, Teacher becoming Staff, and an existing payout disappearing from GET after test-only owned profile removal.24 supporting controls passed; the three historical acceptance failures remained NOT ACCEPTED despite diagnostic exit0.

New payouts now snapshot worker name/type/linked identity IDs and the server recording timestamp from the locked profile. Profile edits and payouts share a SQL transaction/profile-first lock. GET scopes by the payout's academy without a mutable-profile join; legacy/incomplete identity is explicitly `LegacyUnverified`, never guessed from current profiles. `IdentityRecordedAtUtc` is recording-time provenance, not proof of a real bank disbursement. The five-column nullable migration does not backfill or modify old financial values.

Final run0932e043f3714333a106f29b5de235e1 passed34 history controls, same-action audit500 rollback and successful profile/payout recovery, legacy variants/positive foreign sentinel, unchanged five-table snapshots, authorization rejects and89+7 migrations; owned database/login/container removed, nine other containers retained, no429.14 retained payroll rule cases also passed; zero-net Paid semantics remain explicitly policy pending.66 targeted tests and TypeScript/harness build passed. Full details/commands/limitations: [repair report](../REPORTS/PHASE_2B_PAYROLL_HISTORY_IDENTITY_REPAIR.md).

Remain **OPEN**. Next: measured SQL-overlap profile-edit/pay/deactivation ordering, linked Payroll browser/physical devices/direct/platform/stale/critical and migration-backup/rollback/release gates. Existing payroll-link validation, duplicate-period/idempotency/sub-cent rules and legacy identity recovery require separate approved work; this repair is not their acceptance or a new financial policy.

## Measured concurrency successor — 2026-10-12

Latest linked-browser successor: [report](../REPORTS/PHASE_2B_PAYROLL_BROWSER_FEEDBACK.md), run921728d928a943e3a118b78d6c14f672,308.8s/89+7 migrations. Four real Identity/API/owned-SQL browser journeys320/1440px/light-dark/monthly teacher/session staff,24 register states: immutable historical DTOs after name/type/link/deactivation, four honest legacy labels, foreign exclusion,20 exact successful writes/four400/one audit500 rollback/same-details recovery/two audit controls/221 unchanged five-table snapshots/exact audits/no429 PASS. Null-reset and durable feedback repaired at actual handler/linked levels; GET UTC timestamp wire inconsistency repaired without altering stored ticks.27 handler/68 freshly built targeted tests/types/lint0/0/export83/harness0/0 PASS. Final owned resources removed/nine other containers preserved; two failed-run receipts retained and only their marked containers removed. Original24 measured overlap pairs retained, not rerun. Physical/direct/platform/stale/critical/migration-backup-rollback/release remain open. Monetary0.01→₹0 was separately reproduced and is NOT ACCEPTED; next currency/payslip0033 repair, then0009/wider gates. Issue remains **OPEN**.

The next SQL-overlap gate above is now bounded PASS:18 edit/pay pairs (six monthly, six SessionBlock, six deactivation), with both actual serialization orders observed in every mode. Two real PayrollProfiles UPDLOCK/HOLDLOCK HTTP commands were observed in the owned SQL blocking chain before each pair's release. Exact before-or-after identity/link/rate/session values, old payouts, foreign/finance rows, register parity and exact success audits were checked; no mixed state or429. Three audit-denied overlapped pairs returned500/500 with full five-table rollback; three same-profile recovery pairs passed.34 retained history controls/migration preservation/14 payroll rules and66 retained targeted tests also passed.

Runb056963c11194f54b512c976ec80dcaf,108.6s/89+7 migrations/exact owned cleanup; QA-only, no application/schema/permissions change. See [concurrency report](../REPORTS/PHASE_2B_PAYROLL_HISTORY_CONCURRENCY_AUDIT.md). Issue stays **OPEN**; NEXT linked Payroll browser/physical devices/direct/platform/stale/critical/migration-backup-rollback/release gates. This is not arbitrary interleaving/stress, duplicate-payment/idempotency, policy or production acceptance.
