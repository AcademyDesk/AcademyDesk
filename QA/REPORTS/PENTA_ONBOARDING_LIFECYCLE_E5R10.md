# E5-R10 — protected onboarding pause, resume and cancellation

18 October 2026. Codex, recommended engineering route **Sol High**, local/disposable SQL only. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved uncommitted work. Continues E5-R4–R9, not a new backend or test plan.

Scope: existing onboarding draft/controller/context/preparation boundaries, additive focused/SQL/Core compatibility tests and progress documents. No Manual workflow redesign, domain-service rewrite, PENTA source change, migration, developer database update, credential change, model/tool activation, staging, commit, push or Azure action.

## Implemented

- Bearer-only `POST .../drafts/{draftId}/core/requests/{requestId}/transition`, 2KiB request limit, behind existing Testing + draft + HTTP + execution-candidate gates. No model-accessible execution registration. Production and Development remain unavailable regardless of flags.
- Strict candidate.5 WorkflowDecision subset: route-matched GUID nonce, exact keys/no duplicate keys, exact catalog/base revision/current six-field scope, empty candidates/questions, `invalidates_review=true`, and only `request_pause`, `request_resume`, `request_cancel`. No field edits, role/tenant authority, approval/execute/status semantics or arbitrary tools are accepted here.
- Existing academy-first Serializable/linked-Identity transaction independently checks current actor/role/tenant/module/private authority hash/session and protected draft. Every accepted transition increments the revision, retains canonical fields, drops preparation/creation authorization and commits protected state plus mandatory audit atomically. No new workflow table or schema.
- Paused drafts cannot save, review, authorize, execute, prepare or validate through the older endpoints. Only explicit resume returns to collecting, with no operation identity or execution permission. Cancelled is terminal for new transitions/edits; it cancels a draft, never deletes a student. Committed Succeeded drafts cannot be cancelled. Execute-versus-cancel uses the existing shared academy lock and permits only one committed outcome.
- The existing encrypted request ledger now distinguishes preparation and lifecycle kinds. A nonce cannot be repurposed across these ports or reused after a later revision. Current identical replay revalidates authority/session/expiry and returns a snapshot without another write/audit/TTL extension, including after a fresh host/DataProtection-provider instance. Original transition time is retained in `as_of`; the context/catalog is still revalidated, not a permanently cached global turn receipt. The unchanged-catalog replay is proven identical by SQL.
- Extracted the existing bounded HostSnapshot projection for reuse **inside** the transition transaction, before audit/commit. Paused/cancelled project their real states with no operation reference. Current branch/privacy/bounds checks still apply. A medical-note draft requires the secure Manual step; context failure rolls the transition back rather than stripping private text or claiming success.
- Older encrypted preparation entries default to kind `prepare`; new statuses fail closed on older binaries that do not understand them. Preserve the ledger for reconciliation during any future rollback; do not delete or reinterpret records. Per-draft revision/request bounds remain; this is not a complete durable conversation-turn or current-draft-selection gateway.

## Verified

- **298/298 focused .NET tests PASS**, zero failed/error/skipped:280 retained +18 new strict lifecycle parsing cases. Existing controller gate/request-limit assertions cover the new action. Same focused PENTA/shared-onboarding/audit/model-snapshot filter retained. Receipt: `QA/EVIDENCE/penta-onboarding-lifecycle-20261018/onboarding-lifecycle.trx`.
- Isolated build PASS, zero warnings/errors: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql`.
- Existing runner unchanged: `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`. Final run **`aaa1602608cd4d77b8c8e31d92fdb8d6`**, exit0, **121.5s**,101 app/7 Identity migrations, exact owned target/runtime login verified. Inventory342 routes/331 controller method-routes/10 framework Identity method-routes; SHA256 `D4482D143A6A6E620FA47B2FA98227A7AD9C6F04FAC725838F81A65DEAE7C631`.
- **218 named SQL cases PASS**:170 retained +48 new HTTP/SQL lifecycle cases. New matrix covers anonymous/Teacher/other-admin/foreign/default-disabled denials, malformed authority/catalog/action/fields/base requests, resume-state restriction, parallel identical pause/one audit/revision, fresh-host replay, all paused/cancelled legacy bypasses, nonce kind/revision/port reuse, actual prepared+authorized pause invalidation, fields retained on resume/cancel, terminal cancellation, competing distinct pause/cancel200/409, mandatory-audit503/full rollback and independently verified retry, committed-student denial, execute/cancel serialization, secure-context rollback, expiry, stale conversation and changed current authority. Race assertions reject429. No old assertions or counts weakened; no stress/soak claim.
- Successful runner rejected a mismatched cleanup marker, removed its exact database/login, then ownership-checked/stopped/removed only `academydesk-qa-aaa1602608cd4d77b8c8e31d92fdb8d6`. Other/PENTA containers and evidence preserved.
- Initial run `3ff205199fc241408cb6d90677efe8e9` failed at the new secure-note fixture: default HTTP JSON naming policy camel-cased its anonymous-object field, correctly rejected by the existing strict host parser. Fixture now uses the same exact-key dictionary as other canonical field tests; success/privacy assertions unchanged. Initial run is **not accepted**, exit nonzero/121.6s. Its exact run-owned container was ownership/label/loopback checked, stopped and retained for inspection; no broad cleanup.

## Actual pinned Core compatibility

All scripts read actual candidate.5 manifest-pinned PENTA source, disable bytecode output and use only synthetic responses recorded by the disposable HTTP/SQL run. No inference/native model launch, network SDK transport, training or PENTA source modification.

1. `python QA/tools/penta-onboarding-lifecycle-contract.py --penta-root "D:\PENTA AI Models" --receipt QA/EVIDENCE/penta-onboarding-lifecycle-aaa1602608cd4d77b8c8e31d92fdb8d6/core-lifecycle.json`: six source pins PASS; actual `DialogueSDK.transition` accepts **five** recorded pause/resume/cancel journeys, including an approved starting state. Fields retained, exactly one newer revision, no operation reference; stale requests denied before transition and mismatched host results denied. Receipt SHA256 `eb893d392ebfefd1b8f1c06e8a9166dcbc54a50ce16a9292a4068d56c1715947`.
2. Existing preparation compatibility rerun PASS: one recorded SDK.prepare journey/six DialogueSDK.validation reports, canonical/hash/current binding and unapproved execution denial. Receipt SHA256 `1b680dc9cce09b2c4ba4d755e1424840bd9992ffe8920e34f62578a03a164aaa`.
3. Existing context/reducer compatibility rerun PASS: six snapshots, required-field clarification, exact typed preparation, foreign tool/reference denials. Snapshot SHA256 `4083cf7752e1a7662a61f4e7ed369f5b031029b73d814adbb7db07685d1317a4`.

These prove recorded host-port compatibility, **not** a live Core gateway, qualified interpreter, human/browser approval or available customer AI tier. Snapshot availability describes the gated host context, not model readiness.

## Source / binary provenance

| File | SHA256 |
| --- | --- |
| apps/api/Intelligence/Penta/PentaOnboardingLifecycle.cs | D869814964EFD6AD7BA1DE0FF0E022050F5B82272C32C11643389421F34C5A8B |
| apps/api/Intelligence/Penta/PentaOnboardingLifecycleInput.cs | D7392AC509414E8412893784FD157499DCA04262C1CD86C29A971CF73B3EE9D0 |
| apps/api/Intelligence/Penta/PentaOnboardingDraftService.cs | 4E2BCC315110CA58FFB4947DD8DF8CEB1857F41C4CD832F0C5089F105032C61B |
| apps/api/Intelligence/Penta/PentaOnboardingPreparation.cs | B0DCAFD7FD13A2A662C5DA21DDB3E9CEDD936166898155A75D057F7BE8FABD58 |
| apps/api/Intelligence/Penta/PentaOnboardingCoreContext.cs | 202EA474A527346FE731902141C52BCF8AB0EA32AAA872A2E373B2FA8CBC392D |
| apps/api/Controllers/PentaOnboardingController.cs | A0C058984D0BF21E6CA70E808FA9328DD5CA9131BABCF45A1D76C5CEE06ECFDA |
| tests/AcademyDesk.Api.Tests/PentaOnboardingLifecycleTests.cs | 3C7EE7DC8B2BF1B3C95DA082073AD380DC77B025B630B003A82C12EAAB94BC5D |
| tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs | 94FA0F4F526F1D651CAE856773A8F72600A397D4F41096B5B726152684348F84 |
| QA/tools/SqlHarness/PentaOnboardingLifecycleRegression.cs | 2951C9AB3C4AE235AA784532014D45274A60012291998C6A909A0D186F4219FF |
| QA/tools/SqlHarness/OnboardingAtomicRegression.cs | 4FB0D7838276ADCD79E4EE376FC2A1FEA2CE6F54DC7BDB8C614104DDE67B6194 |
| QA/tools/penta-onboarding-lifecycle-contract.py | 59CCD5E40C0BD67A7D7FD7E266AAC91ECB508BE6B5B21FCBEB34B5CA1FA85240 |

Normal API/test-copy binaries match `A80757E88D9E81E7BF088086894F7510C26B49CFDE6FAAA80D36083A8318A8E1`; isolated API/harness-copy match `016EC8DF8579B79FD877194C788F7544D9AD17DAFA71823D09E18E8F706A3345`. Two build-location pairs, not four identical binaries.

## Checklist / next route

- [x] Prior shared onboarding/domain/Identity transaction, encrypted draft, review/execution receipt, context and preparation/validation retained and affected SQL suites rerun.
- [x] Strict lifecycle adapter, durable per-draft nonce history, approval invalidation and terminal/legacy bypass guards.
- [x] Real HTTP/SQL races/replay/rollback plus actual recorded Core SDK compatibility.
- [ ] Next **Sol High**, same project: opaque host-issued human challenge + exact confirmation, operation/execute/status/receipt bridge, preserving current domain transaction and preventing legacy-route bypass for Core-approved operations.
- [ ] Remaining durable turn/current-draft selection and partial-save adapters; controlled authenticated integrated chat UI with truthful review/result/failure states. Existing exact-intent API authorization remains a test-only host candidate, not the future human challenge/UI. Core lifecycle does not supply secure medical-note handling.
- [ ] Qualified private interpreter/tier evaluations, remaining domain/role families, full Manual visual parity, provider/device/privacy/key/retention/ingress/unknown-outcome/soak/enterprise/exact-release gates.90% remains a goal, not measured delivery; this batch does not close BUG-DATA-0001 or the enterprise program.

API restarted session90638 at localhost5092 with process-only Testing environment, startup migration/bootstrap/seeding disabled and PENTA/Mini/operational/media switches off; health200. Retained frontend localhost3000/login200. Actual developer Admin sign-in remains unverified; no reset/reseed/developer SQL update. Evidence local/untracked, `.build-check` ignored, zero staged files and HEADs unchanged. No main/PENTA source/commit/push/merge/Azure/training/paid-resource changes. **Production NOT READY.**
