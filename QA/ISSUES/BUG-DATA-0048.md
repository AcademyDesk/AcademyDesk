# BUG-DATA-0048 — Operational work items accept unchecked assignee and entity references

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major operational ownership integrity |
| Priority | P1 |
| Category | DATA |
| Module | OPERATIONS |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /work-queue |
| API | POST /api/academies/{academyId}/admin-work-items |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | OPERATIONS-WORK-IDENTITY-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/AdminWorkItemsController.cs:6 |
| Class/function | Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create a work item with local, foreign and missing AssignedUserId and EntityId values, including a mismatched EntityType. Inspect the assigned queue and fresh row; compare unassigned control. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Create a work item with local, foreign and missing AssignedUserId and EntityId values, including a mismatched EntityType. Inspect the assigned queue and fresh row; compare unassigned control.

## Expected

Assignees and linked entities are validated within the academy and entity type, or unsupported links are excluded from the public create contract.

## Actual / evidence

Create validates type/title/priority only, then stores AssignedUserId, EntityType and EntityId verbatim. No user/entity lookup or relationship mapping occurs. Runtime NOT RUN.

Source snapshot:

```text
5: [HttpGet] public async Task<ActionResult> List(Guid academyId,string? status,string? type,CancellationToken t){var q=db.AdminWorkItems.AsNoTracking().Where(x=>x.AcademyId==academyId);if(!string.IsNullOrWhiteSpace(status))q=q.Where(x=>x.Status==status);if(!string.IsNullOrWhiteSpace(type))q=q.Where(x=>x.Type==type);return Ok(await q.OrderBy(x=>x.DueAtUtc).ThenByDescending(x=>x.Priority).ToListAsync(t));}
6: [HttpPost] public async Task<ActionResult> Create(Guid academyId,CreateWorkItem r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Type)||string.IsNullOrWhiteSpace(r.Title))return BadRequest();if(r.Priority is not (null or "Low" or "Normal" or "High" or "Critical"))return BadRequest("Invalid priority.");var x=new AdminWorkItem{AcademyId=academyId,Type=r.Type.Trim(),Title=r.Title.Trim(),Description=r.Description?.Trim(),Priority=string.IsNullOrWhiteSpace(r.Priority)?"Normal":r.Priority.Trim(),EntityType=r.EntityType?.Trim(),EntityId=r.EntityId,AssignedUserId=r.AssignedUserId,DueAtUtc=r.DueAtUtc};db.AdminWorkItems.Add(x);await db.SaveChangesAsync(t);return Ok(x);}
7: [HttpPatch("{id:guid}/status")] public async Task<ActionResult> Status(Guid academyId,Guid id,WorkStatus r,CancellationToken t){var x=await db.AdminWorkItems.SingleOrDefaultAsync(x=>x.AcademyId==academyId&&x.Id==id,t);if(x is null)return NotFound();if(r.Status is not("Open"or"InProgress"or"Completed"or"Cancelled"))return BadRequest();x.Status=r.Status;x.CompletedAtUtc=r.Status=="Completed"?DateTime.UtcNow:null;await db.SaveChangesAsync(t);return Ok(x);}
8: [HttpPatch("{id:guid}/collections")] public async Task<ActionResult> CollectionState(Guid academyId,Guid id,CollectionStateRequest r,CancellationToken t){var x=await db.AdminWorkItems.SingleOrDefaultAsync(x=>x.AcademyId==academyId&&x.Id==id&&x.Type=="Collections",t);if(x is null)return NotFound();if(r.EscalationStage is not("Initial"or"Reminder"or"ManagerReview"or"FinalNotice"))return BadRequest();x.EscalationStage=r.EscalationStage;x.PromisedPaymentDate=r.PromisedPaymentDate;await db.SaveChangesAsync(t);return Ok(x);}}
```

## Suspected root cause

Operational linkage fields are trusted as opaque client values.

## Business impact and blast radius

Queue ownership, follow-up routing and entity-linked work tracking can point to invalid or cross-scope records.

## Related / required regression

OPERATIONS-WORK-IDENTITY-001: HTTP/SQL local/foreign/missing/null user and entity matrix, allowed user role policy, type/entity mismatch, list filtering, status updates and rejected no-write assertions.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
