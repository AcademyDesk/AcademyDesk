# Academy Desk handoff

## Current state

Latest local checkpoint (2026-10-03): [Reconcile/void ledger guard](QA/REPORTS/PHASE_2B_PAYMENT_RECONCILE_VOID_GUARD.md) on `codex/enterprise-p0-continuation`. Reconcile now uses the invoice-first SQL lock and transaction, re-reads the payment, updates invoice status when explicitly restoring a Voided payment, and rejects restoration that would overcollect. Fresh real Identity/HTTP/disposable-SQL reconcile/void pairs passed 5/5 twice, including gross/approved-adjustment invoices; explicit restore and replaced-amount rejection passed. Existing TransitionRace (two matrices), Transition, CollectionRace and 1,042 API tests passed. This preserves current explicit restoration behavior without approving its business policy. BUG-DATA-0010 remains OPEN for that policy, Voided-to-Completed, browser/device and release gates. No main/merge/push/Azure. Next: obtain the restoration policy before broader state-transition closure; Sol High for any further financial-state implementation.

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

## Active handoff

CURRENT PHASE: Cursor on Grok 4.7 has reviewed the Codex P0 continuation and is continuing the next open finance race. Branch `codex/enterprise-p0-continuation`, worktree `D:\AcademyDesk-codex-p0`. Do not edit `D:\AcademyDesk` main or its untracked `QA/EVIDENCE`. No merge, push, or Azure deployment.

COMPLETED BY CODEX: Invoice-row lock on payment create (`bbe73f0`), five-pair adjusted collection race (`0c893fa`), concurrent void repair (`ea67638`, `4eecfd0`), and a signed-in Payments browser check against disposable SQL (`a3a03d2`). Reconcile now uses the same invoice lock, re-reads the payment, rejects a void restore that would overcollect, and keeps the invoice status aligned. See `QA/REPORTS/PHASE_2B_PAYMENT_RECONCILE_VOID_GUARD.md`. Issues stay OPEN.

NOT ACCEPTED: Physical device, approval-versus-payment concurrency, over-adjustment and void-restoration policy, zero-net payroll policy, BUG-SEC-0001, certificate pagination and null audit actor, and the pre-Azure gates.

NEXT SLICE: Reproduce approving a pending adjustment at the same time as a payment for the pre-adjustment remainder. Stay in Cursor on Grok 4.7 for that reproduction. Move to Codex, Sol High, only if the race needs a new transaction design. Do not open v0, Lovable, or Framer.

## Data Protection classification

The API container logs that data-protection keys are stored under `/root/.aspnet/DataProtection-Keys` and may be unencrypted. Those keys do not survive container replacement and are not shared by replicas. That is acceptable for this single-container QA run. Before Azure scale-out, production needs a persisted shared key ring. This slice does not add that ring.

Update this file after substantial work. Record the branch, commit, what changed, what was validated, what remains open, and the next route.
