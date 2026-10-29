# E5-R15 — protected host gateway and no-change outcomes

18 October2026. Recommended Sol High. `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved unrelated work. Continues E5-R4–R14 and the existing enterprise program.

## Implemented and deliberately not enabled

- Bearer-only/no-store Testing-only `POST .../conversations/{conversationId}/turns` (16KiB) accepts the existing strict prompt/claim shape, not a client plan, tenant/role/provider or model choice. Existing draft/execution-candidate/HTTP flags plus new `Penta:OperationalOnboardingGatewayContractsEnabled` required. No schema/migration, production interpreter registration, model loading/network connection, frontend or developer DB change.
- Optional `IPentaOnboardingInterpreter` seam has **no production implementation/registration**. Only guarded `QaApiFactory` injects a scripted interpreter after run-owned SQL preflight. Absent connection returns503 before admission; a flag does not qualify or connect a model.
- Existing atomic claim now returns its exact checked snapshot privately to the host service; the pending HTTP response still withholds it, and the extra outcome property is `JsonIgnore`. No raw prompt persistence. Current authenticated SQL permissions/tenant and current draft still independently govern admission.
- Private interpretation runs **outside** SQL/Identity transactions. Completion compares current scope/catalogue/tool definitions/draft/evidence/availability to the exact claim-time snapshot, ignoring clock-only `as_of` changes, inside the existing authoritative SQL completion transaction. Current Identity/role/academy/module/context/revision/hash checks remain. SQL proves a separate branch update can finish during interpretation and its changed catalogue blocks the result without clearing pending or altering draft.
- Two process-wide interpretation slots, immediate busy503 before admission and30s linked deadline. No automatic replan of a used request. Underlying timed-out/caller-abandoned task retains capacity until actual exit; its fault is observed and late result discarded. This is local process protection, not distributed ingress/quota/soak acceptance.
- Added strict candidate.5 nonediting `unsupported` and `clarify` decisions: exact current scope/request/base/catalogue, no candidates, no review invalidation, bounded known unique questions. Actual missing-required keys checked again from host validation for clarification. No raw model prose/errors, unknown tools/SQL/credentials or approval labels accepted.
- Known transport/IO/provider cancellation produces host-owned `unavailable`; malformed JSON/decision produces `invalid_response`. These statuses **cannot be supplied by the public completion/model decision parser**. No-change completion keeps draft/revision/payload/any existing approval intact while committing encrypted turn receipt/required audit/pending release once. It never reports a student/domain success, grants permission or retries a write. All responses have `canExecute=false`; HTTP201 means recorded turn outcome, not completed business work.
- Existing receipts add an optional protected `NoChange` discriminator. Current replay/result verification checks original scope/authority/version/draft/hash/audit and exact snapshot; no-change needs equal base/result revision and no operations, while draft updates retain original+1/history rules. Old receipts default false. Older binaries unaware of new protected fields must fail closed; no rollback/destructive repair was run.
- Out-of-band catalogue/permission changes, cancellation by the actual caller, unknown exceptions, audit failures and store uncertainty retain the durable pending claim rather than invent success or automatically recycle it. Known interpretation-only outage can finish no-change because the interpreter has no host execution capability; no external mutation is assumed/retried.

**Not delivered:** actual PENTA server-to-server authentication/current-context transport, qualified interpreter/native inference, real provider deadline/caller-abort/ignored-cancellation soak, distributed capacity, status/domain execution chat composition or authenticated human/browser approval. The current PENTA return remains CORE-01 blocked and default operational app inactive. [Exact paired return packet](../HANDOFFS/PENTA_ONBOARDING_GATEWAY_BINDING_E5R15.md) records what is needed; no PENTA source edit or automatic chat message occurred.

## Accepted validation

**380 focused tests PASS**, zero failed/error/skipped:362 retained+18 neutral-parser/privacy cases; retained HTTP gates/limits extended. Receipt `QA/EVIDENCE/penta-onboarding-gateway-20261018/onboarding-gateway.trx`. No application failure/failed SQL run this batch.

Normal API/test API-copy DLLs match `CDC7A12C4FD9EF284ECA9F21F17382FDFF2BD12DFB30C6198A281134DA6B4414`; isolated API/harness API-copy match `F9994950F2485243A242F660FC97FBFD7830F2D5DBF6B0E2C7A1F2FF3DAAEBBD`. Two distinct build-location pairs, not four identical binaries.

Isolated build PASS0 warnings/errors,12.95s: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql`. Existing runner unchanged: `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`.

Final **`d3072f0e776546a18a0bf77d3f2659eb`**, exit0/**88.6s**,102 app/7 Identity migrations. Resolved contexts/runtime login match exact owned store. Inventory354 routes/343 controller method-routes/10 framework Identity method-routes; SHA256 `4B7AF8BB799DF3F58EB9D8CEEFE07B94A2B5877321CA38A794573BBAFCC7B441`.

**407 named SQL cases PASS =384 retained+23 new HTTP/SQL cases**. New23-count assertion: anonymous/Teacher/foreign/other actor/default/separate gate/no interpreter/caller authority; unsupported no-change; no repeated interpretation; restart exact neutral receipt; nonediting missing-field clarification; prompt-to-scripted-plan-to-real-atomic-partial; malformed prose/approval withheld; transport exception and simulated provider cancellation unavailable; no held SQL lock/current catalogue drift; final completion audit failure retains pending; no automatic retry after failure; two-slot capacity rejects third **before admission**; both admitted turns finish once; private planning authority change blocked; domain/account identity snapshots unchanged by gateway. The authority test deliberately changes a synthetic Identity concurrency stamp externally; it is not an AI account mutation. No429 allowed as a race/capacity substitute. Existing assertions/runner retained, not weakened.

Actual30s wall timeout, caller cancellation and an interpreter deliberately ignoring cancellation are code guarded but **not exercised by these23 cases**. Provider cancellation case is an injected `OperationCanceledException`, not evidence of real network/native cancellation. These remain paired transport/runtime tests; no silent qualification claim.

Runner refused a mismatched cleanup marker, verified exact database/login removal, then individually ownership-checked/stopped/removed only `academydesk-qa-d3072f0e776546a18a0bf77d3f2659eb`. Final absence checked separately. No pruning, other/PENTA containers or QA evidence deletion.

## Retained pinned Core recorded-port checks

Six checks PASS using actual candidate.5 pins and unchanged existing scripts on this run's retained receipts. These do **not** exercise the new injected interpreter as a live Python/HTTP gateway or new neutral SDK transport.

| Check | Receipt SHA256 |
| --- | --- |
|Claimed partial combined7 journeys|`c7f8ae30298a934b8c5708c99117c186b09888ed86bce5365997f6e16ec82750`|
|Standalone partial5|`d0ce91f9edc3660cc27e91bbb41e50489e9cd8300d90a861cd252437758866d4`|
|Approval/execute/reconcile1|`844a1d05ea07c05986b0a9e509e6c38a22e86d23de1f56c5ab338bf45180532e`|
|Lifecycle5|`a7a590f5233ad877a31c6cbab714eac74a8e751ba7f0e13495b7b2553a4cfdab`|
|Preparation1/validation6|`35631a839411c55d997394da99ad436b030ae45c0d4658e164b9407467dee853`|
|Context6|`9ff3d6f1cac3f541d38354a3760d512724691ca3f8598a4d3060e22750f8b899`|

Use `QA/tools/penta-onboarding-*-contract.py --penta-root "D:\PENTA AI Models"` with matching run receipt/snapshots. No model/native launch, network inference, training or PENTA edits.

## Source receipts

| File | SHA256 |
| --- | --- |
|apps/api/Intelligence/Penta/PentaOnboardingGateway.cs|2980C97F965529E3946223937D7F3FB932AD1FE7B0C96A20153DE39005429D98|
|apps/api/Intelligence/Penta/PentaOnboardingNeutralInput.cs|9BFA4EAF1E0DC7367A1A4A041FE840F5EC5BD0469E7B679C671DFF837AEC74B8|
|apps/api/Intelligence/Penta/PentaOnboardingCompletionInput.cs|984D977BB2662F1880622490942179D8B785D292BC723BF6178C5FA3E7C8B90D|
|apps/api/Intelligence/Penta/PentaOnboardingTurn.cs|AA732F04ACCB0FCF82CCDE4FD09A3129F58264C59A0DE1F4BFFDFF05CF5ACA43|
|apps/api/Intelligence/Penta/PentaOnboardingConversation.cs|6ECFA361E33BE8BD9B44C1E53430AEB21602AD1A499CEBF2A22EF63900AB5146|
|apps/api/Intelligence/Penta/PentaOnboardingDraftService.cs|4E321DDFC1D52FE7E9AC15F759F92BBAC2030D4CCD6F82415AC82CEF1D7E4356|
|apps/api/Intelligence/Penta/PentaOnboardingPreparation.cs|F6C4C6875715CF8772394DB33F704BDB5EC811ABCE03B6F8E0974054D803E695|
|apps/api/Controllers/PentaOnboardingController.cs|19DDF35970399C740A6076C31B5D68CC42303BE108EF7B62F3CF07FFB760F229|
|tests/AcademyDesk.Api.Tests/Infrastructure/QaApiFactory.cs|604F11A39866389D4806160F4F66A5A07294CCC97BEFAFCCB5617732C42DA89F|
|tests/AcademyDesk.Api.Tests/PentaOnboardingNeutralTests.cs|5E5CB39001229EAAA8E85EF9340EEC90CD30DEABB1D35AFD91ACD95CEA1FA1DA|
|tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs|53CD95A5F3B61EAB3AB99E3713C85DB7635016C91E2ADEAC54D92B348061133E|
|QA/tools/SqlHarness/PentaOnboardingGatewayRegression.cs|9683BC47D2146E801E232C3E3472EC518D131C18631BA9D21116CF14AF50D825|
|QA/tools/SqlHarness/OnboardingAtomicRegression.cs|620BEE0FCFDC72C07A4F6A8B8B153130B980B4800DF2C9F4DF9A080A2E1B883F|

## Checklist / next

- [x] Previous shared domain/draft/approval/execution/result/admission/turn checkpoints preserved and affected suites rerun.
- [x] Test-only host gateway composition, private claimed snapshot, neutral/outage outcomes, bounded capacity/current catalogue+authority recheck and real SQL proof with injected interpretation.
- [ ] **Next Sol High:** [paired private context/transport return](../HANDOFFS/PENTA_ONBOARDING_GATEWAY_BINDING_E5R15.md), actual cancellation/deadline/lifecycle qualification and a qualified private interpreter. Default PENTA runtime has no authenticated current callback/accepted planner; do not invent a connection, rerun unchanged failed models or activate a tier. Local authenticated UI adapters can proceed with honest unavailable states, not claim live AI.
- [ ] Authenticated integrated chat/approval/domain-execution journey, actual browser/device tests; all remaining AI-01–AI-14 domain/role families, full Manual visual parity, provider/privacy/keys/retention/operations/security/enterprise/pre-Azure gates remain OPEN. BUG-DATA-0001 browser/device gate remains OPEN.

API restarted session62452 localhost5092 and existing frontend3000 health/login-page200. Process-only Testing/local SQL, migrations/bootstrap/media/PENTA/Mini/all operational flags OFF including gateway. Actual developer Admin sign-in still unverified; no password reset/reseed. No developer SQL/schema/frontend/main/PENTA source/stage/commit/push/merge/Azure/paid resources change. Evidence local/untracked; build ignored; unrelated work preserved. **Production NOT READY.**
