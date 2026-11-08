# BUG-DATA-0016 — Finance and platform summaries combine currencies and present totals as INR

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED / ACADEMY + PLATFORM + DASHBOARD BOUNDED REPAIRS |
| Final verification | NOT RUN |
| Severity | Major financial reporting integrity |
| Priority | P1 |
| Category | DATA |
| Module | FINANCE |
| Role | AcademyAdmin/FinanceUser for academy summaries; current stored Platform Owner flag for global overview |
| Screen / route | /finance; /finance-summary; /dashboard; /platform |
| API | GET finance-governance/summary; GET academy/dashboard; GET /api/platform/overview |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-CURRENCY-001 |
| Evidence classification | C17/C18/C19 controller/HTTP/InMemory and controlled Chromium reproduction/repair; real SQL acceptance separate |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/FinanceGovernanceController.cs:14 |
| Class/function | FinanceGovernance.Summary; Dashboard.Summary; PlatformControl.Overview; finance/dashboard/platform cards |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In disposable fixtures create same-academy fee plans/invoices of INR 1000 and USD 100, with matching-currency payments. Read summary and finance cards; compare single-currency controls. Repeat with platform billing invoices in INR and USD and compare PlatformControl Overview with platform cards. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | C17/C18/C19 isolated InMemory snapshots preserved on reads/denials; real SQL acceptance pending |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | C17 a448103 + ddd34fd; C18 a8d20d80abc46042a4d3a7bf31b15ead121ae361; C19 e9d256221fa49fa467b9a6de38d07f9e3aa3600a; live acceptance pending |
| Retest result | C19 API target46/46, UI42/42, controlled native14/14 PASS; prior source-bound evidence retained below; real SQL/device/critical closure pending |
| Regression result | C19 API2,155/2,155 PASS; frontend2,086 PASS /3 genuine SQL-receipt FAIL, zero skips |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In disposable fixtures create same-academy fee plans/invoices of INR 1000 and USD 100, with matching-currency payments. Read summary and finance cards; compare single-currency controls. Repeat with platform billing invoices in INR and USD and compare PlatformControl Overview with platform cards.

## Expected

Money is grouped by currency or converted using an explicit supported rate/date policy; unlike currencies are never summed as one INR amount.

## Actual / evidence

Summary sums invoice/payment decimal amounts without grouping or currency metadata. FinancePage calls money(summary values) with default INR. Fee-plan and invoice paths permit distinct currencies within one academy. PlatformControl Overview also sums unlike currencies, and platform money formatter hardcodes INR. Source finding; runtime NOT RUN.

Source snapshot:

```text
13:  [HttpGet("collections")] public async Task<ActionResult> Collections(Guid academyId,CancellationToken t)=>Ok(await db.Invoices.AsNoTracking().Where(x=>x.AcademyId==academyId&&x.Status!="Paid"&&x.Status!="Cancelled"&&x.DueDate<DateOnly.FromDateTime(DateTime.UtcNow)).OrderBy(x=>x.DueDate).Select(x=>new{x.Id,x.InvoiceNumber,x.StudentId,x.TotalAmount,x.Currency,x.DueDate,x.Status,DaysOverdue=DateOnly.FromDateTime(DateTime.UtcNow).DayNumber-x.DueDate.DayNumber}).ToListAsync(t));
14:  [HttpGet("summary")] public async Task<ActionResult> Summary(Guid academyId,CancellationToken t){var invoices=await db.Invoices.Where(x=>x.AcademyId==academyId).ToListAsync(t);var payments=await db.Payments.Where(x=>x.AcademyId==academyId&&x.Status!="Voided").ToListAsync(t);return Ok(new{GrossBilled=invoices.Sum(x=>x.TotalAmount),ApprovedAdjustments=invoices.Sum(x=>x.AdjustedAmount),Collected=payments.Sum(x=>x.Amount),Outstanding=Math.Max(0,invoices.Sum(x=>x.TotalAmount-x.AdjustedAmount)-payments.Sum(x=>x.Amount)),Reconciled=payments.Where(x=>x.Status=="Reconciled").Sum(x=>x.Amount),OverdueInvoices=invoices.Count(x=>x.Status!="Paid"&&x.Status!="Cancelled"&&x.DueDate<DateOnly.FromDateTime(DateTime.UtcNow))});}
15:  [HttpPost("collections/{invoiceId:guid}/follow-up")] public async Task<ActionResult> CreateFollowUp(Guid academyId,Guid invoiceId,CollectionFollowUpRequest r,CancellationToken t){var invoice=await db.Invoices.SingleOrDefaultAsync(x=>x.Id==invoiceId&&x.AcademyId==academyId,t);if(invoice is null)return NotFound();if(invoice.Status is "Paid" or "Cancelled")return Conflict(new{message="This invoice no longer requires collections follow-up."});var item=new AdminWorkItem{AcademyId=academyId,Type="Collections",Title=$"Collect {invoice.InvoiceNumber}",Description=string.IsNullOrWhiteSpace(r.Note)?$"Overdue invoice {invoice.InvoiceNumber}.":r.Note.Trim(),Priority=string.IsNullOrWhiteSpace(r.Priority)?"High":r.Priority.Trim(),AssignedUserId=r.AssignedUserId,EntityType="Invoice",EntityId=invoice.Id,DueAtUtc=r.DueAtUtc};db.AdminWorkItems.Add(item);await db.SaveChangesAsync(t);return Ok(item);}
16: }
```

## Suspected root cause

Currency is discarded during aggregation and a presentation default supplies a misleading unit.

## Business impact and blast radius

Gross billed, collected, outstanding and reconciled finance cards for multi-currency academies and platform billing across academies.

## Related / required regression

FINANCE-CURRENCY-001: Real SQL/HTTP plus browser assertions for INR-only, USD-only and mixed currencies; verify exact decimal totals and displayed units without inventing exchange rates.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.


## C9 financial batch — 7 November 2026

Additional Payments, Invoices, Payroll and Reconciliation summary cards reproduced mixed-currency display failures and now group exact returned values by currency without inventing an exchange rate. Reconciliation also preserves displayed cents. This is a partial consumer repair: the original Finance/Platform summary APIs and cards still require their own currency-aware contract/SQL/browser acceptance.

Implementation `4fa371a35be426b9342eb79985ede7d1262c0281`; final browser runner
`aa7a42bc41e6e4b5c5da7b3f44ecf3adccea6e17`. Combined batch: **49 native Chromium
groups PASS**, 31 new TSX checks PASS, and 57 retained financial checks PASS. Full
frontend **1,961 PASS / three missing-genuine-SQL-receipt FAIL / zero skips**.
All browser API state is synthetic: 27 intercepted mutations, zero live financial
writes. Exact-lock build/types/lint PASS (zero errors / existing 63 warnings).

Remain **OPEN**. These shared batch counts are not added per issue and do not
replace original module/critical, real Identity/HTTP/SQL, other-client, device or
release closure requirements. See [C9 source-bound report and Desktop tasks](../HANDOFF/C9_FINANCE_BATCH_2026-11-07.md).

## C17 academy summary repair — 7 November 2026

Original API3/17 and controlled screens0/15 pass. The repaired academy summary
returns explicit per-currency groups and null mixed-currency flat monetary
fields; single-currency numbers and empty zero values retain their shape. Both
known academy screens consume the groups, including rates and reconciliation
amounts, without inventing FX or an INR unit for missing metadata. The collection
list/follow-up monetary predicates count only matching-currency Completed/
Reconciled receipts. No raw ledger row or write/authority policy is changed.

Final API/retained target50/50 and UI/retained target26/26 pass. Full API2,107
pass on C17's source; full frontend2,057 pass with three unchanged genuine SQL
receipt failures, zero skips. Six actual Identity/HTTP cases use explicit
InMemory persistence, not SQL. Original PlatformControl Overview/Dashboard and
their remaining consumers are not repaired by this packet. Keep OPEN for those
paths and all real SQL/critical/browser/device closure requirements.
[Exact contract, evidence and Desktop handoff](../HANDOFF/C17_FINANCE_CURRENCIES_2026-11-07.md).

Final C17 controlled native16/16 PASS, zero runtime/interceptor errors or writes,
normal sandbox and owned cleanup,570 export hashes unchanged. This qualifies
the bounded academy UI rendering scope only; original platform/dashboard and
real SQL/device/critical/release acceptance remain OPEN.

## C18 platform overview repair — 8 November 2026

Implementation `a8d20d80abc46042a4d3a7bf31b15ead121ae361` returns explicit
per-currency platform billing groups; mixed flat totals become null, single
currency and empty numeric contracts remain. The owner tiles and their detail
dialogs retain units and cents. Draft/Void remains excluded, Paid is collected,
and unverified Payment submitted remains outstanding. Owner authority is still
the current stored flag, not a role-name claim. No write/status policy changed.

Original22 API cases11/11 PASS/FAIL and original15 TSX cases4/11 PASS/FAIL
were retained. Final platform/academy target48/48, provisioning/dashboard31/31
and controlled native10/10 pass. Full API2,132/2,132 pass at this exact Cloud
source. Full frontend2,072 PASS /3 genuine missing SQL-receipt FAIL, zero skips.
Native/API fixtures are explicitly synthetic InMemory or intercepted replies;
real SQL, device, critical and final release acceptance remains OPEN.

The two original named summary paths now have bounded repairs. The academy
Dashboard and its fees/payroll composition remain a separate unqualified path.
[Contract, evidence, compatibility and Desktop SQL return](../HANDOFF/C18_PLATFORM_CURRENCIES_2026-11-08.md).

## C19 academy dashboard readback repair — 8 November 2026

Original dashboard23 HTTP/Identity/InMemory cases5 PASS /18 FAIL;14 controlled
TSX cases all failed. The dashboard now shares the existing C17 academy finance
read model, returns explicit groups/null mixed scalars, and uses per-invoice
nonnegative balances rather than netting an overpayment against other students.
Cancelled bills contribute gross history but no receivable; only accepted,
matching-currency receipts reduce that invoice's debt. The tile shows student
fees with explicit units/cents; the modal keeps payroll records separate and
uses invoice API balance metadata with adjustments. No write/accounting status,
permission/tenant or audit policy changes.

Final targeted API46/46 and UI42/42 PASS; native14/14 PASS at320/light and
1440/dark, zero runtime/interceptor errors or API mutations, normal sandbox,
owned cleanup and570 export hashes unchanged. Full API2,155/2,155 PASS; frontend
2,086 PASS /3 missing genuine SQL-receipt FAIL, zero skips/cancellations. These
are controlled fixtures and do not establish real SQL/device/critical closure.

Remain OPEN. Desktop must integrate C17-C19 without replacing its newer profile
audit fixes and execute combined SQL/HTTP/browser/critical/device acceptance.
Invoice List's legacy mismatched-payment-currency projection and Reports-page
independent money/payroll calculations remain separate unqualified consumers;
no claim that all financial screens or release gates are qualified.
[Exact source, contract, evidence and Desktop acceptance tasks](../HANDOFF/C19_DASHBOARD_BALANCES_2026-11-08.md).
