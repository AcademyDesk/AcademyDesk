# E5-R37 — approved restart and actual GPU trial

19 October 2026. Owner explicitly approved one Docker restart to release WSL
cache and a bounded local GPU test if unchanged guards passed.
**RESTART/RESTORATION PASS; FULL GPU OFFLOAD VERIFIED; MODEL NOT QUALIFIED.**

## Restart and preservation

Recorded local immutable inventory at
`D:\PENTA-Models\operations\e5r37-restart\before.json`, then verified four idle
native slots. Stopped only the original two PENTA containers, stopped Desktop
normally, observed docker-desktop WSL Stopped, started Desktop once and restored
the same two containers. No WSL shutdown/unregister, drop_caches, settings edits,
pruning, driver install, unrelated-app termination or additional Docker restart.

Windows available bytes: before1367404544; immediately after stop3103490048;
post-restart inventory4842336256. Later readings fluctuated; these are snapshots,
not a guaranteed permanent reserve. Existing3093922048byte host guard required
three sufficient consecutive samples and passed for both candidate launches.
GPU reserve guard,5.5GB placement policy, context2048/output512/four threads/one
slot/30s request deadline, exact scoring and quarantine remained unchanged.

Final receipt confirms exact25 original container identities/config/host/mount
hashes/running states, original listed image metadata,41 volume metadata records
and separately pinned CUDA image/config/rootfs. Network names and PENTA/host/none
IDs retained; only built-in bridge ID regenerated. No database-content comparison
is claimed. The private D: recovery copy from R35 remains untouched.

## First post-restart launch — verification failure retained

Run `e5r36-399a762bec70`, process exit0/250.13s,51 frozen source pins verified.
The unchanged R36 runner uses its existing e5r36 prefix; this is R37 work, not a
rewritten old receipt. Actual CUDA model reached readiness at126.05s and used
about1301MiB GPU memory, but default verbosity3 omitted the required full-offload
log line. The strict verifier stopped before generation: FAILED_TRIAL_RUNTIME,
RuntimeError,0generations. Original services restored/four idle slots/owned cleanup
verified. Do not relabel this run as a passed model evaluation.

## Diagnostic repair and real inference

Added `operations/tools/run_gpu_e5r37.py` in Mini. It delegates to the existing
R36 lifecycle and changes only launch diagnostics (`-lv 5`), not model, prompt,
resources, offload setting, oracle or safety. New wrapper checks the first receipt
had0generations and exact restoration; it shortens the lifecycle/evaluation to the
remaining original15minute window. No second Docker restart or parameter sweep.
The previous stage is preserved. Increased logging can affect timing; this is not
a clean hardware speed comparison or evidence that every GPU configuration fails.

Diagnostic run `e5r36-046c6840ff55`, process exit0/288.71s,52 frozen stage pins.
Combined first-plan → final-receipt file timestamps span564.10s, below900s;
the diagnostic lifecycle was shortened to630s. Independent final rehash of all
51/52 staged files passed. Actual runtime checks:

- Same official CUDA12 b11382 image/manifest
  `e8318a7b3988f9ca57b28c03486569e60def08f2b7b060550ebfa046e175e6cb`.
- Same2B Q4_0 artifact hash
  `cd70221bebaee0503e0f6717e174250cd7825aa88438b3aabec9ad55731d9bb1`.
- Explicit GPU0, network none, read-only root/model bind, dropped capabilities,
  no-new-privileges and existing host/container memory bounds.
- Native log explicitly reports **offloaded25/25 layers to GPU**. No CPU or partial
  fallback. Actual gpu-runtime receipt:1301MiB used/2638MiB free after loading,
  total4096MiB. Native readiness at129.42s. These prove this bounded load/offload,
  not general model/architecture/production qualification.
- All12 actual tokenizer/template checks fit. First prompt F29-01 has660 tokens.
- **One generation dispatched; no complete raw response by30.063s.** Native gate
  quarantined and stopped;11cases unrun.0completed replies/0exact results, not a
  measured0/12 accuracy score. No output repair, retry/fallback, deadline extension
  or synthetic acceptance substituted for the missing reply.
- Logs locate the delay after launching the660-token prompt in native decode;
  no completed prompt-eval/decode timing exists. Do not claim measured TTFT,
  tokens/second, semantic accuracy, warm-request speed or a definitive root cause.

Both runs' owned containers were stopped/logged/removed after label/identity
verification; no original container was removed. All original containers restored,
original Mini API/native healthy/four idle slots, no owned leftovers. Academy
login3000/API5092 and Mini API8000 health checks200; normal API DLL unchanged.

## Tests and evidence

38 focused offline tests PASS/2.05s:35 retained checks plus3 diagnostic-only
regressions. No serving/domain source changes. Prior386 Python/461SQL-API/29browser
evidence retained, not rerun or claimed as new release acceptance.

| Receipt | SHA256 |
| --- | --- |
| First trial.json | 9801b754ae57339d95545a3753aa8f5792993e08bd2dc2d60602be686ccf86c0 |
| Diagnostic trial.json | bf5c1986583c99fd77005a62f7adf935e9fbc9962edbe228568147d9eab95f39 |
| Diagnostic evaluation.json | e5aa8fe133e194d9a0f1d61b9e8cda6cfc025498419dad6d1393918e1c8c7ac8 |
| gpu-runtime.json | dfc21a3a387f35952689e91f45992c61b9040820c0ee0863dcd63c0c47a10a90 |
| Restart python-final.xml | 2415e8fae6eb729221997a590581596eb9f61dd1f2348516a89c008d80963115 |

Trial receipts/logs remain under Mini operations/evidence with the run IDs above;
restart inventory/JUnit remains D:/PENTA-Models/operations/e5r37-restart. No existing
failure evidence overwritten. Documentation updated in both projects, no staging/
commit/push/main changes, QA/EVIDENCE tracked0/.build-check ignored/diff checks PASS.

## Next Sol High: targeted runtime diagnosis, not another blind run

Read the pinned native path/source, prompt/decode timeline, batch/microbatch,
recurrent-kernel/backend/resource evidence and logging overhead. Distinguish slow
prefill, decode, CPU fallback within graph operations, cold initialization and
instrumentation before proposing one changed measured experiment. Full layer
offload alone does not prove fast request execution. Do not repeat this identical
trial, lower guards, relax30s, try larger models, train, promote or provision Azure
automatically. No new model is required just to investigate the existing failure.

Model admission/read/operational/history/host/domain/role/Manual/device/provider/
privacy/enterprise/release gates and BUG-DATA-0001 remain OPEN. No app/engine/default/
model weights/credentials/DB/schema change or Azure deployment. This batch is not
production readiness,90%AI delivery, or proof that the laptop can never qualify.
