# E5-R22 — explicit same-request onboarding recovery

18 October2026. Existing enterprise/PENTA continuation on `codex/penta-search`,
worktree `D:\AcademyDesk-codex-p0`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.
E5-R21 real SQL/browser UTC fix retained. No new engine or application rewrite.

## Delivered boundary

- Pending review now offers **Check pending request** only after authenticated
  account/current checks provide the exact pending request. No URL/model/storage
  request identifier is adopted; captured binding remains only in memory.
- Explicit GET current → GET same request result.202 remains pending, never a
  success, cancellation or invitation to submit again. No POST/retry/poll/refresh
  token replay, model inference or domain operation.
- A200 draft-only completion is checked against the existing turn-summary
  verifier, fresh account/current/draft/digest/current passive Projection and one
  final same-version current cursor. Raw result fields are never rendered.
  Scope/tool/fields/catalogue/revision drift is withheld. Even an accepted result
  yields read-only review, not a student-created claim, receipt or approval grant.
- Same-tick check locking, current credential/route/abort fences and immediate
  private-state clearing on delivered logout/token change/unmount remain.
- Immutable vendor UI and all disabled send/edit/confirm/model controls retained.
  Manual/read-chat links remain. No feature flag, backend route, DI, model/profile,
  schema, developer data or permission activation.

## Files

- Host review adapter/component add the recovery closure and explicit UI.
- Existing channel exports its unchanged result-summary validation with a smaller
  result-only binding type; one-shot POST admission semantics unchanged.
- Additive42-case recovery test; retained72 channel and48 review checks kept.
- Synthetic product browser extends the retained18 scenarios with pending202,
  completed draft review, current403 and malformed-result withholding.
- Existing opt-in real SQL/browser harness adds a15th pending check journey after
  the retained14. Exact expected read audits rise29→32:22 current,9 draft,1 result.
  Domain/Identity/memberships/operational/academy/branch snapshots still unchanged;
  no inference configured, all prior422 SQL cases retained.

## Validation

- [x]42 new synthetic recovery checks: pending/completed, same-ID/no-write, binding,
  scope/catalogue/field/revision drift, false success, malformed202, HTTP denials/
  outages/no retry, token change, abort and double-click admission.
- [x] TypeScript/focused ESLint/webpack export87, harness build zero warnings/errors.
- [x]20 retained workspace-entry/route-ledger checks.
- [x] Mounted synthetic browser22 scenarios, including18 retained. Receipt:
  `QA/EVIDENCE/penta-onboarding-review-browser-1792339422394/result.json`.
  New paths: pending202 four GETs, completed passive review nine GETs, current403
  three GETs, malformed same-request result four GETs; zero API writes/unexpected
  console errors. Final pending390px screenshot inspected.
- [x] Real disposable SQL/browser15 scenarios +422 retained SQL /exact teardown.
  Final run **`98e7e2e468ca4733944dfe222b9a4c2c`**, exit0/**252.5seconds**.
  49 actual browser GETs/zero writes/429/unexpected console errors.32 exact current
  actor/tenant read audits (22 current +9 draft +1 pending-result), exact unchanged
  domain/Identity/membership/academy/branch/draft/session/turn snapshots.
- [x]102 application/7 Identity migrations/scoped runtime login and exact owned
  database/login/container cleanup; mismatched ownership marker refused. Separate
  inspection confirms successful container absence, failed diagnostic container
  stopped/retained. No other containers pruned or removed.

Contract tests are not SQL/browser/model acceptance. E5-R21's14-case accepted receipt remains retained
at its original run folder; this successor does not overwrite it.

Final real-browser/SQL receipts:
`QA/EVIDENCE/penta-onboarding-review-live-98e7e2e468ca4733944dfe222b9a4c2c/result.json`
and `sql-postconditions.json`. The latter is written only after all exact parent
SQL postconditions pass; the browser result alone does not claim SQL acceptance.
Existing isolated API source/DLL from E5-R21 unchanged, no .NET suite rerun in
E5-R22; E5-R21's265 focused .NET PASS is retained, not counted as newly executed.
No default Turbopack rebuild; existing external-node_modules-junction build
blocker remains OPEN. Accepted webpack export uses existing dependencies.

Preliminary SQL run `71b2fb5266d0414f854f38c0863f65ad` passed all retained SQL
groups and14 retained browser scenarios, then correctly failed the new pending
check: the QA-only GET relay omitted `/claims/{request}/result` and returned405
before the request reached the application. Added that exact GUID-result GET
pattern, not a write or production route/permission change. Original no-error/
pending-status assertions unchanged. Red receipt/screenshot retained under
`QA/EVIDENCE/penta-onboarding-review-live-71b2fb5266d0414f854f38c0863f65ad/`;
exact failed container ownership-checked, stopped and retained, not pruned.

Contract receipts in `QA/EVIDENCE/penta-onboarding-recovery-e5r22/`:
`contracts-final.json`42; `channel.json`72; `review.json`48. Initial41-check receipt
is retained; extra selected-cursor/202 inconsistency rejection added before final42.

| Artifact | SHA256 |
| --- | --- |
| Final recovery contracts | `0cebaed926d7ea1335dfe36654ed85038e5c42beb8918430d73d39b9dbe29ae1` |
| Synthetic browser result | `e866daf2b92adb4527662cf116cb338bb5f429741aa137d2264fbcef4240d73f` |
| Review adapter source | `ff744a5d088840f49c819593686b402dfe6360b6c4f31a84683d35836ae4201e` |
| One-shot/result channel source | `bb4fefbe6ca8f648eefa6c6add3d321bafbf322d200e6c61e271e6a02d262326` |
| Mounted host component source | `9459f2b009c82510dbd97e76fedfad70038e389ed29c61022c5f1adee011a129` |
| Real browser result | `c3605f8d11bedd14a69fd7c138289664cfbbe0090d446258f9529a42b91625e3` |
| SQL postconditions | `e06b2bf7b96010aea7ca5eac0ef2b7f342242fe387a6549d12a398750bee4a83` |
| SQL extension source | `21dfdd9ca8d5bd75fb7d2c9541201d5bdf9a4d1cd9e4441eb599e0b444d26ad7` |
| Live browser source | `f8f65a7f758ab926ee592e119f55608bb580d84149560121ea92044ce3029810` |
| Final harness DLL | `3d1f76df84381642114ca9fc56e6d5e2dcda1d324b674e7ff62b2c32833e4f67` |

Reproduce with existing tooling/export and the same run-owned SQL runner:

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql
$env:QA_ONBOARDING_REVIEW_BROWSER='1'
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic
```

## Next / still open

- [ ] Operational bootstrap/conversation start and available-runtime evidence,
  send/current Projection, complete validation/approval/challenge/execution/status
  UI composition; no claim that this recovery slice enables live AI writes.
- [ ] Real SQL-backed completed-turn recovery while a browser is waiting. The new
  SQL journey proves an unresolved same-request202; completed recovery's22-browser
  extension uses synthetic API responses. Existing actual completion API/SQL
  regressions are retained, not mislabelled as this combined browser journey.
- [ ] CORE-01 semantic/latency qualification with approved profile/resources.
- [ ] Wider roles/families, all-screen Manual/light/dark/physical devices,
  provider/privacy/security/enterprise/exact-release/Azure gates.

Remain on **Sol High** for auth/concurrency/approval integration. Medium for settled
bounded visuals only. No Mini contract change/project switch needed this slice.
BUG-DATA-0001 remains OPEN;90% AI is a target, not delivered coverage.
No stage/commit/push/Azure/production changes. Evidence local/build ignored.

Final local smoke: existing web3000 `/penta/onboarding`200, API5092 `/health`200,
anonymous `/api/auth/session`401. Actual developer Admin login still unverified;
operational gates remain OFF. Normal developer API DLL hash unchanged
`049bba230504354ee8245f32a1ffaa01dc0218429b1f30d7f53bc2f21deaa504`.
Working HEAD/branch unchanged, staging empty, zero tracked QA/EVIDENCE files,
`.build-check` still ignored. Full enterprise testing and production NOT complete.
