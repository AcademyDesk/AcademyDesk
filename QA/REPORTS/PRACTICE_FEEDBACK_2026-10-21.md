# Practice-log feedback and shared controls — 21 October 2026

Local `codex/penta-search`, unchanged HEAD `b1f8cb6`. Continues the existing
BUG-DATA-0043 frontend acceptance gap; the accepted API/SQL repair was not redone.

## Implemented

- Typed, validated initial academy/student/log reads; failed or malformed reads do
  not display a false empty register or permit writes.
- Active learner selection; existing whole-minute 1–1440, nonfuture UTC date and
  text boundaries mirrored for helpful input feedback. Optional text/date stays
  optional. The server remains authoritative.
- Verified create/review receipts before success or list updates; selective reset,
  accessible durable status, and retained drafts on errors.
- Synchronous overlap guard across create/review, including stale handlers. An
  uncertain write is never automatically retried: explicit read-only reload is
  required before another write. No new idempotency or backend policy claimed.
- Existing Manual/PENTA design primitives, labelled fields, shared anchored
  selectors/calendar, direct year selection and responsive action layout.
- Shared date/theme repair is detailed in `SHARED_DATE_THEME_2026-10-21.md`.

No endpoint, role, tenant, domain lifecycle, database or approval policy changed.

## Evidence

- 46 practice checks and 11 actual shared-control checks PASS (57 combined).
- Related current session/login/workspace/control/practice suite:229 PASS before
  the final failed-load display refinement; the affected57 checks reran PASS on
  that final source. Counts overlap and must not be added as distinct tests.
- TypeScript, targeted ESLint and Next webpack static export:PASS,87 routes.
- Final actual exported React browser: **40/40 PASS**, zero page errors/failures,
  across320/1440px and light/dark. Receipt:
  `QA/EVIDENCE/practice-feedback-1792561717154/result.json`.
- Browser cases cover verified/denied/server/network/malformed create,
  verified/denied/network review, denied/malformed initial reads, same-tick
  duplicate submit, draft retention, read-only recovery, active-only selector,
  direct year/month selection/reopen/Escape and hydrated theme pressed state.
- Two earlier browser attempts retained their evidence. A disabled-field locator
  was corrected to the persistent named textarea; assertions were not weakened.
  Earlier32-case passes are retained, superseded by the expanded40-case result.
- Representative mobile dark/light screenshots from the preceding32-case pass
  were visually inspected; this is not a physical-device acceptance claim.

Transport is synthetic. This is not linked Identity/SQL/browser authorization,
native Android/iOS, accessibility certification, timezone-policy/race/fault or
full-portal/release acceptance. Existing875 backend/37HTTP-SQL evidence is retained,
not rerun by this frontend batch. BUG-DATA-0043 remains OPEN.

Full configured frontend lint still FAILS:19errors/40warnings after this batch,
down from27errors/41warnings without rule suppression. Remaining Activity and
other route fixes require their own results. All model/enterprise/release gates
remain OPEN. No main/stage/commit/push/Azure/model/training changes.

Next route:Medium for settled UI/quality fixes;High for consequential host,
authority, transaction and qualified-model integration.
