# BUG-SEC-0003 — Teacher resource actions bypass academy suspension checks

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded host repair verified 2026-10-10 |
| Final verification | PARTIAL: 62 native SQL/HTTP + 29 media regressions + 77 unit cases PASS; browser/device/module/broader critical pending |
| Severity | Major authorization lifecycle gap |
| Priority | P1 |
| Category | SEC |
| Module | TEACHERPORTAL |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher |
| API | GET resources; GET classroom-activity; POST resources/note; POST resources/upload |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SECURITY-LIFECYCLE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/TeacherPortalController.cs:529 |
| Class/function | Resources / ClassroomActivity / CreateClassNote / UploadClassMaterial |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In an isolated two-tenant fixture verify active teacher and own batch controls first. Suspend the academy while keeping the Identity user active; repeat reads and a synthetic note/upload. Separately disable the teacher domain record while leaving its Identity link active. Compare academy-scoped resource API and teacher me. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | TeacherPortalAccessFilter and controller attachment; see scoped repair report/history |
| Retest result | 62/62 strict cases PASS; final run 7696f6a8fd8847d5919a5a6b185440de |
| Regression result | 29 unchanged private-media HTTP + 77 existing media/resource unit tests PASS |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Current bounded repair — 2026-10-10

[Native reproduction and repair report](../REPORTS/PHASE_2B_TEACHER_LIFECYCLE_REPAIR.md): baseline `a629539092e44af7bf4d7fbb28120b40` confirmed both academy suspension and inactive domain Teacher retained all four200 direct operations, including persisted resources/notifications/files. The controller now independently requires current active Identity, persisted Teacher role, active server-linked own-academy Teacher and active academy before any action. Final strict run `7696f6a8fd8847d5919a5a6b185440de` passes62 cases, no429, with fresh no-write/file checks and active/restored controls. Initial calendar-fixture failure corrected and full gate rerun. **OPEN**: browser/physical devices, module policy, other teacher mutations/related portals, concurrent revocation and critical release acceptance remain. Historical static evidence below is retained, not the current runtime verdict.

## Exact reproduction

In an isolated two-tenant fixture verify active teacher and own batch controls first. Suspend the academy while keeping the Identity user active; repeat reads and a synthetic note/upload. Separately disable the teacher domain record while leaving its Identity link active. Compare academy-scoped resource API and teacher me.

## Expected

Suspended academy and deactivated teaching identity cannot retain ordinary resource access through direct endpoints; denied writes create no resource, notification or file.

## Actual / evidence

AcademyAccessFilter skips endpoints with no academyId argument. These teacher actions check linked IDs/batch ownership but never academy IsActive or teacher IsActive; Program checks only Identity user IsActive. Me checks teacher activity, but calling Me is not required before other requests. Static trace only.

Source snapshot:

```text
528:
529:     private async Task<bool> OwnsBatch(ApplicationUser user, Guid batchId, CancellationToken token) =>
530:         await dbContext.Batches.AnyAsync(x => x.Id == batchId && x.AcademyId == user.AcademyId && x.TeacherId == user.TeacherId, token);
531:
```

## Suspected root cause

Lifecycle checks are not shared across no-academyId self-service routes.

## Business impact and blast radius

Four reviewed teacher resource actions; related Portal student/submission paths also require lifecycle tests, not a claim all portal endpoints were audited.

## Related / required regression

SECURITY-LIFECYCLE-001: Real HTTP/SQL/storage controls for active, suspended academy, disabled Identity, disabled teacher, removed role with retained link, foreign batch and disabled module; assert both reads and no-write/file effects. Policy must define module behavior separately.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
