# E5-R45 — local NVIDIA authorization and eager-loading result

19 October2026. Owner authorizes local NVIDIA use for development. Treat this as
permission for bounded relevant local GPU engineering/testing, not mandatory GPU
use for web/API/SQL tasks, unlimited compute, driver/settings changes, unqualified
serving activation, paid Azure, publishing or production approval. Existing domain
security/testing gates remain independent. Diagnostic GPU failures never trigger
an unreported CPU fallback. Downloads/generated caches continue onD:.

**Single pending R44EAGER experiment EXECUTED; BLOCKED_LOAD_OR_RUNTIME.**
No Docker restart/priming/retry/sweep/new model/runtime/install/Azure. Normal
application and PENTA serving defaults unchanged; no training/model promotion.

## Implementation and focused validation

Mini additive `run_gpu_eager_e5r45.py` delegates R41/R39/R36 lifecycle, adds only
CUDA_MODULE_LOADING=EAGER to cold R41 native launch. New empty owned D:cache,
same1GiBdriver cap/space guard. No recursive warm-cache copy. Code checks actual
EAGER/cache/isolation/batch/full-layer-offload BEFORE generation if health passes.
No evaluator/oracle/deadline/resource guard change. Same pinned2B/CUDA12/profile/
128batch/context2048/output512/four threads/one slot/lv5/150sload/30srequest/
12exposed cases/15minute lifecycle. Prior source/receipts/cache unmodified.

11new offline tests+83retained: **94 PASS/2.59s**. Command from Mini root:

```powershell
python -m pytest tests/test_gpu_eager_e5r45.py tests/test_gpu_warm_e5r42.py tests/test_gpu_cache_e5r41.py tests/test_gpu_batch_e5r39.py tests/test_gpu_diagnosis_e5r38.py tests/test_gpu_diagnostics_e5r37.py tests/test_gpu_trial_e5r36.py tests/test_cuda_import_e5r36.py tests/test_cuda_archive_e5r34.py tests/test_cpu4_trial_e5r32.py -q --basetemp D:/PENTA-Models/operations/e5r45-pytest --junitxml D:/PENTA-Models/operations/e5r45-checks/python.xml
python operations/tools/run_gpu_eager_e5r45.py --run
```

Broader Python/SQL/browser suites retained NOT rerun. Focused tests prove guards,
not model semantic correctness or runtime success.

## Observed runtime

Run e5r36-089c8964c6ea, observed exit0/236.26s receipt elapsed; exit0 is bounded
capture/restoration, not model acceptance.57frozen AND current pins rehashed0drift.
Unchanged3093922048byte host guard passed last3samples4471320576/4473335808/
4416196608 after two below-threshold samples2583986176/2663399424.

Candidate process launched with requested EAGER settings, but health probe hit
the original150s deadline: retained log reports `RuntimeError: candidate load
deadline`. The captured native log contains no substantive startup lines (2bytes
of separators); cache files did grow. No readiness200, model-loaded marker,
actual full-layer-offload/batch/EAGER verifier acceptance or inference evidence.
Do not claim verified loaded GPU model or assign a precise native initialization
stage, OOM, deadlock or sole JIT cause from these incomplete runtime logs.

**0tokenizer checks/0generations/all12unrun; no evaluation.json.** This is a load
failure, not a failed answer/accuracy score.150sload/30srequest limits not extended.

Cache metadata: empty0files/0bytes beforelaunch →33files/101218132bytes at
pre-cleanup →34files/106810766bytes after restoration. Existing metadata phase
label `after_evaluation_before_native_cleanup` here means cleanup after failed
health; no evaluator actually ran. D:cache retained privately, never Git/upload/
training. Generated artifacts do not themselves qualify runtime/model.

## Restoration and evidence

Only exact owned native+health containers removed, logs retained; evaluator
container never created.0owned leftovers.25original container identities/config/
state restored, extra host/mount snapshot PASS; original native healthy/four idle
slots/PENTA8000health200. Academy3000/5092 were already stopped/remain stopped;
no app/SQL/password/bootstrap/migration changes or Admin login acceptance.
Both HEADs/main unchanged, staged0/diff checks PASS; QA/EVIDENCE tracked0/
.build-check ignored, unrelated WIP preserved.

| Receipt | SHA256 |
| --- | --- |
| Mini operations/evidence/e5r36-089c8964c6ea/trial.json |26ccd9608823a1e1220bbfd5bb83260ce804fb2d445661140822d0f3dc699f79|
| Same run frozen-plan.json |769e4c2c5cbd8a0ff684dc439604ced57e31ed8a048687c4435980aef5ef6a13|
| Same run health log |6f0af8a155ed163deafa6e0ecc4e4314f74c02b66380d0c2481653284c4f45ef|
| Same run native log |7eb70257593da06f682a3ddda54a9d260d4fc514f645237f5ca74b08f8da61a6|
| D:/PENTA-Models/operations/e5r45-eager/e5r36-089c8964c6ea/cache-metadata.json |33aaf89bfa12d3e5ceb327599e30d053c7e2cfd4dff04c61beae212d54db28c2|
| D:/PENTA-Models/operations/e5r45-checks/python.xml |3cea6d2c76b3759b6930932bac74ae32785a67f1221879ad9760f637012c7490|

## Next route — stop cache/loader tuning

Sol High: inventory a bounded native tracing path or review a same-revision
sm75-native runtime build. Host PATH checks found no nsys/ncu/cuobjdump/nvcc;
none installed/downloaded. Inventory does not exclude tools elsewhere. Do not
download a large toolkit/build image, change driver, run privileged profiling,
loosen150s/30s or restart Docker automatically. Prepare exact tool/runtime source,
storage/resource/security/time limits before any acquisition or new active test.
No further growing-cache/EAGER/batch sweep. Local NVIDIA authorization is retained
for appropriate subsequent development; production hardware remains separately
sized/qualified and paid Azure still requires explicit budget/scope approval.

All model/read/operational/semantic/history/domain/role/Manual/device/provider/
privacy/enterprise/release gates and BUG-DATA-0001 remain OPEN. Production NOT READY.
