# Local API container and production guard — 9 November 2026

Source `b6793d45788b74d8cd5ab82e246c483da8df4aec` on
`codex/penta-host-integration-20261030`. Cloud C23 owns the family and reminder
balance changes. This local batch covers the separate release container and
session guard. [Cloud scope](https://github.com/AcademyDesk/AcademyDesk/blob/2022e8209f3082f2542334803be737f26cdf2031/QA/HANDOFF/C23_PRIORITY_FINANCE_READERS_2026-11-08.md)

## Offline container build

Build `38e86740a0c2448d919735433d2e7685` restored and published Release in Docker
from a Git archive of the API tree. The scratch recipe added two SDK-stage
copies for a local NuGet feed and its configuration. The restore, publish and
runtime instructions came from the checked-in Dockerfile. Docker build ran
with `--network=none` and `--pull=false`. All 53 local package archives matched
their cache checksums, and all 53 cache content identities matched the resolved
assets manifest. The resulting image ID was
`sha256:309bbc476912e1dd4ee728e11f696cad198aa508025959c2b96fb59d90a35af6`.

The source Dockerfile and application trees are unchanged. The cached feed
reported `NU1905` because it supplies no vulnerability data. Hosted CI,
fresh dependency audit and the unmodified release build remain open.

## Production-mode Kestrel and disposable SQL

Run `ac54a8a6a9874b0fb3ff94c400015b41` reused the exact image ID and passed
24 checks. API and SQL ran on a private internal Docker network with zero
published ports. The requests went to Kestrel on the API container's own
loopback. Startup used the image's Production defaults, real schema and
Identity migrations, an isolated database, synthetic accounts and a
short-lived test certificate mounted read-only. Private Blob and PENTA Mini
activation were disabled.

| Check | Result |
| --- | --- |
| Startup and Identity | SQL health 200, anonymous platform access 401, Platform Owner and Academy Admin login 200 |
| Tenant isolation | Two actual onboarding requests returned 201, the owner could list both academies, the admin could read their own students and received 403 for the other tenant and platform control |
| Inactive model path | PENTA Mini health returned 404 for the signed-in admin while their Manual API read returned 200 |
| Encrypted key ring | An encrypted key file was written and the original admin bearer still worked after the API container restarted |
| SQL outage and recovery | Health changed to 503 when the owned SQL container stopped, then 200 after restart. Both synthetic academies remained. |

The first runtime attempt missed the SQL readiness message in an 80-line
tail. The next run found SQL but Docker's internal network did not publish
the requested port. The final run retained the internal network and used
in-container loopback. These setup failures remain in the local receipts.
All three windows removed their owned containers and networks. Existing
Docker inventory was preserved apart from the one new QA image. Both
original PENTA services were restored by exact ID and image. Their final
health check returned 200, and the lease was released.
This run proves application restart and SQL connectivity recovery. A staged
database restore and Azure revision rollback remain separate gates.

## Production guard

The guard now requires full `SECURITY-SESSION-001` acceptance through the
existing `BUG-AUTH-REVOCATION-001` record. The targeted bug stays classified P1.
The five existing P0 issue records and the broader session gate remain open.
The 15 guard tests passed locally, including a fixture where the five P0s
pass and partial session verification still blocks release. Candidate CI now
includes the guard tests. Running the guard on the real records returned exit 1
and listed all six open gates.

Physical iOS and Android, the remaining linked critical workflows, exact
release verification and image build, and staging restore and rollback remain
required. The release also needs PENTA model qualification before enabling a
model-backed workflow. [Release gates](../09_RELEASE_GATES.md)
