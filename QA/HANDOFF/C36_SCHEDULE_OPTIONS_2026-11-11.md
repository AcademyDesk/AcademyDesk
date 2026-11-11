# C36 least-privilege scheduling workspace — 11 November 2026

MODEL:Sol High for authority/read intent;Medium for settled UI/docs.
TOOL:Cloud Codex. REPOSITORY:AcademyDesk/AcademyDesk.
BRANCH:codex/cloud-c36-schedule-options-20261111.
BASE:c2a49077685203a7dfe74a62ba76db14a71d4eb0,clean published C35.
DESKTOP latest f708879fc09a40a540c5d6f0fd5b3e814507bed4 already preserved;
Desktop owns protected invoice issuance. No overlap.
STATUS:CLAIMED;capture original HTTP/page failures before edits.
TASK:scheduling subset BUG-FUNC-0018;preserve existing scheduling domain rules.

Exact MAY MODIFY paths(10):

- apps/api/Controllers/ClassSessionsController.cs — GET options/minimal DTOs only
- apps/web/src/app/schedule/page.tsx — scoped options,read readiness/pending cleanup
- tests/AcademyDesk.Api.Tests/ScheduleOptionsAccessTests.cs
- QA/tools/schedule-options.test.cjs
- QA/tools/schedule-options-browser.cjs
- QA/tools/schedule-defaults.test.cjs — scoped fixtures/hooks only;retain payload/default assertions
- QA/ISSUES/BUG-FUNC-0018.md
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json
- QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md
- QA/HANDOFF/C36_SCHEDULE_OPTIONS_2026-11-11.md

MUST NOT MODIFY:permission/filter/module/Identity/session policy,existing server
List/Create/Update actions,admission/clash/teacher inheritance/scheduling services/
SQL/migrations,teacher/attendance/host/finance/Desktop invoice work,other pages,
globals/vendor/Mini,existing assertions/raw evidence. No main/Azure/environment
publication/public access/model loading/download/inference/training/activation.
Mini/Plus/Pro unqualified;Ultra deferred.

Policy:GET sessions/options inherits scheduling.manage/current global academy/
tenant/actor/module/maintenance gates. Return only batch id/name/teacherId/branchId,
teacher id/firstName/lastName and branch id/name,no tracking/writes/audit,no-store,
scoped/sorted. Default references must be local;foreign references project null.
Preserve default/override and IST→UTC intent,all existing server mutation rules.
No full batch/workforce/MultiBranch management grant or fallback. Validate the
workspace and gate writes/empty register until fresh reads succeed;preserve drafts,
reject stale/unmounted reads and duplicate pending writes,release busy state on
rejection,keep useful refusal/confirmed-vs-readback notices. No automatic replay
or new server concurrency policy;broader uncertain-write recovery remains separate.

VALIDATE:original/final actual HTTP/Identity/InMemory role/direct/custom-grant/
revocation/foreign/inactive/maintenance/Core/minimized/default/no-write matrix,
actual page read/race/failure tests,all18 retained scheduling-default controls,
native sandboxed Chromium320/1440 delegated profile/scoped reads/defaults/IST/
create/status/empty/refusal;exact-lock types/build/lint/full frontend/API/guard.
Retain genuine missing SQL receipts;no native SQL/device qualification.
STOP WHEN:scoped source/results/handoff feature-pushed and exact remote SHA verified.
NEXT ROUTE:Desktop Sol High real SQL/Identity/browser/device acceptance;Cloud next
separately claimed promotions/makeup/events scope. Model-loading stop remains.
