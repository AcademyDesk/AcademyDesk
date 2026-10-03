# BUG-DATA-0024 — Payment submission removes unverified platform invoices from outstanding totals

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED / bounded local repair |
| Final verification | 51/51 real Identity/HTTP-SQL and1019/1019 existing backend rerun PASS; browser/critical/currency/race gates OPEN |
| Severity | Major financial reporting integrity |
| Priority | P1 |
| Category | DATA |
| Module | PLATFORM |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /platform; /platform-services |
| API | GET /api/platform/overview; POST platform-services/billing-invoices/{invoiceId}/payment-submission |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PLATFORM-BALANCE-001 |
| Evidence classification | Accepted static trace plus real owner/create/issue/admin submission and fresh SQL baseline/final evidence |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | One real owned-SQL lifecycle reproduced the1000 unpaid deficit; one final51-case owned-SQL run |
| Source | apps/api/Controllers/PlatformControlController.cs:31 |
| Class/function | Overview / AcademyPlatformServices.SubmitPayment |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In disposable fixtures issue one INR1000 platform invoice. Read overview, submit an optional reference as academy admin without owner approval, then read overview and persisted invoice again. Compare owner Paid and Void transitions. |
| API response | Baseline submitted invoice left billed1000/collected0/outstanding0; final submitted stays outstanding1000 until owner Paid |
| Database before/after | Exact invoice/audit writes and preserved full snapshots;40 no-write reads/rejections in final51-case run |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted one-predicate repair; HEAD unchanged, no push/deployment |
| Retest result | PARTIAL PASS — original lifecycle/optional/repeat/rejection/decimal/zero/two-tenant HTTP-SQL; browser/critical OPEN |
| Regression result | 51 HTTP-SQL and1019 existing backend rerun PASS; owned cleanup; broader gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In disposable fixtures issue one INR1000 platform invoice. Read overview, submit an optional reference as academy admin without owner approval, then read overview and persisted invoice again. Compare owner Paid and Void transitions.

## Expected

Unverified submitted payments remain outstanding or appear in a separate explicit pending balance that reconciles billed and collected totals; submission alone must not imply collection.

## Actual / evidence

SubmitPayment changes status to Payment submitted. Overview includes that amount in TotalBilled but excludes it from both CollectedBilling and OutstandingBilling, with no pending monetary total. Source finding; runtime NOT RUN.

The above describes the historical accepted Phase1 source snapshot. Local successor2026-10-02 reproduced it with real owner issuance and administrator submission, then repaired only the outstanding aggregation. See below; production runtime state is not inferred.

Source snapshot:

```text
30:             CollectedBilling = await invoices.Where(x => x.Status == "Paid").SumAsync(x => (decimal?)x.Amount, token) ?? 0,
31:             OutstandingBilling = await invoices.Where(x => x.Status == "Issued" || x.Status == "Overdue").SumAsync(x => (decimal?)x.Amount, token) ?? 0,
32:             OverdueInvoices = await invoices.CountAsync(x => x.Status == "Overdue", token),
33:             Plans = await academies.GroupBy(x => x.SubscriptionPlan).Select(x => new { Plan = x.Key, Count = x.Count() }).ToListAsync(token),
```

## Suspected root cause

Outstanding aggregation recognizes only Issued and Overdue, omitting the normal payment-review state.

## Business impact and blast radius

Platform financial dashboard understates unpaid balances while academy payment claims await review; no funds actually move in this flow.

## Related / required regression

PLATFORM-BALANCE-001: Real HTTP/SQL lifecycle Draft/Issued/Overdue/Payment submitted/Paid/Void, independent expected same-currency totals before and after each transition, rejected submissions and repeat requests; browser labels must match reconciliation policy.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local successor — 2026-10-02

[Repair/evidence](../REPORTS/PHASE_2B_PLATFORM_BILLING_REPAIR.md): Payment submitted now contributes to OutstandingBilling; Paid alone contributes to CollectedBilling. Draft/Void exclusions and all writers/auth/DTO/audit/current transition semantics are unchanged. Optional null/omitted/empty/trimmed references and repeated claims are tested; current Draft submission remains permitted compatibility, not a newly approved workflow.

51/51 real Identity/HTTP-SQL cases PASS: one create/five status updates/five submissions produce11 exact invoice writes and distinct actor-linked audits;17 permitted reads plus23 rejected requests produce40 identical captured snapshots. Independent INR lifecycle and two-tenant decimal/zero aggregates reconcile. Separately1019/1019 existing backend tests rerun PASS. Baseline/final builds0 warnings/errors;594 accepted entries pinned. Both disposable run databases/logins/containers/roots/ports removed. No failed build/runtime attempt in this slice.

Issue/Phase2B/release OPEN for browser labels/tiles/feedback, linked critical suite, currency/legacy/unsupported-state policy, concurrent multi-query snapshots/transitions and fault/rollback. No actual-money-transfer guarantee, real dev/customer data, normal services/assemblies, Azure/provider, commit/push/deploy changes. Next accepted platform gap BUG-DATA-0022, same Sol High.
