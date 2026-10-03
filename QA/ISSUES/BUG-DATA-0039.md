# BUG-DATA-0039 — Next-class make-ups drop the room of in-person sessions

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | ACCEPTED STATIC DIAGNOSIS / bounded local repair verified |
| Final verification | PARTIAL:516 backend (39 new)/50 real HTTP-SQL/53 controlled TSX PASS; browser/lifecycle/critical pending |
| Severity | Major scheduling location loss |
| Priority | P1 |
| Category | DATA |
| Module | SCHEDULE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /makeup; /calendar |
| API | POST makeup-classes |
| Environment | Local isolated owned SQL/Identity HTTP and actual controlled TSX; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | SCHEDULE-MAKEUP-LOCATION-001 |
| Evidence classification | Accepted static diagnosis plus repaired direct/API-SQL/controlled TSX; no old-code runtime reproduction claimed |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/MakeupClassesController.cs:27 |
| Class/function | Create UseNextScheduledClass |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Historical source excerpt below plus QA/REPORTS/PHASE_2B_MAKEUP_LOCATION_REPAIR.md/source snapshot |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create isolated scheduled session with standard InPerson delivery and RoomName Studio A. Request a next-scheduled make-up for that batch and inspect saved venue and queued notification. |
| API response | Repaired200 full summary/readback and400/403/401 asserted; captured synthetic GET fixture |
| Database before/after | Fresh full summary, queued notices/success audit and unchanged rejected/source snapshots verified |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted API/form/calendar repair; no deployment |
| Retest result | PASS bounded mode-specific location mapping, manual/inherited controls and queued notices |
| Regression result | 516 backend/50 HTTP-SQL/53 controlled TSX PASS; failed harness attempts retained/excluded; browser/lifecycle OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Local repair checkpoint — 2026-10-01

[Repair report](../REPORTS/PHASE_2B_MAKEUP_LOCATION_REPAIR.md): user clarified Offline uses room only, Online/Hybrid use meeting link only. Inherited InPerson canonicalizes to Offline, retaining room; optional rooms and required virtual links preserved; incompatible fields cleared. Manual/inherited form validation and calendar matching locations repaired.39 new backend tests/516 total,50 real HTTP-SQL and53 controlled TSX (22 new/31 reused) PASS. No external message delivered or legacy data rewritten. OPEN for live browser/device/status/lifecycle/critical; local only, no commit/deploy.

## Historical baseline reproduction

Create isolated scheduled session with standard InPerson delivery and RoomName Studio A. Request a next-scheduled make-up for that batch and inspect saved venue and queued notification.

## Expected

Inherited physical make-up retains the source session room and a consistent delivery mode.

## Historical baseline / evidence

Session creation uses InPerson, but make-up inheritance copies RoomName only when mode is Offline. It stores InPerson with null venue/link, bypassing its earlier Online/Offline/Hybrid request allowlist. Notification therefore lacks the room. Runtime NOT RUN.

Source snapshot:

```text
26:             if (session is null) return BadRequest(new { message = "There is no upcoming scheduled class for this batch. Select a make-up date and time instead." });
27:             startUtc = session.StartUtc; endUtc = session.EndUtc; teacherId = session.TeacherId ?? batch.TeacherId; deliveryMode = session.DeliveryMode; venue = session.DeliveryMode == "Offline" ? session.RoomName : null; meetingLink = session.DeliveryMode is "Online" or "Hybrid" ? session.RoomName : null;
28:         }
29:         else { if (!request.StartUtc.HasValue) return BadRequest(new { message = "Select a make-up date and time." }); startUtc = request.StartUtc.Value; endUtc = startUtc.AddMinutes(batch.SessionMinutes is > 0 ? batch.SessionMinutes : 60); }
```

## Suspected root cause

Session and make-up physical-delivery vocabularies differ at the inheritance boundary.

## Business impact and blast radius

Next-scheduled physical make-up venue, directory, calendar and queued location notice.

## Related / required regression

SCHEDULE-MAKEUP-LOCATION-001: Full HTTP/SQL InPerson/legacy Offline/Online/Hybrid source sessions with room/link/null; compare inherited record, list/calendar and notification text without delivering messages.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
