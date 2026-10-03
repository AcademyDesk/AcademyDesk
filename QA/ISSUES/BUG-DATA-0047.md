# BUG-DATA-0047 — Expense update can attach a foreign or nonexistent branch and arbitrary status

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major finance-record integrity |
| Priority | P1 |
| Category | DATA |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /expenses |
| API | PUT /api/academies/{academyId}/expenses/{expenseId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-EXPENSE-UPDATE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/ExpensesController.cs:26 |
| Class/function | Update |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Create a local expense, then update it with a foreign/missing BranchId and an unsupported Status. Compare Create and a same-academy branch control in a fresh read. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Create a local expense, then update it with a foreign/missing BranchId and an unsupported Status. Compare Create and a same-academy branch control in a fresh read.

## Expected

Update validates all supplied relationships and constrains status to the documented expense lifecycle; rejected updates preserve the original row.

## Actual / evidence

Create validates branch ownership, but Update assigns BranchId and Status directly after checking only description/amount. No branch lookup or status vocabulary is applied. Runtime NOT RUN.

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
