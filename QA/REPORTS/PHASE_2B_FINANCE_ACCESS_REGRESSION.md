# Phase 2B — Finance action-level role/tenant regression

2026-10-01. **Bounded current-rule role/tenant regression PASS twice, 129 cases per run; known lookup/audit gaps OPEN, Phase 2B NOT CLOSED.** Agreed Sol High; accepted Astra G03 mappings reused, not re-audited. QA-only extension of the existing in-process TestServer/SQL harness. No application authorization/permission change, normal dev database change, extra listening frontend/backend, commit/push or Azure access/deployment.

## Scope and exclusions

Six actor classes, each exercising 16 actions: AcademyAdmin, Owner, FinanceUser, custom finance.manage-only role, Teacher and Student. Actions are Invoice List/Create/Status; Payment List/Create/Reconcile/Void; Adjustment List/Create/Decide; Reminder Queue; Payroll Profiles/CreateProfile/UpdateProfile/Payouts/Pay. This is a bounded subset of the accepted all-role matrix, not all 12 system roles or every finance controller/action.

The expected current-rule distinction is preserved: Admin/Owner allowed all 16; FinanceUser/custom finance.manage allowed 11 finance/reminder actions but denied five unmapped Payroll actions; Teacher/Student without grants denied all 16. Current-rule PASS is not approval of product-role policy or maker-checker/restoration behavior. Allowed positive-net synthetic payouts record rows only, not bank payments. All fixtures use local owned SQL; fixture entities are not customer data.

Additional expected controls: 11 foreign-academy route writes; seven foreign-row guards under own academy; anonymous payment; custom grant absent/valid/expired/revoked using the same bearer, plus grant cannot authorize unmapped Payroll; FinanceControls/Finance disabled; inactive academy/user; PlatformOwner role-only versus database flag cross-tenant action. Expected 129 cases total per completed run. Flagged-owner bypass of inactive academy/module and deactivated flagged user are not sampled here.

Each rejected/read action compares fresh no-tracking snapshots of all current-run invoices, payments, adjustments, payroll profiles/payouts and notifications across both tenants. Allowed actions verify response IDs/target fields in fresh SQL; lists exclude foreign fixture invoice/profile IDs, including a seeded foreign payout. Audit counts are observed separately: rejected controller actions can append audit rows under existing [BUG-API-0002](../ISSUES/BUG-API-0002.md), so audit growth is not called a financial mutation or silently treated as a clean audit acceptance. No claim of all-table rollback or exactly-once auditing.

Known [BUG-FUNC-0006](../ISSUES/BUG-FUNC-0006.md) dependency checks compare FinanceUser allowed direct finance actions with blocked Students/AdminWorkItems GET prerequisites. API dependency reproduction is separate from current authorization-rule PASS; live pages and full load chain remain NOT RUN. No broad students.manage permission is granted as a workaround.

## Build and fixture attempts

Initial new QA-module build failed with tuple-name inference and expression-tree local-function errors (4 warnings/17 errors); corrected in QA only. [Initial compiler log](../EVIDENCE/logs/phase-2b-access-build-initial.log). Intermediate compiler repair passed; then initial SQL fixture attempt `d823dfb1f8fd44268cf1332b5c3fe3ee` on loopback 57825 failed before access cases because a 42-character synthetic invoice number exceeded the column limit. [Fixture attempt log](../EVIDENCE/logs/phase-2b-access-attempt-fixture.log). Neither is a reproduced application defect or passing access run. Fixture changed to 39 characters and seeded foreign payout/read coverage added. Exact failed-run name/label/loopback ownership was checked before removing only that container and its disposable database/login; no independent SQL DROP/login-cleanup claim for that failed attempt. Temp root/listener/container absence is independently checked in validation.

Corrected final harness build PASS, zero warnings/errors. [Build log](../EVIDENCE/logs/phase-2b-access-build-corrected.log). Application/frontend hashes unchanged, so previous API 140/140, page 7/7 and typecheck are retained historical results, not newly rerun or claimed as new passes. Frontend/browser/device/lint/production-build NOT RUN in this QA-only slice.

## Final real Identity/HTTP/SQL runs

Both runs exit 0 with all 129 cases PASS, six actor matrices complete, and seven separate rejected-action audit observations each. No new authorization-rule mismatch found in this bounded set. Actual status codes are retained per case in the logs; controller-level foreign rows return 404 (five) or 400 (two), not assumed all 403. Cross-tenant route writes and lacking role/grant/module/inactive gates return 403; anonymous payment returns 401. Captured financial/notification snapshots stay unchanged on those refusals. Current DB role/grant/active/flag changes are observed on existing bearer tokens; permission provisioning routes themselves are not certified.

| Fresh run | UTC start | Loopback SQL port | Duration | Evidence |
| --- | --- | --- | --- | --- |
| `a4c20f76c0d947a883a1d36e72f1d0e0` | 2026-10-01T06:26:33.6863918Z | 53407 | 143 s | [First corrected run](../EVIDENCE/logs/phase-2b-access-sql-run1.log) |
| `4a6d4995ede14ec29eb41aaee4c20f06` | 2026-10-01T06:29:18.0702962Z | 50316 | 140.3 s | [Second corrected run](../EVIDENCE/logs/phase-2b-access-sql-run2.log) |

Each uses two exact owned SQL contexts/runtime login, actual Identity/middleware, migration histories 80 application/7 Identity. Health 200, anonymous protected 401, actual login/bearer 200 and original two-tenant GET/POST controls PASS. Runtime digest unchanged `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`, 307 routes/296 controller method-routes/10 framework Identity method-routes. Module completion rejects a mismatched ownership marker, removes run-owned database/login/host roots, then exact labelled loopback container. Independent final checks confirm both current containers/roots/listeners and initial failed fixture container/root/listener absent. Only disposable current QA resources removed; earlier interrupted resources and normal dev/Azure untouched.

Both runs reproduce the same existing API dependency: FinanceUser direct finance actions succeed, but Students and AdminWorkItems GET return 403 without financial changes. Live browser workflow NOT RUN. Seven foreign-row controller refusals each append one generic audit row despite unchanged financial state; that is partial evidence for BUG-API-0002's rejected-action branch, not reproduction of the primary post-save audit-failure case. Flagged cross-tenant payment succeeds with audit delta 0, preserving current bypass behavior rather than approving an audit omission. No new issue or permission weakening introduced.

## Source and limits

[85-record snapshot](PHASE_2B_FINANCE_ACCESS_SOURCE_SNAPSHOT.json) extends prior 81 with QA access module and existing filter/permission/module source. Of previous captures only harness Program and runner changed; all 49 previously captured application/test records remain unchanged, HEAD `20bb6047f9edf733ac8e2a226621cc582ec54b3c`. [Validation/cleanup](../EVIDENCE/logs/phase-2b-access-validation.log).

Explicit `Run-ReconciledPayment.ps1 -Module Access` selects `--finance-access`; strict status/snapshot/persistence assertions throw on failure. Requests are naturally paced at 650 ms minimum, with the real 120/min limiter, middleware, audit and Identity preserved. No mock auth or rate-limit bypass. This module is not the full critical suite, permission API provisioning certification, all-role/custom-grant combinations, concurrent finance writes, Guardian/UI permission checks or restoration/zero-net policy acceptance. Phase 2B and issues remain OPEN.

## Next bounded task

Existing shared save/audit ambiguity [BUG-API-0002](../ISSUES/BUG-API-0002.md): isolate a post-business-save audit failure and rejected-action logging through the real pipeline, then repair the confirmed path with scoped regression. Continue **Sol High**; use accepted source findings, not another Astra audit. Preserve unresolved restoration/zero-net policy, finance lookup least-privilege design, broader concurrency/critical/media/device/release gates; no deployment.
