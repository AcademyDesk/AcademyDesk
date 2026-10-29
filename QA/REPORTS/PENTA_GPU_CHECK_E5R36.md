# E5-R36 — CUDA imported; GPU enumerated; model trial RAM-blocked

19 October 2026. Continues approved local GPU path after E5-R35, not Azure.
**RUNTIME IMPORT VERIFIED / GPU DEVICE CHECK PASS / GENERATION NOT RUN.**

## Delivered and verified

- Added offline import verifier and isolated device-check/trial helpers in Mini;
  no serving/default/engine/model/Academy application changes.
- Pinned official CUDA12 b11382 linux/amd64 manifest
  `e8318a7b3988f9ca57b28c03486569e60def08f2b7b060550ebfa046e175e6cb`,
  source revision `11fe02151f79c41d0d4af7da708755d73b9c0da6`.
- All13 compressed blobs and expanded tar diff-IDs independently verified against
  the pinned manifest/config. Expanded bytes4371045376; conservative calculated
  free-space requirement24664090214bytes, with D:139.55GiB initially available.
  No extraction of arbitrary archive paths to the host.
- Imported one OCI tar from D: into Docker's confirmed D: working-data location.
  Tar retained at `D:\PENTA-Models\operations\e5r36-cuda\llama-b11382-cuda12-oci.tar`,
  bytes2592348160, SHA256
  `10f1d99fa7622000a31479df47bfb42a31f504457b0300716c400bfe6ed9d22e`.
  No network download, moving-tag pull or deletion of old CPU image/tags.
- First import helper exited1: Docker load completed, but verification assumed
  config digest was the image ID. This containerd OCI store uses manifest identity.
  Failure retained in `INITIAL-IMPORT-FAILURE.md`. Corrected verifier and new
  regression tests; `--inspect-existing` verified existing exact descriptor,
  amd64/Linux, entrypoint/revision and all13 rootfs diff-IDs, exit0/104.78s.
  **No second import**. Config digest remains
  `fab69fb6698b1029c7f8ca96d7f635dfe4386c48056abf8348bd530f0e8ab3aa`.
- One isolated device check, run `e5r36-f954da206c67`: explicit GPU device0,
  network none, read-only root, caps dropped, no-new-privileges,512MiB host bound,
 45s ceiling, no model mount/load/generation. Exit0/5.47s; actual output
  `CUDA0: Quadro T1000 (4095 MiB, 3297 MiB free)`.
  Owned check container removed after recording logs; original container identity,
  config/host/mount hashes and running states preserved exactly.

## Model-trial result: blocked, not failed model quality

Run `e5r36-a1da046126c4`, observed process exit0/101.47s. Planned same pinned
Qwen3.5-2B Q4_0/compact-v3,12 exposed F29 regressions, full offload requested99,
2048context/512output/4threads/one slot/30s per request,15min lifecycle ceiling.
Reuses the existing exact evaluator, memory guard, native admission/quarantine,
owned cleanup and original-container restoration without modifying their source.
New launch adapter requires actual full GPU offload before any generation; no
CPU or partial-offload fallback, automatic sweep or deadline/scoring relaxation.

Actual status **BLOCKED_MEMORY_GUARD**. Same host requirement3093922048bytes;
nine available-RAM samples ranged1419239424–2013147136bytes, all below guard.
**Zero candidate model loads, zero generations; all12 cases unrun.** GPU kernel
execution, fit, latency and correctness are therefore NOT established. Source
artifact hash verified `cd70221bebaee0503e0f6717e174250cd7825aa88438b3aabec9ad55731d9bb1`.

Runner verified51 frozen stage pins, restored all original container identities/
configuration/states, original native four idle slots and healthy baseline.
No owned trial containers left. Academy login3000/API5092 and Mini API8000 healthy.
Read-only follow-up: vmmemWSL working set6.08GiB; WSL Cached6049712kB and
MemAvailable6158760kB while Windows had insufficient available RAM. Inference:
retained WSL cache after import likely contributes; it does not prove a model
needs more physical RAM or that the GPU is incompatible.

## Tests and retained evidence

35 focused offline tests PASS/3.27s: new import/device/launch safety checks,
retained archive tests and existing CPU4-wrapper checks. Final JUnit
`D:\PENTA-Models\operations\e5r36-cuda\python-gpu.xml` SHA256
`56e06231201e63a6da792248ecb1d5403208feb30abe0f515b53a7a3d62b0e64`.
Earlier18/20/22 test checkpoints retained;35 is the final selected total, not new
runtime generations or full-suite coverage.

Import receipt SHA256 `1b3655f54f94339a6028fcdc82c51e32bccd42c3995f561a229bae3540c2646b`;
device receipt `f99140d2f2a06480f03a0ce93a4d558fbbef1eb4acb16e044124ac09b2bde8ef`,
both under `D:\PENTA-Models\operations\e5r36-cuda`.
Trial receipt `D:\PENTA AI Models\operations\evidence\e5r36-a1da046126c4\trial.json`,
SHA256 `5dfb541d49a51de8c3ee97cd489a1d5bd3301344e7a6063a9698e7001772d51a`.
The failed first import verifier and RAM-blocked trial are retained, not relabeled
as inference success. Prior386 Python/461 SQL-API/29 browser evidence NOT rerun.

## Next route and boundaries

Sol High. Requested separate permission for one supported Docker restart to
release WSL cache, preserve all data/containers, restore original service state,
then one bounded GPU retry ONLY if unchanged RAM/GPU guards pass. At this
checkpoint permission is awaiting response; no restart/settings/cache-drop/
unrelated-app closure or repeat trial performed. Do not lower the guard, run
WSL drop_caches, change memory settings or provision Azure automatically.

No application/model defaults/training/promotion/production/credentials/DB/schema
changes, staging/commit/push/main or Azure. Active Docker data and new artifacts
remain D:, recovery copy retained. Post-import D:130.79GiB/C:23.53GiB free snapshot.
Both project handoffs updated. All real-model admission/read/operational/history/
host/domain/role/Manual/device/provider/privacy/enterprise/release gates and
BUG-DATA-0001 remain OPEN. This is not production readiness.

Official context: [Docker Windows GPU passthrough](https://docs.docker.com/desktop/features/gpu/)
and [same-revision CUDA build](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/.devops/cuda.Dockerfile).
