# C30 enforce saved platform maintenance mode — 10 November 2026

MODEL: Sol High for operational access and owner recovery; Medium for settled docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c30-maintenance-access-20261110.
BASE:4b9f48af6926175ecfb26af9355c656bf4068207 (clean published C29).
DESKTOP:29c0a940102e1436304e67c16cd339c1d81204b1; fresh fetch unchanged.
TASK: BUG-FUNC-0010 saved maintenance mode enforcement for MVC operational API
reads/writes, with defined platform-owner and authentication recovery. CLAIMED;
reproduce original behavior before editing source.

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
business reads and writes return503/platform_maintenance with the saved plain
status message or a clear default, no-store, before action execution or success
actor audit. Only the current server-resolved IsPlatformOwner flag bypasses this
gate; academy Owner/Admin and spoofed/stale role labels do not. Existing endpoint
permissions/tenant/module/audit controls still run for exempt/bypass actors.

Identity minimal endpoints (login/refresh/account recovery) and /health keep their
existing pipeline outside this MVC filter. Exact MVC recovery actions are current
session GET and authenticated password changes (session and family variants),
with existing security validation intact. Profile edits/images and ordinary
Portal/Teacher/API operations are blocked. No prefix-wide authentication exemption.
Maintenance does not revoke identities, drain already-started work or govern
background jobs/static assets; release backup/migration gates remain separate.

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
