# BUG-DATA-0056 — Editing a payroll profile rewrites the worker name shown on historical payouts

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major payroll history attribution integrity |
| Priority | P1 |
| Category | DATA |
| Module | PAYROLL |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payroll; payout register |
| API | GET /api/academies/{academyId}/payroll/payouts; PUT /payroll/profiles/{profileId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PAYROLL-HISTORY-IDENTITY-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PayrollController.cs:39 |
| Class/function | Payroll.Payouts / UpdateProfile |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated SQL create an active profile named Worker A and record one synthetic payout. Read payout list and save its workerName/payslipNumber. Update the same profile workerName to Worker B, read payout list again and compare the existing payout. Also test deactivation, missing profile and worker-link change. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
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
