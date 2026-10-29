# E5-R17 — abandonment, deadline and late-result integrity

18 October 2026. Sol High. Continues E5-R16 and the existing enterprise program.
Academy `D:\AcademyDesk-codex-p0`, `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`; PENTA `D:\PENTA AI Models`, `codex/academy-language-contract`, HEAD `2f8806967709a1a7ec29e5176e7c31968adbf6aa`. Unrelated dirty work preserved; no main edit or publication.

## Reproduced defect and bounded correction

Four new Python regressions failed on the previous planner. Directly awaiting a
provider inside `asyncio.timeout` allowed a provider suppressing cancellation to
extend the qualified caller deadline/swallow caller cancellation. A later fault
could also arrive instead of the required bounded timeout/cancellation outcome.
Before receipt retained: `QA/EVIDENCE/penta-abandonment-20261018/python-before.xml`.

`QualifiedPlanner` now creates an owned task and awaits it through a shielded,
bounded caller. On cancellation/timeout it requests cancellation, allows one
cooperative scheduling turn, then exits the waiter without indefinitely awaiting
cleanup. Actual task exit releases its semaphore/admission; ignored cancellation
keeps capacity occupied. Late results/errors are observed/discarded. Bounded queue
admission and cancelled queued no-dispatch remain. Explicit orderly `drain()` keeps
unfinished work owned; it is not an automatic shutdown/recovery implementation.

No prompts, weights, model profile, decoder, qualification counts, capability,
permission, approval or wire shape changed. No model registration/activation.
This is **runtime safety**, not improved model reasoning/training/qualification.
No CPU-blocking event-loop/plugin/process-kill guarantee or production soak claim.

New Python6: timeout/caller cancellation crossed with late success/fault4; actual
private ASGI runtime timeout returns504/no second context read/no late plan1;
actual delayed loopback native-HTTP socket timeout/quarantine/no resend1. The last
uses a synthetic HTTP server and synthetic gate, **not a live native model or
Linux cross-process inference gate acceptance**. Native inference remains unrun.

## Accepted local evidence

- **319 selected Python tests PASS**, zero failed/skipped;313 retained+6 new.
  One existing Starlette/httpx deprecation warning. `python-final.xml` SHA256
  `21B17F251D6846EB4148517B764220AC2B661A07257AAA1854E4C5873E721B44`.
- **400 focused .NET tests PASS**, zero failed/skipped, using existing compiled
  source (`--no-build --no-restore`); no Academy application code change this batch.
  `abandonment.trx` SHA256 `54569D59288360BB5273BB2B6FD197F631C4F6BE0DA70FC943B4491D49EBDD01`.
- Receipts under `QA/EVIDENCE/penta-abandonment-20261018/`. Four-case before-failure
  SHA256 `15B5F7326A8E36A7E777BFCB614E5370A6DF0DA008BC173EAE45C4544A8AFF8E`;
  intermediate101-case focused pass also retained, not substituted for final319.
- Final isolated harness build PASS0 warnings/errors,13.54s. Harness DLL SHA256
  `CC064B47BA26D3FDDB803CE5267D73F1CC3EDEE54F5645496DC5CA515FC6B013`.
  Existing runner and production rate-limiter settings were not edited this batch.

## SQL acceptance — final source PASS

Final run **`73a4374dd98549759e12ae507f8bbda4`**, exit0/**121.1s**:
**422 cases PASS =412 retained +10 new abandonment checks**, no429.
102 application/7 Identity migrations, exact owned contexts/runtime SQL login
verified. Inventory355 routes/344 controller method-routes/10 framework Identity
method-routes, SHA256 `B3CF50C85842A23E0A2E9320604D251FD3FF267CC9CF606D58B67022DC19C19F`.
Original runner refused a mismatched cleanup marker, verified exact database/login
removal, then individually ownership-checked/stopped/removed only this final SQL
container. Python fixture child exited/removed; final resource absence checked
separately. Failed earlier diagnostic containers remain stopped/preserved.
Actual unchanged
30-second host timer is exercised; no test timeout shortening or fake provider
cancellation is used to represent that deadline.

New10 assert: both30-second outcomes unavailable/no draft revision change; both
unfinished slots retained/no third admission; cancelled private callbacks denied;
late valid partial and late fault cannot change SQL; previously unadmitted request
works after true release; direct real-SQL host caller-token cancellation stops its
waiter/leaves pending202; its slot remains occupied; private callback denies; late
partial does not change draft/claim; same abandoned request remains409/no replan.
Caller token is tested at the actual service/SQL boundary, not represented as a
physical device/browser disconnect. Existing HTTP cases/23-count assertion retained.

Six existing pinned Core recorded-port compatibility checks PASS on final run
receipts: context6/partial5/approval-result1/lifecycle5/preparation1-validation6/
claimed-partial7. These validate unchanged parser/reducer/SDK source, not model
semantics or the changed planner by themselves. Receipt SHA256 in that order:

```
7126af1c60716a533e303f4e84d050e93f600a923136c5d8667099e7fa83bda2
c562d69fbc54b4c1b52fed9a21eee20e50fab052c8793f564175ced687f643fc
04a3f306e39007ca218a4166bb57d100a45596137ce3b4c659e12e30dd22e2f2
b2814ab955a28947f2d7e2954bf91ee4a96069adf243edc59152c8224c244a9f
84916f79138d9a555b2ccbae658ea30bb95dacdffcc2a7416227f90c28fba7bb
15d2e3973facf3607ec9416d02296d87cdd3527b3a218b030c0dbcd83f13b54b
```

Runs `5c606cb87ec64bed8b1f97dcdafec1bd` and `4463d767d3334878b72155fb27bbf558`
**FAILED** after new10/private bridge cases at a later fixture save429, each
exit-532462766/128.1s. Neither accepted nor erased. Initial attribution to the
HTTP120/min budget was incorrect; a separate host did not resolve it. Source
inspection identified `SaveAsync`'s SQL20-drafts/actor/day guard: added fixtures
exhausted the original actor's allowance. Additive suite now uses a separate
synthetic actor created before unchanged domain/account snapshots, its own real
host/client and the same owned SQL/config/services/static two-slot gateway. No
actor/tenant quota or HTTP-limit increase/reset/disable,429-as-pass, weakened
assertion or operation retry. Original suite retains its actor/clients. Both
failed container names/run labels/loopback bindings verified, then stopped and
retained for diagnosis. Exit137 reflects these explicit stops, not evidence of
an OOM. No prune/delete. Final source differs from both failed attempts.

## Source boundaries and next

PENTA current planner SHA256 `81FFF8900DE94203CEE810058869A36E785B15102BDC8D40629DE0FF255B9472`, new tests `12CAF8AC67365A5D4072D18D56970638901CCE16B1C334D3E5B6245239AE0E47`. Additive manifest `operations/runtime-source-pins-e5r17.json` preserves prior planner hash `ad62bf89e8b0dfdf3dd9d3c5a025f92d285606802be2e106d58de13f665a30bf`. Candidate.5 archive/pins are historical, not a representation of this patched planner. Base runtime/native/contracts/reducer/decoder/prompt hashes verified unchanged.

Academy only `QA/tools/SqlHarness/PentaOnboardingGatewayRegression.cs` code changed; SHA256 `A8AB3A5B5F6950B645FE88B309D9DA658C2C32570E91491CDDA15D9D5194FF8C`. Application API DLL pair remains `049BBA230504354EE8245F32A1FFAA01DC0218429B1F30D7F53BC2F21DEAA504`, isolated API/harness copy pair `98AE006ACD97723867F64E2DC618826792F3681B30F60B7AE0EAA77AA00D7637`.

[Existing Mini qualification continuation](<D:/PENTA AI Models/operations/QUALIFICATION_NEXT_E5R17.md>) separates semantic failures, unrun rows, typed clear/reference errors and saved4B median36.537s/max68.619s against host30s. No unchanged trial repeats, canonical-score substitution, hidden retries, frozen expectation change, heldout training or unapproved resource/model activation. No new inference trial or model download this batch.

Next **Sol High**: actual semantic remediation/development-only candidate with unchanged frozen/fresh acceptance and appropriate authorized measured capacity; authenticated chat/review/approval/result composition may proceed with truthful unavailable states. CORE-01 model acceptance, actual engine/process/replica/ingress/soak, browser/device, other families/roles/languages, Manual parity/privacy/provider/enterprise/pre-Azure gates remain OPEN. BUG-DATA-0001 remains OPEN. Runtime tests do not establish90% conversational coverage or production readiness.

- [x] Reproduced/fixed ignoring-cancellation planner bug; before failure retained.
- [x] Actual host30-second timer/caller token/late-result/capacity SQL proof.
- [x] Retained earlier gateway/private transport and affected regressions.
- [x] Updated both handoffs and evidence-backed Mini qualification continuation.
- [ ] Semantic model qualification and authenticated integrated UI acceptance.
- [ ] Wider domain/role/device/provider/privacy/enterprise/release completion.

Local API5092/frontend3000 respond200 with every PENTA operational flag OFF.
Developer Admin sign-in remains unverified; no reset/reseed. No schema/dev DB/
frontend/production credentials/main/stage/commit/push/Azure/paid resources change.
QA evidence local/untracked, build ignored. **Production NOT READY.**
