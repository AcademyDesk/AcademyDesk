# E5-R4 — shared onboarding implementation and real SQL regression

17 October 2026. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved uncommitted work. Continues E5-R3 using the existing ERP implementation; not a new backend or a restart of enterprise testing.

## Implemented

Moved existing student/guardian/account creation from `StudentOnboardingController` into scoped `StudentOnboardingService`. The existing POST route, request record, response field names, validation messages, optional account behaviour and `AtomicAcademyMutation(IncludeIdentity = true, IncludePlatformOwner = true)` remain. Manual now calls the shared implementation rather than maintaining a second creation path for future AI use.

The service re-reads current actor activity, academy and Owner/AcademyAdmin membership, and checks active academy/module access for non-platform actors. Relational calls require both SQL Server contexts enlisted in the identical underlying transaction before any data/Identity work; missing or separate transactions and mixed SQL/nonrelational providers fail closed with 503. Existing same-academy platform-plus-admin opt-in behaviour is retained. No model-supplied authority, roles, approval or tenant context is accepted.

This is a backend prerequisite, **not an enabled conversational write**. No new PENTA tool, approval endpoint, draft table, operational route or model activation. The service does not own the caller's approval/idempotency/audit ledger and its 200 outcome alone does not prove commit. The existing Manual host filter still owns transaction/audit/commit. A future AI caller must provide its own validated current authority, durable revision-bound approval, claim/concurrency control and atomic authoritative result/audit. Credentials remain secure Manual input and must never enter conversational payloads. Existing request placement is retained for source/wire compatibility.

## Validation actually run

- Normal SQL harness/API build and isolated onboarding harness build both passed with zero emitted warnings/errors. Isolated command: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql`.
- Final focused test receipt: `QA/EVIDENCE/penta-onboarding-service-20261017/onboarding-shared-service-final.trx`: **203/203 passed**, zero failures/errors/skips. Filter: `FullyQualifiedName~StudentOnboardingServiceTests|FullyQualifiedName~AuditOutcomeTests|FullyQualifiedName~Penta`. Three new transaction/provider guard cases plus 200 retained tests.
- Initial focused run had one test-only assertion error: calling GetDbConnection on the deliberately InMemory context. Corrected the assertion to inspect only relational connections; the three provider combinations and denial requirements were not weakened. Initial failed receipt remains local alongside final receipt.
- Windows PowerShell refused script execution under its existing policy before any resources were created. No execution policy changed. Ran the existing script with installed PowerShell 7: `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`.
- SQL run **`0577c9bae72949978b328a38db9b38bb`**, exit **0**, elapsed **46.7 seconds**. Runtime route inventory 333, controller method/routes 322, framework Identity method/routes 10; inventory SHA256 `3CA8D1FABA64A043749774D75B9EE01ABC90DA1428443AE2C2C7517BE1477728`. Scoped runtime login/target, health, real Identity login, bearer access and cross-tenant controls passed.
- **All original 21 HTTP/SQL/Identity onboarding cases passed unchanged**, including invalid parent password, duplicate parent, domain link/Identity user/role/audit failures, persisted-user membership failure, role-creation failure, successful accounts, omitted optional accounts, Owner/platform opt-in and denied anonymous/teacher/foreign-route calls. Fresh snapshots verify no domain/Identity/audit residue on failure and exact success/link/account/audit deltas.
- **Seven additive direct-service real SQL cases passed**: no boundary, separate transactions, teacher, foreign academy, anonymous, inactive actor and successful optional-account-free creation explicitly rolled back. Before/after snapshots match. The last case is deliberately NOT a committed/audited AI success.
- Runner confirmed **100 application / 7 Identity migrations**, refused mismatched-marker cleanup, removed only its owned database/login and then ownership-checked/stopped/removed `academydesk-qa-0577c9bae72949978b328a38db9b38bb`. Subsequent exact-name inventory confirms absence. No container pruning or other-container cleanup.
- Restarted the existing API at localhost5092; `/health` **200** and existing frontend localhost3000 `/login` **200**. Process-only Testing profile, same local database, migrations/bootstrap/seeder/media/PENTA disabled as in E5-R3. No developer database migration, account reset or successful developer Admin sign-in this batch. API session55481/frontend80830 remain local inspection runtimes, not service installation or production configuration.

Normal API/test API copies share SHA256 `349381C4BD4F9013CA23C568FD1AE628A5F92FD4F2F9A0E5FE990BE8662C9759`. Isolated API/harness API copies share `3569C97A46CA3A0DFDA6EE22CC6DA480C35115F75674D9556101C84830058F3B`. The two build locations produce different binaries; do not claim all four hashes identical. Existing local HTTPS-redirection and decimal precision warnings remain, not clean-runtime acceptance.

## Current source SHA256

| Source | SHA256 |
| --- | --- |
| apps/api/Infrastructure/StudentOnboardingService.cs | A1E0966FCA5B2F39EBD183C129BF1DD11BCCFDE2DCCB1545623C07B79808C7BA |
| apps/api/Controllers/StudentOnboardingController.cs | CCC5D2A77249B56375CC4CE6B2F318B149A8AFDDB975DE4E18FEC4B307FF25AD |
| tests/AcademyDesk.Api.Tests/StudentOnboardingServiceTests.cs | DFE2C221BFFF737C7241121C4755B25785B252EBA709707CAC26B938FFFF5B11 |
| QA/tools/SqlHarness/OnboardingAtomicRegression.cs | A5A96203B9DAF78D2483755E4DAC39F01FC9425B60FC8D4D5CA1651985E3B753 |

Program.cs adds only the scoped onboarding registration in this batch; its pre-existing uncommitted meeting registrations are preserved, not attributed here. Earlier source-inventory hashes are historical: onboarding controller intentionally changed in this batch. Full enterprise/model/browser/device suites and unrelated onboarding modules were not rerun.

## Remaining / next route

- [x] Retained earlier readiness recovery and payload/preparation/receipt binding checkpoints.
- [x] Shared existing onboarding implementation with direct-call boundary/current-authority checks, 203 focused tests and 28 named real SQL cases.
- [ ] Next, High integration route: durable host draft/revision/current-catalog/approval/status and atomic execution binding; no dispatch until those checks and paired Mini qualification are accepted.
- [ ] Verify developer Admin login with current credentials or separately authorized narrow local reset; no implied reset permission from “next.”
- [ ] Authenticated UI and real linked conversational execution across the existing AI-01–AI-14 scope, qualified Mini, remaining enterprise/device/provider/privacy/operations/exact-release gates. Manual visual migration remains deferred, fallback retained.

No primary-main or PENTA source change, new schema, stage/commit/push/merge/Azure operation or issue closure. Three repository HEADs unchanged, zero staged files, QA evidence local/untracked and .build-check ignored. Production remains **NOT READY**; 90% eligible AI operations is a target, not measured delivery.
