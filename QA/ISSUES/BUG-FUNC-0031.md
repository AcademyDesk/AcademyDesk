# BUG-FUNC-0031 — Admin intelligence can fail when a scheduled session references a missing teacher

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major operational-dashboard availability |
| Priority | P1 |
| Category | FUNC |
| Module | OPERATIONS |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | Admin intelligence |
| API | GET /api/academies/{academyId}/admin-intelligence |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | OPERATIONS-INTELLIGENCE-ORPHAN-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/AdminIntelligenceController.cs:20 |
| Class/function | Get workload projection |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures retain a scheduled session with TeacherId referencing a deleted, foreign or otherwise unavailable teacher. Request intelligence and compare a valid teacher control. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixtures retain a scheduled session with TeacherId referencing a deleted, foreign or otherwise unavailable teacher. Request intelligence and compare a valid teacher control.

## Expected

Intelligence returns an explicit unknown/unassigned teacher row or filters invalid data; one inconsistent relationship cannot fail the entire dashboard.

## Actual / evidence

Workload dereferences FirstOrDefault(... )!.Name with no null guard. A session whose TeacherId is not in the local teacher projection throws before response serialization. Runtime NOT RUN.

Source snapshot:

```text
19:         var teachers = await db.Teachers.Where(x => x.AcademyId == academyId).Select(x => new { x.Id, Name = x.FirstName + " " + x.LastName }).ToListAsync(token);
20:         var workload = sessions.Where(x => x.TeacherId != null).GroupBy(x => x.TeacherId).Select(group => new { TeacherId = group.Key, TeacherName = teachers.FirstOrDefault(teacher => teacher.Id == group.Key)!.Name, ScheduledHours = Math.Round(group.Sum(x => (decimal)(x.EndUtc - x.StartUtc).TotalHours), 1), Sessions = group.Count() });
21:         var risks = await db.AttendanceRecords.Where(x => x.AcademyId == academyId).GroupBy(x => x.StudentId).Select(group => new { StudentId = group.Key, Records = group.Count(), Present = group.Count(x => x.Status == "Present" || x.Status == "Late" || x.Status == "Online") }).ToListAsync(token);
22:         var attendanceRisk = risks.Where(x => x.Records >= 3 && x.Present * 100m / x.Records < 75m).Select(x => new { x.StudentId, x.Records, AttendanceRate = Math.Round(x.Present * 100m / x.Records, 1) });
```

## Suspected root cause

Nullable/weakly enforced teacher reference is treated as guaranteed join result in in-memory projection.

## Business impact and blast radius

Admin intelligence response availability when historical or inconsistent session data exists.

## Related / required regression

OPERATIONS-INTELLIGENCE-ORPHAN-001: HTTP/SQL valid/missing/foreign/inactive/null teacher session matrix, workload totals, response status and no leakage across academy data.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
