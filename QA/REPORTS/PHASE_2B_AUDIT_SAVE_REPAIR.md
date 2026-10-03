# Phase 2B — Finance save/audit boundary repair

2026-10-01. **Primary fault reproduced twice; local finance repair PASS twice (six audit +129 access cases per run), API 155/155 PASS. BUG-API-0002 and Phase 2B remain OPEN.** Agreed Sol High. Accepted Astra findings reused, not re-audited. Laptop-only changes; no commit, push, Azure access/deployment, or normal dev database mutation.

## Reproduction and repair

Two fresh guarded Identity/MVC/SQL runs reproduce [BUG-API-0002](../ISSUES/BUG-API-0002.md). The harness denies INSERT on only the disposable run-owned AuditLogs table for its generated runtime principal. Business saves still have normal permissions. Each invoice request returns 500 but creates one invoice; each payment request returns 500 but creates one payment and changes its invoice to Paid. No audit row is persisted. The same action-level 400/404 refusals leave financial rows unchanged but each append a generic audit. These are runtime evidence, not a second static review.

The scoped [AcademyAccessFilter](../../apps/api/Security/AcademyAccessFilter.cs) repair:

- Inspects pending IActionResult status (including ProblemDetails, Forbid and Challenge), canceled/exception state and any written response failure before creating a success audit. MVC has not yet written a pending BadRequest/NotFound/etc status into Response.StatusCode at this point.
- Starts a relational transaction before ordinary audited mutations on the existing domain-only finance allowlist: FeePlans, Invoices, Payments, Expenses, FinanceAdjustments, FinanceGovernance, FeeReminders, AcademyExports and added Payroll. Business SaveChanges calls and generic audit save share the scoped AcademyDeskDbContext and commit together. An audit failure or failed action exits without commit; transaction disposal rolls back. Read methods/AuditLogs exemption unchanged; AcademyExports currently exposes a read action.
- Leaves authorization, module/grant rules, platform-flag bypass, other controllers and Identity/Blob transactions unchanged. No permission broadening, swallowed audit error, new migration, outbox or response contract.

The transaction is not a cross-store solution, invoice concurrency lock, exactly-once retry protocol or protection from a lost response after commit. Non-finance post-save audit ambiguity remains to be addressed. Platform-flag bypass continues to skip the generic audit/boundary. Do not infer protection for student/teacher Identity updates, provisioning or media completion with its own SQL/Blob lifecycle. InMemory unit tests test pending status decisions, not SQL atomicity.

## Executed evidence

| Stage | Fresh run / evidence | Result |
| --- | --- | --- |
| Before-fix SQL 1 | `3dc0fd1d57ec426c856e55510af4ee0c`; loopback 62559; UTC 06:38:36.4342321; 34.2 s; [log](../EVIDENCE/logs/phase-2b-audit-baseline-run1.log) | Six cases; invoice/payment fault and rejected-action defect reproduced; normal write/read controls PASS; owned cleanup exit 0 |
| Before-fix SQL 2 | `0f05f15a5e324c24a6af92453d644c34`; loopback 62585; UTC 06:39:50.0646597; 25.4 s; [log](../EVIDENCE/logs/phase-2b-audit-baseline-run2.log) | Same reproduction and controls; owned cleanup exit 0 |
| Final API suite | [155/155 tests](../EVIDENCE/logs/phase-2b-audit-api-tests.log) | PASS, zero failed/skipped; 15 added status/outcome tests plus previous 140 |
| Final harness build | [build](../EVIDENCE/logs/phase-2b-audit-build.log) | PASS, zero warnings/errors |
| Repaired SQL 1 | `203ec6a1daf44e0e8fe849c8165ba522`; loopback 64979; UTC 06:41:12.1687420; 126.2 s; [log](../EVIDENCE/logs/phase-2b-audit-fixed-run1.log) | Six audit cases +129 finance access cases PASS, cleanup exit 0 |
| Repaired SQL 2 | `652b0a69c44d428c89d35667da9914e6`; loopback 61331; UTC 06:43:32.8775263; 143.8 s; [log](../EVIDENCE/logs/phase-2b-audit-fixed-run2.log) | Six audit cases +129 finance access cases PASS, cleanup exit 0 |

Each repaired run checks injected audit denial yields 500 **without** a new invoice/payment; payment's invoice stays PartiallyPaid and the captured invoice/payment/adjustment/profile/payout/notification snapshot is unchanged. 400/404 actions create no generic audit. Permission fault is removed in finally. A subsequent invoice request returns 201 with its response ID/amount in fresh SQL and one correctly attributed actor/academy/route audit; GET returns 200 without audit growth. This is a bounded request recovery test, not global idempotency.

The existing 129-case role/tenant module is strengthened to throw if any read/refusal grows the audit. It retains six actors ×16 actions plus own/foreign routes/rows, grants, modules, inactive academy/user and platform role-vs-flag controls. Seven previously observed foreign-row success audits now have delta zero. FinanceUser Students/AdminWorkItems lookup 403 remains an existing OPEN API dependency; no students.manage workaround. Reminder business audit plus generic audit may correctly add two rows, so no false all-writes-exactly-one assertion. Flagged cross-tenant payment retains audit delta zero and bypass semantics; this is observed, not approved.

Both baseline and repaired runs preserve real auth, rate limiter, MVC exception handler, SQL and audit (except explicit disposable table INSERT fault). Runtime endpoint digest remains `04C6E9D46574388934AA809302CCB3C6240B8494D320B9E61D089838624539D9`: 307 routes/296 controller method-routes/10 framework Identity method-routes. Resolved contexts/runtime credentials match exact owned SQL, migrations 80 application/7 Identity; health/auth/two-tenant controls remain. No additional listening application server/frontend is created.

## Source, cleanup and limits

[92-record final snapshot](PHASE_2B_AUDIT_SAVE_SOURCE_SNAPSHOT.json) extends previous 85: filter and three QA harness files changed, seven source records added (QA fault module, friend assembly declaration, unit tests, four existing allowlist controllers). Of 52 prior application/test captures, only the filter changed; 51 are unchanged. HEAD remains `20bb6047f9edf733ac8e2a226621cc582ec54b3c`. Existing unrelated dirty work preserved. [Validation](../EVIDENCE/logs/phase-2b-audit-validation.log) records hashes, evidence/link checks and exact resources absent after the second run.

Each wrapper refuses wrong ownership-marker cleanup, removes its run-owned database/login/host folders, then rechecks exact container name/label/loopback ownership before removing that container. Only four current disposable QA containers/data are in scope; earlier interrupted resources are untouched. No dev/Azure schema/data/keys changed.

The first read-only final validator falsely rejected the declared source differences because of PowerShell join/comparison precedence. Corrected parentheses pass the same 92 hashes and exact declared differences; both attempts are retained in the validation log. This is a QA validator repair, not a product defect or another SQL run.

Browser, frontend, Android/iOS, all-role combinations, concurrent mutations, all-table rollback, result-serialization/commit faults and full critical suite NOT RUN. SQL fault coverage is invoice/payment, not fault injection on every allowlisted action. Other domain-only writes, cross-context Identity/media/provisioning and platform-owner behavior remain pending; phase and issue are not closed.

## Next bounded task

Continue BUG-API-0002 for **non-finance domain-only student/teacher creation** using accepted lifecycle mappings and the existing guarded harness. Use action-level boundaries so Identity-coupled updates are not incorrectly pulled into the same guarantee. Continue **Sol High**. Broader critical/concurrency/lookup, unresolved restoration/zero-net policy, media/device and release gates remain pending; no deployment.
