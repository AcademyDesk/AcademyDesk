# BUG-DATA-0032 — Promotion decisions can contradict already committed enrollment changes

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / local repair verified |
| Final verification | PASS twice in isolated real HTTP/SQL harness; issue remains OPEN |
| Severity | Major progression lifecycle inconsistency |
| Priority | P1 |
| Category | DATA |
| Module | BATCH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /batch-promotions; direct decision API |
| API | PATCH batch-promotions/{id}/decision |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | BATCH-PROMOTION-001 |
| Evidence classification | Baseline controller test reproduced terminal overwrite/retry failure; bounded repaired HTTP/SQL verification recorded |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/BatchPromotionsController.cs:3 |
| Class/function | Decide |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Approve a pending synthetic promotion and verify completed source plus active target enrollment. Send Rejected for the same promotion ID. Separately retry Approved after initial approval. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local working tree only; no commit/push/Azure deployment |
| Retest result | 13 focused controller tests plus 376/376 backend suite PASS |
| Regression result | 26 real Identity/HTTP/SQL cases PASS twice; see QA/REPORTS/PHASE_2B_PROMOTION_REPAIR.md |
| Closure notes | Remain OPEN: browser/device, wider progression lifecycle, existing-record remediation, critical/performance/release gates not run |

## Exact reproduction

Approve a pending synthetic promotion and verify completed source plus active target enrollment. Send Rejected for the same promotion ID. Separately retry Approved after initial approval.

## Expected

Terminal decisions follow an explicit state machine; a repeated request is safely idempotent or rejected without contradictions or duplicate enrollment.

## Actual / evidence

The prior implementation checked only requested status, not current promotion state. Baseline focused tests had 6 product-behaviour failures: terminal records could be overwritten, Approved replay threw after its source enrollment was completed, and inconsistent source/target states were accepted; a seventh baseline failure asserted the then-missing atomic boundary. The repaired endpoint rejects every non-Pending decision with409, rejects stale source/active target conflicts before mutation, and uses a per-promotion SQL application lock inside the atomic domain/audit boundary.

Final local evidence: [13 focused controller tests](../../tests/AcademyDesk.Api.Tests/BatchPromotionDecisionTests.cs) and [376/376 backend tests](../EVIDENCE/logs/phase-2b-promotion-suite.log) pass. [Two fresh isolated HTTP/SQL runs](../REPORTS/PHASE_2B_PROMOTION_REPAIR.md) each pass 26 cases, including deterministic concurrent decisions, timeout, authorization, audit-insert rollback and platform transaction fallback. No production/Azure database was used.

Source snapshot:

```text
2: namespace AcademyDesk.Api.Controllers;
3: [ApiController][Route("api/academies/{academyId:guid}/batch-promotions")]public sealed class BatchPromotionsController(AcademyDeskDbContext db):ControllerBase{[HttpGet]public async Task<ActionResult> List(Guid academyId,CancellationToken t)=>Ok(await db.BatchPromotions.AsNoTracking().Where(x=>x.AcademyId==academyId).OrderByDescending(x=>x.EffectiveDate).ToListAsync(t));[HttpPost]public async Task<ActionResult> Create(Guid academyId,PromotionRequest r,CancellationToken t){if(r.SourceBatchId==r.TargetBatchId||!await db.Enrollments.AnyAsync(x=>x.AcademyId==academyId&&x.StudentId==r.StudentId&&x.BatchId==r.SourceBatchId&&x.Status=="Active",t)||!await db.Batches.AnyAsync(x=>x.AcademyId==academyId&&x.Id==r.TargetBatchId,t))return BadRequest();db.BatchPromotions.Add(new BatchPromotion{AcademyId=academyId,StudentId=r.StudentId,SourceBatchId=r.SourceBatchId,TargetBatchId=r.TargetBatchId,EffectiveDate=r.EffectiveDate,Notes=r.Notes?.Trim()});await db.SaveChangesAsync(t);return Ok();}[HttpPatch("{id:guid}/decision")]public async Task<ActionResult> Decide(Guid academyId,Guid id,PromotionDecision r,CancellationToken t){var x=await db.BatchPromotions.SingleOrDefaultAsync(x=>x.Id==id&&x.AcademyId==academyId,t);if(x is null)return NotFound();if(r.Status is not("Approved"or"Rejected"))return BadRequest();if(r.Status=="Approved"){var source=await db.Enrollments.SingleAsync(e=>e.AcademyId==academyId&&e.StudentId==x.StudentId&&e.BatchId==x.SourceBatchId&&e.Status=="Active",t);source.Status="Completed";source.EndDate=x.EffectiveDate;source.LifecycleReason="Promoted to target batch";db.Enrollments.Add(new Enrollment{AcademyId=academyId,StudentId=x.StudentId,BatchId=x.TargetBatchId,StartDate=x.EffectiveDate,Status="Active"});}x.Status=r.Status;x.Notes=r.Notes?.Trim()??x.Notes;await db.SaveChangesAsync(t);return Ok();}}
4: public sealed record PromotionRequest(Guid StudentId,Guid SourceBatchId,Guid TargetBatchId,DateOnly EffectiveDate,string? Notes);public sealed record PromotionDecision(string Status,string? Notes);
5:
```

## Suspected root cause

Decision endpoint has no current-state guard or replay handling.

## Business impact and blast radius

Promotion history, source completion, target roster and safe retry after a lost response.

## Related / required regression

BATCH-PROMOTION-001: Full pending/approved/rejected decision matrix, replay, stale source, two pending promotions and concurrent decisions; authoritative row counts and atomic no-write assertions.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
