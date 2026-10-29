# E5-R5 — durable encrypted onboarding drafts and exact-revision review

17 October 2026. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved local work. Continues E5-R4's shared domain-service extraction; no ERP rebuild or repeated Mini qualification run.

## Delivered, and exact boundary

Added `PentaOnboardingDraft` and an **inactive, Testing-only** `PentaOnboardingDraftService`. It is not registered in DI, has no controller/route, and requires explicit `Penta:OperationalDraftContractsEnabled`. Neither the existing read pilot nor the synthetic draft/approval ledger is modified or promoted. The candidate has no domain service dependency, tool dispatch, inference, send or account provisioning.

The SQL-backed aggregate stores academy/actor/session/context/revision/status/expiry metadata and a purpose-bound Data Protection ciphertext. Canonical fields, payload digest and current-authority fingerprint are inside the encrypted body, not plain SQL/audit fields. Protection binds academy, actor, session and draft IDs; decrypted metadata must match the SQL row. Only ten bounded onboarding fields are admitted: student names, DOB, number, email and phone, and parent names, email and phone. Unknown tools/fields, credential fields and wrong typed values are rejected. This is a bounded host candidate, not the complete onboarding catalog or a new shared PENTA contract.

Each operation uses the existing matching-configured-store SQL guard and requires idle contexts with no pending changes. It discards only unchanged domain tracking cache before its own transaction. SQL academy locking serializes edits/confirmations; current Identity/role/academy records are queried without relying on tracked entities or token role claims. Only active same-academy Owner/AcademyAdmin users are admitted; Platform Owner is intentionally not borrowed into academy authorization. Fresh security/concurrency stamp, roles and academy policy/settings bind the review. Other actors/conversations cannot read the draft.

Initial saves support partial details; full canonical replacement is explicit rather than a patch. Omission clears a field, and retained fields must be supplied by the paired dialogue adapter. Exact identical save replays return the same revision without another audit or expiry extension. Accepted edits increase revision and invalidate confirmation. Drafts are bounded to ten minutes and cannot outlive their session; expired drafts cannot be edited or confirmed. Twenty drafts per actor / 100 per academy per day and 100 revisions bound this candidate.

Confirmation checks exact revision and canonical hash, current authority/session/context, expiry and required student/guardian details. Repeated and concurrent identical confirmations create one transition. A changed draft, context, security stamp or current permission cannot use the old review. Required draft/read/confirmation audits contain bounded IDs, policy, tool and revision, **not private fields or the payload hash**; audit failures roll back the corresponding draft/review transition.

**Confirmed is only a durable review record, not an execution approval.** Every returned view has `CanExecute = false`. `ReadyForReview` means required details are present under these limited checks; it is NOT complete business validity, duplicate clearance, reference validation, quota eligibility or an approved domain request. The future executor must independently reauthorize, perform all existing domain validation/duplicate/reference checks, bind the complete reviewed operation, consume confirmation once under concurrency control, and commit domain/Identity/result/audit atomically. No student/account is created by this service.

## Schema and verification

Scaffolded `20261017054835_AddPentaOnboardingDrafts` through the existing **offline** `QaDesignTimeContextFactory` (`--qa-design-time`, nonconnecting loopback port1). Up creates only the new draft table and two indexes, including an academy/actor/session composite FK to the existing Mini session. Migration tests assert the operation set and matching current EF snapshot. Generated Down drops only this new table; no downgrade was run. Existing unrelated uncommitted model/snapshot changes were retained, not replaced or attributed to this batch.

The migration was applied **only to run-owned disposable SQL**. The normal developer database was not migrated, seeded or reset. No tracked configuration change, provider enablement or new runtime route.

- Final isolated harness build: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql` PASS, zero warnings/errors.
- Final **211/211 focused tests PASS**, no failures/errors/skips. Seven new candidate gate/schema/store-boundary test cases, the existing model/snapshot consistency test and 203 retained E5-R4 focused cases. Receipt `QA/EVIDENCE/penta-onboarding-drafts-20261017/onboarding-drafts-final.trx`; filter `FullyQualifiedName~Penta|FullyQualifiedName~StudentOnboardingServiceTests|FullyQualifiedName~AuditOutcomeTests|FullyQualifiedName~PayrollHistoryTests.Model_matches_migration_snapshot`.
- Initial build at a new isolated path lacked restored assets; no restore bypass or runtime mutation. Used the existing restored isolated onboarding path. An initial test compile needed the Hosting import; one analyzer warning was corrected using the predicate Assert.Single overload. Earlier 209-test receipt remains local and is not substituted for the final 211-test receipt.
- Initial SQL development pass `30d4d5f583ee4cd4afdbf813d452dba2`: 28 retained + 27 initial draft cases passed, exit0 /49 seconds, owned cleanup confirmed. Final code adds matching-store checking and five additional SQL cases; use the final run below as current evidence.
- Final existing runner: `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`, run **`d5725769ad6642ea9c64d5507328b635`**, exit **0**, **50.7 seconds**.
- **60 named SQL cases PASS**: original21 HTTP/SQL/Identity onboarding assertions and count unchanged, seven retained direct shared-service cases, plus32 additive direct draft/review cases. These are not60 HTTP endpoints or linked model tests. Runner code/assertions were not weakened; the existing module invokes the additive suite after the original matrices.
- Runtime inventory remains333 routes /322 controller method-routes /10 framework Identity method-routes, SHA256 `3CA8D1FABA64A043749774D75B9EE01ABC90DA1428443AE2C2C7517BE1477728`. Health, real synthetic Identity login/bearer and two-tenant controls passed. Disposable store applied101 application /7 Identity migrations.
- Final runner confirmed exact owned database/login teardown, mismatched-marker cleanup refusal and ownership-checked stop/removal of only `academydesk-qa-d5725769ad6642ea9c64d5507328b635`. The initial run's exact container was likewise removed. No other container pruning/removal, no QA evidence deletion.

The32 new direct SQL scenarios are partial creation, incomplete confirmation denial, identical save replay, full revision2, exact confirmation, confirmation replay, edit invalidation, old confirmation denial, wrong hash, stale edit, teacher denial, anonymous denial, foreign tenant denial, other-admin isolation, wrong-conversation isolation, forged-role denial, credential-field denial, unknown-tool denial, malformed typed input, minor partial draft, missing-guardian denial, fresh-scope reload, SQL metadata tamper, competing-edit CAS, required confirmation-audit rollback, concurrent confirmation idempotency, draft-audit rollback, inactive actor denial, changed security-stamp denial, new draft under fresh authority, stale-conversation denial and expiry denial. Competing edits return exactly201/409; two confirmations return200/200 with one transition/audit for that revision. Final student/guardian/link/account snapshot equals its initial snapshot: **zero domain/account effects**.

## Provenance

| Source | SHA256 |
| --- | --- |
| apps/api/Intelligence/Penta/PentaOnboardingDraftService.cs | 1EA375735110D270D35E5F46499B85622F966A84B9A1A7B3A8AE64C96BCB0393 |
| apps/api/Domain/Entities/PentaOnboardingDraft.cs | 3FCE2CB603F0F7E9B7C8680F014AED747DD10393E313A24BD3CE9C7744FB778D |
| tests/AcademyDesk.Api.Tests/PentaOnboardingDraftTests.cs | BC468CB6AC45F166D0D294A659E0D006B34A30C8D814A669123A278AD59580C0 |
| QA/tools/SqlHarness/PentaOnboardingDraftRegression.cs | 713065C40958CD51E0CDFF2EF6AE8AC4DE59A068FE85AA79A523BFD912956642 |
| apps/api/Migrations/AcademyDeskDb/20261017054835_AddPentaOnboardingDrafts.cs | D98164472829722C19EFEFAF65552723B53227AB1644F6AF38E21035DE11F1F5 |

Normal API/test API copies match `29DAB6B0ACAB443F3BDC3339E8A4D72E18B7E664B2F9906D176F7E44EE75714D`; isolated API/harness API copies match `AFE26D757703C92F91D60344179F71D976AF8D5F08704A89ED637C88D61E64D5`. These are two distinct build-location pairs, not four identical binaries.

## Current local viewing and remaining work

Existing API restarted session21677 on localhost5092, health200; existing frontend session80830 on localhost3000, login-page200. Same process-only Testing/local-connection safeguards as E5-R4; startup migrations/bootstrap/media/PENTA/Mini/new draft gate disabled. The new table is therefore not needed by any active developer route. Admin login still unverified; no account reset or reseeding. Existing runtime decimal/TLS warnings are not cleared by this batch.

- [x] Previous readiness, canonical payload/receipt binding and shared existing domain-service checkpoint retained.
- [x] Durable encrypted candidate draft/review store, current authority/context/revision binding, real SQL CAS/replay/audit rollback and offline schema consistency.
- [ ] Next High route: complete onboarding field/catalog/reference/conditional validation and duplicate resolution, then one-time confirmation consumption/execution/status/result binding to the existing shared service and Identity/audit transaction. Do not treat this candidate review record as sufficient authority.
- [ ] Paired operational SDK/Core adapter, accepted private interpreter, authenticated controlled frontend and real linked SQL/HTTP/browser journey. No model change/benchmark run was required here; CORE-01 remains blocked.
- [ ] Further AI-01–AI-14 domain families, retention/deletion/key/backup/restart/multi-instance qualification, all role/resource catalogs, full enterprise/device/provider/privacy/operations/exact-release gates. Expiry is not proof of physical deletion or a production privacy policy.

No PENTA source, primary-main, staging/commit/push/merge/Azure change. Evidence stays local/untracked, builds ignored, existing work preserved. No issue/release closure or whole-app completion percentage. **Production NOT READY.**
