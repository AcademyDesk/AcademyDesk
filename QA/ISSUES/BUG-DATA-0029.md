# BUG-DATA-0029 — Circular course prerequisites can block all entry paths

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / LOCAL REPAIR; broader OPEN |
| Final verification | Bounded controller/HTTP/SQL/concurrency PASS; browser/device/all-linked/critical NOT RUN |
| Severity | Major enrollment rule integrity |
| Priority | P1 |
| Category | DATA |
| Module | ACADEMIC |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /academic-governance |
| API | POST academic-governance/prerequisites |
| Environment | Isolated Testing Identity/HTTP/SQL Server2022; normal dev/Azure data untouched |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | ACADEMIC-GRAPH-001 |
| Evidence classification | Baseline five direct-controller cycle failures; final341 backend + two fresh SQL32-case PASS |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Five baseline graph failures; two independent final SQL runs |
| Source | apps/api/Controllers/AcademicGovernanceController.cs:34; original line3 excerpt below historical |
| Class/function | AddPrerequisite / Enrollments.Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | [Prerequisite repair report](../REPORTS/PHASE_2B_PREREQUISITE_REPAIR.md), logs/TRX/source fingerprints linked there |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Synthetic self/two/three/long/diamond/legacy graphs, invalid membership, deterministic reciprocal/duplicate/three-node concurrent pairs; scoped POST/GET and fault controls |
| API response | Final bounded200/400/409 and authorization401/403; deliberate lock503 and audit fault500 with no persisted edge |
| Database before/after | Fresh SQL full captured edges/audits and unrelated snapshots checked; successful saves add exactly one edge; rejections/fault unchanged |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted prerequisite guard/academy SQL lock/domain audit boundary; no Azure deployment |
| Retest result | PASS two fresh real Identity/HTTP/SQL runs x32; initial fixture module403 excluded/cleaned |
| Regression result | PASS backend341/341 (19 new); deterministic SQL concurrency and audit rollback; frontend/browser/critical not rerun |
| Closure notes | Remain OPEN; legacy cycles/removal/enrollment/browser/device/all-linked/critical gates remain |

## Local repair checkpoint — 2026-10-01

[Bounded repair/evidence](../REPORTS/PHASE_2B_PREREQUISITE_REPAIR.md): cycle-forming POST400 without edge/audit; academy SQL application lock prevents concurrent reciprocal/duplicate/three-node closing inserts across replicas. Normal actors share the save/audit transaction; platform bypass owns its graph transaction without changing existing audit policy. Backend341/341 and two fresh real HTTP/SQL runs x32 PASS; clean isolated build/owned cleanup/source delta verified. Five cycle baseline failures and one future boundary assertion retained separately. Initial SQL403 was a missing synthetic module, not a product regression; no app permission relaxed. Existing stored cycles are not migrated/deleted; allowed edges into legacy cycles, UI removal/enrollment/device/critical acceptance still pending. No dev DB/Azure/commit/push. Next BUG-DATA-0030, Sol High.

## Exact reproduction

In isolated fixtures add A requires B, then B requires A. Attempt valid enrollment into either course for a student with neither completed. Repeat a three-course cycle and an acyclic control.

## Expected

Adding a prerequisite that creates a cycle rejects without storing the edge; a new learner must have a possible completion order.

## Historical Actual / evidence (before repair)

API rejects only self-reference, missing/foreign courses and duplicate edge. It accepts multi-node cycles; enrollment requires completed prior courses. UI has no edge removal. Runtime NOT RUN.

Source snapshot:

```text
2: namespace AcademyDesk.Api.Controllers;
3: [ApiController][Route("api/academies/{academyId:guid}/academic-governance")]public sealed class AcademicGovernanceController(AcademyDeskDbContext db):ControllerBase{[HttpGet("grading-schemes")]public async Task<ActionResult> Schemes(Guid academyId,CancellationToken t)=>Ok(await db.GradingSchemes.AsNoTracking().Where(x=>x.AcademyId==academyId).ToListAsync(t));[HttpPost("grading-schemes")]public async Task<ActionResult> AddScheme(Guid academyId,GradingSchemeRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Name)||r.PassingPercent<0||r.PassingPercent>100)return BadRequest();try{System.Text.Json.JsonDocument.Parse(r.BandsJson??"[]");}catch{return BadRequest(new{message="Grade bands must be valid JSON."});}db.GradingSchemes.Add(new GradingScheme{AcademyId=academyId,Name=r.Name.Trim(),PassingPercent=r.PassingPercent,BandsJson=r.BandsJson??"[]"});await db.SaveChangesAsync(t);return Ok();}[HttpGet("prerequisites")]public async Task<ActionResult> Prerequisites(Guid academyId,CancellationToken t)=>Ok(await db.CoursePrerequisites.AsNoTracking().Where(x=>x.AcademyId==academyId).ToListAsync(t));[HttpPost("prerequisites")]public async Task<ActionResult> AddPrerequisite(Guid academyId,PrerequisiteRequest r,CancellationToken t){if(r.CourseId==r.RequiredCourseId||!await db.Courses.AnyAsync(x=>x.AcademyId==academyId&&x.Id==r.CourseId,t)||!await db.Courses.AnyAsync(x=>x.AcademyId==academyId&&x.Id==r.RequiredCourseId,t))return BadRequest();if(await db.CoursePrerequisites.AnyAsync(x=>x.AcademyId==academyId&&x.CourseId==r.CourseId&&x.RequiredCourseId==r.RequiredCourseId,t))return Conflict(new{message="This prerequisite is already configured."});db.CoursePrerequisites.Add(new CoursePrerequisite{AcademyId=academyId,CourseId=r.CourseId,RequiredCourseId=r.RequiredCourseId});await db.SaveChangesAsync(t);return Ok();}}
4: public sealed record GradingSchemeRequest(string Name,decimal PassingPercent,string? BandsJson);public sealed record PrerequisiteRequest(Guid CourseId,Guid RequiredCourseId);
5:
```

## Suspected root cause

Local edge checks omit graph reachability/cycle validation.

## Business impact and blast radius

Configured course groups can become inaccessible to new learners through normal enrollment, requiring administrative data repair or a separate approved override.

## Related / required regression

ACADEMIC-GRAPH-001: HTTP/SQL self/two-node/long-cycle/diamond/disconnected graph cases, concurrent reciprocal inserts, cross-tenant isolation and exact no-write assertions on cycle rejection.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
