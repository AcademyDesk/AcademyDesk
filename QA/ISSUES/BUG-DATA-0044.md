# BUG-DATA-0044 — Learning-resource batch and subject scope can be internally inconsistent

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONTROLLER-REPRODUCED:4 baseline mismatch failures; local repair |
| Final verification | PARTIAL:73 HTTP-SQL-file-Student/Guardian checks PASS; prior912 backend retained; browser/closure OPEN |
| Severity | Major audience-targeting integrity |
| Priority | P1 |
| Category | DATA |
| Module | LEARNING |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /resources |
| API | POST /api/academies/{academyId}/resources; POST /resources/upload |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | LEARNING-RESOURCE-SCOPE-001 |
| Evidence classification | Executed controller + real Identity/HTTP/SQL/file/audit/family checks; browser/closure pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/LearningResourcesController.cs:10 |
| Class/function | Create / Upload / ValidateScope |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create a local batch for Course A and a local Course B. Create/link-upload a published resource supplying that batchId and Course B, then inspect fresh row and family resource results for enrolled Course A and Course B learners. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted ValidateScope repair; no deployment |
| Retest result | 73 real HTTP-SQL-file-Student/Guardian checks PASS; prior37 controller cases retained; browser pending |
| Regression result | Prior912 backend PASS retained; linked/critical/release OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Create a local batch for Course A and a local Course B. Create/link-upload a published resource supplying that batchId and Course B, then inspect fresh row and family resource results for enrolled Course A and Course B learners.

## Expected

When both values are supplied, the resource scope requires batch.CourseId to equal courseId, or the product explicitly prevents the unsupported combination in the UI and API.

## Actual / evidence

ValidateScope only confirms each ID independently belongs to the academy. Batch.CourseId is never compared with request.CourseId, yet the family query applies both filters. A mismatched record is stored as a contradictory target and can disappear for the intended batch audience. Runtime NOT RUN.

Source snapshot:

```text
9:  [HttpPatch("{resourceId:guid}/publish")]public async Task<ActionResult> Publish(Guid academyId,Guid resourceId,PublishResourceRequest r,CancellationToken t){var x=await db.LearningResources.SingleOrDefaultAsync(v=>v.Id==resourceId&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.IsPublished=r.IsPublished;await db.SaveChangesAsync(t);return Ok();}
10:  private async Task<string?> ValidateScope(Guid academyId,Guid? batchId,Guid? courseId,CancellationToken t){if(batchId.HasValue&&!await db.Batches.AnyAsync(x=>x.Id==batchId&&x.AcademyId==academyId,t))return "Invalid batch.";if(courseId.HasValue&&!await db.Courses.AnyAsync(x=>x.Id==courseId&&x.AcademyId==academyId,t))return "Invalid subject.";return null;}
11: }
12: public sealed record CreateResourceRequest(string Title,string? Description,string? Type,string Url,Guid? BatchId,Guid? CourseId,bool IsPublished);public sealed record ResourceSummary(Guid Id,string Title,string? Description,string Type,string Url,Guid? BatchId,Guid? CourseId,bool IsPublished);public sealed record PublishResourceRequest(bool IsPublished);public sealed class UploadResourceRequest{public string? Title{get;set;}public string? Description{get;set;}public string? Type{get;set;}public Guid? BatchId{get;set;}public Guid? CourseId{get;set;}public bool IsPublished{get;set;}=true;public IFormFile? File{get;set;}}
```

## Suspected root cause

Referential existence validation does not enforce the relationship between two supplied scope dimensions.

## Business impact and blast radius

Published link and upload resources scoped by both batch and subject, including administrator-selected audience expectations.

## Related / required regression

LEARNING-RESOURCE-SCOPE-001: HTTP/SQL batch-course match/mismatch/null matrix for link and upload endpoints, inactive/foreign/deleted IDs, family resource visibility for matching enrolled students and no-write assertions for rejected combinations.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local controller execution — 2026-10-01

Superseding [real HTTP/SQL/family check](../REPORTS/PHASE_2B_RESOURCE_SCOPE_SQL_CHECK.md):73 checks PASS on one fresh isolated Identity/SQL run. Both endpoints' scope/auth/module/binding guards; exact stored rows/upload bytes/actor audits; scoped List/Publish; independently enumerated Student/Guardian audiences, publication/permission/revocation/completed-enrollment controls. No application changes this follow-up; initial QA-only compiler failure corrected and retained. Owned resources cleaned, normal dev/Azure untouched, browser/linked/critical/race/fault/media/legacy-policy closure gates OPEN. The controller-only NOT RUN handoff below is historical.

[Bounded repair and evidence](../REPORTS/PHASE_2B_RESOURCE_SCOPE_REPAIR.md): baseline37 cases33 PASS/4 expected FAIL (link/upload active/inactive mismatches accepted200); corrected shared guard passes full912 backend cases,37 new. Missing/foreign/empty identities, optional scopes and inactive eligibility preserved; no-write/file/source-row controls and List/Publish checked via direct controllers/InMemory. No customer/dev/Azure data/service/deployment changes. These are not real HTTP/SQL, family visibility or device passes. Next real Identity/disposable SQL and family audience readback; issue remains OPEN.
