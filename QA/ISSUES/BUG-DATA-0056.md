# BUG-DATA-0056 — Editing a payroll profile rewrites the worker name shown on historical payouts

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded API/schema repair verified |
| Final verification | RETEST / NOT CLOSED; concurrency/browser/device/wider release gates OPEN |
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
| Retest result | Bounded34 native controls + migration preservation PASS; concurrency/browser/device NOT RUN |
| Regression result | 66 targeted tests (14 new +52 retained),14 retained SQL payroll rule cases/types/harness PASS; zero-net policy pending |
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
