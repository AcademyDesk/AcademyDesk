# Phase 2B — financial policy decision and implementation bundle

Date:2026-10-10. Academy worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, inspected HEAD `01e5531783bb34c44da282e85b6d8c4d9bf4a75e`. Sol High. E0 rank3, following the bounded teacher and guardian repairs. **PROPOSED / NOT APPROVED / NOT IMPLEMENTED.** This packet combines three existing policy gates; it does not restart the audit or create a new testing program.

## 1. Decisions to approve together

| Decision | Recommended product contract | Change from current behavior | Approval boundary |
| --- | --- | --- | --- |
| FP1 — BUG-DATA-0002 | Separate unpaid price reductions, refund obligations/settlements, and credit-note documents. Do not call legacy adjustment approval a cash refund or issued tax note. | Today all five types increment the same AdjustedAmount and reject reductions exceeding the unpaid balance. There is no separate cash-refund or tax-document workflow in this entity/controller. | Owner approves product separation and interim availability/wording. Qualified accounting/tax review approves applicability, recognition, document and reporting rules before financial release. |
| FP2 — BUG-DATA-0003 | Allow a valid zero-net earnings/deductions statement, explicitly **No cash due**, not a cash payout marked Paid. | Today gross1000/deductions1000 creates a Paid/net0 payout, default BankTransfer and PaidAtUtc. Gross1000/deductions1000.01 remains rejected. | Owner approves statement-vs-payout semantics. Payroll reviewer confirms lawful deductions, period and statutory treatment; arithmetic bounds alone do not authorize deductions. |
| FP3 — BUG-DATA-0010 | Treat Voided as terminal. Reject both generic restoration and dedicated reconciliation of a voided row; a verified correction is a separately recorded payment with preserved provenance and existing balance safeguards. | Dedicated reconciliation currently restores under an invoice lock and cap. Generic Voided-to-Completed has no matching restoration guard/recomputation in current source; not freshly runtime-reproduced here. | Owner approves terminal status. Correction authority, original/new linkage and approval/audit requirements must be settled before enabling a new correction workflow. |

These recommendations are **not** silent defaults. Record an explicit owner decision (FP1/FP2/FP3, date, selected alternative, reviewer and release limits) before behavioral implementation. Alternatives: retain restricted, expressly approved restoration through a dedicated correction action for FP3; reject zero-net payout attempts until a separate statement workflow exists for FP2. No generic status change should be used to bypass a correction contract.

Product approval is not accountant sign-off or permission to deploy. All three issues remain OPEN. Existing five P0 bounded repairs and original finance races remain intact.

## 2. FP1 — reduction, refund and credit-note contract

### Interim implementation after owner approval

- Keep Discount/Scholarship/Concession arithmetic, invoice-first transaction, approval conflict checks and audit unchanged.
- Clearly distinguish stored legacy Refund/CreditNote labels from actual payout or issued documents on request, directory, approval, export and AI artifacts. Never fabricate a refund receipt, credit-note number or settlement status.
- Recommended interim availability: stop creating new Refund/CreditNote requests through the generic reduction endpoint until their dedicated workflows exist. Display existing records with their original type/value/status and a legacy explanation; pending decisions require explicit reviewed disposition, not automatic approval, cancellation or reinterpretation.
- Deploy API and UI compatibility together. A disabled option in Manual or PENTA is not a server-side guard. Existing customers/clients and pending records need a migration/disposition plan before enabling the restriction.

### Dedicated workflows, not a scalar relabel

- Persist a refund obligation separately from the invoice reduction and the later settlement. Identify academy, payer/beneficiary, original invoice/payment references, currency, reason, approved amount, authorized approver, and settlement evidence. The exact eligibility/cap and accounting entries depend on reviewed policy.
- State proposal/approval/settlement separately: approval is not money movement; recorded transfer is not verified settlement. Reserve approved amounts under a shared transaction/lock so concurrent proposals or settlements cannot reuse the same refundable capacity. Use idempotency and explicit unknown-outcome handling; do not retry a possible external payout blindly.
- A credit note needs its own invoice linkage, issue identity/number/date, reviewed commercial/tax basis, applicable particulars and audit/export trace. Its document effect and any refund payable must be applied once, never twice through both AdjustedAmount and another ledger path.
- Keep original collections, reductions, obligations and actual disbursements distinguishable in invoice, Student360, payroll-independent finance, collections, document, dashboard and reminder projections. No negative receivable hidden by Math.Max is an accepted refund implementation.
- No production payout provider, GST return filing, chart of accounts, GST rates, tax registration, exemption or statutory reporting framework is configured/inferred by this packet.

## 3. FP2 — zero-net statement contract

- Preserve effective Monthly/profile gross and SessionBlock/default-or-authorized-override behavior. Preserve rejection of negative deductions, deductions above gross, invalid gross/session/profile/tenant. Do not invent new override permission or rounding policy.
- If approved, gross equals authorized deductions records earned gross/deductions/net0 as a statement with NoCashDue semantics. It must not create a cash-disbursement request, claim BankTransfer settlement or invent a paid-at timestamp/reference.
- Current PayrollPayout has nonnullable PaidAtUtc and a Paid/default BankTransfer representation. Define a compatible statement/settlement schema and DTO before implementation; changing only Status is insufficient. Distinguish statement completion date from actual payment date in list, payslip, export, totals and AI output.
- Positive net continues through the existing payout path until separately reviewed. No retrospective customer rows are rewritten. Legacy Paid/net0 rows retain their original data with explicit legacy interpretation pending authorized remediation.
- Define normalized pay-period identity, duplicate/idempotent requests, approved deductions and concurrent creation behavior before claiming a production-safe payroll statement. Zero-net treatment is not blanket permission for 100% deductions under wage law.

## 4. FP3 — terminal void and verified correction contract

- Both PATCH status=Completed on a Voided row and PATCH reconcile on a Voided row must reject after current tenant/actor checks. Proposed400: `Voided payments cannot be restored. Record a new verified payment.` The exact public error contract requires approval before tests adopt it.
- Preserve idempotent repeat void, historical reconciliation reference/timestamp, Completed-to-Reconciled reference-taking path, adjusted balances and Cancelled invoice handling. Do not downgrade Reconciled to Completed as an accidental correction shortcut; settle that separate transition explicitly.
- Serialize status decisions under the same academy-scoped invoice-first transaction; re-read payment after lock acquisition. Every accepted operation recalculates the same deterministic aggregate. A model, UI confirmation or remembered permission cannot authorize correction.
- Keep a voided record immutable for collected status. A new correction needs independently verified receipt evidence, a persistent original/new relationship and approved authority; it is not an unrestricted generic payment replay. Retain the existing `Payment exceeds the invoice balance.` response for new collections exceeding the remaining balance.
- Existing ReconcileVoidRace contains successful restoration and two200 expectations because it proves the old policy's consistency. Do not relabel that receipt as proof of terminal policy, skip the module, relax assertions or suppress failures. An approved policy change requires explicit versioned expectations: reconcile-before-void may produce two200s; void-before-reconcile must produce200/400 with unchanged historical evidence and zero collection. Preserve the same race iteration, exact-status, no429, fresh SQL/read-view and financial integrity assertions. Archive the old contract as historical, not as current executable acceptance.

## 5. Implementation order — one owner-approved bundle, bounded releases

| Slice | Backend + Manual/PENTA presentation | Required evidence before acceptance |
| --- | --- | --- |
| F1A | Interim legacy explanations and server-enforced generic-type restriction; explicit pending-record disposition. No tax or cash workflow activated. | Exact old/new client compatibility, optional/nulls, pending/approved/rejected legacy reads, approved reduction transaction/approval guards; real SQL/API and UI status evidence. |
| F3 | Terminal-void guards and policy-versioned correction capability; no new correction permission inferred. | Original transition/collection/approval races preserved; both restoration-entry refusals, serialized reconcile/void outcome matrix, no-write and immutable history. |
| F2 | Statement/settlement representation, zero-net semantics and affected DTO/read views; upgrade/rollback design preserving legacy rows. | Monthly and SessionBlock zero/cent/negative boundaries, duplicate/period/concurrent semantics, positive-net parity, statement-vs-cash exports and upgraded schema. |
| F1B | Dedicated refund obligation/settlement and credit-note document workflows under the reviewed accounting/tax contract. | Approved eligibility/ledger/document policy, migration compatibility, caps/reservation/idempotency/settlement failure/reversal, projections, audit and all release gates. |

Each slice needs a truthful API result and usable Manual flow; PENTA may expose it only after its separately versioned tool contract and host authority pass. No finance tool is activated by this document. A genuine shared Core/tool change uses the established [Mini handoff](../../PENTA_HANDOFF_TO_MINI.md); this packet does not require changing project or delivering a new Mini contract now. The unfinished shared AI frontend and Mini86/94 gate remain independent.

## 6. Acceptance matrix — planned, NOT RUN in this packet

Reuse existing original modules; add only the missing policy cases. Stable IDs below are proposal requirements, not new executed test counts.

| ID | Gate |
| --- | --- |
| FP1-01 | All three reduction types: partial/fully collected, approved200 on1000/collected600 gives balance200; excess rejected without financial writes. |
| FP1-02 | Generic Refund/CreditNote restriction enforced server-side; legacy approved/rejected/pending data remain exact; no fake payout/document. |
| FP1-03 | Refund eligibility and cap for partial/full collection use reviewed policy; concurrent obligations/settlements reserve once; no duplicate refund. |
| FP1-04 | Approval, queued transfer, failed/unknown/verified settlement and reversal maintain correct obligation and immutable evidence. |
| FP1-05 | Credit-note numbering/linkage/document particulars and tax applicability checked under a signed contract; document+refund do not double-reduce. |
| FP1-06 | Manual/API/PENTA/source/export totals reconcile separately to original receipt, reductions, liability and disbursement. |
| FP2-01 | Monthly gross1000/deductions0,999,999.99 preserve net1000,1,0.01;1000.01/1001/negative reject with no financial writes. |
| FP2-02 | Monthly and SessionBlock exact zero produce approved statement semantics, no bank payout/reference/paid-at claim; default/override/session controls preserved. |
| FP2-03 | Duplicate/normalized period, retry, concurrent creation and authorized deductions cannot create duplicate statements/payouts. |
| FP2-04 | Legacy rows, upgraded schema, payslip/list/export/API/PENTA distinguish earnings statement from actual cash settlement. |
| FP3-01 | Both Voided-to-Completed and Voided-to-Reconciled refuse with approved exact400; state and historical reference/timestamp unchanged. |
| FP3-02 | Reconcile/void race5/5 retains strict permitted serialized outcome pairs, aggregate/status/read-view agreement, no429. |
| FP3-03 | Existing two-void/create-void/collection/adjustment/approval-payment race5/5 matrices retain exact financial assertions. |
| FP3-04 | Verified separately recorded correction retains original provenance; balance-cap error unchanged; no double receipt or stale approval. |
| FP3-05 | Cancelled/repeated-void/current reconciliation and each allowed/disallowed transition are explicit, not generic status fallthrough. |
| SHARED-01 | Anonymous/unauthorized finance/foreign tenant/revoked actor and forged prompt/tool/context/approval all deny; no protected data or domain writes. |
| SHARED-02 | Audit failure, transaction rollback, stale approval, replay and partial/unknown outcome cannot falsely report success or silently retry. |
| SHARED-03 | Manual and AI confirmations, unavailable actions, amount/currency/source evidence, API outage, keyboard/mobile and physical Android/iOS tested at stated scope. |

No issue closure from an arithmetic guard, policy document or emulated mobile test. Exact-candidate enterprise, security, migration/restore, browser/device and pre-Azure gates still apply.

## 7. Source and inherited evidence

The three controllers and current entity/authority/race files were inspected without edits. All were unchanged relative to inspected HEAD. Key source SHA256 pins:

| Source | SHA256 |
| --- | --- |
| apps/api/Controllers/FinanceAdjustmentsController.cs | 7F50197ABF61241C875C9733DCCF8A122D59C42C6618BCC8931B683B5DC0835D |
| apps/api/Controllers/PayrollController.cs | 18488D7397C83CC071841C11DC3FC40B620FDC17A24A8D4880CEB4582EC0C23C |
| apps/api/Controllers/PaymentsController.cs | 43FFDCE25FE48C3E2FE045E88ADE65B9915D85DE543BA8B9181FA690E80754FE |
| QA/tools/SqlHarness/CollectionRaceRegression.cs | E64150799A7292D49D015E08D0EC722D9B16C7EC5CFEFEA27D9297D82DF2C85E |
| QA/tools/SqlHarness/ApprovalRaceRegression.cs | 6C053A6067FBDD6799E9A018CCA9E55F2913C7214211F5AA32C5F37A72727026 |
| QA/tools/SqlHarness/PaymentVoidRaceRegression.cs | 4859C8AA210267E34A0B0AC412988ED0DCA3CD6579DA9867D43CF799AE12BD75 |

Accepted bounded evidence is inherited, not rerun or inflated here: [adjustment/refund review](PHASE_2B_ADJUSTMENT_REFUND_POLICY_REVIEW.md), [approval/payment guard](PHASE_2B_APPROVAL_PAYMENT_GUARD.md), [payroll guard](PHASE_2B_PAYROLL_NET_REPAIR.md), [void/evidence repair](PHASE_2B_PAYMENT_TRANSITION_REPAIR.md), [reconcile/void guard](PHASE_2B_PAYMENT_RECONCILE_VOID_GUARD.md). Existing tests contain current-policy expectations; inspection is not fresh runtime acceptance.

## 8. Accounting/tax references and limits

Owner's IFRS/applicable Indian GST direction remains accepted as direction, not a claim that every academy is GST-registered, taxable or statutorily IFRS-reporting. Architectural separation is a recommendation, not a journal-entry/tax engine or legal opinion.

- IFRS15 paragraph55 discusses a refund liability where customer consideration is expected to be returned. [IFRS Foundation issued2024 reference](https://www.ifrs.org/content/dam/ifrs/publications/pdf-standards/english/2024/issued/part-a/ifrs-15-revenue-from-contracts-with-customers.pdf?bypass=on), official search excerpt retrieved2026-10-10; older full-standard URLs redirect to subscription access. No assertion that the latest complete standard was independently read.
- [CBIC CGST Act section34](https://taxinformation.cbic.gov.in/content-page/explore-act/1000304/1000001) describes credit-note conditions and reporting; [CBIC Rule53](https://taxinformation.cbic.gov.in/content-page/explore-rules/1000144/1000001) prescribes document particulars. Official indexed excerpts retrieved2026-10-10; direct open timed out. The section34 page also marks a2026 insertion as not yet notified: do not implement an announced amendment as effective law. No deadline/rate/effective-date rule is coded from these excerpts.

Qualified review must establish reporting framework, supply/tax/registration/exemption basis, applicable effective law, numbering/timing/return requirements, allowable deductions and accounting entries. No compliance certification from source references. Full legal verification remains a release prerequisite for the applicable workflow.

## 9. Actual completion and next route

Completed: source-pinned three-policy recommendation, migration/legacy boundaries, backend/Manual/AI slice order and18 planned acceptance requirements. Documents only. No application/test strategy/runner changes, SQL provisioning, runtime/browser/device tests, external payments, Mini mutation, main merge, push or Azure deployment. Evidence/unrelated dirty work preserved. Documentation paths/pins/scope/diff validation performed; no new product PASS claimed.

**Next:** owner approval of FP1/FP2/FP3 and interim legacy disposition. Sol High for consequential financial implementation; qualified reviewers before accounting/tax/payroll release. While pending, Sol Medium can batch Trial Bookings and Curriculum feedback from the existing seven-route queue without changing finance policy. Do not block that safe queue or reopen accepted Astra work.
