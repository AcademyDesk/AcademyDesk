# C31 explicit maintenance notice and safe client recovery — 10 November 2026

MODEL: Sol High for session/transport isolation; Medium for settled presentation.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c31-maintenance-notice-20261110.
BASE:51cd043c68c637d05726debd61beb21e100e29f9, clean published C30.
DESKTOP:fed2914 protected regular rescheduling imported with provenance; no client
maintenance path overlap. Desktop retains batch teacher reassignment next.
TASK: C30/BUG-FUNC-0010 client maintenance feedback and GET-only recovery.
STATUS: IMPLEMENTED / CLOUD VALIDATED; native acceptance and release remain OPEN.

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

## Source and ownership return

Implementation: `b8dec194391c7ec7b8f5220f2305e3657e7da7b6`.
Claim published and verified before source edits:
`3a33e5627ce847328f47c0091170c073bf65ce2d`.
Imported Desktop rescheduling `fed29147cee23c272073a8da9131fc709b722a76`
with provenance at `ff4575d7de60fcaf811acd4a7c50e12531a57feb`.
Desktop advanced during C31 to batch reassignment
`0758493c3bee2f07dd1b49220bed2fe46be3e46d`, imported with provenance at
`70b4dd4259e7dd3e872cc9a007e50902664a0e58`.
All18 imported paths, including migration source and native harnesses, were
byte-compared to Desktop's published commit. No migration was applied by Cloud.
Desktop now owns deterministic finance reads for PENTA; don't edit those paths.
Cloud's12-path claim remains the only Cloud-authored packet scope. Root shared
handoffs retain Desktop's update; this separate report is the Cloud return.

The eight implementation/test paths are committed together. Issue, this report
and both derived inventory paths complete the12-path claim. Final publication
SHA is verified using `git ls-remote`, recorded in the final response and private
`publication.json`; the documentation commit cannot contain its own SHA.
No main, Azure, repository-settings, workflow dispatch, PR, environment settings,
public access or model changes were made. The cloud is observed running/connected
with current observations and restricted network enforced. A null VPN failure is
not laptop/service readiness. No D: or laptop access is claimed.

## Resulting behavior and limits

The new authenticated GET returns only maintenanceMode, restricted and message.
It uses the exact shared canonical settings read and current server owner flag,
does not create settings, and permits no profile/business mutation. No existing
auth/profile/password action body or authentication registration was changed.
Its exact action joins the maintenance recovery allowlist; no prefix bypass.
No-store successful responses keep current availability out of caches.

Transport returns the exact original Response immediately. An asynchronous clone
checks at most8192bytes for up to1500ms; malformed, generic, foreign, obsolete,
aborted and cross-workspace results do not signal. Oversize/slow hints are ignored
and the original failed request remains available to its caller. A signal holds
only workspace, login epoch and serial; no credentials or saved message. Full
session continuity is checked again after parsing and cached hints are invalidated
on session changes.401 renewal retains its old rules; a503 after that one allowed
replay can signal the renewed own session without further replay or refresh.

The shared notice is inside the unchanged route/session boundary, including AI
routes, and is an unconditional sibling of page content. Login/register bypass
is unchanged. No page reload, navigation, draft reset or form remount is involved.
No initial speculative status request is sent on every page. A current hint starts
an explicit GET, checks can be requested manually or on focus/every30s while
monitoring, and a10s bound releases a stuck status read. Checks coalesce. A newer
hint discards an older result and queues one fresh read. No other response can
declare recovery. Failed/malformed/unauthorized status keeps truthful unknown
guidance and any previous verified message. Confirmed recovery stops polling and
asks the user to review saved records before deliberately submitting again.

Modern login epochs allow ordinary token renewal without dropping the notice;
legacy epoch-less sessions require the original access token. Unmount, abort,
logout, replacement, foreign workspace events and late results are covered.
Messages are React text, with wrapping for long values; no HTML execution. Current
owner restriction=false offers the actual Settings recovery route. An owner
normally bypasses maintenance503, so this reactive notice does not independently
announce every saved maintenance setting to owners. The dedicated GET can still
read it and test current owner-flag changes. No mutation authority is inferred
from client state. Existing uncertain-save page safeguards remain authoritative.

C30's full boundary still applies: MVC operational /api actions only; exact
session/password recovery, Identity minimal endpoints, health, static files,
background jobs and already-started actions keep their declared behavior. This
is not a complete database write freeze or a backup/restore mechanism. Internal
Testing-only PENTA context and model-free host contracts remain unchanged.

## Actual checks and evidence

Private raw output: `/workspace/c31-maintenance-notice-20261110`, outside Git.
Original failures are retained alongside final output; no tests were skipped,
softened or supplied with invented SQL receipts.

| Check | Observed result |
| --- | --- |
| Original availability HTTP/Identity/InMemory |15 cases:2 PASS/13 FAIL (missing route404) |
| Original client test file |Failed before helper/component implementation; retained log |
| First implementation API attempts |Stale compiled API artifact still lacked the method; diagnostic route/reflection checks retained the failures; after recompilation route and assertions passed |
| Current focused API |42/42 PASS:16 new availability and26 retained C30 enforcement |
| Full API before latest Desktop import |2,431/2,431 PASS;zero skips |
| Combined API after Desktop0758493 |2,431/2,431 PASS;zero skips;fresh compile/full regression;no native SQL acceptance inferred |
| Actual helper/TSX controlled tests |35/35 PASS, including clone bound, expiry, renewal, serial/race, stale login, owner text, unknown/recovery and GET-only checks |
| Native Chromium |32/32 PASS:4 workspaces ×320/1440 ×recovery/generic503/failed-status/logout; browser sandbox enabled, owned profile removed |
| Native draft scope |Actual Admin grading form input identity and values preserved; exactly one attempted blocked POST, zero replay; other3 workspaces cover notices/status/logout with synthetic primary read failures |
| Build |Locked cached Next16.4.3/React19.2.8 optimized webpack export PASS;87 static aliases,570 files; no dependency installation |
| Types / lint |TypeScript PASS;lint0 errors/61 retained warnings |
| Full current frontend |88 files;2,378 PASS/3 FAIL/2,381 tests;zero skips/cancellations |
| Frontend failures |Only communication-drafts45/48; absent Cloud `QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`; Desktop's historical receipts are not present here |
| Release guard |15/15 controls PASS;real guard exit1, five P0 records plus broader-session gate stay open |
| Issue inventory |120 records:5P0/102P1/13P2;0 literal Closed+Pass;no input errors;not120 untouched defects or a completion percentage |
| Hosted CI |UNKNOWN: `gh run list` request for this exact feature branch returned Forbidden; no bypass/retry/dispatch. Workflow push filters do not include this branch |

Commands: dotnet test with --no-restore/-p:UseSharedCompilation=false for the
focused and combined runs; --no-build for the first full run; TRX retained.
`node QA/tools/maintenance-notice.test.cjs`, native runner with installed
`QA_BROWSER_EXECUTABLE=/usr/bin/chromium`, `node QA/tools/run-frontend-tests.cjs`,
`tsc --noEmit`, `npm run lint`,
`NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 npm run build -- --webpack`,
`node --test QA/tools/production-gates.test.cjs`,
`node QA/tools/check-production-gates.cjs`,
`node QA/tools/priority-issue-status.cjs --write`.
An initial mistyped guard invocation targeted a nonexistent filename and was
retained; the actual check-production-gates result above is the release verdict.
No external audit disclosure was retried. Current credential/environment values,
private raw logs and test artifacts are not published.

## Prioritized remaining acceptance / next owners

1. Desktop Sol High: integrate the eight C31 implementation/test paths and this
   return selectively, retaining its newer finance/host work. On isolated real
   Identity/SQL verify saved mode on/off with the same bearer, current owner
   changes, exact recovery/no-store, no status-write/audit, and existing ordinary
   role/tenant refusals. Retain C30 owner-audit/replica/in-flight boundary tasks.
2. Desktop: actual authenticated browser with the SQL backend for all4 workspaces,
   interrupted ordinary writes, retained form state, zero replay, focus/check,
   owner recovery, failed/slow status and logout/replacement. Extend to native
   devices/other engines; synthetic Chromium does not replace these checks.
3. Finish five P0 acceptance records in existing order: DATA0001/0002 finance,
   reminder/family and permission/currency agreement; DATA0003 payroll identity,
   positive-net/history/print; DATA0010 audited void/evidence restore;
   SEC0001 private media authorization/recovery and actual capture/devices.
   AUTH-REVOCATION001's broader SECURITY-SESSION001 is an additional mandatory
   release scenario, including remaining roles/devices/critical workflows.
4. Release integrator: exact-source hosted CI and fresh security/dependency audit,
   combined release images/runtime, SQL migration/backup/restore/rollback and
   staging acceptance. Preserve the three missing Cloud receipt failures until
   genuine source-bound input exists. No issue is automatically closed.
5. Cloud: continue a separately claimed current non-AI client/workflow acceptance
   gap after reading current source/regressions; do not redo already accepted
   audits or overlap Desktop's deterministic PENTA finance reads. Remaining
   Manual/native/release gates mean AI is not the only work left.

Mini/Plus/Pro remain unqualified, Ultra deferred.90% eligible conversational
outcomes remains a target without measured acceptance. Deterministic Testing-only
host checks are not intelligence or model qualification. Candidate loading,
download, inference, training and activation remain stopped.
