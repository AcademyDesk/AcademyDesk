# E5-R58 — corrected 3B intent comparison

20 October 2026. Route: Sol High. Continues the existing qualification program.

Follow-up: [R59inactive two-stage composition](PENTA_TWO_STAGE_PROVIDER_E5R59.md)
is now implemented/tested offline. Fresh full-plan and actual runtime qualification
below remain OPEN; the intent-only score here is unchanged.

## Final result — 11/12 intents, NOT ACCEPTED

Owner requested continuation after the exact source-proven template correction.
One bounded run `e5r58-cb3a0c41508d` completed in107.12s, process exit0.
Twelve model requests completed,11exact/1semantic mismatch/0unrun. All finished with
`stop`; maximum15 completion tokens within64cap. Median request2.036s/max3.302s.
Independent strict parsing/rescore of every raw reply against unchanged frozen expected
intents confirms11/12. `classification_only_passed=false`, decision `NOT_ACCEPTED`.
No full plans or host operations were generated/executed. No active trial remains.

Original25containers/config/host/mount/state exactly restored, original services healthy,
four idle slots. All57frozen staged source hashes and R56source pins reverified unchanged.
Only this run's owned temporary containers removed; no original containers, volumes,
images, cached models or QA/EVIDENCE deleted. No restart, automatic retry or pruning.

| Case | Expected intent | Actual intent |
| --- | --- | --- |
| IE-01 | replace | replace |
| IE-02 | refine | refine |
| IE-03 | refine | refine |
| IE-04 | replace | replace |
| IE-05 | pause | pause |
| IE-06 | resume | resume |
| IE-07 | cancel | cancel |
| IE-08 | status | status |
| IE-09 | clarify | refine |
| IE-10 | unsupported | unsupported |
| IE-11 | refine | refine |
| IE-12 | refine | refine |

IE-09 asks for an unspecified northern zone while the trusted catalog offers only
`East practice zone`. The model should request clarification, not choose an update intent.
This is a classifier error; no reference value or final plan was generated, so it is
not evidence that a host mutation occurred or that full-plan validation was exercised.
Keep the failure as an exposed regression, not training data or a prompt-tuning target.

Compared with2B's5/12 on the same frozen cases/prompt/schema/scoring,3B scored11/12.
Model/tokenizer/template differ, so do not isolate a cause or generalize to workflow,
language, security or production accuracy. All3Bprovider fingerprints differ from2B.
No automatic tier/default/model substitution follows this small diagnostic.

Evidence retained under `D:/PENTA AI Models/operations/evidence/e5r58-cb3a0c41508d`:

- `trial.json` SHA256 `989d9d9e4316118071a8c7448b5d413431d4b1c1dfdd9a9b825544e8c3314a91`
- `ministral3-3b/evaluation.json` SHA256 `5dcaf500f41b7d507a5718ff2c56f1c69997d0e36fd7adfa2e3dd9050b603b02`
- `gpu-runtime.json` SHA256 `428f7ad3350393b5d2274de749d749fd109726ef09573861d42f260ba8f4cd6a`

The runtime receipt inherits the R57 diagnostic label from the reused helper; its
location, trial/model hashes and E58 source-verifier receipts identify the E58 run.
Do not rewrite frozen receipts to change that cosmetic label.

R57 failure/source diagnosis: [preceding report](PENTA_3B_INTENT_COMPARISON_E5R57.md).
Prepared verifier/launch related suite: 252 PASS/9.83s, one existing deprecation warning.
No repeat of the enterprise suite is needed to collect this diagnostic.

## Frozen scope

Ministral-3-3B-Instruct-2512 Q4_K_M, 2147023008 bytes, rehashed to
`9ed150d4367e68df0ac8e1540f6ddc65b42d0ee26378329d1ecbca60f93fc5f8`.
Same verified native75 binary and pinned source `11fe02151f79c41d0d4af7da708755d73b9c0da6`;
no build, download, template override or model-output repair.
Accept only the exact source-proven Mistral guard substitution and one final LF removal;
actual server template must hash to `8a292f27fb149a8699d1f56623407437c6212897b26c96e58c877b29545f6142`.
Actual process candidate CUDA mapping, full offload and 128/128 batch are checked before generation.

Unchanged host admission 4026071200 bytes (three consecutive samples within45s),
GPU free3583.560699 MiB on4096 MiB T1000, context2048, four threads/one slot,
64 output tokens, request30s/load150s/evaluation480s/lifecycle900s caps.
No CPU fallback, relaxed reserves or model/default activation.

Same R56 twelve now-exposed synthetic English intent cases, same instruction/schema/evaluator/scoring.
Prior2B classification5/12 is the diagnostic comparator. This is not blinded fresh acceptance,
full-plan/tool execution, multilingual/security/portal testing, training data or production readiness.

## Completed verification and next path

- [x] Collect actual runtime and per-case receipts; strict raw-response rescore11/12.
- [x] Verify original25/container/config/host/mount/state and four-idle-slot restoration.
- [x] Verify all57frozen stage hashes and unchanged R56 source pins.
- [x] Verify actual source-transformed runtime template, candidate CUDA library through
  `/proc/1/maps`, full27/27offload, actual128/128batch, and outgoing schema on all12dispatches.
- [x] All12token preflights fit:419–503input tokens plus64output≤2048context.
- [x] Implemented atR59: an **inactive** bounded intent→draft-plan composition
  using the existing restricted catalog schema and mandatory reducer, with a shared
  30s deadline, maximum2calls/64+448output budget and truthful failure/quarantine handling.
  Preserve unavailable-reference/clarification failure coverage; no keyword/oracle repair.
- [ ] Author independent fresh ambiguity/transition/field-preservation/security cases
  before inference. Existing12now-exposed cases remain regressions, never a training corpus.
- [ ] Run separately bounded actual full-plan evaluation after offline composition tests;
  do not present intent accuracy as field extraction, edit correctness or approved execution.
- [ ] Return a qualified engine through Academy host/SQL/API/role/approval/audit/browser
  integration, Manual fallback and remaining enterprise/device/pre-Azure gates.

The prepared252focused tests from the preceding batch remain valid; no production source
or evaluator changed in this run, so that suite and the enterprise suite were not repeated.
Actual native load/schema/template/token/response/restoration evidence was collected now.

No application/domain/database/main/Git stage/commit/push/Azure changes.
Existing full-plan/host/app/Manual/role/device/enterprise/pre-Azure gates and BUG-DATA-0001 remain OPEN.
