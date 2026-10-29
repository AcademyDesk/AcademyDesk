# M1 — protected meeting readiness endpoint

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`.

## Implemented

- `GET /api/academies/{academyId:guid}/meetings/capabilities`, authenticated, no-store/no-cache. No write, preview, history, update or cancel endpoint added. No frontend creation enablement.
- `MeetingReadinessService` independently resolves current Identity user and current server roles, checks active identity, matching academy, Owner/AcademyAdmin membership, active academy and Engagement entitlement. Client/model role or academy claims are not accepted as authority. Platform-owner flag does not bypass foreign academy scope.
- Response contract `meeting-host/0.1`:server-clock `asOfUtc` and three fixed providers (GoogleWorkspace,Zoom,Microsoft365). Every provider is `implemented:false`, `available:false`, reason `IntegrationUnavailable`, all operations false and supported settings empty. Configured/secure markers,provider references and arbitrary provider text cannot activate or expand operations.
- No channel credential/reference/attendee data read or returned. The service does not read credentials,construct an HTTP client,write records or call a provider. No secrets setup or schema migration.
- Existing Engagement controller mapping extended for MeetingsController; existing module/permission/finance/communication behavior otherwise unchanged. Explicit domain checks remain even where the global filter has a platform-owner path.

## Verification

- `dotnet build apps/api/AcademyDesk.Api.csproj --no-restore --verbosity quiet`:PASS,zero warnings/errors.
-30 new tests PASS:27 service/controller/metadata cases plus3 actual Testing-host HTTP authentication cases. Real Identity UserManager/RoleManager over isolated EF InMemory stores validate own Owner/Admin,denied roles despite Owner claim,foreign route/platform-owner flag,role revocation,changed tenant,inactive/deleted identity,missing/inactive academy,missing/malformed entitlement andmarker/reference spoofing. Read-only/no audit/domain mutation andnon-secret response shape checked.
- Actual Program startup/DI and HTTP routing tested through existing guarded QaApiFactory. Anonymous,malformed bearer and genuinely protected-but-expired bearer tickets receive401 before database access. Test host uses existing approved isolated roots/configuration;only its run-owned temporary host folders removed through QaRunGuard. No SQL connection or provider request made. This is NOT an authenticated-success SQL pipeline test.
- Final combined87/87 PASS:30readiness +57existing communication permission/notification channel cases. No skips/failures. Earlier two xUnit style warnings in new assertions corrected;final build/run clean.
- Repeatable final evidence: `QA/EVIDENCE/meeting-readiness-20261015/readiness.trx`. Command:`dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~MeetingReadinessTests|FullyQualifiedName~CommunicationPreferenceAccessTests|FullyQualifiedName~NotificationChannelTests' --logger 'trx;LogFileName=readiness.trx' --results-directory QA/EVIDENCE/meeting-readiness-20261015 --verbosity quiet`.
- Scoped whitespace checks PASS. No frontend source edits,so prior meeting18focused/30synthetic visual proof retained,not rerun. No full enterprise suite rerun for this bounded endpoint.

## Remaining / next

Successor2026-10-15:[authenticated SQL/HTTP verification](MEETING_READINESS_SQL.md) now passes21/21M1cases with owned teardown. The earlier open SQL task below is historical to this implementation checkpoint. Fresh isolated harness restore surfaced unresolved moderate/high transitive Pkcs advisories;see successor before any release claim.

M1 code and bounded tests complete;real authenticated HTTP/SQL Owner/Admin/foreign role/tenant/revocation/module validation remains OPEN,not replaced by direct InMemory tests. Next Sol High: extend the existing disposable SQL harness for these M1 cases,then M2 typed preview/approval/version/durable operation/outbox work. Do not enable create/history before M2–M4/provider/release qualification. Existing UI2/Mini/domain conversation,security/device/runtime/E9–E10 gates remain OPEN.

No external connection/provisioning/invitation,credentials,migration,production database,main/staging/commit/push/Azure change. No other container pruned. Prior dirty application work/local QA evidence retained. No new Mini contract or project switch required. Owner full PENTA+Manual app→authorized go-live→Hayansh→product website order unchanged;90% AI remains a target,not delivered coverage.
