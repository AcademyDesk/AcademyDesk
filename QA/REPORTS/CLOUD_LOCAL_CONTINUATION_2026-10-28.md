# Cloud-first continuation from a Windows machine — 2026-10-28

The user is now on a Windows machine with PowerShell and wants development to
remain primarily in the cloud. The terminal attached to this chat is still the
managed Linux `/workspace` executor. No local Windows executor, filesystem mount
or VPN is attached. Opening this chat on the PC does not grant access to its
Docker daemon, repositories, ignored evidence, credentials or model cache.

## Verified cloud state

- AcademyDesk branch `codex/release-export-security` started clean at
  `161420bd7baca7dbdb1ac9041694d76f963d9030`. PentaMini started clean and is unchanged.
- Managed local-socket Docker server responds with version 28.4.0. The pinned
  llama.cpp image is cached; SQL Server 2022 is not cached.
- Current policy remains restricted, with no VPN/TCP destinations configured.
  Three previously saved draft hosts remain absent from enforced HTTP policy:
  `centralus.data.mcr.microsoft.com`, `westcentralus.data.mcr.microsoft.com`,
  `us.aws.cdn.hf.co`. Configuration was only read; nothing was changed/published.
- No repeat of unchanged denied SQL/model downloads, no customer DB connection,
  and no SQL harness or inference readiness claim.

## Read-only check on Windows

`QA/tools/check-machine.cjs` prints a small JSON inventory to stdout. It reports
OS/CPU/RAM, Node/Git/.NET/Python versions, repository branch/HEAD/dirty status,
local Docker version/OS and SQL image cache, and the presence/size of three
specific artifacts. It does not print paths, remotes, filenames in Git status,
raw command errors, environment variables, credential/configuration files or
artifact contents. It explicitly selects a local Docker socket instead of using
a saved/inherited remote context. Each child command has a five-second timeout.

From an existing checkout containing the script:

```powershell
node D:\AcademyDesk\QA\tools\check-machine.cjs --academy D:\AcademyDesk --penta D:\PentaMini
```

Adjust the two paths if those repositories are elsewhere. Node 18+ is required
for the inventory; the tested application environment uses Node 24.19.0.

If the PC has an older checkout, obtain just the checker without switching
branches or touching local application changes. These commands fetch the
authorized development branch and save the checker in the temporary directory:

```powershell
git -C D:\AcademyDesk fetch origin codex/release-export-security
if ($LASTEXITCODE -ne 0) { throw 'Fetch failed; stop here.' }
$machineCheck = Join-Path $env:TEMP 'academydesk-check-machine.cjs'
git -C D:\AcademyDesk show FETCH_HEAD:QA/tools/check-machine.cjs | Set-Content -Encoding UTF8 $machineCheck
if ($LASTEXITCODE -ne 0) { throw 'Could not read checker; stop here.' }
node $machineCheck --academy D:\AcademyDesk --penta D:\PentaMini
```

Only the JSON output is needed for the next decision. The checker itself makes
no network requests or file writes. The preceding fetch/extraction commands do.
Do not reset/clean local changes or upload `.env`, user-secrets, DB backups,
customer data, Docker config, or entire QA evidence directories.

## What local findings can contribute

1. If the original synthetic communication-roundtrip SQL log exists, preserve
   it on the PC. Its contents/provenance must be reviewed before a scoped transfer
   into ignored cloud evidence. Presence alone does not resolve the three failing
   frontend assertions or prove current SQL behavior.
2. Existing GGUF artifacts may avoid a fresh download, but size alone is not
   verification. Use PentaMini's pinned SHA-256 manifest before inference. Do not
   put model weights or Docker image archives in GitHub.
3. A cached local SQL image can support local isolated testing if later needed.
   Do not expose Windows Docker, SQL or other services publicly. Local Docker is
   not automatically usable by the cloud, and a local cache does not bypass the
   cloud's enforced destination policy.
4. Prefer applying the three already-reviewed network requirements through the
   supported environment configuration UI, then recheck observed enforced policy.
   The earlier user instruction prohibits agent publication. Until the user
   applies those settings, independent code and controlled-test work can continue.

Development changes remain on the feature branch with recurring GitHub pushes
authorized. `main`, Azure and environment publication remain separate. Existing
QA history and open enterprise gates continue; no replacement QA program is added.

## Validation

Syntax/help, cloud inventory execution, and smoke checks for invalid arguments,
absent tools/repositories/artifacts, JSON output and no path/error disclosure PASS.
The cloud's default sandbox suppressed Node child-process stdout in the first
negative smoke; rerunning that smoke outside the sandbox passed. No application
test result is inferred from the checker. Windows PowerShell,
Docker Desktop and the user's local artifacts are unverified until a local run.
Application source/dependencies are unchanged, so earlier application test results
are retained rather than rerun or represented as fresh results.

Next route: Codex for the inventory interpretation and guarded SQL/HTTP fixtures
once network access is applied; continue the existing release gates.
