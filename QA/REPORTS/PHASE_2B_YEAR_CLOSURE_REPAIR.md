# Phase 2B — Closed academic-year term creation

2026-10-01. Sol High. Accepted [BUG-DATA-0030](../ISSUES/BUG-DATA-0030.md) reused; no repeated Astra audit. One application controller repaired locally. Issue and Phase2B remain OPEN, not release acceptance.

## Repair and regression

[AcademicPeriodsController](../../apps/api/Controllers/AcademicPeriodsController.cs) now rejects new terms under a closed own-academy year with409 and guidance. Existing valid date/name rules, successful200 DTO, missing/foreign parent400, missing/foreign close target404, open-child close409 and close-current=false semantics remain unchanged. No reopening workflow, schema, permissions or frontend changes.

CreateTerm and CloseYear alone opt into the existing domain save/audit transaction. Both use the same parameterized parent query with UPDLOCK/HOLDLOCK until transaction completion; platform-owner bypass/direct SQL calls use an owned transaction without changing existing bypass audit policy. [Microsoft documents these SQL lock hints](https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table?view=sql-server-ver17). CreateYear/current-year switching and CloseTerm are unchanged and not given new audit boundaries.

[22 new tests](../../tests/AcademyDesk.Api.Tests/AcademicYearClosureTests.cs) [baseline](../EVIDENCE/logs/phase-2b-year-closure-baseline.log):17 passed/5 failed—three closed-parent/stale-request product failures plus two expected future boundary assertions. [Final full backend suite](../EVIDENCE/logs/phase-2b-year-closure-suite.log)/[TRX](../EVIDENCE/year-closure/year-closure-suite.trx):363/363 passed (prior341 +22), no skips. InMemory alone does not certify SQL/auth/concurrency.

## Real HTTP/SQL evidence

[Harness](../tools/SqlHarness/AcademicYearClosureRegression.cs):38 exact cases in each [verified run1](../EVIDENCE/logs/phase-2b-year-closure-sql-verified-run1.log) and [verified run2](../EVIDENCE/logs/phase-2b-year-closure-sql-verified-run2.log), using identical final binaries:

- 10 open/closed/current/date-boundary/name controls,3 missing/foreign parent cases,6 close-year state/membership controls and2 stale-form steps.
- 6 POST/PATCH role controls (foreign Admin/Teacher403, anonymous401) and2 scoped list controls. Successful terms compare stored fields and response DTO; all captured prior term/year rows, current/closed flags and actor/route audits are checked. Unrelated Governance people/academy/Identity/finance/task plus course/batch/enrollment/prerequisite snapshots remain unchanged; not every database table.
- 2 real AuditLogs INSERT faults yield500 with term/year rollback;2 recovered writes prove permission restoration/lock release.3 platform-owner controls verify both transaction fallback paths and closed-parent rejection while retaining no-audit bypass.
- 2 competing close/create pairs: a held parent lock blocks the first request, then the second; SQL DMV blocking-chain evidence requires WAIT1 and WAIT2 before release. Both launch orders are tested and both winners observed. Exactly one200 and one409, one matching audit and valid state: either open year+new open term, or closed/noncurrent year+no new open term. This is bounded two-request concurrency, not stress/load certification.

[Verified isolated build](../EVIDENCE/logs/phase-2b-year-closure-sql-verified-build.log):0 warnings/errors. Two interrupted harness attempts are retained/excluded from final product counts: [initial SQL session-ID Int16 mismatch](../EVIDENCE/logs/phase-2b-year-closure-sql-run1.log), then [direct-blocker-only observation](../EVIDENCE/logs/phase-2b-year-closure-sql-final-run1.log). Harness-only fixes cast the session ID and follow queued blocking descendants; they do not relax application rules or remove the two-waiter proof. Exact owned failed containers were checked and removed.

## Isolation, acceptance and next

Generated loopback-only SQL containers/databases, least-privilege runtime login, real Testing/Identity/MVC pipeline and existing ownership preflight;80 domain/7 Identity migrations and unchanged311/300/10 route inventory/digest. Owned database/login/root/container cleanup is verified, including failed attempts; disposable synthetic data is regenerable and logs retained. Normal dev database/services, Azure/Blob/customer data untouched; no commit/push.

[Snapshot](PHASE_2B_YEAR_CLOSURE_SOURCE_SNAPSHOT.json)/[validator](../tools/validate-year-closure.cjs)/[output](../EVIDENCE/logs/phase-2b-year-closure-validation.log):167 captures,161 prior unchanged,2 QA dispatcher/wrapper changes and4 first captures (repaired controller, new test/harness/validator); controller pre-repair hash retained. Normal and historical binaries untouched; new isolated binaries pinned. Previous frontend233/TypeScript/lint results are historical, not rerun. No normal dev stack duplicate/restart.

Browser/device/stale-form error rendering, all linked scenarios/roles, other academic lifecycle writers/current-year concurrency, full critical/performance/release gates remain pending. Existing inconsistent records are not migrated or repaired. Next accepted gap: promotion decision replay/terminal-state integrity BUG-DATA-0032, Sol High; reuse its audit rather than repeat unchanged work.
