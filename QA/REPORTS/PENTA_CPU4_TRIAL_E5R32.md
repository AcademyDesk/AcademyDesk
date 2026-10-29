# E5-R32 — approved local4CPU trial

19 October2026. Owner explicitly approved the bounded4CPU experiment.
**EXECUTED / NOT ACCEPTED.** Not a full benchmark or production qualification.
Academy `codex/penta-search` HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`;
Mini `codex/academy-language-contract` HEAD `2f8806967709a1a7ec29e5176e7c31968adbf6aa`.
Unrelated dirty work retained. No stage/commit/push/main/Azure.

## Scope actually enforced

- Existing Qwen3.5-2B Q4_0 artifact/hash and exact cached CPU runtime/API images.
  Same compact-v3 prompt hash
  `807825ebc094a9ae006cddd6e21268cd3eade02e1eae19bb8e19eab0410bae27`.
- **4CPU allocation/4native threads**, verified live Docker NanoCpus4000000000,
  `-t4 -tb4`, and native threadpool log. GPU off/network none/no public native port.
- Maximum12already-exposed F29regressions, labeled `exposed_regression`, not
  freshly unseen holdout. Identical full Proposal+Decision expectations; no repair.
-15minute lifecycle ceiling, maximum480s evaluator with restoration reserve;
  unchanged one slot,2048context,512output,30s request deadline, one attempt,
  memory guard and stop-on-quarantine. No larger model/download/runtime install.

Additive trusted wrapper reuses existing guarded lifecycle and scoring. Reusable
runner/evaluator now expose code-owned thread/count/budget constants, retaining
old2CPU/72case/3600s defaults. Native30s/schema/reducer/gate unchanged. A reduced
subset's bookkeeping uses its actual planned length; it never creates acceptance
or changes exact score. Four offline tests check12case/profile selection, original
defaults, exact approved resource settings and no replaced scoring/deadline/gate.

## Real result

Run **`e5r32-937241db4b32`**, observed **exit0 /147.12s**, safely inside900s.
Exit0 is successful result capture/restoration, not an AI pass.

-12/12actual tokenizer preflight fits; largest713input+512output≤2048.
- Exactly1generation dispatched: F29-01,660input tokens.
- **30.051s**, no complete raw response, real native quarantine; run stops.
- **0complete answers/0exact/11unrun**. Do not report0/12accuracy or11generated
  wrong answers. No valid accuracy comparison or real application action.
- Native logs show prompt processing around10.05s for656processed tokens, versus
  about16.51s in R31. Different tokenizer/native log counts are not substituted.
  At about21.72s after dispatch100tokens had generated; at27.83s159tokens;
  output remained incomplete at cancellation. Prompt processing was observably
  faster, but this is not a complete-response throughput improvement or matched
  statistical benchmark. It did **not** clear the existing deadline.

No complete answer exists to score semantics. Original2Bread86/94, R29's observed
incorrect correction/clear and other models' unqualified results remain OPEN.
Faster hardware would not itself prove semantic correctness or permissions.

## Verification and resource restoration

- [x]386selected Python tests PASS (382retained+4new), actual JUnit16.81s,
 0failures/errors/skips, inherited Starlette/httpx warning.
- [x] Exact weight size/SHA256 checked. Existing3093922048byte start guard,
  final3consecutive sample minimum4451115008bytes. Earlier below-guard samples
  retained. No threshold reduction, unrelated app shutdown or memory trimming.
- [x]50staged/current source pins independently match; prior R29/R31 immutable
  stages/receipts remain unchanged. Current generic trial tools have new hashes;
  old full-file hashes remain historical, not silently repackaged.
- [x] Original PENTA IDs/images/config/running inventory restored; final receipt
  baseline_restored/original_containers_restored/frozen_source_unchanged=true;
  native4idle slots and Docker healthy observed. No owned trial container remains;
  only exact run-labeled trial resources removed after logs retained, no pruning.
- [x] Academy web3000/login and API5092health stayed200 during/final smoke;
  unchanged normal API binary. No developer SQL migration/seed/bootstrap/password
  reset, model activation or application source change. Actual Admin login unverified.
- [x] Diff checks/zero staged files/unchanged HEADs. Prior SQL/browser enterprise
  evidence retained, **NOT rerun** or treated as model acceptance.

## Evidence and pins

Mini `operations/evidence/e5r32-937241db4b32/`: `frozen-plan.json`, `trial.json`,
50file stage, model config/evaluation and owned native/runner/health logs.
Academy JUnit `QA/EVIDENCE/penta-cpu4-e5r32/python-final.xml`, intentionally local.

| File | SHA256 |
| --- | --- |
|Mini trial.json|`5A9895353ECFC5782B82CAF85235606DE81C8BFAE5C81EB29FDA9FF07E2777FA`|
|Mini model evaluation.json|`ACC91C704DA392C6943359C43DF595C14365DD602E979726F9374039FB329578`|
|Mini operations/tools/run_cpu4_e5r32.py|`3C48E87C4F9B25D4AEC06EF24C5D2F38092DB8F7D4089872C02DBB27F952C31A`|
|Mini operations/tools/evaluate_cpu4_e5r32.py|`E684120D1093070352FF2A8C8565EB418631A914C63BC4AA87548E010FE19D15`|
|Mini operations/tools/run_transitions_e5r29.py|`ACBAD63C05A3B142767F44DABB367EA9C5B34670F9CA9FEA2A7206F29081975D`|
|Mini operations/tools/evaluate_transitions_e5r29.py|`D0E192E9154F7503B2B192C68F83D9BE022E760C332C12F4821BE23CE1149B5F`|
|Mini tests/test_cpu4_trial_e5r32.py|`AB024DB0B57970A3DE6A3D28A79313B6406D4C21A72196E88259D335FA0AC472`|
|Academy python-final.xml|`F6F1C8B4ACEBE9C6C5382B49E41E44775A675C22B2BE20129B2E9F166E176ACA`|

Commands from Mini root with `PYTHONPATH=src`, `PYTHONDONTWRITEBYTECODE=1`:
R31's12selected pytest suites plus `tests/test_cpu4_trial_e5r32.py`,
`-q -p no:cacheprovider --junitxml=D:/AcademyDesk-codex-p0/QA/EVIDENCE/penta-cpu4-e5r32/python-final.xml`;
`python operations/tools/run_cpu4_e5r32.py` preflight, then
`python operations/tools/run_cpu4_e5r32.py --run` once. Temporary environment restored.

## Next route / stop repeated CPU trials

The approved trial is complete. **Do not repeat this2B/compact-v3/4CPU trial**,
add threads/longer deadlines or try a larger model to find a passing output.
Retain the failure and preserve production unavailability.

Next Sol High: assess existing local GPU/runtime compatibility and formulate one
measured acceleration proposal, without installation/download/build/inference by
default. QuadroT1000 presence is not evidence this pinned CPU image can use it.
Any new GPU runtime/artifact acquisition or paid private Azure compute needs a
specific approval; this local4CPU approval grants neither. Semantic remediation
and full exact/read/new-independent qualification still required after any speed
change; no training on exposed test answers or canonical-score substitution.

Real model/host admission, history/other families/roles/Manual/privacy/provider/
device/security/enterprise/release and BUG-DATA-0001 remain OPEN.
**Production NOT READY.** No new production credential/data access, commit/push,
deployment, external chat handoff or project switch.
