# Current API container diagnostics — 5 November 2026

Source: `f19de88bd4d9fed857fa0715b47c877c78a834a0` on `codex/penta-host-integration-20261030`.
Status: diagnostic runtime smoke PASS; production Dockerfile build remains OPEN.
No application, Dockerfile, dependency, business-rule or model source was changed.

## Production Dockerfile offline attempt

Run `2aabdfb5bd044830b2bd794c33400516` used the actual apps/api/Dockerfile,
cached SDK/runtime bases, --network=none and --pull=false. It failed at the
existing dotnet restore step with NU1301 because the container build lacked
NuGet access/cache for this restore. This is a real uncompleted build gate,
not a package-version repair or passing release image. No download, package
change or repeated offline restore attempt. Exact services and full inventory
were restored; original API health200 and lease release verified.

## Narrower actual runtime diagnostic

Run `c9b50f815bd1401db907a023ac3a9dba` published the current API with the existing
local package cache, --no-restore, Release and disabled repeated NuGet audit for
this diagnostic only. This is not fresh dependency-audit acceptance. A temporary
ignored recipe copied that published output onto the cached ASP.NET10 runtime.
It did not modify the production Dockerfile or replace its build gate.

- Cached publish and diagnostic image build passed; image build networking off.
- Owned image: `academydesk-local-host:qa-c9b50f815bd1401db907a023ac3a9dba`.
- Image ID: `sha256:5176cb952cf8a17c6c63731ac70bb05ba752459914214e5560c6dcb04a36b9ef`.
- Published API DLL SHA256: `97a539beb33a533e74c7d6bdbb956e2ac7adbb898928c0459a457c0153eb515f`.
- Runtime container restricted to768MiB/1CPU, loopback ephemeral port, no mounts.
- Testing environment; migration/bootstrap/media disabled; only an explicitly
  unavailable loopback database target, no supplied credential/customer database.
- Actual startup and /health unavailable-database response503 PASS.
- Actual anonymous /api/academies response401 PASS.
- Owned container label/name/loopback/no-mount verified before removal.
- Original containers/volumes/networks/images preserved; one run-owned diagnostic
  image intentionally retained locally. Exact PENTA IDs/images/healthy/API200 and
  validation lease release PASS. Raw receipts/logs remain under .build-check.

The owner-authorized safe memory window retained the unchanged3GiB guard and
admitted at4336066560available bytes after stopping only the two
original services and reclaiming disposable Linux page cache as needed. No user
application was closed, no pruning/download/model experiment or Azure action.

## Cloud return / remaining release gates

Cloud C4 can run the unmodified production Dockerfile and fresh dependency audit
when its approved environment supplies the pinned package-restore prerequisites.
Keep failure evidence, exact source/image identity and actual runtime tests.
Do not change package versions or weaken audits to mask missing connectivity.

This smoke does not test a real container-to-SQL connection, production settings,
real model quality, authenticated PENTA browser flows, devices, Blob, private
cross-repo CI or staging/release. Those established gates remain OPEN. Local L1
host SQL results remain in PENTA_HOST_SQL_QUALIFICATION_2026-11-05.md. Cloud C1/C2
remain independently assigned. No main merge, production/customer DB or Azure.
