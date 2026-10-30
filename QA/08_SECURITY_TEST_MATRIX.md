# Security test matrix

All cases below are NOT RUN unless specifically backed by an execution artifact. Source findings are risks to reproduce in an isolated environment, not claims of a production penetration test. [Role matrix](04_ROLE_PERMISSION_MATRIX.md) and endpoint authorization appendix define the tested boundaries.

| Test ID | Scope / executable expectation | Source / evidence | Current risk |
| --- | --- | --- | --- |
| AUTH-001 | Valid/invalid login, no account enumeration in public errors, correct user/workspace; inactive account denied | Program, AuthSessionController, lib/api.ts | No HTTP authentication suite |
| AUTH-002 | Refresh retry uses new token; expired refresh preserves input and offers login; parallel refresh behavior | lib/api.ts request/academyApi | BUG-API-0003 stale header override |
| SECURITY-ROLE-001 | Every HTTP method: anonymous, allowed actor, wrong role, academy B, inactive academy, disabled module; no unauthorized read or write | AcademyAccessFilter + endpoint action guards | Global filter is present; absence of an attribute is not evidence of no protection |
| SECURITY-ROLE-002 | Permission grants and role policy agree for onboarding and strict admin endpoints | StudentOnboarding, Staff, PortalAccounts, AcademyRoles, AccessGrants | BUG-FUNC-0002; policy conflicts require review |
| SECURITY-GUARDIAN-001 | Own child / other child; revoke link and each academic/finance/documents/leave flag; re-fetch and download directly | PortalController.ParentAccess/CanAccessStudent and StudentGuardians | Nested object and download isolation untested |
| SECURITY-SESSION-001 | Disabled/deleted user, password change, refresh expiry, logout, role switch and workspace localStorage isolation; no secrets in evidence | Program bearer options; api.ts; AuthSession | LocalStorage XSS impact, refresh concurrency and server revocation behavior need tests |
| SECURITY-FILE-001 | Direct upload URL requires authorized scope, including guessed/known URL and range requests | UseStaticFiles; teacher upload webroot | BUG-SEC-0001 public private-material risk |
| SECURITY-UPLOAD-001 | File content/extension/MIME consistency; zero/limit/oversize; path traversal; interruption; cleanup; no leaked private URLs | TeacherPortal, Portal, AuthSession upload actions | Different size/type checks; local replica storage; no antivirus pipeline found |
| SECURITY-INPUT-001 | XSS payload stays inert in UI and HTML downloads; malformed JSON/IDs return safe validation; SQL-like strings cannot escape parameterized LINQ | Portal HTML download encoding; EF controller queries; layout constant theme script | No direct raw SQL execution found in scanned API source; do not claim SQLi-proof from scan alone |
| SECURITY-EXPORT-001 | CSV formula-leading values neutralized under approved export policy | AcademyExports.Csv, AuditLogs export, reports downloadCsv | BUG-SEC-0002 |
| SECURITY-DEBUG-001 | Development-only OpenAPI and seeder absent in production; errors disclose no stack/connection details; sample WeatherForecast decision | Program + WeatherForecastController | Sample route remains; seeder contains development credentials (values deliberately omitted) |
| SECURITY-RATE-001 | Login/reset throttling and ordinary traffic bounded; proxy headers cannot bypass chosen limit identity; CORS trusted origins only | Program rate limiter before authentication, cleared KnownProxies/IPNetworks | User-based partition may not see authenticated principal; deployment proxy trust must be established |

Additional required cases under endpoint scenarios: role assignment cannot grant unintended system/owner capability; custom role names/permissions remain tenant-scoped; Identity default register route cannot choose role/tenant flags; public certificate verification exposes only intended information; access grants reject expired/unknown scope; audit export/delete restricted; HTML download must encode user text and not execute URLs/scripts. Policies and framework default routes must be enumerated in the actual test host rather than assumed from UI routes.

Inspect dependency vulnerabilities with the pinned lock files in Phase 2; no dependency advisory scan was run here. Headers/CSP/HSTS, data-protection key persistence across Azure replicas, upload storage durability and production secret rotation require deployment configuration evidence beyond this repository. Do not copy secrets from pipeline/environment into QA. No production exploit attempt or destructive request is authorized by this audit.

Cloud follow-up (2026-10-28): NuGet identified high/moderate Pkcs advisories in
9.0.11 via SqlClient 6.1.6. An explicit API reference now resolves 9.0.22 in API
and tests; both transitive-inclusive audits report zero known vulnerable packages.
The full API suite passes 1,073 tests. This does not close deployment/runtime,
SQL integration or other security gates. See
[dependency remediation report](REPORTS/CLOUD_DEPENDENCY_AND_LINT_2026-10-28.md).

## Observed scoped verification — 29 October 2026 IST

Exactc7fca5f CI40840527086 completed SUCCESS; actual job steps/logs were read.
RoleReplacement37 core/56 extended cases; strict RoleRevocation37 observations;
SessionRevocation36cases; native SessionRefresh11 requests; RefreshConcurrency25
requests/four controlled overlaps all passed with zero reported policy gaps.
Private Blob75 assertions and legacy direct/range/revocation controls passed.
All6 owned SQL backup/restore stages passed, including exact70-table/schema
fingerprints and restored Identity/HTTP/privileges. These supersede the earlier
not-run statements only for their actual bounded cases, not the entire matrix.

[Current acceptance mapping](REPORTS/CLOUD_WORK_ITEM_VALIDATION_2026-10-29.md)
records remaining P0/browser/device/runtime-header/rate/proxy/storage and Azure
PITR/media/application-schema recovery requirements. A synthetic frontend CSV
probe on LibreOfficeDev alpha accepted18 non-NUL and19 NUL-containing rows as
non-formulas; the initial multi-file timeout is retained. Stable supported
consumers/real HTTP exports remain pending. No issue
was closed or production security acceptance inferred from CI alone.
