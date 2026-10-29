# E5-R39 — approved smaller-batch GPU experiment

19 October2026. Owner continued the specific R38 smaller-batch proposal.
**EXECUTED; NOT ACCEPTED.** No automatic retry, Docker restart, new model,
download, training, promotion, application/default change or Azure operation.

## Scope and implementation

Mini additive `operations/tools/run_gpu_batch_e5r39.py` delegates to the existing
R36 lifecycle and exact evaluator. Compared with R37 diagnostic launch, adds only
native `-b128`, retaining `-lv5`. Rejects existing batch/verbosity overrides and
verifies actual native logical/physical128/128 BEFORE generation. Existing full
layer offload check, source/artifact pinning, host/GPU reserves, context2048,
output512, four threads/one slot,30s request deadline, quarantine and scoring
remain unchanged.12 exposed F29 regressions/900s lifecycle/480s evaluator ceiling.
Original general defaults remain2CPU/72cases/3600s; they were not rewritten.

Same CUDA12 b11382 image e8318a7b3988f9ca57b28c03486569e60def08f2b7b060550ebfa046e175e6cb,
same2B Q4_0 artifact cd70221bebaee0503e0f6717e174250cd7825aa88438b3aabec9ad55731d9bb1,
same transitions-compact-v3 prompt807825ebc094a9ae006cddd6e21268cd3eade02e1eae19bb8e19eab0410bae27.
No external network/host execution/SQL or private academy inputs in the candidate.

12 new offline guard tests +47 retained focused checks: **59 PASS/1.05s**.
These validate launch/actual-batch guards, not model quality. Previous broader
Python/SQL/browser evidence retained, NOT rerun or claimed as fresh acceptance.

## Actual result

Executed once from `D:\PENTA AI Models`:

```powershell
python -m pytest tests/test_gpu_batch_e5r39.py tests/test_gpu_diagnosis_e5r38.py tests/test_gpu_diagnostics_e5r37.py tests/test_gpu_trial_e5r36.py tests/test_cuda_import_e5r36.py tests/test_cuda_archive_e5r34.py tests/test_cpu4_trial_e5r32.py -q --basetemp D:/PENTA-Models/operations/e5r39-pytest --junitxml D:/PENTA-Models/operations/e5r39-batch/python.xml
python operations/tools/run_gpu_batch_e5r39.py --run
```

Run `e5r36-18cae0b0b975` deliberately retains the delegated runner's e5r36 prefix;
it is new R39 evidence, not an overwritten R36/R37 trial. Observed process exit0,
173.25s receipt elapsed. Exit0 means bounded lifecycle completed/restoration
passed, NOT model acceptance.53 frozen source pins independently rehashed:0drift.

- Required host reserve3093922048bytes. Initial five samples below threshold;
  final three4729995264/4824629248/4832727040bytes passed unchanged guard.
- Explicit GPU0,25/25 layers offloaded, actual128/128batch verified. GPU after
  load1265MiB used/2674MiB free. Input embedding remains397.85MiB CPU-mapped:
  full layer offload is not all-operations-on-GPU proof.
- All12 real tokenizer/template preflights fit, maximum713input tokens.
- First F29-01 prompt660tokens, **one provider call/30.032s/no complete raw reply**.
  STOPPED_NATIVE_QUARANTINE,11unrun,0completed replies. No measured model accuracy,
  complete-response speed or successful operational subset; no output repair.
- Saved native log: memory fitting4.17s; warm-up marker17.211246→57.534770s
  (40.323524s window), model ready57.612090s. First native decode boundary
 61.402411s→cancel91.400719s with no completed queue yield/prefill/reply.

Readiness was earlier than R37's129.422840s, but cache/environment histories differ;
do not assign all startup change causally to batch size. The actual request still
fails30s. Smaller batch did not solve the first-request problem. Logs do not
identify an exact kernel, prove a queue deadlock or establish that9B/Azure is needed.

## Restoration and repository state

Existing owner-labelled lifecycle captured/logged/stopped/removed only the three
run-owned candidate/health/evaluator containers. No owned leftovers. All25 original
container identities/config/state restored; extra before/after snapshot comparison
including host configuration and mounts PASS (observed runner output). Four idle
original native slots restored, original native healthy/API8000 health200.

Academy web3000/API5092 were already stopped before this experiment and remain
not listening; no Academy process/DB/password/bootstrap/migration change. Admin
sign-in remains unverified. Do not claim app health or login from PENTA restoration.
Both worktree HEADs/main unchanged, no stage/commit/push/merge; QA/EVIDENCE local,
tracked0; .build-check ignored. Existing unrelated WIP preserved.

## Evidence

Trial files remain under Mini `operations/evidence/e5r36-18cae0b0b975`.

| File | SHA256 |
| --- | --- |
| trial.json | f3bbb901fa512fb99b904ba2ab9db57e5c6acb51271b5074d97791898a3a8b97 |
| gpu-runtime.json | 70688af0dc602d4bc0f6f61be235b8b838b0325c6058ea88df8dab0c3583d895 |
| qwen35-2b/evaluation.json | ad7404498cbac085f9bd68f63e5f48bc71659d354f6b547d034a46a881bb913d |
| e5r36-18cae0b0b975-qwen35-2b-native-logs.txt | a033804171708a65ec1142cfbf13f85dd790380f98b7805b82067f2ebed18272 |
| D:/PENTA-Models/operations/e5r39-batch/python.xml | bd298cfd744b96321525af282258bb7452de3f40b458aa1daf39e5c0a18a96b2 |

## Next route / open work

Sol High: targeted native-backend investigation/profiling plan using the saved
R37/R39 evidence; do not repeat either unchanged configuration or automatically
sweep batch sizes/relax guards/deadlines. Any new service-interrupting experiment,
runtime replacement or paid hardware needs its own bounded reviewed scope.
Restore existing Academy local processes separately before UI work, without
reseeding/resetting credentials or enabling unqualified PENTA.

Real-model read/operational/semantic qualification still OPEN. Broader history,
domain/role/Manual/device/provider/privacy/enterprise/release gates and
BUG-DATA-0001 remain OPEN. Production NOT READY; no90%AI delivery claim.
