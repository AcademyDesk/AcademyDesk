# E5-R34 — CUDA runtime archive directly on D:

19 October2026. Owner explicitly directed downloads to D:, not C:.
**DOWNLOAD COMPLETE AND VERIFIED ON D:. Docker import and GPU trial NOT RUN.**

## Scope

Download the official same-version CUDA12 linux/amd64 OCI files directly to:

`D:\PENTA-Models\runtimes\llama.cpp\11fe02151f79c41d0d4af7da708755d73b9c0da6\cuda12-amd64-oci`

Pinned manifest `sha256:e8318a7b3988f9ca57b28c03486569e60def08f2b7b060550ebfa046e175e6cb`;
13compressed layers total2592290611bytes, plus18100byte configuration and manifest/
layout/receipt metadata. No new model download. This is an OCI directory archive,
not a claim the image is imported into Docker or ready to execute.

Added `operations/tools/download_cuda_archive_e5r34.py` in Mini: standard-library
HTTP streaming straight into fixed D: content-addressed files; no image pull,
Docker cache or C: temporary layer files. Verifies exact manifest/config/source
revision, file lengths and SHA256. Existing files are verified/reused, never
overwritten; failed partials retained on D:. Cross-host registry redirects drop
the in-memory public-registry bearer; no credentials/signed URLs persisted or
printed. Unsafe redirects/digests/paths and insufficient D:reserve fail closed.

## Verification

-10new offline downloader safety tests PASS,0failures/0.47s; no model tests rerun.
-Registry manifest digest/inventory preflight verified before downloading.
-Observed downloader exit0/266.38s. All14blobs(config+13layers) and manifest
  independently rehashed after completion: exact lengths/digests match,
 0partial files. OCI layout/index/config version/source verified, archive on D:.
-Runtime files are not executable model acceptance. No binary/GPU compatibility
  or performance test was performed; no Docker import or C:image-store use.
-Prior386model-code tests and actual CPU failures retained, NOT repeated here.
-No inference, GPU/model load, driver/runtime execution, Docker import/config/data
  relocation or container-state change. No Azure/push/commit/main/database action.
-Academy login3000/API5092health200 and PENTA original services running/healthy
  observed during download. No downtime scheduled or user data/evidence deleted.

SHA256 pins:

| File | SHA256 |
| --- | --- |
|Mini operations/tools/download_cuda_archive_e5r34.py|`51BEB5F0650EAC5123FE3FE0B8B9FFF48A67B7C611A478363E3E4311622C83D2`|
|Mini tests/test_cuda_archive_e5r34.py|`5A21FEA9C5A8C5FF73655C3F6913FF8C061AD2C993739D07E4D0B8F25C05819F`|
|D:archive download-receipt.json|`5A92E5F56AF1C7D4857C5B2292E476247F54B3BDC7D5B53E2C6256855CF58582`|
|Academy QA/EVIDENCE/penta-cuda-archive-e5r34/python-final.xml|`F270FFE4C31BCCC2676AA2E8D72F814F754DDC2B0D184302EFCBC67ACCF90574`|

Commands from Mini root with `PYTHONDONTWRITEBYTECODE=1`:
`python -m pytest tests/test_cuda_archive_e5r34.py -q -p no:cacheprovider --junitxml=D:/AcademyDesk-codex-p0/QA/EVIDENCE/penta-cuda-archive-e5r34/python-final.xml`,
`python operations/tools/download_cuda_archive_e5r34.py` (metadata preflight),
then `python operations/tools/download_cuda_archive_e5r34.py --download` once.
Post-download independent PowerShell Get-FileHash verification covered all14blobs
and manifest. Token remains memory-only; no credentials in archive/evidence.

## Important next boundary

Saving the archive on D: does not relocate Docker's image/container working
storage. Docker still uses its existing C: data VHD. A later import could consume
C: unless that storage is deliberately relocated first. Do not import on the
assumption the archive's drive changes Docker's data root.

To keep future image layers and GPU test working data on D: as well, a supported
Docker Desktop storage-location change needs separate approval and planned service
interruption, preserving existing images/volumes/containers and verified recovery.
Do not hand-move the VHD, prune/delete content or rewrite settings ad hoc.
Official references: [Docker settings](https://docs.docker.com/desktop/settings-and-maintenance/settings/),
[Docker backup/restore](https://docs.docker.com/desktop/settings-and-maintenance/backup-and-restore/).

Stay Sol High for storage/runtime/authority work. GPU compatibility/performance/
accuracy and all real model/host/domain/Manual/device/enterprise/release gates
remain OPEN. The archive download does not qualify the model or production.
