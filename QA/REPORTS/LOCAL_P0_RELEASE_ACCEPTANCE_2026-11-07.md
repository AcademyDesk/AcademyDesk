# Local P0 release retest — 7 November 2026

Desktop built the native harness on `8963869` with zero warnings or errors.
The runtime, API test and SQL harness trees match the subsequent combined
branch. Cloud C11 at `fe0b673` contributes frontend and test source only.
[C11 integration](LOCAL_C11_SOURCE_INTEGRATION_2026-11-07.md)

Docker Desktop started from its existing per-user install. The run used
cached SQL Server and Azurite images, synthetic records, a loopback-only test
server and the existing QA entry points. Temporary local runner copies applied
the memory limits and disabled image pulls. Azurite used a bounded temporary
data mount. Application and tracked runner source stayed unchanged.

| Scope | Result |
| --- | --- |
| Original private file leak and legacy media | PASS. Authorized reads returned exact bytes. Anonymous, foreign-tenant, revoked and unpublished reads denied content in the original case and 29 additional HTTP cases. SQL run `a4a909aa3d79472187da23bd9bf7999a`. |
| Private Blob | PASS. 75 real HTTP, SQL and private Azurite assertions. Disabled storage returned 503 with no session or disk fallback. The run removed the owned private Blob container. SQL run `454e06b5964641f19ff4605a9a539158`. |
| Payment status | PASS. Full, partial and adjusted voids, reconciliation evidence, retained mixed collections and all 12 restoration cases. Rejected paths preserved SQL. SQL run `54f3567e5f954b09866ca04bacddc4c5`. |
| SQL backup and restore | PASS. All six checks, including source-bound backup verification, removal of the post-backup canary, exact fingerprints across 82 tables and restored Identity/HTTP. The original reconciled balance and excess-collection checks also passed during setup. SQL run `d8b921c3bc224f53bb71b4c98e9ddf1f`. |

The four SQL runs verified all 85 application and seven Identity migrations.
They removed only the owned databases, logins and containers. Original
Docker inventory, both PENTA services, the API health probe and the
validation lease were restored. The 1,991 API test result from the preceding
candidate remains applicable to the unchanged backend.

The production guard still reports five open P0 records. This local backup
does not qualify staging or Azure point-in-time recovery, media restore,
or deployment rollback. The current release's CI, images, broader critical
workflows and physical-device checks remain pending.
[Release gates](../09_RELEASE_GATES.md)
