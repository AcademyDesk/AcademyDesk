# E5-R13 — protected current selection and durable turn admission

18 October 2026. Recommended route Sol High. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved local work. Continues E5-R4–R12, not a backend rebuild or new enterprise testing plan.

## Implemented boundary

- Three bearer-only/no-store ports on the existing Testing-only onboarding controller: `GET .../conversations/{conversationId}/current`, `POST .../selection` (2KiB), `POST .../claims` (16KiB). Existing draft/execution-candidate/HTTP switches remain required. These are host admission ports, not model tools or production activation.
- Reused Mini session/turn tables and academy-first Serializable/linked-Identity transaction. Offline migration `20261018050649_AddPentaOnboardingConversationState` adds only one nullable `nvarchar(4000)` ciphertext column. No parallel workflow table. Applied to fresh disposable SQL only, not developer/Azure SQL; no downgrade run or data-preserving deployment upgrade qualification claimed.
- Current SQL actor/role/active academy/Core module/private authority hash revalidated on every call. Exact bounded request keys, nonempty GUID nonce, current operational version, draft revision and canonical hash. No client/model roles, tenants, approvals, extra/duplicate keys or permissive model dispatch. Claim text is bounded to2000 characters and is never persisted or echoed.
- Purpose-bound encrypted cursor pins academy/actor/session, retained Mini context version, current authority, operational version, selected draft and optional pending request. Current projection reuses owned protected draft/privacy/reference/result checks; foreign branch and medical context fail before cursor commit. Authority/context changes invalidate rather than inherit permissions.
- CAS selection and encrypted purpose-separated request ledger commit with mandatory audit. Competing distinct selections return200/409. Current exact binding replay adds no state/audit/TTL extension; returns a fresh projection, not a promised byte-identical saved response. Old replay cannot restore an earlier pointer. Selection is not approval.
- Claim atomically advances operational version, saves one `OnboardingPending` turn/encrypted input digest, sets the shared pending marker and commits required audit. Competing prompts return202/409, never429 in this matrix. Response is pending/no planning snapshot/`canExecute=false`. **No inference, adapter dispatch, domain execution, scheduler or turn completion exists in this port.** Same-ID pending replay, altered text, new IDs, selection changes and restart cannot recycle an unknown request.
- Existing draft mutations require an idle session and cannot race a pending claim. Read-only Mini refuses explicitly operational-owned sessions before interpreting their ledger; operational state does not wrap or alter its encrypted MiniState/context version. A separate read-only session retains the old flow, verified with a synthetic unavailable planner, not a model.
- Required audit failure rolls back cursor/pending/turn together. Copied ciphertext and cross-port nonce reuse fail closed. No plaintext prompt/digest/authority fingerprint in audit. This is not generic secret detection, key lifecycle, retention/privacy acceptance or a production recovery policy.

## Accepted verification

**343 focused tests PASS**, zero failed/error/skipped:325 retained+18 new parser/migration cases; existing controller gate/limit checks extended. Retained filter includes PENTA, shared onboarding, audit outcomes and model snapshot. Receipt `QA/EVIDENCE/penta-onboarding-conversation-20261018/onboarding-conversation.trx`.

Normal API/test API-copy match `C759422F093391FD27E9A9CEDE6DDFF213CF42750C9F922B9B5CFB7D97109FE0`; isolated API/harness API-copy match `16085271A50A5B0F560980D62A12B2CE2AFDC2C8AF655EAD05DEE50A8646C668`. Two build-location pairs, not four identical binaries.

Isolated build PASS, zero warnings/errors: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql`. Existing unchanged runner: `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`.

Final accepted run **`a1422fda02844d36a4f01f8931a90814`**, exit0/**80.6s**, **102 app/7 Identity migrations**, both resolved contexts match exact run-owned target/runtime login. Inventory351 routes/340 controller method-routes/10 framework Identity method-routes; SHA256 `104ACBA834A06C006015E134484825AF0886A6AF04AD8D6CE08933EB7F6B3D3D`.

**338 named SQL cases PASS =295 retained+43 new HTTP/real SQL cases.** New matrix: anonymous/Teacher/foreign/other-actor/default-disabled; unselected current/no execution; revision/hash/version/approval/target denials; competing selections/replay/restart/switch/old nonce/cross-port nonce; competing prompts/encrypted digest/no raw prompt/read-state preservation; pending/changed/new-ID/restart/legacy-save/read-Mini bypasses; no admission inference; selection/claim audit rollback and independently verified explicit resubmission; copied/corrupt cursor/context change; medical/foreign reference rollback; draft/session expiry; separate read-only flow/current authority and no domain/account effects. Exact status assertions and new43-count assertion retained. No previous assertion or runner weakened.

Runner refused a mismatched cleanup marker, verified database/login removal, then ownership-checked/stopped/removed only `academydesk-qa-a1422fda02844d36a4f01f8931a90814`; final absence separately checked. No prune/PENTA container change.

### Failed/superseded diagnostics — not acceptance

- `016ec8177ca5461aa8c09b6a483356ef`, failed84.5s: new fixture supplied display `Executor`, not existing lowercase API capability `executor`. Fixture corrected, application and expected409 unchanged.
- `ebcbaf0be793456581019ea0c6f755b6`, failed83.4s: Web JSON camel-cased contract-owned PascalCase field names in the new fixture; medical draft never reached selection. Fixture now explicitly preserves keys/typed values and uses existing branch kind `reference`, not `ref`; parser not relaxed.
- `c8be602ecb21426c98352617f161cd1f`, deliberately interrupted24.3s after detecting incomplete fixture correction. Only exact run-ID/command-owned harness process stopped, not a passing/application-regression result.

All three diagnostic containers individually name/label/loopback ownership-checked, stopped and **retained**, with database/evidence. Previous failed containers/evidence not deleted. Final result uses only final source-matched run above.

## Retained actual Core compatibility

All five existing scripts PASS against final run receipts and actual manifest-pinned candidate.5 PENTA source. Recorded-port checks only, **not** live SDK HTTP/inference/browser/human approval or completed new gateway qualification.

| Port | Evidence and SHA256 |
| --- | --- |
|Partial/reducer|5 journeys; `95d6fc6fa00f88a0129b1bdc13d1e530a7935c96fd50c6f34a509a68610cd6d3`|
|Approval/execute/reconcile|1 journey; `b4d8e267f843ac56e8a44843710bf2a4b9330fa58fd1490f51cb0a61796b6965`|
|Lifecycle|5 journeys; `4d7654e13dfaa6fb3ee5c821e493b6d10dde5e1bdd815effe6bf2693eb3b0752`|
|Preparation/validation|1 journey/6 reports; `db0a4a5915e6a827aebf34c24b71343da444ff63105988b66612996b27b434ac`|
|Context/schema/reducer|6 snapshots; `e12a582479dd1c6b62eb8183f301516d5953721dc0bee9d5018345c6b5edab0b`|

Use existing `QA/tools/penta-onboarding-*-contract.py`, `--penta-root "D:\PENTA AI Models"` and matching `--receipt` (context `--snapshots`) under `QA/EVIDENCE/penta-onboarding-{partial,approval,lifecycle,preparation,core-context}-a1422fda02844d36a4f01f8931a90814/`. No PENTA edits/native/model launch/training/network inference.

## Source provenance

| File | SHA256 |
| --- | --- |
|apps/api/Intelligence/Penta/PentaOnboardingConversation.cs|9095BC701CC4C0F42E671BECD826B570693E4730D4F6EE86B6D76BA000FCB1FB|
|apps/api/Intelligence/Penta/PentaOnboardingConversationInput.cs|30D677516A675A13AFC483DE650F55F1CBF83488E797B20187BAD544A0212103|
|apps/api/Controllers/PentaOnboardingController.cs|3C0474C7DDB22FF800D2B406CD131F09EEE3FC536B1C240683769594BA0016E7|
|apps/api/Domain/Entities/PentaMiniSession.cs|4AD1FC6EC2B8DDDBF2B3A1F05691A82AB5DBD0E587E3C97C8073552F8C35B076|
|apps/api/Intelligence/Penta/PentaMiniOrchestrator.cs|7AAB737939FF0F496A265F069CA3D044BC7F2C34ACA9F6996A20EAE36F85C64D|
|apps/api/Data/AcademyDeskDbContext.cs|DCA9EE21B2D37992510F8A77BD66064DA9143DAD8571E914C9D0A080D7DA77CC|
|apps/api/Migrations/AcademyDeskDb/20261018050649_AddPentaOnboardingConversationState.cs|990CFE0B5BB6C921260215E49EC165C29D21A43F33A32E6DE7260A065F0C517E|
|apps/api/Migrations/AcademyDeskDb/20261018050649_AddPentaOnboardingConversationState.Designer.cs|3A5CAE956CF60BA0786B27B45795262335D9B4C52521D5A20B8BFA4881BC2828|
|apps/api/Migrations/AcademyDeskDbContextModelSnapshot.cs|9C27A755681B7108FCB07B4E2C75E4CC99D056832C0C82A5090A5D7643224B88|
|tests/AcademyDesk.Api.Tests/PentaOnboardingConversationTests.cs|00E228044C5EE931CB745022AFB71445DAB2747CA9B6D9EF96F224ED1C3E4BF9|
|tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs|897664A21207C4457E54334D301E382C54A2F1F602E0B86F46B7FD74CB894155|
|QA/tools/SqlHarness/PentaOnboardingConversationRegression.cs|A69237C7F23801D1768389A26182ED0892B3F8686561B327F966C917C4BB90E0|
|QA/tools/SqlHarness/OnboardingAtomicRegression.cs|F4E539D4100A357B213219E0ED8CC604F427F1DBCA3378F8CFD9D380FFFBB7A1|

## Checklist / continuation

- [x] Existing shared domain, protected drafts/partial/lifecycle/preparation/approval/result retained; affected SQL and recorded SDK checks rerun.
- [x] Protected current selection and durable prompt admission/current authority/CAS/restart/audit/race proof. Admission only.
- [ ] **Next Sol High, same project:** claim-aware adapter dispatch and atomic verified turn completion/receipt/reconciliation, then assembled private Core gateway. **Do not clear pending first to call today's idle-session endpoints:** that reopens the race. Current endpoints deliberately cannot execute against a pending claim. No permissive completion endpoint or automatic expired-claim recycling.
- [ ] Qualified interpreter and authenticated conversational UI/real approval/linked SQL-browser journey. No shared Core change/project switch required now; no full AI/model qualification by this batch.
- [ ] Remaining AI-01–AI-14 domain/role workflows, full Orbit+Aura/Prism Manual parity, provider/privacy/keys/retention/operations/device/security/regression/enterprise/pre-Azure gates OPEN. BUG-DATA-0001 remains OPEN for its browser/device work.

API restarted session76446, localhost5092 health200; frontend3000 login-page200. Process-only Testing/local DB, startup migrations/bootstrap/media/PENTA/Mini/operational flags OFF. Actual Admin sign-in unverified; no reset/reseed. No developer schema, frontend, primary-main/PENTA source/stage/commit/push/merge/Azure/paid-resource change. Evidence local/untracked, builds ignored, unrelated dirty work preserved. **Production NOT READY.**
