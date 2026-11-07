# BUG-DATA-MUSIC-AUDIT-001 — Music status changes and required audits need one transaction

| Field | Value |
| --- | --- |
| Status | OPEN |
| Priority | P1 |
| Scope | Music progress status PATCH |
| Baseline | `f5cad5c`, real Identity and disposable SQL, 7 November 2026 |
| Related | BUG-DATA-0054 and Cloud C8 |

SQL run `f22e8b381b8f44d9993f44b6ed5b9581` passed 22 value and authority
controls. For Admin, a required audit failure returned 500 while the updated
music progress row persisted. The Platform Owner request returned 200 and
changed the row without reaching the audit interceptor. The requests used
different scores, so each case exercised an actual mutation.

The update action now uses the existing domain and audit transaction with
Platform Owner participation. Verify native 500 and unchanged progress and
audit tables for both actors when the audit fails. A successful authorized
update must record one attributed audit. Keep the issue open for its critical
and release closure checks.

[Local C8 SQL report](../REPORTS/LOCAL_C8_MUSIC_PROGRESS_SQL_2026-11-07.md)

Repaired run `af180925d7b049e5a1bdc1b4fee60c3c` passed 25 controls, including
both audit rollbacks and the attributed Platform Owner update. Full API
regression passed 1,991 tests. Critical and release closure remains open.
