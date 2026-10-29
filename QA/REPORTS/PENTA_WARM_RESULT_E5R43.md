# E5-R43 — approved warm-cache retry, actual inference

19 October2026. Route Sol High. **EXECUTED / NOT ACCEPTED.** Owner continued
the explicit request to retry R42 once with unchanged guards. Used the existing
prepared wrapper unchanged. No code changes/new tests/restart/priming/download/
model change/guard relaxation/Azure. R42 memory-blocked result retained separately.

## Actual bounded result

From `D:\PENTA AI Models`, once:

```powershell
python operations/tools/run_gpu_warm_e5r42.py --run
```

New run `e5r36-8197f52f5f2c`, observed exit0, receipt elapsed161.74s.
Exit0 proves result capture/restoration, NOT model acceptance.57frozen AND current
source pins independently rehashed:0drift. R42's83offline tests retained, NOT rerun;
no fresh broader Python/SQL/browser/enterprise validation claimed.

Same verified2B Q4_0/official CUDA12 b11382/compact-v3/profile/driver610.60/T1000,
batch128/128/context2048/output512/four threads/one slot/30s/scoring/quarantine/
12exposed F29 cases/900s lifecycle. Original source cache and fixed R41receipt
hashes matched before attempt and after restoration. Dedicated D:copy exactly
matched20files/69789152bytes/fingerprint before native launch; source never mounted
writable. Actual cache mount/environment/isolation/batch/offload verified before
generation. GPU1271MiB used/2668free after load,25of25 layers offloaded; CPU-mapped
embedding remains, not an all-GPU-operation claim.

Unchanged3093922048byte host guard samples:
2957864960,3026411520,3080900608,3122794496,5045321728,5049102336.
Last THREE passed, unlike R42; no sampling-window extension or Docker restart.

All12 actual tokenizer/template checks fit. First F29-01 input660tokens:
**one call30.063s/no complete raw reply/STOPPED_NATIVE_QUARANTINE/11unrun**.
No accuracy measured, no operational subset pass, no silent repair/replay/fallback.

## New native progress evidence — not a complete response

Model ready36.570542s from native log start. Warm-up marker window
35.381901→36.536739s (1.154838s); automatic fitting7.61s. These are startup,
outside the30s request limit. Earlier readiness than R41 is not a controlled
causal performance benchmark: cache and host history differ.

First native decode42.235463s; first returned work yield58.126493s. Prompt progress
then advanced128→256→294→422→532→656tokens;656processed at60.102572s,
reported17.87s prompt processing. Next substantial work interval60.512301→
72.045499s. First sampled token logged72.046862s, about29.811399s after native
dispatch; second72.181963s. Cancellation72.232880s; third token sampled during
teardown72.298652s. The provider received no complete raw message.

Thus this run differs from cold R41: native work did return, prompt processing
advanced and two tokens were sampled before cancellation. This is NOT measured
HTTP streaming TTFT, a full answer, semantic success or approved action.
Do not extrapolate output throughput from two tokens. No exact per-kernel/JIT
timing attribution or confirmed deadlock/root cause exists.

## Cache provenance and outcome

| Phase | Files | Bytes |
| --- | ---: | ---: |
| Verified copy before native launch |20|69789152|
| Ready before generation |20|69789152|
| After evaluation before cleanup |22|80824657|
| After original restoration |22|80824657|

Two additional files/11035505bytes appeared. This is accurately a **pre-populated
compiler-cache** comparison, not proof that all request kernel variants were warm.
Providing the retained cache did not achieve a complete reply within30s. No next
cache retry/priming/request replay is authorized by this result. Source R41cache
still exactly20files/69789152bytes/fingerprint
4b4146f39e252e7e1aa1e6adfeed4a10669eac147c981bd0bf610823346fe591.
Generated executable caches retained privately on D:, never Git/upload/training.
Cold R41failure remains, no production/default activation.

## Restoration and evidence

Only exact run-owned candidate/health/evaluator containers removed with logs
retained.0owned leftovers;25original container identities/config/state restored,
extra host/mount snapshot PASS. Native healthy/four idle slots/PENTA8000 health200.
Academy3000/5092 already stopped/remain stopped; no app/SQL/credentials/bootstrap/
migration change or Admin login acceptance. Both HEADs/main unchanged, staged0,
diff checks PASS, QA/EVIDENCE tracked0/.build-check ignored; unrelated WIP preserved.

Mini evidence root `operations/evidence/e5r36-8197f52f5f2c`.
Cache records `D:\PENTA-Models\operations\e5r42-warm\e5r36-8197f52f5f2c`.
Delegated R42/R36 naming and approval text retained; this is the separately
approved R43 retry, not an overwritten R42receipt or newly authored runner.

| Receipt | SHA256 |
| --- | --- |
| trial.json |c63070468594c9429937b8a064686e5f3852e6394b5b313840959e0d6a1f71d4|
| gpu-runtime.json |1fa6fc690b63bde03f6d8d1f707e616c560c1a40a0b4596e1c45bd3a5453c46e|
| qwen35-2b/evaluation.json |d34cc958b5d90561c96fa9ea3c4a64d1da668fac577d2264e95cfd30d7984f4b|
| Native log |2b11e7121ee8bf989a465c3ac2f49b7d84b3b72c0233efea2c10f0ba9bb1414d|
| Cache directory cache-metadata.json |40239fc52a00c79230e7889ba970bdf80d2d69f73757c371033dd5c80671b6a2|
| warm-seed.json |706443c8a8f91de1f56416fee791c0dc6cac3af51f81333b214868192a6a910c|
| source-preservation.json |8ee26b3fe5fe284dbbffcbe7e2224e403ab24e688d2e22ff8afbaa9f92ae703c|

## Next route: no more blind cache retries

Sol High: read-only examination of the two long native work intervals and cache
metadata timing, then a specific profiling/initialization proposal if warranted.
Do not rerun R41/R43 unchanged, keep recursively copying growing caches, relax30s,
alter exact scoring or jump to paid hardware/model size as an assumed fix.
Any new active diagnostic/runtime build/service interruption needs its own scope;
no automatic Docker restart/driver/install/Azure action. Existing app-process
restoration and authenticated UI work are separate pending items.

Real read/operational/semantic/host/history/domain/role/Manual/device/provider/
privacy/enterprise/release gates and BUG-DATA-0001 remain OPEN. Production NOT READY.
