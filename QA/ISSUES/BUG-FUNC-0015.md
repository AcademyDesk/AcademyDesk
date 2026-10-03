# BUG-FUNC-0015 — Delegated academic staff can create schemes but cannot change their status

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major permission contract inconsistency |
| Priority | P1 |
| Category | FUNC |
| Module | ACADEMIC |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /academic-governance |
| API | POST academic-governance/grading-schemes; PATCH grading-schemes/{schemeId}/status |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ACADEMIC-ACCESS-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/GradingSchemeLifecycleController.cs:4 |
| Class/function | Status / PermissionCatalog / academic-governance toggleScheme |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Use an active same-tenant custom role with academics.manage and Core/AcademicGovernance modules. Create a scheme, then use Deactivate. Compare AcademyAdmin and expired/revoked grant controls. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Use an active same-tenant custom role with academics.manage and Core/AcademicGovernance modules. Create a scheme, then use Deactivate. Compare AcademyAdmin and expired/revoked grant controls.

## Expected

An authorized scheme-management workflow has a consistent, explicit lifecycle policy; available actions must reflect permissions and use least privilege.

## Actual / evidence

AcademicGovernanceController maps academics.manage, but GradingSchemeLifecycleController lacks a mapping, so global filter denies the same nonadmin user status changes and active-scheme lookup. Page still renders action. Runtime NOT RUN.

Source snapshot:

```text
3: [ApiController][Route("api/academies/{academyId:guid}/grading-schemes")]
4: public sealed class GradingSchemeLifecycleController(AcademyDeskDbContext db):ControllerBase{[HttpGet("active")]public async Task<ActionResult> Active(Guid academyId,CancellationToken t)=>Ok(await db.GradingSchemes.AsNoTracking().Where(x=>x.AcademyId==academyId&&x.IsActive).OrderBy(x=>x.Name).ToListAsync(t));[HttpPatch("{schemeId:guid}/status")]public async Task<ActionResult> Status(Guid academyId,Guid schemeId,SchemeStatusRequest r,CancellationToken t){var scheme=await db.GradingSchemes.SingleOrDefaultAsync(x=>x.Id==schemeId&&x.AcademyId==academyId,t);if(scheme is null)return NotFound();scheme.IsActive=r.IsActive;await db.SaveChangesAsync(t);return Ok(new{scheme.Id,scheme.IsActive});}}
5: public sealed record SchemeStatusRequest(bool IsActive);
6:
```

## Suspected root cause

Scheme lifecycle controller was omitted from the permission catalog; its module also falls back to Core unlike governance creation.

## Business impact and blast radius

Delegated academic administrators cannot manage scheme lifecycle through the same page; no broad role elevation should be used as a fix.

## Related / required regression

ACADEMIC-ACCESS-001: Real HTTP all-role/grant/module matrix plus browser create/list/deactivate/reactivate and active lookup; preserve tenant boundaries and explicitly approve module policy.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
