# BUG-FUNC-0030 — Submission review omits student and assignment identity

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONFIRMED: bounded baseline execution; local repair |
| Final verification | PARTIAL: backend/controlled TSX/HTTP-SQL PASS; browser/closure OPEN |
| Severity | Major review attribution ambiguity |
| Priority | P1 |
| Category | FUNC |
| Module | LEARNING |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /submission-review |
| API | GET assignment-submissions; PATCH assignment-submissions/{id}/review |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | LEARNING-REVIEW-CONTEXT-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/submission-review/page.tsx:8 |
| Class/function | SubmissionReviewPage list / review |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Seed two learners submitting identical text to different assignments in an isolated fixture. Open administrative Submission Review and identify whose work each Review action targets using only visible page content. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted repair; no push/deployment |
| Retest result | Bounded local PASS; see Phase 2B report |
| Regression result | 821 backend /15 controlled TSX /21 real HTTP-SQL PASS; not full closure |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Seed two learners submitting identical text to different assignments in an isolated fixture. Open administrative Submission Review and identify whose work each Review action targets using only visible page content.

## Expected

Each review row identifies the learner and assignment, with sufficient batch/submission context to give feedback to the intended record.

## Actual / evidence

Frontend retains only id/status/responseText/teacherFeedback and renders status plus response. It loads no student/assignment lookup and offers no identifying labels or filter; feedback prompt also lacks identity. API raw rows contain StudentId and AssignmentId but UI does not use them. Runtime NOT RUN.

Source snapshot:

```text
7: type Academy = { id: string };
8: type Submission = {
9:   id: string;
10:   status: string;
```

## Suspected root cause

Review display projection omits the identifying context required to select a record safely.

## Business impact and blast radius

Administrative review of multiple learner assignments; risks mistaken feedback even though PATCH uses the clicked row ID.

## Related / required regression

LEARNING-REVIEW-CONTEXT-001: Browser identical/empty response fixtures across students/assignments/batches, visible identity and accessible Review labels, cancel/confirm/failure paths and exact persisted feedback target. Include mobile wrapping and keyboard focus.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Phase 2B local repair — 2026-10-01

2026-10-10 bounded successor [feedback/browser report](../REPORTS/PHASE_2B_SUBMISSION_REVIEW_FEEDBACK_CHECK.md):15 original assertions retained +7 controls22/22 and24 synthetic production-export browser cases PASS at320/1440 light/dark. Identical names/titles/responses remain distinguished by IDs/batch; keyboard prompt confirmation/cancel, exact target, malformed/wrong-student refusal, rejected/uncertain writes and restored actions covered. Original context/UTC/authority contract unchanged; only stale notice clears on confirmed save. No live SQL/physical device/screen-reader/concurrent reviewers/critical/release acceptance; issue remains OPEN. Earlier21 real SQL/821 backend result historical, not rerun.

[Execution report](../REPORTS/PHASE_2B_REVIEW_CONTEXT_REPAIR.md):8 expected baseline controller failures;821 backend (8 new),15 controlled TSX and21 real Identity/global-filter/HTTP-SQL checks PASS. Existing GET/review PATCH preserve raw fields and add independently tenant-scoped student/assignment/batch labels. UI identifies exact row in visible/accessibility/prompt context, retains null fallbacks, prevents page-local duplicate clicks and announces matching success or failure. UTC time does not shift when SQL omits its suffix. Exact SQL target/audit preserved, denied/read requests no-write. Initial fixture-only SQL failure retained, corrected and excluded. Issue remains OPEN for browser/mobile/keyboard/screen-reader, linked/critical, concurrency/fault and release verification; creation BUG-DATA-0042 not altered. Dev services/database/Azure untouched.
