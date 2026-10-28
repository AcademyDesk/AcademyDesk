# Dependency remediation and frontend quality increment — 2026-10-28

MODEL: Current Codex session
TOOL: Codex
REPOSITORY: AcademyDesk
BRANCH: codex/release-export-security
ENVIRONMENT: Cloud development; no customer database accessed
TASK: Remove known Pkcs advisories, address a bounded lint slice, diagnose SQL setup
MAY MODIFY: API dependency pin, practice-log types, QA documentation and handoff
MUST NOT MODIFY: Financial rules, authorization, migrations, deployment, customer data
VALIDATE: Full API suite, NuGet audit, frontend TypeScript/lint and production build
NEXT ROUTE: Codex for guarded SQL/HTTP integration; Cursor for contextual lint repair

Base commit: `b374b35482fa07c3dba41e27f4a746d9efc3a62e`. This increment remains
local and uncommitted. No new commits, pushes, merges or deployments were made.

## Dependency finding and repair

`dotnet nuget why` traced the vulnerable dependency:

`EFCore.SqlServer 10.0.12 → Microsoft.Data.SqlClient 6.1.6 → Pkcs 9.0.11`.

NuGet reported high [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx)
and moderate [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7)
advisories. Both identify 9.0.0–9.0.20 as affected, with a fix in 9.0.21.
NuGet's package index confirmed 9.0.22 is available. Added an explicit API reference
to 9.0.22, retaining the transitive dependency's major version and leaving the
SqlClient/EF dependency set unchanged. Review this pin when updating SqlClient.

Both the API and test project assets resolve 9.0.22. After restore, NuGet's
`package --vulnerable --include-transitive --format json` reported zero known
vulnerable packages in each project. This is advisory-feed evidence, not proof
that the application or entire runtime is free of security defects.

## Frontend quality

Replaced five explicit-any types in the practice-log page with concrete Academy,
Student, PracticeLog, PracticeForm and React form-event types matching the existing
API fields. Save/review return early until an academy is available. No API request
shape, teaching policy or persistence rule was changed. Response types are compile-time
contracts; runtime JSON validation and browser acceptance are not established here.

## Executed checks

- `dotnet test AcademyDesk.slnx --nologo -m:4`: 1,073 passed, zero failed/skipped.
  Earlier Pkcs advisory warnings are absent from this run.
- API and test NuGet vulnerability audits: zero known vulnerable packages each.
- `npx tsc --noEmit`: passed.
- `npx eslint src/app/practice-logs/page.tsx`: passed.
- `npm run build -- --webpack`: passed.
- Full `npm run lint -- --format json`: still failed, now 34 errors / 77 warnings,
  compared with 39 errors / 77 warnings before this increment. No rules disabled.

No additional product tests were introduced for the bounded typing change.
SQL behavior of the updated client dependency remains unverified pending the
disposable SQL fixture. Existing enterprise release gates remain open.

## Confirmed infrastructure blockers

The managed Docker daemon works. The SQL Server Developer image manifest is
reachable, but Docker layer/config downloads fail with Forbidden. Redirect
inspection identified `centralus.data.mcr.microsoft.com` and
`westcentralus.data.mcr.microsoft.com` outside the current allowlist.

PentaMini's pinned 0.8B model downloader also fails at a redirected destination,
`us.aws.cdn.hf.co`, despite access to huggingface.co. No model verification or
inference readiness passed, and no PentaMini source was modified.

These three exact domains were added to the environment configuration draft,
preserving all prior custom domains and presets. Saving the draft did not apply
the rules or publish the environment. The user must review/save the network
changes in environment settings and publish the environment as required by the
configuration tool before subsequent-task readiness can be claimed. No request
was routed around the proxy or denied policy, and no TLS/checksum checks were disabled.

After network changes apply, retry the owned SQL image pull, run the existing
guarded SQL harness with a generated run ID/loopback port and ephemeral credentials,
and verify migrations and the selected regression. Never reuse the configured
database without establishing that it is an approved disposable target. Retry
the repository model downloader, require its size/SHA256 verification, then test
readiness and a real chat request before claiming inference works.
