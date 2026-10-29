# E5-R31 — changed compact-profile2B trial

19 October2026. Continued owner-requested local model evaluation after E5-R30.
**NOT ACCEPTED.** No model/host/production gate is closed.
Academy `codex/penta-search` HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`;
Mini `codex/academy-language-contract` HEAD `2f8806967709a1a7ec29e5176e7c31968adbf6aa`.
Unrelated dirty work preserved; no stage/commit/push/main/Azure.

## Exact scope / implemented trial wiring

One local Qwen3.5-2B Q4_0 trial of the genuinely changed, inactive
`transitions-compact-v3` instruction, SHA256
`807825ebc094a9ae006cddd6e21268cd3eade02e1eae19bb8e19eab0410bae27`.
Same artifact, native image,2048context/512output/2CPU/one slot/30s bound, actual
memory/idle-baseline/labeled-resource cleanup and exact scoring as R29. No
resource increase, GPU/runtime download, training or paid compute.

Added explicit evaluator/runner wrappers reusing the actual R29 fixtures, scoring,
native provider and lifecycle. Existing runner now has code-owned profile/pins/
evaluator/run-prefix constants, retaining the original defaults and all existing
assertions. A trusted wrapper pins compact-v3 and2B only. No browser/model input
can choose these values, alter resources, spoof approval or select a tier.
New preflight also requires exact reported profile as well as prompt hash.

Original R29 staged source/receipt is immutable and still retained. The current
reusable runner's bytes changed; the old R29 full source hash is historical, not
claimed to match current source. Existing E5-R30 compact engine/profile source
pins remain unchanged; no new prompt edits after this trial's answers.

## Actual result

Run **`e5r31-3a4f0760a921`**, observed top-level **exit0**, recorded147.29seconds,
within the unchanged60minute lifecycle ceiling. Runner exit0 means safely
recorded/restored, not qualification success.

- All72actual template/tokenizer checks fit: largest837input+512output≤2048.
- Exactly **1generation request**, fresh-now-exposed regression F29-01.
- Native request returned no complete raw answer at **30.053s**; actual gate
  quarantined and dispatch stopped. **0complete answers,0exact,71unrun**.
- Remaining cases are unrun, not71model failures or successful refusals. There
  is no valid full-suite accuracy score and no measured accuracy improvement.
- Actual native log: about16.51s prompt-processing for656tokens; at28.82s from
  dispatch,100tokens generated/~8.19tokens/s; cancellation around30s. These are
  bounded log observations, not a complete-response latency or GPU measurement.
- This does not establish compact-v3 is semantically better/worse or faster/slower
  than v2: one censored request and different live machine conditions cannot
  support such ranking. The smaller input alone did not produce a timely answer.

No Academy SQL/domain action, actual conversation, customer data, approval or
student creation occurred. The original read86/94 failure and R29 semantic errors
remain OPEN. No token/timeout expectation was relaxed to obtain a pass.

## Verification / restoration

- [x]382selected Python tests PASS (378retained+4new wrapper/default-boundary
  checks), actualJUnit14.44s/0failures/errors/skips; inherited Starlette/httpx warning.
- [x] Exact model size/hash verified; guard3093922048bytes, final3sample minimum
 4700209152bytes before loading. No guard reduction or app termination.
- [x]50staged source files frozen before output and independently checked after;
  original container IDs/images/config/running-state inventory restored. Native
  healthy/4idle slots; exact labeled trial resources removed, logs retained.
- [x] Original developer Academy frontend/API left running throughout; login200/
  health200 observed during trial. Final smoke recorded separately. No rebuild,
  migration/seeding/bootstrap/password/default-PENTA activation.
- [x] Prior461SQL/API+29browser acceptance retained NOT rerun for a model-only
  trial. Diff checks/source hashes/zero staged files; no enterprise closure.

Mini evidence: `operations/evidence/e5r31-3a4f0760a921/`: `frozen-plan.json`,
`trial.json`,50file stage, model config/evaluation and native/runner/health logs.
Academy JUnit `QA/EVIDENCE/penta-compact-trial-e5r31/python-final.xml` local only.

| Path | SHA256 |
| --- | --- |
|Mini trial.json|`60FB14D73AF09FBB51B7B9D4D2B75D11DF3FFC49FFC23368F667E853D6BB8013`|
|Mini model evaluation.json|`CE7513BBC8690E94B243AE92F999A460363321BF70D828A9EB6DEEF47388DE20`|
|Mini operations/tools/run_transitions_e5r29.py|`A5EB1932598F6487EB486F0981399CFE4D405829892FF5921898A016DC1BF36E`|
|Mini operations/tools/run_compact_e5r31.py|`C513E65E099A281FBC0BD99077740794DA084348416500A3B6B53D1202BB7AD6`|
|Mini operations/tools/evaluate_compact_e5r31.py|`730CB6CD9A19ACB0B3D8A5010B7704868B9257A1AF23CB2B23156AEAF7EECC92`|
|Mini tests/test_compact_trial_e5r31.py|`3E5AD9AC05011D268541033802FFF0A059ED41E9AC96A6E9945DA609D75893AE`|
|Academy python-final.xml|`0374A077B21D59A07E6DF7212DEF33ECB6CDBD8CF9D119F17527EDFB9D26E570`|

Commands in Mini root, `PYTHONPATH=src`, `PYTHONDONTWRITEBYTECODE=1`:
E5-R30's11selected suites plus `tests/test_compact_trial_e5r31.py`,
`-q -p no:cacheprovider --junitxml=D:/AcademyDesk-codex-p0/QA/EVIDENCE/penta-compact-trial-e5r31/python-final.xml`;
`python operations/tools/run_compact_e5r31.py` (preflight), then
`python operations/tools/run_compact_e5r31.py --run` once. Shell environment restored.

## Next decision, not another identical inference attempt

Prompt shortening alone did not clear the deadline. Do not repeat unchanged
2CPU runs, extend30s, reset quarantine or activate the model to accelerate launch.
Local inventory has6CPU cores/12logical processors; observed QuadroT1000 4096MiB,
but no cached GPU runtime was listed and pinned runtime is the CPU-tested path.
GPU compatibility/build/download is a separate unvalidated change, not automatic.

**Proposed smallest changed-resource experiment, awaiting explicit approval:**
one local2B/compact-v3 trial, existing images/artifact,4CPU allocation/4native
threads instead of2, one slot, unchanged context/output/30s/memory guard. Maximum
12exposed fresh regression generations/15minutes including lifecycle, stop on
first uncertain timeout; exact scoring, retained raw results and original baseline
restoration. No new model/runtime download, GPU/driver install, Azure, training,
promotion or production flag. Even12exact would not qualify full model/read/host
scope. This is a hypothesis, not a guaranteed performance fix or user authorization.

Sol High owns this bounded runtime/accuracy decision. If approved, implement and
test exact profile/resource wiring before one attempt; preserve all previous
failures/unrun counts. Real read/operational qualification and reviewed admission,
history/other families/roles/Manual/privacy/provider/device/security/enterprise/
release and BUG-DATA-0001 remain OPEN. Production NOT READY.
