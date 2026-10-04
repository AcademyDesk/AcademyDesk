# Academy Desk handoff

## Current state

| | |
| --- | --- |
| Project | Academy Desk |
| Product | AI-native SaaS operating platform for academies |
| Initial market | Music academies, with architecture capable of supporting other instructor-led academies such as dance and arts |
| Repository | `D:\AcademyDesk` |
| Accepted source of truth | GitHub |
| Branch | `main` |
| Current verified baseline | `535e6784e38b20fa1c9486b889544706f3c454a7` |
| GitHub `origin/main` | Same verified commit |

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

| | |
| --- | --- |
| Last governance update | Documentation set created for agent context. No application, test, migration, Docker, Azure, or `QA/EVIDENCE` changes. |
| Next engineering route | Continue the existing enterprise remediation and testing program from the verified baseline. Route the task with `AI_MODEL_ROUTING.md`. |
| Commit state | These governance files are ready for review and are not committed by this handoff. |

Update this file after substantial work. Record the branch, commit, what changed, what was validated, what remains open, and the next route.

## Checkpoint: /penta design preview (branch `v0/penta-preview`)

| | |
| --- | --- |
| Branch | `v0/penta-preview` |
| Commit | `8faf473` |
| What changed | Added a new, isolated `/penta` route: a read-only student/batch lookup design preview. Not wired into existing navigation or routing used by other pages. |
| Data source | Synthetic demo data only, hardcoded in the page. No real API or SQL integration. No live AI involved. |
| Authorization | Owner/Admin gating shown in the UI is conceptual/visual only — it is not backed by real authentication, session, role, or tenant checks. Treat as unverified authorization. |
| Validated | TypeScript validation passed. Reported browser flows (single-match result, multi-match disambiguation, empty state) were exercised in the preview and passed, including a fix for incorrect pluralization in the disambiguation count text. |
| Explicitly NOT validated | Real API integration against PENTA's existing contracts, role/tenant security enforcement, source/data-provenance links, mobile layout, accessibility (screen reader/keyboard), and full regression acceptance. These remain open before this can be treated as production-ready. |
| Next route | 1. Codex reviews this diff against `AI_MODEL_ROUTING.md` and existing PENTA API contracts. 2. Cursor integrates the approved design against the real PENTA API/SQL data layer, adds real authorization checks, and closes the open validation gaps above. |
| Merge/deploy state | Not merged to `main`. Not deployed. This checkpoint covers documentation only — no merge or deployment was performed. |
