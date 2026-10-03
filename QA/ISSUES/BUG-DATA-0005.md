# BUG-DATA-0005 — Session clash checks do not use effective teacher and branch assignments

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major scheduling integrity |
| Priority | P1 |
| Category | DATA |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /schedule; /calendar; /batch-setup |
| API | POST/PUT sessions; PUT batches/{id} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-RULE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/ClassSessionsController.cs:35 |
| Class/function | ClassSessions.Create/Update / Batches.Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed two batches for same teacher. POST overlapping session omitting optional TeacherId so batch teacher applies. Separately move a session into conflict using PUT. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Seed two batches for same teacher. POST overlapping session omitting optional TeacherId so batch teacher applies. Separately move a session into conflict using PUT.

## Expected

Effective teacher/room clash rejected for both create and reschedule.

## Actual / evidence

Create checks overlap only when request.TeacherId exists, then falls back to batch.TeacherId. Room clash compares requested BranchId before applying batch.BranchId fallback, so omitted branch can bypass or falsely trigger room checks. Update does not repeat overlap checks. Every Batches.Update also reassigns all future Scheduled sessions to the batch teacher, including null, without clash checks; explicit substitute assignment can be overwritten even on unrelated edits.

Source snapshot:

```text
34:
35:         var assignedTeacherId = request.TeacherId ?? batch.TeacherId;
36:         var session = new ClassSession { AcademyId = academyId, BatchId = request.BatchId, TeacherId = assignedTeacherId, BranchId = request.BranchId ?? batch.BranchId, StartUtc = request.StartUtc, EndUtc = request.EndUtc, DeliveryMode = string.IsNullOrWhiteSpace(request.DeliveryMode) ? "InPerson" : request.DeliveryMode.Trim(), RoomName = request.RoomName?.Trim() };
37:         dbContext.ClassSessions.Add(session); await dbContext.SaveChangesAsync(cancellationToken);
```

## Suspected root cause

Validation before fallback resolution and missing reschedule clash validation.

## Business impact and blast radius

Teacher double bookings and contradictory calendars.

## Related / required regression

SCHEDULE-RULE-001: SQL integration for inherited/explicit teacher and adjacent/overlapping times; explicit/inherited branch and same-room cases; parallel schedule writes; batch teacher/name/status changes with future, past, substitute and non-Scheduled sessions, preserving cross-tenant isolation.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
