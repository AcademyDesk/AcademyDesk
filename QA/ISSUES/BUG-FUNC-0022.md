# BUG-FUNC-0022 — Manual make-ups do not honor Use class teacher

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major assignment mismatch |
| Priority | P1 |
| Category | FUNC |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /makeup |
| API | POST makeup-classes |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-MAKEUP-TEACHER-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/MakeupClassesController.cs:22 |
| Class/function | Create manual branch / MakeupPage teacher selector |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | For a synthetic batch with assigned teacher leave the teacher selector on Use class teacher, choose manual date/time and submit. Compare explicit override and next-scheduled controls. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

For a synthetic batch with assigned teacher leave the teacher selector on Use class teacher, choose manual date/time and submit. Compare explicit override and next-scheduled controls.

## Expected

Default Use class teacher inherits batch teacher; explicit teacher overrides deliberately.

## Actual / evidence

UI posts null teacher for its default. Manual branch keeps request.TeacherId unchanged, while only next-scheduled branch applies session/batch fallback. Manual row remains unassigned despite the displayed promise. Runtime NOT RUN.

Source snapshot:

```text
21:         if (!new[] { "Online", "Offline", "Hybrid" }.Contains(deliveryMode, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Venue must be Online, Offline, or Hybrid." });
22:         DateTime startUtc; DateTime endUtc; Guid? teacherId = request.TeacherId; string? venue = request.Venue?.Trim(); string? meetingLink = request.MeetingLink?.Trim();
23:         if (request.UseNextScheduledClass)
24:         {
```

## Suspected root cause

Teacher fallback is implemented for only one scheduling mode.

## Business impact and blast radius

Manual make-up teacher assignment and downstream readers relying on TeacherId.

## Related / required regression

SCHEDULE-MAKEUP-TEACHER-001: Browser/HTTP/SQL assigned/unassigned batch, inherited/explicit/foreign/inactive teacher, manual/next-scheduled, and fresh readback assertions.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
