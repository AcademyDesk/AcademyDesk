# E5-R14 — claim-aware atomic draft completion and reconciliation

18 October 2026. Recommended route Sol High. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved unrelated work. Continues E5-R4–R13 and the existing enterprise program.

## Delivered scope

- Added bearer-only/no-store Testing-gated `POST .../conversations/{conversationId}/claims/{requestId}/complete` (32KiB) and `GET .../claims/{requestId}/result`. Existing draft/execution-candidate/HTTP flags remain required. No new schema, model catalog, production activation or frontend change.
- Strict completion envelope contains only the exact original claim and a supported scoped draft decision. Original claim digest/version/revision/hash/request ID, current selected draft, current SQL actor/academy/role/module/private authority and retained context are revalidated inside the academy-first Serializable/linked-Identity transaction. Model tenant/role/approval/result claims never authorize anything.
- Extracted private caller-transaction helpers from the **existing** partial, preparation and lifecycle implementations. Their original idle-session endpoints preserve validation, history, replay, approvals and response behaviour. Claim-aware completion calls the same helpers within its transaction, not another HTTP endpoint or nested transaction. No clearing/releasing/saving pending first.
- Supported decisions are `clarify`, `host_prepare`, `request_pause`, `request_resume`, `request_cancel`. Helpers retain conditional guardian/default/reference/duplicate/privacy/required-field rules, request history and approval invalidation. Arbitrary SQL/code, unknown tools, credentials, approval or domain execution cannot be completion kinds.
- Paused drafts may now admit a conversation claim so an explicit resume can proceed. Partial/preparation still reject Paused; only the existing lifecycle resume transition can change that state. Cancelled/Succeeded cannot admit new claims. This deliberately supersedes R13's broader paused-admission restriction, without granting creation authority.
- Draft change, adapter audit, encrypted exact turn receipt, mandatory completion audit, cursor and pending-marker release commit **once together**. Same completion races return exactly201/200 with identical receipt/one revision; conflicting decisions on the same claim return201/409. No429 is accepted by race assertions. The retained Mini read-state/version are not rewritten.
- Result is `draft_updated`, never student-created/succeeded, with `canExecute=false`; preparation returns only the canonical awaiting-approval operation. Existing separate host challenge/confirmation/execution remain required. A later partial invalidates actual approved proof. No student/guardian/account mutation occurs in the new turn suite.
- Receipt ciphertext binds actor/academy/session/request/context/operational version/draft/base+result revision/current authority/payload/claim+decision digests/kind/completion audit and timestamp. Raw prompts are not stored. Exact current replay verifies protected draft history/status, original mandatory audit, current snapshot and prepared-operation bytes before returning stored result; it never dispatches again. New turns, edits/approval changes, authority/context changes or missing evidence fail closed rather than restoring old results.
- Read-only result lookup returns202 for an actually pending claim and200 for the current verified completed receipt. It adds required redacted read audit. Audit failure withholds results. Failed validation/privacy/audit/deadline cannot release pending or partially commit a draft. Rejected plans may be explicitly corrected under the same unchanged pending claim; no automatic restart/retry/expiry recycling or permissive clear endpoint is introduced.
- Found a real-clock edge during review: sampling receipt and audit times independently would break strict replay even if a frozen test clock passed. Both now use the same sampled completion instant; SQL regression uses an advancing clock and checks replay. Final save also rechecks deadline **after** SQL writes and before commit, so mid-save expiry rolls back the whole transition.

This is deterministic **host draft completion**, not qualified inference, a live Core HTTP bridge, a complete conversational gateway, human/browser approval, or domain-execution turn completion. A model proposal remains untrusted input.

## Accepted verification

**362 focused tests PASS**, zero failed/error/skipped:343 retained+19 new completion-parser cases; existing HTTP gate/size assertions extended. Shared onboarding/audit/model-snapshot tests remain in the filter. Final receipt `QA/EVIDENCE/penta-onboarding-turn-20261018/onboarding-turn-final.trx`. Initial same-count `onboarding-turn.trx` predates the clock/receipt hardening and is not final-source acceptance.

Normal API/test API-copy binaries match `8B5B3CE4843C4866597FC1ABA273230B2C00FC048502CC778CF8A229637CA333`; isolated API/harness API-copy match `9171C59AFDE958A3F534BF47D12318BB91C742ACE3A9C291A72971F973CBACBF`. Two build-location pairs, not four identical binaries.

Isolated harness build PASS, zero warnings/errors,31.26s: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql`. Existing runner unchanged: `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`.

Final **`2a163599570047d988b320f4af1c8f64`**, exit0/**90.3s**,102 app/7 Identity migrations, both resolved contexts match the exact run-owned store/runtime login. Inventory353 routes/342 controller method-routes/10 framework Identity method-routes; SHA256 `8A2D9DCCE321CFAC856F0332C0E11471EA52A5A7958A343968B21F2E8056ED1D`.

**384 named SQL cases PASS =338 retained+46 new HTTP/real SQL cases.** New46-count assertion includes: anonymous/Teacher/foreign/other actor/default gate; result access; changed prompt/hash/version/scope/privilege/SQL/execute/base; pending result202; route nonce; identical completion201/200; exact atomic state/audit; altered replay; host recreation/lost-response result; missing audit; newer turn; canonical preparation/no permission; legacy execution denial; separate actual host approval; approved-draft partial invalidation; pause/partial-denial/resume/cancel/terminal admission; final audit rollback/pending/verified explicit completion; advancing-clock replay; read-audit withholding; mid-save expiry rollback; medical/foreign-ref rollback; expired completion; competing decisions201/409; stale retained context/current authority; unchanged domain/account snapshot. Existing runner/assertions and all prior matrix counts retained, not weakened.

Runner rejected mismatched cleanup marker, confirmed run-owned database/login removal and individually ownership-checked/stopped/removed only `academydesk-qa-2a163599570047d988b320f4af1c8f64`. Final absence checked separately. No prune, other/PENTA container or evidence deletion.

### Interrupted prior diagnostic

Run `4349e0fa174442e59bf90cb92f8da001` was launched before the chat/tool-session interruption. On resume its full new journey file and retained receipts existed, no owned process/container remained, but the final log/exit/duration could not be retrieved. It is **not counted** as explicit acceptance and was superseded by the fresh final run after clock/receipt hardening. No cleanup or claim of direct exit evidence was invented for it. Previous stopped failed containers/evidence retained.

## Actual pinned Core recorded-port checks

Six checks PASS using unchanged scripts and actual candidate.5 manifest-pinned PENTA source. No PENTA file change/bytecode/model/native launch/network inference/training. Not live SDK HTTP or model qualification.

| Check | Evidence SHA256 |
| --- | --- |
|Partial/reducer,7 journeys (retained5+new claimed2)|`3ea211a620027ebee639a88a9bfbc81a3ffa0c2332366f264110a3613292d72b`|
|Retained standalone partial,5 journeys|`9ea1ada0500c04a42ad7346e8d68baad82a75fb892b9241c0c223d2c060303c8`|
|Approval/execute/reconcile,1 journey|`106e7aaedb5425dd5f08fbdf251377f7e51d36e04f2733376681b0edd9abb1e1`|
|Lifecycle,5 journeys|`bc7684c8af44d1cecf33ebd4aa2bca33b6d20ff3c1e9b3d85f40d924450e4b53`|
|Preparation/validation,1 journey/6 reports|`dee86a4327410e15549960094e6cacb28130f3be1c67bea0d40aa74f6a14f6f1`|
|Context/schema/reducer,6 snapshots|`c953f4cd3d54d2ec02d111ba9ed45a4cbce3c16d2d08f2f7e238272f9a711226`|

New `QA/EVIDENCE/penta-onboarding-turn-2a163599570047d988b320f4af1c8f64/core-turn-partial.json` contains only the two new journeys (collecting and approved start). Its adjacent `core-partial.json` combines these with the exact five retained run receipts to meet the **unchanged** compatibility script's full matrix requirements. These are seven recorded checks, not seven new SQL cases. Use `penta-onboarding-partial-contract.py --penta-root "D:\PENTA AI Models" --receipt <combined core-partial.json>`. Other existing scripts take matching receipt/snapshots in the retained run directories. Claimed preparation/lifecycle are proven in SQL; no new recorded Python transport wrapper is claimed for them.

## Source provenance

| File | SHA256 |
| --- | --- |
|apps/api/Intelligence/Penta/PentaOnboardingTurn.cs|A20E5C586506790F1F812400FA4C5B1294A4F4F75E794A3935F9305A016C265B|
|apps/api/Intelligence/Penta/PentaOnboardingCompletionInput.cs|F0B815EDB691CBDDAEE41EEAFB50CE657A4154E1753E16ABD7B3F1D992E3A7AD|
|apps/api/Intelligence/Penta/PentaOnboardingPartial.cs|2D44238219086CA8A23E80469459EDC764225AF0001238F30162E18ABC08D523|
|apps/api/Intelligence/Penta/PentaOnboardingPreparation.cs|2497C048B09B62E8A1B22E9E12C3C1356A429E2561447A9378489053BE519C74|
|apps/api/Intelligence/Penta/PentaOnboardingLifecycle.cs|06256A306419189AD9D76F441FC3ADE5CE62A9A20E16883809012B3C8F133FE6|
|apps/api/Intelligence/Penta/PentaOnboardingConversation.cs|B0B3FAB2D39B096A0F079EA97394B1B6DF1D7BB0B53B0636EA96EB290DDA9015|
|apps/api/Controllers/PentaOnboardingController.cs|B0A891D9D1D8C702F5589000AEBE75F43E32C4A0D097D7CC537AC80906978B95|
|QA/tools/SqlHarness/PentaOnboardingTurnRegression.cs|4CBFE3DF42A4349BF40434398A7B3709DF7025E7E944AA878540914465297F38|
|QA/tools/SqlHarness/OnboardingAtomicRegression.cs|47921D47CF7561BA6249CE839121248BC853149141C181587F3744937E1F9DA1|
|tests/AcademyDesk.Api.Tests/PentaOnboardingCompletionTests.cs|1E18DC10F80D844EF97DFC0323EFC46065FC09524F7DA274FCCC603AC41420D4|
|tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs|41F93F795F302CCA1666D62AF3E197878E1C1267AF523B20008970A9CB7FB55E|

## Checklist and next

- [x] Previous selection/admission/draft/shared-domain/approval/result checkpoints preserved and affected suites rerun.
- [x] Claim-aware draft adapters, atomic completion/receipt and current verified reconciliation, races/restart/deadline/audit/authority proof.
- [ ] Next **Sol High**, same project: assemble private Core gateway and remaining non-mutating refusal/clarification/provider-unavailable outcomes with bounded recovery; bind actual current snapshot/claim to private interpretation and exact draft completion. No automatic pending clearing or arbitrary caller-claimed success. Domain execution/human approval turn composition remains separate from draft-update receipts.
- [ ] Qualified interpreter/transport and authenticated conversation/approval UI + linked SQL/browser journey; no accepted operational model return or project switch needed/claimed by this batch.
- [ ] Remaining AI-01–AI-14 domain/role workflows, all-screen Manual Orbit+Aura/Prism parity, provider/privacy/keys/retention/operations/device/security/full enterprise/pre-Azure release gates remain OPEN. BUG-DATA-0001 browser/device work remains OPEN. No whole-app readiness percentage or production approval.

Existing API restarted session35614 on localhost5092, process-only Testing/local SQL, migrations/bootstrap/media/PENTA/Mini/operational flags OFF; frontend restarted session10834 on localhost3000. Health/login-page checks recorded separately; actual Admin login remains unverified, no password reset/reseed. No developer SQL/schema, frontend, main/PENTA source, stage/commit/push/merge/Azure/paid resources change. Evidence local/untracked, builds ignored; unrelated dirty work retained. **Production NOT READY.**

Final checks: API `/health`200 and frontend `/login`200;11 source receipts match, scoped diff checks PASS, no staged files, final owned container absent, evidence untracked/build ignored. Worktree/primary/PENTA HEADs unchanged. Login-page availability is not authenticated Admin acceptance.
