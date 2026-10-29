# BUG-DATA-0006 — Enrollment activation, transfer and promotion bypass admission invariants

## Shared PENTA/native operational backend successor — 28 October 2026, OPEN

Native create/update/transfer now use one shared implementation, preserving the
existing admission lock/prerequisite/capacity/duplicate/history rules and audit
boundary. New PENTA service independently enforces current Owner/Admin database
authority in a shared SQL/Identity transaction; no tool activated.3,183API/zero
skips,57new real SQL/HTTP/service +28retained lifecycle cases and36CI guards PASS.
Five mixed Manual/service last-seat and five duplicate races each give201/409,
one active row. Five transfer/create races give one winner/409 and preserve the
losing source. Six mandatory audit faults roll back create/update/transfer. Current
source/exact restoration/cleanup qualifier973096375be64b9aaf67878869808f3a.
The old lifecycle success fixture now enables waitlist capacity; all28 assertions
and independent disabled/full-capacity refusals remain. No new promotion rule,
model/conversational acceptance or broad issue closure. Issue remains OPEN.
See `../REPORTS/PENTA_ENROLLMENT_SHARED_2026-10-28.md`.

## Waitlist lifecycle successor — 24 October 2026, locally qualified

Later [configuration successor](../REPORTS/ADMISSION_CONFIGURATION_SERIALIZATION_2026-10-24.md)
also qualified:66ordinary HTTP checks/three actual pending-config/application-lock
wait chains/three required-audit rollback controls. Exact service/inventory/cleanup/
lease and independent qualification PASS; no backend change/rebuild.147focused
QA tools PASS. Later [admission authority](../REPORTS/ADMISSION_AUTHORITY_2026-10-24.md)
qualified84HTTP checks/31role-tenant-foreign-ID denials/two authorized readbacks;
12table hashes unchanged per operation and no protected sentinel disclosure.
Exact originals/inventory/cleanup/lease and independent qualifier PASS.
Wider role/custom-grant/guardian/device/full critical/release gates stay OPEN.

[Waitlist lifecycle report](../REPORTS/ADMISSION_WAITLIST_LIFECYCLE_2026-10-24.md):
current development API build `f42f065951044f48bf7e55b75bc9727b` reused.
SQL `a423cd348ab14082a79e7d00331770b9`, window
`fb9b3552625541d8aabc587d96bfbbf8` PASS78HTTP checks/10outcomes,
three barrier-controlled waitlist-place pairs and two required-audit rollback/
recovery controls. Same-row identity, retained nullable fields and capacity/status
counts verified. Independent source-pinned qualification PASS; exact original
services restored healthy/full inventory/owned cleanup/lease release PASS.
No new backend or product-policy change in this successor. Separate enrollment
feedback repair also passed current-export browser validation:46HTTP checks,
four normal and four authoritative refusal journeys; see
[feedback report](../REPORTS/ENROLLMENT_CONFLICT_FEEDBACK_2026-10-24.md).
Physical devices, wider transition/configuration/security/release coverage and
full critical regression remain open. This issue remains OPEN.

## Policy/admission lock successor — 24 October 2026, locally qualified

[Policy serialization report](../REPORTS/ADMISSION_POLICY_SERIALIZATION_2026-10-24.md):
native baseline confirmed actual policy writer blocked on graph while admission
escaped201/one enrollment. AddPrerequisite now takes the shared academy admission
lock before graph lock, preserving graph checks and required-audit transaction.
Full2927API tests/build PASS; fresh development build
`f42f065951044f48bf7e55b75bc9727b` changes only AcademicGovernanceController.
Repaired SQL `e6f42852846249e38b51e47ee6c1fb4b` PASS48checks/three actual
blocking/audit cases, owned cleanup and original services/inventory restored.
Its parent window `fcdd4ac478f146efbe036ed974d7f6dc` remains FAIL because the
supplementary actual-image inspection was refused by the3GiB memory guard.
Payload-only retry also refused2.926GiB before execution; exact originals restored
healthy/full inventory/lease release PASS. No active validation run.
Later payload-only window `e3eb6f719afa4b0bbc2384e02f463f7e` PASS65actual-image
files equal publish; exact originals healthy/full inventory/lease PASS.
Independent source-pinned policy successor qualification PASS without rerunning
SQL/API/browser; original failed envelopes stay failed. Issue/remaining transition/
device/release gates remain OPEN, not whole enterprise acceptance.

## Admission-rule successor — completed locally, 24 October 2026

[New rule report](../REPORTS/API_IMAGE_ADMISSION_RULES_2026-10-23.md): owner
approved same-transaction promotion source-course completion credit and rejection
of duplicate Active/Waitlisted admissions. Shared tenant/student-scoped completion
and duplicate checks now cover Create, activation, transfer and promotion;
waitlist entry transitions enforce enabled/spare capacity. Historical records
remain intact.48new tests reproduced27failures before repair, then full2927API
tests PASS. Fresh DEVELOPMENT image `1ee2ef4724e345ac82eb8660c94f7992` built;
exact original services/inventories restored. Native successor385HTTP checks/
17rule outcomes/27barrier pairs/six required-audit rollback controls PASS. Four
real mobile/desktop light/dark browser journeys and65actual-image file hashes PASS;
130QA-tool tests and independent source-pinned successor qualification PASS.
Exact originals healthy, full inventories preserved, owned resources/lease removed.
Remaining configuration-race/device/release gates stay OPEN. Older proof below is historical
to its original image, not current-source qualification. Issue stays OPEN.

## Local capacity repair/native successor — 23 October 2026

[Repair report](../REPORTS/API_IMAGE_ADMISSION_REPAIR_2026-10-23.md): shared SQL
transaction-owned academy lock spans enrollment create/update/transfer, promotion
decision and batch capacity edits. Activation open/capacity/duplicate guards and
promotion target capacity guard added without changing prerequisite/waitlist
policy. Development image `3798bb0533ff4ba9a57ee3f3bafad484`; standard Dockerfile
release build remains unqualified after offline NuGet restore failure.
Corrected native SQL `dce68fefd4ac413e98e81ab1acd638d0`, window
`3d80147374d64793a020b9d3d3c79b74` PASS145checks/15barrier pairs/five real
audit-failure rollback-recovery controls; exact originals healthy/inventories/
cleanup/lease PASS.113focused and full2879API tests PASS; affected browser36checks/
four real light/dark mobile/desktop journeys and exact final restoration/inventory
PASS.150QA-tool tests/independent development qualifier PASS. First promotion-fixture403
and original reproduction FAIL retained. No claim of full admission parity,
prerequisite/transfer-duplicate/waitlist-policy repair, physical or release closure.
Issue stays OPEN.

## Current immutable-image activation reproduction — 23 October 2026

[Native negative report](../REPORTS/API_IMAGE_ADMISSION_REPRODUCTION_2026-10-23.md):
SQL `0a81a6a9409b442bbd52510922058076`, window
`21350108d4a342c79d0818421c46d6fa`:36setup/security/domain seeds then actual
Owner PUT200 activates Waitlisted into a full capacity1batch. Fresh SQL shows
two Active rows instead of one. Expected409/no-change gate FAIL, not an accepted
repair. Exact owned cleanup/original services healthy/full inventory/lease release
PASS. Initial transport failure retained separately, not domain proof.
Activation capacity is now RUNTIME-CONFIRMED; other prerequisite/transfer/
promotion/duplicate/race paths below retain their earlier separate scope.
No application fix, closure or deployment. High repair packet linked in report.

## Native happy-path scope clarification — 23 October 2026

[Enrollment native report](../REPORTS/API_IMAGE_ENROLLMENT_BROWSER_2026-10-23.md)
adds four real create/duplicate/pause/resume journeys with exact SQL/audits and
optional-null/pending/cancel/reason guards. Those passed against an open batch
with spare capacity and no prerequisites. They do **not** repair or reproduce
the admission-invariant bypasses below, nor prove concurrent capacity/transfer/
promotion correctness. This issue remains OPEN / STATIC-FINDING pending its
specific negative/race reproduction and shared admission-path repair. Do not
close it from the new successful normal-flow browser receipt.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Original activation bypass RUNTIME-CONFIRMED; current bounded capacity/rule/policy/waitlist/configuration successor cases qualified; wider gates OPEN |
| Final verification | Local rule385checks/27pairs/six audit controls; current lifecycle78checks/three pairs/two audit controls and configuration66checks/three wait chains/three audit controls PASS; current UI46checks/eight journeys PASS; role/device/full critical/release OPEN |
| Severity | Major enrollment integrity |
| Priority | P1 |
| Category | DATA |
| Module | BATCH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /enrollments; /batch-promotions |
| API | PUT enrollments/{id}; POST enrollments/{id}/transfer; PATCH batch-promotions/{id}/decision |
| Environment | Current immutable API with disposable local SQL/real Kestrel/Identity; production state not inferred |
| Device/viewport | Current enrollment export passed eight native browser journeys at390/1440 light/dark; physical Android/iOS and wider UI cases NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | BATCH-RULE-001 |
| Evidence classification | Native SQL/API original reproduction plus bounded repaired race/rule/policy/lifecycle/configuration and linked browser successors; no whole-domain closure |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Original native activation bypass reproduced; repaired sequential negative and 15 barrier pairs PASS; other admission findings retain static scope |
| Source | apps/api/Controllers/EnrollmentsController.cs:50 |
| Class/function | Enrollments.Update/Transfer / BatchPromotions.Decide |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Activate a waitlisted enrollment in a full batch via Update. Transfer to a batch whose prerequisites are unmet or where the student already has enrollment. |
| API response | Original activation 200 bypass retained; repaired activation 409/no-write and concurrent outcomes captured in linked repair report |
| Database before/after | Disposable native SQL snapshots verified for capacity repair and audit rollback; other admission invariants not yet runtime accepted |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Capacity repair implemented locally, not committed; other admission findings remain OPEN |
| Retest result | Development-image activation409/no-change and15capacity pairs PASS; original failure retained, wider gates OPEN |
| Regression result | Full 2,927 API tests, 130 focused QA-tool tests and affected native SQL/browser regression PASS on the rule-successor development image; earlier 150-tool checkpoint retained historically; full release gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Activate a waitlisted enrollment in a full batch via Update. Transfer to a batch whose prerequisites are unmet or where the student already has enrollment.

## Expected

All paths into active enrollment enforce agreed capacity, duplicate and prerequisite rules.

## Original actual / evidence (historical source snapshot)

Create checks those rules; Update only validates status/reason; Transfer checks capacity/open state but not prerequisite/duplicate parity. Promotion approval inserts Active target enrollment without checking target active/open state, capacity, prerequisites or existing target enrollment. Creation itself also permits repeated Waitlisted rows because its duplicate check only checks Active.

Source snapshot:

```text
49:         if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.LifecycleReason)) return BadRequest(new { message = "A lifecycle reason is required when changing an enrolment from Active." });
50:         enrollment.Status = status; enrollment.EndDate = request.EndDate; enrollment.LifecycleReason = string.IsNullOrWhiteSpace(request.LifecycleReason) ? null : request.LifecycleReason.Trim();
51:         await dbContext.SaveChangesAsync(token);
52:         return Ok(new EnrollmentSummary(enrollment.Id, enrollment.StudentId, enrollment.BatchId, enrollment.StartDate, enrollment.EndDate, enrollment.Status));
```

## Suspected root cause

Invariants implemented only in create path.

The original excerpt above is not current repaired source. The local capacity
boundary and activation guards now cover the paths described in the repair
report; prerequisite and transfer/waitlist duplicate-policy parity remain open.

## Business impact and blast radius

Roster capacity, learning prerequisites, duplicated billing/session participation.

## Related / required regression

BATCH-RULE-001: Transition matrix plus concurrent creation/activation/transfer/promotion SQL tests; no duplicate active/waitlisted enrollment and all approved admission guards on every path.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
