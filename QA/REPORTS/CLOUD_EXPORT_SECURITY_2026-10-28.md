# Cloud export security increment — 2026-10-28

MODEL: Current Codex session
TOOL: Codex
REPOSITORY: AcademyDesk
BRANCH: codex/release-export-security
ENVIRONMENT: Published cloud development environment; synthetic InMemory data
TASK: Reproduce and repair SECURITY-EXPORT-001 / BUG-SEC-0002
MAY MODIFY: CSV encoding, related regression tests, issue records and handoff
MUST NOT MODIFY: Financial rules, authorization, migrations, dependencies, deployment
VALIDATE: API regression suite, frontend encoder tests, TypeScript, production build
STOP WHEN: Bounded export repair is verified and remaining release gates are recorded
NEXT ROUTE: Codex for guarded SQL/HTTP and security gate validation; Cursor for routine lint work

Baseline HEAD: `f7af512f884ca5fe8ac3ddb287c852492592e709`. No pre-existing
tracked/untracked changes were present. The report and repair are included in a feature-branch checkpoint for review.

## Reproduction and repair

Thirty synthetic academy/audit export cases failed before repair because formula
markers remained executable-looking cell contents. Added shared API quoting and
neutralization for academy and audit exports, and a frontend encoder used by report
downloads. Prefixing is export-only: stored data, query filters, tenant selection,
column names and authorization are unchanged. Proper quoting still contains commas,
quotes and embedded newlines. Typed API negative numbers are preserved; formula-like
frontend strings are conservatively protected as text.

## Executed validation

- Before fix: targeted `CsvExportSecurityTests`: 30 failed, zero passed/skipped.
- After fix: `dotnet test AcademyDesk.slnx --no-restore --nologo -m:4`:
  1,073 passed, zero failed/skipped, including 31 new CSV cases.
- `node QA/tools/test-csv-export.cjs`: 17 passed, zero failed/skipped.
- `npx tsc --noEmit`: passed.
- `npx eslint src/lib/csv.ts`: passed. Full baseline lint was not rerun or repaired;
  earlier onboarding recorded 39 errors and 77 warnings.
- `npm run build -- --webpack`: production build passed on the baseline with the
  newly applied font access, and passed again after the repair.

The tests exercise actual controller export bytes with EF InMemory; they do not
establish SQL query translation, HTTP access control, real spreadsheet interpretation,
or browser download acceptance. No SQL connection or customer data was used.

## Remaining gates

BUG-SEC-0002 remains OPEN until supported spreadsheet consumers and HTTP/SQL/browser
export behavior receive their required review. An apostrophe can be visible in
non-spreadsheet CSV consumers; stripping it or re-exporting may undo protection.
Existing financial/file-access P0 gates, SQL migrations/transactions/concurrency,
tenant/RBAC integration, critical browser workflows, backup/restore and dependency
vulnerability remediation remain open. NuGet still reports moderate/high advisories
for transitive System.Security.Cryptography.Pkcs 9.0.11.

No environment draft was changed or published. The feature branch is checkpointed separately; no merge or deployment ran.
PentaMini was unchanged; its prior 49-test result is retained, not rerun here.
