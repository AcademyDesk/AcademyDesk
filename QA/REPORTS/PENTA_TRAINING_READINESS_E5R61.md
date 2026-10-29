# E5-R61 — development curriculum and local training readiness

Final handoff:21 October2026. Evidence produced20 October2026. Route:Sol High.
This continues the existing enterprise/PENTA plan, not a new audit/testing program.

## Completed

- [x] Read-only inspection of the configured D:3B model library and two known Python
  environments. This is not a claim that every disk or environment was inventoried.
- [x] New strict offline operational review contract reusing existing PrivacyLabel/
  ReviewLabel declarations; no serving import, API, trainer or automatic learning.
- [x]56contract-authored synthetic review records:42development/train candidates,
  14separately worded validation candidates,14lessons. **All56pending review;
  zero training-eligible.** No acceptance split or SFT-ready export was created.
- [x] Lessons cover preservation, explicit clearing, reset/partial drafts, unavailable/
  ambiguous references, protected-request refusal, zero/false/decimal/rows and draft controls.
  Families/entities/catalog tools are separate from exposed operational cases; validation
  uses separately authored templates rather than relabeling development templates.
- [x] Answer-free layered exclusion package:142prior conversation/read cases plus104
  operational IDs =246known IDs. Includes normalized user-message hashes, canonical Turn
  hashes and blocked families. Existing exclusion index left unchanged. This is a scoped
  exclusion union, not proof of complete global contamination detection.
- [x] Privacy/review/eligibility, strict label/reducer binding, cross-split family/message
  leakage, exposed-ID/lineage/reference hash protections and readiness checks tested.
- [x] **631related tests PASS,0failures/0errors/0skips**;JUnit15.759s.
  Includes82new curriculum tests,172existing training-declaration tests and377retained
  operational tests. Counts overlap preceding runs;do not add631to earlier totals.
  One existing Starlette/httpx deprecation warning. No enterprise suite rerun.

Declarations/hashes do not authenticate reviewers, prove privacy/licensing or detect all
paraphrased derivatives. No model-authored hidden reasoning, customer data, secrets or
minor personal records were collected. The review package is not permission to train.

## Readiness result:NOT READY

The pinned index for Ministral-3-3B-Instruct-2512 references two original checkpoint shards:
`model-00001-of-00002.safetensors` and `model-00002-of-00002.safetensors`.
Neither exists in its configured upstream directory. Metadata lists7698180096bytes
(about7.70decimal GB) of original weights, not total training-memory need.
The downloaded Q4_K_MGGUF is an inference artifact, not the original checkpoint.
Index/config/tokenizer/tokenizer-config hashes were reverified against local library pins.

Hardware snapshot:QuadroT1000/4096MiB,3938MiBfree;16.88GBphysicalRAM,
3.90GBavailable at collection. These are point-in-time observations, not current guarantees.
The original weight bytes alone exceed the existing5.5decimalGB guarded local workload
cap and4GiBVRAM **before** training overhead. Quantized/offloaded adapter feasibility
is unmeasured;this does not claim every possible training method is impossible.
No guard/policy was relaxed to start an unsafe experiment.

The main development Python lacks torch/transformers/adapter-training dependencies.
The existing embedding environment has torch2.14.1+cpu/transformers5.19.0/safetensors0.8.0,
but no peft/bitsandbytes/accelerate/datasets/trl. It is not a verified GPU trainer and
was not modified. Package metadata inspection is not a CUDA training compatibility test.

### Six training prerequisites still open

1. Original pinned training checkpoint acquisition/verification and tokenizer compatibility.
2. A separately designated, version-pinned GPU training environment.
3. Measured safe training workload peak and bounded experiment plan.
4. Reviewed resource placement within owner limits;larger/paid compute requires approval.
5. Reviewed curriculum provenance/privacy/labels;no exposed answers in training.
6. Untouched acceptance split, artifact conversion/identity, pre/post safety/regression,
   rollback and release evidence. This extends the assessment's corpus gate, not an
   assertion that the machine can train after just installing dependencies.

Do not download, install, provision Azure, train or activate a tier based on a generic
continuation. A concrete training/resource decision is required first. No training
cost/SKU/time promise is supported by this local inventory.

## Evidence and code

Package:`D:/PENTA-Models/operations/e5r61-readiness/review-fa5ad2c1d0e7`:

- `readiness.json`:436c11d6bb4204a3ddfa103b1dac81d26b445f0f3be8120fe88c85cfaa39b12f
- `review-corpus.json`:28cb5347dffb2b00cb6527f69f78efc306bd852a00d3013f1e7dc21382d7856e
- `exclusions.json`:a3e7c0e79dd2d77a7b5b5b84605b13eaa49cb38847c2c2ecfca006143287b407
- Canonical dataset digest:1e9fc189802c9042a07415eb31cb6ae85ed58b1428b0cf355af1a329f76622a5
- JUnit:`D:/PENTA-Models/operations/e5r61-checks/regression.xml`.

New code lives only in Mini's offline review boundary:
`src/penta_mini/operational/curriculum.py`,
`operations/tools/field_curriculum_e5r61.py`,
`operations/tools/prepare_curriculum_e5r61.py`,
`tests/test_operational_curriculum_e5r61.py`.
No training/export/provision action mode exists in the preparation script.

## Final preservation checks — 21 October 2026

- [x] Evidence receipt hashes and the four tested E61 source identities verified.
- [x] 88 unique frozen source files unchanged across the prior diagnostic/provider pins.
- [x] Original 25 container identities, images, configurations and running states match
  the R60 baseline; both original PENTA services healthy/idle, four idle inference slots.
- [x] 41 Docker volumes retained. No container cleanup, restart or pruning in this batch.
- [x] Both working branches and HEADs unchanged; zero staged files, no tracked
  `QA/EVIDENCE`, and Git diff checks passed. Academy `.build-check` remains ignored.
  Main HEAD remains `f7af512f884ca5fe8ac3ddb287c852492592e709` and was not edited.

## Next execution

Next Sol High:review/expand the development-only curriculum and prepare an explicit
bounded model-improvement decision with data/artifact/resource/rollback gates.
An authorized supervised adapter experiment is one candidate,not a guaranteed fix.
Do not keep tuning exposed evaluation prompts, rerunning the failed model unchanged,
or replacing production with a model that only passes scripted tests.

The existing3Breal full-plan result remains fresh6/12/exposed7/12,NOT ACCEPTED.
No model weights improved during this batch. Host/domain/Manual/device/enterprise/
release gates and BUG-DATA-0001 remain OPEN. No stage/commit/push/main, serving/model/
tier/default, database, training/inference/download/install/restart/prune/Azure action.

Project-wide remaining work is summarized without a false small-job count in
[pending packages snapshot](../../ACADEMY_REMAINING_WORK_2026-10-21.md).
