# Cancelled calendar sessions — 2026-10-09

Issue: [BUG-FUNC-0021](../ISSUES/BUG-FUNC-0021.md), **OPEN**.
Starting HEAD: `75c1f7206e1a919fedab3361d4e86bf8c6bcfc3c`.
Worktree/branch: `D:\AcademyDesk-codex-p0`, `codex/penta-search`.
Route: Sol Medium / Codex. Bounded frontend repair in the existing enterprise
program; not a new AI tool, audit restart or lifecycle-policy rewrite.

## Verified source contract and implementation

`ClassSessionsController.List` already returns `Status` without dropping history.
Admin update accepts Scheduled/Completed/Cancelled/NoShow; Teacher portal also
accepts InProgress/Rescheduled and returns the stored status. Calendar previously
omitted that field and rendered cancelled sessions as ordinary joinable classes.

The existing issue permits clearly marked, nonactionable cancellation history.
The repair follows that option, without deleting or filtering historical rows:

- Retain all session records and original UTC date, teacher, delivery and location.
- Carry status into the grid and agenda; normalize known status labels without
  mutating the stored/API value. Missing status is **Not specified**, never an
  invented Scheduled claim. Unknown status is escaped literal text. Use a Map,
  not prototype-inheriting object lookup (`constructor`/`__proto__` regressions).
- Cancelled classes are non-link elements in the grid and have no agenda Open
  action, for Online/Hybrid and InPerson, including case/space variants and
  legacy batch-link fallback. Stored meeting URLs remain plain history text.
- Explicit Cancelled text, themed border/background and the agenda explanation
  **This class is cancelled. Meeting access is unavailable.** do not rely on color.
- Other statuses retain their prior meeting behavior. Completed/NoShow/
  Rescheduled join eligibility is not redefined by this fix.

No API, domain, SQL, auth, schema, timestamps, Mini, source receipts, broad CSS
or portal change. Four scoped calendar CSS rules only. This disables
Academy calendar links, not the external provider's meeting or copied URLs.

## Controlled regression

Expanded the existing actual-TSX tests: all 50 earlier projection/timezone
checks retained plus 31 cancellation/legacy/status/filter cases. **81/81 per
timezone, 324/324 total PASS** across UTC, Asia/Kolkata, Los Angeles and Sydney.

Pre-fix evidence: `QA/EVIDENCE/calendar-timezone-1791545917805`, 29 new cases
fail in each zone. One additional Sydney old-fixture failure exposed a real
timer scheduling assumption in the controlled harness, not another product
defect. The harness now explicitly queues/flushes the production mount callback;
no assertion or application timer is removed. Production timers are exercised
separately in the real browser. The first post-fix matrix also expected four
same-day grid cards instead of the existing three-card/+1-more cap; that QA
expectation was corrected and now verifies the retained cap explicitly.

- Post-fix cap mismatch retained: `QA/EVIDENCE/calendar-timezone-1791545983034`.
- Final controlled pass: `QA/EVIDENCE/calendar-timezone-1791546089088`.

## Browser and build

The existing production-export browser runner retains all earlier date,
month/year/Today, filters, collapse and mobile horizontal-scroll assertions.
It additionally reloads with 27 synthetic lifecycle rows (nine statuses × three
delivery modes), checks exact grid/agenda labels and history counts, six
cancelled records with no visible/hidden meeting links, positive non-cancelled
destinations, filter/collapse recovery, read-only requests and no filter refetch.
Run across four browser timezones, 320/1440px, October/January rollover and
light/dark: **32/32 combinations PASS**, zero unexpected console/hydration
errors. Final evidence and screenshots:
`QA/EVIDENCE/calendar-timezone-browser-1791546113284`. Dark mobile cancellation
card screenshot inspected; explicit cancellation text, retained history and no
Open meeting action observed. This is emulation, not physical-device proof.

TypeScript, targeted calendar ESLint and production webpack static export
(83 pages) PASS. Previous backend/SQL results are retained, not rerun.

## Acceptance boundary and handoff

BUG-FUNC-0021 stays OPEN. Synthetic response transport is not real cancellation
mutation/Identity/SQL persistence proof. Linked API/browser transition, original
critical regression and physical Android/iOS acceptance remain open; no full
WCAG/spoken-screen-reader or production/enterprise acceptance is claimed.
No SQL container was created/removed; runner closes only its own browser/server.
QA/EVIDENCE stays local and all unrelated dirty work remains preserved.

**NEXT — Sol High / Codex:** extend the existing CalendarDetails disposable
HTTP/SQL harness with a scoped Scheduled → Cancelled transition, fresh SQL and
GET readback; consume that real fixture in the calendar/browser regression.
Keep tenant/role negative controls and run-owned cleanup. Do not replace the
synthetic tests or weaken the original SQL runner. No customer/dev DB writes.

Mini's return is unchanged (2026-10-09 15:30:49): CONTRACT READY / ENGINE BLOCKED,
saved 86/94. [Existing N2u handoff](../../PENTA_HANDOFF_TO_MINI.md) remains the
Sol High route for qualification, followed by original SQL/security/15-turn
source and human-view gates. This calendar slice does not qualify Mini or
authorize another PENTA action/history vertical. No main merge or Azure deploy.
