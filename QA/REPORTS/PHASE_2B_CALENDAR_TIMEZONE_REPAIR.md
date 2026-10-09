# Calendar India-time consistency repair — 2026-10-09

Issue: [BUG-UI-0007](../ISSUES/BUG-UI-0007.md), **OPEN**.
Starting commit: `e36b828cc4d1b7674e73e1696dd7ce8c01bd8a13`.
Worktree: `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`.
Route: Sol Medium / Codex; existing enterprise program continues in parallel
with PENTA, not a new audit or an extension of Mini's tools.

## Change and boundary

The calendar displayed item times in Asia/Kolkata but grouped and navigated
with browser-local dates. Real UTC instants are now projected to India civil
day markers. Grid/month arithmetic uses UTC on those markers (not UTC as the
display timezone). Grid, agenda, initial month, heading, Today and filters agree
on IST. The visible month header declares IST even when the mobile description
is hidden. Initial month is resolved after mounting, so static-export HTML
does not bake in the build machine's date. Its owned timer is cleaned on unmount.

No session timestamps, create/update rules, API requests, sorting, teacher or
location precedence, meeting URL rules, permissions, CSS or database changes.
Classes, make-ups and events retain their previous actions. Cancelled-session
handling is the separate still-open BUG-FUNC-0021; it is not fixed here.

## Evidence and validation

- Expanded existing `calendar-details.test.cjs`; all 39 previous controlled
  projection cases remain. Eleven new cases cover India midnight, month/year
  rollover, leap day, US/Sydney DST, initial month, Today/navigation and all
  three item types. Run under UTC, Asia/Kolkata, America/Los_Angeles and
  Australia/Sydney: **50/50 per timezone, 200/200 total PASS**.
- Production-export Edge/Playwright DOM: four browser timezones, 320/1440px,
  October/January boundaries: **16/16 PASS**. Checks include three same-day
  items, agenda/date/time parity, initial month, Today, previous/next month,
  outside-month marking, filters, collapse/expand, mobile horizontal grid
  scrolling and visible IST label. No whole-page overflow, unexpected console
  or hydration errors, writes or navigation-triggered calendar refetches.
- TypeScript, targeted calendar ESLint, browser-runner syntax, `git diff`
  whitespace checks and production webpack static export (**83 pages**) PASS.
- Mobile screenshot inspected. Existing narrow-screen grid scrolling and
  agenda truncation are not redesigned or certified by this date repair.

The first controlled baseline recorded 132/200 passing, 68 failing observations.
This includes selected-month fixture assumptions under non-India timezones and
one new QA punctuation expectation (`Oct, 2026` vs `Oct 2026`), not 68 distinct
product defects. India-boundary grid day and initial-month failures reproduced
the registered defect. The punctuation expectation was corrected without
weakening date/year assertions. A first browser run used an event-specific
locator after filtering to classes; that QA locator was corrected to the Today
cell, preserving filter/date assertions. The first synchronous initialization
failed the React effect lint rule; the final owned mount timer passes lint.
An added prerender assertion initially expected boolean `true` instead of the
literal JSX ARIA value `"true"`; the exact-value check was corrected, retained.
All failures remain in local evidence or the command transcript.

Evidence (intentionally local, never staged):

- `QA/EVIDENCE/calendar-timezone-1791545128553`: pre-fix controlled baseline.
- `QA/EVIDENCE/calendar-timezone-1791545161833`: post-fix punctuation mismatch.
- `QA/EVIDENCE/calendar-timezone-1791545305027`: initial 200/200 final-source
  arithmetic checks before the presentation-only IST header label addition.
- `QA/EVIDENCE/calendar-timezone-browser-1791545339397`: initial 16/16 DOM pass.
- `QA/EVIDENCE/calendar-timezone-browser-1791545555410`: final 16/16 DOM run,
  including visible IST and horizontal grid scroll on 320px mobile.
- `QA/EVIDENCE/calendar-timezone-1791545602991`: retained QA ARIA mismatch.
- `QA/EVIDENCE/calendar-timezone-1791545653300`: final 200/200 controlled run,
  including the date-independent initial prerender assertions.

The optional captured-SQL make-up fixture now initializes via its own frozen
clock rather than assigning an arbitrary instant to the month state. Its
location/action assertions remain unchanged. That opt-in fixture was NOT RUN;
this change does not claim new SQL coverage.

## Not run / acceptance boundary

No new live API/Identity/SQL, Mini inference, whole enterprise suite, physical
Android/iOS, spoken screen reader or Azure test. Older API/SQL evidence is
retained, not rerun or relabelled. Synthetic transport is not API/SQL proof.
No SQL container was created or removed. Browser runner closes only its own
browser and loopback server. Other local work and QA/EVIDENCE are preserved.

BUG-UI-0007 remains OPEN until linked API/browser, original critical regression
and applicable physical-device acceptance are complete. Enterprise/release
gates are not closed. Mini remains CONTRACT READY / ENGINE BLOCKED at saved
86/94; original qualified return and SQL/security/15-turn read gates still apply.

## Handoff

Next independent application item: inspect BUG-FUNC-0021 against current
session status contracts, then mark cancelled classes nonactionable while
retaining history, if the existing policy supports that bounded fix. Sol Medium
for settled presentation; Sol High if lifecycle/domain policy is unresolved.
Mini work stays with the existing [N2u handoff](../../PENTA_HANDOFF_TO_MINI.md),
Sol High; do not reinterpret prompts or repair intelligence in the host.
No main merge or Azure deployment is authorized by this result.
