# E5-R7 — protected onboarding HTTP adapter

Completed 18 October 2026; implementation/focused tests started 17 October. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`, with existing local work preserved. Continues E5-R4–R6; no replacement backend or new testing program.

## Implemented boundary

- Scoped `PentaOnboardingDraftService` and five actions under `/api/academies/{academyId}/penta/onboarding/conversations/{conversationId}/drafts/{draftId}`: PUT save, GET status, POST review, POST authorize-creation, POST execute. Require **Testing**, `Penta:OperationalDraftContractsEnabled` and separate `Penta:OperationalOnboardingHttpContractsEnabled`. Creation authorization/execution also require `Penta:OperationalOnboardingExecutionContractsEnabled`. All off in the normal local runtime; production cannot activate them through flags.
- Bearer-only authorization rejects cookie-only requests. Existing current-user/security-stamp middleware and AcademyAccessFilter remain, followed by independent current SQL actor/role/tenant/session/payload/approval checks. Route academy is untrusted and checked against server identity; client/model roles grant nothing. Platform ownership does not authorize academy conversational onboarding.
- Exact top-level JSON keys, duplicate/unknown-key rejection, bounded revisions/context, lowercase SHA256 binding and typed fields. Server supplies fixed tool identity/revision. Existing strict payload parser rejects credentials, arbitrary tool selection and caller approval flags. Protocol field names are case-sensitive, e.g. `StudentFirstName`, not Web-JSON camelCase DTO properties. Save replaces the full field set, not PATCH.
- Review remains separate from explicit creation intent/current revision/hash. Only the existing no-portal-account student/optional-guardian operation is supported. Authenticated API tests submit that intent; **they do not prove a real human/browser confirmation or qualified conversation**. Future controlled UI must provide actual explicit user confirmation.
- E5-R6 shared transaction owns student/guardian/grants, encrypted receipt, Succeeded and mandatory audit. AcademyAccessFilter excludes this controller from its generic second audit/transaction, like the other PENTA controllers; current role/tenant checks remain.
- Available responses are no-store/no-cache. Store/cryptographic/invalid-operation failures return safe503 reconciliation guidance without exception text, SQL or payload leakage. No automatic retry or false success. Reconcile an uncertain outcome with the same draft; Manual fallback is not permission to blindly duplicate creation.
- Request-size metadata:16KiB save /1KiB bound POSTs, plus12,000-character structural cap. Tests cover metadata/structure, **not real Kestrel/ingress byte-limit enforcement**.
- `CanExecute` remains false: no enabled chat/model catalog binding or customer write UI. Teacher/Student tools and active read-only protocol0.1 unchanged. No schema/migration/developer SQL/frontend source change.

## Actual verification

- **255/255 focused .NET tests PASS**, zero failures/errors/skips:238 retained+17 new controller gate/body/auth-metadata/cache/SQL-guard cases. Receipt `QA/EVIDENCE/penta-onboarding-http-20261017/onboarding-http.trx`, counters rechecked18October. Filter: `FullyQualifiedName~Penta|FullyQualifiedName~StudentOnboardingServiceTests|FullyQualifiedName~AuditOutcomeTests|FullyQualifiedName~PayrollHistoryTests.Model_matches_migration_snapshot`. Includes current EF model/snapshot consistency.
- `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql`: PASS, zero warnings/errors.
- `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`: final run **`c87a82d56461452c8dc2efb028aa84c4`**, exit0, **67.8seconds**,101app/7Identity migrations. Inventory338 routes /327 controller method-routes /10 framework Identity method-routes, SHA256 `AA65212707E3980EBA7E73E8A685A0FD96F3ADDC3891CDFD6C8DB30287DCD71E`. Both contexts match exact run-owned SQL/runtime login; synthetic real Identity/bearer and two-tenant controls PASS.
- **118 named SQL cases PASS**:21 original Manual HTTP +7 shared-service +32 draft +26 execution +**32 new authenticated HTTP/real SQL cases**. Original assertions/counts/runner not weakened. Cases are not118 endpoints or model/browser journeys.
- New32: anonymous/invalid bearer/cookie-only; Teacher/platform/foreign tenant; disabled adapter; five malformed/credential/authority/tool/approval bodies; save/no-store; other-admin draft privacy; owned status; unconfirmed execution; review; review-only execution; generic yes; model approval; exact creation intent; stale revision; wrong hash; concurrent HTTP201/200 identical receipt/one student/one audit/no generic audit; replay without changes; Succeeded status; mandatory audit safe503/full rollback; explicit retry after independently proven rollback; changed context; expired authorization; inactive actor; revoked current SQL role despite old bearer claims. Snapshots include domain/minimized Identity/drafts/audits without printing private values.
- Runner rejected mismatched cleanup marker, removed exact owned DB/login, ownership-checked/stopped/removed only `academydesk-qa-c87a82d56461452c8dc2efb028aa84c4`. Subsequent exact-name inventory confirms absence. Other containers/evidence preserved; no prune.
- Earlier run `208e8e88a9654406b25159e919c46b5f` failed expected403/actual400 because synthetic HTTP serialization camel-cased protocol keys. Fixture now uses an exact-key dictionary; no application denial relaxed. Its stopped failed-run container remains retained for inspection. Compile-time attribute metadata/Identity import errors corrected before final build. Failed attempts are not counted as acceptance.
- `git diff --check` PASS (existing line-ending warnings only). No full enterprise/unrelated-module/device/real-model/provider rerun or closure.

## Source provenance

| Current file | SHA256 |
| --- | --- |
| apps/api/Controllers/PentaOnboardingController.cs | 28ACAF85244AD2F4AB93BF10F6D002E69FF75001F57CA8D491F585A5B26BE487 |
| apps/api/Program.cs | 01F1C92E4E03B528DE4C19AA856F310E8BB0BB2B1BE63EB2D869AB3EF6E0542F |
| apps/api/Security/AcademyAccessFilter.cs | 793D6892E5465E15804EAB8CCB4719F1A0C043948E0F903AFDB28C933194AF1D |
| apps/api/Intelligence/Penta/PentaOnboardingDraftService.cs | 60334D24B0FF3D554722214E83F82CC6E544417969F4D545AE8B4E108FD77F77 |
| apps/api/Intelligence/Penta/PentaOnboardingExecution.cs | 877629964EADC723F63029FA9DA01A23E6D645D38B32159E961EA1B7CC8830F7 |
| tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs | 23200368FFC5E6AFDA0696909B0B60F5EF542CA2829CE5D3F7DC9F75AF53D821 |
| QA/tools/SqlHarness/PentaOnboardingHttpRegression.cs | CC7694E5F3587C9D02B4B14C580D3A129FFAEEA8D86FD675840604692192C00F |
| QA/tools/SqlHarness/OnboardingAtomicRegression.cs | C11B3990180787DA22F35810B641970FCFB66E0648497CC0ECCA3F135007B827 |

Normal API/test API copies match `B40A85B2F620029768C8BD70F8D7FD06957FA78F65F47AD1D877A97B45C62D16`; isolated API/harness API copies match `29C5BAAE606F2B591930E800D9E6CF95A0C51824CEBC49BB879C1FE0397946DA`. Two build-location pairs, not four identical binaries.

## Runtime and next

Restored existing API session21704/localhost5092 and frontend29983/localhost3000; health/login-page200. Process-only Testing skips destructive development seeding/bootstrap/startup migrations; media/PENTA/Mini/draft/execution/HTTP switches disabled. No developer SQL/password/reset/reseed changes. **Admin sign-in remains unverified**; login-page200 is not authentication acceptance.

- [x] Prior shared-domain/encrypted draft/explicit authorization/atomic receipt work retained.
- [x] Protected test-only HTTP/current-authority/concurrency/audit rollback proof.
- [ ] Next **Sol High**, same project: actual candidate.5 SDK/Core host catalog/current-context/preparation/validation/confirmation/receipt port and controlled authenticated chat UI. This API/session fixture is not complete integration. Pair the actual versioned PENTA return without engine rewrite or automatic activation.
- [ ] Qualified private inference/CORE-01/selected tiers; remaining AI-01–AI-14 domain/role bindings; unknown-outcome/key/retention/privacy recovery; remaining Manual visuals and enterprise/device/provider/operations/exact-release gates. Actual socket/ingress bounds and authenticated browser confirmation open. Do not substitute identical synthetic tests for missing evidence.

Primary main/PENTA source untouched, HEADs unchanged; no stage/commit/push/merge/Azure/model activation/paid resource/download/training. QA evidence local/untracked, `.build-check` ignored. **Production NOT READY.**
