# E5-R49 — native sm75 build: timed out, originals restored

20 October 2026. Sol High / local-only runtime build and qualification continuation.
Do not restart enterprise testing or activate an unqualified model.

## Final checkpoint — previous active notices superseded

Run `e5r49-5fc2792a5a3d` finished after2492.26s: **FAILED_COMPILATION**.
The single-job CMake command reached its unchanged2400s internal deadline.
Package preparation succeeded; compile exit1; `oom_killed=false` and recorded
cgroup OOM counters zero. No linked candidate or `artifacts.json` was produced.
Owned builder removed; all25 original container/config/host/mount/state records
restored and four idle slots healthy. D:free128535769088bytes. This was not a
successful build. The approved Docker restart is consumed; do not repeat it.

`finish_sm75_e5r49.py` correctly stopped at **STOP_BUILD_NOT_ACCEPTED**;
no binary verification, model load or inference trial ran. Final R49 focused
suite188PASS/2.01s (`D:/PENTA-Models/operations/e5r49-checks/final-tools.xml`),
including loader/live-map, lifecycle, continuation and archive safety. Earlier
167/184 counts are historical, not additive or proof of model qualification.

Partial preservation: per-file copy exceeded90s and its exact owned python copy
PID3993 was SIGTERM-stopped by full command-line identity comparison;compiler not
targeted. Partial directory retained unverified/no resume acceptance. Corrected
single-file salvage archive completed13.672s,4746files/230285552payload bytes,
0changed files;SHA256251d5226ee5451d4d5dd4ddefae480099a704fc994dd58ef7a8671d41ca07d15.
`unqualified-build-salvage.tar`/`.json` remain under the build root. This is not a
candidate/full build/checkpoint-qualified runtime and does not authorize a retry.

Final single-file salvage also retained: `unqualified-build-final-salvage.tar`
and matching JSON,4785files/241358897payload bytes/0changed files/14.0849065s,
SHA256 `fb4d1b3023e95acde20ec754b228c7a0c34cb9c1d4c8d3d368103414c2bf098d`.
R50 independently rehashed all4785 members and matched all3684 source files to
the original pinned ZIP;146 object files retained. This permits a separately
approved guarded incremental attempt, not use as a serving runtime. Old receipts,
archives and unverified partial per-file copy remain unchanged.

Build output/provenance root:
`D:\PENTA-Models\operations\e5r49-build\e5r49-5fc2792a5a3d`.
`compile.txt`, `prepare.txt`, `packages.txt`, compiler versions, `isolation.json`,
frozen helper hashes, and `build.json` preserve progress and final result.

## Completed acquisition and preflight correction

- Pinned official builder `docker.io/nvidia/cuda@sha256:4b9ed5fa8361736996499f64ecebf25d4ec37ff56e4d11323ccde10aa36e0c43`
  pulled once,exit0,167.04s; actual identity/platform verified.
- 5161997061compressed bytes/11layers,reported image size14600007119bytes.
  Shared existing layers reused. Active Docker storage verified on D:.
  D:free139048325120→130357649408bytes during acquisition; no caches deleted.
- First preflight rejected an invented `dataFolder` key before download. Corrected
  to existing `CustomWslDistroDir`, exact resolved D:directory matching the prior
  accepted importer; failure retained, no host setting edited.
- Acquisition left original container snapshot unchanged. Receipt:
  `D:\PENTA-Models\operations\e5r49-builder\acquisition.json`.
- No-model128MiB readonly/offline builder inventory exit0,
  `e5r49-63c3513eff0b`, originals unchanged. NVCC12.8.93 confirmed; GCC14,CMake,
  Python3 absent initially. GCC13/default make present; no global install.

## First guarded build and explicit restart approval

Run `e5r49-be229850b95b`,75.77s,**BLOCKED_BUILD_MEMORY_GUARD**,0package installs,
0compiles/model loads/inference calls. Required3758096384bytes =2.5GiB build
limit+1GiB reserve; original three-consecutive/45s rule. Samples382074880,
564310016,1254883328,1563508736,1565003776,1564037120,1568808960,1568886784,
1484476416. All below threshold. Original25/config/host/mount/state restored/4idle.

Owner then explicitly approved **one Docker restart and one guarded build attempt**.
`restart_for_build_e5r49.py` stopped only the verified idle pair, used supported
Desktop stop/start and terminated only `docker-desktop` WSL after clean stop.
No global WSL shutdown, other distro, app termination, driver/settings or pruning.
Restart53.43s; cached images/volume metadata and all25 original containers exact,
four idle slots restored. Available809496576→3705548800bytes after full restore.
Receipt `D:\PENTA-Models\operations\e5r49-restart\restart.json`.
This one restart approval is consumed; do not automatically repeat it.

## Executed build configuration (historical; no longer active)

`e5r49-5fc2792a5a3d` passed the unchanged build reserve. Package preparation
succeeded inside owned container only. GCC14.2.0/CMake3.28.3/NVCC12.8.93 and full
installed-package versions recorded. Source archive remains pinned
`b6dfbe4469265b84dbb9257c8e68fa47c41d479908892720ccab470ff5958535`,same source
revision11fe02151f79c41d0d4af7da708755d73b9c0da6/b11382.

Compiler configured **75-real**,not PTX-only default;one make/NVCC job,twoCPU cap,
2684354560byte memory and same swap cap. Network bridge disconnected before
compile and actual empty network-interface inventory verified. No privileged
container/GPU/model access.600s package/2700s compile driver limits,10GiB runtime
disk reserve/100MiB log limit; no global compiler install or unbounded nproc.
Inference-only build has embedded UI disabled,not a change to Academy frontend.

## Prepared next checks — NOT YET EXECUTED

`verify_sm75_e5r49.py`:verify successful/restored build,all artifact hashes and
source/native target;actual version,cuobjdump sm75 cubin,and GPU device probes
using readonly candidate overlay on pinned runtime. No model load.

`run_sm75_trial_e5r49.py`:single uninstrumented readonly candidate overlay trial,
same pinned runtime base/model/profile/128batch/2048context/512output/150sload/
30srequest/guards/evaluator/scoring/quarantine/12cases/15min/restoration. Actual
overlay mount/loader path and artifact identity reverified before generation.
No serving-image/default replacement or model promotion. Execute only after
binary verifier passes; no profiler timing used for acceptance.

167focused offline testsPASS/2.72s (148retained+19new acquisition/build/restart/
artifact/overlay checks). JUnit D:/PENTA-Models/operations/e5r49-checks/focused.xml.
These do not constitute build success,model qualification or enterprise closure.

Application/default/model weights/DB/credentials/Git staging/commit/push/main/
Azure unchanged. Academy3000/5092 remains stopped. BUG-DATA-0001 and existing
semantic/host/domain/role/device/Manual/enterprise/release gates OPEN.
