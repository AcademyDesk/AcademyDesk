# BUG-DATA-0013 — Access-review sign-off omits reviewer identity and record correlation

## Qualified linked browser return — 26 October 2026, OPEN

Backend2995tests/native49HTTP/four rollback controls now linked to fresh UI41
checks/627file export/eight synthetic journeys/four actual Owner-login native
sign-offs/both themes/mobile-desktop/one additional audit-fault recovery. Exact
four browser reviews/two tasks/eight audits/actor correlations/13table fingerprints/
owned cleanup/original services/inventory/API200/independent qualifier PASS.
SQL3a0152f3563f44d883c4310f9355f566/window a7c362f7409842f8a9915f03dc702168.
See `QA/REPORTS/REVIEW_SIGNOFF_FEEDBACK_2026-10-26.md`. Wider concurrency,
physical devices and release remain OPEN; no full issue or production closure.

## Qualified provenance repair — 26 October 2026, OPEN

Fresh API3368e36621f942bfac353b8ac92824da2995tests/65actual image files;
SQL23c4ffbcb9054108a85c6505a62f3b2f49HTTP/five attributed sign-offs/four
500rollback controls/final5reviews3linked tasks10audits. Spoofed actor/scope fields
ignored, role/tenant/module refusals and overlong400remain no-write. All13tables
unchanged on faults. Exact originals/inventory/cleanup/independent qualifier PASS.
Frontend18targeted/41combined checks pass; fresh lint/build/browser pending. Separate
sign-off concurrency/devices/wider release remain OPEN; historical table below
describes original discovery, not the current scoped runtime evidence.

## 25 October 2026 provenance/audit continuation — OPEN

Runtime baseline confirmed46ordinary HTTP checks/three missing-provenance200
sign-offs/two unsafe audit outcomes. Ordinary generic audit500 leaves review/task
saved; platform200has no required audit. Anonymous/foreign/Teacher/Student HTTP
refusals remained enforced. Disposable SQL `90c6a162794c44b0b100f1569ab612fc`,
window `39a3d8718f764732af258c87af9be85e`; exact cleanup/originals/inventory PASS.

Minimal controller repair uses the existing server UserManager, links review/
optional task/correlated domain audit/receipt, and opts into existing domain audit
transaction including the existing platform path. Follow-up remains unassigned,
notes remain optional; overlong follow-up description fails400before save. No
schema, model tool or role/tenant permission expansion. All2995API tests PASS,
including13new real EF/UserManager/controller/boundary cases. Fresh image/native
rollback/recovery/qualification pending; see
`QA/REPORTS/ACCESS_REVIEW_PROVENANCE_2026-10-25.md`.

This is not issue closure; concurrency/live sign-off UI/device/critical/release
acceptance remains OPEN. Static discovery metadata below is historical.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Moderate governance traceability gap |
| Priority | P2 |
| Category | DATA |
| Module | OPERATIONS |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /access-review/sign-off |
| API | POST access-reviews |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | AUTH-REVIEW-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/AccessReviewsController.cs:3 |
| Class/function | SignOff |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Have two synthetic academy administrators submit reviews, with and without follow-up. Inspect review ReviewerUserId and generic audit metadata; attempt exact association to review IDs. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Have two synthetic academy administrators submit reviews, with and without follow-up. Inspect review ReviewerUserId and generic audit metadata; attempt exact association to review IDs.

## Expected

Each sign-off records authenticated reviewer and follow-up correlation sufficient to identify the attested review reliably.

## Actual / evidence

ReviewerUserId is never assigned. Generic audit logs contain actor and route but no created review ID; follow-up has no EntityId or AssignedUserId. Platform-flag path bypasses generic audit entirely.

Source snapshot:

```text
2: namespace AcademyDesk.Api.Controllers;
3: [ApiController][Route("api/academies/{academyId:guid}/access-reviews")]public sealed class AccessReviewsController(AcademyDeskDbContext db):ControllerBase{[HttpGet]public async Task<ActionResult> List(Guid academyId,CancellationToken t)=>Ok(await db.AccessReviews.AsNoTracking().Where(x=>x.AcademyId==academyId).OrderByDescending(x=>x.ReviewedAtUtc).ToListAsync(t));[HttpPost]public async Task<ActionResult> SignOff(Guid academyId,AccessReviewRequest r,CancellationToken t){db.AccessReviews.Add(new AccessReview{AcademyId=academyId,Notes=r.Notes?.Trim()});if(r.CreateFollowUp)db.AdminWorkItems.Add(new AdminWorkItem{AcademyId=academyId,Type="AccessReview",Title="Resolve access review actions",Description=r.Notes?.Trim(),Priority="High"});await db.SaveChangesAsync(t);return Ok();}}
4: public sealed record AccessReviewRequest(string? Notes,bool CreateFollowUp);
5:
```

## Suspected root cause

Sign-off saves notes/time without assigning review provenance or linking the optional work item.

## Business impact and blast radius

Reliable review attribution and remediation correlation; generic audit exists for ordinary admin writes but is not a substitute for exact record linkage.

## Related / required regression

AUTH-REVIEW-001: Two reviewers/concurrent sign-offs, optional follow-up, platform-owner path; assert persisted reviewer identity, review-to-task association and audit correlation without accepting client-supplied actor.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
