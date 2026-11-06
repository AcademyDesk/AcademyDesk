# BUG-FUNC-0005 — Cancelling the adjustment approval prompt still approves the request

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONFIRMED in actual handlers and controlled Chromium; live acceptance pending |
| Final verification | PARTIAL — controlled frontend PASS; original live closure gates OPEN |
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
| Fix commit | 1ec22e733696fa84e4dbf947da463fbe4a2d123a (bounded frontend source) |
| Retest result | Controlled handler/browser/contract PASS; see 6 November return below |
| Regression result | Current cloud frontend1918PASS/3genuine evidence-prerequisite FAIL; no skips |
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

## Cloud C6 bounded return — 6 November 2026

Source1ec22e7 distinguishes the native prompt's null dismissal from an explicit
empty optional note. Cancel/Escape's shared null return exits before saving state,
mutation or list reload; rejection still requires a nonblank reason. Accepted
empty/named approval and nonblank rejection retain the existing exact request.

Actual TSX regression:16/16PASS, including the seven added decision controls.
Pre-fix handler14PASS/2FAIL and preserved pre-fix Chromium receipt demonstrate
approval cancellation dispatched PATCH; rejecting cancellation also incorrectly
showed validation feedback. The real browser uses native Page.prompt, not a mock
window.prompt. Final native-browser cases cover approve/reject dismissal, explicit
empty/named approval, empty/whitespace rejection, deliberate rejection and denied
response. No live financial write or SQL evidence is claimed; all API responses
were intercepted. Hardware Escape behavior and actual database before/after/
critical acceptance remain open under the original closure requirements.

Original static/source history above is retained. Status remains OPEN: no synthetic
transport or DTO check is substituted for required current live HTTP/SQL/role/
critical/device evidence. Source, baseline/final receipts and Desktop next checks:
[Cloud C6 handoff](../HANDOFF/C6_MANUAL_CONTROLS_2026-11-06.md).

## Local C6 integration — 6 November 2026

The current integrated pinned build passes 36 TSX contracts, 15 actual Manual
browser groups and 24 PENTA groups. Browser APIs are intercepted synthetic
responses. Full frontend: 1,920 PASS / one Windows symlink EPERM / zero skips.
Status remains OPEN; live HTTP/SQL/role/device closure is still required. See
[local C6 report](../REPORTS/LOCAL_C6_MANUAL_CONTROLS_2026-11-06.md).
