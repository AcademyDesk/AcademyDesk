# Batch 28 — Finance operations, compensation and work queue

Operations continuation: [Batch 38](38_OPERATIONS_FORM_SEMANTICS.md) supplies exact G01/G02 field, request and state tables for FINANCE-FORM-001/006 and OPERATIONS-FORM-001. This historical batch's "remaining" wording is superseded for those forms. BUG-DATA-0053 is the distinct Finance Policy payslip-theme overwrite; existing reset/notice findings remain separate.

Compensation continuation: [Batch 33](33_TEACHER_COMPENSATION_SEMANTICS.md) now supplies exact field, state and action-permission tables for TEACHER-FORM-002 and TeacherCompensationController. Preserve the rest of this historical batch. Current counts/status are in [the semantic checklist](SEMANTIC_CHECKLIST.md) and reports/counts.json.

Permission continuation: [Batch 36](36_LATER_ACTION_PERMISSIONS.md) supplies G03 outcomes for all Expenses and AdminWorkItems actions. The existing G01/G02 traces below remain applicable; runtime NOT RUN.

2026-09-29. Source specification; runtime NOT RUN. Completes ExpensesController (List/Create/Update) and TeacherCompensationController (Get/Save):5 actions. AdminWorkItemsController was mapped in batch06; this batch completes the remaining Work Queue form without double-counting its controller.

FINANCE-FORM-001 adds5 native expense controls plus one register filter. FINANCE-FORM-006 adds10 policy controls. TEACHER-FORM-002 adds7 compensation controls plus teacher selector. OPERATIONS-FORM-001 adds7 work-item controls plus queue filter/status controls.

## G02 — Expenses and finance policy

Expense creation requires description and positive amount, defaults currency INR/category General/date today and validates a supplied branch is local. Test precision, currency/category vocabulary, optional branch/date, date boundaries, duplicate/rapid submit, inactive/foreign branch and fresh-list totals. UI sends fixed INR and clears all fields after success, then reloads; an unknown post-save refresh must not be presented as a safe retry. Batch 37 clarification: after a successful reload it sets "Expense recorded.", so this notice is not cleared by that reload. A rejected reload escapes the handler before the notice. The load requires both expense and branch responses to succeed even though branch selection is optional.

BUG-DATA-0047: Update validates only description/amount but writes BranchId and Status directly. Unlike Create it does not confirm local branch ownership and has no status vocabulary. Test rejected values preserve the original record and verify whether currency is intentionally immutable.

Finance policy reuses FinanceGovernanceController mapping from batch06. It builds a replacement payload from FormData, where absent checkbox fields become false and blank optional values serialize as empty strings. Test exact tax/payment boundaries, optional authority/logo/signature/template fields, browser required/number behavior, false versus omitted checkbox, loaded values with unsupported templates and a fresh settings read after save. Do not infer that a URL field uploads, validates or protects a branding asset.

## G02 — Teacher compensation and work queue

Teacher compensation permits Monthly or Hourly with the model’s required primary rate, rejects negative values and serializes a summary into Teacher.CompensationJson. Test zero/decimal/max/locale numeric input, model switches, optional band rates, effective date boundaries, invalid historical JSON fallback, inactive/foreign teacher and concurrent saves. There is no effective-date history: each Save replaces the whole JSON, so past/future changes must have an approved payroll policy before implementation changes.

The teacher selector sits outside the native form and includes inactive teachers. On changing selection, responses can race with a prior selection and replace form state. Test slow/reordered selection, 403/404/parse/transport errors, disabled Save state and readback. Success is visible on a direct response but no request identity prevents stale details being saved for the newly displayed teacher.

Work Queue creates title/type/priority/assignee/due date+time/description. It converts a browser-local datetime via new Date(...).toISOString(), then lists it using browser local time, so timezone/day-boundary behavior needs explicit acceptance. Batch 37 correction: Create accesses event.currentTarget.reset() after awaiting the request, exposing the existing BUG-FUNC-0001 null-currentTarget path before the reset/success/reload sequence. If that step succeeds, load clears the success message (BUG-FUNC-0003); status move likewise loses its notice. Controlled type/priority are retained; assignee/date/time have explicit resets. Test committed-row plus reset failure separately from rejected POST and failed readback; a failure notice is not evidence that retrying is safe.

BUG-DATA-0048: AdminWorkItemsController.Create stores AssignedUserId, EntityType and EntityId without verifying a same-academy user or valid typed entity. Test unassigned valid control and local/foreign/missing/mismatched identities; CollectionState separately scopes type Collections but does not establish original work-item linkage.

## G03 — Access, failure and visual acceptance

Expenses maps finance.manage; compensation and work items have no direct catalog mapping. Verify anonymous, wrong-academy, inactive/suspended, revoked grant, owner/admin/finance/manager/teacher/family matrices and no-write/no-audit-success on denial. Existing global filter behavior is not a runtime authorization PASS.

At 320/375/390/430, 768 and 1280/1440 plus 200% zoom, test expense/work cards, wide finance-policy inputs, conditional hourly fields, dropdown placement/layering, native date/time year selection, mobile keyboard, long text/amounts, focus and status messages. No browser, real HTTP/SQL, payroll, finance policy, or work item action was run.

## Outcome

Two new OPEN P1 source findings: BUG-DATA-0047 (expense update skips branch/status validation) and BUG-DATA-0048 (work-item assignee/entity references unchecked). Existing BUG-FUNC-0003 extends. Adds2 controllers/5 actions and4 forms/29 native lexical controls plus4 standalone controls. Cumulative82/82 forms,485 lexical+126 standalone/composed=611/649 controls,66/70 complete controllers. Source counters are not runtime pass or percentage completion.

Next: imports, dashboard/intelligence, weather endpoint and final residual shared-control reconciliation. G01/G02/G03 remain open; Phase1 NOT CLOSED and Phase2 not started. No app repairs, commit or Azure deployment.
