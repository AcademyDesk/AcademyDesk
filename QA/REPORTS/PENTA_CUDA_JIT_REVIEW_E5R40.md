# E5-R40 — cold CUDA compilation and cache review

19 October2026. Sol High. **READ-ONLY RUNTIME INVESTIGATION COMPLETE.**
No inference, new container, service pause/restart, runtime install/download,
application/default/model/DB change or Azure action. Documentation only; no
new tests authored or rerun. R39's59 focused checks remain historical evidence,
not59 newly executed checks or model acceptance. Existing enterprise work retained.

## Local evidence comparison

Rehashed saved logs/evaluations/receipts; no historical evidence overwritten.

| Run | Actual first660token request | Native progress before cancellation |
| --- | --- | --- |
| R32 four-thread CPU | 30.051s, no complete reply |656prompt tokens processed in about10.05s;159generated tokens reported before cancellation |
| R37 CUDA2048/512 |30.063s, no complete reply |No completed first native yield/prefill timing before cancellation |
| R39 CUDA128/128 |30.032s, no complete reply |No completed first native yield/prefill timing before cancellation |

This is a localization clue, not a controlled statistical CPU/GPU benchmark.
All three used the compact profile and same artifact/input; none passed30s or
qualified model semantics. No prompt/output simplification or deadline increase
is justified by this comparison. CUDA actual layer offload was verified, but
CPU-mapped embeddings still coexist; exact kernel execution remains unprofiled.

## New primary-source lead

1. The exact runtime's [CUDA CMake defaults](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/ggml/src/ggml-cuda/CMakeLists.txt)
   specify75-virtual for Turing. That means PTX for runtime compilation, not
   precompiled sm75 device code. The [pinned Dockerfile](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/.devops/cuda.Dockerfile)
   disables native-host compilation and defaults CUDA_DOCKER_ARCH to default.
   The [pinned publishing workflow](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/.github/workflows/docker.yml)
   supplies CUDA12.8.1 without an architecture override. This supports expecting
   Turing PTX in the official image; actual binary disassembly was NOT performed.
   The logged750 architecture list alone never proved sm75 SASS was embedded.
2. [Upstream issue29513](https://github.com/ggml-org/llama.cpp/issues/29513)
   reports a closely matching first-request slowdown after healthy startup on
   sm75/Qwen3.5. October8 discussion identifies JIT compilation; warm-cache
   restarts behave differently. It concerns a different model/build/OS execution
   path, so it is corroboration, not proof of our root cause or an applied fix.
3. [NVIDIA's environment reference](https://docs.nvidia.com/cuda/cuda-programming-guide/05-appendices/environment-variables.html#cuda-cache-path)
   documents the Linux default compiler cache at ~/.nv/ComputeCache, a writable
   CUDA_CACHE_PATH override, and cached compilation reuse. Lazy module loading
   can defer kernel initialization until first use; startup health is not a
   guarantee that every request kernel has already been initialized.

## Static launch/cache audit

Pinned image revision matches11fe02151f79c41d0d4af7da708755d73b9c0da6; config
fab69fb6698b1029c7f8ca96d7f635dfe4386c48056abf8348bd530f0e8ab3aa rehashed.
No CUDA_CACHE_PATH/CUDA_CACHE_DISABLE/FORCE_PTX_JIT setting in image config or
the frozen native launch helpers. Native launch uses read-only root/model,
network none, dropped capabilities and a64MiB writable /tmp; no writable home
or explicit compiler-cache mount. Default image user is unspecified (root).

**Inference:** the trial does not deliberately provide writable persistent CUDA
compiler caching. Its default home cache path is on a read-only filesystem;
container removal cannot preserve candidate-local generated cache. Actual driver
cache access/failure and compilation durations were not traced. Host/WSL driver
caching may differ, so do not claim cache misses or compilation time were measured.
No cuobjdump installed; none downloaded. No claim of a confirmed cache permission
exception, missing SASS binary, OOM, deadlock or defective GDN kernel.

## Recommended next bounded diagnostic — NOT RUN

One cache-enabled comparison to R39, without changing model/runtime/profile/batch:

- Use a new exact run-owned directory underD:/PENTA-Models/operations for writable
  compiler cache, mounted only at a dedicated candidate path. No C:downloads,
  existing cache deletion, original-service configuration or writable model root.
- Set CUDA_CACHE_PATH to that dedicated path and an explicit1GiB driver cache
  cap; verify D:free space first. The driver cap is not an OS filesystem quota.
  Cache contains generated executable artifacts: local only, never Git/training/
  customer data, never shared across untrusted users. Retain it for diagnosis.
- Record cache file counts/bytes/times before loading, at readiness and after
  request/cancellation; record image/artifact/driver/source identity. Metadata,
  not raw private content; synthetic inputs only. Do not call cache growth alone
  a proven timing attribution or functional pass.
- Keep actual128/128,25GPUlayers/context2048/output512/four threads/one slot,
  all host/GPU guards, exact scoring,30s per request/quarantine/12exposed cases/
  15minute lifecycle and original restoration/owned cleanup. No priming request,
  hidden warm-up generation, exception to cold-request acceptance or auto retry.
- No EAGER-loading/graph/driver/build changes at the same time. No restart,
  parameter sweep, replacement runtime, extra model or Azure provisioning.

Purpose: measure the compiler-cache lead, not promise this enables a cold first
request within30s. If it fails, preserve the failure; do not replay a quarantined
request to manufacture a warm pass. Any later warm-cache comparison or sm75-native
build requires a separately reviewed scope and distinct cold/warm reporting.
Separate scoped approval is required before another PENTA service interruption.

## Evidence identities and final checkpoint

| Saved log | SHA256 |
| --- | --- |
| R32 e5r32-937241db4b32 native log |8bb7cc352531dc0facb30312645f071de3335ec532ed4f0f8230bda9b9d75676|
| R37 e5r36-046c6840ff55 native log |9ef6a0944d2cefc9f191bfd56adc8426e7a0888146c0f5e10ec6b78422967d70|
| R39 e5r36-18cae0b0b975 native log |a033804171708a65ec1142cfbf13f85dd790380f98b7805b82067f2ebed18272|

Original PENTA services remain running/native healthy/four idle slots/API8000
health200. Academy3000/5092 still not listening; no app restart/login verification.
Both worktree branches/HEADs/main unchanged; zero staged files, diff checks PASS,
QA/EVIDENCE tracked0/.build-check ignored, unrelated WIP preserved.

Next route Sol High: reviewed cache-enabled diagnostic above, not another blind
batch-size trial or weaker gate. Model/read/operational/history/domain/role/Manual/
device/provider/privacy/enterprise/release gates and BUG-DATA-0001 remain OPEN.
Production NOT READY; faster hardware/model size alone cannot establish correctness.
