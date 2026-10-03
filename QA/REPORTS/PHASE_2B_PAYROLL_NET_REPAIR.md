# Phase 2B — Negative-net payroll guard repair

2026-10-01. **Local negative-net original-case/module retest PASS twice; zero-net POLICY-PENDING; issue/phase NOT CLOSED.** Accepted Astra audit and twice-failing Phase 2A evidence reused, not re-audited. Continue agreed Sol High allocation. No commit/push, Azure access/deployment, normal development database change, bank disbursement or additional listening development frontend/backend. TestServer is the existing isolated in-process QA harness.

## Repair and before-fix evidence

[BUG-DATA-0003](../ISSUES/BUG-DATA-0003.md): one guard in PayrollController.Pay rejects deductions greater than the effective gross before constructing/saving a payout. Monthly gross remains the profile amount; SessionBlock gross remains the explicit request amount or cycle default. Existing positive gross/session, nonnegative deduction and active/own-profile checks are unchanged. No schema, rate/override authorization, historical ledger correction or zero-net policy change.

Initial new-test run: 8 PASS/8 FAIL, including **five fixture binding failures** (integer InlineData could not bind nullable decimal). Those five are not product findings. [Initial log](../EVIDENCE/logs/phase-2b-payroll-net-before-initial.log). Corrected the test argument representation before product repair; corrected before-fix run: **12 PASS/4 expected FAIL**, all accepted negative-net payouts (Monthly 1000.01/1001; SessionBlock default 1000.01 and explicit gross 250/deduction 250.01). [Corrected before-fix log](../EVIDENCE/logs/phase-2b-payroll-net-before-corrected.log) is tool-output limited/truncated in some stack traces; count/result retained, not claimed complete raw output. Original twice-failing real HTTP/SQL evidence remains unchanged in [Phase 2A report](PHASE_2A_PAYROLL_BOUNDARY_REPRO.md).

## Final-source verification

- **118/118 API tests PASS**, zero failures/skips: 16 new payroll cases plus existing 102, including prior reconciliation/adjustment tests. [Test log](../EVIDENCE/logs/phase-2b-payroll-net-tests.log). InMemory cases cover positive/cents/negative deductions, default/override SessionBlock gross, zero gross/sessions, Monthly ignoring request gross, inactive/foreign profile refusal; not full HTTP authorization certification.
- Harness build PASS, zero warnings/errors. [Build log](../EVIDENCE/logs/phase-2b-payroll-net-build.log).
- Two fresh isolated SQL Server 2022 containers with real Program/Identity/TestServer, exact owned context/runtime login, fresh no-tracking profile/payout snapshots. Both exit 0; migration histories 80 application / 7 Identity. Runtime inventory 307 routes, 296 controller method/routes, 10 framework Identity method/routes; digest `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`.

| Run | UTC start | Loopback SQL port | Duration | Evidence |
| --- | --- | --- | --- | --- |
| `8e69da3817134e7fae00763bc04227c7` | 2026-10-01T05:49:47.1260903Z | 54546 | 44.7 s | [First run](../EVIDENCE/logs/phase-2b-payroll-net-sql-run1.log) |
| `635127d5b0824bfd8a4b4005f9eeb0bb` | 2026-10-01T05:50:50.6220547Z | 58058 | 37.2 s | [Second run](../EVIDENCE/logs/phase-2b-payroll-net-sql-run2.log) |

Both runs pass health 200, anonymous protected 401, actual Identity login/bearer 200 and two-tenant own student reads/cross-tenant GET/POST 403 with no foreign student write. Payroll APIs use real entitlement/authorization/audit middleware, not mock auth. These tenant controls do not exhaust payroll-specific permissions.

Each run has **14 rule cases PASS plus one zero-net observation POLICY-PENDING**, not 15 fully accepted cases:

- Monthly gross 1,000: deductions 0/999/999.99 return 201 with exact SQL/list net 1,000/1/0.01; deductions 1000.01/1001/−0.01/−1 return 400 with captured profile/ledger unchanged. Zero negative-net Paid rows persist.
- Deductions exactly 1,000 still return 201, persist one Paid row with net 0 and appear in list. Existing behavior preserved; not a policy acceptance PASS. Requires explicit business decision before closure.
- SessionBlock cycle 1,000/12 sessions: default excessive 1000.01 rejected; explicit gross 250/3 sessions with deductions 250.01 rejected. Explicit 249.99 deductions returns exact net 0.01; default 999 deductions returns net 1/12 sessions. Explicit zero gross, zero sessions and negative deductions rejected without captured financial changes. Accepted response/fresh SQL/payout-list gross, deductions, net and sessions agree; optional method/reference default to BankTransfer/null.

Snapshots preserve all earlier payout rows and profile fields through rejected requests. No-write assertions concern the captured profile/payout ledger, not all tables/audit logs. Data are synthetic; no real worker was paid. Boundary arithmetic uses decimal(18,2) cent values, not an invented sub-cent rounding rule.

## Evidence, cleanup and limits

[74-record source snapshot](PHASE_2B_PAYROLL_NET_SOURCE_SNAPSHOT.json) extends prior 70 with PayrollController, payroll reproduction/regression and unit tests. Of previous captures only harness Program/selected-finance runner changed; previous finance repair/frontend/media hashes unchanged. Product diff is the single PayrollController guard. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`; unrelated dirty changes preserved. [Validation/cleanup](../EVIDENCE/logs/phase-2b-payroll-net-validation.log); `git diff --check` PASS, line-ending warnings only.

Runner reuses `Run-ReconciledPayment.ps1 -Module Payroll` with strict `--payroll-net`; failure throws/nonzero after original boundary observation and before reporting module success. Historical no-argument reproduction remains observational. This explicit bounded gate skips other finance/media/transition modules; it is not a full critical suite PASS or phase/release acceptance.

Harness ownership cleanup removed both generated databases/logins/temporary host folders, followed by exact name/label/loopback-checked container stop/removal. Independent final inventory confirms both containers, owned roots and SQL listeners absent. Only synthetic current-run data removed; older interrupted resources and normal dev/Azure data untouched.

Zero-net decision, sub-cent/rounding/limits, duplicate periods/idempotency, profile updates/rate/override permission, historical invalid ledger cleanup, broader payroll-specific role/tenant/browser/device and full critical regression remain pending. No frontend lint/typecheck/build/recording-device rerun. Related adjusted Student/document/dashboard/reminder balance follow-up from BUG-DATA-0002 stays queued, not silently fixed/certified.

## Next bounded task

Proceed to already reproduced [BUG-DATA-0010](../ISSUES/BUG-DATA-0010.md): payment void/invoice state and reconciliation-evidence consistency. Agreed **Sol High**, use accepted reproduction rather than repeating Astra audit. Policy-sensitive Voided→collected restoration remains explicitly pending; do not invent permission to restore money or silently close it. Zero-net payout also remains pending. Phase 2B/issues/media/device/critical/policy/release gates remain open; no deployment.
