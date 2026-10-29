# E5-R35 — approved Docker Desktop working storage relocation

19 October 2026. Owner approved the separately requested C: → D: relocation
and temporary Docker interruption. **RELOCATION COMPLETE; SERVICES RESTORED.**

## Supported move and recovery

Docker Desktop 4.94.0.241994, WSL2/Linux engine 29.8.2. Used the actual Desktop
Settings → Resources → Advanced → Disk image location → Browse → Apply & restart
→ Yes, move it. No hand-move, guessed settings edits, WSL unregister, reset,
prune, image/container/volume deletion or Docker update.

Active disk: `D:\DockerDesktopData\DockerDesktopWSL\disk\docker_data.vhdx`.
Desktop UI and persisted `CustomWslDistroDir` both confirm the D: location.
Docker performed the move; the old C: data VHD no longer exists.

Before changing the setting, stopped only the original two idle PENTA containers,
then stopped Desktop normally. WSL showed docker-desktop Stopped; an exclusive
read-open of the original disk succeeded. Copied the exact VHD while offline to
`D:\DockerDesktopRecovery\e5r35\docker_data.vhdx`, 15635316736 bytes. Independently
hashed source and copy while offline: both SHA256
`0803079dd0eb66ac2e9355fe4b08da1e28de8b4186f815da4793db0f784bfdf8`.
Recovery copy remains; no automatic cleanup. It contains private local Docker
data and must remain local/outside Git, not attached or uploaded as evidence.
Bind-mounted host files are outside this VHD backup and were not moved.

Post-move free-space snapshot: C:23.54 GiB (before8.90), D:139.55 GiB
(before168.76). D: includes both the active disk and the retained recovery copy.
These are observations, not a promise of space required by a future CUDA import.

## Verification and honest delta

Local generated receipts: `D:\PENTA-Models\operations\e5r35-storage\before.json`
and `after.json`. Inventory helper hashes configuration in memory; it does not
record environment values or credentials. Receipts preserve the original state.

- All25 container IDs, names, image IDs, Config/HostConfig/Mounts hashes and
  original running states match exactly.
- All6 image IDs/tags/repository digests match exactly.
- All41 volume names and complete metadata hashes match exactly.
- PENTA custom network, host and none network IDs are unchanged.
- The first strict whole-inventory comparison exited1 because Docker regenerated
  its built-in `bridge` network ID. No container/image/volume mismatch occurred.
  Independent reviewed comparison PASS: network names unchanged, all other IDs
  exact, and changed built-in bridge inspected as name bridge/driver bridge/local.
  Do not describe the entire network inventory as identical or hide this failure.
- Restarted only pentaaimodels-inference-1 and pentaaimodels-api-1, preserving their
  original IDs/images/configuration. Inference healthy, API health200, four native
  slots idle. Startup503 while loading was followed by successful idle-slot probe.
- Existing Academy login3000 and API health5092 both HTTP200 before/after.
  Normal API DLL SHA256 remains
  `049bba230504354ee8245f32a1ffaa01dc0218429b1f30d7f53bc2f21deaa504`.

Receipt hashes: before `af0f2296e68e6b4b86a2ae597a7053fbf7394fe8664ae99f651c559f6c2ac4e5`;
after `5c6f322333fa94160eb15b2c941dd72198922bd36b093b667b316b44bacd8a4f`;
helper `d4cb047971f78084bf8004e32d18a0f20c65b00e99442855d3465dab778539f6`.
No new database content test or complete bind-mount backup is claimed.

## Boundaries / next route

No application/model/default changes, CUDA image import, GPU test, inference
generation, driver/toolkit install, training, model promotion or Azure work.
No stage/commit/push/main edits. Academy and Mini HEADs unchanged. Existing dirty
work/evidence retained; QA/EVIDENCE tracked0, .build-check ignored, diff checks PASS.
Prior386 Python,461 SQL/API and29 browser evidence retained, not rerun for storage.

Next Sol High: reviewed, isolated pinned CUDA import on the now-D: Docker data
store and bounded GPU compatibility trial, with current free-space/unpacked-size
check, existing guards/deadline/scoring and baseline restoration. No paid/Azure
compute, automatic sweeps or relaxed acceptance. Model qualification, host/domain/
role/history/Manual/device/provider/privacy/enterprise/release gates and
BUG-DATA-0001 remain OPEN. This infrastructure step is not production readiness.

Official process: [Docker settings](https://docs.docker.com/desktop/settings-and-maintenance/settings/)
and [offline VM disk backup](https://docs.docker.com/desktop/settings-and-maintenance/backup-and-restore/).
