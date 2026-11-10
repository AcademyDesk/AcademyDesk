# C29 explicit guardian revocation and inactive-domain access — 10 November 2026

MODEL: Sol High for privacy/authorization/atomicity; Medium for settled docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c29-guardian-access-20261110.
BASE:a713a506853733215111d4fb29fc725f14c521d8 (clean published C28).
DESKTOP:7d5c013c7312a91a6bb472e86d3a7d8a4d34c891, unchanged on fresh fetch.
TASK: BUG-SEC-0006/0007 explicit per-link revocation and inactive guardian domain
checks on provisioning and portal child access. CLAIMED; reproduce originals first.

Exact MAY MODIFY paths:

- `apps/api/Controllers/StudentGuardiansController.cs` — explicit portal-access state, revoke/grant validation and existing domain/actor audit opt-in only
- `apps/api/Controllers/PortalAccountsController.cs` — inactive Guardian provisioning guard only; preserve existing Identity/SQL boundary and authorization
- `apps/api/Controllers/PortalController.cs` — active owned guardian checks on Me/children/ParentAccess/profile; null-safe repeated permission reads
- `tests/AcademyDesk.Api.Tests/GuardianAccessLifecycleTests.cs`
- `tests/AcademyDesk.Api.Tests/GuardianProvisioningGuardTests.cs`
- `QA/ISSUES/BUG-SEC-0006.md`
- `QA/ISSUES/BUG-SEC-0007.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C29_GUARDIAN_ACCESS_2026-11-10.md`

MUST NOT MODIFY: protected PENTA/scheduling/attendance/enrollment, SQL harness or
migrations, shared auth/filter/session/Identity transaction code, guardian update
workflow, frontend/vendor/Mini or unrelated work. Desktop retains regular
scheduling/assigned-Teacher attendance and native SQL/device ownership. No main,
Azure, environment publication/replacement/public-access or model execution.
Preserve the stop on model loading/download/inference/training/activation;
Mini/Plus/Pro remain unqualified and Ultra deferred.

An explicit false revokes the selected guardian's link at every student age;
it clears all subordinate flags and records revoke time. Explicit true preserves
existing grant/subpermission defaults but cannot grant inactive domain records.
Missing/null state rejects; false must never mean a missing value. Preserve the
existing minor guardian requirement/default on initial link/onboarding. Revoking
one guardian must not revoke other guardians, siblings or the Student's access.

Inactive guardian must not provision a new usable account or read child records
with an otherwise active Identity. Recheck current domain state on each request;
do not reactivate guardians or manipulate identity credentials/tokens. Existing
active scoped paths, student access, guardian subpermissions and role/tenant
checks remain. Current provisioning requires matching real SQL/Identity stores;
do not bypass this boundary to manufacture an InMemory HTTP success. Guard-only
direct-controller checks and real portal HTTP tests must be labelled separately.

VALIDATE: original/final HTTP/Identity/InMemory portal/link tests, guard-only
provisioning controller checks, retained family/privacy/media/identity tests and
full API suite; no SQL/audit rollback/concurrency/physical-device claim. Web source
stays unchanged; reuse exact source-bound C28 web evidence with hash proof. Retain
original failures and raw results outside Git; no evidence fabrication/skips.
STOP WHEN: bounded source fix, tests and ordered Desktop SQL/browser/device
handoff committed/pushed with exact feature SHA. Five P0/broader session and
remaining CI/provider/recovery/release gates stay open; no auto-closure.
NEXT ROUTE: Desktop Sol High for protected SQL, Identity/audit races and live role/
portal/device acceptance; Cloud next bounded nonoverlapping non-AI task.
