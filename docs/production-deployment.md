# Azure release preparation

Reviewed 23 November 2026 against AcademyDesk c3617f28d458287189ca7173c8fe26ddea8fa271.
This is a source review and staging proposal, not a provisioned environment or
deployment approval. No Azure CLI or connected Azure inventory tool is available
in this Desktop session. Resource group academydesk-prod, storage account
academydeskmedia and container academydesk-media are owner-supplied identifiers;
their effective configuration, networking, identities and backup policies remain
unverified. No resources, credentials or customer data were changed.

## Existing path and gaps

Keep the existing Static Web Apps static export, Container Apps/ACR API, Azure SQL
and private Blob pattern. The actual .github/workflows/deploy-azure.yml is manual
workflow_dispatch only, requires refs/heads/main, calls full verification and
checks critical issue acceptance before Azure login. It does not deploy on a push
to main. Do not dispatch it for this task.

| Source observation | Required acceptance |
| --- | --- |
| API and web are rebuilt at deployment | Bind candidate SHA, package locks, base image digests, API image digest and web artifact hash. Promote tested immutable artifacts through a reviewed pipeline change; a CI image pass does not qualify a different later rebuild. |
| Azure login uses AZURE_CREDENTIALS and SWA token | Protected environment secrets, least privilege and owner approval. OIDC is a future reviewed option, not configured by this document. |
| Registry setup requests system identity | Verify identity exists and has scoped AcrPull; the command does not establish the role assignment. |
| Application and Identity use DefaultConnection | Encrypted private SQL connectivity and least-privilege runtime principal. Managed-identity SQL needs verified Entra principal and actual connection tests; do not guess a connection string. Runtime must not require DDL authority. |
| Workflow enables startup migrations before rollout | Implement a single-owner migration job with a separate principal and disable runtime startup migration after approval. Do not race migrations across replicas. |
| Production MediaStorage defaults disabled | Actual private Blob RBAC, networking and protected upload/recovery/download acceptance before enabling. Azurite passes are separate. |
| Protected key ring is explicit opt-in and not configured by deployment | Shared persistent private key storage, certificate mount/rotation and restore. Multi-replica Identity tickets and encrypted PENTA receipts must survive revisions. |
| /health checks application DB connectivity | Read back startup/readiness/liveness probes; check Identity schema, media and model separately. AI downtime must not disable Manual ERP. |
| Forwarded headers clear known proxy/network restrictions | Verify actual trusted ingress/header spoofing boundary. HTTPS redirection alone does not prove forwarded-header authority. |
| Mini provider/pilot allow only Development/Testing loopback | Production private transport, service identity/authentication, network policy and qualification still required. Never run Azure in Testing or relax the environment guard to bypass acceptance. |

## Configuration inventory — names only

Keep values in protected GitHub environments/Key Vault/private mounts, never in
source, chat, frontend variables or published evidence.

- Existing production environment secrets: AZURE_CREDENTIALS,
  AZURE_STATIC_WEB_APPS_API_TOKEN, AZURE_SQL_CONNECTION_STRING and optional
  PLATFORM_OWNER_PASSWORD.
- Existing variables: AZURE_RESOURCE_GROUP, AZURE_REGISTRY_NAME,
  AZURE_REGISTRY_LOGIN_SERVER, AZURE_API_CONTAINER_APP, PRODUCTION_API_URL,
  WEB_APP_ORIGIN, PLATFORM_OWNER_EMAIL and PLATFORM_OWNER_NAME.
- Paired CI repository secret: PENTA_SOURCE_READ_TOKEN, dedicated read-only
  Contents access to private AcademyDesk/PentaMini. Owner must enter it securely
  in Actions settings. This connector cannot administer secrets. Do not reuse a
  broad local Git credential or skip the paired job.
- API: ConnectionStrings__DefaultConnection and exact HTTPS
  Cors__AllowedOrigins__0.
- Media: MediaStorage__Enabled, MediaStorage__ServiceUri,
  MediaStorage__ContainerName, optional MediaStorage__ManagedIdentityClientId,
  MediaStorage__MaxFileBytes and MediaStorage__ChunkBytes. Current source caps
  files at 2,000,000,000 bytes and chunks at 8,388,608 bytes. No unlimited size or
  universal inline-format guarantee. Host authorization remains required.
- Keys: Security__DataProtection__Enabled, KeyRingDirectory, CertificatePath and
  CertificatePassword under that same prefix. Rotation uses
  Security__DataProtection__DecryptionCertificates__<n>__Path / __Password.
  Mount private PFX/P12 outside both the key directory and application/web roots.
- Web: NEXT_PUBLIC_API_URL is public configuration. Keep
  NEXT_PUBLIC_PENTA_MINI_EVALUATION unset/false outside isolated test exports.
  Penta:Enabled and Penta:Mini:Enabled stay default-off; flags alone do not enable
  or qualify production Mini.
- Bootstrap is one controlled operation. Remove the bootstrap password/reference
  afterward. No development seeding in staging or production.

## Concrete staging proposal — approval and provisioning still required

1. Owner approves a separate non-customer staging environment and cost ceiling.
   Follow existing regional/infrastructure patterns: isolated SQL, private media
   storage boundary, API revision, static origin and identity. Never point test
   traffic at production customer data. Shared resources need explicit isolation.
2. Infrastructure owner inventories IDs/region/SKU, API/ACR identities and role
   assignments, private endpoints/DNS, public firewall/anonymous Blob settings,
   TLS domains, logs/alerts and backup retention. Record non-secret metadata.
3. Verify API-subnet private SQL and Blob resolution/access plus public denial.
   Browsers use HTTPS API routes, not public Blob, broad SAS, DB or model access.
   Test actual ingress chunk limits/timeouts, ranges and revoked credentials.
4. Persist/encrypt and back up Data Protection keys and certificates before
   scaling. Prove Identity tokens and legacy receipts across replica/revision
   changes and isolated restore, with revocation intact. Define recovery for
   genuinely lost keys rather than disabling protection.
5. Prepare a migration manifest for both EF contexts from the exact candidate
   (current local fixture: 86 application and 7 Identity migrations). Review
   idempotent scripts, capture backup/PITR point, take a single migration lock,
   apply with the migration principal, verify schema/history, then start a
   non-migrating runtime revision.
6. Restore SQL to an isolated DB and private Blob versions/soft-deleted objects to
   an isolated destination. Restore encryption keys privately. Compare table/
   ledger/Identity fingerprints, protected receipts, original media hashes and
   tenant/revocation controls. Measure recovery time/point and retention. Local
   restore tests do not prove Azure PITR or private-media restoration.
7. Test rollback to the previous API digest and matching web artifact only with a
   compatible schema. Otherwise use the reviewed restore/forward-fix procedure.
   A code rollback does not undo SQL. Retain a failed synthetic rollout/migration.
8. Configure TLS-only domains/renewal alerts, exact CORS, security/cache headers
   and redacted correlated logs. Exclude raw prompts, financial bodies, tokens,
   upload credentials and certificate passwords. Test routed alerts for health,
   5xx/401/403/429 anomalies, SQL/storage/audit failures and unavailable AI.
9. Execute six critical closure chains, actual Android/iOS/laptop media and
   session checks, role/tenant/approval denial, exact-money recovery and Manual
   save during AI downtime. Bind every result to source, artifact, environment
   and private evidence digest.
10. Only after independent model/tool and eligible-outcome qualification introduce
    a reviewed production private Mini service. Mini/Plus/Pro remain unqualified;
    Ultra is deferred. AcademyDesk remains authoritative for deterministic
    finance, domain authorization, approvals, transactions and mandatory audit.

## Source-bound release decision

- [x] Existing configuration reviewed; no deployment dispatched.
- [x] C55 source/native/client/browser evidence preserved.
- [x] Exact c3617f2 ordinary CI passed: 2,600 API tests, dependency audits,
  SQL/private Blob, frontend/build and API container smoke.
- [ ] Paired Core job passes on the exact release SHA with the required credential.
- [ ] Genuine private Mini prerequisites and full-suite failures resolved.
- [ ] Production private inference transport and tier/workflow qualification.
- [ ] All six critical records complete required final acceptance, including
  physical devices; existing critical guard remains blocked.
- [ ] Actual staging inventory/configuration/TLS/health/alerts and controlled
  migration, backup/restore/private Blob/key recovery and rollback evidence.
- [ ] Owner approves one immutable release candidate after these gates.

The 90% target is unmeasured. Count agreed eligible end-to-end outcomes by role
and workflow: correct result, current host authority, required approval, audited
persistence/readback and recovery. Do not count passing prompts, assertions,
static adapters or the narrow 12/12 learner evaluation as 90% operation coverage.
Keep denied/unsupported states explicit and Manual usable. This preparation does
not authorize main merge, paid provisioning or Azure deployment.
