# Phase 2B — FinanceUser Payments/Invoices student lookup

Historical checkpoint. Subsequent [Governance access](PHASE_2B_GOVERNANCE_ACCESS_REPAIR.md) and [invoice document-settings](PHASE_2B_INVOICE_SETTINGS_REPAIR.md) repairs supersede the dependency limitations/next-task pointers below. Original student-lookup evidence remains retained.

2026-10-01. **Bounded local repair PASS: API 187/187, controlled frontend 13/13, two fresh Identity/HTTP/SQL runs ×56 primary cases. BUG-FUNC-0006 and Phase 2B remain OPEN.** Agreed Sol High. Accepted Astra finding and prior dependency reproduction reused, not another static audit. No commit/push/Azure deployment or normal development database changes.

## Bounded change and access contract

[InvoicesController.StudentOptions](../../apps/api/Controllers/InvoicesController.cs) adds `GET /api/academies/{academyId}/invoices/student-options`, inheriting existing `finance.manage` and Finance subscription gates. Its AsNoTracking query is constrained to the route academy, ordered last name/first name/ID, and explicitly projects **id, firstName, lastName, email, phone**. Email/phone preserve the existing invoice Bill to/Preview/Print billing contact; no full profile DTO. Branch/active flag/birth/admission dates/fees/address/emergency/medical/admin notes/account metadata are excluded; nullable contacts remain optional.

Inactive own students remain available to resolve historical invoice names and preserve existing list/invoice-create behavior, not to approve a new active-student billing policy. Only [Payments.load](../../apps/web/src/app/payments/page.tsx) and [Invoices.load](../../apps/web/src/app/invoices/page.tsx) replace `/students` with this route. No layout/popup/date redesign, global filter/permission/module/schema change, finance arithmetic or write-path change. Generic Student GET/POST remain forbidden to FinanceUser.

## Reproduction and verification

[Actual TSX controlled-hook tests](../tools/finance-lookups.test.cjs) before application edits: **four tests, two allowed-load FAIL/two denied-load controls PASS**, [baseline log](../EVIDENCE/logs/phase-2b-lookup-before-client.log). Generic Students GET returns 403 in the fixture; both pages fail the narrow-route expectations. Prior actual Identity/HTTP/SQL dependency evidence is retained historical, not counted as newly executed.

Final six lookup tests cover allowed loads without student-management calls, denied lookup handling, Payments name resolution in selector/directory, and the actual Invoices Preview/Print handler's name/email display. Together with seven existing payment canonical-balance/collected-payment tests, **13/13 PASS**, [log](../EVIDENCE/logs/phase-2b-lookup-client.log). These execute transpiled application TSX with controlled hooks/API results, not live browser/React lifecycle/mobile/end-to-end save-click proof.

[Three new API units](../../tests/AcademyDesk.Api.Tests/InvoiceStudentOptionTests.cs) cover projection/order/nulls/inactive rows/no tracking, empty tenant lookup and five-field contract/unchanged permission mappings. InMemory is used for those narrow unit assertions only, not authorization/transaction proof. Full API [187/187 PASS](../EVIDENCE/logs/phase-2b-lookup-api-tests.log); harness [build](../EVIDENCE/logs/phase-2b-lookup-build.log) zero warnings/errors; frontend [typecheck](../EVIDENCE/logs/phase-2b-lookup-typecheck.log) exits 0/no diagnostics. Targeted [lint](../EVIDENCE/logs/phase-2b-lookup-lint.log) exits 0, **zero errors/four warnings**: two existing effect-dependency/two img warnings; not clean/full frontend certification.

[HTTP/SQL module](../tools/SqlHarness/FinanceLookupRegression.cs) uses actual Identity tokens, unchanged filter/controllers/rate limiter, both EF stores and fresh guarded SQL. Each run has **56 primary cases**:

- Ten actor lookups: Admin/Owner/FinanceUser/finance-only custom role allowed; Teacher/Student/FrontDesk/Operations/student-management-only custom role/no-finance custom role forbidden.
- Twenty page prerequisite reads: four allowed actors × invoices/payments/fee-plans/finance settings/academy list. Finance and FinanceControls both enabled.
- Six FinanceUser/custom-role controls retain student-management/governance-work-items 403 and foreign-route 403; anonymous 401/Admin foreign-route 403 add two.
- Six grants: absent/wrong-academy/expired/revoked denied; valid/permanent finance grant allowed on original token.
- Four Finance module disabled/enabled controls for Admin/FinanceUser; inactive academy/user denied and reactivated original-token lookup allowed (three).
- Four save checks: student mutation 403 and foreign-student invoice 400 with no captured mutation; own invoice 201 and payment 201 with fresh SQL linkage/Completed payment/Paid invoice/one audit per write. One post-save lookup refresh.

Successful lookups assert exact five-field JSON keys/count/order/values against fresh own SQL, including populated contacts/inactive/null-contact fixtures; sensitive marker and foreign-name leakage fail. Reads/rejections compare all captured Students/Teachers/Branches/AuditLogs, selected Identity user/role/link metadata and invoice/payment/adjustment/payroll/notification rows. Credentials, grants and uncaptured tables are excluded; no all-database claim. Setup intentionally modifies synthetic module/grant/user-active fixtures between checks. Only two expected finance writes mutate captured finance/audit data.

| Fresh run | Loopback SQL / UTC start / elapsed | Evidence |
| --- | --- | --- |
| `607fc7d2c1104674a1834f8c3022b71e` | 51825 /08:01:21.5695276 /62.7 s | [run 1](../EVIDENCE/logs/phase-2b-lookup-run1.log): 56 PASS, cleanup exit 0 |
| `731b2c4f37d14b81a90c5ba968c4ba44` | 49254 /08:03:02.3356697 /59.7 s | [run 2](../EVIDENCE/logs/phase-2b-lookup-run2.log): 56 PASS, cleanup exit 0 |

Health/auth/two-tenant preflight checks are secondary, not double-counted. Runtime **308 routes/297 controller method-routes/10 Identity method-routes**, SHA256 `1EA5BFF7B2A7811D7D7B0B2A2D378D3470755AF091CC76DB12F4CA3F41E06D4D`: one new GET explains the increment, accepted static inventories retained historical/not regenerated. Migrations remain 80 application/7 Identity. Resolved contexts match the exact owned SQL target/principal. No listening frontend/backend created.

## Source, cleanup and limits

[109-record snapshot](PHASE_2B_FINANCE_LOOKUP_SOURCE_SNAPSHOT.json) extends previous 103: three prior captures changed (Payments/QA entry/wrapper), **100 unchanged**. Six additional captures: two existing application files newly pinned here (Invoices controller/page), three new test/module files and validator. Three application files change this slice; unrelated dirty work preserved. This is not whole-repository capture. HEAD `20bb6047f9edf733ac8e2a226621cc582ec54b3c` unchanged.

Final API SHA256 `bf265cc50b1df2a585c08a3ea091259dd8043a2fa94cf917acb3bf0c0d21ff98`, QA harness `25b3313b41974d176f848dc4051bca59ce1216da2ea07a0e6b364420ba252bbe`. Application/harness unchanged between SQL runs; client tests/documentation/capture validation extended separately. [Scoped validator](../tools/validate-finance-lookups.cjs) checks hashes/HEAD/logs/links/independent cleanup, [validation log](../EVIDENCE/logs/phase-2b-lookup-validation.log).

Guarded cleanup rejects a false marker and removes only each owned database/login/root/container. Both current resources removed; synthetic fixtures can be recreated by rerunning. Older retained resources, normal dev SQL and Azure untouched. No commit/deployment or machine policy change.

**Governance generic AdminWorkItems remains 403/OPEN**, deliberately asserted, not broadly authorized. Invoices still requires FinanceControls for its existing settings prerequisite; Finance-only lookup success does not mean Finance-only full-page acceptance. Global platform-owner bypass unchanged/not newly certified. Live browser/devices, all-role/mixed-role combinations, full denied-UI guidance, exhaustive billing-field policy, inactive-student business policy, scale/pagination/deletion/permission races, cancellation/concurrency/full critical/release acceptance pending/NOT RUN. No issue/phase closure.

## Next

Accepted BUG-FUNC-0006 **Finance Governance collections work-items dependency**, using finance-scoped read/update contracts instead of generic work-item access. Preserve tenant/module/role/assignment boundaries; ask only where business authority is unresolved. **Sol High** for implementation/targeted regression, not repeat Astra audit. Browser acceptance/Finance-only settings coupling and restoration/zero-net/media/device/critical/release gates remain follow-ups. No deployment planned.
