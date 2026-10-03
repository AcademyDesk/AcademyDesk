# Audited baseline

Audit dates: 2026-09-27/28 UTC/Asia-Kolkata session; exact existing test run times are embedded in the TRX. Repository `D:/AcademyDesk`, branch `main`, HEAD `20bb6047f9edf733ac8e2a226621cc582ec54b3c`. Inventory timestamp is in `INVENTORY/source-inventory.json`. This is a **dirty working-tree baseline**, not a clean-commit certification.

Pre-existing modifications preserved: `apps/web/src/app/student-onboarding/page.tsx` adds a success redirect; `apps/web/src/app/student-profile/page.tsx` reads a success query notice. `.build-check/` was already untracked build output. Source hashes record these exact working files. Phase 1 writes only QA documents/tooling/evidence and isolated build outputs; no application repairs or production deployment.

| Item | Observed version / architecture |
| --- | --- |
| Flutter / Dart | Not applicable: no pubspec.yaml or Flutter client found |
| .NET SDK | 10.0.401 |
| Backend target | net10.0, ASP.NET Core MVC API |
| EF Core / Identity / OpenAPI packages | 10.0.12 |
| Node / npm | v24.21.0 / 11.19.0 |
| Next / React / React DOM | 16.3.5 / 19.2.8 / 19.2.8 |
| TypeScript / ESLint / Tailwind | Installed 5.9.3 / 9.39.5 / 4.3.3 |
| SQL technology | Microsoft SQL Server via EF Core SqlServer; two DbContexts |
| Authentication | ASP.NET Identity API, bearer/refresh tokens, roles + IsPlatformOwner + linked person IDs |
| Existing tests | xUnit 2.9.3; runner 3.1.4; Test SDK 17.14.1; coverlet.collector 6.0.4; EF InMemory 10.0.12 |
| Browser/component test frameworks | None declared in apps/web/package.json; no frontend test script |
| Existing commands | dotnet test; npm run lint; npm run build; TypeScript via npx tsc |
| Deployment | Manual workflow_dispatch Azure pipeline; API image + static frontend; migrations enabled on API startup |

## Executed checks

| Check ID | Command | Result | Evidence |
| --- | --- | --- | --- |
| EXISTING-001…008 | dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/qa-phase1 --logger "trx;LogFileName=phase1-existing.trx" --results-directory QA/EVIDENCE/logs --verbosity minimal | 8 passed, 0 failed, 0 skipped; reported test execution duration 2 seconds | EVIDENCE/logs/phase1-existing.trx |
| QUALITY-LINT-001 | npm run lint; repeated with --format json to retain all diagnostics | FAIL, exit 1; 46 errors, 81 warnings | EVIDENCE/logs/eslint.json; BUG-FUNC-0004 |
| QUALITY-TYPE-001 | npx tsc --noEmit --incremental false | PASS, exit 0, no diagnostics | Observed command result; no business assertions |
| Local API schema availability | GET http://localhost:5092/openapi/v1.json | BLOCKED: fetch failed (connection unavailable) | No schema captured; no API startup/DB mutation attempted |

`dotnet test` compiled the backend/test project in isolated output and ran eight assertions-based tests. A Next production build was reported in prior work, but not rerun in Phase 1 and is not counted as this audit's PASS. No SQL Server integration test, UI/browser test, real-device test, load test or production mutation was executed. Prior deployment/health checks do not certify these working-tree UI changes.
