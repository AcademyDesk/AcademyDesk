# BUG-FUNC-0019 — Batch teaching-time JSON uses names the default parser does not match

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED — paired bounded local repair verified; OPEN |
| Final verification | 277 API /116 controlled frontend /two fresh Identity-HTTP-SQL runs x48 PASS; browser/device/all-linked/critical NOT RUN |
| Severity | Major batch creation blocked |
| Priority | P1 |
| Category | FUNC |
| Module | BATCH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /batch-setup |
| API | POST batches |
| Environment | Isolated local owned SQL2022/Testing; real middleware/Identity/model binding; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | BATCH-TIMES-001 |
| Evidence classification | Accepted static trace + real HTTP/SQL baseline and bounded local repair, not production/browser acceptance |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | One fresh baseline run; two fresh final-source SQL runs pass all48 cases each |
| Source | apps/api/Controllers/BatchesController.cs:58 |
| Class/function | Batches.Validate / batches.createBatch |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | [Batch teaching-time repair](../REPORTS/PHASE_2B_BATCH_TIMES_REPAIR.md), sanitized logs and exact source/assembly snapshots |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Select Monday and09:00 in batch setup with otherwise valid values. Inspect nested MeetingDaysJson containing day/startTime. Compare an isolated direct request with Day/StartTime and with no teaching days. |
| API response | Baseline casing400/null-entry500; final valid201/200, invalid400 structured message; role403/anonymous401 |
| Database before/after | Fresh SQL schedule string equality in row/response/GET; rejected requests preserve captured batch/session/governance/task/audit state |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Uncommitted local bounded repair; no push/deployment |
| Retest result | PARTIAL PASS — selected POST/PUT cases twice x48 with real Identity/SQL; actual browser NOT RUN |
| Regression result | 277 API /116 controlled frontend PASS; build/TypeScript PASS; existing page lint FAIL unchanged (2 errors/3 warnings) |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Select Monday and09:00 in batch setup with otherwise valid values. Inspect nested MeetingDaysJson containing day/startTime. Compare an isolated direct request with Day/StartTime and with no teaching days.

## Expected

UI-generated teaching-time JSON is accepted and round-trips without requiring manual casing changes.

## Actual / evidence

2026-10-01 current bounded repair: [report](../REPORTS/PHASE_2B_BATCH_TIMES_REPAIR.md). A fresh real HTTP/SQL baseline reproduces camel/mixed nested keys400 and null/mixed-null entries500 for both POST/PUT, without captured writes. Final sources accept Pascal/camel/mixed keys, reject null/invalid entries with sanitized400, and preserve raw valid JSON through row/response/GET. Optional missing/null/blank JSON remains supported; the form now sends null when no days are selected. Explicit empty arrays remain invalid. Two fresh runs x48 PASS, API277 and controlled frontend116 PASS. No browser/device/critical closure and no claim about the historical Teacher error.

### Historical audit and earlier observation (superseded by the result above)

2026-10-01 bounded runtime observation during [Batch preservation verification](../REPORTS/PHASE_2B_BATCH_PRESERVATION_REPAIR.md): [sanitized fixture response](../EVIDENCE/logs/phase-2b-batch-preservation-before-sql-camelcase.log) returns400 for nested lowercase day/startTime with "Select a valid class time for every teaching day." Corrected Pascal-case controls succeed. These failed setup runs are excluded from preservation pass counts and owned resources cleaned. Parser is unchanged/unfixed; full browser and optional/empty/mixed/malformed/null cases remain pending. Next bounded task uses Sol High and the accepted parser audit, not a new audit.

UI emits lowercase day/startTime, but the explicit JsonSerializer call has default case-sensitive options and record properties Day/StartTime. Unmatched fields are null and day validation rejects them. No selected days produces an empty array, also rejected. Global MVC JSON options are not passed to this separate parser. Runtime NOT RUN.

Source snapshot:

```text
57:             {
58:                 var sessions = JsonSerializer.Deserialize<List<BatchMeetingTime>>(r.MeetingDaysJson);
59:                 var validDays = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
60:                 if (sessions is null || sessions.Count == 0 || sessions.Any(x => !validDays.Contains(x.Day, StringComparer.OrdinalIgnoreCase) || !TimeOnly.TryParse(x.StartTime, out _)))
```

## Suspected root cause

Embedded JSON contract differs from ordinary MVC camel-case request serialization.

## Business impact and blast radius

Batch setup creation with either selected days or an empty selection; direct correctly cased JSON is a control.

## Related / required regression

BATCH-TIMES-001: Full HTTP/UI exact current payload, Pascal/camel/mixed case, empty/omitted JSON, malformed inputs and persisted schedule equality. Do not weaken invalid-day/time validation.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
