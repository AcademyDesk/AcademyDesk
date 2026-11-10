# C31 explicit maintenance notice and safe client recovery — 10 November 2026

MODEL: Sol High for session/transport isolation; Medium for settled presentation.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c31-maintenance-notice-20261110.
BASE:51cd043c68c637d05726debd61beb21e100e29f9, clean published C30.
DESKTOP:fed2914 protected regular rescheduling imported with provenance; no client
maintenance path overlap. Desktop retains batch teacher reassignment next.
TASK: C30/BUG-FUNC-0010 client maintenance feedback and GET-only recovery.
STATUS: CLAIMED; capture original behavior before changes.

Exact MAY MODIFY paths:

- `apps/api/Controllers/AuthSessionController.cs` — authenticated availability GET only; existing session/profile/password actions unchanged
- `apps/api/Security/PlatformMaintenancePolicy.cs` — exact availability action recovery exemption only
- `tests/AcademyDesk.Api.Tests/MaintenanceAvailabilityTests.cs`
- `apps/web/src/lib/api.ts` — bounded asynchronous maintenance-response hint only; preserve raw responses, authentication, renewal, no503 replay and workspace/session guards
- `apps/web/src/components/service-availability-notice.tsx`
- `apps/web/src/components/workspace-frame.tsx` — mount notice inside existing session boundary without altering its checks or conditionally unmounting forms
- `QA/tools/maintenance-notice.test.cjs`
- `QA/tools/maintenance-notice-browser.cjs`
- `QA/ISSUES/BUG-FUNC-0010.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C31_MAINTENANCE_NOTICE_2026-11-10.md`

MUST NOT MODIFY: auth/Identity token lifecycle logic, server maintenance enforcement,
Program/DI/protected PENTA/SQL/migrations/scheduling/attendance, business forms,
shared session boundary rules, globals/vendor/Mini, previous tests/evidence or
unrelated work. No main/Azure/environment publication/public access or model
loading/download/inference/training/activation. Mini/Plus/Pro unqualified; Ultra
deferred;90% eligible conversational outcomes and production not qualified.

Policy: validate structured platform_maintenance503 asynchronously on a bounded
clone, preserving the original Response and never refreshing/replaying503. Only
an initiating current own session can signal; late/foreign/workspace/auth-change
responses are ignored. Signals contain no credentials or owner status message.
The client verifies current state through the dedicated authenticated no-store
availability GET, including actual server owner restriction; auth/session200 or
an arbitrary business200 cannot establish recovery. Generic503 is not maintenance.

No speculative initial GET on every public/login view. A current maintenance
signal starts the notice's status read; active/unknown maintenance can be checked
explicitly and on focus/limited interval. Requests coalesce; queued newer signals
invalidate an older status result. Preserve all entered values and child mounts;
no refresh/navigation/reload, POST replay, implicit new business intent or logout.
A successful status check may announce availability, but user must review/reload
business records and deliberately submit using the existing page safeguards.
Respect all four workspace/login epochs, logout, replacement, unmount, abort and
legacy token continuity. Render saved status as text, with clear owner recovery.

VALIDATE: original/final actual HTTP availability contract, source-controlled VM
helper/component races, full retained refresh/logout/cross-tab tests, native
sandboxed Chromium with synthetic current status/faults/drafts at320/1440 and
all workspaces, exact-lock build/types/lint/full frontend/API and release guard.
Preserve original failures/raw results outside Git; no weakened checks or fabricated
SQL receipts. Native SQL/browser/device acceptance remains Desktop-owned.
STOP WHEN: bounded source and source-bound handoff feature-published with exact
SHA, current checks and explicit remaining native/release gates. No auto-closure.
NEXT ROUTE: Desktop Sol High for actual Identity/SQL/role/live browser/device
acceptance; Cloud next independent non-AI issue. Model loading remains stopped.
