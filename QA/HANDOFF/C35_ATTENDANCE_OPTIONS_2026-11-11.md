# C35 least-privilege attendance workspace — 11 November 2026

MODEL:Sol High for authority/roster/read isolation;Medium for settled UI/docs.
TOOL:Cloud Codex. REPOSITORY:AcademyDesk/AcademyDesk.
BRANCH:codex/cloud-c35-attendance-options-20261111.
BASE:c4bd654f01d6b8ebf44219b68ea850e366c3736a,clean published C34.
DESKTOP latest observedfa2e19d30504734a4790341f3546d64e0b073836 already preserved;
Desktop owns Admin payment approval/recovery flow. No overlap.
STATUS:CLAIMED;capture original actual HTTP/page failures before edits.
TASK:attendance subset of BUG-FUNC-0018;preserve current note/session safety.

Exact MAY MODIFY paths(11):

- apps/api/Controllers/AttendanceController.cs — absolute GET attendance/options and minimal DTOs only
- apps/web/src/app/attendance/page.tsx — scoped workspace/read readiness;preserve mark payload/record-generation/note behavior
- tests/AcademyDesk.Api.Tests/AttendanceOptionsAccessTests.cs
- QA/tools/attendance-options.test.cjs
- QA/tools/attendance-options-browser.cjs
- QA/tools/attendance-isolation.test.cjs — scoped synthetic fixture only;retain assertions
- QA/tools/attendance-notes.test.cjs — scoped synthetic fixture only;retain assertions
- QA/ISSUES/BUG-FUNC-0018.md
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md
- QA/HANDOFF/C35_ATTENDANCE_OPTIONS_2026-11-11.md

MUST NOT MODIFY:permission/filter/module/Identity/session policy,attendance List/
Mark action bodies,status/note/active-enrollment validation,teacher portal/host,
scheduling/admission/domain/SQL/migrations,finance/PENTA,new Desktop work,other
pages,globals/vendor/Mini,existing assertions/raw evidence. No main/Azure/
environment publication/public access/model download/load/inference/training/
activation. Mini/Plus/Pro unqualified;Ultra deferred.

Policy:GET api/academies/{academyId}/attendance/options inherits attendance.manage
and current global academy/tenant/actor/module/maintenance checks. Only minimal
batch labels,session id/batchId/startUtc,active enrollment studentId/batchId/status
and referenced student id/firstName/lastName. Scope every relation to local student/
batch/academy,omit nonactive enrollment/private records,deduplicate roster pairs,
no tracking/writes/audit,no-store. Preserve existing server attendance write rules,
including historical session behavior;options are not authorization/reservations.
Client uses options then existing selected-session attendance GET;never falls back
to unrelated full APIs. Validate all required arrays/ids/relations/dates;failed or
obsolete workspace reads must not fabricate zero roster/no schedule. Provide
GET-only reload,preserve note drafts and existing record/read/save generation
checks;retired workspace reads/saves cannot unlock another roster. No business
write retry/replay added;broader uncertain-save recovery is separate.

VALIDATE:original/final actual HTTP/Identity/InMemory role/direct/custom-grant/
revocation/expired/foreign/inactive/maintenance/owner/Core/minimized/active-roster/
no-write matrix;actual TSX route/schema/readiness controls;all retained attendance
notes/isolation tests;native sandboxed Chromium320/1440 Operations/Manager with
synthetic options/records/notes/failures/session transitions;exact-lock types/
build/lint/full frontend/API/release guard. Preserve missing Cloud SQL receipts.
STOP WHEN:bounded source/checks/source-bound handoff feature-pushed and exact
remote SHA verified. No automatic issue closure/native SQL/device qualification.
NEXT ROUTE:Desktop Sol High actual SQL/Identity/browser/device acceptance;Cloud
next separately claimed remaining0018 workflow. Model-loading stop remains.
