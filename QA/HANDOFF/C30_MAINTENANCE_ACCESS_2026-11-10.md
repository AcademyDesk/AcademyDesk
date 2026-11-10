# C30 enforce saved platform maintenance mode — 10 November 2026

MODEL: Sol High for operational access and owner recovery; Medium for settled docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c30-maintenance-access-20261110.
BASE:4b9f48af6926175ecfb26af9355c656bf4068207 (clean published C29).
DESKTOP:29c0a940102e1436304e67c16cd339c1d81204b1; fresh fetch unchanged.
TASK: BUG-FUNC-0010 saved maintenance mode enforcement for MVC operational API
reads/writes under /api, with defined platform-owner and authentication recovery.
SOURCE FIXED / Cloud verification complete; source pin recorded;
native SQL/replica/browser/device acceptance remains open.

Exact MAY MODIFY paths:

- `apps/api/Security/AcademyAccessFilter.cs` — invoke maintenance gate before the academy-route early return; preserve existing authorization/audit/transaction logic
- `apps/api/Security/PlatformMaintenancePolicy.cs` — read-only current setting and exact recovery-action policy
- `apps/api/Controllers/PlatformControlController.cs` — deterministic singleton tie ordering matching the read gate only
- `tests/AcademyDesk.Api.Tests/PlatformMaintenanceAccessTests.cs`
- `QA/ISSUES/BUG-FUNC-0010.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C30_MAINTENANCE_ACCESS_2026-11-10.md`

MUST NOT MODIFY: Program/DI, Identity/auth/session implementations, protected
PENTA/attendance/scheduling/enrollment, SQL harness/migrations, frontend/vendor/
Mini, raw accepted evidence or unrelated work. No main/Azure/environment/public-
access changes or model execution. Desktop retains assigned-Teacher attendance,
protected regular rescheduling, native SQL/host/device ownership.

Policy: while the current singleton maintenance flag is true, ordinary MVC
business reads and writes under /api return503/platform_maintenance with the saved plain
status message or a clear default, no-store, before action execution or success
actor audit. Only the current server-resolved IsPlatformOwner flag bypasses this
gate; academy Owner/Admin and spoofed/stale role labels do not. Existing endpoint
permissions/tenant/module/audit controls still run for exempt/bypass actors.
Anonymous operational requests keep authentication refusal instead of a mode
response; no owner bypass is inferred from a missing Identity.

Identity minimal endpoints (including login/refresh/registration/account
management/recovery) and /health keep their
existing pipeline outside this MVC filter. Exact MVC recovery actions are current
session GET and authenticated password changes (session and family variants),
with existing security validation intact. Profile edits/images and ordinary
Portal/Teacher/API operations are blocked. No prefix-wide authentication exemption.
Maintenance does not revoke identities, drain already-started work or govern
background jobs/static assets; release backup/migration gates remain separate.
The pre-existing internal Testing-only leased context endpoint is outside /api;
its model-free/no-database host-composition contract and every lease/security gate
remain unchanged. This scope is exercised by its retained eight host checks.

Read the newest UpdatedAtUtc-or-CreatedAtUtc setting, then descending Id on ties,
matching platform settings selection. The gate never creates, cleans or writes
settings. A missing/off record preserves existing behavior; no stale cache. Owner
GET/settings cleanup remains the existing contract, with only tie ordering added.

VALIDATE: actual HTTP/Identity/InMemory owner save true/false + existing bearer,
ordinary read/write/no-write snapshots, scoped and unscoped controllers, all
relevant roles, tenant/permission refusal after disabling, current owner checks,
recovery auth/health, new/missing/duplicate/tied settings, saved/default message
and cache headers. Full API + unchanged release guard; label native SQL/replica/
browser/device acceptance separately. Web source stays unchanged and compatible
C28 export/build/browser evidence may be reused by matching hashes.
STOP WHEN: bounded source/tests and ordered native handoff are feature-published
with exact source/result pins. Five P0/broader session and all remaining production
acceptance gates stay open; no automatic issue closure. Model loading/download/
inference/training/activation remains stopped; Mini/Plus/Pro unqualified, Ultra
deferred,90% conversational-outcome coverage not accepted.
NEXT ROUTE: Desktop Sol High for real SQL/audit/role/browser/replica/in-flight
acceptance; Cloud next bounded nonoverlapping non-AI task.


## Source result and scope

Implementation: `acc0c9748edcc262831816056d84e9b0769b85b1`. Original claim
ba10ecf8c95ce05e4db9f7bbcf4afd0aee03a4ba was pushed before source edits.
This is an application maintenance control in isolated fixtures; the Cloud
execution environment and any real platform settings were not changed.

The existing global action filter now reads current maintenance state after authentication and before
operational /api controller actions, including controllers without academyId.
Authenticated nonowners receive503 with code platform_maintenance, a plain
saved/default message and Cache-Control:no-store. The action, application file/
Identity writes and successful actor audit are not executed. Anonymous requests
keep401; existing authentication/role/model-binding/request-limit failures keep
their validation path. A role label alone never creates a bypass. Current server
IsPlatformOwner retains the existing endpoint controls and can inspect/disable
maintenance. Flag changes affect the next request using an existing bearer;
no cache or identity revocation is introduced.

The current setting is newest UpdatedAtUtc-or-CreatedAtUtc then descending Id.
The read gate does not create/delete/normalize settings. Platform settings' own
existing singleton selection/cleanup adds the same tie ordering; no other owner
settings/audit logic changes. Missing/off settings preserve business behavior.

Authentication minimal endpoints, health and exact MVC session/password recovery
remain as declared above. The internal Testing-only leased snapshot endpoint is
outside /api and retains its no-database/model-free composition and every guard.
Program, private host/attendance/scheduling and global Identity/security-stamp
implementations were not edited. Current private-file and business actions under
/api are gated before their controller actions. A maintenance operation is not
an in-flight-work drain, background job stop, transaction freeze, static-file
shutdown or replacement for staging backup/restore procedures. Existing Identity
minimal endpoints include registration/account management; they retain their
current authentication/rate/security behavior. Do not treat this mode as a
complete database-write freeze.

## Verification boundary

| Check | Actual result / limitations |
| --- | --- |
| Original maintenance controls |26 actual HTTP/Identity/InMemory cases:4 PASS/22 FAIL; saved true did not restrict operational reads/writes. Raw source/log/TRX retained |
| First repaired target |110 PASS/1 FAIL:anonymous request got503 instead of the existing401. Retained failure; fixed missing-actor handling without relaxing the assertion |
| Corrected target |111/111 PASS:26 maintenance plus85 retained audit and guardian checks |
| Scoped target |119/119 PASS, including8 retained actual default-host composition checks. Internal private lease, environment/flag refusals, two-read bound and absent model services preserved; no replacement database/interpreter supplied by those host fixtures |
| Initial full API |2,414 PASS/1 FAIL:retained anonymous holiday authorization/no-SQL check caught a maintenance query before authentication. Retained failure; authenticated-only maintenance lookup restores the earlier no-SQL refusal contract |
| Final corrected target | 124/124 PASS:26 new maintenance and98 retained audit/guardian/default-host/holiday controls;zero skips |
| Full combined API |2,415/2,415 PASS;zero failures/skips on final C30+Desktop29c0a94 source |
| Web/build/browser reuse |All164 tracked web source hashes and570 export files match pinned C28 implementation db012648e608e1fa5cf7291ce0271af3f4b1407d/publication a713a506853733215111d4fb29fc725f14c521d8. Its build/types/lint,29 controlled and24 native synthetic cases passed;full frontend2,343 PASS/3 genuine missing-SQL-receipt failures. These are retained unchanged-UI results, not a new C30 maintenance browser/device run |
| Release guard |15/15 controls PASS; actual guard exit1 keeps five P0 records and the required broader-session acceptance record open |
| Hosted CI |UNKNOWN; this feature push does not trigger verify.yml main/release-export-security filters. No workflow dispatch/PR/main merge/Azure deployment |

The26 new HTTP cases cover10 actual roles (Owner,Admin,Sales,Marketing,FrontDesk,
Finance,Operations,Teacher,Guardian,Student), scoped and unscoped reads/writes,
saved/default message and cache headers, fresh no-write snapshots, real owner
PUT enable/disable with the same actor bearer, spoofed owner-role labels, current
server owner-flag removal, owner recovery and preserved later403 tenant/permission
refusals, login/session/refresh/health/password validation recovery, blocked profile
upload without application file/Identity changes, no-row/legacy newest/tied
settings and read-only gate behavior. Only synthetic users/data were used.

Temporary upload roots are unique owned c30-maintenance directories. They were
removed by the fixture after baseline and final runs; zero owned roots remained
at the recorded check. A C# fixture declaration compile error was corrected
before the original execution and retained separately. No existing assertion
was skipped or weakened. The first full-run anonymous/no-SQL failure was retained;
its original test is unchanged and included in the corrected final target. Production SQL, replica/in-flight, actual user/browser
and physical-device acceptance is not established by InMemory or prior UI runs.

Raw originals, all logs/TRX (including the caught anonymous regression), exact
web reuse hashes, resource cleanup/checks, guard/inventory and source/assembly/
publication manifest remain outside Git at`/workspace/c30-maintenance-access-20261110`.
No QA/EVIDENCE, temporary upload or generated export is pushed.

## Desktop handoff, in priority order

1. Integrate C30's source/result pins or its exact8 claimed paths, preserving
   newer host/attendance/scheduling, SQL and local work. Reconcile the small
   AcademyAccessFilter maintenance prelude while retaining its complete current
   authorization/audit logic; do not replace a newer shared filter wholesale.
   Regenerate both derived inventory files from current integrated issue records.
2. On disposable real Identity/HTTP/SQL, save maintenance true/false as the actual
   platform owner. Verify ordinary current-role API reads and writes are503,
   no action/application-file/Identity/domain/audit writes occur, and next requests
   with the same bearer recover after disabling. Repeat role/tenant/module and
   private download/PENTA business API cases without enabling real models.
3. Verify owner recovery from an existing and fresh login, exact session/password
   recovery, current owner-flag removal, spoofed owner labels,401/403 handling,
   health and default/saved plain message/no-store behavior. Existing operational
   permissions must still deny after disabling; owner bypass grants no new tool
   or private-file permission. Keep Identity minimal endpoints' declared behavior.
4. Validate SQL newest/null-update/tied ordering, owner singleton cleanup and
   read-only ordinary gate, owner settings/audit failure rollback, concurrent
   toggles/requests and multi-replica freshness. Document handling of already
   in-flight requests and background jobs; this action gate does not drain them.
5. Run real owner/academy/Teacher/Guardian/Student UI under maintenance, including
   login and recovery, forms, uploads/private downloads, service-message/error
   display, GET-only retries and deliberate resubmission after mode is off.
   Complete actual browser/device, linked critical/documented acceptance before
   closing BUG-FUNC-0010. Current UI is unchanged; no dedicated new maintenance
   screen or new maintenance browser acceptance is claimed here.

## Remaining production and AI gates

The issue stays OPEN for native SQL/replica/audit/browser/device and linked
acceptance. All120 issue records stay inventoried (5 P0/102 P1/13 P2), not120
untouched defects or a completion percentage. Five P0s and broader-session,
all-role/physical-device, prior Cloud returns' native acceptance, real providers/
meetings/delivery, exact candidate hosted CI/current security/image checks and
staging backup/restore/rollback remain open. Do not retry the previously rejected
external npm audit disclosure under Continue.

Desktop continues assigned-Teacher attendance/protected regular rescheduling and
native host/SQL/migration/data/device ownership. Cloud can continue independent
non-AI source defects after publication. The next UI candidate is explicit
maintenance feedback and read-only recovery from the503 code; claim the actual
client paths first, and preserve current session renewal and draft safeguards. Non-AI entry acceptance is not complete;
keep the explicit stop on model loading/download/inference/training/activation.
Mini/Plus/Pro remain unqualified, Ultra deferred and90% eligible conversational
outcomes unaccepted. No main/Azure/environment publication/public-access change.
