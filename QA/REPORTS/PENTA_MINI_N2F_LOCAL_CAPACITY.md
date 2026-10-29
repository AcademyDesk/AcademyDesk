# N2f — bounded local inference coordination

2026-10-08. Sol High / Codex implementation packet, existing branches
`codex/penta-search` (Academy `d3c4ccf`) and
`codex/academy-language-contract` (Mini `a50bbd3`). No commit/push/merge/Azure.
This report covers synthetic local capacity only; linked correction/reset
SQL/browser and production throughput/recovery remain separate OPEN gates.

## Reproduction and bounded design

One fixed simultaneous HTTP pair uses unchanged N2-08/N2-09 from the frozen
26-case fixture, SHA
`c6f147ce72d68e10e1fa45c42c06b7d49623434aa1889ab894005be5d0462174`.
Baseline 0.1.6/planner-0.7: **0/2 exact**, both HTTP 200 protocol ERROR,
"Inference is unavailable.", no tool, state unchanged, about 17.539s each.
Backend logs show both tasks active, failed free-space/KV-cache decoding,
then "Context size has been exceeded." The configured 2048/four-slot runtime
does not safely handle this overlap. This is observed resource exhaustion,
not an asserted per-slot context division or evidence that each prompt is too big.

Mini-only changes: `inference_gate.py` + provider integration; no planner,
tools, arguments, authorization, host API/frontend, SQL or model changes.
Linux advisory file locks coordinate provider instances/processes sharing
the same container/filesystem and exact normalized backend URL. One active
inference, at most two waiters, 30s queue deadline. Waiting is bounded, not
FIFO or a throughput guarantee. Caller cancellation while waiting does not
dispatch; cancellation after admission leaves a shielded worker owning the
lock until the response is consumed or its deadline expires. A worker cancelled
before its body starts releases its unused lease without dispatch.

Dispatched work has an **80s total transport deadline**, replacing the old
120s per-operation HTTP timeout; no retry. Combined queue/work bound is
approximately 110s plus small coordination/parse overhead, inside the
unchanged Academy 130s HTTP deadline. Temperature 0, max_tokens 256,
response schema/streaming, llama image/command/context/weights stay unchanged.
Definitive HTTP errors and complete malformed replies release the lease safely.
An uncertain transport/worker-cancellation outcome persists a non-sensitive
fault/inflight marker and denies subsequent inference/readiness. A leftover
inflight marker after process loss also fails closed. Coordination I/O errors
map to safe ERROR, not a prepared tool. Markers contain no prompt/customer data.

## Scope and operational limits

This is a **Linux, single-container local admission guard**, not distributed
production orchestration. Separate containers/filesystems, alternate endpoint
aliases and direct llama clients bypass it. Native Windows Python inference is
not supported by this POSIX guard; the laptop uses the existing Linux Docker API.
Container replacement discards its temporary markers. Before recreating this
API during QA, all probes were complete and all four `/slots` entries verified
`is_processing=false`; inference itself was not recreated. Never treat API
restart or an expired client timeout as proof that backend work stopped.

There is no automatic quarantine recovery or stale-lock stealing. Operational
recovery needs admission stopped, exact backend-idle proof, fault evidence
preserved and approved marker recovery/restart. Distributed admission, durable
fault persistence across container replacement, recovery runbook acceptance,
fair scheduling and production load sizing remain OPEN. The single-container
guard is a safe local mitigation, not unlimited capacity or an Azure release gate.

## Verification and evidence

New regression baseline: original nine tests **2 pass / 7 fail** before provider
integration. Setup import/root-path collection errors and one interrupted test
run are recorded separately, not counted as behavior passes. The new waiting
test has a bounded timeout so an unsupported transport cannot hang the runner.
Original accepted tests and fixture/evaluator expectations remain unchanged.
Final candidate extends to 15 capacity/fault checks: serialization across
instances/processes, queue bound/deadline/cancel, active and pre-start cancel,
worker shutdown cancel, transport faults, HTTP 400/500, malformed JSON, process
loss marker, and coordination/marker I/O faults. All **134/134 Mini tests PASS**
against the final installed Docker package; inherited Starlette/httpx warning.
Unchanged Academy evaluator tests **25/25 PASS**.

Final-image paired trials **3/3 guarded, 6/6 exact**, latencies 9.467/20.812s,
12.076/22.465s, 12.113/22.528s. Separate process/API overlap **2/2 exact**:
HTTP N2-08 25.070s, direct-provider probe N2-09 34.923s (includes queue wait).
All **eight** guarded results independently exact/state/contract/allowlist
regraded against unchanged Academy expectations and actual deployed provider/
gate hashes; not a pooled throughput benchmark. Results in local
`QA/EVIDENCE/penta-mini-n2f-20261008/regrade.json`.
Earlier candidate successes and the failing baseline remain separate files.
The bounded probe executes no connector/domain action. No SQL/preview/bridge
is started, no SQL container removed or pruned, and no production data touched.

Final local API package/service **0.1.7/planner-0.7**, protocol 0.1; installed
provider SHA `ebefa6266d86874b1f713248d90078d5fcb4db3596746174e094ae33de1a2b0b`;
gate SHA `02c22bfb0d9089462f749f163954905e3959692142e8bfc8088067ede777c1db`.
Planner/policy remain the N2e hashes `45dbe60e...` / `826e30b9...`.
Final API image
`sha256:53b47c7514547c66cf5ea8056560fe958f2780c7a5d754d496c3f07ed1a38cff`.
Inference container
`c06a0392290ce1b9198ff49e2bf6d1e9d4c2891b88e115b40e0329dda580aca2`,
image `sha256:559ac229adefe0f7e2d4e32f5222f26927b6bb8ba44b9db08e2544afebf41984`
unchanged. Weights/settings not changed or independently re-attested here.

Docker Desktop became unavailable after final paired trials completed and
before evidence copy/cross-process verification. Started the existing local
installation hidden; same API/inference containers/images recovered, no recreate
or prune at that point. Completed results recovered from the API filesystem;
cross-process test then ran once on the final image. Final readiness 200 and
all four slots idle. This restart is a setup interruption, not a capacity failure
or a demonstrated quarantine recovery. No outstanding owned probe process.

N2e 47 serialized planning observations are retained, not rerun as a new
intelligence benchmark. Historical Academy builds/1,107 API tests retained,
not rerun because no Academy application source changed. Main remains
`f7af512f884ca5fe8ac3ddb287c852492592e709`, untouched; QA/EVIDENCE untracked/local.
Mini serving runtime is an unpublished local candidate, not linked acceptance.

Next Sol High: settle host/runtime provenance, then run prepared N2d correction/
reset SQL and 15-turn browser gates against owned disposable source fixtures.
Do not overlap new capacity/benchmark load with those acceptance journeys.
N1/N2, physical devices, privacy/history, enterprise/P0 and Azure release OPEN.
