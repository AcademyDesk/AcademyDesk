# BUG-DATA-0031 — Batch edits, status toggles and teacher assignment reset unrepresented delivery settings

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / local repair |
| Final verification | Two fresh SQL runs x27, API237/237 and controlled frontend110/110 PASS; existing lint FAIL; browser/device/all-linked/critical pending |
| Severity | Major unintended batch data loss |
| Priority | P1 |
| Category | DATA |
| Module | BATCH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /batch-setup; /teachers |
| API | PUT batches/{batchId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | BATCH-PRESERVE-001 |
| Evidence classification | Accepted trace plus controlled TSX/unit and real Identity/HTTP/SQL reproduction and bounded repair |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/batches/page.tsx:183 |
| Class/function | saveBatch / toggleActive / TeachersPage.assignBatch / Batches.Apply |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Prepare a synthetic batch with code, waitlist capacity, Online delivery, OneToOne class type, schedule JSON, meeting link, room, notes and closed enrollment. On fresh populated fixtures, edit only name, toggle active, and assign a teacher from /teachers. Compare complete persisted rows for each path. |
| API response | Baseline13 cases reproduce loss/rejected online assignment; final list/create/update projections preserve extended settings |
| Database before/after | Two fresh SQL runs x27: complete batch/session rows, captured unrelated state/audit count, explicit null clear and rejected no-write |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted repair; HEAD unchanged, no push/Azure |
| Retest result | PARTIAL PASS — original-case/nullable/inherited-binding SQL and controlled frontend; live browser pending |
| Regression result | API237/237, controlled frontend110/110, SQL x27 twice, TypeScript/build PASS; pre-existing lint2 errors/4 warnings unchanged |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Prepare a synthetic batch with code, waitlist capacity, Online delivery, OneToOne class type, schedule JSON, meeting link, room, notes and closed enrollment. On fresh populated fixtures, edit only name, toggle active, and assign a teacher from /teachers. Compare complete persisted rows for each path.

## Expected

Changing visible fields, active state or assigned teacher preserves all unrelated settings; intentional clearing is explicit.

## Actual / evidence

Historical accepted finding: Batch-setup edit/toggle and TeachersPage.assignBatch omit unrepresented replacement settings; edit also clears endDate. The API list already returned MeetingLink, but the frontend Batch model/assignment omitted it. Summary omitted class type/session length/frequency/teaching-day JSON. Real baseline13 cases confirmed loss and Online/Hybrid assignment rejection; inherited optional request properties bind correctly. [Local repair/evidence](../REPORTS/PHASE_2B_BATCH_PRESERVATION_REPAIR.md) adds complete summaries and a shared payload builder for the three paths. Two fresh final SQL runs x27 PASS. Raw API replacement/validation/concurrency behavior remains unchanged; browser/device/critical acceptance and previously lost production data recovery are not claimed.

Source snapshot:

```text
182:   }
183:   async function saveBatch(batch: Batch) {
184:     if (!editName.trim() || !editCourseId)
185:       return setMessage("Batch name and course are required.");
```

## Suspected root cause

Abbreviated UI payloads are sent to a replacement operation without preserving fields not represented in the editor.

## Business impact and blast radius

Batch delivery, schedule metadata and teacher assignment reliability; list summary omits several settings so UI-only readback cannot prove preservation.

## Related / required regression

BATCH-PRESERVE-001: Browser plus real HTTP/SQL fully populated/null edit/deactivate/reactivate/assign-teacher; compare every unrelated property, deliberate clear, Online/Hybrid validation and rejected-update no-write. Test inherited optional record properties via actual model binding rather than assuming they cannot deserialize.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
