# BUG-DATA-0047 — Expense update can attach a foreign or nonexistent branch and arbitrary status

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-CONFIRMED; bounded repair verified |
| Final verification | RETEST — native/linked browser passed; physical-device/critical/release gates OPEN |
| Severity | Major finance-record integrity |
| Priority | P1 |
| Category | DATA |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /expenses |
| API | PUT /api/academies/{academyId}/expenses/{expenseId} |
| Environment | Local disposable SQL/Identity/API and exported browser; production state not inferred |
| Device/viewport | Headless 320/768/1440 light/dark; real SQL-linked 320/1440; physical devices NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-EXPENSE-UPDATE-001 |
| Evidence classification | Historical static trace + baseline controller reproduction + repaired native SQL/API and linked browser |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Starting controller 9/32 cases PASS; final 33/33 expense cases PASS |
| Source | apps/api/Controllers/ExpensesController.cs:26 |
| Class/function | Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | QA/REPORTS/PHASE_2B_EXPENSE_INTEGRITY_REPAIR.md and local QA/EVIDENCE/expense-integrity-20261011/ |
| Screenshot | Local expenses-live-1791694077039 and expenses-feedback-browser-1791693970183; phone exact-money reviewed |
| Console logs | Linked/controlled browser evidence captured; expected denied branch responses distinguished from application faults |
| API request | Create a local expense, then update it with a foreign/missing BranchId and an unsupported Status. Compare Create and a same-academy branch control in a fresh read. |
| API response | Native matrix exact success/400/401/403/404/500 statuses; rejected no-writes and audit failure rollback verified |
| Database before/after | Final run9b58a8ddbad64e4bb5c690262c8c5785: fresh row/audit/foreign sentinel/unrelated-ledger assertions PASS |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Scoped expense successor on codex/penta-search; see feature history and repair report |
| Retest result | 66/66 SQL/API; four linked browser journeys/five saves/four rejected writes PASS, no429 |
| Regression result | 69 targeted API tests,45 handler/money,54 controlled responsive cases; builds/types/lint/export83 PASS |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## 2026-10-11 repair successor — OPEN / RETEST

See [repair report](../REPORTS/PHASE_2B_EXPENSE_INTEGRITY_REPAIR.md) for exact commands, rejected intermediates, verification and cleanup. Branch ownership, existing Recorded status, field limits and decimal(18,2) guards now precede mutation. SQL replacement reads are locked through the domain/audit boundary; five valid/invalid and five valid/valid concurrent pairs pass without mixed rows. Optional historical fields/currency are preserved. Audit failure rolls back the write.

The existing record form now has truthful durable feedback, pending/readiness/uncertain-response safeguards and exact amounts. Branch lookup403 does not block an authorized no-branch expense. This is not a new update/delete editor, accounting lifecycle or idempotency implementation. Physical Android/iOS, full critical regressions and release acceptance remain OPEN. Original static excerpt below is historical, not current source. No production/Azure claim.

## Exact reproduction (historical baseline)

Create a local expense, then update it with a foreign/missing BranchId and an unsupported Status. Compare Create and a same-academy branch control in a fresh read.

## Expected

Update validates all supplied relationships and constrains status to the documented expense lifecycle; rejected updates preserve the original row.

## Actual / evidence

Historical baseline: Create validates branch ownership, but Update assigns BranchId and Status directly after checking only description/amount. No branch lookup or status vocabulary is applied. Superseded by the runtime-confirmed repair above; retained for traceability.

Source snapshot:

```text
25:     [HttpPut("{expenseId:guid}")]
26:     public async Task<ActionResult> Update(Guid academyId, Guid expenseId, UpdateExpenseRequest request, CancellationToken token)
27:     { var x=await dbContext.Expenses.SingleOrDefaultAsync(v=>v.Id==expenseId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.Description)||request.Amount<=0)return BadRequest(); x.Description=request.Description.Trim();x.Amount=request.Amount;x.Category=request.Category?.Trim()??"General";x.BranchId=request.BranchId;x.ExpenseDate=request.ExpenseDate;x.Status=request.Status?.Trim()??x.Status;await dbContext.SaveChangesAsync(token);return Ok(); }
28: }
```

## Suspected root cause

The replacement update contract omits validations present on creation and lifecycle policy.

## Business impact and blast radius

Expense register, branch reporting and finance controls can show invalid scope or fabricated status.

## Related / required regression

FINANCE-EXPENSE-UPDATE-001: HTTP/SQL local/foreign/missing/null branch and status matrix, positive create/update/readback, rejected no-write, concurrent edits and currency preservation.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
