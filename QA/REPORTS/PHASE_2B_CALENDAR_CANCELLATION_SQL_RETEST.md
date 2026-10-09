# Calendar cancellation HTTP/SQL retest — 2026-10-09

Issues: [BUG-FUNC-0021](../ISSUES/BUG-FUNC-0021.md) and
[BUG-UI-0007](../ISSUES/BUG-UI-0007.md), both **OPEN**.
Starting HEAD: `af759b6065f351bd5881a7f4e13def1c8b3f603d`.
Worktree/branch: `D:\AcademyDesk-codex-p0`, `codex/penta-search`.
Route: Sol High / Codex, existing enterprise integration continuation.

## Scope and discovered defect

Retained all original 11 CalendarDetails cases and their Scheduled positive
fixture. Added eight cases: foreign/anonymous/unassigned-teacher/invalid-status
no-write denials; authorized cancellation with response/fresh SQL/history
checks; read-only admin and assigned-teacher GETs; full projection/readback
parity. Cancellation changes only the target status, not teacher, batch, branch,
location, delivery, start/end or other academy rows/defaults. History count stays
three and existing meeting locations remain stored.

The first captured SQL run exposed an additional real contract defect: SQL
`datetime2` readback has `DateTimeKind.Unspecified`, so GET emitted an unmarked
`2026-10-08T10:00:00` while PUT emitted `2026-10-08T10:00:00Z`. The test stopped
on original-field equality rather than masking it. Browsers parse the unmarked
value as local time, invalidating cross-timezone display guarantees.

Class-session list and Teacher portal session projections now explicitly mark
these existing UTC column values as UTC for serialization. No tick conversion,
SQL data migration, input/status-policy rewrite, authorization change or new
frontend product code. Teacher Me uses the same projection rule; this packet's
SQL timestamp proof targets admin and assigned-teacher calendar GETs. Remaining
event/make-up and other timestamp contracts are not accepted by this repair.

## Fresh HTTP/SQL proof

Build: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/calendar-details-sql`:
zero warnings/errors. Existing runner unchanged:

`powershell -NoProfile -ExecutionPolicy Bypass -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module CalendarDetails`

Execution-policy override is process-only, not a saved machine/Git setting.
Final owned run `fb3c1a3286fd4946a2518cbbea001136`, loopback SQL port 59027:
**19/19 PASS**, real Identity/bearer/HTTP pipeline and fresh SQL contexts, no
429. Explicit UTC Z assertions preserve SQL ticks. Original tenant/login/no-write
controls retained; 88 application + 7 Identity migrations, runtime login scope,
negative false-marker cleanup, final database/login teardown and exact-owned
container stop/removal PASS. No customer/dev/production database.

Evidence: `QA/EVIDENCE/calendar-cancellation-20261009/sql-final.log`.
SHA256: `2dcdf9cc319be6ed4c61edfffc5b4d92b918eb5567564348ed421cd9b936f589`.

Failure evidence is not replaced: `sql.log` in the same directory records run
`0ab96601caa24215acc219a43d7f5e36` failing on startUtc response parity. An earlier
attempt hit disabled PowerShell scripts (no SQL); the subsequent output sink
used a nonexistent logs directory and left run
`2d4430da296a44f0bb18d1769aae1436` without a complete test receipt. It is **not
accepted**. Both failed-run database-name inventories are retained locally.
After processes ended, exact names/run labels/loopback ports and disposable DB
inventories were checked before removing only those two containers. Their
disposable databases are unrecoverable; captured evidence remains. Older stopped
QA and running Mini containers were not touched. The final runner removed only
its own third container through its unchanged ownership guards.

## Actual calendar receipt and regressions

Completed captures require one exact run identity, 19-case success, all eight
new cases, database/login and container cleanup, zero exit, one fixture, stable
unique IDs/history, teacher/location parity and explicit UTC timestamps before
replay. **18/18 capture-guard checks PASS**, including failed/partial/mixed-run,
missing cleanup, changed identity/location/history and unmarked-UTC rejection.
This is QA receipt checking, not signed evidence or security attestation.

Actual TSX: all original 81 cases retained; original real Scheduled capture
plus the cancellation capture add two cases. **83 per timezone / 332 total
PASS** across UTC, Kolkata, Los Angeles and Sydney. Both snapshots retain the
same records and override teacher/location, but only Cancelled loses the target
grid/agenda meeting action. No presentation request writes data.

Production-export DOM: supplemental **16/16 SQL-response-replay combinations
PASS** (four browser timezones × 320/1440px × light/dark). Scheduled → reload
with captured Cancelled GET shows explicit status/note, three history rows, no
visible or hidden target meeting link, original legacy positive action, filters/
collapse recovery, read-only requests and consistent **3:30 pm IST**. Zero
console/hydration errors. Dark Sydney mobile screenshot inspected. The existing
32-combination synthetic lifecycle/date/browser matrix is preserved and rerun.

Final controlled evidence: `QA/EVIDENCE/calendar-timezone-1791547544788`.
Final SQL capture browser evidence:
`QA/EVIDENCE/calendar-timezone-browser-1791547535684` (run ID and log SHA recorded
in each receipt). Retained/default synthetic browser:
`QA/EVIDENCE/calendar-timezone-browser-1791547525063`, 32/32 PASS.
API TRX: `QA/EVIDENCE/calendar-cancellation-20261009/test-results/`.

Important: HTTP cancellation and fresh SQL were real; browser transport replays
those captured GET responses. This is **not** a directly live browser→API mutation
or physical Android/iOS test. Synthetic auxiliary lookup/session-shell data do
not establish all live endpoints. Previous 83-page frontend build/typecheck/lint
are retained, not rerun: frontend application source did not change here.

New UTC serializer regressions **4/4 PASS** for Unspecified/UTC and Scheduled/
Cancelled, unchanged stored ticks/rows and academy/range projection. Full API
suite **1,126/1,126 PASS**, no skipped tests, on the preserved working snapshot.
Unrelated local Mini/backend work remains excluded from this publication.
`git diff --check` PASS; generated artifacts ignored, evidence intentionally local.

## Handoff / acceptance boundary

No issue closure, main/Mini change, merge, Azure, new AI tool/history or complete
enterprise/production/90% claim. Mini remains CONTRACT READY / ENGINE BLOCKED,
saved 86/94; the existing [N2u return gate](../../PENTA_HANDOFF_TO_MINI.md) is
unchanged. No engine/model/benchmark rerun in this packet.

**NEXT — Sol High / Codex:** direct loopback calendar browser/API cancellation
through the existing run-owned bridge, plus remaining event/make-up UTC source
checks. Retain these tests/fixtures and all original critical/tenant controls.
Physical-device and release gates stay open. Sol Medium is suitable only for
bounded settled visual follow-up; do not restart the audit or expand Mini tools.
