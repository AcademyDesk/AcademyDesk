# E5-R16 — paired private context and loopback transport

18 October 2026. Sol High. Continues E5-R15 and the existing enterprise program.
Academy `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`; PENTA `D:\PENTA AI Models`, `codex/academy-language-contract`, HEAD `2f8806967709a1a7ec29e5176e7c31968adbf6aa`. Existing dirty work preserved.

## Delivered, inactive

- Additive `penta-private-loopback-1` uses the actual unchanged candidate.5 runtime/planner/reducer. No model/protocol0.1/schema/dependency/weight/prompt replacement. No production interpreter or lease-registry DI registration, global private app, environment secret loader or automatic activation.
- Fixed literal loopback HTTP origins with explicit ports only. No remote DNS, proxy, redirect, compressed response or certificate-validation exception. This local candidate is **not production TLS/workload identity/network-isolation/credential-rotation acceptance**. Cross-host/Azure deployment needs its own reviewed secure transport and release gates.
- Separate composition-owned random256-bit channel credential and per-request random capability, exact version and closed existing prompt body. Credentials do not enter model prompts, client responses, command arguments or source. No model/client roles/tenant/catalog/provider/callback URL are accepted as authority.
- Fixed private current callback binds capability, request ID and SHA256 of exact raw POST bytes. The digest binds the turn; it is not authentication. Callback uses a new DI scope and Serializable SQL/linked Identity transaction to recheck actor/role/academy, session/context/authority, exact pending request/original protected prompt digest, current draft/revision/hash/expiry and full catalogue, then persists required read audit. No DbContext escapes the request; no SQL lock is held during interpretation. Ordinary browser current still withholds planning context while pending.
- New private controller remains unavailable without Testing, all existing gates, separate private-bridge gate and explicit lease registry. `[AllowAnonymous]` avoids ERP bearer handling so the **private capability** can be checked instead; ordinary authenticated user tokens cannot resolve context. Invalid/missing/duplicate headers, query, request/body mismatch, closed/expired/cancelled leases deny.
- Leases permit two serialized reads within30 seconds and are removed on interpreter exit. Two host slots/no-replan remain. Core retains its existing before/after checks; host independently rechecks before completion. Per-worker hashed-lease replay cache is bounded128 entries/60 seconds, not a distributed ledger/quota.
- Callback HTTP bound5 seconds/262144 bytes; .NET interpretation response bound65536 bytes, strict version/media type and host decision validation. Failure never grants approval, executes a domain operation, retries or selects a fallback. Known outages finish no-change; changed context retains the durable pending claim. No second brain/draft store.

PENTA return: `D:\PENTA AI Models\operations\PRIVATE_BRIDGE_E5R16.md`; additive source pins `operations/private-bridge-source-pins-e5r16.json`. Both project handoffs updated. Candidate.5 archive/manifest remains historical, not silently repackaged. No external message/publication.

## Final validation

- **400 focused .NET tests PASS**, zero failed/skipped:380 retained +20 new. Existing PENTA/shared-onboarding/audit/model-snapshot filter. Receipt `QA/EVIDENCE/penta-private-bridge-20261018/private-bridge-final.trx`; SHA256 `042514DE5E553B870F959F81E5415ACC4F440B1106801E9B83C1DFC05A9BA87D`.
- **313 selected PENTA Python tests PASS**, zero failed/skipped:282 retained selected operational tests +31 new. One existing Starlette/httpx deprecation warning. Receipt `QA/EVIDENCE/penta-private-bridge-20261018/python-regression-final.xml`; SHA256 `DFCF622EEAC79A2E2A0624761895D8BD844856C11E05C54DCE281FC3624C7A3E`. Historical606-test receipt retained, not replaced by this selected run.
- Isolated harness build PASS0 warnings/errors,67.68s: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/onboarding-atomic-sql`.
- Original runner unchanged: `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`.
- **412 named real SQL cases PASS =407 retained +5 new paired socket cases**. Final run `65ad36112d19495bb7c628506edf6feb`, exit0/94.9s,102app/7Identity migrations. Exact owned contexts/runtime login verified. Inventory355 routes/344 controller method-routes/10 framework Identity method-routes, SHA256 `B3CF50C85842A23E0A2E9320604D251FD3FF267CC9CF606D58B67022DC19C19F`.

New5: actual .NET -> Python private factory/planner/reducer -> fixed host callback/controller -> real SQL with two current checks; ERP bearer denied as private lease; unauthenticated Python socket denied before context; second-read catalogue drift withholds plan/leaves draft unchanged/pending retained; owned Python-process outage produces unavailable without retry/domain write. Existing23 gateway cases and previous domain/account snapshots retained. No429 counted as a guarded race.

**Scripted provider with explicitly synthetic qualification only**. No native/model skill or accepted qualification claim. Tests import actual PENTA code, not a replacement interpreter implementation. Actual caller-abort/30-second wall deadline/ignored-cancellation/native late-result/soak acceptance remains open.

Initial compiler errors (missing string-protection namespace and Guid/string fixture argument) corrected; no tests weakened. Normal build encountered owned API file lock; only exact verified worktree API stopped/restored. Earlier pre-body-binding SQL run `556ab3fc70e94cf7b3550a17e553f5b0` passed97.3s but is superseded by final source. Both runs' SQL containers individually removed by original ownership-checked runner; final database/login teardown confirmed, mismatched-marker cleanup refused. Final container absence checked separately; no prune/other containers/evidence deletion.

API/test DLL pair `049BBA230504354EE8245F32A1FFAA01DC0218429B1F30D7F53BC2F21DEAA504`; isolated API/harness copy pair `98AE006ACD97723867F64E2DC618826792F3681B30F60B7AE0EAA77AA00D7637`. Two build-location pairs, not four identical binaries.

Six retained actual pinned Core recorded-port checks PASS on final run: context6, partial5, approval/result1, lifecycle5, preparation1/validation6, claimed partial7. They remain recorded-port compatibility, separate from new socket evidence. Receipt hashes in that order:

```
d3b5bd61ed9d910bc907a3ed317e4996afb0ee0f29344216cefcdbfadc3e8fa4
d6cc54aa55813d22a1bd4178587182d023ff436fadd5d8b731d62b484ad2c5eb
9f309bf6a2434f0715391dc9baf2df3f30e8337e29ed53411079d2afbc1e0b69
877b07a61b4cda39f580319806478b581a3b4f0b7b3fde757123bed14d53a4ec
4b3ad877c7463757c35cefc9da46c60e1a1d9a0ab530908ef6276dfa547baeb4
832d2e7b2cf1583574636e35e3a9dd1574589f8ad23151bf0fd700a6acdeff5f
```

## Exact Academy source pins

| File | SHA256 |
| --- | --- |
|apps/api/Intelligence/Penta/PentaPrivateLoopbackBridge.cs|0FBCB5733256F5CF047432A47C164D863E6E6CD7170E7C87B39CC8CFFB783BF3|
|apps/api/Intelligence/Penta/PentaOnboardingClaimedContext.cs|F363506D8FFB6209485F6816207140DA10BD588228B63BDA953F4ABEA919FF0F|
|apps/api/Controllers/PentaPrivateContextController.cs|E088440E035BA98A4B969BDD9C5BFB3DB814C49036D0556A9AA722B122E54E80|
|apps/api/Intelligence/Penta/PentaOnboardingGateway.cs|43EF772471FCEB77A8B7C332DE30901A24CFC6846DCA8CBB0F9BB4CC79722B28|
|apps/api/Intelligence/Penta/PentaOnboardingDraftService.cs|C503B3540BF61D41E7CAF6352D54A29352F5534480AA7B67E9943CE01FD5FF0E|
|tests/AcademyDesk.Api.Tests/Infrastructure/QaApiFactory.cs|A42524933E25B4047D8112A4336BF76A02CC909FEBA7F01C43D4C35A2F95DFED|
|tests/AcademyDesk.Api.Tests/PentaPrivateBridgeTests.cs|08F203C401C9849E9D2A38C32BC548EAB89F44FEE8E068FB808761BDB996DD32|
|QA/tools/SqlHarness/PentaPrivateBridgeFixture.cs|186C04F9D7E178967EE70B49AF678CA6B175FC34B1D3D1ADEAAF825B20F11B94|
|QA/tools/SqlHarness/PentaOnboardingGatewayRegression.cs|19FBC40F66234EC01F816F10A9C2942608F310F75702DDBDDD2D0C0328D75EED|
|QA/tools/penta-private-bridge-fixture.py|648838F34E4DA6A8A0F8FAB385C506CDFCC474EC4557E3AB2D254B50FADEED96|

## Checklist / next

- [x] Previous shared domain/draft/approval/execution/admission/turn/gateway checkpoints preserved.
- [x] Inactive paired private context + actual loopback socket/SQL proof; both handoffs updated.
- [ ] **Next Sol High:** actual cancellation/deadline/late-result qualification and measured CORE-01 remediation. Preserve failed2B86/94,4B83/94 and partial8/15/47unrun; no unchanged trial repetition, weakened expectations, resource increase/model activation implied.
- [ ] Controlled authenticated chat/review/approval/result UI and exact qualified interpreter linked journeys/browser/device acceptance. This bridge is bounded English Executor onboarding, not every language/capability/domain.
- [ ] Other AI-01–14 role/domain families, Manual visual parity, providers/privacy/retention/keys/operations/enterprise/pre-Azure gates OPEN. BUG-DATA-0001 browser/device gate OPEN. Production NOT READY.

API session63411 localhost5092 health200; frontend3000 login-page200. All operational flags OFF including private bridge; process-only Testing/local SQL, migrations/bootstrap/media/PENTA/Mini OFF. Actual developer Admin sign-in unverified; no reset/reseed. No developer schema/DB/frontend/main/production credentials/stage/commit/push/merge/Azure/model/training/paid resources change. Primary main HEAD remains `f7af512f884ca5fe8ac3ddb287c852492592e709`. Evidence local/untracked, build ignored, unrelated work preserved.
