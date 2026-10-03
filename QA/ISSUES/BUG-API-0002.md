# BUG-API-0002 — Successful write can be followed by an audit-save failure

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | Local finance, student/teacher create and explicit linked Update same-store boundaries PASS twice per batch; other domains/platform/browser/concurrency/critical pending |
| Severity | Major ambiguous outcome |
| Priority | P1 |
| Category | API |
| Module | SHARED |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | All academy-scoped mutations passing audit gate |
| API | Academy POST/PUT/PATCH/DELETE |
| Environment | Fresh guarded local Identity/MVC/SQL; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | API-RELIABILITY-001 |
| Evidence classification | Primary post-save audit fault and rejected-action branch reproduced twice; scoped local repair retested |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Finance and student/teacher create faults each reproduced 2/2; each scoped repair PASS 2/2 final runs |
| Source | apps/api/Security/AcademyAccessFilter.cs:97 |
| Class/function | ExecuteAndAuditAsync |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | PHASE_2B_AUDIT_SAVE_REPAIR.md and PHASE_2B_PEOPLE_CREATION_AUDIT_REPAIR.md, source snapshots and before/final SQL logs; historic excerpt retained |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In test SQL force an audit insert failure after a successful controller save. Observe API result and fresh domain record. Repeat action-level BadRequest handling. |
| API response | Before/final injected fault HTTP 500; rejected 400/404; recovered invoice 201; GET 200 |
| Database before/after | Before fault: invoice/payment/person persisted without audit; scoped repairs: no write on audit failure; successful create plus attributed audit persists |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted finance/result-status and action-level student/teacher creation transaction repair; no deployment |
| Retest result | Latest 26 linked +28 create +six finance cases PASS twice on fresh SQL; other domain/provisioning/platform post-save faults pending |
| Regression result | Latest API 178/178; 60 SQL cases PASS twice; prior 129 finance access cases historical, not rerun; critical/browser/concurrency NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Latest linked-update repair — 2026-10-01

[Linked student/teacher repair](../REPORTS/PHASE_2B_LINKED_PEOPLE_REPAIR.md) reproduces both Update actions returning 500 after person and two linked accounts persist on generic-audit INSERT failure, twice. Explicit IncludeIdentity enlists the existing Identity context in the same configured SQL transaction; original store/validation and audit faults now roll back captured person/accounts/audits. API 178/178, two final runs ×60 cases PASS; current cleanup/no deployment. This supersedes the historical Update exclusion below, not every cross-store/mutation path. Platform-flag bypass, provisioning/media/other mutations/concurrency/browser/critical remain OPEN. Next accepted BUG-DATA-0051 paired Update branch-validation gap, agreed Sol High.

## Historical action-level creation repair — 2026-10-01

[Student/teacher creation repair](../REPORTS/PHASE_2B_PEOPLE_CREATION_AUDIT_REPAIR.md) reproduces both Create actions returning 500 after persisting a person without audit, twice with guarded SQL permission fault. Method-only marker extends the existing domain transaction boundary to these two Create methods. API 163/163 and two final runs ×(28 people +six finance cases) PASS. Faults roll back person records; minimal/populated optional fields and fresh attributed 201 success are verified. Captured Identity/finance/notification state remains unchanged; current role/tenant/branch refusals preserve captured state and add no success audit.

OPEN: Student/Teacher Update are deliberately unmarked and still require coordinated domain/Identity repair; other domain mutations/provisioning/media/platform bypass/response-loss/concurrency/critical/browser remain pending. No account provisioning, permission broadening, schema/frontend change or deployment. Next paired linked student/teacher updates using existing BUG-DATA-0052/lifecycle mappings, agreed Sol High.

## Historical scoped finance repair — 2026-10-01

[Finance save/audit repair](../REPORTS/PHASE_2B_AUDIT_SAVE_REPAIR.md) reproduces the primary fault twice using run-owned AuditLogs INSERT denial after actual controller business saves. Local filter repair encloses domain-only ordinary finance mutations in one SQL transaction and checks pending action-result status for success auditing. Two final runs pass six fault/recovery/read/rejection cases and 129 role/tenant cases each; API 155/155 PASS. Rejected 400/404 and seven foreign-row refusals now add no generic success audit. Faulted invoice/payment writes roll back, rather than returning 500 after persisting.

OPEN: this is not a shared all-controller/cross-store solution. Non-finance domain writes, Identity-coupled student/teacher updates/provisioning, Blob/media transactions, platform-flag bypass, lost-response/commit faults, concurrency and full critical/browser gates remain. No audit exception is swallowed or permission broadened. Next bounded slice is domain-only student/teacher creation with action-level boundaries, agreed Sol High. No deployment/issue closure.

## Historical partial rejected-action observation — 2026-10-01

[Finance access regression](../REPORTS/PHASE_2B_FINANCE_ACCESS_REGRESSION.md) observes seven foreign-row controller refusals (five 404/two 400) on each of two fresh Identity/HTTP/SQL runs. All captured financial/notification rows remain unchanged, but generic audit count increases by one per refused action. Scope is the existing result-status/audit branch only; this is not the primary audit-insert failure after a successful business save. Keep STATIC-FINDING for that primary failure and NOT RUN for fault injection, atomicity/retry/durable client recovery. No audit/application change or issue closure. Next bounded task tests that primary failure through the real pipeline.

## Exact reproduction

In test SQL force an audit insert failure after a successful controller save. Observe API result and fresh domain record. Repeat action-level BadRequest handling.

## Expected

Business save and audit have defined atomicity; client is not told an already-committed operation simply failed. Failed actions are not audited as successes.

## Before-fix actual / evidence

Filter runs a second SaveChangesAsync after controller action; no encompassing transaction. It checks Response.StatusCode before IActionResult executes rather than inspecting result status.

Historical source snapshot (not current repaired filter):

```text
96:     {
97:         var executed = await next();
98:         if (executed.Canceled || executed.Exception is not null || context.HttpContext.Response.StatusCode >= StatusCodes.Status400BadRequest) return;
99:
```

## Suspected root cause

Post-action audit persistence and response lifecycle are treated as one successful operation.

## Business impact and blast radius

All audited mutations; retries can duplicate work.

## Related / required regression

API-RELIABILITY-001: Audit write fault + rejected action tests through full MVC pipeline, compare actual HTTP status and DB changes.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
