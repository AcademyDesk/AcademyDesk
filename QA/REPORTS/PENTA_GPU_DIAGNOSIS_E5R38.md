# E5-R38 — offline diagnosis of the existing GPU timeout

19 October 2026. Route: Sol High. **DIAGNOSIS COMPLETE; MODEL NOT QUALIFIED.**
No new inference, Docker restart, service mutation, application/domain change,
model download, training, promotion, commit, push or Azure operation this batch.
This continues R37, not a new testing plan. Existing enterprise gates remain open.

## What the saved evidence establishes

Analyzed the preserved `e5r36-046c6840ff55` native log and evaluation, without
rewriting either. Native timestamps are minutes.seconds.milliseconds.microseconds.

| Observation | Evidence / interpretation |
| --- | --- |
| Readiness | 129.422840s from native log start; automatic memory fitting took18.55s and the warm-up marker window was67.400731s. These startup costs are outside the host request deadline. |
| First request | F29-01,660 input tokens; arrives6.748009s after readiness. Native decode-boundary to cancellation window29.995744s; host request30.063s. |
| Native work | No completed prompt-eval timing, finished queue yield, sampled reply or complete raw response before cancellation/cleanup. Native decode includes prefill: this is not measured slow output generation or TTFT. |
| Placement | 25/25 layers offloaded, CUDA model buffer1148.14MiB, CPU_Mapped model buffer397.85MiB. Input embedding remains CPU-mapped. Full layer offload does NOT mean every operation runs on GPU. |
| Batching | n_batch2048, n_ubatch512, context2048; one slot/four threads. No automatic-fit parameter reduction was reported. |
| Outcome | One provider dispatch, native_uncertain/no raw reply, quarantine,11 unrun. No model accuracy measurement or qualified model. |

Earlier zero-MiB CUDA lines belong to automatic-fit planning, not the final actual
allocation. R37's “no CPU fallback” means no partial *layer* offload, not CPU-free
execution. CUDA graph support was compiled; actual graph capture is not proven.
Declined cancellation/next-response tasks while native work runs are not proof of
a queue deadlock. Debug logging overhead is unmeasured. Per-kernel GPU utilization,
CPU throttling and cold compilation timings were not retained, so an exact kernel
bottleneck or definitive root cause cannot honestly be assigned.

## Pinned upstream source interpretation

Reviewed exact runtime revision `11fe02151f79c41d0d4af7da708755d73b9c0da6`,
not mutable latest documentation. No executable/source build was installed.

- [server-context.cpp](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/tools/server/server-context.cpp): the logged yield wraps native decode and synchronization; the missing return localizes delay inside or awaiting that work, not to a specific kernel.
- [server-queue.cpp](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/tools/server/server-queue.cpp): worker queue handling can decline tasks while native execution continues; that alone does not establish deadlock.
- [arg.cpp](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/common/arg.cpp) and [llama-context.cpp](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/src/llama-context.cpp): `-b` controls logical maximum batching and effective physical batching is bounded by it. A proposed `-b128` therefore gives effective128/128 rather than2048/512 without lowering context/output limits or truncating the prompt.
- [ggml-cuda.cu](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/ggml/src/ggml-cuda/ggml-cuda.cu) and [gated_delta_net.cu](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/ggml/src/ggml-cuda/gated_delta_net.cu): graph/kernel implementation suggests possible cold/runtime granularity costs, but saved logs cannot prove which path caused this timeout.

## Offline implementation and validation

Mini `operations/tools/analyze_gpu_e5r38.py` reads only the two saved inputs;
an optional receipt is exclusively created at the exact D: diagnostic path.
It rejects missing timeline/placement markers, changed outcome and impossible
ordering. No Docker/network/inference API is invoked. Nine new tests plus38
retained focused tests: **47 PASS**, pytest console1.30s/JUnit suite1.196s.
No SQL/browser/full enterprise suite rerun or new acceptance is claimed.

Command executed from `D:\PENTA AI Models`:

```powershell
python -m pytest tests/test_gpu_diagnosis_e5r38.py tests/test_gpu_diagnostics_e5r37.py tests/test_gpu_trial_e5r36.py tests/test_cuda_import_e5r36.py tests/test_cuda_archive_e5r34.py tests/test_cpu4_trial_e5r32.py -q --basetemp D:\PENTA-Models\operations\e5r38-pytest --junitxml D:\PENTA-Models\operations\e5r38-diagnosis\python.xml
python operations/tools/analyze_gpu_e5r38.py --receipt D:\PENTA-Models\operations\e5r38-diagnosis\diagnosis.json
```

| Evidence | SHA256 |
| --- | --- |
| Saved R37 native log | 9ef6a0944d2cefc9f191bfd56adc8426e7a0888146c0f5e10ec6b78422967d70 |
| Saved R37 evaluation | e5aa8fe133e194d9a0f1d61b9e8cda6cfc025498419dad6d1393918e1c8c7ac8 |
| D:/PENTA-Models/operations/e5r38-diagnosis/diagnosis.json | 746519a007f20cd5b4c2e46add008b095e003945a0b8df6fa241456633850933 |
| Same directory python.xml | 5009de76380d3a6fb636657bac02a3ecb376b1525e5bb88d0279b2e10057b91b |

## Next experiment proposal — NOT executed or accepted

One bounded changed-batch trial: same pinned2B Q4_0, runtime/profile/scoring,
context2048/output512, four threads/one slot/full layer offload, diagnostic `-lv5`,
unchanged host/GPU reserves,30s deadline and quarantine. Add only native `-b128`;
verify actual128/128 in logs before dispatch. Same12 exposed F29 cases,15minute
lifecycle maximum, no retries/sweep or automatic restart. Exact original service
restoration and owner-labelled candidate cleanup remain mandatory.

Hypothesis: smaller native work chunks may change first-prefill latency on this
recurrent model/device. It may also be slower. This is not a promised fix, relaxed
acceptance or proof that9B/Azure is needed. Obtain separate scoped approval before
another service-interrupting experiment; the R37 one-restart authorization is spent.

All model/read/operational/history/host/domain/role/Manual/device/provider/privacy/
enterprise/release gates and BUG-DATA-0001 remain OPEN. Production NOT READY.

## Final repository and service checkpoint

Both worktree branches/HEADs unchanged: Academy codex/penta-search at b1f8cb6;
Mini codex/academy-language-contract at2f88069; main remainsf7af512 and untouched.
Zero staged files in both, git diff checks PASS, QA/EVIDENCE tracked0 and
.build-check ignored. Existing unrelated dirty work preserved.

PENTA API8000 returns200, original two containers running/native healthy/four idle
slots. Academy frontend3000 and API5092 are **not currently listening**, confirmed
by HTTP, TCP-connect and listener inventory. Previous R37 health200 is historical,
not current health. Cause of their stopping is not established by this diagnosis.
No restart or app login acceptance occurred; restoration must precede app UI work.
