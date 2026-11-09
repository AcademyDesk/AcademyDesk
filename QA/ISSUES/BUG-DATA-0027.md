# BUG-DATA-0027 — Trial booking moves converted leads back into the pipeline

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / Cloud source repair verified |
| Final verification | C27 scoped Cloud HTTP/UI/browser PASS; real SQL/races/audit/device and full critical acceptance pending |
| Severity | Major lifecycle inconsistency |
| Priority | P1 |
| Category | DATA |
| Module | SALES |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /trial-bookings; /leads; /sales-marketing |
| API | POST sales-marketing/trials |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SALES-TRIAL-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/SalesMarketingController.cs:53 |
| Class/function | CreateTrial |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Convert a synthetic lead successfully, then select that lead in trial booking and submit a valid date with optional teacher omitted. Compare fresh lead stage/link and pipeline/dashboard. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | `f377bb17906542d7dfccae144e84ea2d345c1290` (C27 feature return; no deployment) |
| Retest result | C27 same36 API cases PASS; expanded53 HTTP/InMemory and29 TSX/helper checks PASS;40 native synthetic cases PASS |
| Regression result | Final combined API2,325/2,325 PASS;frontend2,314 PASS/3 genuine missing-SQL-receipt failures;SQL/device/critical acceptance pending |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Convert a synthetic lead successfully, then select that lead in trial booking and submit a valid date with optional teacher omitted. Compare fresh lead stage/link and pipeline/dashboard.

## Expected

Converted leads retain their terminal state; any permitted post-conversion trial must not regress the lead pipeline. Invalid transition rejects before writes.

## Actual / evidence

Trial picker includes all leads and CreateTrial only validates lead existence/academy. It unconditionally sets Stage to TrialBooked, retaining ConvertedStudentId. Ordinary UpdateStage explicitly forbids changes after conversion. Runtime NOT RUN.

Source snapshot:

```text
52:         db.TrialClassBookings.Add(trial);
53:         var lead = await db.Leads.FindAsync([r.LeadId], t); if (lead is not null) lead.Stage = "TrialBooked";
54:         await db.SaveChangesAsync(t);
55:         return Ok(trial);
```

## Suspected root cause

Trial side-effect bypasses the terminal-state guard present in direct lead stage updates.

## Business impact and blast radius

Converted leads can be counted as both converted and trial-booked, with contradictory disabled pipeline controls.

## Related / required regression

SALES-TRIAL-001: Full HTTP/UI unconverted/converted/Lost/Won lead matrix, completed/cancelled/repeated trials, concurrent conversion/booking and no-write assertions on rejected terminal transitions.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## C27 source repair and scoped verification — 9 November 2026

The original source finding above is retained as historical evidence. C27
reproduced the HTTP defects (36-case original:19 PASS/17 FAIL), fixed the
stage/conversion/trial boundary and least-privilege teacher lookup, and verified
53 current HTTP/Identity/InMemory controls,29 actual TSX/helper controls and40
native Chromium scenarios. Original page controls were8 clock PASS/21 page
FAIL. Browser faults and persistence are synthetic, not SQL acceptance.

[Exact C27 source/result boundary and ordered Desktop SQL/race/audit/device
handoff](../HANDOFF/C27_SALES_TRIAL_INTEGRITY_2026-11-09.md).

Remain OPEN. Legacy Converted-without-link can use actual Convert to create a
real student; no historical rows were silently repaired. Ordinary unconverted
Won/Lost and optional-teacher trials are retained. SQL serialization, required
actor-audit rollback including Platform Owner, actual role/browser/device,
legacy dashboard and all linked critical/documented acceptance remain pending.
No model loading/activation, main merge, Azure deploy or production qualification.
