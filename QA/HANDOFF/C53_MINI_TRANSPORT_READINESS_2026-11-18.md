# C53 — Mini application transport and integration readiness

MODEL: Sol High
TOOL: Cloud Codex
REPOSITORY: AcademyDesk/AcademyDesk
BRANCH: codex/cloud-c53-mini-transport-readiness-20261118
BASE: 4cbe56a76119ad257924d975c1bb2ecb97196022
STATUS: IMPLEMENTED / CLOUD VALIDATED — 18 November 2026 UTC

The owner assigned cloud the remaining application-side model readiness while
Desktop owns downloaded artifacts. Cloud claims exactly:

- `apps/api/Intelligence/Penta/PentaMiniProvider.cs` — local provider transport only.
- `tests/AcademyDesk.Api.Tests/PentaMiniProviderTests.cs` — correct HTTP fixture media type.
- `tests/AcademyDesk.Api.Tests/PentaMiniTransportBoundaryTests.cs` — new regressions.
- `QA/HANDOFF/C53_MINI_TRANSPORT_READINESS_2026-11-18.md` — this packet and readiness handoff.

Fresh Desktop tip4cbe56a7 and existing cloud claims were checked; checkout clean.
No other current edit overlaps these Mini transport paths. Desktop retains
protected-host controllers/domain services/SQL/migrations, physical runtime,
artifact checks, model execution and shared root trackers. This task does not
activate a model, alter DI registration, expand tools or remove environment gates.

## Concrete gap and intended behavior

`ReadinessAsync` calls `TryGetProperty` on arbitrary parsed JSON. A valid JSON
array/null/scalar throws InvalidOperationException instead of returning safe
Unavailable. The transport also accepts non-JSON media types and non-final2xx
responses as completed data. Reproduce with regression tests before editing.
Require completed HTTP200 JSON without unsupported content encoding. Keep the
current three-second readiness deadline, bounded16KiB parser/duplicate-key rule,
per-response runtime provenance for plans, no retries/fallback and loopback-only
Development/Testing transport. Preserve `{}` ->Degraded readiness behavior.

Test malformed status/header/body shapes, enabled/disabled gates, valid replies,
no retries, cancellation and per-response recovery. Run relevant existing PENTA
API/host tests and the existing no-model cross-language shared-contract check.
Document the exact current model/capability boundary and Desktop return evidence.
Preserve failures and source/lock/vendor/frozen-Mini files.

No main merge, Azure deployment, environment publication, public access change,
new package/model download, model loading, inference, training or activation.
Mini/Plus/Pro remain unqualified; Ultra deferred;90% target unmeasured. All six
critical acceptance records and the separate private Core credential gate remain.

## Completed transport repair

The new55-case regression suite first reproduced **28 FAIL /27 PASS** on the
unchanged adapter. It demonstrated non-object readiness exceptions, acceptance
of non-final2xx results, incorrect/missing media types, unsupported encodings and
missing explicit JSON/identity request headers. The original failure TRX/log is
retained; these were source defects, not artifact or laptop-connectivity failures.

The adapter now requires completed HTTP200. Both requests advertise JSON with
identity encoding. Responses require application/json with absent or UTF-8
charset and absent or a single identity content encoding. Compressed/ambiguous
representations, malformed/duplicate/oversized/deep JSON and array/null/scalar
readiness shapes safely return unavailable/schema failure without retry.
Normal readiness true/false/empty-object behavior remains Available/Degraded/
Degraded. A readiness result is operational availability only, not model approval,
weight attestation or authorization. The real Mini readiness endpoint does not
supply a runtime identity header; the existing per-plan response identity check
remains mandatory and is not silently replaced with readiness metadata.

The existing provider test transport was corrected from StringContent's default
text/plain to the actual Mini API's application/json. Its payload, authority,
provenance, no-retry and disabled-environment assertions remain intact. The new
suite explicitly tests rejection of text/plain/HTML, including valid JSON bodies.
No inference/model or application request schema changed.

### Fresh results

| Check | Result | Evidence scope |
| --- | --- | --- |
| Before repair, new transport suite | 27 PASS /28 FAIL | Preserved reproduction |
| Focused provider/provenance/private-bridge suite | 105 PASS /0 FAIL/SKIP | Includes55 new boundary cases |
| Broader current PENTA API/host tests | **785 PASS /0 FAIL/SKIP**,31s | Includes the105 focused cases; do not add twice |
| Existing Mini/Core/private-bridge suites | **190 PASS /0 FAIL/ERROR/SKIP** | Synthetic providers and owned loopback HTTP only; one dependency warning |
| Existing cross-language contract verifier | **10 PASS** | Typed payload hashes, scope/revision/receipt bindings; no inference imports |
| Locked tier planning metadata | PASS | All three selected pins, deferred Ultra, manifest/registry hashes, unapproved/unselectable controls |
| Final compilation and diff checks | PASS | Final broad build has no compiler warnings/errors |
| Actual production acceptance decision | BLOCKED /exit1 | All six critical records remain open |

The focused run's new nullable-test-helper compiler warning was corrected before
the broader build; no assertion was removed. The final785 includes all new tests.
No full frontend/backend/SQL/Mini semantic suite, new image/build/audit or physical
device acceptance is implied by these selected results.

```sh
/workspace/.tools/dotnet/dotnet test AcademyDesk.slnx --no-restore --nologo -m:2 -p:UseSharedCompilation=false --filter FullyQualifiedName~Penta --logger 'trx;LogFileName=c53-penta.trx' --results-directory /workspace/c53-mini-transport-readiness-20261118/penta-results
/workspace/PentaMini/.venv/bin/python QA/tools/check-penta-shared-contract.py --mini-root /workspace/PentaMini
# Run in the PentaMini checkout, without model/inference imports or downloads:
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=src .venv/bin/python -m pytest -q -p no:cacheprovider tests/test_operational_contract.py tests/test_operational_private_bridge.py tests/test_operational_runtime_bridge.py tests/test_protocol_bounds.py tests/test_tier_registry.py --basetemp=/workspace/c53-mini-transport-readiness-20261118/mini-temp --junitxml=/workspace/c53-mini-transport-readiness-20261118/mini-contracts.xml
```

## Cloud/Desktop ownership and exact readiness boundary

The owner's downloaded artifacts remain Desktop-owned. Cloud owns application
contracts, permitted capability integration, model-response validation,
availability/fallback/recovery and the cloud source/test return. Desktop keeps
artifact/runtime checks, separately authorized model execution, native protected
Identity/SQL/migrations, real storage/devices and local evaluation evidence.
This split is source ownership, not a claim of access to D: or the laptop GPU.

| Area | Current source-bound state | Next responsibility |
| --- | --- | --- |
| Mini target | Qwen3.5-2B Q4_0 at f6d5376be1edb4d416d56da11e5397a961aca8ae | Desktop verifies the original file/runtime; no cloud weight handling |
| Plus target | Ministral3 3B Instruct2512 Q4_K_M at eb599d408350ea2bb60452cb86be7c7b2fc28227 | Qualification target only; no customer application routing approved |
| Pro target | Qwen3.5-4B Q4_K_M at e87f176479d0855a907a41277aca2f8ee7a09523 | Qualification target only; no customer application routing approved |
| Ultra / reserves | Ultra deferred;9B internal only; Granite/Gemma/embedding reserves inactive | No automatic routing, escalation, fallback or simultaneous model loading |
| Mini application provider | Repaired strict local HTTP transport; Development/Testing plus both enable gates required | Desktop integrates C53 and validates the actual pinned server responses |
| First real model tool contract | Protocol0.1, only SearchLearners/GetLearner; current Owner/Admin academy Finance policy; selected IDs from current trusted results | Cloud retains strict contract; Desktop runs first separately authorized private evaluation through the real host/SQL |
| Current guided application experiences | Lookup, finance, scheduling/enrollment, Teacher attendance, portal and platform have source-bound deterministic/guided evidence | These do not establish qualified model-backed conversation or the90% target |
| Shared model-backed composer and broader capabilities | REMAINING cloud integration/qualification work; current guided FinanceChat is not a real Mini conversation binding | Use the versioned host contract and authoritative receipt/recovery path; never present guided mocks as model intelligence |
| Protected private Core interpreter | Existing typed private loopback lease plumbing, not a newly registered/activated production interpreter | Keep explicit gates/current context; no unapproved model or tool activation |
| Production / tier availability | Mini/Plus/Pro unqualified and nonselectable; Production/Staging provider gates remain closed | Requires accepted model, host, enterprise, device, staging and release evidence |

One Core and tenant-scoped Brain remain the architecture. Tier/model choice
cannot grant tenant/role/tool rights, bypass approvals or write SQL directly.
Runtime response identity remains response-reported source metadata, not a signed
model-file attestation. Current learning metadata remains training_eligible=false.
The cloud work is **prepared for the first pinned integration/evaluation step**;
it is not a declaration that every AI workflow or production gate is finished.

## Concrete Desktop return and following cloud work

1. Review/import this bounded C53 return without replaying C49/payroll or replacing
   current local work. Re-run the focused/broader PENTA selection on the integrated
   source and retain the repaired adapter's200/JSON/size/provenance behavior.
2. Preserve the locked Mini selection. Verify original weight/projector/tokenizer/
   engine/template/decoding and source pins on Desktop. Keep private archives and
   held-out prompts there; cloud does not need weights, raw private inputs or secrets.
3. The existing stop before model loading remains. A separately authorized private
   run must bind the actual selected model/runtime, Academy/Mini sources and
   independent fixture hashes. Record current tenant/role enforcement, tool and
   argument correctness, clarification/refusal/correction/follow-up, audit and real
   SQL read outcome, latency/timeouts, memory/concurrency and agreed resource/SLO
   budgets. Do not invent budgets, tune on exposed cases or run models together.
4. Return a sanitized result with those pins, explicit pass/fail/unrun counts,
   failures, confirmed host outcomes and restored resources. Raw prompts, bearer
   tokens, database settings and model files remain private. Guided/synthetic tests
   and reported runtime headers cannot substitute for model qualification.
5. Cloud continues actual model-backed composer wiring and versioned capability
   expansion from that verified contract. Broader finance/enrollment/scheduling/
   teacher/portal/platform requests need their own qualified model-to-host binding;
   the current two-tool Mini bridge is not silently expanded to cover them.

## Current hosted and release status

Desktop tip was fetched again before publication and remains
`4cbe56a76119ad257924d975c1bb2ecb97196022`, with no overlap in claimed files.
GitHub run [44065049153](https://github.com/AcademyDesk/AcademyDesk/actions/runs/44065049153)
has ordinary API/SQL/media/frontend SUCCESS and separate private Core transport
FAILURE. Its annotation still requires `PENTA_SOURCE_READ_TOKEN` with read-only
Contents access to private AcademyDesk/PentaMini. No secret/settings change or
workflow dispatch occurred. That older hosted success does not certify this new
C53 transport commit; exact publication checks are returned separately.

Release remains blocked on `BUG-DATA-0001`, `BUG-DATA-0002`, `BUG-DATA-0003`,
`BUG-DATA-0010`, `BUG-SEC-0001`, `BUG-AUTH-REVOCATION-001`, broader linked critical/
physical-device acceptance, staging configuration/restore/private Blob/rollback,
the required Core job and private-model qualification.90% remains unmeasured.

## Preservation and evidence

All1,814 unclaimed tracked base files are byte-exact, including16 vendor files.
All33 frozen Mini files match their original hashes and its checkout is clean at
14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8. No protected controller/domain/SQL/
migration/DI source, frontend, package/lock file or shared tracker was edited.
The original shared-contract receipt was copied before rerunning its verifier.
Prior feature branches and raw failure artifacts remain intact. Only the four
claimed feature files are published. No main merge, Azure/environment publication,
public-access change, model download/loading/training/inference/activation.

Private raw directory: `/workspace/c53-mini-transport-readiness-20261118`.
The final user return provides the verified remote publication SHA. Next route:
Sol High for application authority/contract integration and Desktop for the exact
private runtime/native qualification prerequisites above.

| Receipt relative to the private raw directory | SHA256 |
| --- | --- |
| `before-tests.log` | `6a76dd8aff2d60fcab8697424e59769c1f21b956bc9f9e1b3f17aaf2f51d4e97` |
| `before-results/c53-before.trx` | `749cc3b05efa05465d6ab1ab5a61785fdaa6cb1831832e03da4b826d6613fc19` |
| `focused-tests.log` | `d9057814438cfa60edbab4aaa37f030933ae49456a168823c88353ca770d3473` |
| `penta-tests.log` | `1ccf4b86ecbe57f72bd650ab970bf25c7455cc3fac39d016977872eea76f466b` |
| `penta-results/c53-penta.trx` | `1e9a833fb2186c0e4b3652a8504295cb1fbc0f345dbdf731e3db2a7db026af23` |
| `mini-contracts.log` | `d2b4baf3ffaac61c254ee9103770625567ba3dc3f0eb3357ac1017cd41ee1f97` |
| `mini-contracts.xml` | `1001e7f5bfd854e208a499b7694c6de52d3b1e9a3e3d3ca6cba606902f4a9e12` |
| `shared-contract-result.json` | `2d4f9416a50346291e2a5892df45fc5c890b9a033a22aa817025dbb5eb45acaf` |
| `test-summary.json` | `a3e61e51659dd6aee9b3d0e63dc3b0e57a48144f58ed4556ad4b9406dae7fa91` |
| `locked-plan-check.json` | `88e6473c9ec87f3048063aae7b868e9ce2089af2d7ea203d3aac9ace53dbb0df` |
| `source-integrity.json` | `2e4c622156323767b99afcdc20f5563ce0476b92ee3b2325e927a295e22510e1` |
| `desktop-ci-jobs.json` | `55169fc0a1d6680e3b80361ccaaea606045064148814910793c8683b4e501431` |
| `production-gates.log` | `d7342475f63187e45a79e0a482851f5fe6345953f2878dc8851c6244c6b959e6` |
