# Academy Desk handoff

## Current state

Latest implementation checkpoint (2026-10-04): **PENTA AI0-S2b synthetic quota** on `codex/enterprise-p0-continuation`. Approval, usage reservation and usage entry schemas now have academy-scoped FKs; the only enabled R0 diagnostic records `ApprovalMode=None` and cannot be confirmed into a real action. A new synthetic claim atomically reserves one UTC-day work unit under an academy SQL lock (defaults 20/user, 100/academy); known usage reconciles and uncertain usage stays reserved. [S2b report](QA/REPORTS/PENTA_AI0_S2B_SYNTHETIC_QUOTA.md) records 1,055/1,055 API and final run-owned real Identity/HTTP/SQL concurrency, FK, unknown-outcome and restart pass `e18756078110487684eaa6fec8e68753`; owned resources removed. This is **not a live-model financial budget or approval workflow**. Preserve local `QA/EVIDENCE`. No live provider, academy write, GitHub push, merge or Azure deploy. NEXT: S2c priced budget, conservative recovery/fault boundary, then S3 only after its gates. Model: GPT-6.1 Sol High, Codex Default mode.

Prior implementation checkpoint (2026-10-04): **PENTA AI0-S2a** added a SQL-backed synthetic task/execution/attempt ledger, tenant-linked keys, per-actor/tool idempotency claim, recorded replay/conflict/pending behavior and PENTA-owned outcome audit. The gateway remained default-off and Development/Testing-only. [S2a report](QA/REPORTS/PENTA_AI0_S2A_EXECUTION_LEDGER.md) records 8/8 targeted API, 1,052/1,052 full API and real Identity/HTTP/disposable-SQL evidence. The current S2b checkpoint above supersedes its next-step pointer; enterprise release gates remain open.

Prior implementation checkpoint (2026-10-04): **PENTA AI0-S1** was locally implemented and bounded tests passed on `codex/enterprise-p0-continuation`. The authenticated, default-off gateway allowed only a synthetic Development/Testing diagnostic, with explicit current academy/admin policy and fixed tool dispatch. [AI0-S1 report](QA/REPORTS/PENTA_AI0_S1_FOUNDATION.md) records final full API 1,051/1,051, targeted PENTA 7/7, SQL harness build 0 warnings/errors, and run-owned Identity/HTTP/SQL PENTA pass `fa30408d8666477ab0d0e24d742d32dd`; owned SQL resources were removed. At S1 the generic audit recorded one synthetic success; S2a now owns that audit. No customer data, live provider, domain mutation, UI or Azure deployment. The five P0 issues and release gates remain open.

Prior PENTA design checkpoint (2026-10-04): the owner confirmed **Academy Desk PENTA** with Pulse, Executor, Navigator, Twin and Autopilot. Executor replaces the AI product name Operator; Navigator replaces Growth while expanding opportunity/strategic guidance. Existing subscription/module names and historical reports retain their meanings. [Product vision](AI_PRODUCT_VISION.md), [implementation plan](ACADEMY_DESK_AI_IMPLEMENTATION_PLAN.md) and [architecture decision PENTA-001](PENTA_ARCHITECTURE_DECISION.md) define one shared platform, explicit actor/academy policy, narrow tools, approval binding, execution/audit ownership, idempotency, budgets and ten golden journeys. This paragraph preserves the pre-implementation design checkpoint; current implementation evidence is above.

The AI0-S1 task packet in [the decision](PENTA_ARCHITECTURE_DECISION.md#next-task-packet-ai0-s1) is now implemented locally. AI0-S2 adds durable execution/approval/usage records and SQL proof. Astra's focused authority-design step is complete; return only for a consequential unresolved architecture/security decision.

Enterprise continuation remains independently open: retain the finance/race/browser repairs below and finish the existing policy, physical-device, security and release gates. PENTA design does not close BUG-DATA-0001/0002 or any other issue, and does not authorize merge, push or Azure deployment.

Latest product direction (2026-10-04): the owner confirmed IFRS principles and applicable Indian GST rules for adjustment/refund/credit-note design. [Policy review](QA/REPORTS/PHASE_2B_ADJUSTMENT_REFUND_POLICY_REVIEW.md) now records this direction and the required separate workflows. This is not an accounting/legal sign-off or a product-code fix. Current `Refund`/`CreditNote` still only reduce unpaid balance; `BUG-DATA-0002` remains OPEN. Release requires implementation, qualified tax/accounting review, tests and all normal gates. No main/merge/push/Azure.

Latest local checkpoint (2026-10-04): [Adjustment/refund policy review](QA/REPORTS/PHASE_2B_ADJUSTMENT_REFUND_POLICY_REVIEW.md) traced the five adjustment types through UI, API and invoice ledger. All currently reduce unpaid collectible balance; `Refund` performs no payout and `CreditNote` has no distinct document/tax handling. The transaction guard is a safety gate, not a product-policy decision. No application behavior changed. `BUG-DATA-0002` remains OPEN. Next: obtain the business/accounting treatment for refunds, credit notes and post-payment reductions before implementation; continue independent physical-device and release-gate work. No main/merge/push/Azure.

Latest local checkpoint (2026-10-04): [Linked Finance Governance browser/SQL check](QA/REPORTS/PHASE_2B_FINANCE_APPROVAL_LINKED_BROWSER.md) on `codex/enterprise-p0-continuation`. A QA-only BrowserApproval harness mode seeded a fully collected invoice and pending discount in run-owned SQL. Signed-in source-identical browser confirmation produced one real 400 and the exact balance alert; reload kept PendingApproval. Final SQL asserted Paid/1,000 collected/two payment rows/adjusted 0 and an unapplied proposal. Tenant-isolation preflight and 390×844 no-overflow check passed; harness build 0 warnings/errors. Owned SQL, browser and frontend were stopped. No application source, main, merge, push or Azure change. BUG-DATA-0002 remains OPEN for over-adjustment/refund policy, physical devices, broader role/fault and release gates. Next route: Sol High only for the finance policy decision; Cursor for routine physical-device/browser validation.

Latest local checkpoint (2026-10-04): [Finance approval browser feedback](QA/REPORTS/PHASE_2B_FINANCE_APPROVAL_BROWSER_FEEDBACK.md) on `codex/enterprise-p0-continuation`. The Finance Governance page replaces an unsupported native prompt with an inline approval/rejection note form, displays the backend balance-rejection message as an alert, refreshes the pending queue, and guards a decision in flight. A source-identical page with synthetic loopback API passed desktop browser cancel/reject/required-reason checks; TypeScript passed and targeted lint had 0 errors/3 inherited warnings. This is bounded UI evidence, not a linked SQL/browser or physical-device pass. BUG-DATA-0002 stays OPEN for policy, linked/browser-device and release gates. No main/merge/push/Azure.

Latest local checkpoint (2026-10-04): [Approval/payment transaction guard](QA/REPORTS/PHASE_2B_APPROVAL_PAYMENT_GUARD.md) on `codex/enterprise-p0-continuation`. `FinanceAdjustmentsController.Decide` now serializes on the same invoice SQL row as payment creation, re-reads the pending adjustment after the lock, and rejects an approval that would put collected money above the adjusted collectible amount. The earlier 5/5 live SQL overcollection reproduction now passes 5/5 in three fresh runs, with payment winning and approval returning 400/no write. An approval-first control rejected the stale payment and preserved paid 600/balance 200. Existing Adjustment and AdjustmentRace SQL modules and all 1,044 API tests passed. BUG-DATA-0002 stays OPEN for over-adjustment/refund policy, browser/physical device and full release gates. No main/merge/push/Azure. Next: bounded browser/device and policy follow-up; Sol High only for unresolved finance policy or deeper concurrency, Cursor for routine validation.

Previous local checkpoint (2026-10-03): [Reconcile/void ledger guard](QA/REPORTS/PHASE_2B_PAYMENT_RECONCILE_VOID_GUARD.md) on `codex/enterprise-p0-continuation`. Reconcile now uses the invoice-first SQL lock and transaction, re-reads the payment, updates invoice status when explicitly restoring a Voided payment, and rejects restoration that would overcollect. Fresh real Identity/HTTP/disposable-SQL reconcile/void pairs passed 5/5 twice, including gross/approved-adjustment invoices; explicit restore and replaced-amount rejection passed. Existing TransitionRace (two matrices), Transition, CollectionRace and 1,042 API tests passed. This preserves current explicit restoration behavior without approving its business policy. BUG-DATA-0010 remains OPEN for that policy, Voided-to-Completed, browser/device and release gates. No main/merge/push/Azure.

Previous local checkpoint (2026-10-03): [Payments browser/SQL check](QA/REPORTS/PHASE_2B_PAYMENTS_BROWSER_CHECK.md) on `codex/enterprise-p0-continuation`. Synthetic loopback browser and disposable SQL passed the Reconciled/adjusted balances, exact collection, stale-balance rejection, mobile emulation and final two-row ledger assertions. `apps/web/src/app/payments/page.tsx` now gives durable success/error feedback and describes the adjusted amount; a new QA browser fixture is added. TypeScript and harness build pass. Changes are local in this worktree only; do not merge/push/deploy yet. BUG-DATA-0001/0002 remain OPEN for physical devices and broader gates.

| | |
| --- | --- |
| Project | Academy Desk |
| Product | AI-native SaaS operating platform for academies |
| Initial market | Music academies, with architecture capable of supporting other instructor-led academies such as dance and arts |
| Repository | `D:\AcademyDesk` |
| Accepted source of truth | GitHub |
| `main` | `f7af512f884ca5fe8ac3ddb287c852492592e709` until the Docker branch is reviewed |
| Active branch | `codex/enterprise-p0-continuation` in `D:\AcademyDesk-codex-p0`; Cursor branch retained separately |
| Application baseline | `535e6784e38b20fa1c9486b889544706f3c454a7` |

## Tech stack

- Next.js frontend
- ASP.NET Core API
- EF Core
- SQL Server
- Docker
- Azure target deployment
- GitHub source control

## Baseline validation completed

The following checks passed at commit `535e6784e38b20fa1c9486b889544706f3c454a7`:

- .NET build passed
- 1,042 API tests passed
- TypeScript validation passed
- Dockerfile static validation passed
- QA parsing validation passed
- `git diff` checks passed
- staged secret scan passed

## ENTERPRISE TESTING IS NOT COMPLETE

The baseline above does not complete enterprise testing. Continue the existing testing program. Do not restart it from zero, and do not treat a later unit or API pass as a substitute for the remaining gates in `ENTERPRISE_TESTING_ROADMAP.md`.

Dockerfile static validation is not equivalent to:

- complete image builds
- container startup
- runtime validation
- database integration
- end-to-end integration

`QA/EVIDENCE` remains primarily local and is intentionally not broadly committed. Missing committed evidence files do not mean the underlying QA work is absent. Do not delete useful QA work because GitHub governance has been introduced.

Development seeder credentials were removed from current source. Local development values were rotated using secure local secrets. Do not reintroduce hard-coded credentials.

## DO NOT RESTART THE EXISTING TESTING PROGRAM

Continue from the existing baseline, QA work, tests, and evidence.

Start from these living records rather than inventing a replacement plan:

- `QA/00_QA_README.md`
- `QA/02_TEST_MASTER_PLAN.md`
- `QA/03_TEST_MATRIX.md`
- `QA/08_SECURITY_TEST_MATRIX.md`
- `QA/09_RELEASE_GATES.md`
- `QA/ISSUES/INDEX.md`
- `QA/REPORTS/`

Earlier QA checkpoints record their own historical counts and commits. Those records stay as history. The verified GitHub baseline for new work is `535e6784e38b20fa1c9486b889544706f3c454a7`.

## Current known work categories

- Authentication and security
- Authorization
- Role isolation
- Tenant and academy isolation
- Responsive UI
- Dropdown layering
- Dropdown positioning on scroll
- Mobile and null failures
- Time and clock standardization
- Layout and alignment
- Integration testing
- Transaction and concurrency testing
- Enterprise remediation

Known shared UI cases already tracked in QA include `UI-DROPDOWN-001`, `UI-SCROLL-001`, and `UI-TIME-001`, with issue records such as `BUG-UI-0001`. Open issues remain in `QA/ISSUES/INDEX.md`. Phase 2B repair reports in `QA/REPORTS/` are continuation points, not a closed release.

## Product direction in force

Academy Desk is evolving into an AI-native academy operating system on top of the existing deterministic platform. The current ERP, APIs, permissions, and business rules stay. Read `AI_PRODUCT_VISION.md` before changing product behaviour, architecture, UX, or AI functionality.

Azure deployment waits for the pre-Azure quality gate in `ENTERPRISE_TESTING_ROADMAP.md`. Do not deploy unless explicitly instructed.

## Retained enterprise remediation handoff

The current PENTA engineering handoff is at the top of this file. This finance checkpoint remains the continuation point for the enterprise remediation track; its recorded passes are preserved.

CURRENT PHASE: BUG-DATA-0002 approval versus payment guarded locally. The issue stays OPEN.

COMPLETED THIS SLICE: The earlier five-pair failing race (`d66c4ad4118c4001a2a3e9703ea1d7e6`) is retained as baseline. The local invoice-first transaction and balance guard passed corrected strict five-pair SQL/HTTP runs (`50a9f525d4164ba1aa09db0368c03d71`, `811706c0ed0a45b482737284791c572d`, final `5903c5aec0f8436c89b08b24e94d02b2`). All five in each run observed payment-first, approval 400/no write; a forced approval-first control rejected the stale payment with 400 and left the adjusted ledger exact. Adjustment and AdjustmentRace SQL modules and 1,044 API tests passed. All owned SQL containers were removed.

NOT ACCEPTED: Physical device, over-adjustment/refund policy, void-restoration policy, BUG-SEC-0001, certificate pagination and null audit actor, and the pre-Azure gates. Codex's earlier collection, void, reconcile, and Payments browser checks remain valid.

TEST RESULTS: `QA/REPORTS/PHASE_2B_APPROVAL_RACE.md` is the retained failing baseline; `QA/REPORTS/PHASE_2B_APPROVAL_PAYMENT_GUARD.md` is the repair/retest.

NEXT SLICE: Review the bounded finance guard, then use Cursor for routine browser/physical-device checks or Sol High if an unresolved finance policy/concurrency decision is needed. Do not open v0, Lovable, or Framer for this finance gate.

MODEL/TOOL ROUTING: Codex/Sol High for this transaction repair; Cursor for routine retesting. Astra only if policy/architecture remains unresolved after focused review.

BRANCH: `codex/enterprise-p0-continuation` in `D:\AcademyDesk-codex-p0`. Do not merge to `main`. Do not edit `D:\AcademyDesk` main.

## Data Protection classification

The API container logs that data-protection keys are stored under `/root/.aspnet/DataProtection-Keys` and may be unencrypted. Those keys do not survive container replacement and are not shared by replicas. That is acceptable for this single-container QA run. Before Azure scale-out, production needs a persisted shared key ring. This slice does not add that ring.

Update this file after substantial work. Record the branch, commit, what changed, what was validated, what remains open, and the next route.
