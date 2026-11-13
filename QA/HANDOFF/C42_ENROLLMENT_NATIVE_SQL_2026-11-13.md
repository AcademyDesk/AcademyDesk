# C42 cloud enrollment SQL/browser acceptance — 13 November 2026

OWNER:Cloud Codex. MODEL:Sol High. TOOL:Codex. ENVIRONMENT:managed Linux cloud.
BRANCH:codex/cloud-c42-enrollment-native-sql-20261113.
BASE:1b0b967b8f31d156ce2324abfc8d968e7f72ed83 (published C41).
STATUS:CLAIMED. Desktop is unavailable per owner; latest read-only Desktop tip
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
