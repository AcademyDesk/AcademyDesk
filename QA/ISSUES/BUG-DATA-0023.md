# BUG-DATA-0023 — Invalid activity deletion scope silently targets both audit stores

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | REPRODUCED — bounded local repair verified |
| Final verification |42 real HTTP-SQL cases/1019 existing backend rerun PASS; browser/critical OPEN |
| Severity | Major unintended audit deletion scope |
| Priority | P1 |
| Category | DATA |
| Module | PLATFORM |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /platform/control?tab=Activity%20logs; direct deletion API |
| API | DELETE /api/platform/activity-logs |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PLATFORM-AUDIT-001 |
| Evidence classification | Real Identity/HTTP/owned SQL; original static trace retained below |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PlatformControlController.cs:214 |
| Class/function | DeleteActivityLogs |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In disposable fixtures seed platform and academy audit rows inside/outside a chosen interval. As owner send an unknown nonnull scope such as AcademyAdmn with valid dates. Compare exact valid scope and explicit all-groups contract controls. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted deletion-scope allowlist; no deployment |
| Retest result |42 HTTP-SQL cases PASS: exact removals/survivors/success audit and no-write rejections;1019 backend rerun PASS |
| Regression result | Named/null scopes, half-open boundaries, authority/model-binding guards PASS; browser/cancel/critical OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In disposable fixtures seed platform and academy audit rows inside/outside a chosen interval. As owner send an unknown nonnull scope such as AcademyAdmn with valid dates. Compare exact valid scope and explicit all-groups contract controls.

## Expected

Unknown scope rejects without deletion. All-groups deletion requires the explicitly supported all-groups representation.

## Actual / evidence

Two negated string comparisons both evaluate true for an unknown scope, loading and deleting matching rows from both tables. Only date ordering is validated. Static finding; no logs deleted in this audit.

Source snapshot:

```text
213:         if (request.ToUtc <= request.FromUtc) return BadRequest(new { message = "Choose a valid start and end date." });
214:         var includePlatform = !string.Equals(request.Scope, "AcademyAdmin", StringComparison.OrdinalIgnoreCase);
215:         var includeAdmin = !string.Equals(request.Scope, "PlatformOwner", StringComparison.OrdinalIgnoreCase);
216:         var platform = includePlatform ? await db.PlatformAuditEntries.Where(x => x.OccurredAtUtc >= request.FromUtc && x.OccurredAtUtc < request.ToUtc).ToListAsync(token) : [];
```

## Suspected root cause

Invalid scope falls through to the broadest destructive target.

## Business impact and blast radius

Owner-authorized direct or stale-client deletion requests; no nonowner access or cross-role privilege bypass claimed.

## Related / required regression

PLATFORM-AUDIT-001: Real HTTP/SQL scope matrix with typo/whitespace/unknown/valid/null inputs, exact interval boundaries, cancel/no-request browser test and preserved out-of-range rows; invalid inputs must produce no deletion or success audit.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local successor — 2026-10-02

[Activity-deletion scope repair](../REPORTS/PHASE_2B_ACTIVITY_DELETION_REPAIR.md) supersedes the original runtime-NOT-RUN statements. Three unsafe scope strings reproduced deletion of both stores in owned synthetic SQL. The endpoint now rejects unsupported nonnull strings before loading/deleting/auditing; existing case-insensitive named values and frontend null/All contract retained.42 real Identity/HTTP-SQL cases PASS; exact removed IDs/full surviving rows, boundaries and12 actor-linked success audits;30 denied/invalid requests leave captured snapshots unchanged.1019 existing backend tests rerun PASS. Nullable DTO omission still maps to null/All, explicitly recorded compatibility rather than new explicit-presence policy. Owned SQL cleaned; previous sources/evidence/normal binaries, frontend and list-query semantics unchanged. No real logs/dev database/services/Azure/commit/deploy touched. Remains OPEN for browser confirmation/cancel, linked/critical/races/faults and wider date/legacy policy gates. Next BUG-DATA-0024 platform outstanding totals; Sol High.
