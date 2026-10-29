# E5-R46 — pinned native diagnostic tools and actual CUDA binary inspection

Recorded 20 October 2026, continuing the 19 October local diagnostics.
Route: Sol High / Codex, local-only, additive operational tools in the existing
Mini worktree. No application, model/default, driver, database or Azure change.

## Completed

- Downloaded only two pinned official NVIDIA diagnostic packages directly to
  `D:\PENTA-Models\diagnostics\e5r46-tools`; no global installation or full toolkit.
- `cuda-cuobjdump-12-8_12.8.90-1_amd64.deb`, 208398 bytes,
  SHA256 `f2b5902466653c6d4fed0d82af136447bf2701c51410dab1798792f9beb10340`.
- `NsightSystems-linux-cli-public-2026.3.1.157-3804839.deb`, 215148110 bytes,
  SHA256 `3eb87ec08e5f8b8f153537847747bd5cfabb51b9c8793873b26a3c55dc813ad1`.
- Bounded acquisition/extraction validates hashes, paths, internal links, storage
  and download limits. Initial Nsight extraction failed on chained library links;
  recovery reverified all existing regular files against the same pinned archive
  before completing only missing internal link copies. No destructive re-extract.
- Actual `cuobjdump --list-elf` and `--list-ptx` inspection of
  `/app/libggml-cuda.so` in the unchanged pinned official runtime:
  `sha256:e8318a7b3988f9ca57b28c03486569e60def08f2b7b060550ebfa046e175e6cb`.

## Actual finding

The inspected library contains ELF/native code for **sm86, sm89, sm120a** and PTX
for **sm50, sm61, sm70, sm75, sm80, sm90**. There is no sm75 native cubin in this
library. This confirms the PTX-only path for this laptop's sm75 GPU in this
artifact; it does not quantify JIT time or prove the entire timeout's cause.
No inference or model load was performed by the binary inspection.

Evidence: `D:\PENTA-Models\operations\e5r46-inspection\inspection.json`,
`elf.txt`, `ptx.txt`; run `e5r46-59a8dbebad89`, original container snapshot unchanged.

## Profiler compatibility checks and retained failures

Initial `/tools/nsys` mount was rejected because the CLI requires its expected
`target-linux-x64` installation layout. Correct-layout version/profile-help probes
both passed, actual version `2026.3.1.157-263138048394v0`. Original failed probe
retained separately in `e5r46-nsys-probe`; successful probe in
`e5r46-nsys-layout-probe` under the same D: operations root.

R47 device-only tracing initially emitted raw QDSTRM but could not convert it
because only the target folder was mounted. Mounting the complete pinned package
exposed the included `QdstrmImporter`; offline conversion passed without GPU,
model load, network, added capabilities or host security changes. `nsys import`
is not a supported command in this CLI; its rejected help probe is retained.
Actual import used the packaged importer, not an invented nsys command.

## Scope and acceptance

Packages and trace evidence remain on D:, not in Git. All temporary tool
containers were ownership-checked and removed; original services were unchanged
by these probes. CPU sampling/context-switch collection disabled. No SYS_ADMIN,
privileged containers, driver/kernel/security changes, paid resources or restart.

Focused combined tool/runtime suite subsequently passed **137 tests** in R47,
including 17 acquisition/extraction tests,20 trace-wrapper tests and6 analyser tests. This is
offline diagnostic coverage, not model quality or enterprise acceptance.
See `PENTA_NATIVE_TRACE_E5R47.md` for the separate model run and finalization fix.

Official references: [NVIDIA binary utilities](https://docs.nvidia.com/cuda/cuda-binary-utilities/index.html),
[Nsight CLI documentation](https://docs.nvidia.com/nsight-systems/UserGuide/index.html),
[CUDA package index](https://developer.download.nvidia.com/compute/cuda/repos/ubuntu2404/x86_64/Packages),
[Nsight package index](https://developer.download.nvidia.com/devtools/repos/ubuntu2404/amd64/Packages).

No model qualification, full app/portal completion, enterprise testing closure,
BUG-DATA-0001 closure or Azure readiness is implied.
