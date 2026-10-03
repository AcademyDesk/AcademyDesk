# BUG-DATA-0038 — Leave requester types bypass person validation and accept unrelated person IDs

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | ACCEPTED STATIC DIAGNOSIS / bounded local repair verified |
| Final verification | PARTIAL:477 backend tests (51 new)/63 real HTTP-SQL checks PASS; browser/lifecycle/critical pending |
| Severity | Major leave record integrity |
| Priority | P1 |
| Category | DATA |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /leave; direct leave API |
| API | POST leave-requests |
| Environment | Local isolated owned SQL/Identity HTTP pipeline; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-LEAVE-IDENTITY-001 |
| Evidence classification | Accepted baseline static trace plus repaired direct-controller and real Identity/HTTP/fresh SQL regression; no old-code runtime reproduction claimed |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/LeaveRequestsController.cs:11 |
| Class/function | Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Historical source excerpt below and QA/REPORTS/PHASE_2B_LEAVE_IDENTITY_REPAIR.md/source snapshot |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated tenant fixtures submit lowercase student or an unknown RequesterType with both person IDs null. Then submit Student with a valid local StudentId and a foreign TeacherId; compare exact valid Student/Teacher controls. |
| API response | Repaired empty200,400 binding/validation,403 and401 asserted in isolated regression; baseline response not captured |
| Database before/after | Full leave/audit snapshots unchanged for denied requests; valid fresh SQL and scoped GET match; historical baseline not executed |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted Create validation repair; no deployment |
| Retest result | PASS bounded requester/person guards; null/missing/numeric binding400 verified separately |
| Regression result | 477/477 backend and63/63 real HTTP-SQL PASS; no rejected leave/audit writes; eligibility/access unchanged |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Local repair checkpoint — 2026-10-01

[Repair report](../REPORTS/PHASE_2B_LEAVE_IDENTITY_REPAIR.md): supported types normalized and stored canonically; exactly one matching same-academy person required, including rejection of opposite empty/local/foreign IDs. Owned inactive eligibility and empty200 preserved.51 new direct tests/477 suite total,63 real HTTP-SQL cases PASS; accepted source diagnosis reused, not old-code runtime replay. Scope covers Create identity guards and two scoped List readbacks, not Decide/GET ordering-empty/lifecycle/browser/device/audit rollback/release. OPEN, local only; no commit/deploy.

## Historical baseline reproduction

In isolated tenant fixtures submit lowercase student or an unknown RequesterType with both person IDs null. Then submit Student with a valid local StudentId and a foreign TeacherId; compare exact valid Student/Teacher controls.

## Expected

Accept only normalized supported requester types and exactly one matching same-tenant person; reject incompatible or foreign extra IDs before saving.

## Historical baseline / evidence

Person checks run only for exact Student or Teacher. Any other nonblank type skips both checks, and both supplied IDs are copied regardless of type. There is no mapped relationship enforcing tenant/person consistency. Runtime NOT RUN; no cross-tenant data disclosure is claimed.

Source snapshot:

```text
10:     [HttpGet] public async Task<ActionResult> List(Guid academyId,CancellationToken t)=>Ok(await db.LeaveRequests.AsNoTracking().Where(x=>x.AcademyId==academyId).OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new LeaveSummary(x.Id,x.RequesterType,x.StudentId,x.TeacherId,x.StartDate,x.EndDate,x.Reason,x.Status,x.DecisionNotes)).ToListAsync(t));
11:     [HttpPost] public async Task<ActionResult> Create(Guid academyId,CreateLeaveRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Reason)||r.EndDate<r.StartDate)return BadRequest(new{message="Reason and valid dates are required."});if(r.RequesterType=="Student"&&(!r.StudentId.HasValue||!await db.Students.AnyAsync(x=>x.Id==r.StudentId&&x.AcademyId==academyId,t)))return BadRequest(new{message="Select a valid student."});if(r.RequesterType=="Teacher"&&(!r.TeacherId.HasValue||!await db.Teachers.AnyAsync(x=>x.Id==r.TeacherId&&x.AcademyId==academyId,t)))return BadRequest(new{message="Select a valid teacher."});var x=new LeaveRequest{AcademyId=academyId,RequesterType=r.RequesterType,StudentId=r.StudentId,TeacherId=r.TeacherId,StartDate=r.StartDate,EndDate=r.EndDate,Reason=r.Reason.Trim()};db.LeaveRequests.Add(x);await db.SaveChangesAsync(t);return Ok();}
12:     [HttpPatch("{id:guid}")] public async Task<ActionResult> Decide(Guid academyId,Guid id,DecideLeaveRequest r,CancellationToken t){if(r.Status is not("Approved" or "Rejected"))return BadRequest();var x=await db.LeaveRequests.SingleOrDefaultAsync(v=>v.Id==id&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.Status=r.Status;x.DecisionNotes=r.Notes?.Trim();await db.SaveChangesAsync(t);return Ok();}
13: }
```

## Suspected root cause

Requester discriminator is not allowlisted and opposite person ID is not excluded.

## Business impact and blast radius

Invalid or mixed-person leave rows submitted through direct API; UI normally supplies canonical types.

## Related / required regression

SCHEDULE-LEAVE-IDENTITY-001: Full HTTP/SQL type case/space/unknown/null, zero/both IDs, local/foreign/missing/inactive person matrix; rejected requests must leave no row or success audit.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
