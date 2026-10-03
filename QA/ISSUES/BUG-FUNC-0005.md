# BUG-FUNC-0005 — Cancelling the adjustment approval prompt still approves the request

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major unintended financial action |
| Priority | P1 |
| Category | FUNC |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /finance-governance |
| API | PATCH finance-adjustments/{adjustmentId}/approval |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-APPROVAL-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/finance-governance/page.tsx:44 |
| Class/function | decide |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Using synthetic pending adjustment, click Approve then cancel the browser prompt. Capture outgoing request and fresh adjustment/invoice state; repeat dismissal via Escape and compare accepting an empty optional note. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Using synthetic pending adjustment, click Approve then cancel the browser prompt. Capture outgoing request and fresh adjustment/invoice state; repeat dismissal via Escape and compare accepting an empty optional note.

## Expected

Cancel/Escape sends no mutation and leaves adjustment PendingApproval; accepting an empty optional approval note remains a distinct affirmative action.

## Actual / evidence

window.prompt returning null is converted to empty string. Only rejection with blank note returns early, so approval cancellation continues with approve:true.

Source snapshot:

```text
43:     if (!academy) return;
44:     const notes = window.prompt(approve ? "Approval note (optional)" : "Rejection reason") ?? "";
45:     if (!approve && !notes.trim()) return setNotice("A rejection reason is required.");
46:     setSaving(true);
```

## Suspected root cause

Cancellation and intentionally empty note collapse into the same value.

## Business impact and blast radius

Accidental financial approval, invoice adjustment and ledger state; browser reproduction NOT RUN.

## Related / required regression

FINANCE-APPROVAL-001: Browser dialog cancel/Escape/accept-empty/accept-note for approve; cancel/empty/nonempty for reject; assert network absence and DB unchanged on cancel, one mutation on deliberate confirmation.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
