# Metadata shape safety — bounded backend repair

Date:2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `67711e1d48fc8f2a5c4f62fd8b5265e1796582fa`.
Existing issue [BUG-API-0004](../ISSUES/BUG-API-0004.md) remains OPEN. Continue the existing enterprise queue; no audit/test-strategy restart.

## Reproduction and repair

Frozen new35-case suite on starting application source:10 PASS/25 FAIL (`QA/EVIDENCE/metadata-shape-20261011/baseline.trx`). Compensation controller reads and private announcement readers throw on valid JSON with the wrong root/property kind; null audience-only helper also returned true. These are controller/EF InMemory and pure reader tests, not a native SQL baseline or25 separate defects.

Narrow guards in TeacherCompensationController.Property, PortalController's announcement predicates and PlatformControlController's audience/expiry readers. Non-object compensation returns the existing empty summary; invalid announcement shapes fail closed or yield null expiry in the owner archive. No broad exception catch, rewrite/backfill, new migration, permissions, model/tools, frontend, financial calculation or write-path change.

Compatibility retained: current/legacy case-insensitive compensation aliases, zero rates and date fields; string importance/time/audience format; Admin/Teacher/Both and missing-audience legacy object behaviour. A malformed optional start *string* retains existing semantics; non-string/non-null start now fails closed. Null/missing metadata cannot become an active banner. Platform archive remains audience-based, not active-banner-based: an Admin record with invalid expiry is still listed with null expiry. Existing substring audience matching/time parsing/pagination policies are not redesigned here.

## Verification

- `final.trx`:80/80 PASS =35 new metadata +41 retained NotificationChannel +4 retained HolidayRoute tests. No full API/enterprise claim.
- Both metadata and retained-announcement harness builds: final0 errors/0 warnings. Initial harness compilation caught an incorrect receipt key and tuple-name inference; corrected to the existing composite receipt key and explicitly named tuple type without changing assertions.
- Native `MetadataShape`:72/72 real Identity/HTTP/disposable-SQL checks PASS, run `6c6bd1fba98048f1b764393272e621f6`, elapsed77.5 seconds (`sql-final.log`).14 compensation reads,50 portal/platform reads and8 access/scoping controls; exact summary/zero/alias/date/audience/expiry/sentinel responses; captured teacher/notification/receipt/domain-audit/platform-audit snapshots unchanged after every GET. Anonymous401, Teacher/Student/foreign compensation403, missing teacher404, non-platform Admin403 and foreign portal scope preserved; no429.
- Unchanged `AnnouncementAudience`:47/47 real HTTP/SQL cases PASS, run `a4d77e87e5e34f859fd9456393c95937`, elapsed57.1 seconds (`announcement-regression.log`). Existing owner publication creates exactly one notification/platform audit; all GET snapshots unchanged. Existing live-role changes, linked-role precedence, audience/time/tenant behaviour preserved. Assertions/runner/module source unchanged.
- Both runs verify88 application+7 Identity migrations, scoped runtime login, mismatched-marker cleanup refusal and exact owned database/login/container removal. Existing PENTA/retained QA containers untouched. Neither is a native failing-baseline run; the before-fix reproduction is the35-case suite above.
- Evidence remains local/untracked. `.build-check` artifacts ignored. Earlier frontend/device/PENTA proofs retained, not rerun here.

## Repeat commands

```powershell
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/metadata-shape --filter "FullyQualifiedName~MetadataShapeTests|FullyQualifiedName~NotificationChannelTests|FullyQualifiedName~HolidayRouteTests"
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/metadata-shape-sql --nologo -v quiet
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module MetadataShape
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/announcement-audience-sql-final --nologo -v quiet
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module AnnouncementAudience
```

Use only guarded disposable SQL; remove only the runner-owned fixture after exact ownership verification. Keep existing assertions/rate limits and retained PENTA/old QA containers untouched.

## Continuity and limits

Native acceptance complete; scoped feature publication prepared under the standing directive, SHA/result supplied in final Git handoff. Unrelated Mini source,32/28 continuity lines, private-conversation documents, interrupted Platform visuals and QA/EVIDENCE excluded/preserved. Main/Azure/production databases/credentials untouched; no Mini contract handoff needed. BUG-API-0004/browser/physical Android-iOS/full critical/release gates remain OPEN; this is not all-metadata or whole-portal acceptance.

Next Sol High: existing BUG-DATA-0047 expense update branch/status integrity reproducer, checking latest source/evidence before any repair. Shared Manual redesign remains queued: all4 portals match PENTA AI, Orbit + Aura dark /Prism light; AI frontend belongs to PENTA AI project. High for shared foundation/domain/host contracts; Medium for settled visual rollout. No repeat of accepted audits.
