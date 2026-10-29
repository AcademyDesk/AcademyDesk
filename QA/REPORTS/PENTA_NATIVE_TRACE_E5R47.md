# E5-R47 — CUDA trace capture, cleanup-hook repair and guarded continuation

Recorded 20 October 2026. Continues R45/R46; does not restart the enterprise plan.
Route: Sol High for native runtime diagnosis; no reason to change model for this
unresolved runtime problem. Tools/tests changed in `D:\PENTA AI Models` only;
Academy changes are these reports and continuation records, not application code.

## Checklist

- [x] Pinned diagnostic artifacts acquired/inspected; see `PENTA_NATIVE_TOOLS_E5R46.md`.
- [x] CUDA-only device trace collected without elevated privileges or model load.
- [x] Packaged importer converts device QDSTRM to a valid Nsight report.
- [x] Report exports to SQLite; read-only bounded analyser reports 130 CUDA API
  events. This is device enumeration, not model-kernel performance evidence.
- [x] One model diagnostic run attempted; timeout and capture failure retained.
- [x] Fixed finalization hook to match original cleanup's verified container ID,
  not only the candidate's name; foreign/original-service targets rejected.
- [x] No-model explicit-stop smoke produced a valid finalized report.
- [x] One corrected guarded attempt made; memory guard blocked before model load.
- [x] 137 focused offline tests passed, original services restored/healthy/4 idle.
- [ ] Complete model CUDA trace, exact long API/kernel attribution and measured fix.
- [ ] Uninstrumented real model semantic/latency qualification and host acceptance.
- [ ] Existing enterprise/security/role/device/release gates; BUG-DATA-0001 OPEN.

## Actual executions — keep distinct

### Device-only trace and conversion

`e5r47-13aca543a78f`: exit0, GPU enumeration only, 0 model loads/inference calls;
default container isolation, no CPU sampling/context-switch capture. Raw
`D:\PENTA-Models\operations\e5r47-trace-probe\device.qdstrm` retained. Initial
target-folder-only mount omitted the packaged importer: raw capture succeeded,
automatic conversion did not. Complete package mount fixed the dependency layout.

Offline importer `e5r47-f15743243a0a` exit0 produced `device.nsys-rep`.
Offline export `e5r47-0a34a95ad0f9` exit0 produced `device.sqlite` under
`D:\PENTA-Models\operations\e5r47-offline`. Original container snapshots unchanged.
The actual read-only analysis sees 130 runtime API events; largest aggregate is
one `cudaMemGetInfo_v3020` at829.278717ms. No model kernel/JIT conclusion follows.

### First instrumented model run — not accepted

Run `e5r36-7f85fa2dfce7`, lifecycle216.12s, original guard passed. Same pinned
2B weights/runtime/profile/128batch/context2048/output512/four native threads;
no EAGER loading override. Actual offload/runtime/cache verified;12 token fits.
First660-token request30.064s, `native_uncertain`, no complete raw plan, quarantine
stop,11 remaining cases unrun. One request attempt is not one completed answer;
no accuracy percentage, model qualification or uninstrumented speed result.

Wrapper defect: finalization hook expected `docker stop ... <name>` while the
retained lifecycle uses the verified container ID. Hook never ran; cleanup still
restored original services, but killed capture before report completion. The
67788-byte temporary raw file remains intact. Recovery importer
`e5r47-3b38efd82d53` exit3 reports `IncompleteFileException`; it is NOT a valid
model trace and is not used for cause attribution. No result was silently repaired.

### Finalization repair and no-model proof

`run_gpu_trace_e5r47.py` now records the actual owned container ID and finalizes
only when original cleanup targets that ID/name. Single-attempt stop, bounded45s;
no foreign target, no elevated privileges, no changed evaluation/health budget.
Regression tests cover name/ID matching and rejecting original/foreign containers.

No-model smoke `e5r47-4e0061052a65`: explicit `nsys stop --session=<owner>` exit0,
valid `stopped.nsys-rep`, original snapshot unchanged. This validates collection
finalization, not inference. Temporary test container only was removed.

### Corrected guarded attempt — no model executed

Run `e5r36-f6a2a1cd9036`, lifecycle79.60s. Required3093922048bytes and three
consecutive sufficient samples in the original45s window. Samples:
1518333952,2293497856,2296631296,2316013568,2356305920,2361966592,2282233856,
3257647104,3399192576. Only the last two passed: **BLOCKED_MEMORY_GUARD**.
0 model loads,0 inference/tokenizer calls,12 unrun, no candidate/trace/cache
directory. Do not describe this as another semantic failure or completed trace.
No automatic retry, Docker restart, process termination or guard relaxation.

## Verification and preservation

Final focused suite: **137 PASS /1.62s**; JUnit
`D:\PENTA-Models\operations\e5r47-checks\final.xml`.
Includes retained94, acquisition/extraction17, trace-wrapper20, analyser6.
Earlier131 PASS/2.25s and20 repair-specific PASS/0.17s are narrower checkpoints,
not additional independent tests added to137. Larger enterprise suites NOT rerun.

Both model lifecycle receipts confirm all25 original containers/config/state and
frozen staged sources restored/unchanged. Extended wrapper confirms original
host/mount snapshot. Four idle native slots; PENTA8000 `/health`200. Owned model
containers absent after cleanup. Evidence/caches/packages retained privately on D:.
Source helper corrections after the first frozen stage are intentional; original
stage and failed trace remain unchanged. No evaluator/oracle change.

Academy3000/5092 still not listening; no authenticated UI/login acceptance.
No source/default/weights/roles/security/DB/credentials/main/staging/commit/push,
Azure provisioning/deployment, driver/kernel/global installation or restart.
`QA/EVIDENCE` stays local, `.build-check` ignored. BUG-DATA-0001 remains OPEN.

Final independent live inventory:25/25 original IDs/images/config hashes/running
states match the corrected attempt's initial inventory; only original API and
inference containers running. Both worktree `git diff --check` exit0,staged0;
Academy HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`,Mini HEAD
`2f8806967709a1a7ec29e5176e7c31968adbf6aa`,untouched main HEAD
`f7af512f884ca5fe8ac3ddb287c852492592e709`. QA/EVIDENCE tracked0.

Key immutable evidence SHA256:

| Artifact | SHA256 |
| --- | --- |
| First model lifecycle `7f85fa2dfce7/trial.json` | `66322d25adc11c6d08c6a9c5610118f6949866a516912ea4b45508671327fcfd` |
| Guard-blocked lifecycle `f6a2a1cd9036/trial.json` | `41d10d63485216dd9e0408bfb07a88bbc9fb88175fb7e2eca450572c7a5048ad` |
| Focused final JUnit | `32a26987be07610d6d9b9df3fe7ffe57d6428588ba12fb175d5b9b54e627487a` |

## Next execution, not another tuning loop

1. Sol High: check local memory and idle services; no automatic memory-reset action.
2. Once the unchanged reserve passes, use the repaired wrapper for one complete
   CUDA model trace. Device finalization prerequisite is already evidenced above.
3. Export/analyse that actual model report using the read-only analyser. Attribute
   long compiler/module/API/kernel waits before changing the runtime. Do not use
   the device-only report or incomplete first capture as substitute evidence.
4. If tracing shows PTX compilation dominates, prepare a bounded same-revision
   sm75-native build; pin source/toolchain/resources and validate resulting cubins.
   No compiler/toolchain image acquired or runtime replacement built this batch.
5. A supported fix must then pass the unchanged uninstrumented30s/semantic gates;
   retain existing broader host/domain/security/portal/device/pre-Azure program.

All initial tier choices remain unqualified/unavailable. No larger model,
paid Azure or production claim is justified by diagnostic tooling success.
