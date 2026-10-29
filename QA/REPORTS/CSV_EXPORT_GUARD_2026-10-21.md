# BUG-SEC-0002 — bounded spreadsheet-view export repair

21 October 2026. Worktree `D:/AcademyDesk-codex-p0`, branch `codex/penta-search`.
Sol High. Issue remains OPEN; local patch is not client/device/release acceptance.

## Source-confirmed gap and implementation

AcademyExports, AuditLogs and browser Reports quoted/doubled double quotes without
neutralizing formula-leading strings. Added matching C#/TypeScript spreadsheet-view
serializers and integrated all three paths. Existing source queries, academy filters,
audit action/entity/date filters, 200-row audit bound, names, MIME and finance rules
remain unchanged. Database values are never rewritten.

The policy quotes cells and doubles quotes, prefixing risky **text** with a tab inside
the quoted field. It covers ASCII/full-width = + - @, leading controls and markers
after leading whitespace/Unicode format marks/apostrophes. The backend iterates Unicode
runes, including supplementary format characters. Numeric objects remain numeric
text; untrusted numeric-looking strings remain guarded text when applicable.
Normal text, embedded commas/semicolons/quotes/newlines and multilingual labels retain
their original content. Both serializers preserve original input after the guard.

This is a human spreadsheet-view export, **not lossless machine interchange**. UI
notices explain the intentional tab and the need to review programmatic imports.
No universal spreadsheet/import/re-save guarantee is made. Current primary guidance:
[OWASP CSV Injection](https://github.com/OWASP/www-community/blob/master/pages/attacks/CSV_Injection.md)
describes Excel's quoted-tab mitigation, consumer differences and downstream-data trade-off.

## Executed validation

- [x] 27 .NET helper/controller tests; scoped InMemory rows/unchanged snapshots,
  audit/register byte output, foreign sentinel exclusion and unknown-resource404.
- [x] Full existing API suite: **2,861 passed**, zero failed/skipped. TRX:
  `.build-check/spreadsheet-csv-results/csv-guard-regression.trx`.
- [x] 27 tests execute the actual TypeScript serializer and verify Reports wiring.
- [x] Four actual native Edge Blob/anchor downloads:320/1440 x light/dark contexts.
  The actual Reports `downloadCsv` function is executed with the actual helper.
  Saved files parse independently to exact six rows/two cells, retaining guarded
  text and embedded delimiter/quote/newline content; no page errors/external requests.
- [x] TypeScript `--noEmit --incremental false` and targeted ESLint passed for
  serializer, Reports, Data Operations and Finance source.
- [x] Isolated SqlHarness compiled with zero warnings/errors. Offline dependency
  resolution used the existing package cache and an empty local feed, no network
  source or new package download. No project package versions were changed.

Native browser receipt:
`QA/EVIDENCE/spreadsheet-csv-1792556419882/result.json` plus four synthetic CSVs.
This tests native download plumbing, not authenticated full-page layout/SQL/Excel.

## SQL/Identity/HTTP gate — final PASS after guarded startup

The first attempt was blocked before creating SQL, as preserved below. Memory
later recovered naturally to3.84GiB, above the unchanged3GiB guard. A separate
attempt passed: **run7a2076155e0744069a61a9d18e9c16b6, exit0,160.8s**.
No restart, guard relaxation, new image download or rate-limit change was needed.

- [x] 32 actual export requests: all five registers plus audit, exact malicious
  guardian/audit cells, independent CSV parsing, audit filtering, Teacher/Student/
  anonymous/foreign denial and unchanged six-table domain/audit snapshots.
- [x] Existing Access regression retained: six actor classes x16 actions plus
  foreign-route/row, anonymous, grant/module/inactive/account/platform controls.
  Passed after the additive export checks; no assertions or fixtures removed.
- [x] Runtime360routes/349controller method-routes/10Identity; resolved contexts
  matched exact owned target/runtime login;102application/7Identity migrations.
- [x] No429; exact owned database and login removed after mismatched-marker refusal;
  only run-owned container stopped/removed. Original25containers/41volumes retained.

Final log: `.build-check/spreadsheet-csv-results/sql-access.log`.
The Access suite's generic Students/AdminWorkItems403 note is a **retained negative
control**, not proof of a new gap: BUG-FUNC-0006 already has minimized replacement
lookup endpoints and local repairs. Do not grant broader permissions or repeat them.

Added opt-in `QA_CSV_EXPORT_CHECK=1` to the existing Access runner. It executes
five registers plus audit export, malicious guardian/audit text, filters,
Teacher/Student/anonymous/foreign denials, unchanged fresh domain/audit snapshots
and explicit no429. Existing finance-access cases still run afterward, unchanged.
The env guard rejects wrong modules before resource creation. An isolated artifact
path is required; no baseline runner path or default behavior is replaced.

Before invoking that runner, the local preflight found less than **3GiB** available
host RAM (later observed2.68GiB). It stopped before container creation: **zero SQL
requests, zero new containers and no cleanup required**. No Docker restart,
lowered guard or fallback deployment was attempted. Never report this gate as passed.

Prepared command once the unchanged memory guard can pass:

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/csv-export-sql
$env:QA_CSV_EXPORT_CHECK='1'
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module Access
```

Run only against its original guarded disposable target, cached SQL image and owned
cleanup. Clear the opt-in environment afterward. Do not prune other containers.

## Remaining acceptance

Final frontend successor: exact-source typecheck/targeted lint and87-route export
PASS. `QA/EVIDENCE/exports-fullpage-1792557864459/result.json` records36 actual
exported-page journeys: Reports/Data Operations/Finance,320/1440 light/dark and
appropriate success/denied/network/wrong-type cases. Synthetic APIs are explicit;
this is not linked real-SQL/browser or spreadsheet-client acceptance.

- [x] Actual guarded SQL/Identity/HTTP export gate (final run above).
- [ ] Sandbox supported Excel/LibreOffice import/save/reopen evidence; no unsafe
  active-link/formula execution, no blanket universal-client claim.
- [ ] Linked role/full-page/device and exact-candidate enterprise/release gates.

BUG-SEC-0002 and BUG-DATA-0001 remain OPEN. No main edit, staging, commit, push,
Azure/production access, new model inference/training or deployment occurred.
