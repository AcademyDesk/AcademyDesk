# E5-R48 — valid model trace proves substantial PTX compilation waits

20 October 2026. Local-only continuation of R47, Sol High runtime diagnosis.
No application/domain/permission/default/model/driver/database/Azure change.

## Outcome and checklist

- [x] One owner-continued guarded run using the repaired existing trace wrapper.
- [x] Actual full GPU offload/128batch/cache isolation verified;12 token fits.
- [x] Valid finalized model report; stop0,3.944s. No capture-hook repair needed.
- [x] Offline export and read-only analysis of actual model trace.
- [x] Direct CUPTI PTX-to-CUBIN compile events identified, not file-time inference.
- [x] Extended analyser preserves JIT/API correlation and visibility limitations.
- [x] Metadata-only same-source/sm75-native build plan pinned; no layers pulled.
- [x] 148 focused tests PASS; original25 containers/services restored/healthy/4idle.
- [ ] Acquire/build a resource-bounded sm75-native candidate and inspect its cubins.
- [ ] Uninstrumented semantic/latency qualification and host integration acceptance.
- [ ] Existing enterprise, role/tenant/security, device, portal and release gates.

## Actual model diagnostic

Run `e5r36-a5f768b46bbd`,171.46s total lifecycle. Unchanged memory requirement
3093922048bytes, samples2423418880,2649128960,4207857664,4216684544,4229427200;
last three passed. Same2B artifact, CUDA image, compact-v3 prompt,2048context,
512output budget,four threads,one slot,128/128batch,150s health/30s request.
Runtime verification confirms all25/25 layers offloaded; that is not CPU-free
execution (CPU-mapped input embedding remains). GPU1293MiB used/2646MiB free.

First660token request30.050s,no complete raw response/plan,quarantine,11unrun.
Receipt `generated=1` counts a provider request attempt; `raw_responses=0`.
No accuracy percentage/model qualification or uninstrumented speed acceptance.
No scoring/deadline/context/oracle relaxation and no fallback.

Trace receipt:
`D:\PENTA-Models\operations\e5r47-modeltrace\e5r36-a5f768b46bbd\trace-receipt.json`.
Actual `trace\model.nsys-rep`249961bytes, finalized by the corrected verified-ID
hook. Offline export `e5r47-811c62be8cdb` exit0,original snapshot unchanged:
`D:\PENTA-Models\operations\e5r47-offline\e5r47-811c62be8cdb\model.sqlite`.

## Direct evidence, with limits

| Captured event | Count | Aggregate | Longest |
| --- | ---: | ---: | ---: |
| CUPTI JIT PTX→CUBIN Compile | 18 | 64.612934941s | 14.918127462s |
| CUPTI JIT cache store | 18 | 0.722786589s | 0.121718641s |
| CUDA runtime API events | 8220 | See analyser | See below |

Same-thread/same-correlation-ID containment connects long JIT events to real
`cudaFuncGetAttributes_v3020` and `cudaFuncSetAttribute_v9000` calls. The former
25calls aggregate33.066451990s,max15.108767867s; latter7calls aggregate32.335949555s,
max9.181592835s. These enclosing API times **overlap** JIT durations and must not
be added to them. JIT is now directly observed, not just a cache-mtime lead.

Largest compile spans in trace-relative seconds include50.333640239–65.251767701,
26.143972898–38.372994440,84.380007397–93.464825688,
68.781684532–77.063106166 and77.166184278–84.287993852. Native log request work
starts around1:08.387; independent logger/trace origins mean do not claim an exact
request-only JIT total from that approximate alignment. The graph of captured
long calls plus R46's verified PTX-only sm75 artifact supports a native-cubin
candidate, not a guarantee that it will meet30s or pass semantic cases.

Important profiler limitations retained explicitly: "Not all CUDA events might
have been collected"; unified-memory tracing unsupported here; hardware tracing
unsupported and legacy software-instrumented trace used. No kernel activity
table in this capture. **Absence of kernel records does not prove no GPU work**,
nor establish kernel durations or exclude other bottlenecks. No SYS_ADMIN,
privileged container,host perf/security/kernel change or driver upgrade was used.

Cache0→16files/46351068bytes atready→19/63602604aftereval→20/69789152final,
retained on D:. No further cache/EAGER/loader sweeps or blind model retry.

## Next candidate prepared, not built or activated

`operations/tools/plan_sm75_e5r48.py` validates the cached source archive SHA256
`b6dfbe4469265b84dbb9257c8e68fa47c41d479908892720ccab470ff5958535`,revision
`11fe02151f79c41d0d4af7da708755d73b9c0da6`/b11382. The inspected upstream
Dockerfile uses CUDA12.8.1/Ubuntu24.04/GCC14; CMake defaults sm75 to virtual PTX.
The candidate plan overrides **`CMAKE_CUDA_ARCHITECTURES=75-real`**, not a different
model,weight quantization,source revision,host driver or application contract.
Embedded Web UI is disabled (server-side inference candidate, not Academy UI).

Metadata-only official registry checks pin Linuxamd64 builder:
`docker.io/nvidia/cuda@sha256:4b9ed5fa8361736996499f64ecebf25d4ec37ff56e4d11323ccde10aa36e0c43`;
config `sha256:154bd2b08b4ccf152598c633958f4da7426d27a160d877fd8ccecb266285e35a`.
11compressed layers total5161997061bytes. Metadata receipt on D:
`D:\PENTA-Models\operations\e5r48-build-plan\plan.json`.
No layer pull,toolchain install,build,candidate runtime,model activation or paid
resource provisioned. Registry bearer ephemeral/in-memory,not persisted.

Before acquisition/build: bound download/storage/unpacked/build-output reserve,
compiler memory,CPU,job count and total time; resolve/record GCC14/CMake/OpenSSL
package versions. Avoid upstream unrestricted `-j$(nproc)` and unrelated UI/Node
build stages. Run compilation in an owned capped container with source/archive
verified and D:output,not global compiler install. Retain frozen source and
installed-package/compiler provenance; plan is not yet a bit-reproducible build.
Inspect actual resulting sm75 ELF and runtime revision, then run the unchanged
guarded uninstrumented acceptance. No serving-image swap until qualification.

## Validation and preservation

148focused testsPASS/1.66s: previous137 plus5 JIT/visibility tests and6 metadata
plan tests. JUnit `D:\PENTA-Models\operations\e5r48-checks\final.xml`.
An earlier10analyser testsPASS/0.39s and6plan testsPASS/0.14s are overlapping
focused checkpoints,not added to148. Larger enterprise suites NOT rerun.
Read-only analyser uses mode=ro/query-only,statement/output limits,no model/SQLhost
execution. Synthetic tests cover same-thread correlation,no double-count claim,
negative intervals,optional/malformed schema,retained visibility warnings and
source/platform/native-target/no-execution plan checks.

Original25containers/config/host/mount/state restored,four idle slots;frozen61
staged files unchanged. Only owned candidate/health/runner removed. Prior failed
trace and blocked attempts preserved. Academy3000/5092 still stopped; no login,
host CRUD/role/device/Manual/90%AI or production acceptance implied.
No stage/commit/push/main,credentials,production DB,driver/global configuration,
Docker restart or Azure changes. BUG-DATA-0001 and all existing release gates OPEN.

Final independent check:25/25 original IDs/images/config hashes/running states
match;all61 staged and corresponding current source pins match;owned candidate
containers absent;PENTA8000health200/native4idle. Both tracked diff checks exit0,
staged0,QA/EVIDENCEtracked0,`.build-check`ignored. Academy/Mini/main HEADs remain
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4` /
`2f8806967709a1a7ec29e5176e7c31968adbf6aa` /
`f7af512f884ca5fe8ac3ddb287c852492592e709` respectively.

Evidence SHA256:

| Artifact | SHA256 |
| --- | --- |
| Lifecycle | `2f18eaee4e363b4fd783a8bae06d6f9cd97404fb16352b81995453df0732d28e` |
| Finalized model trace | `a74c8c5ff2d402b9f06b4a9539182e838b151845d18d057e1ced60c0875c455b` |
| Exported model SQLite | `bac802e21c103f7c0d72b8b127ad84a25992a0cc9bd3d881dc0352398fa9b645` |
| Final148-test JUnit | `5a2bf7fc59bd72a2a6afee832dd373c403d0041e85204986012a3ed7beb44914` |
| Metadata-only build plan | `191c51684aa005227dc385673c42d3cc54e153cd902217210f6afef0cc2a40da` |
