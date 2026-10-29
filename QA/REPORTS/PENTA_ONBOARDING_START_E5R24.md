# E5-R24 — explicit protected conversation start

19 October2026. Continue existing enterprise/PENTA work on `codex/penta-search`,
`D:\AcademyDesk-codex-p0`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.
This is the missing start/bootstrap slice, NOT completed conversational sending.

## Delivered

- The unbound `/penta/onboarding` route checks current account and host readiness
  with GET only. It offers explicit **Start onboarding conversation** only when
  the independently Testing-gated host reports an interpretation connection.
  Presence is NOT model qualification; the composer remains disabled.
- Explicit start allocates one host-owned session, empty encrypted draft,
  selected cursor version1, protected start marker/receipt and required audits in
  one serializable SQL/Identity transaction under the existing academy lock.
  No prompt, inference, student, guardian, account, financial or domain operation.
- Input is exactly one canonical lowercase GUID `requestId`. Tenant and actor
  come from current host records; no role/tenant/session/draft/field inputs are
  accepted. Daily actor/academy session/draft/turn limits remain enforced.
- Same actor/tenant/request concurrent submissions return201/200 with identical
  saved identities, one allocation and one creation audit. Purpose collisions,
  changed authority, corrupted ownership and missing audit fail closed. Replays
  never extend expiry or replace the selected workflow.
- Explicit **Check conversation start** reads the same captured request ID after
  uncertain delivery; no POST retry, polling, token-refresh replay or new ID.
  Saved recovery works without an interpreter after a host restart. Recovery is
  in-memory for this page lifetime, not yet a durable recent-conversations UI.
- Client checks exact shapes/tenant/request/IDs/UTC expiry/non-execution flags;
  rechecks account/readiness immediately before POST and account after success.
  Double-click lock, abort/token/route fences and delivered logout clearing.
  Manual remains independent. Fixed internal navigation uses verified GUIDs only.
- Three additive bearer-only endpoints use the existing controller's Testing
  gates, no-store headers, payload limit, exception redaction and safe errors.
  No feature flag, interpreter registration, schema migration, production route
  activation, model/protocol change or default permission expansion.

## Verification

- [x]38 new synthetic start transport checks, including no mount writes, explicit
  one-shot admission, lost-response GET recovery, offline/role/tenant/input/scope/
  grant/expiry/HTTP failures, abort, navigation, credentials and double-click.
- [x]186 focused onboarding .NET tests PASS, including10 new strict-start/SQL
  guard cases plus existing gate/request-limit assertions extended to new routes.
  Actual test run is recorded in ignored `.build-check/onboarding-start-tests/results/onboarding-start.trx`.
  A preliminary `--no-restore` command against a new artifacts folder exited0
  without tests/output; it is NOT counted. The subsequent restored run reports
 186 passed,0 failed,0 skipped.
- [x] TypeScript/focused ESLint/webpack87 export and isolated harness build PASS.
- [x]22 retained synthetic product-review and22 read-chat browser scenarios PASS.
  The old missing-ID scenario now supplies a draft without conversation ID,
  preserving its zero-request/fail-closed assertion for incomplete review bindings;
  the completely unbound route's new explicit-start contract is tested separately.
- [x]20 retained workspace-entry checks PASS. Retained operation44/recovery42/
  channel72/review48 checks rerun separately (see final outputs).
- [x] Final run `ae54393cecb245409e6214b5472acc75`, exit0/194.5s:
  **449 SQL checks (422retained+29new) and24 real browser scenarios
  (15passive+4operational+5new Start) PASS**.102app/7Identity migrations applied
  only to exact owned disposable SQL; runtime route inventory358 accepted.
- [x] New Start browser: Prism320, Orbit1440, committed response loss/GET recovery,
  disconnected interpreter and Teacher denial.34 actual HTTP requests, exactly
  one POST per successful start, no replay/inference/student creation/429 or
  unexpected console errors. Parent verifies3 sessions/empty drafts/start markers/
  start audits, exact actor/tenant, no execution audits and unchanged unrelated
  domain/Identity/academy/branch/session/draft/turn state.23 audited events recorded.
- [x] All retained actual review/operation parent SQL assertions pass, including
  R23's3 expected synthetic student creations/execution audits. Those belong to
  the retained creation test, NOT to this new empty-session Start operation.
- [x] Exact owned successful database/login/container removed; mismatched cleanup
  marker refused. All three failed diagnostic containers stopped/retained,
  unrelated/PENTA containers untouched. No pruning or evidence deletion.

Initial run `71034dcb899447a792c8c5ce587c39e6` passed retained422 SQL and the
first24 new start checks, then failed a QA expectation: stale bearer after a
security-stamp change returned authentication401, not domain409. Application
correctly rejected it before reaching the service. The regression now separately
requires exact401 for that stale bearer, fresh authenticated bearer409 against
the old authority epoch, inactive HTTP403 and independent service403; all original
no-write snapshots remain. No application behavior/error handler changed to make
the test pass. Initial runner failed (-532462766/226.4s), so no acceptance claim.
Bounded diagnostic receipt is retained under
`QA/EVIDENCE/penta-onboarding-start-failed-71034dcb899447a792c8c5ce587c39e6/`.
Exact container label/name checked, stopped and retained; other resources untouched.

Second run `899ea348ab6343cf9895e9ba6cde45e4` confirmed the stale401/fresh409
split, then exposed the second QA expectation mismatch: an authenticated but
inactive actor is denied403, not401. Restored that exact established HTTP403
expectation; the separate service403 and unchanged-state assertions remain.
Again no application change, broad accepted-status set or no-write relaxation.
This failed run is excluded from acceptance and its owned container retained stopped.

Third run `55476ede74994a5c9538a16da910ff02` passed all422 retained/29 new
SQL cases and15passive/4operational browser journeys. New Start passed mobile and
desktop, then correctly hit HTTP429 in its third journey. Read-only SQL counts
proved Academy A had reached100 daily drafts (51sessions/72turns), rather than
the120-request HTTP limiter. The new browser fixture now creates a separate
synthetic academy inside the SAME disposable SQL target. No quota/rate limit,
date, application source, retained fixture or no429 assertion is changed.
New fixture snapshots also include academies/branches. Third run is excluded
(-532462766/244.7s); its bounded failure evidence and stopped owned container are retained.

## Scope and files

New host partial `PentaOnboardingStart.cs` and three controller methods. New
client start loader/component and no-query page branch; existing read-chat link
description updated. Existing shared domain, approval/execution/gateway, private
Mini contract and immutable vendored operational/foundation source are retained.
New strict .NET/transport/real SQL/start-browser tests extend existing harness;
all previous422 SQL and15passive/4operational browser scenarios remain required.

## Source and build pins

SHA256:

| File | SHA256 |
| --- | --- |
| `apps/api/Intelligence/Penta/PentaOnboardingStart.cs` | `6539F5545245F12AA6654F06ED0B7B92FA3AF8985850EF397E3DF78502D93685` |
| `apps/api/Controllers/PentaOnboardingController.cs` | `3D51978AE687AAD71DE4B4E5AF071E7B3EE6AC7EBC7F3A0C0E1E852B02408830` |
| `apps/web/src/lib/penta-onboarding-start.ts` | `5842C1892BF275972D307C22BC211CB9C1D993174B252223B2A7465B61BCC05E` |
| `apps/web/src/components/penta/onboarding-start-workspace.tsx` | `0F2252C53B192728DA91D6A7D5192F2B4F7B09D77064D45135B7298CBFD234B4` |
| `apps/web/src/app/penta/onboarding/page.tsx` | `632B878BA3A6B46939CEE6EF5E1466037AF6A2B99A79FED8FF94C46190685C4D` |
| `QA/tools/SqlHarness/PentaOnboardingStartRegression.cs` | `64C9CC83082D957343282F3A76EAFBB60A77C5DACAFE6E1F74F5EC2B0418F7DA` |
| `QA/tools/SqlHarness/PentaOnboardingStartBrowserRegression.cs` | `5AA9CD6193C85985659495FA85D40DA34FD0CB47109DABFEC3E7C47FAD011669` |
| `QA/tools/penta-onboarding-start-live.cjs` | `418C631A1EE5E4FDBDCFF5077700CD56AEC36DC35646B3EE2294ABD4DCABAB63` |
| `QA/tools/penta-onboarding-start.test.cjs` | `EDA3B2DBA76D1CBAD1CE0F3D80364A190ED3DF5CE40FFFBD332239E422EF8AEE` |
| `apps/web/src/components/penta/penta-chat-workspace.tsx` | `05A77BA1F851EAC206C23A4D9E67D16555BF1D6C0E781B7A3BF496789292C463` |
| `QA/tools/penta-onboarding-review-browser.cjs` | `F961B838BA605A9DA87ABD0A46881BA8B458C18513021FE3227C64CE1B18DFD0` |
| `tests/AcademyDesk.Api.Tests/PentaOnboardingHttpTests.cs` | `BACEC21AEA73C1A1AA9C70ECFEA0D50AA164B420CC6DB5B9F7253085451F3F57` |
| `QA/tools/SqlHarness/OnboardingAtomicRegression.cs` | `3F7C6FD96B7F3821A5E774B8DE390B9A1DC509EE60DEBFF52DF3BF33012B0BC0` |
| Isolated harness DLL | `44752406D0BE92F9F3EAFE896D6ABB1F6E09CAFC86244F5E3B916AFBBFEBAEB2` |
| Isolated harness API DLL | `AC396B3FCF4B97D26344FD3EAA6AD65BDA2AD1ED921E7D9C6EAE3BB7C4EC9CE7` |
| Separate focused-test API DLL | `9517A1C92C6EEB27605CAE2BDD679582EF7DAC516F4D4D3184D5CDCEA6D36C2C` |

Builds use separate ignored artifacts roots; those API DLL hashes are not claimed
identical. Normal developer API DLL remains unchanged at
`049BBA230504354EE8245F32A1FFAA01DC0218429B1F30D7F53BC2F21DEAA504`.
No candidate assemblies/migrations are applied to developer SQL/runtime.

Existing developer processes were no longer listening at the morning smoke check;
cause unknown. Restored the same frontend3000 (session87345) and unchanged normal
API5092 (session56534), not another environment. Local Development connection
was validated in memory without disclosing values; initial conservative parser/
exact-name checks failed closed before launch, then the actual loopback/non-QA/
non-production-named Academy development target passed. Testing process only,
migrations/bootstrap/PENTA/Mini/operational gates OFF; no seeding/password reset,
configuration edit or developer DB migration. Web onboarding200/API health200/
anonymous session401. Actual developer Admin sign-in remains unverified.

| Evidence (local or ignored) | SHA256 |
| --- | --- |
| `QA/EVIDENCE/penta-onboarding-start-e5r24/contracts.json` | `08B00E35B221AB0469E3F082B9F08CF170E49E8E3E3F242015AEB11A9A11D308` |
| `.build-check/onboarding-start-tests/results/onboarding-start.trx` | `FA98ED871069E78335E4C19FEE6F5F2998A4A0E0BF8CFC8E51CD8490779FA10E` |
| `QA/EVIDENCE/penta-onboarding-review-browser-1792374127939/result.json` | `E866DAF2B92ADB4527662CF116CB338BB5F429741AA137D2264FBCEF4240D73F` |
| `QA/EVIDENCE/penta-chat-contract-1792374169726/result.json` | `F15EB791DD567F79E7521BB3E9DA09BEAA28B2E90E6D408609C1542B7B2D0EA9` |
| `QA/EVIDENCE/penta-onboarding-start-ae54393cecb245409e6214b5472acc75/sql-postconditions.json` | `D0A848B362D6DB00594CC2C167C6A35CB07A2D0A6504E5C7DBFA21A8E81B234E` |
| `QA/EVIDENCE/penta-onboarding-start-live-ae54393cecb245409e6214b5472acc75/result.json` | `99D51FCB2623B190DB52559ED5C91FF97511A14B10A28DFC7F408A2269311695` |
| Same Start browser directory `sql-postconditions.json` | `B80B6EC74FD3635A87C3ECFB7DBAC34ADCB19FA060B9431A205E2646F41B2436` |
| `QA/EVIDENCE/penta-onboarding-review-live-ae54393cecb245409e6214b5472acc75/result.json` | `D4673EC9680393B1219DED4A17CBD5004593FB1F6FFFA44DBD374594E4798E3F` |
| `QA/EVIDENCE/penta-onboarding-operation-live-ae54393cecb245409e6214b5472acc75/result.json` | `57D2197BFA5311E9BE8DC1F9BC21A6F50478AB052679321487922414045D990C` |

Real browser receipts alone do not establish SQL acceptance; each parent
postcondition file is written only after successful fresh SQL verification.
Saved synthetic account/token material is never written into these receipts.

Reproduce after the product export is built:

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql
$env:QA_ONBOARDING_REVIEW_BROWSER='1'
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic
```

## Next / remaining

- [ ] Mounted conversational send/clarification/current Projection against the
  existing one-shot gateway, with verified qualified-runtime readiness rather
  than treating interpreter presence as qualification.
- [ ] Actual SQL-backed completed-pending-turn recovery into mounted review.
- [ ] Terminal saved receipt reload/re-entry and durable start history/recovery.
- [ ] Independent CORE-01 Mini semantic/latency qualification; no engine inference
  or training was run here. Any genuinely changed shared contract uses the
  existing paired handoff, not a new engine or another product rewrite.
- [ ] Remaining families/roles, Manual full-page parity/devices, provider/security/
  privacy/enterprise/exact-release/approved production delivery gates.

Remain on **Sol High** for authority/concurrency/send integration; Medium for
settled bounded visuals. BUG-DATA-0001 OPEN.90% AI remains the product target.
No stage/commit/push/Azure, main/PENTA edits, developer DB migrations/reset,
credentials/provider/paid resources or default-model activation. Production NOT READY.
