# BUG-DATA-0009 — Payroll profiles accept unchecked or mismatched worker links

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major payroll data integrity |
| Priority | P1 |
| Category | DATA |
| Module | PAYROLL |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payroll |
| API | POST profiles; PUT profiles/{profileId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PAYROLL-RULE-002 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PayrollController.cs:54 |
| Class/function | Valid / CreateProfile / UpdateProfile |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | As tenant A admin in isolated SQL, POST Staff with a nonexistent or tenant B StaffUserId and valid monthly amount/name. POST Teacher with both IDs null. PUT an existing A profile to a teacher belonging to B. Record response and persisted links; do not execute a real payout. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

As tenant A admin in isolated SQL, POST Staff with a nonexistent or tenant B StaffUserId and valid monthly amount/name. POST Teacher with both IDs null. PUT an existing A profile to a teacher belonging to B. Record response and persisted links; do not execute a real payout.

## Expected

Worker type determines exactly one valid same-tenant worker link on create and update; invalid/cross-tenant links reject without changing profile.

## Actual / evidence

Valid checks worker type/name/model/positive amounts, not required links. Create only checks TeacherId when supplied and never validates StaffUserId. Update validates neither link. PayrollProfile has scalar IDs and no worker navigation/FK configured in the reviewed model. Runtime reproduction pending.

Source snapshot:

```text
53:
54:     private static bool Valid(SavePayrollProfileRequest r, out string message) { message = ""; if (r.WorkerType is not ("Teacher" or "Staff")) { message = "Choose Teacher or Staff."; return false; } if (string.IsNullOrWhiteSpace(r.WorkerName)) { message = "Enter the worker name."; return false; } if (r.PaymentModel is not ("Monthly" or "SessionBlock")) { message = "Choose monthly or session-block payment."; return false; } if (r.PaymentModel == "Monthly" && r.MonthlyAmount is not > 0) { message = "Enter a positive monthly salary."; return false; } if (r.PaymentModel == "SessionBlock" && (r.AmountPerCycle is not > 0 || r.SessionsPerCycle is not > 0)) { message = "Enter sessions per cycle and payout amount."; return false; } return true; }
55:     private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
56:     private static PayrollProfileSummary Summary(PayrollProfile p) => new(p.Id, p.WorkerType, p.TeacherId, p.StaffUserId, p.WorkerName, p.PaymentModel, p.MonthlyAmount, p.AmountPerCycle, p.SessionsPerCycle, p.EffectiveFrom, p.IsActive);
```

## Suspected root cause

Worker identity invariant is absent from shared validation and differs across create/update.

## Business impact and blast radius

Unattributed or wrongly attributed payroll profiles and downstream payouts; not proof of cross-tenant data disclosure.

## Related / required regression

PAYROLL-RULE-002: HTTP/SQL matrix Teacher/Staff x valid/missing/unknown/cross-tenant/both-links, POST and PUT; verify unchanged rows on rejection and valid worker names/links on success.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
