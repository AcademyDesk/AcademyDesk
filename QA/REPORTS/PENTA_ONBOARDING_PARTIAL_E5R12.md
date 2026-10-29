# E5-R12 — partial conversational onboarding drafts

18 October 2026. Codex / recommended engineering route Sol High. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved local work. Continues E5-R4–R11 and the existing enterprise testing program.

## Implemented scope

- Added bearer-only `POST .../drafts/{draftId}/core/requests/{requestId}/partial`,16KiB limit, under the existing Testing + draft + HTTP + execution-candidate gates. No frontend, model catalog or provider activation. Manual/read-only conversation services unchanged.
- Reused the existing strict preparation parser for a separate partial subset: exact `clarify` decision, current six-field scope/catalog/base revision, route-matched GUID nonce, one allowed onboarding tool/full replacement, no removals,1–3 unique known questions and `invalidates_review=true`. Unknown tool/credential/authority fields and duplicate JSON keys remain rejected. This is a host-specific subset, not a change to Core's portable contract.
- Current SQL actor/role/academy/module/private authority hash/session and protected draft revalidated inside the existing academy-first Serializable/linked-Identity transaction. Only nonterminal, nonpaused, unexpired current drafts can advance. No new workflow store/table/migration.
- Canonical partial fields use existing parser/domain-default/blank normalization, including explicit false preservation and minor guardian defaults. Questions must refer to fields actually missing from the **new draft's** host-required set, including conditional guardian fields from the supplied DOB. A complete draft must use preparation, not be mislabeled partial. Partial readiness does not replace full domain/business/duplicate validation at preparation/confirmation/execution.
- Full replacement is explicit: Core must resolve refine/clear and retained values before calling this adapter. Omitted optional fields are removed, not silently merged. Saving a partial draft increments its revision, removes preparation/approval, returns collecting with no operation reference, and commits encrypted payload/request history/mandatory audit together. It never calls domain creation or creates accounts.
- Existing protected request history distinguishes `partial` from preparation/lifecycle. Current identical replay revalidates authority/session/privacy/current catalog and returns a snapshot with original transition time, no extra write/audit/TTL extension, including after host restart. Changed fields/questions, old revisions and nonce reuse across ports fail409. This is per-draft request history, **not** a complete durable conversation-turn claim ledger or current-draft selector.
- Core origin includes partial-save history, so the legacy authorization/execution boundary cannot be escaped by later ordinary edits. Partial replacement of a prepared/approved draft invalidates its old operation/proof. Paused/cancelled/Succeeded remain terminal or protected as appropriate.
- Existing context projection blocks foreign/inactive branch references and medical-note sharing before commit. Projection failure rolls back partial state; no silently stripped medical text or foreign labels. Secure medical input remains in Manual. No blanket privacy approval or generic free-text secret classifier is claimed.
- Backward rollback to a binary unaware of encrypted `partial` history must fail closed and preserve evidence for reconciliation, not delete/repair/resend it. Developer database untouched.

## Accepted verification

**325 focused tests PASS**, zero failures/errors/skips:312 retained+13 new parser cases, plus extended existing controller gates/request-limit assertions. Receipt `QA/EVIDENCE/penta-onboarding-partial-20261018/onboarding-partial.trx`. Current EF model/snapshot and shared onboarding/audit checks included in the retained filter. Normal API/test-copy binaries match `4BB4CD121DBBCD75C8304C3588126E5CC4756B64E7C1BF45380101FBB2CF7268`; isolated API/harness-copy match `15D828825FBAD8A2C76CB57C671170AD5E03A081844CB8694AD6395669F3419D` (two build-location pairs, not four identical binaries).

Isolated build PASS, zero warnings/errors: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql`. Existing runner unchanged: `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`. Final run **`d532be16e84f4a4dbe3b32376d59e2b2`**, exit0/**74.9s**,101 app/7 Identity migrations; both contexts match exact run-owned target/runtime login. Inventory348 routes/337 controller method-routes/10 framework Identity method-routes; SHA256 `6FF61FAD7112EA7D723FA1C5D817A4709684DCEA896E5700E17F7CA6DB71AC6D`.

**295 named SQL cases PASS**:257 retained +38 new partial-save HTTP/SQL cases. New matrix: anonymous/Teacher/foreign/other-actor/default-disabled; forged scope/base/catalog/approval/questions/present-question/credential/tool; simultaneous identical request200/200/one revision/audit/no domain writes; restart replay; changed fields/questions; retained names/explicit false; stale replay/ID reuse/cross-lifecycle/cross-preparation nonce reuse; optional clearing; complete draft must prepare; approved partial edit invalidates proof/no writes; legacy confirmation denied; minor guardian requirements; medical secure step/foreign reference rollback; required audit failure/full rollback/explicit verified retry; competing partial200/409; paused/cancelled/expired/stale-session/current-authority denials. Races reject429. All existing suite counts/assertions/runner retained. This is a bounded deterministic candidate, not production soak.

Runner rejected mismatched cleanup marker, verified exact database/login removal, then ownership-checked/stopped/removed only `academydesk-qa-d532be16e84f4a4dbe3b32376d59e2b2`. Other/PENTA containers, previous stopped failed containers and evidence preserved; no prune.

## Actual pinned Core compatibility

Five scripts PASS against this run's recorded HTTP/SQL receipts, using actual candidate.5 manifest-pinned PENTA source and no bytecode writes. These are **recorded-port checks**, not live SDK HTTP transport, interpreter inference, human/browser approval or tier qualification.

- `penta-onboarding-partial-contract.py`: six source pins, actual reducer + DialogueSDK.save_partial accepted **five** recorded journeys (collecting and approved starts); retained candidate values/false flags, newer collecting draft/no operation identity; stale requests and wrong responses denied. SHA256 `09db0cdddfea16fadc50455f7732daa64e7b8ba36e61702a6857dc05dd98ed81`.
- Existing approval execute/reconcile check PASS: one journey, altered binding/terminal/lost-response reconciliation without redispatch. SHA256 `b9ba47c481b7528f3b97f179a20551d0f40c2807ce3da0baf06430dbc2278232`.
- Existing lifecycle check PASS: five journeys. SHA256 `dc13555b818edf2b71f4c9a452469edcefa41a4e1df529a456575ea1d6b7b152`.
- Existing preparation/validation check PASS: one journey/six validation reports. SHA256 `36b270b2cd6a88de860ebd8d1d280b418944a1b102dc2347dd0cbc8d129b3f60`.
- Existing context/reducer check PASS: six snapshots. SHA256 `7693cad22c2b32026838ec2d955a73b6c99116a58cfee0c035e798a71c9c505d`.

Run each `QA/tools/penta-onboarding-*-contract.py` with `--penta-root "D:\PENTA AI Models"` and matching receipt under `QA/EVIDENCE/penta-onboarding-{partial,approval,lifecycle,preparation,core-context}-d532be16e84f4a4dbe3b32376d59e2b2/`. No model/native launch, network, training, repeated engine benchmark or PENTA source edit.

## Source provenance

| File | SHA256 |
| --- | --- |
| apps/api/Intelligence/Penta/PentaOnboardingPartial.cs | 2F7D9794817F5EC6D75042EDF05D5DC3CBBEFB7A66EBF1C6C3A1CF5636B4C633 |
| apps/api/Intelligence/Penta/PentaOnboardingPreparationInput.cs | 068A00223970A8D0CE47B9F34F71D4E7DDF2B92C38A6762ED36C74EA8E85C5EE |
| apps/api/Intelligence/Penta/PentaOnboardingDraftService.cs | 80E116B9D455960D62D1CE6748C01964A5F7F081D308A3CD76AA49676C1BA03C |
| apps/api/Controllers/PentaOnboardingController.cs | ECE1CEF1EA3D50109DDEBC4276B5BD4F67F0769F70DD3477490B251E7ADB1536 |
| tests/AcademyDesk.Api.Tests/PentaOnboardingPartialTests.cs | 64A453B4316FC7A9F7953023A7C56241B788BB2447028FCCB40C7E38275F8411 |
| tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs | 8E79F23FDAE06FDD2223FE500A1BF741E625557BC85473DB39CA64666E13C35B |
| QA/tools/SqlHarness/PentaOnboardingPartialRegression.cs | C51AE2BBF1D88BA44E107E27E11B9A5F3AC73CD8DA6995CF90FA281DB0DE0564 |
| QA/tools/SqlHarness/OnboardingAtomicRegression.cs | C8B047A96ACA642403F9933563E854232EC1DBD58F9BBAFFD32E8C525FDFF09B |
| QA/tools/penta-onboarding-partial-contract.py | 6B30F467EA2970B072CD5A9D024FEFCB228F77315B75F92CF2BFA0979A6719E4 |

## Checklist / next

- [x] Existing shared onboarding, protected draft/lifecycle/preparation/approval/result work retained.
- [x] Strict partial-save adapter, current required-question checks, encrypted replay history and approval invalidation; focused tests pass.
- [x] Current-source SQL and actual pinned Core recorded-response compatibility checks PASS; compiled pairs/source hashes recorded, owned teardown verified.
- [ ] Next Sol High, same project: protected current-draft selection and durable turn claims, preserving the existing read-only Mini state and source-specific authority. Assemble the live host/Core gateway, then controlled authenticated conversational UI with actual user confirmation/result/error states. Partial save alone does not provide selection, orchestration or inference.
- [ ] Private model qualification, remaining domain/role families, Manual visuals, provider/privacy/key/retention/ingress/unknown-outcome/soak/device/enterprise/exact-release gates.90% is still the product target, not delivered coverage. BUG-DATA-0001 remains OPEN.

No main/PENTA source/staging/commit/push/merge/Azure/model activation/training/download/paid resources. Evidence local/untracked, `.build-check` ignored. API session8543/process-only Testing, migration/bootstrap/seeding disabled and all PENTA/Mini/operational/media switches off; health200. Retained frontend3000/login200. Developer Admin sign-in unverified; no password reset/reseed/developer SQL changes. **Production NOT READY.**
