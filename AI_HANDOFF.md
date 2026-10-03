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
| Active branch | `cursor/certificate-validation` in `D:\AcademyDesk-cursor-certificate` |
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

CURRENT PHASE: BUG-DATA-0001 concurrent collection repair on `cursor/certificate-validation`. The issue stays OPEN; this branch is not merged or pushed.

COMPLETED THIS SLICE: Commit `bbe73f0` locks the academy-scoped invoice row with SQL Server `UPDLOCK, HOLDLOCK` before the Completed/Reconciled sum and holds a transaction through payment insertion. Ordinary academy requests reuse the finance filter transaction; direct/platform callers own one. The five-pair regression additionally checks the exact over-balance 400 message without reducing its original assertions. Prior failing run `45e986ef6c4b4a72a840fcf09b604e4a` remains historical evidence.

TEST RESULTS: SQL harness build 0 warnings/errors. Fresh `CollectionRace` run `7f5f02d9f7674c6aa7fb31aa3c6dfdb4` PASS 5/5: one 201 and one 400 with required message per pair, collected 1000, two rows, no 429. Fresh `Reconciliation` run `d107470396c54192acbd73def3e6d59e` PASS; original reconciled 600/balance 400/excess 500 rejection retained. API suite 1,042/1,042 PASS. Both owned SQL containers were removed. See `QA/REPORTS/PHASE_2B_COLLECTION_RACE_REPAIR.md`.

NOT ACCEPTED: Live Payments browser, physical device, broader finance concurrency, and pre-Azure quality gates. BUG-DATA-0001 remains OPEN. No Azure or production database touched.

OPEN BLOCKERS: BUG-DATA-0001 race/browser/device, BUG-DATA-0002, BUG-DATA-0003, BUG-DATA-0010, BUG-SEC-0001, and BUG-FUNC-0032 pagination/audit actor/device.

NEXT SLICE: Retest review and the remaining P0 queue return to Cursor on Grok 4.7. Do not open v0, Lovable, or Framer.

MODEL/TOOL ROUTING: Grok 4.7 in Cursor.

BRANCH: `cursor/certificate-validation`. Do not merge to `main`.

## Data Protection classification

The API container logs that data-protection keys are stored under `/root/.aspnet/DataProtection-Keys` and may be unencrypted. Those keys do not survive container replacement and are not shared by replicas. That is acceptable for this single-container QA run. Before Azure scale-out, production needs a persisted shared key ring. This slice does not add that ring.

Update this file after substantial work. Record the branch, commit, what changed, what was validated, what remains open, and the next route.
