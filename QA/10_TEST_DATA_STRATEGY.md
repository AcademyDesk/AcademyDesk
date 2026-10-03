# Test environment and data strategy

## Existing state

The eight existing xUnit cases each create a uniquely named EF InMemory database. They use synthetic rows and never read the application's configured SQL connection string. Safe to execute as inspected. The regular `DevelopmentIdentitySeeder` creates and updates sample identities/business records; it is not a safe QA reset mechanism. Production startup can apply both EF migration sets and bootstrap a Platform Owner. Do not boot that host against an unchecked database during testing.

## Planned isolation

Use an ephemeral SQL Server database named `AcademyDesk_QA_<run-id>` (or disposable dedicated instance) and a separate `Testing` host environment. Apply AcademyDeskDbContext and IdentityDbContext migrations. Require an explicit test-run marker and local/approved staging host allowlist before setup/reset. Refuse reset of an unmarked DB even if its name looks like a test name. Use separate per-run storage, server ports and credentials. None of this SQL fixture infrastructure exists yet.

Configuration keys to isolate: `ConnectionStrings:DefaultConnection`, `Database:ApplyMigrationsOnStartup`, `Bootstrap:PlatformOwnerEmail/Password/DisplayName`, `Cors:AllowedOrigins`, `NEXT_PUBLIC_API_URL`, ASP.NET environment and upload WebRootPath. Values are not stored in QA reports. Test secrets come from isolated CI secrets or per-run generation, never copied from Azure. Disable both seeders and outbound delivery in the test host; no email/SMS/payment gateway side effects.

## Synthetic factories

| Fixture | Required variants |
| --- | --- |
| Academy A/B | Active/inactive; Launch/Growth/Professional; modules enabled/disabled; unique tenant markers |
| Identity | PlatformOwner (role + flag), Owner, AcademyAdmin, Manager, Operations, Sales, Marketing, FrontDesk, FinanceUser, Teacher, Student, Guardian; custom role; expired/revoked grants; inactive/missing user |
| People | Adult/minor/birthday boundary; parent absent/partial/complete; no/one/many guardian links and each access flag; active/inactive teacher; optional fields omitted/null/empty/complete |
| Academic delivery | Course prerequisite complete/incomplete; open/full/closed/waitlisted batches; source/target enrollment; assigned/unassigned teacher; completed/cancelled sessions; duplicate attendance |
| Finance | Invoice 1000, payment 600, reconciliation, adjustment 200, remaining balance 200; voided payment; full adjustment; payroll gross 1000 and deductions 0/1000/1001; independent compensation vs payroll profile |
| Content | Empty/long/Unicode names and notes; malformed/literal-null JSON metadata; 0/1/5/100 records; volume fixtures scaled only in performance environment |
| Uploads | Synthetic tiny PNG/PDF/audio; wrong extension/content; 50 MB boundary for teacher upload; interrupted upload; run-owned temporary folders |

Tests create data through direct factories for rule tests; through API for HTTP/E2E tests where the creation chain is the subject. Fresh-context reads verify persisted values, relations, uniqueness and audit. Financial fixtures never use real bank references. Use `.invalid` addresses and non-deliverable placeholders; no real children/customer data.

## Cleanup / restore

Record every created resource in a run manifest. Prefer dropping the verified disposable DB and run-specific storage, never broad directory deletion. On failure retain isolated DB briefly for debugging with access controls, then clean by manifest. Do not clean the live local development DB or Azure tenant. Restore checks restore an isolated backup into a new marked database and verify schema, row counts, representative relations, identities, file references and checksums; retention/RPO/RTO are BASELINE REQUIRED and need operator confirmation.

Mock deterministic network failures and external integrations; keep authentication, model binding, SQL constraints and serialization real in integration tests. Freeze clock for age, expiry, billing/cycles and screenshots. Record UTC and Asia/Kolkata conversions. Independent tests should not depend on execution order or the current date.
