# Work-item ownership and linked-record integrity — BUG-DATA-0048

Date: 2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `588f3e0d228f82d3811bbbcd0f916c68159d374a`. Route: Sol High for Identity/tenant/domain-audit integrity. This is the existing E0/E1 queue, not a new audit or testing strategy.

## Reproduction and repair

- The unchanged starting controller passed 12/36 new tests and failed 24/36: invalid assignees, unsupported/cross-academy/mismatched links and oversized database fields were accepted. These are test cases, not 24 independent defects. Baseline TRX remains local.
- Both general work creation and FinanceGovernance collections follow-up now use the same Identity guard: optional null is valid; a supplied assignee must exist in this academy, be active, not a Platform Owner, and not currently locked out. Teacher/Student/Guardian roles, including mixed staff/portal identities, are excluded consistently with the existing staff roster. Owner/Admin, normal and custom non-portal staff remain supported. Assignment grants no role or permission.
- Linked records must have both fields or neither, a nonempty ID, a supported exact type and a matching academy-owned record. The allowlist is the two existing application-generated links: Invoice and PersonDocument. Collections links require Invoice; Compliance links require PersonDocument. Unlinked tasks remain valid. Blank type plus null ID is normalized to null. No dynamic table resolution, new arbitrary entity vocabulary, migration or legacy row rewrite.
- Existing database string bounds are checked before writes: type 60, title 240, description/note 4000. Existing canonical priorities/defaults are retained; the alternative finance creation path now rejects fabricated priorities instead of storing them.
- General creation opts into the existing domain/audit transaction, including already-authorized Platform Owner actions. The attribute adds atomicity, not authority. Existing FinanceControls transaction, invoice balance calculation, route/role/tenant guards, list filters, completion timestamps and collection-state APIs remain intact.
- Manual work queue no longer offers explicitly inactive staff. Historical assigned tasks whose staff are unavailable are labelled honestly, not silently changed to Unassigned. Authoritative safe 4xx explanations are rendered with retained drafts; existing 5xx/network uncertainty, durable success, pending/readback guard, optional null payloads and no-auto-retry behaviour remain. No full portal/style redesign is claimed.

## Verification

| Layer | Result |
| --- | --- |
| New controller tests | 36/36 PASS; baseline 12 PASS / 24 FAIL |
| New + retained collections/task/audit tests | 100/100 PASS; retained assertions unchanged, constructor fixture wiring only |
| Actual TSX handler tests | 21 unchanged plus 3 additive cases: 24/24 PASS |
| Real SQL / Identity / authenticated HTTP | 79/79 PASS, no HTTP 429 |
| Installed TypeScript / targeted page ESLint | PASS, lint 0 errors / 0 warnings |
| Production webpack build/export | PASS, 83/83 pages |
| Exported responsive browser | 32/32 synthetic cases PASS; 320/1440px, light/dark |

Native matrix: ten invalid-assignee modes through both creation paths; invalid partial/type/table/tenant/missing links; field/priority guards; anonymous/Teacher/Student/Finance/foreign-admin queue restrictions; foreign routes and record patches; optional assignment and valid local links; tenant-scoped filtered reads; fresh SQL completion/reset timestamps; existing collection-state HTTP success; missing/foreign invoice follow-ups; suspended academy; teacher/student financial denial. Success creation checks exact added row/defaults/references, unchanged prior and foreign sentinel rows, unrelated finance/Identity snapshots and one attributed academy audit.

SQL DENY INSERT on only the owned AuditLogs table verifies general and finance creation return 500 with no domain/audit changes. Restoring the permission verifies recovery. This is creation audit rollback, not acceptance of every existing status mutation's audit-failure path. Throttles are unchanged and 429 is not treated as success.

The unchanged responsive browser matrix exercises general create/status actions against controlled transport: confirmed success, failed readback,400 rejection and committed-but500 uncertainty. Draft/reset/no-repeat/polite notice/one-write and no-overflow assertions pass. Synthetic success retains the fixture's historical201 response, while native API checks assert the actual200 contract. This is not linked SQL or physical-device evidence. New inactive-owner/error-detail cases are proven by actual-handler checks, not falsely attributed to this unchanged browser matrix. Phone light-success and dark-rejection screenshots were visually inspected.

## Execution and cleanup

- Run `98c4b1324a354c3e83002ed528ceea48`: 79/79 native checks, exit 0, 83.2 seconds. Application/Identity migrations 88/7 and scoped runtime login verified.
- Negative cleanup refused a mismatched database marker, then the owned database/login/container were removed. Exact container `academydesk-qa-98c4b1324a354c3e83002ed528ceea48` absent in subsequent inspection. No prune or unrelated container removal.
- TRX and SQL/handler logs under `QA/EVIDENCE/work-item-integrity-20261011/` remain intentionally local. `.build-check` remains ignored. No production database or credential changes.
- Final synthetic browser result: `QA/EVIDENCE/work-queue-feedback-browser-1791695910269/result.json`,32 cases. No browser console errors beyond asserted synthetic rejection/readback status failures.
- Frozen QA inventory is not regenerated; its recorded source/commit binding is historical. Successor report/issue/checklist and diff consistency are checked separately.

## Limits and next route

BUG-DATA-0048 remains **OPEN / RETEST**. No direct linked-browser-to-SQL, physical Android/iOS, concurrent offboarding/deletion guarantee, legacy invalid-row remediation or full critical/release acceptance is claimed. Identity and record existence validation is point-in-time; it does not lock against a concurrent account revocation or record deletion. Existing status/audit fault and broader workflow gates remain open. The API status remains 200 on creation; no new permissions or PENTA execution tools are enabled.

Feature-branch publication only; unrelated Mini/private/Platform work and lower handoff/README additions are preserved and excluded. No main merge, Azure deployment, PENTA Core change or new Mini handoff. Existing FW1 AI frontend handoff and Manual Orbit + Aura dark / Prism light rollout remain queued.

NEXT: existing BUG-DATA-0049 normalized duplicate identities within student import; inspect latest evidence and reproduce only the missing cases. Sol High for import persistence/tenant/audit decisions; Sol Medium for settled Manual visual rollout later.

## Commands

```powershell
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/work-item-integrity --filter "FullyQualifiedName~WorkItemIntegrityTests|FullyQualifiedName~CollectionTaskTests|FullyQualifiedName~CollectionsBalanceTests|FullyQualifiedName~AuditOutcomeTests"
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/work-item-integrity-sql
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module WorkItemIntegrity
node --test QA/tools/work-queue-feedback.test.cjs
# After building/exporting apps/web with its installed toolchain:
node QA/tools/work-queue-feedback-browser.cjs
```
