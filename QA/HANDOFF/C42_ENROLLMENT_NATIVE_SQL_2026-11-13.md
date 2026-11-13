# C42 cloud enrollment SQL/browser acceptance — 13 November 2026

OWNER:Cloud Codex. MODEL:Sol High. TOOL:Codex. ENVIRONMENT:managed Linux cloud.
BRANCH:codex/cloud-c42-enrollment-native-sql-20261113.
BASE:1b0b967b8f31d156ce2324abfc8d968e7f72ed83 (published C41).
STATUS:BLOCKED by exact SQL image CDN policy; no native qualification. Desktop is unavailable per owner; latest read-only Desktop tip
83452a129c1a7315922dbeeaf1b50de3d62cbdac. No Desktop files are imported.

TASK:close bounded C38/C39/C40/C41 native acceptance using guarded disposable
cloud SQL and the real existing application host/browser. Keep all application,
SQL harness and shared sources untouched unless an actual defect needs a separate
explicit file claim. Cloud Docker is available; SQL Server is an existing app/QA
runtime dependency but its image is not cached. Use the existing QA SQL image,
local-only ephemeral credentials and current migration sets. No model calls.

MAY MODIFY (exact7 paths):
- QA/tools/CloudEnrollmentSql/CloudEnrollmentSql.csproj (new)
- QA/tools/CloudEnrollmentSql/Program.cs (new)
- QA/tools/CloudEnrollmentSql/EnrollmentCases.cs (new)
- QA/tools/CloudEnrollmentSql/BrowserBridge.cs (new)
- QA/tools/cloud-enrollment-sql-browser.cjs (new)
- QA/tools/run-cloud-enrollment-sql.py (new)
- QA/HANDOFF/C42_ENROLLMENT_NATIVE_SQL_2026-11-13.md

MUST NOT MODIFY:application/backend/frontend/domain/Identity/permissions/SQL/
migrations,existing tests/SQL harness/root shared handoffs/trackers/evidence,
Mini/vendor/lockfiles/environment settings or existing containers/data.
No main/Azure/environment publish/replace/delete/public internet enablement,
model loading/download/inference/training/activation,new application dependencies,
paid compute or workflow dispatch. Only an owned loopback SQL container/database/
limited runtime login and owned test/bridge profiles may be provisioned/cleaned.

VALIDATE:existing QaRunManifest/QaRunGuard/QaApiFactory linked byte-exact;container
identity/image/name/hostname/run label/loopback binding,SQL server identity,unique
DB/ownership marker,least-privilege runtime and both migrations before HTTP.
Positive start/replay/recovery/quotas/expiry/purpose;bounded source search and audit;
all six updates and transfer/source/replacement;canonical approval/current-access/
stale-capacity/source/audit rollback/races/receipt behavior;real exported Chromium
through owned loopback bridge to actual Identity/API/SQL,GET-only reload recovery.
Retain original failures and restore/remove only proven owned resources on success.
STOP WHEN:bounded evidence/report feature-pushed and remote verified;unsupported
native/model/device/CI/release checks stay explicit. NEXT:remaining native/workflow
and scheduling/attendance packets after current evidence is accepted.


## Investigated return — blocked, application source unchanged

The exact claim bf478aa2ac0a577e74a414f34539c1280c856f8c was published and
remote-verified before prototype work. The recovered executor is running and
connected with restricted HTTP policy enforced;GitHub feature access works
through the normal approved task proxy path. Desktop remains83452a129c1a7315922dbeeaf1b50de3d62cbdac.

The local Docker28.4.0 daemon works through its guarded local socket. It had zero
containers and no cached SQL image;only the retained model runtime image existed
and was not run or altered. Available memory was about33GiB with58GiB disk free.
The existing QA SQL image mcr.microsoft.com/mssql/server:2022-latest pull failed
Forbidden. Daemon proxy metadata is empty;the task's managed proxy remains set.
This difference alone was not treated as proof of the final failure's cause.

The permitted task proxy can read the MCR manifest and image URL HEAD responses.
The official config-blob GET returned an HTML redirect to
westus.data.mcr.microsoft.com instead of the required image configuration.
The SHA-256/length checks rejected it before image loading. A bounded HEAD to
that exact official redirect through the inherited managed proxy returned
403 Forbidden. This establishes the missing SQL image CDN prerequisite;no alternate
mirror,listener,credential or proxy-policy bypass was attempted. No image was
loaded and no container,database,login or host/SQL test was created/run.
A future reviewed exact-domain policy change would be needed for this runtime;
this packet does not save,apply or publish any network/environment change.
Do not infer VPN/laptop readiness or a platform capability-startup root cause
from this registry-specific denial. The cloud executor itself recovered.

A test-only standalone harness prototype was drafted using byte-exact linked
existing QaRunGuard/QaApiFactory and the existing QA package versions. Its offline
restore succeeded from the installed cache,without downloading .NET packages.
Because native execution is blocked,the unfinished prototype was NOT published
as a working acceptance harness. Its source/generated restore outputs were
backed up byte-exact outside Git before removing only the newly-created owned
prototype directory. The final tracked change is this report alone. No original
application,test,harness,shared tracker,raw evidence or Mini/vendor path changed.

Private evidence:/workspace/c42-enrollment-native-sql-20261113 contains the
manifest,guarded prototype backup,offline restore log,sanitized blocker receipt
and private official redirect response. Signed redirect URLs and artifacts are
not committed. No credential or missing SQL evidence was fabricated.

NEXT:Cloud C43,separately claim the class schedule/reschedule browser host while
this concrete dependency is blocked. C38/C39/C40/C41 native SQL/browser acceptance
remains OPEN;retain C41's111client/104synthetic Chromium,2,717frontend passes/3
genuine missing-receipt failures and C40's2,743backend passes as source-bound
prior evidence,not fresh native qualification. Five P0 plus AUTH-REVOCATION001,
current CI/security/images/runtime,actual devices,migrations/restore/staging and
model/outcome acceptance remain open. Mini/Plus/Pro unqualified;Ultra deferred;
model loading/download/inference/training/activation remains stopped.
