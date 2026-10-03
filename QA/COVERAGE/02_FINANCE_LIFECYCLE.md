# Coverage batch 02 — finance lifecycle

Individual-verdict continuation: [Batch 41](41_FINANCE_COHORT_VERDICTS.md) reconciles the four forms and ten Invoices/Payments/FinanceAdjustments action rows in this batch. It records BUG-DATA-0055 for a direct invoice-status bypass. All 14 rows remain NOT ACCEPTED and runtime NOT RUN; the historical summary below is not a PASS.

Review date: 2026-09-28. **STATIC REVIEW / TEST DESIGN; runtime NOT RUN.** Baseline: [acceptance review](../REPORTS/PHASE_1_ACCEPTANCE.md). Scope: four native forms (13 controls), three standalone filters, and the adjustment decision interaction. Three complete controller permission mappings: InvoicesController, PaymentsController, FinanceAdjustmentsController. Finance Governance is only partially reviewed: decision interaction and loading dependencies, not its settings/collections forms.

## Sources and stable tests

All paths below are relative to the repository root. UI sources are `apps/web/src/app/{route}/page.tsx`; API sources are `apps/api/Controllers/{controller}Controller.cs`. Storage sources are `apps/api/Domain/Entities/{Invoice,Payment,FinanceAdjustment}.cs`, `apps/api/Data/AcademyDeskDbContext.cs` and its migration snapshot. Effective access sources: Program, AcademyAccessFilter, PermissionCatalog, SubscriptionPlanCatalog.

| Route / form | Controller and request | Stable Test ID |
| --- | --- | --- |
| invoices / form-1 | Invoices.Create / CreateInvoiceRequest | FEES-FORM-003 |
| payments / form-1 | Payments.Create / RecordPaymentRequest | FINANCE-FORM-008 |
| finance-reconciliation / form-1 | Payments.Reconcile / reconciliation reference | FINANCE-FORM-007 |
| finance-adjustments / form-1 | FinanceAdjustments.Create / adjustment request | FINANCE-FORM-002 |
| finance-governance / decide (not a native form) | FinanceAdjustments.Decide / approve, notes | FINANCE-APPROVAL-001 |
| Lifecycle invariants | Payment status and reconciliation transitions | FINANCE-RULE-003 |
| Effective page prerequisites | Finance-only role dependencies | SECURITY-ROLE-003 |

Test IDs describe scenarios, not passing tests. Reuse the inventory's existing endpoint test IDs for raw API boundary variants.

## Field mapping and boundary obligations

Every row requires valid, omitted/null/blank/whitespace, wrong JSON type and malformed input cases where applicable. GUIDs require malformed, missing, nonexistent and another-tenant controls. API requests must assert response plus fresh-context persistence/no-write behavior. Decimal storage is (18,2): test negative, zero, 0.01, fractional-cent rounding, largest representable 9999999999999999.99 and overflow; browser Number precision is a separate boundary. String limits require N-1/N/N+1 after trimming, Unicode and padding. Do not replace SQL constraints with InMemory assertions.

| Form / control | UI → request → guard/storage | Specific obligations |
| --- | --- | --- |
| Invoice: studentId | Required controlled select → Guid StudentId → same-academy student required (400); no active-student guard found | Auto-selected first student; empty directory; stale/foreign/inactive student; no invoice on invalid ID |
| Invoice: feePlanId | Optional select → nullable Guid → provided plan must be active and same academy (400) | Blank becomes null; switching plan fills amount; switching to custom leaves prior amount, verify intended behavior |
| Invoice: amount | Optional number, min 1, step .01 → nullable decimal → supplied amount, else plan amount, else 0; must be >0 | Explicit zero does not fall back; raw API accepts positive sub-unit amounts unlike HTML min; blank with/without plan; no hidden fallback assumptions |
| Invoice: dueDate | Optional native date → nullable DateOnly → default UTC today +7 | Blank becomes null; ISO, malformed, leap/impossible dates; past/future dates have no explicit server bound; timezone boundary for default |
| Payment: invoiceId | Required select → Guid → same-academy invoice or 404 | UI lists gross balance >0, not cancellation eligibility; stale/cancelled/foreign invoice; no mutation on invalid |
| Payment: amount | Required number min .01, max selectedBalance or undefined, step .01 → decimal >0; controller compares Completed-only payments against gross invoice total | Test after reconciliation and approved adjustment (FINANCE-RULE-001/002); exact remaining amount, excess .01, double click/concurrent requests; verify no overcollection |
| Payment: method | Select UPI/Cash/BankTransfer/Card/Cheque/Offline → nullable string; blank becomes Offline, otherwise Trim; storage max 30 | API accepts arbitrary method text currently; length boundaries; do not invent server enum validation |
| Payment: reference | Optional text → nullable string Trim; max 150 | Missing/null vs empty retained distinctly; 149/150/151; no uniqueness/idempotency constraint found |
| Reconciliation: reference | Conditional required text → trimmed reference; nonblank server guard; sets Reconciled and UTC timestamp | Whitespace, repeated reference, repeated reconciliation, status transition matrix; reconciliation reference has no explicit length cap (nvarchar(max)), distinct from original payment reference |
| Adjustment: invoiceId | StandardSelectField named invoiceId → Guid → same-academy invoice or 404 | Empty disables submit; list includes all invoices; verify hidden-field validation and foreign/stale/cancelled/fully paid invoice |
| Adjustment: type | StandardSelectField → string; Discount/Scholarship/Concession/Refund/CreditNote accepted case-insensitively; max 30; original casing stored | Missing/unknown/alternate casing; approved Refund currently increases invoice adjustment like other types, not outbound refund; policy must define meaning |
| Adjustment: amount | Required number min .01 step .01 → decimal >0 | No remaining-balance cap found; test over-credit, full credit, repeated pending requests, rounding; intended over-credit policy needs approval |
| Adjustment: reason | Required textarea → nonblank string Trim; required max 2000 | Whitespace and 1999/2000/2001; failure should preserve entered values and not produce unexplained 500 |
| Reconciliation filter: reconciliationStatus | StandardSelectField → local Unreconciled/Reconciled/All | No request payload; Unreconciled excludes Reconciled and Voided; All includes all; combine with date and empty results |
| Reconciliation filter: reconciliationRecordedOn | StandardDateField → local date string | Compares paidAt in browser local date, not reconciliation date; midnight/timezone, clear, no matches; counts currently not date-filtered |
| Adjustment filter: adjustmentFilter | StandardSelectField → local All/PendingApproval/Approved/Rejected | Combine reload and newly created/decided adjustment; empty state and selected filter retained |

Invoice storage also constrains number to 40 with unique (academy, invoiceNumber), currency 3, status 30, adjusted amount (18,2). Number uses UTC seconds plus a small random suffix: collision must fail safely, not silently duplicate. Payment currency 3/status 30 and adjustment currency 3/status 30/approval notes 2000 have SQL limits. Query indexes for payment and adjustment are not idempotency keys.

## Conditional UI and submit lifecycle

| Interaction | Current source behavior | Required browser evidence (not executed) |
| --- | --- | --- |
| Invoice load/create | Students, plans, invoices and governance settings must all succeed. Create clears fields, reloads, then sets “Invoice issued.” No submitting guard/catch in create | Slow/failing request, duplicate click, durable success, only reset after save, no success on rejection |
| Invoice preview/print | Selected snapshot opens inline preview; close clears it; print calls window.print; branding depends on settings | Correct invoice/academy, close/reopen after changes, accessible focus, printable layout; distinguish gross total from adjusted outstanding instead of assuming labels mean same amount |
| Payment create | Loads students/invoices/payments together; computes Completed-only gross balance. Success clears invoice/amount/reference and reloads, but has no success notice | Success message requirement remains unsatisfied in source; errors retain values; repeated clicks/network failure must not duplicate payment |
| Reconcile | Action only shown for non-Reconciled/non-Voided; inline form Cancel clears selection/reference; saving guard. Success notice then load clears notice | Cancel sends no PATCH; required whitespace; success remains visible after reload; action removed after success; failed reload distinguishable from failed mutation |
| Adjustment request | Captures form before await; submitting guard; success resets form/state, sets notice then load clears notice | Do not misclassify as currentTarget-null bug; verify durable notice, persisted request, duplicate prevention and retained input on failure |
| Adjustment decision | Pending-only actions; shared saving state; window.prompt returns null on cancel but code maps it to empty string; only blank rejection returns early | Approve Cancel/Escape must send no mutation; accepting empty optional approval note is different. Reject cancel/blank/nonblank; exactly one request on intentional confirmation; linked BUG-FUNC-0005 |

Notice clearing on reload is tracked by existing BUG-FUNC-0003; successful-entry feedback is also covered by FORM-SUCCESS-001. A database save followed by failed list reload must not invite duplicate resubmission.

## API-only actions and ledger state expectations

| Action | Source behavior | Contract / invariant to test |
| --- | --- | --- |
| Invoice list/create | Scoped academy; paid sums non-Voided; balance clamps total-adjusted-paid to zero. Create returns 201 summary; defaults Issued, UTC issued date, INR unless plan currency | Round-trip values, Location behavior, sorting, currency consistency, no cross-tenant rows; no assumption a GET-by-ID route exists |
| Invoice status PATCH | Allows Issued/PartiallyPaid/Paid/Overdue/Cancelled case-insensitively, stores supplied trimmed casing; empty 200 | State cannot contradict money/collections policy; lowercase variants must not evade case-sensitive downstream checks; response not always JSON |
| Payment list/create | Optional invoice filter scoped to academy; foreign/unknown filter yields empty. Create marks invoice Paid/PartiallyPaid using gross total and Completed-only payments; 201 summary | BUG-DATA-0001/0002; independent decimal calculation and tenant isolation; empty collection is distinct from 404 |
| Payment status PATCH | Exact Completed/Reconciled/Voided; only payment status changes; empty 200 | Full-payment void must reopen applicable invoice state. Reconciled must not be reachable without required evidence. Rejected transition no writes |
| Payment reconcile PATCH | Nonblank reference; overwrites status with Reconciled and timestamp even if previously Voided | Invalid transitions, repeat reconcile, reference/timestamp integrity, concurrent status change; BUG-DATA-0010 / FINANCE-RULE-003 |
| Adjustment create/list | Creates PendingApproval, returns 200 summary; optional invoice filter and descending creation order | Amount does not affect invoice before approval; response schema and ordering; explicit optional filters |
| Adjustment decision PATCH | Only PendingApproval else 409; sets decision time even when rejected. Approved adds amount to invoice adjusted total; sums Completed/Reconciled, computes status; one SaveChanges | Reject must not change invoice money; repeated decision 409; simultaneous decisions require concurrency test (not confirmed race). Missing approve Boolean binding, notes limits/blank→null; same-actor create+approve currently allowed by shared permission, maker-checker policy unresolved |

Concrete new P0 reproduction specification: isolated invoice 1000 → record Completed payment 1000 → invoice Paid → PATCH payment Voided → read invoice balance/status and overdue collections. Source predicts balance 1000 with stale Paid status. Also test partial payment, adjustment, reconciliation before void, repeated transitions and concurrent writes. No actual financial mutation was executed during review.

## Effective access (G03)

All three controllers depend on the real authentication/active-user middleware and AcademyAccessFilter. Anonymous is denied; IsPlatformOwner flag bypasses tenant/module/permission restrictions after active-user checks. A role string alone is not this flag. Others require matching academy, active academy and enabled module. Owner/AcademyAdmin pass the permission gate; other roles require catalog/custom permissions. Foreign tenant/row IDs must never alter data.

| Controller / all actions | Module | Required permission after common gates | Default role expectation |
| --- | --- | --- | --- |
| Invoices: list/create/status | Finance | finance.manage | FinanceUser allowed; other non-admin roles denied unless explicitly granted |
| Payments: list/create/status/reconcile | Finance | finance.manage | Same; no stronger status/reconcile action permission found |
| FinanceAdjustments: list/create/decide | FinanceControls | finance.manage | Same; no separate approver permission found |

Test all 12 catalog roles, custom finance-only grants, revoked grants, module disabled, inactive academy/user, mismatched tenant and flagged/unflagged PlatformOwner. Default non-finance roles include Manager, Operations, Sales, Marketing, FrontDesk, Teacher, Student and Guardian; do not infer denial remains after a valid custom grant.

| Page | Required load dependencies | Access mismatch |
| --- | --- | --- |
| Payments | Students + invoices + payments | Students needs students.manage, absent from default FinanceUser |
| Invoices | Students + fee plans + invoices + governance settings | Student permission mismatch; FinanceControls settings required even if Finance is enabled alone |
| Reconciliation | Invoices + payments | Both Finance / finance.manage; verify real role controls |
| Adjustments | Invoices + adjustments | Requires both Finance and FinanceControls |
| Governance decision queue | Adjustments + collections + admin-work-items + settings | AdminWorkItems has no catalog permission mapping; non-admins fail closed even with finance grant |

BUG-FUNC-0006 / SECURITY-ROLE-003 records unusable finance-page dependencies. Fix design must preserve least privilege: do not simply grant student-management or weaken unmapped-controller protection. Confirm intended lookup/approval access before implementation.

## Findings and remaining scope

New source findings: BUG-DATA-0010 (P0 transition consistency), BUG-FUNC-0005 (P1 cancelled approval still sent), BUG-FUNC-0006 (P1 finance lookup permissions). All OPEN / STATIC-FINDING / final verification NOT RUN. No runtime verdict is implied by these tables.

Still uncovered: full Finance Governance branding/settings and collections forms, fee-plan/reminder workflows, the rest of source controls and conditional branches. Authentication/portal provisioning/private-file access are the next bounded high-risk review. Aggregate multi-currency presentation, maker-checker policy, cancelled-invoice writes, refund meaning and concurrency require explicit test/policy decisions, not speculative repairs.
