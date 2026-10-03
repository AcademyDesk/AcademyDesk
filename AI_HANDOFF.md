# Academy Desk handoff

## Current state

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

CURRENT PHASE: Sol High owns continuation of the existing enterprise QA/remediation program, with P0 before P1. Branch/worktree `codex/enterprise-p0-continuation`, `D:\AcademyDesk-codex-p0`, forked from clean `cursor/certificate-validation` at `bff83ca108b24833cc0f8d03684258337630eb80`. Do not edit `D:\AcademyDesk` main or its untracked `QA/EVIDENCE`. No merge, push or Azure deployment has occurred on this branch.

INHERITED REPAIR: Commit `bbe73f0` guards PaymentsController.Create with an academy-scoped SQL Server invoice `UPDLOCK,HOLDLOCK` spanning Completed/Reconciled balance read and insert. The original unadjusted five-pair race passed 5/5 (`7f5f02d9f7674c6aa7fb31aa3c6dfdb4`), sequential reconciliation passed (`d107470396c54192acbd73def3e6d59e`), and the API suite passed 1,042/1,042. Historical failing run `45e986ef6c4b4a72a840fcf09b604e4a` remains evidence.

CURRENT SLICE: BUG-DATA-0010 concurrent two-row voiding reproduced a stored PartiallyPaid invoice with zero collected in all five baseline pairs (`08d4f5b7d6b34ef3b7fa07ebc64d9973`). `PaymentsController.UpdateStatus` now locks the academy invoice row with SQL Server `UPDLOCK,HOLDLOCK`, re-reads payment after the lock and holds the transaction through invoice/payment save. Final five-pair gross/approved-adjustment two-void race (`09080e6c2b9c40aa86dc2c82a16866a2`) PASS 5/5; extended create/void race (`839ffd89254449deb7de71493e29dd60`) also PASS 5/5. First unadjusted post-fix run (`42dde8d5a7e040718b96a7043e316f77`) PASS 5/5. Existing Transition (`b0ff1b3939294c6b9c69b4d26f5d4867`) and CollectionRace (`f437a282eb614893be48609b2b28219f`) PASS; API tests 1,042/1,042 PASS. SQL harness build zero warnings/errors, owned resources cleaned. See `QA/REPORTS/PHASE_2B_PAYMENT_VOID_RACE_REPAIR.md`.

PREVIOUS SLICE: A separate `AdjustmentRace` module was added without changing product code or the original test. Fresh real Identity/HTTP/disposable-SQL five-pair run `34bba6beae6745ada1abf22005c5ff62` PASS: on invoice 1000, approved adjustment 200 and Reconciled 600, simultaneous 200+200 yielded one 201/one exact-message 400, collected 800, two rows, Paid and intact adjustment every time; no 429. Original `CollectionRace` rerun `902ccc02aeb845a4ba774b3179ed73c9` PASS 5/5. Harness build zero warnings/errors. Fresh Adjustment (`87241ab7f5834ad397f5d89ca43d7052`), Transition (`76bd09af6ebe430996b51aadac20f76b`) and Payroll (`7b7e3b20b6044ffe85d5f0c36d399c4a`) bounded SQL modules also PASS. All labelled run-owned containers were removed. See `QA/REPORTS/PHASE_2B_P0_CONTINUATION.md`.

NOT ACCEPTED: Live Payments browser, physical device, simultaneous adjustment approval versus payment, over-adjustment/refund policy, Voided restoration policy, reconcile/void and other untested interleavings, zero-net payroll policy, broader media/security, critical regression and pre-Azure quality gates. BUG-DATA-0001/0002/0003/0010 and BUG-SEC-0001 remain OPEN. BUG-FUNC-0032 official PDF pagination/audit actor/device remains OPEN. The browser-control runtime previously failed to initialize; use `QA/HANDOFFS/CURSOR_PAYMENTS_BROWSER.md` only for a browser-only Composer 2.5 evidence handoff if needed. No browser PASS claimed.

DOCKER CLASSIFICATION: `cursor/docker-runtime-validation` at `beb3f1c` has a documented single-container runtime gate ready for review, not a full release pass or automatic merge. See `QA/REPORTS/PHASE_DOCKER_RUNTIME_VALIDATION.md` for residual risks.

NEXT ROUTE: Sol High continues P0 engineering. Cursor Composer 2.5 may supply bounded browser/UI evidence; do not return routine ownership to Grok 4.7. Do not open v0, Lovable or Framer for this P0 slice.

## Data Protection classification

The API container logs that data-protection keys are stored under `/root/.aspnet/DataProtection-Keys` and may be unencrypted. Those keys do not survive container replacement and are not shared by replicas. That is acceptable for this single-container QA run. Before Azure scale-out, production needs a persisted shared key ring. This slice does not add that ring.

Update this file after substantial work. Record the branch, commit, what changed, what was validated, what remains open, and the next route.
