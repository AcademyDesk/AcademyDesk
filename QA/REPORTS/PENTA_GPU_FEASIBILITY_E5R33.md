# E5-R33 — read-only local GPU/runtime feasibility

19 October2026. **ASSESSMENT COMPLETE / GPU TRIAL NOT AUTHORIZED OR RUN.**
Continues E5-R32, not another CPU inference trial. No application, engine,
configuration, driver or container-state change. Academy branch `codex/penta-search`
HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`; Mini branch
`codex/academy-language-contract` HEAD `2f8806967709a1a7ec29e5176e7c31968adbf6aa`.
Dirty work retained; no stage/commit/push/main/Azure.

## Observed local facts

| Check | Actual observation | Limit |
| --- | --- | --- |
| GPU | Quadro T1000,4096MiB total,3938MiB free at observation,compute capability7.5,driver610.60 | Point-in-time free memory, not guaranteed inference capacity |
| Docker | Linux/amd64 daemon29.8.2; `nvidia` runtime listed | Runtime registration does not prove GPU passthrough |
| WSL | docker-desktop running with WSL version2 | No driver/kernel update performed |
| Cached native | CPU image `559ac229...`, b11382/revision11fe021... | Existing native container has no DeviceRequests |
| Device enumeration | Existing native binary `server --list-devices`: `(none)` | This CPU/non-GPU-requested container does not establish GPU incompatibility of the laptop |
| Cached libraries | `/app` contains CPU backends, no CUDA backend listed | Cached image cannot simply become accelerated by increasing GPU-layers |
| Existing API container | `/dev/dxg` and `/usr/lib/wsl/lib` absent | Expected non-GPU container observation, not a host passthrough failure |
| Cached images | No CUDA llama.cpp image listed | New runtime acquisition required before testing |
| Disk | C:8.9GiB free; D:171.2GiB free | These are snapshots, not a pull-space guarantee |
| Docker data file | `C:\Users\Admin\AppData\Local\Docker\wsl\disk\docker_data.vhdx`, file length14.6GiB | Logical file length, not independently measured physical allocation or internal free space |

No GPU workload, temporary GPU container, model load, sample benchmark, driver/
CUDA toolkit install, WSL update, image pull/build or service pause/restart was
performed. Device enumeration ran a second short-lived command in the existing
native container with no model argument; existing service remained running.

## Official compatible-runtime candidate — metadata only

Docker documents Windows NVIDIA GPU passthrough using the WSL2 backend and an
explicit GPU device request. Our environment has those prerequisites partly
observed, but still needs actual container-device verification.
[Docker GPU documentation](https://docs.docker.com/desktop/features/gpu/).

The exact existing llama.cpp source revision documents separate CUDA images;
its CUDA Dockerfile enables GGML_CUDA, with CUDA12.8.1 build/runtime bases.
This avoids immediately changing both model and llama.cpp source version.
[Pinned Docker documentation](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/docs/docker.md),
[pinned CUDA Dockerfile](https://github.com/ggml-org/llama.cpp/blob/11fe02151f79c41d0d4af7da708755d73b9c0da6/.devops/cuda.Dockerfile).

Registry metadata fetched using `docker buildx imagetools inspect`, no layers pulled:

- Official tag observed: `ghcr.io/ggml-org/llama.cpp:server-cuda12-b11382`.
- Index digest: `sha256:ef08b5a98b1170f2b62177be0a4027c88a84043c55afbf190dd18b9e7cdcfebf`.
- **linux/amd64 manifest digest**:
  `sha256:e8318a7b3988f9ca57b28c03486569e60def08f2b7b060550ebfa046e175e6cb`.
- Config digest: `sha256:fab69fb6698b1029c7f8ca96d7f635dfe4386c48056abf8348bd530f0e8ab3aa`.
- Remote labels report b11382, source revision
  `11fe02151f79c41d0d4af7da708755d73b9c0da6`, CUDA12.8.1, linux/amd64,
  `/app/llama-server` entrypoint. These are reported metadata, not independent
  binary/architecture execution verification or supply-chain/security acceptance.
-13compressed layers total **2592290611bytes** (~2.59GB decimal/~2.41GiB).
  Actual transfer may share cached layers, but unpacked size/scratch space and
  Docker VHD growth are unknown. Do not claim this is the total disk requirement.

**Inference, not qualification:** T1000 plus an official CUDA12 runtime is a
plausible local2B evaluation path. No claim it will fit full offload, meet30s,
produce correct edits, or support this GPU architecture until checked. A missing
host `nvcc` command does not itself establish container-runtime incompatibility;
no host toolkit installation is proposed as a prerequisite here.

## Safe next step requires explicit scope approval

Request permission for **one official digest-pinned CUDA image download and one
bounded local2B GPU evaluation**, not a driver installation or deployment:

1. Recheck storage before any pull. Determine sufficient unpack/cache/scratch
   space plus OS reserve. C:8.9GiB is not accepted as proven sufficient. Stop if
   safe space cannot be established. Do not prune Docker, delete evidence/files,
   move the Docker data file or change settings without separate direction.
   D:free space is not automatically usable by Docker's current C:data location.
2. Pull only the linux/amd64 digest above, not moving `latest`, new models or a
   different runtime version. Preserve old CPU image/tags and every baseline
   container. After pull, independently inspect runtime/device enumeration and
   confirm compute7.5 support. No driver/kernel/Toolkit update automatically.
3. One explicitly GPU-requested, owned isolated compatibility check before model
   loading. If passthrough/build compatibility fails, STOP with its evidence;
   no repeated starts or GPU-fallback hidden behind CPU labels.
4. If compatible, same2B Q4_0 artifact/compact-v3/context2048/output512/one slot/
   30s strict bound/exact scoring and actual host-memory guard. Record GPU free
   memory and actual allocation/offload; GPU memory plus KV/workspace/desktop
   reserve must fit. No automatic partial-offload experimentation to find a pass.
   Review one exact offload setting before loading, not a parameter sweep.
5. At most12exposed regression generations/15minutes for local test after image
   acquisition; stop on first uncertain outcome. Original PENTA idle-baseline
   pause/restore, owned labeled cleanup/log preservation and fresh state checks.
   No Academy process/data/password changes, external inference, Azure or charges.
   Download time/bandwidth/storage still exist; zero new cloud spend is not zero
   resource cost or a promise of success.
6. Capture speed and exact correctness independently. Any success remains a
   limited exposed-regression result; original94read/operational/independent-new/
   security/host/SQL/browser/role/device/enterprise gates still required.

This packet does not grant approval or run those steps. Stay **Sol High** for
runtime/authority decisions. Existing frozen trial evidence and code tests are
retained; **no new pytest, SQL/browser or generation test was run** for read-only
feasibility. No point rerunning386code tests when no executable code changed.

Final developer login3000/API5092health200; both PENTA baseline containers running,
native healthy. HEADs/staging unchanged, local evidence preserved/build ignored.
CORE-01 and BUG-DATA-0001 remain OPEN; full app NOT production-ready.
