# BUG-FINANCE-DECISION-GUARDS-001 — Finance decision reason and audit enforcement

| Field | Value |
| --- | --- |
| Status | OPEN |
| Priority | P1 |
| Scope | Finance adjustment decision PATCH |
| Baseline | `746c69f`, real Identity and disposable SQL, 7 November 2026 |
| Related | BUG-FUNC-0005 and C6 live Manual acceptance |

SQL run `6a0a2ff2b855463a8c8a2a2d9dba7178` reproduced four failures. Three
rejection requests with null, empty or whitespace notes each returned 200 and
changed persisted state. The Platform Owner approval returned 200 and changed
persisted state without reaching the required audit interceptor. The equivalent
Admin audit-failure control returned 500 and rolled back. Thirty-six other
native controls passed.

The repair enforces the existing rejection-note requirement in the controller
and applies the existing domain and audit transaction to Platform Owner
decisions. Approvers can still submit an empty optional note. Existing invoice
locking and role and tenant checks apply to the decision.

Verify native 400 and unchanged SQL for all three blank-note cases. Verify
native 500 and rollback for both actors when the audit fails. A successful
authorized decision must persist one audit with the actual actor and tenant.
Keep this issue open for the established critical and release checks.

[C6 live acceptance report](../REPORTS/LOCAL_C6_LIVE_CONTROLS_2026-11-06.md)

## Repaired acceptance on 7 November 2026

SQL `ac6860b7f7734f26beee7e37ae216824` passed all 41 native controls. All three
blank notes returned 400 with unchanged SQL. Both actors returned 500 and rolled
back after the audit fault. A successful Platform Owner decision added one
attributed audit. The browser passed all 13 phases and the full API suite passed
1,989 tests. Critical and device closure remains open.
