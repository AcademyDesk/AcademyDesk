# E5-R50 — one incremental native build continuation

20 October2026. Route: Sol High for runtime/build integration. Local only.

## Final result — build failed, originals restored, not accepted

Owner's “continue” accepted the pending scoped incremental-build request. No
Docker restart. Run `e5r49-9244f3d51042` finished321.31s, output
`D:/PENTA-Models/operations/e5r49-build/e5r49-9244f3d51042`.
Status **FAILED_COMPILATION**,prepare0/compile1,no OOM. The cache restored135
CUDA objects; nine new units brought actual count to144 and native CUDA library
linked. Subsequent CPU target failed because the file-only archive had omitted
empty target output directories. Exact failure: opening dependency file
`CMakeFiles/ggml-cpu-x64-feats.dir/ggml-cpu/arch/x86/cpu-feats.cpp.o.d` failed
with “No such file or directory.” This is a cache-restoration tooling defect,
not a passed candidate or evidence of a model failure.

Owned builder removed; all25 original container/config/host/mount/state records
restored,healthy/four idle slots. D:free127693414400bytes. Single continuation
stopped **STOP_BUILD_NOT_ACCEPTED**. No binary probes/model load/inference ran.
No background build remains. The approved single incremental attempt is spent.

## Checklist

- [x] R49 final outcome recorded:2400s compile timeout,not OOM;originals restored.
- [x] Final salvage SHA256 independently reverified:
  `fb4d1b3023e95acde20ec754b228c7a0c34cb9c1d4c8d3d368103414c2bf098d`.
- [x] All4785 member hashes/sizes, regular-only unique safe paths,2GiB bound.
- [x] All3684 source files match pinned source ZIP;146 object files retained.
- [x] Full installed package inventory matches R49 exactly before reuse.
- [x] Unchanged memory guard passed: last3available samples4716589056,
  4715634688,4715651072bytes vs3758096384required. No threshold relaxation.
- [x] Offline compilation isolation and same2.5GiB/2CPU/onejob verified.
- [x] 49 focused safety tests initially PASS/1.45s; expanded210PASS/2.79s.
- [x] Failed attempt completed; owned cleanup/original25/healthy4idle restored.
- [ ] Complete successful incremental build (R50 failed at missing directory).
- [ ] Actual artifact hashes/version/native75cubin/device/library verification.
- [ ] Unchanged uninstrumented synthetic semantic/latency trial if proof passes.

First test invocation had27passes/22setup errors because D:temporary parent was
missing. Created only that new test directory and reran with a fresh basetemp;
49/49 pass, original error receipt retained. Not an application failure.

## Implementation and boundaries

`operations/tools/resume_sm75_e5r50.py` verifies cache and source again inside
the owned container before safe exclusive extraction to `/work`, preserving
timestamps and original absolute CMake paths. No extractall/link/device/member
overwrite. Cache75-real/Release/native-off/CPUvariants/CUDA/thread flags checked.
Existing build lifecycle gained explicit resume mode; default fresh-build path
is unchanged. Helpers frozen per run. Prepared exact-version packages must match
the complete prior dpkg inventory. No global compiler/driver installation.

Same pinned source11fe02151f79c41d0d4af7da708755d73b9c0da6/b11382 and official
CUDA12.8.1 builder. CMake single job2400s internal cap/2700s external command cap,
600s package cap,10GiB disk reserve/100MiB log cap. No model/GPU in builder.
Old receipts, both salvage archives and partial unverified per-file copy retained.

## Prepared correction — NOT executed in a new build

After R50 finished, resume helper now runs bounded `cmake -S /work -B /work/build`
before building. CMake regenerates its own missing empty target directories from
the existing cache, with no new -D overrides. Before AND after regeneration,
source commit/build number,75-real,compiler paths,CUDA threads,Release,CPU variant/
native/backend switches,UI/test/example and linker settings must still match.
Configure cap180s; original2400s single-job compile cap remains unchanged.
Frozen R50 helpers/receipts retained,not retrospectively changed. Two functional
mocked-subprocess tests exercise directory regeneration and rejection of changed
configuration; these do not replace a real corrected build. Final focused suite
212PASS/6.21s (`D:/PENTA-Models/operations/e5r50-checks/final.xml`). Owner approved
one corrected guarded attempt; [R51 current result](PENTA_CORRECTED_BUILD_E5R51.md)
records it separately. No automatic retry/restart.

No application/default/weights/prompt/schema/scoring/DB/credential changes.
No staging/commit/push/main/Azure. Academy3000/5092 remain unstarted.
R49's188 tests retained, not added to overlapping current suites. Broader enterprise suites
not rerun. All qualification/domain/security/role/device/Manual/release gates and
BUG-DATA-0001 remain OPEN.90%AI is a target,not delivered coverage.

Next route stays High until actual candidate proof and unchanged evaluation are
resolved;Medium is suitable for settled visual/documentation implementation.
