# Schedule success feedback — 2026-10-09

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD
`602d08a292ac07613bc491c577c16534abcccaef`. Existing BUG-FUNC-0003 continuation,
not a new enterprise plan or whole-form acceptance.

## Reproduction and repair

The actual Schedule create handler gave no success notice; status update supplied
one that `load` cleared. Rejected network requests also escaped the handlers and
pending status state ended before readback. The new controlled baseline against
HEAD has 23 cases: 4 PASS / 19 FAIL, zero skipped/cancelled. Evidence:
`QA/EVIDENCE/schedule-feedback-baseline-final-20261009.log`.
An initial baseline run was interrupted because the new pending-test fixture
awaited a blocked duplicate before releasing its gate. The fixture now releases
the gate in finally; original assertions remain. Initial failure evidence retained.

Only Schedule production code changes. Confirmations survive refresh; a committed
save followed by failed refresh remains explicitly confirmed with reload/no-repeat
guidance. 5xx/network write outcomes are unconfirmed, never success; 4xx retains
existing rejection wording. No automatic retry. A synchronous ref blocks stale
duplicate/opposing handlers, and controls stay disabled through readback. Finally
always releases pending state. Per-class time/location clears only after confirmed
create; batch/teacher/branch/mode and existing IST/UTC payloads/default rules remain.
The polite atomic status region is visible in the current page. No redirect or
unsupported autofocus; the user remains in Schedule with the refreshed list.
Inputs/status selectors now have accessible names. No domain/API/SQL/authority,
AI runtime, delivery rules or lifecycle changes.

## Validation

- 23 new actual-TSX feedback checks + all 18 existing Schedule-default checks:
  41/41 PASS; no assertions removed. Existing hook fixture only gained stable
  useRef support. Log `QA/EVIDENCE/schedule-feedback-final-20261009.log`.
- Exported Schedule browser: 12/12 PASS at 320/1440, light/dark, normal/
  refresh-failure/rejected responses, create then status-update, input reset or
  retention, exact single POST/PUT, UTC/null payload and no horizontal overflow.
  No unexpected JS/hydration/console errors; deliberate 400/503 resource errors
  are separately counted. `QA/EVIDENCE/schedule-feedback-browser-1791565610425`.
  Reviewed 320px light screenshot. Synthetic API transport, not live Identity/SQL.
- TypeScript PASS; Schedule ESLint exit 0, zero errors and one inherited
  exhaustive-deps warning. Production webpack export 83 pages PASS.
- Syntax and git diff checks PASS. No backend suite or fresh SQL/device rerun
  for this frontend-only change; previous API/calendar/SQL evidence retained.

BUG-FUNC-0003 remains OPEN for other forms, linked SQL/browser, physical devices
and full critical regression. Do not claim all forms or enterprise readiness.
No containers created/removed, main/Mini/Azure touched or production data accessed.
Unrelated local work and QA/EVIDENCE preserved and excluded from publication.

NEXT: Attendance feedback variant of BUG-FUNC-0003, Sol Medium for bounded UI;
Sol High if persistence/authority/source contracts need change. Mini remains saved
86/94 CONTRACT READY / ENGINE BLOCKED under its existing qualification handoff.
