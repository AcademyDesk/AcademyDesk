# E5-R21 — real SQL-backed mounted onboarding review

18 October2026. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`,
HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Existing enterprise/PENTA
continuation, not a new testing plan or completion of operational AI.

## Scope and boundaries

The E5-R20 mounted review had actual product browser evidence with synthetic API
responses. This batch pairs that existing exported page with the real application
HTTP pipeline, Identity and disposable run-owned SQL. The Node URL relay forwards
original authentication and actual host responses; it does not fabricate session,
draft, current snapshot, status, permissions or errors. Transport is a QA
loopback relay/TestServer, not production ingress/CORS qualification.

- Eight valid theme/viewport cases: Prism/Orbit at320/390/768/1440 pixels.
- Pending request, wrong selected draft, different actor, foreign academy,
  Teacher workspace and delivered local sign-out.
- Canvas has actual current host values, disabled controls, no usable approval or
  composer, scrollable final field and visible Close; Manual remains available.
- Parent harness checks unchanged students/guardians/links/invoices/payments,
  drafts/sessions/turns, users/memberships and academies/branches. Only the exact
  expected authenticated read audits may be added.
- Synthetic real bearer tokens transferred via child stdin only, never command
  line, console, screenshots or receipts. Only synthetic test names/data used.
- Browser relay accepts GET only. Fixture creation/selection/claim happens before
  the browser/no-write snapshot through normal protected host contracts.
- No interpreter injected into review factory, no inference/training/model
  qualification or approval/action activation. Developer flags/data untouched.

## Changes

- `QA/tools/SqlHarness/PentaOnboardingReviewBrowserRegression.cs` — additive,
  opt-in SQL/browser fixture, restricted relay and exact fresh SQL postconditions.
- `QA/tools/penta-onboarding-review-live.cjs` — actual product/Edge browser runner.
- Existing `OnboardingAtomicRegression.cs` adds one conditional extension after
  every retained assertion. Existing runner adds fail-closed opt-in/module/export
  preflight; unchanged migrations, limits, assertions and cleanup ownership.
- `apps/api/Intelligence/Penta/PentaOnboardingDraftService.cs` labels the draft
  view expiry's persisted UTC ticks with `DateTimeKind.Utc`. SQL datetime2 reload
  loses Kind; the old view serialized a timezone-less timestamp that E5-R20's
  strict browser adapter correctly withheld. No clock conversion, TTL extension,
  schema/data migration, client relaxation or permission change. Two fresh SQL
  fixture readbacks require explicitZ and exactly unchanged persisted UTC ticks.

## Final verification

- [x] Harness build, zero warnings/errors; JS syntax and scoped diff checks.
- [x]48 retained review contract/pin checks, evidence
  `QA/EVIDENCE/penta-onboarding-review-e5r21/review.json`.
- [x]265 focused .NET tests, zero failed/skipped. Existing onboarding/operational/
  shared student-service filter; not a rerun of every historical400-test selection
  or the complete API suite. TRX: `QA/EVIDENCE/penta-onboarding-review-e5r21/dotnet/e5r21-onboarding.trx`.
- [x] Final real SQL/browser run **`dfffe9ea2a7b4519978835660bbcf2c7`**, exit0,
  **138.1seconds**. All422 retained SQL cases plus14 additive browser scenarios
  PASS; two extra fresh SQL expiry readbacks preserve exact ticks/explicitZ.
- [x]29 exact own actor/tenant read audits:20 conversation +9 draft reads. Exact
  before/after domain/Identity/membership/academy/branch/draft/session/turn snapshots
  unchanged.45 actual browser GETs, no browser writes/429/unexpected console errors.
- [x]102 application/7 Identity migrations, scoped runtime login/context guards,
  mismatched cleanup marker rejection and exact database/login deletion PASS.
  Existing runner verified ownership then stopped/removed only the final run's
  SQL container; separate final inspection confirms it is absent. Two preliminary
  diagnostic containers remain stopped/retained, not pruned.
- [x] Viewed final390px Prism and1440px Orbit screenshots: long canvas scrolls
  while Close remains visible. This is headless viewport evidence, not physical
  Android/iOS/tablet or full portal visual acceptance.

Final receipt folder: `QA/EVIDENCE/penta-onboarding-review-live-dfffe9ea2a7b4519978835660bbcf2c7`.
Browser `result.json` records the14 journeys and actual HTTP statuses. Separate
`sql-postconditions.json` is written only after all parent SQL assertions pass.
The browser receipt alone does not claim the parent SQL postconditions.

| Artifact | SHA256 |
| --- | --- |
| Browser result.json | `2a2cbd783408152b1315bd6a236870ffba4d0c7c381729cab64c3d942a9e6277` |
| SQL postconditions.json | `2a59b593912aacc0cac028441616b98807dd3405699b256de1385a87f258a351` |
| Live browser source | `019c73a10562162f1edf4e1b682fd7ba609ad6d72aa58fd7eba048f250280c59` |
| SQL browser extension source | `2527adbba279d8953f22af6ee27d21f514822639f23f5ca0e97624c4639ead85` |
| Draft service source | `606e4fc07bd5f0385badddf69266c07cc1ff207fdd112bffb5be696a7797b3bc` |
| Final isolated harness DLL | `8777eb114aa9d06fe4164f1f4978992da0739a5daa890b1a9a34a428a8f087f4` |
| Final isolated API DLL | `ec834b4f50e0199fc1278f7d3dae1051c6d3f6f42db44154fdb7867d3182ef92` |

Reproduce from this worktree with the existing export/dependencies:

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql
$env:QA_ONBOARDING_REVIEW_BROWSER='1'
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic
```

The opt-in is test-process only. No .env/configuration/dependency/schema changes.
No frontend rebuild required by this backend-only format correction; previous
webpack export87/TypeScript/lint passes retained, not rerun. Existing default
Turbopack external-node_modules-junction blocker still OPEN. Normal developer API
DLL remains unchanged; the new candidate was built/tested in ignored isolated
artifacts. No developer database migration/seed/password reset or model activation.

Preliminary run `909811772ffa4d16af867c07c97aec55` passed the retained SQL groups
then stopped before the browser because the new fixture serialized protocol field
keys with Web JSON DTO casing. Corrected to a case-preserving dictionary, matching
the existing HTTP fixture pattern. Its compiled diagnostic lacked status output;
do not attribute that setup failure to application behaviour. Also corrected the
new fixture's expected claim admission to202 (not200), per the existing contract.
No retained assertion/permission/rate-limit change. Exact failed SQL container was
ownership-checked, stopped and retained; no pruning or evidence deletion.

Second run `cd09ac4fe86f4f1d929c169c8dfb9b0b` reached the actual browser after all
retained SQL groups. The first valid320px dark case received real200 account,
current and draft responses, then failed closed: `expiryHasExplicitZone:false`.
Bounded red receipt `QA/EVIDENCE/penta-onboarding-review-live-cd09ac4fe86f4f1d929c169c8dfb9b0b/failed.json`
and screenshot retained. This is the application defect fixed above, not a test
assertion to remove. Frontend source/export and all its strict checks unchanged.

## Remaining / next route

- [ ] Operational conversation session/turn/current Projection/approval/status
  composition; this screen is passive review only.
- [ ] CORE-01 semantic/latency/model qualification with approved profile/resources.
- [ ] Other workflow families/roles, full Manual visual/device parity, private
  providers, security/privacy/operations/enterprise/exact-release/Azure gates.

BUG-DATA-0001 stays OPEN.90% AI remains a target, not measured delivery.
Continue **Sol High** for authenticated operational integration; Medium is suitable
only for settled bounded visual work. No Mini contract/project switch required
for this host integration. No commit/push/Azure/model/resource activation.
