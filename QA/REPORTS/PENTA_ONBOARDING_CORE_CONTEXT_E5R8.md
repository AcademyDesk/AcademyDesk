# E5-R8 — actual Core context/catalog port

18 October 2026. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus preserved local work. Continues E5-R4–R7, not a replacement backend or testing plan.

## Implemented

- Added bearer-only `GET .../penta/onboarding/conversations/{conversationId}/drafts/{draftId}/core-context` under all existing Testing/draft/HTTP/execution switches. Uses the already scoped shared draft service; no new workflow store/schema/migration/DI dependency. Serving protocol0.1 and its read catalog remain unchanged.
- Produces actual candidate.5 `HostSnapshot` shape with authenticated server-resolved academy/actor, owned conversation/draft, canonical typed fields, current partial/approved/succeeded state and complete29-field host-owned `ToolDescriptor`. It does not expose arbitrary actions or another role's tools. Field types/bounds come from the existing parser, not the illustrative PENTA acceptance kit.
- Shared deterministic `StudentOnboardingRules.RequiredFields` uses the same minor/optional-parent condition as domain validation. Required/non-clearable DOB/name and conditional parent name/email are reflected in the catalog. No passwords or account usernames; portal-account creation remains a secure Manual workflow. Descriptor metadata is not full validation, approval or execution acceptance.
- Stable branch choices come only from current active same-academy SQL records. SQL GUID-to-text conversion initially produced uppercase IDs and could reject a valid lower-case saved reference; the first real SQL regression caught it. Host now formats actual GUID values consistently after materialization; tests retain strict equality and select the real owned branch. Foreign/inactive sources remain excluded. Over100 choices, overlong labels or context over16,000 characters fail closed without silent truncation. Paging/narrowing and native tokenizer limits are still future integration work.
- Existing encrypted draft is the private authority epoch. Connection reference includes its unique draft ID; `authority_version=1` identifies that immutable epoch, **not a global user permission revision**. Full private authority-hash changes reject the draft; a new draft yields a distinct connection reference. Zero-based host session version is projected as positive SDK `context_version=hostVersion+1`; callbacks must use this documented mapping, not change the stored session version. No hash/stamp or authority value from the browser/model is trusted.
- Serializable domain/linked-Identity transaction rechecks current authority, session/context/pending request, draft hash/expiry and selected branch before returning context. Every successful read requires its own atomic audit. Core `succeeded` additionally verifies the source student and required execution audit; a status label alone is insufficient. Ordinary review maps to collecting, never approved/prepared. Actual explicit host creation authorization maps to approved, with a stable revision-bound operation reference; this is still not a full `PreparedOperation`/approval-challenge port.
- A nonblank medical/accessibility note blocks Core projection with secure-workspace guidance rather than silently omitting the field. This is a specific field boundary, not general secret detection or completed privacy/consent acceptance. No raw prompts, model prose, arbitrary evidence or decrypted security state are added. Actual model calls remain disabled; no customer data was sent to a model.
- Added read-only `QA/tools/penta-onboarding-core-contract.py` to validate actual synthetic SQL-generated contexts with five manifest-pinned PENTA source files and its real executable `HostSnapshot`/reducer. No source changes or bytecode writes in PENTA; no inference/native runtime/model download/activation.

## Verification

- **264/264 focused .NET tests PASS**, zero failure/error/skipped (255 retained+9 new catalog/required-field/branch tests). Final post-fix receipt `QA/EVIDENCE/penta-onboarding-core-context-20261018/onboarding-core-context-final.trx`. Existing filter retained; current EF model/snapshot check included. Earlier pre-fix focused receipt is historical, not final SQL proof.
- Isolated harness build PASS, zero warnings/errors; repeated incremental build after the strengthened branch-selection fixture also PASS. Existing runner/assertions retained.
- **139 named SQL cases PASS**:118 retained+21 new authenticated Core-context HTTP/SQL cases. Final run `fe7f052ffe714246873202464a1fb08b`, exit0/122.3seconds,101app/7Identity migrations. Runtime inventory339 routes/328 controller method-routes/10 framework Identity method-routes, SHA256 `AA69D01750405F4E696582AB941787D4D5C18425EC6E35C3EBDA25ABB7BC5D93`. Both contexts match the exact owned target/runtime login. Final DB/login cleanup and ownership-checked container stop/removal completed; other resources/evidence retained.
- New21 cover anonymous/Teacher/tenant/other-admin/missing-draft/foreign-conversation/default-disabled denials, complete catalog/server scope, active owned branch choices, valid selected branch/partial minor context, review not approval, actual host authorization, source/audit-backed succeeded, missing audit denial, medical field boundary, foreign branch denial, stale context, required-audit503/full rollback, changed authority, distinct fresh scope, expiry. Snapshots verify denied/faulted calls make no domain/Identity/draft/audit changes.
- **Actual pinned Python schema/reducer compatibility PASS** for6 SQL-generated snapshots, typed replacement/missing-field and minor-parent clarification, unknown-tool and unregistered-branch rejection. Command: `python QA/tools/penta-onboarding-core-contract.py --penta-root "D:\PENTA AI Models" --snapshots QA/EVIDENCE/penta-onboarding-core-context-fe7f052ffe714246873202464a1fb08b/snapshots.json`. Receipt SHA256 `CB17E93319F87C558EE2E222A45F8158A660B2CEE476EFCC0C7CA12E2DEFF051`. This is not a full ConnectorSDK host call, actual language/inference run or model qualification.
- Failed first run `7669a17ed34f47a2ac802a6cbf8b9691`: retained118 earlier cases passed, new context suite reached branch identity mismatch. Corrected application GUID formatting, not a relaxed assertion. Exact label/name/loopback ownership verified before stopping only that failed SQL container; it remains preserved for inspection. No prune or evidence deletion.

## Provenance

| File | SHA256 |
| --- | --- |
| apps/api/Controllers/PentaOnboardingController.cs | 3E76BAA2CA4E0E49330EB3725F0007362FA7AF276E6364D5D4C06B650D54FC5C |
| apps/api/Infrastructure/StudentOnboardingRules.cs | 5AD23446A096931AA1AABAD31CF5617A7EB44F9909182AFEC5B91913FD8863A7 |
| apps/api/Intelligence/Penta/PentaOnboardingFields.cs | 3959097005AC4C2218FFE4F49ACDF0779DBA29B931D456E41C357CB28167A795 |
| apps/api/Intelligence/Penta/PentaOnboardingCoreContext.cs | BB7B0B12E7E7E0922447F5C76236DA0B521044D09D51F40C8573E06C26AA9245 |
| tests/AcademyDesk.Api.Tests/PentaOnboardingCoreContextTests.cs | F90EE8E2A390336F00EB0BF386CF84587E130FBC191FAAC8DE0688749263992C |
| tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs | 4C4A3C411B2D4A7096449C6F156A6E3A8754DAED306CEF2D19C08A7A99FBE2A6 |
| QA/tools/SqlHarness/PentaOnboardingCoreContextRegression.cs | 8CCF4834FA17F2655B9C5074535D7D595A6C466490BAD8B289595FD82BD1E5D3 |
| QA/tools/penta-onboarding-core-contract.py | EBC9C5C770D963960573388C2921EC41AFC0CA87AED1EC88F3A35403C1B1E40F |
| QA/tools/SqlHarness/OnboardingAtomicRegression.cs | 2DEB4A2E1F60DE0652AEA0A323E539AB1F1BA40E14FA07890A22077691E94568 |

Normal API/test API copies match `5C568E36A358B83926F009B17163DEAA5AACE0FFFF0DC69B6A442CE0D96D0250`; isolated API/harness API copies match `AE6B04B568C8A666C3319D31DAA7136F3F504795EBD313937E909EB4EFA85AB2`. Two pairs, not four identical build artifacts. Five consumed Core pins match `operations/release-manifest-candidate.5.json`; that is selected-source compatibility, not complete package/engine qualification.

## Remaining integration / runtime

- [x] E5-R4–R7 shared domain/drafts/exact authorization/one-time result/HTTP work retained.
- [x] Current authenticated host context/catalog implemented; focused tests pass.
- [x] SQL/Python context-port acceptance above. No unchanged real-model trials or full enterprise rerun.
- [ ] Next **Sol High**, same project: actual durable request/current-draft selection and SDK preparation/validation/canonical defaults/opaque approval/operation/receipt/status ports, then controlled authenticated chat confirmation. Current context read is not the complete `HostConnector` or `DialogueHost`; pause/resume/cancel, medical secure-step validation, optional blank normalization and request reconciliation remain explicit work. No model-produced request may approve/execute.
- [ ] Qualified private interpreter/CORE-01/selected tiers; all remaining domain and role bindings; privacy/retention/key/unknown-outcome/device/provider/enterprise/exact-release gates. Native context budget and actual inference semantics remain unqualified. No claim of90% coverage or production readiness.

Local API restarted session16830 on5092 with all operational/PENTA/media switches off, bootstrap/seeding/startup migrations disabled. One transient local health503 during restart/testing recovered to200 without configuration/SQL changes; cause not established and resource/soak qualification remains open. Frontend3000 retained. Admin authenticated sign-in remains unverified; no password reset/reseed/developer SQL change. No frontend code changes, main/PENTA source edits, stage/commit/push/merge/Azure/training/new models/paid resources. Evidence local, `.build-check` ignored. **Production NOT READY.**
