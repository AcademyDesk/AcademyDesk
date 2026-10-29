# E5-R44 — read-only first-use native timing review

19 October2026. Sol High. Saved R43 logs/cache metadata only; no inference,
container/service change, executable code edit, new tests or suite rerun.
R42's83offline checks retained historically, not rerun or expanded acceptance.

## New evidence

R43's57frozen source pins rehashed0drift. Native logSHA256
2b11e7121ee8bf989a465c3ac2f49b7d84b3b72c0233efea2c10f0ba9bb1414d;
cache metadataSHA25640239fc52a00c79230e7889ba970bdf80d2d69f73757c371033dd5c80671b6a2
still match the preserved report.

| Event | Saved observation |
| --- | --- |
| First native chunk |42.235463→58.126493s;15.891030s |
| Next long work interval |60.512301→72.045499s;11.533198s |
| New cache file5/0/6ed90efe243025 |5978788bytes,mtime1792422108180639300ns since Unix epoch |
| New cache file1/0/049c4f1bf8c6ad |5056685bytes,mtime1792422121978465000ns since Unix epoch |
| Separation of new file writes |13.7978257s |
| Separation of two long-work returns |13.919006s |
| Sampler initialization |0.14ms, explicitly logged at60.102728s |
| First sampled token |72.046862s,29.811399s after native dispatch |

File-write spacing and native-return spacing differ by0.1211803s. This is
consistent with two first-use compilation/finalization events accompanying the
long waits. However native logs are process-relative and file mtimes wall-clock;
there is no independently recorded shared-clock origin or compiler API trace.
Do not claim exact overlap, kernel names, compile durations, causal attribution
or percentage of latency explained. File mtimes are completion/write metadata,
not compilation start/stop. No raw cache executable contents read or changed.

The0.14ms measurement rules out the *logged sampler-init operation* as either
multi-second wait; it does not prove all grammar/sampling overhead negligible.
The request did progress, ruling out a permanent first-yield deadlock for this
run. Its complete reply still failed30s; no semantic accuracy measured.

## Specific next diagnostic proposal — not executed

NVIDIA documents [CUDA_MODULE_LOADING=EAGER](https://docs.nvidia.com/cuda/cuda-programming-guide/05-appendices/environment-variables.html#cuda-module-loading)
as loading modules/kernels at initialization rather than deferring loading to
first use, with higher startup time and memory footprint. Its [lazy-loading
guide](https://docs.nvidia.com/cuda/cuda-programming-guide/04-special-topics/lazy-loading.html)
describes the timing/memory tradeoff. This does not guarantee elimination of every
first-use compilation, shape-specific initialization or graph overhead.

Recommend one isolated **empty-cache eager-loading** comparison to R41 cold
baseline, NOT a recursive copy of R43's growing cache:

- Retain same pinned2B/CUDA12/runtime/profile/batch128/128/context2048/output512/
  four threads/one slot/lv5. Add only CUDA_MODULE_LOADING=EAGER to R41 native
  launch. New empty run-owned D:cache with same1GiB cap/space guard and metadata
  phases, no original cache mutation or new model/runtime install.
- Verify actual EAGER environment, cache/isolation settings, full layer offload
  and128/128 before evaluation. No generation used to prime/prewarm the process.
- Keep original150s native-load health budget,30s request/quarantine/exact
  scoring, unchanged host/GPU reserves,12exposed cases and15minute lifecycle.
  If eager startup exceeds the load limit or uses too much memory, report blocked
  load/guard—not a pass, and do not extend the limit or retry automatically.
- Capture startup/readiness/first-yield/cache timings separately. No claim that
  moving necessary work before readiness makes total cold startup free. A pass
  would cover this subset only, not prior read/semantic/full-host/release gates.
- Preserve original services/configuration/mount/state, remove only exact owned
  candidate containers, retain logs/D:cache locally. No Docker restart/driver/
  graph-toggle/runtime build/parameter sweep/Azure/paid resource changes.

Separate scoped approval is required before this new service interruption.
No implementation/test/launch of EAGER was performed by this read-only review.
If this changed experiment also fails, stop cache/loader tuning and obtain a
specific native compiler/kernel trace or separately reviewed architecture-native
runtime build; do not keep retrying until a warm answer happens to pass.

## Final checkpoint

Both worktree branches/HEADs/main unchanged, staged0/diff checks PASS,
QA/EVIDENCE tracked0/.build-check ignored, unrelated WIP/evidence preserved.
Original PENTA healthy/API8000health200/four idle slots; Academy3000/5092 still
not listening. No app/SQL/password/login/bootstrap/migration changes. All real
model/semantic/read/operational/history/domain/role/Manual/device/provider/privacy/
enterprise/release gates and BUG-DATA-0001 remain OPEN. Production NOT READY.
