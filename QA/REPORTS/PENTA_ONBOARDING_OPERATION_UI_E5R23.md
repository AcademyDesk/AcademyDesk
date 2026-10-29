# E5-R23 — mounted exact-operation approval and saved result

18 October2026. Existing Academy Desk/PENTA continuation, worktree
`D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. No application rewrite.

## Delivered boundary

- The shared operational screen now connects a host-prepared onboarding draft to
  explicit **Review prepared operation**, the shared **Confirm exact operation**,
  a separate **Create approved student**, and the authoritative saved receipt.
  Approval is not creation. No success is shown before receipt/audit/source checks.
- Only an unchanged, authenticated review projection can construct the channel.
  WeakMap provenance is an ephemeral client guard, NOT domain authorization.
  Academy Desk independently reauthorizes every real HTTP operation using its
  existing current-authority, tenant, draft, revision, digest, proof, expiry,
  duplicate, transaction and mandatory-audit boundaries.
- Current credential/route/logout/abort guards, fixed endpoints, bounded JSON,
  exact request/operation/proof/field/scope checks and one-shot mutation latches.
  No automatic retry, token refresh, prompt replay, SQL/code execution, arbitrary
  tools or model-selected endpoint. A denied/unknown outcome hides private canvas
  state; explicit **Check saved result** reads the same operation only.
- If the server committed but the response is lost, lookup can recover the real
  receipt without another creation. Unknown confirmation does NOT become permission
  to execute. No student or guardian portal accounts are created by this workflow.
- Manual and independent read-chat remain available. Host-only responsive CSS
  wraps mobile header/toolbar controls and allows the workspace to scroll instead
  of compressing the composer beneath the new operation-status panel.
  Immutable vendored UI stays
  unchanged. Normal operational/gateway/model flags stay OFF. This mounts an
  inactive host adapter; it does NOT qualify Mini or enable customer AI writes.

## Verification

- [x] 44 new synthetic operation-channel checks (including late abort, logout,
  navigation, token replacement, parallel create, malformed/tampered scope/proof,
  no false receipt, lost execute and lost confirmation).
- [x] Retained 42 recovery, 72 channel, 48 review and 20 workspace-entry checks.
- [x] TypeScript and focused ESLint pass; webpack production export87 succeeds.
  Default Turbopack external-junction issue remains as previously documented.
- [x] Isolated SQL harness build passes with zero warnings/errors.
- [x] Final exact-source run `41159ce4926940f39eb9c9be9a636904`:422 retained
  SQL cases +15 passive +4 operational real-browser scenarios PASS, exit0/280.9s.
  Final parent SQL asserts55 exact actor/tenant audits,3 students/3 distinct
  execution audits,3 Succeeded drafts, tampered draft still Prepared and all
  unrelated snapshots unchanged.71 actual operation-page requests, zero429 or
  unexpected console errors; the deliberate failed delivery/409 are recorded.
- [x] Final22 synthetic product-review browser scenarios PASS against rebuilt
  export. Mobile header label/toolbar visibility assertions and manual screenshot
  inspection pass. This is not physical-device or all-portal visual acceptance.
- [x]102app/7Identity migrations only on exact owned disposable SQL; runner
  confirms owned database/login removal and stops/removes its exact container.
  Both successful run containers are absent; no other containers pruned.

The additive real-browser extension runs AFTER all retained422 SQL cases and15
passive real-SQL/browser journeys under the existing opt-in runner. It uses real
Identity, SQL and host responses with synthetic prepared fixtures; no interpreter.
Four new journeys: Prism320 creation, Orbit1440 creation, Orbit390 committed
response loss/read-only recovery, Prism768 tampered approval/no creation.
Independent parent SQL checks require exactly3 new students,3 succeeded drafts,
3 distinct execution audits, the tampered draft still Prepared, and55 exact
actor/tenant audits. Existing students, guardians, links, invoices, payments,
unrelated drafts, sessions, turns, accounts/roles, academies and branches must
remain byte-identical. No limiter override or weakened retained assertions.

## Source / binary pins

SHA256 (current R23 scope only; other dirty work is preserved):

| File | SHA256 |
| --- | --- |
| `apps/web/src/lib/penta-onboarding-operation.ts` | `11D2F4EA44C32B6C421720D20A15C0AFE573E67BB14A5775C96876641EAE03AF` |
| `apps/web/src/lib/penta-onboarding-review.ts` | `45E22A6F5EDD0D5CACF968768E4FEAD6BDEBA06812C30D05BE1BA4FBCDB8AEED` |
| `apps/web/src/components/penta/onboarding-operation-workspace.tsx` | `9D55B35864181FFAB217FCF4254AF75BD3C597C6B54872EFA69621F7491643ED` |
| `apps/web/src/components/penta/onboarding-review-workspace.tsx` | `82E4A80CB07A847904FCE5C256473B0AB80F38859E84258092DAC58E472B8BFC` |
| `apps/web/src/components/penta/onboarding-review.module.css` | `837447B4FAF22DC78A15C5B4B9130D0FB487E43F6BC5FA8B23CFBFC101B7617F` |
| `QA/tools/SqlHarness/PentaOnboardingOperationBrowserRegression.cs` | `7E0714E110F16F1210CDCD6B2E42B6AEDF145A05373B07C647685DF2EF6C9964` |
| `QA/tools/penta-onboarding-operation-live.cjs` | `18E5B6F57A2499E02E969FD14B257CC2BAC6029EB1AE4D6EFEFA032D8335A068` |
| `QA/tools/penta-onboarding-operation.test.cjs` | `C9EA04B2F969C666299EE08CDA761046AC4773C2D347905F7DA3B659B12B8E7D` |
| Isolated final `SqlHarness.dll` | `08952CAC9B7D2B76EEE107A62E801E27044E3025EE3CBB0135C62349491722EE` |
| Isolated API DLL (unchanged from R22) | `EC834B4F50E0199FC1278F7D3DAE1051C6D3F6F42DB44154FDB7867D3182EF92` |

`QA/EVIDENCE/penta-onboarding-operation-e5r23/contracts-final.json`:
`98237A6DCF98C85ABAC75631942683C954498EE4C0E0E8DCFAA0368B0D3CB5C4`,44/0.
Earlier41-case receipt retained. An initial command referenced nonexistent
`penta-entry.test.cjs`; corrected to existing `workspace-entry.test.cjs`,20/20.
No application defect or validation claim is inferred from that command typo.

Initial real run `dc80182819bf43f99bcfaa8d2c97392b` passed retained422 SQL,
15 passive and4 operational browser journeys,3 actual students/audits, exit0/288.5s,
102app/7Identity migrations and exact owned teardown. Its parent receipt showed
the expected55 audits; final harness adds an explicit exact-count assertion and
the rebuilt frontend additionally fixes screenshot-observed mobile wrapping.
Do not substitute this earlier run for final exact-source acceptance.

Final evidence (local/untracked):

| Artifact | SHA256 |
| --- | --- |
| `QA/EVIDENCE/penta-onboarding-operation-live-41159ce4926940f39eb9c9be9a636904/result.json` | `761154B78A2FB1B21AF4D1A1884A6C620C77D4AC866BF8DCDCD4BCF41543A73A` |
| Same directory `sql-postconditions.json` | `BA82569E872736CEE7A043CA645E70C9DF08BB8E6C24383CB05521138049D193` |
| `QA/EVIDENCE/penta-onboarding-review-live-41159ce4926940f39eb9c9be9a636904/result.json` | `FE6DBEB4D27F5130FFDBF8C2EE44F28640C675765014F265E951D4830DC6FB38` |
| Same review directory `sql-postconditions.json` | `FF9469EC576104B3870C7605137A2D0A9FFF56BBAAB982895DD678C3BC1F9E0E` |
| `QA/EVIDENCE/penta-onboarding-review-browser-1792341940122/result.json` | `E866DAF2B92ADB4527662CF116CB338BB5F429741AA137D2264FBCEF4240D73F` |

Parent postconditions, not the browser receipt alone, establish real SQL
acceptance. R21's265 focused .NET tests remain retained, NOT newly rerun; no
backend application source was changed in R23. Existing normal developer API DLL
is unchanged: `049BBA230504354EE8245F32A1FFAA01DC0218429B1F30D7F53BC2F21DEAA504`.
Local web3000 onboarding200/API5092 health200/anonymous session401; actual developer
Admin login remains unverified, no password reset or seeder used.

Reproduce locally (no production/developer SQL target):

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql
# Build the product first in apps/web: npm run build -- --webpack
$env:QA_ONBOARDING_REVIEW_BROWSER='1'
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic
```

## Remaining / next

- [ ] Actual SQL-backed completed-pending-turn recovery into the mounted review.
  E5-R22's unresolved202 case remains real; its completed branch is synthetic.
- [ ] Secure conversation start/session bootstrap and mounted send/current
  projection/edit/clarification flow, using the existing gateway/domain services.
- [ ] Durable full-page reload/re-entry after a committed operation. The current
  passive bootstrap intentionally refuses Succeeded; in-memory lost-response
  recovery is NOT a claim that terminal reload recovery is complete.
- [ ] Independent CORE-01 Mini semantic/latency qualification and approved runtime.
- [ ] Other operational families/roles, full Manual portal parity, physical
  devices, provider/privacy/security/enterprise/exact-release/production gates.

Remain on **Sol High** for the auth/approval/concurrency integration. Medium suits
settled bounded visuals. No Mini contract/project switch required for this host
slice. BUG-DATA-0001 remains OPEN.90% AI is a goal, not measured delivery.

No stage/commit/push/Azure, developer DB migration/reset, model/resource activation
or credential change. Main and PENTA project are untouched. Local evidence stays
untracked; build output stays ignored. Production is NOT READY.
