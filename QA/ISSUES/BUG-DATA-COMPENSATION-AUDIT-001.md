# BUG-DATA-COMPENSATION-AUDIT-001 — Compensation save and required audit are not atomic

| Field | Value |
| --- | --- |
| Status | OPEN |
| Priority | P1 |
| Scope | Teacher compensation PUT; Admin and Platform Owner |
| Reproduction | Real disposable Identity/HTTP/SQL, 6 November 2026 |
| Baseline | Host `c92490f`; unchanged application, new bounded SQL regression |
| Related | BUG-DATA-0050 / Cloud C7 live acceptance |

The original save commits CompensationJson before the shared filter saves its
required attributed audit. Injecting an audit-save failure returned HTTP 500 for
an Academy Admin while fresh SQL retained the changed compensation. Platform
Owner bypassed the filter, returned 200 and never reached the required audit.

Observed baseline SQL run `64c397682ee840e4b4f10f447d3fb8d1`, window
`346f835d79c74832be1b2b956a88dc43`: 24 value/readback/rejection/authority controls
passed; Admin faultHits=1 / state unchanged=False, Platform Owner status=200 /
faultHits=0. The second request reused the same candidate value, so its unchanged
data flag is not evidence of rollback. Both actual failures are retained locally.

Implemented scoped repair opts only TeacherCompensationController.Save into the existing
AtomicAcademyMutation domain transaction and explicitly includes Platform Owner.
No Identity write, permission grant, payroll/zero-net rule, schema or API payload
change is intended. Reads retain their existing behavior.

Required retest: identical captured browser bodies roundtrip exactly through
current HTTP and fresh SQL; invalid/anonymous/Teacher/foreign writes remain denied;
each successful authorized save adds exactly one attributed audit; forced audit
failure returns 500 and leaves compensation, other teachers, audit, money and
Identity unchanged for Admin and Platform Owner. Run API regression. Keep this
issue OPEN until the established critical/release closure requirements are met.

Source-bound report: ../REPORTS/LOCAL_COMPENSATION_SQL_2026-11-06.md.

## Bounded retest — 6 November 2026

Fresh SQL 7f8e7cd7937a477791af866108b4d7f6 passes all 28 controls, including
Admin and Platform Owner audit-failure rollback and normal one-audit saves. Full
API suite: 1,986 PASS, zero failed/skipped. Owned cleanup, original services and
main preservation PASS. Status stays OPEN for remaining critical/release gates;
see the source-bound report for the retained failed baseline and exact limits.
