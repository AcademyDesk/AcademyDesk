# C33 least-privilege enrollment lookups — 10 November 2026

MODEL: Sol High for authorization/data minimization;Medium for settled UI/docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c33-enrollment-options-20261110.
BASE:28be47a5d85f397ea01ab36dbe12a09eb0b5207f (clean published C32).
DESKTOP latest observed241810e488fcd7a1951dd0b341ae1c63a75b77fa already preserved;
Desktop owns protected expense/payroll finance reads. No overlap.
STATUS: CLAIMED; reproduce FrontDesk lookup refusal before changes.
TASK: enrollment subset of BUG-FUNC-0018; preserve C32 lifecycle/recovery.

Exact MAY MODIFY paths (10):

- `apps/api/Controllers/EnrollmentsController.cs` — GET options and minimal DTOs only;all existing actions unchanged
- `apps/web/src/app/enrollments/page.tsx` — consume scoped options;preserve C32 lifecycle/receipt/recovery
- `tests/AcademyDesk.Api.Tests/EnrollmentOptionsAccessTests.cs`
- `QA/tools/enrollment-options.test.cjs`
- `QA/tools/enrollment-lifecycle.test.cjs` — synthetic fixture route/schema only;retain all assertions
- `QA/tools/enrollment-lifecycle-browser.cjs` — strict scoped lookup fixtures/FrontDesk role;retain40 scenarios and no-replay assertions
- `QA/ISSUES/BUG-FUNC-0018.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C33_ENROLLMENT_OPTIONS_2026-11-10.md`

MUST NOT MODIFY: permission catalog/filter/module rules,Identity/session logic,
existing enrollment mutation/domain/admission/host/SQL/migrations,finance,
other workflow pages,globals/vendor/Mini,unrelated work or existing evidence.
No main merge,Azure/environment publication/public access/model download/load/
inference/training/activation. Mini/Plus/Pro unqualified;Ultra deferred.

Policy: GET enrollments/options inherits the same existing students.manage,
current actor/academy/module/tenant checks as enrollment List. Return only student
id/firstName/lastName and batch id/name, sorted deterministically,no tracking/no
writes/no-store. Include historical inactive/closed references to preserve naming;
server admission still controls selectable target validity. Never return contacts,
financial/teacher/branch/private detail or mutate lookup records. No permission
expansion to full batch reads/writes or unrelated management. Platform-owner
behavior matches existing filter policy. Client rejects malformed/partial options,
retains C32 drafts and recovery and never falls back to broader endpoints.
Remaining0018 batch/schedule/attendance/promotions/makeup/events scopes are separate.

VALIDATE: original/final actual HTTP/Identity/InMemory FrontDesk/delegated-grant/
revoked/expired/denied/inactive/foreign/maintenance/owner/empty/minimal/no-write
matrix;exact client route/schema controls;retain all38 C32 controlled and40 native
sandboxed Chromium scenarios at320/1440 with synthetic FrontDesk fixtures;types,
exact-lock build/lint/full frontend/API/release guard. Preserve failures privately;
no fabricated SQL receipts or weakened assertions. No native SQL claim.
STOP WHEN: scoped implementation/checks/source-bound handoff feature-pushed and
remote SHA verified. No automatic issue closure/production acceptance.
NEXT ROUTE: Desktop Sol High actual SQL/Identity/browser/device acceptance;
Cloud next separately claimed non-AI gap. Model-loading stop remains.
