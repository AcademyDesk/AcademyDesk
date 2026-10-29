# E5-R26 — mounted conversational onboarding host adapter

19 October2026. `D:\AcademyDesk-codex-p0`, `codex/penta-search`,
HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.
**SQL/browser host-adapter acceptance: PASS.** This is host integration,
not qualified Mini, general conversational intelligence or production activation.

## Implemented

- Prompt-first shared composer is now connected to the existing one-shot channel
  and protected gateway through an explicit readiness check. Mount stays passive;
  no prompt, inference, approval or creation is dispatched by visiting the page.
- Missing details can be collected in successive explicit turns, retaining a
  bounded in-memory transcript. Questions use validated registered field keys
  and current host labels; raw model/server prose and arbitrary URLs are not
  forwarded. Transcript is not persisted as private history or training data.
- Every message rechecks current account and exact actor/academy/session/draft/
  context/authority/cursor/revision/hash readiness. Completion independently loads
  fresh account/current/draft/digest/current and verifies the saved turn version.
  Presentation-only composition preserves verified provenance for the existing
  operation channel; it cannot add tools, fields, operations or approvals.
- A prepared draft disables the composer and uses the existing separate exact
  review → host proof → approval → explicit creation → source/audit receipt flow.
  Dialogue messages remain when reviewing/confirming the operation.
- Lost/unknown responses withhold private workspace details. **Check message
  result** uses the captured same-ID GET and fresh host projection, never a new
  request ID, POST replay, automatic retry, polling or token-refresh replay.
 202 stays pending. Abort, current credentials, exact route, version and late-result
  fences remain; no browser cancellation is presented as server cancellation.
- Current Executor re-selection is a local no-op/remount, not a submitted tool
  request or permission change. Other unavailable capabilities remain disabled.
- Real Mini qualification is still absent. This candidate admits only an explicit
  `IPentaOnboardingSyntheticInterpreter` QA marker, existing Testing/Core gates,
  gateway gate and new OFF-by-default browser-contract flag. The real private
  loopback interpreter does not implement the marker and cannot become eligible
  merely by being connected or by a returned model/tier/role label.
- Readiness always says `qualification:not_qualified`; eligible QA says
  `runtime:synthetic_test`, otherwise unavailable. The UI visibly says
  **Disposable test interpreter — not qualified AI**, exposes no qualified model
  selector, and rejects unknown/claimed-qualified metadata. There is deliberately
  no production or real-model-ready branch yet.
- Added bearer/Testing-only GET `.../conversations/{id}/send-readiness` and POST
  `.../conversations/{id}/browser-turns` (16KiB limit). Server checks synthetic
  admission independently; existing Identity/academy authorization, SQL locking,
  durable claims, strict interpretation, current-authority revalidation, domain
  validation and audit transactions remain authoritative. No SQL inside planning.
  Readiness itself is current, scoped and transactionally audited.

## Real red result and visual repair

Initial run `e87feb77dffe4307b31dab614da8049e` passed451 retained SQL and24
retained browser scenarios, then failed new light320 clarification visibility.
The first `/browser-turns` was201; all fresh verification/readiness GETs were200,
with no console errors. Immutable vendor mobile `.op-status{display:none}` was
intended for badges but also hid `.op-message.op-status` transcript articles.
The host now overrides **only status-message articles**, not toolbar badges or
vendor source. New verified projections scroll their own transcript to the latest
reply. The original visibility wait is unchanged; retained review-browser cases
now explicitly require status replies visible on narrow screens.

Failed run exit-532462766/232.7s is excluded from acceptance. Its detailed HTTP/
screenshot evidence and bounded diagnostic receipt are retained. Exact name/run
label/loopback port ownership checked before stopping only that failed container;
container/data retained, not pruned or deleted. This is a real UI fix, not a
model/prompt adjustment, quota increase, timeout extension or relaxed assertion.

Second run `36f0f6962b184411b5479b2108b19ae9` again passed451 retained SQL/24
retained browser scenarios, then exposed a second real defect. The status reply
was now visible but generic: completion materialization dropped the partial
adapter's already-validated question keys. `PentaOnboardingCompletionInput` now
copies `Preparation.Questions` into its completion record; a new focused .NET
test requires exact preservation. No new model text/keys are trusted. The same
browser missing-label assertion remains. Failed run exit-532462766/194.8s and
all evidence are retained; exact owned container verified/stopped/retained.

## Verification already observed

- [x]36 new synthetic dialogue contracts PASS: two-turn clarification/preparation,
  provenance, no mount writes, explicit readiness, unavailable/unmarked metadata,
  forged scope/grants/qualification, HTTP failures, unknown question keys, same-ID
 200/202 recovery, parallel sends, abort/route/token fences and invalid input.
- [x]277 retained client checks PASS:72 channel,44 operation,33 terminal,
 42 recovery,48 review,38 Start. Total with the36 new checks is313. The48 include
  all12 immutable vendor source pins. These are not real model or SQL acceptance.
- [x]189 focused onboarding .NET tests PASS,0 failed/0 skipped, with actual TRX.
  Three new question-preservation/SQL/default-runtime guard tests and environment/bearer/request
  size assertions extended to both endpoints. No empty no-test run counted.
- [x] TypeScript, focused ESLint, webpack87-page export, isolated harness build
  (zero warnings/errors) and diff/whitespace checks PASS. Default Turbopack issue
  remains open; webpack is the verified path, not a Turbopack repair claim.
- [x]22 retained synthetic read-chat browser scenarios PASS; final22 synthetic
  review-browser cases PASS with the new status visibility assertion. These use
  synthetic API bodies and do not qualify SQL, model reasoning or physical devices.
- [x] Final run `536e877689ec45f5b95f26a66a7532c1`, exit0/170.4s,
  **451 retained SQL +10 independent new HTTP/SQL guards =461**, and27 actual
  browser contexts (24retained+3new) PASS. R25's3 additional nested reload
  journeys also retained.102app/7Identity migrations only to exact owned SQL,
  runtime inventory360 (349controller routes+10Identity) matched both DbContexts.
- [x] New dialogue browser84 actual HTTP requests:43 Prism320 full conversation
  through separately approved creation,29 Orbit1440 committed-turn response loss
  with same-ID GET recovery then preparation only,12 unmarked connected-runtime
  denial at390. No429/unexpected console errors, external traffic or raw API-body
  substitution. Executor re-selection dispatched no HTTP and did not disable chat.
  Mobile and desktop screenshots inspected; physical devices NOT RUN.
- [x] Parent independently proved one new student/execution audit,3sessions,
 3drafts (revision3 Succeeded/Prepared, revision1 empty),7turn rows,4scripted
  interpretations/0unmarked calls, required claim/completion/partial/preparation/
  readiness/result audit counts and unchanged unrelated domain/Identity/academy/
  branch/session/draft/turn snapshots.67total new audits recorded; required subsets
  asserted exactly, not a separate assertion that total67 is immutable.
- [x] Anonymous401,Teacher403,other-actor404,foreign403 on both new routes;
  unmarked-interpreter503 and browser-flag-off503 before interpretation/admission.
  All10 independent denials preserve exact owned rows/audits and planner counts.
- [x] Mismatched cleanup marker refused; exact successful owned database/login/
  container removed. Both failed containers stopped/retained, unrelated/PENTA
  resources untouched. No pruning, evidence deletion or production data used.

The retained browser journeys separately create3 synthetic students; the new
dialogue journey adds exactly one. Neither count represents real customer data
or model-authorized execution. Lost completed-turn response recovery is now
real SQL/browser proof. A request observed pending202 and subsequently completed
in the same mounted browser journey remains a distinct OPEN case (synthetic
coordinator proof exists); do not equate completed-response loss with pending202.

No actual model inference, Mini quality/latency qualification, real prompt-injection
acceptance, production CORS/ingress or physical-device acceptance is claimed.
The10 guard cases are separate HTTP/SQL checks, not10 additional browser contexts.

## Source/artifact pins

SHA256 of final tested sources; evidence remains local/untracked.

| Path | SHA256 |
| --- | --- |
| `apps/api/Intelligence/Penta/PentaOnboardingGateway.cs` | `E32A1EBDFC22122324156DEB77DCB3E6A46AB39A6C0A6DC565A61E0458677F93` |
| `apps/api/Intelligence/Penta/PentaOnboardingConversation.cs` | `1627CFFC9000E8E476371C73DD3BAB50454BAB8BEB255C0DE2918B803BEF9EC0` |
| `apps/api/Controllers/PentaOnboardingController.cs` | `D6E2A13C01A2B517C6F3901FC42E59C758892D3DD96BDD573D02C4D218FDA2C2` |
| `apps/web/src/lib/penta-onboarding-dialogue.ts` | `E23A61DAA76DAF49716875602E816603F05B8471A1CED97C8E863E69AFEDECB2` |
| `apps/web/src/lib/penta-onboarding-channel.ts` | `E6213461085146F5AD2FFA1B65C9F5E98761240819542939EEE4655CFD2B914A` |
| `apps/web/src/lib/penta-onboarding-review.ts` | `381492CDC7BDD72BFE89C9CA7BFD30FB37A825E74709AAD725F272019024B72D` |
| `apps/web/src/lib/penta-onboarding-operation.ts` | `1871CE2FF68B93F9EB89D26E92F8F17113D9C430AF872113286D6583ABE1FC96` |
| `apps/web/src/components/penta/onboarding-dialogue-workspace.tsx` | `0D6CFD27BDBD5B34A057B21108A6F122D1A51B18663E59B54E8F75A1AA544ED7` |
| `apps/web/src/components/penta/onboarding-operation-workspace.tsx` | `FCE136443AE6C92E774F7BAC6586A7CE71DAE1AB4683A4E63C4D4AB5CA313853` |
| `apps/web/src/components/penta/onboarding-review-workspace.tsx` | `A5B5EC158530788AB5831F9762E3C01D0CB25AF1605F0ADF9B9122876982C61E` |
| `apps/web/src/components/penta/onboarding-review.module.css` | `497F184D6023390E1FDC36AD73928F3769D36A04AD3C74FAC6D143F49B5B4418` |
| `QA/tools/penta-onboarding-dialogue.test.cjs` | `8485E8F3ECCA58D433D0731657A53C9AB9B3EFA280314ACE2EDCDEED98605B6D` |
| `QA/tools/penta-onboarding-dialogue-live.cjs` | `BF3C0AA4EAD07D47617E23BE2684CAB286204CD60F023DC7690F32A83327DC11` |
| `QA/tools/SqlHarness/PentaOnboardingDialogueBrowserRegression.cs` | `5E904256FD0A0F77BC6CB5C6C8D7E5E26C8381DCBD05CAAE0A496F10A3138B8F` |
| `QA/tools/SqlHarness/OnboardingAtomicRegression.cs` | `C50135B478A6389FDDDB55D33E985BAF201060B394CA7151F3D3866C7AD5D495` |
| `tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs` | `FC2E22C140B829ADBFC545229C27F2A106DD7E317962F2F1E631DCB470623DDE` |
| `QA/tools/penta-onboarding-review.test.cjs` | `94C3E2C4F89F304F1DC7AA243653F9473A8992772253949F11EC50BBC26DF697` |
| `QA/tools/penta-onboarding-review-browser.cjs` | `36E9F36E6FD79130E278217B9872B2CA5F9643C1A89979822B90F49A7EA978ED` |
| `QA/EVIDENCE/penta-onboarding-dialogue-e5r26/contracts.json` | `1C2AA2DA6F84247851BFFE1D1D044B97AB4FD41E86147ACC33612DA5A078C928` |
| `apps/api/Intelligence/Penta/PentaOnboardingCompletionInput.cs` | `A3B83BD243C61D4C59B57AB9FB65F4D9128D46AD40952CA51AB19E627E99F8B6` |
| `tests/AcademyDesk.Api.Tests/PentaOnboardingCompletionTests.cs` | `01D43562ABE273C7131EDA3B8FE2552DAFE8A5D49825F6DCA0748EE29641C2E3` |
| `QA/EVIDENCE/penta-onboarding-dialogue-live-536e877689ec45f5b95f26a66a7532c1/result.json` | `E2446DBE51933D2942E9CF533783B45D06E71595531F7007805C8F68E6030575` |
| `QA/EVIDENCE/penta-onboarding-dialogue-live-536e877689ec45f5b95f26a66a7532c1/sql-postconditions.json` | `1F6E198797863117029DB6C579C7BFACAD196BE95F4D33C1D710E14F1EDC10D9` |

TRX `.build-check/onboarding-dialogue-tests/results/onboarding-dialogue-final.trx`:
`333AB8046EB0A0960532189A4B8894184D0324C78D9ADA99EEDAB5D3CDCC91E1`.
Isolated harness DLL `DB80909A3074650CBE4882EBA39CC0A59C4C442C839599240A746157E3539ABA`;
its API DLL `DC6DBBE43C601270F783DB22D4E5D5E02C93FEE0F39F1DDA1983FAF1317111FA`.
Normal developer API DLL stays
`049BBA230504354EE8245F32A1FFAA01DC0218429B1F30D7F53BC2F21DEAA504`.

Final synthetic review browser receipt `QA/EVIDENCE/penta-onboarding-review-browser-1792377622951/result.json`
SHA256 `E866DAF2B92ADB4527662CF116CB338BB5F429741AA137D2264FBCEF4240D73F`;
read-chat `QA/EVIDENCE/penta-chat-contract-1792377383067/result.json`
`F15EB791DD567F79E7521BB3E9DA09BEAA28B2E90E6D408609C1542B7B2D0EA9`.
Retained actual review/operation/Start browser `result.json` AND parent
`sql-postconditions.json` remain under their respective same-run evidence folders.
No rerun of the56-role-entry suite this slice; R25's result is retained prior proof.

Commands: existing named Node client/browser scripts; focused ESLint and
`npm run build -- --webpack`; isolated harness build with
`--artifacts-path .build-check/onboarding-atomic-sql`; focused restored .NET test
with `--filter FullyQualifiedName~PentaOnboarding`, actual final TRX above;
`QA_ONBOARDING_REVIEW_BROWSER=1` and
`pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`.
Temporary QA environment switch removed afterwards, configuration files unchanged.
Final normal web3000 onboarding200/API5092 health200/anonymous401; actual developer
Admin login remains unverified, normal backend DLL deliberately not swapped for
the isolated candidate or used to migrate/reset developer SQL.

## Remaining boundary and next route

This integrates the first AI-04 host conversation candidate using existing
backend/domain services. It does NOT complete all workflows,90% AI, the backend,
enterprise testing or production readiness. No schema/default configuration,
vendor/engine/weights/developer database/password/main/PENTA project change;
no model download/training/resource activation, stage/commit/push/Azure.

Stay **Sol High** for the qualified-runtime/admission/authority seam, actual
pending202-to-completed browser pairing and other protected conversational domain
families. Mini CORE-01's independently frozen semantic/latency/resource gates stay
BLOCKED. A real accepted runtime needs its own reviewed admission evidence and
paired model/host tests; the synthetic marker must never be added to a real model
adapter to make this UI work. Reuse the established Mini handoff, not a new engine
or testing plan. No automatic project switch/message or paid evaluation.

Durable/expired-session history, all other roles/families, complete Manual parity,
physical devices, privacy/security/provider/enterprise/exact-release gates and
BUG-DATA-0001 remain OPEN. Sol Medium is appropriate for settled bounded visuals
only after their authority contracts are settled. Full app is NOT production-ready.
