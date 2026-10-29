# E5-R11 — host-issued onboarding approval and result bridge

18 October 2026. Codex / recommended Sol High route. Local worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved uncommitted work. Continues the existing onboarding/enterprise program; no backend restart.

## Scope and controls

- Added five bearer-only,1KiB POST ports under the existing draft route: `core/challenge`, `core/confirm`, `core/operation`, `core/status`, `core/execute`. Existing Testing/draft/HTTP/execution-candidate gates remain mandatory. Production/Development cannot activate these by setting flags. No model-tool registration or frontend activation.
- Strict binding accepts only exact operation reference, current revision, lowercase canonical payload digest and, only for confirmation/execution, an opaque GUID approval reference. Unexpected authority/approval fields, duplicate keys and malformed references fail closed. Route/current SQL identity resolves the academy/actor; no model-supplied role or tenant is authority.
- Challenge and explicit confirmation are code-owned host actions, not WorkflowDecision kinds. Challenge uses current authority/session/protected preparation, current domain/duplicate validation, and a random nonce with a five-minute deadline capped by the original draft/session deadline. Challenge remains awaiting approval and creates no student. Current challenge replay retains its exact nonce/deadline with no extra audit. Expired unconfirmed challenges need an explicit new issuance; superseded nonces cannot confirm.
- Exact confirmation revalidates the same scope/revision/hash/nonce/current authority and business rules, then records authorization and mandatory audit atomically. Concurrent identical confirmation produces one approval audit, no domain mutation and no expiry extension. The new metadata is inside the existing encrypted draft, not a separate workflow database or model memory.
- Core origin is retained in protected request history, not just the latest preparation marker. The older review/authorization/execution routes cannot authorize or dispatch a Core-origin draft, even after a plain edit clears its current preparation. Editing, pause/resume or cancellation drops its proof; current Core preparation/confirmation is required again. Separate non-Core test-host flows and Manual onboarding remain supported.
- Core execution wraps the existing shared domain/Identity transaction rather than creating another service. It independently verifies current exact proof/binding, authority, session, expiry, business rules, references and duplicates. Deadline checks run before execution, after the shared create and immediately before commit. Student/optional guardian/access grants, protected Succeeded receipt and mandatory execution audit remain one transaction. No portal accounts or external provider effects.
- Core operation/status reads independently verify current actor/context and, for Succeeded, real owned student plus the original execution audit. Uncommitted status has no receipt/audit/source success references. Successful result carries exact operation/scope/digest/revision, committed receipt/audit and owned source IDs. Required read-audit failure withholds the response; missing execution evidence cannot manufacture success.
- A committed exact execution replay returns the same receipt, never dispatches again, and remains reconcilable after approval/draft expiry while session/current authority still permit it. Expired uncommitted approvals are not exposed as usable operations. Superseded/paused/cancelled operation bindings fail409; inspect the authoritative draft lifecycle rather than inventing a historical operation receipt. A full historical operation ledger is not claimed.
- Current completed operation preview now says onboarding completed/reconcile/no repeat, rather than reusing the uncommitted “no student has been created” wording. Future UI must display all canonical fields and obtain an explicit human gesture; authenticated synthetic HTTP confirmation is **not** human/browser acceptance. No approval port may be added to the model planner allowlist.
- No schema/migration/developer SQL change. Old Core-origin Authorized/Succeeded bodies without the new proof fail closed; preserve them for manual reconciliation, never inherit authority, delete their evidence or automatically create again. Existing non-Core encrypted records remain compatible.

## Accepted verification

Final current-source validation **PASS**, run `7bd0224e75964ac589cceb673b5a7197`. This accepts the gated host adapter slice, not live conversational AI, a human/browser approval interface or production readiness.

- Current focused .NET receipt: **312/312 PASS**, zero failed/error/skipped, `QA/EVIDENCE/penta-onboarding-approval-20261018/onboarding-approval-accepted.trx` (298 retained +14 new binding parser tests; disabled-controller/limits assertions extended). Includes shared onboarding/audit/model-snapshot checks.
- Initial reflection-only failure found collision with inherited ControllerBase.Challenge overloads; method renamed IssueChallenge without changing the route. Nullable preparation-ID warning corrected. Earlier receipts retained, final accepted focused run warning-free.
- Intermediate SQL run `95ebf5aeb86a48efa0af05a713c8a4e1` passed257 cases, exit0/161.6s,101app/7Identity and exact owned teardown. Its binary preceded the final success-preview correction/assertion, so it is **not the final source-matched acceptance run**. Actual pinned SDK execute/reconcile compatibility passed against that intermediate receipt; the final receipt was independently rechecked below.
- Final isolated build PASS, zero warnings/errors, using `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql`. Existing runner unchanged: `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`. Final run **`7bd0224e75964ac589cceb673b5a7197`**, exit0/**132.9s**,101 app/7 Identity migrations, exact run-owned target/runtime login verified. Inventory347 routes/336 controller method-routes/10 framework Identity method-routes; SHA256 `C9FC66FBEAD23474CD138E4E9AD87D61518190A5B1FDD7A191288ADFF6686AE0`.
- **257 named HTTP/direct SQL cases PASS**:218 retained +39 new approval/result cases. New cases cover role/tenant/actor/disabled/forged binding denials; legacy confirmation/execution bypass; invented proofs; concurrent challenge/confirmation/one audit/no creation; unapproved execution denial; operation/status with no fabricated uncommitted receipt; execution201/200 with one student/receipt/audit; truthful completed preview; current committed status; minor/guardian sources with no accounts; fresh-host replay/status after expiry; expired/superseded proofs and explicit challenge renewal; edited-draft origin retention; required challenge/confirmation/execution/status audit rollback; approval expiring during domain create with full rollback; missing committed audit and changed-current-authority denials. Race assertions reject429. Retained preparation/lifecycle positive fixtures now use the stronger challenge/confirmation/Core execution path with exact nonce assertions; no old denial/count/runner assertions weakened.
- Both successful runs removed only their exact run-owned DB/login/container after the unchanged mismatched-marker/label/name/loopback checks. No prune or changes to other/PENTA containers or retained earlier failed evidence.

## Actual Core compatibility — final recorded receipts

All four scripts passed against the **final SQL run**, with actual manifest-pinned candidate.5 source and bytecode writes disabled. These replay synthetic host responses recorded through real HTTP/SQL; they do not run live SDK HTTP transport, inference, human clicks, model training or native engines.

- `penta-onboarding-approval-contract.py`: five source pins; actual ConnectorSDK.execute/reconcile accepted one approved/committed journey. Altered hash/revision denied before dispatch; terminal replay reconciles without execution; synthetic lost-response callback requires exact status reconciliation with one execute-port call, not automatic resend. SHA256 `03d78db7033cc0b548610ddf7aedf4438833424730d584332f62202b775c0468`.
- `penta-onboarding-lifecycle-contract.py`: six pins/five actual DialogueSDK.transition journeys; fields retained, operation identity invalidated, stale calls/wrong response denied. SHA256 `fbf76d9412befd5ed318913a5bb46456a44fcec3670ec4c2eefbb59745426b34`.
- `penta-onboarding-preparation-contract.py`: six pins/one actual SDK.prepare journey/six validation reports; canonical/current bindings match, unapproved execute denied. SHA256 `4129dd5248a7486cc482bf4d58a5eaf4f0f962b3a54e14c4cb8dfa70d365811b`.
- `penta-onboarding-core-contract.py`: six host snapshots accepted by actual schema/reducer; required-field clarification and foreign tool/reference rejection retained. SHA256 `4af18ec9af5b11b5a7171c884c33c37362827e4a19fbedc3fd1207dfa82f499f`.

Commands use the matching `QA/EVIDENCE/penta-onboarding-{approval,lifecycle,preparation,core-context}-7bd0224e75964ac589cceb673b5a7197/` receipts and `--penta-root "D:\PENTA AI Models"`. No PENTA source/contract change or repeated model qualification run.

## Source / compiled provenance

| File | SHA256 |
| --- | --- |
| apps/api/Intelligence/Penta/PentaOnboardingApproval.cs | 999664AC1C33AA2A064FECFCC026FEC2DBC4AF01F94C1BC8DAD617E92AB15C94 |
| apps/api/Intelligence/Penta/PentaOnboardingOperationBinding.cs | 990E5383F0EA36F8589BCD21041376A1BF02EC0E92C346AD0D907056B7B007FE |
| apps/api/Intelligence/Penta/PentaOnboardingDraftService.cs | 73B820FCD0714CC43F472BCF69764F253196A739BA1787E25AC8B9FD7D865B98 |
| apps/api/Intelligence/Penta/PentaOnboardingPreparation.cs | AAE2330BD275E0F13431730B75378208C582F9F9590ED5782C786F00561B9C8B |
| apps/api/Intelligence/Penta/PentaOnboardingExecution.cs | AAFF2596538AF808118F0D7C17A8FFC7FED7B6DBE73B38B4D82E1E92BFE45CA2 |
| apps/api/Intelligence/Penta/PentaOnboardingCoreContext.cs | 17B4A3903E0E904F7973AE2B215D681E7FB097D413DB4E991C47B686E65537B2 |
| apps/api/Controllers/PentaOnboardingController.cs | 34142920D916797DCF0C7319DFFE2E7801916867EE2BE39DF859A65559EE21F9 |
| tests/AcademyDesk.Api.Tests/PentaOnboardingOperationTests.cs | C16F264443A736FA7A0CAEC8D57DB965CB221D905F2BA46B3FA01E0B62642C47 |
| tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs | E4673853B2A99939C8E2CA6FEBE24E9DE336AB90F226E5FCF2F8AE64BC36A4DF |
| QA/tools/SqlHarness/PentaOnboardingApprovalRegression.cs | 6FBA0CFC5116166019B539D0765423701CF22B88B8810E69B0F0AD007F3F1035 |
| QA/tools/SqlHarness/PentaOnboardingPreparationRegression.cs | 1E5C9F06D1CCE4B47C65B364A7A57FB36890A7940360BF4B885060CD4C75C5BA |
| QA/tools/SqlHarness/PentaOnboardingLifecycleRegression.cs | 8B26450829281A325DF1D261EAD1FAD1CD6BE366BB2D654934857856418EC1B4 |
| QA/tools/SqlHarness/OnboardingAtomicRegression.cs | 7BCB6ABDE257FA71567B535DDC1A3FE0FEC070E2CC9A782C353238C09C8E04A8 |
| QA/tools/penta-onboarding-approval-contract.py | 90C1303486D127B3CE5A36F251ECEBC27EC48936CC8FDF5FE8035BAB677484C3 |

Current normal API/test API copies match `944CD7063D38CBE001846D2944FA40C4875A50C40EDFC5446F6850CCC90F4365`; final isolated API/harness API copies match `2FA7D8B420A16AC9748519E4F47BF17F38C8FB3F56C80D57D9675EF1E25EBC9C`. Two build-location pairs, not four identical binaries. Unrelated enterprise/physical-device/provider/model suites are not closed by these focused results.

## Remaining / next route

- [x] Shared deterministic onboarding, protected drafts/lifecycle/preparation, one-time domain transaction and prior enterprise evidence retained.
- [x] Host challenge/confirmation/current-operation/execution/status/receipt adapters implemented; focused tests pass.
- [x] Final current-source SQL/SDK validation, compiled-pair/source provenance and exact owned teardown recorded.
- [ ] Next **Sol High**, same Academy project: durable turn claims/current-draft selection and partial-save adapters, assembled current-authorized Core gateway, then controlled authenticated chat UI/review/result/failure states. No PENTA contract change/project switch needed for these compatible host adapters.
- [ ] Qualified private interpreter/tier evaluations, remaining domain/role families, Manual visual parity, provider/privacy/key/retention/ingress/unknown-outcome/soak/device/enterprise/exact-release gates.90% remains an outcome target, not delivered coverage. BUG-DATA-0001 and enterprise testing remain OPEN.

API restarted session80932/localhost5092, process-only Testing, startup migration/bootstrap/development seeding disabled and PENTA/Mini/operational/media switches off; health200. Retained frontend localhost3000/login200. No main/PENTA source/staging/commit/push/merge/Azure/model activation/training/download/paid resource action. Production NOT READY. Evidence stays local/untracked; build artifacts ignored. Developer Admin sign-in remains unverified; no password reset/reseed or developer SQL update.
