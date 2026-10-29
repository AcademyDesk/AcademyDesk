# BUG-SEC-0006 — Minor guardian portal-access revocation is overridden by age rule

## Selected-child presentation successor — 24 October 2026

[Current report](../REPORTS/GUARDIAN_SELECTION_CACHE_2026-10-24.md): actual load
callback baseline11cases/10fail/1pass reproduced old-child retention/stale-response
overwrite/unhandled denied read. One portal page repaired: clear old state while
loading, generation-check transport+JSON, contain denial/error, clear auxiliary
lists and invalidate on unmount.11new cases and51combined retained read-state
checks PASS. Fresh build627files/typecheck/targetlint/48synthetic responsive
Student-Parent cases and exact service-window restoration/inventory PASS.
Corrected native two-child response-delay/Admin revoke-regrant browser run now
qualified:50HTTP checks/four real390/1440light-dark guardian journeys/eight
Admin writes/14table read snapshots/four PNG hashes, no429/page errors/overflow.
Exact original services healthy/full inventory/cleanup/lease and independent
qualifier PASS. First QA locator timeout retained/exact restoration PASS.
API/domain permissions and earlier backend repairs unchanged.
Existing90/55/queued/reload native evidence remains historical to its exact
binary/export; no blanket guardian/device/full-security or issue closure.
Idle already-rendered view without a new read/navigation and current Me selector
refresh/custom grants/push invalidation remain separate; do not claim immediate
live revocation from the selected-child loading/denial repair.

## Bounded linked guardian reload UI — 2026-10-13

[Report](../REPORTS/PHASE_2B_LIFECYCLE_LINKED_BROWSER.md): QA-only fresh run4d004a61979c48a8a1623fe163461d8b (262s), four linked guardian journeys at320/1440px/light-dark, real minor grant/revoke/regrant Admin PATCH/eight flag and grant-revoke timestamp-state checks with exact actor/tenant/route audits. Revoked Me200/empty permitted children gives denial UI with no protected heading/KPI/child selector/forms; independent own-child403 excludes protected markers. Same original token restores actual permitted child after reload. Combined eight Teacher/guardian journeys/24 states/16 controls/122 unchanged15-field reads/no429/90+7/exact owned cleanup, nine original containers retained. **OPEN/RETEST**: finance-only and already-rendered/live-context browser, other routes/physical/wider security-release remain separate. Original90 native/40 queued matrix retained, not rerun; no app change. Failed QA teardown evidence retained/redacted, fresh complete gate passed; inspected mobile denial readable, header truncation queued Batch2.

## Queued-before-grant-query successor — 2026-10-13

[Report](../REPORTS/PHASE_2B_LIFECYCLE_QUEUED_AUTHORIZATION.md): QA-only run27e22382a60040608b50b5075c51bfea,174.8s passed original90 cases and10 controlled queued portal/finance revocations/20 denied-restored reads/20 real own-Admin PATCH writes. Committed SQL/response five flags/timestamp ticks, immutable link identity/relationship/other grants and exact success actor/tenant/route audit verified. Same guardian token's queued own details/invoice request gets403 without selected protected content markers or changes to15 snapshot fields; real regrant restores200. No429;90+7/non-admin runtime/negative-marker/exact owned cleanup. Failed overlong synthetic invoice seed retained; only prefix shortened, constraint/assertions unchanged and complete gate rerun. **OPEN/RETEST** for browser/device, already-admitted work, Identity/role/stamp races/other routes/wider release. Not natural SQL-lock contention or whole concurrent-revocation acceptance; no app change.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded repair verified 2026-10-10 |
| Final verification | Current selected-child50native HTTP/four linked browser journeys/eight Admin writes +11handlers/48synthetic cases PASS; prior90/55/queued/reload retained; idle-context/device/broader critical OPEN |
| Severity | Major access revocation failure |
| Priority | P1 |
| Category | SEC |
| Module | GUARDIAN |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | Direct guardian portal-access API |
| API | PATCH students/{studentId}/guardians/{guardianId}/portal-access |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | Current390/1440 light/dark native selected-child journeys PASS; physical Android/iOS NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-GUARDIAN-003 |
| Evidence classification | Original server revocation reproduced/repaired; current selected-child presentation reproduced/repaired and native-qualified; wider gates OPEN |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/StudentGuardiansController.cs:54 |
| Class/function | SetPortalAccess |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed an active minor with a linked guardian and portal account. As an authorized administrator PATCH allowPortalAccess=false with all four subpermissions explicitly false. Inspect response, fresh link and guardian child/detail access. Repeat adult and unlinked controls; also test omitted subpermissions. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | SetPortalAccess explicit authority repair; see scoped report/history |
| Retest result | 90/90 strict cases; run f717a944a28847deaa1703f522c895c8 |
| Regression result | Existing GuardianFlags native gate and targeted unit tests; results in report |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Current bounded repair — 2026-10-10

[Report](../REPORTS/PHASE_2B_GUARDIAN_REVOCATION_REPAIR.md): baseline `bce44b1494ef485f83b7b620faf06dba` confirmed explicitfalse and omitted-subpermission revocations silently retained the minor's grant and child-detail/Me access with the same guardian token. SetPortalAccess now respects the explicit AllowPortalAccess independently of age; initial Link minor defaults and grant subpermission defaults stay unchanged. Final native SQL/HTTP90 cases PASS, checking minor/adult/18th-birthday/unknown-DOB, explicit/omitted revoke, restricted/default regrant, unlink, current-token read visibility, authorization/tenant controls and captured SQL/file preservation. Discovered and separately tracked [BUG-API-0009](BUG-API-0009.md) child-list500 query failure repaired in the same bounded flow. **OPEN** for browser/device, concurrent revocation, broader lifecycle/role and release gates. Historical static evidence below retained, not the current verdict.

## Exact reproduction

Seed an active minor with a linked guardian and portal account. As an authorized administrator PATCH allowPortalAccess=false with all four subpermissions explicitly false. Inspect response, fresh link and guardian child/detail access. Repeat adult and unlinked controls; also test omitted subpermissions.

## Expected

An explicit revocation is respected, or rejected clearly with an actionable policy explanation; it must not silently regrant access. Requirements for a minor to have a guardian must not make each individual guardian irrevocable.

## Actual / evidence

isMinor OR request.AllowPortalAccess forces CanAccessPortal=true, resets AccessRevokedAtUtc to null and updates grant time. Omitted subpermissions default true. Child identity/contact access remains available with all subpermissions false. Deleting the link is a separate available operation, not revocation through this endpoint.

Source snapshot:

```text
53:         var isMinor = link.Student.DateOfBirth.HasValue && link.Student.DateOfBirth.Value.AddYears(18) > DateOnly.FromDateTime(DateTime.UtcNow);
54:         var access = isMinor || request.AllowPortalAccess;
55:         link.CanAccessPortal = access;
56:         link.CanViewAcademicProgress = access && request.AllowAcademicProgress;
```

## Suspected root cause

Default minor-access policy overrides an explicit per-guardian revocation command.

## Business impact and blast radius

Minor guardian links using the portal-access endpoint; current guardian page does not expose this endpoint.

## Related / required regression

SECURITY-GUARDIAN-003: Real HTTP/SQL matrix for minor/adult/18th-birthday/unknown DOB, explicit revoke, omitted vs false flags, regrant and unlink; verify both response and new guardian reads deny after revocation.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
