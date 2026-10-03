# Enterprise testing roadmap

## Current verified baseline

Commit: `535e6784e38b20fa1c9486b889544706f3c454a7`

GitHub `origin/main` matches this commit.

Already completed at this baseline:

- .NET build PASS
- 1,042 API tests PASS
- TypeScript PASS
- Dockerfile static checks PASS
- QA parsing PASS
- `git diff` checks PASS
- staged secret scan PASS

## THE ABOVE DOES NOT COMPLETE ENTERPRISE TESTING

Dockerfile static validation is not a complete image build, container startup, runtime validation, database integration, or end-to-end integration.

Unit and API passes do not close authentication, authorization, tenant isolation, concurrency, browser, responsive, or Azure gates.

## DO NOT RESTART TESTING FROM ZERO

Continue from existing tests, QA issues, reports, tools, and evidence.

Do not delete useful QA work because GitHub governance has been introduced. `QA/EVIDENCE` remains primarily local and is intentionally not broadly committed. Absence from git is not absence of the work.

Inspect and continue these records:

| Record | Role |
| --- | --- |
| `QA/00_QA_README.md` | QA control centre and checkpoint history |
| `QA/02_TEST_MASTER_PLAN.md` | Layers, automation boundaries, and infrastructure sequence |
| `QA/03_TEST_MATRIX.md` | Stable scenario backlog |
| `QA/04_ROLE_PERMISSION_MATRIX.md` | Role and permission boundaries |
| `QA/08_SECURITY_TEST_MATRIX.md` | Security cases and current risk notes |
| `QA/09_RELEASE_GATES.md` | Proposed developer, staging, and production gates |
| `QA/ISSUES/INDEX.md` | Open issue register |
| `QA/REPORTS/` | Phase 1 and Phase 2 execution reports |
| `QA/COVERAGE/` | Module coverage expectations |

Earlier checkpoints in `QA/00_QA_README.md` record historical counts and commits. Keep them. New work starts from the verified baseline above and from the still-open issues, not from a new plan that discards them.

Development seeder credentials were removed from current source. Local development values were rotated using secure local secrets. Do not reintroduce hard-coded credentials, and do not copy secrets into QA evidence.

## Pending and continuing test areas

### 1. Full Docker validation

- actual API image build
- actual web image build
- container startup
- runtime environment
- networking
- health checks
- failure behaviour

### 2. Database and SQL Server

- real SQL Server integration
- EF migrations
- migration upgrade path
- transaction behaviour
- constraints
- rollback and failure behaviour
- realistic database semantics rather than relying solely on EF InMemory

`QA/02_TEST_MASTER_PLAN.md` already states that EF InMemory cannot substitute for disposable SQL Server persistence, transaction, and concurrency proof.

### 3. HTTP and API integration

- actual HTTP pipeline
- routing
- middleware
- model binding
- serialization
- validation
- status and response semantics

### 4. Authentication

- login and token lifecycle
- refresh behaviour
- invalid and expired tokens
- protected endpoints

Continue from `QA/08_SECURITY_TEST_MATRIX.md` and the auth issues already registered under `QA/ISSUES/`.

### 5. Authorization and RBAC

Validate the application roles and access boundaries already defined in `QA/04_ROLE_PERMISSION_MATRIX.md` and `QA/08_SECURITY_TEST_MATRIX.md`.

### 6. Tenant and academy isolation

Confirm one academy or tenant cannot access or mutate another academy's data. Include the two-academy isolation expectation already in the master plan and release gates.

### 7. Security

Continue remediation and regression testing for identified security risks in `QA/ISSUES/` and `QA/08_SECURITY_TEST_MATRIX.md`.

### 8. Concurrency and transactions

- duplicate operations
- concurrent writes
- transaction boundaries
- race conditions
- conflicting operations

### 9. Frontend, browser, and end-to-end

- core user journeys
- API integration
- navigation
- forms
- error states

Browser and end-to-end coverage follows `QA/02_TEST_MASTER_PLAN.md` and the open browser or device notes on existing Phase 2B reports. Do not mark a workflow accepted from API tests alone when its issue still records browser or device work as open.

### 10. Responsive and mobile

Include known areas:

- dropdown overlays
- dropdown anchoring while scrolling
- mobile null errors
- layout and alignment
- clock and time standardization

Existing shared cases include `UI-DROPDOWN-001`, `UI-SCROLL-001`, and `UI-TIME-001` in `QA/03_TEST_MATRIX.md`, with `BUG-UI-0001` for dropdown geometry. Continue those protocols. CSS inspection is not browser proof.

### 11. Regression

Re-run affected tests after fixes. Preserve prior passing evidence when a report says those suites were retained and not rerun, and say so in the handoff. Do not silently replace an older result with a narrower new run.

### 12. Pre-Azure quality gate

Before Azure deployment, confirm:

- required builds pass
- critical automated tests pass
- Docker runtime validation passes
- the database migration path is validated
- authentication, authorization, and tenant-isolation blockers are resolved
- no known deployment-blocking security issue remains

Also respect `QA/09_RELEASE_GATES.md`. A blocked or not-run P0 case fails that gate. Do not deploy unless explicitly instructed, and do not deploy while this gate is open.

### 13. Azure validation

After deployment:

- startup
- health
- migrations
- API connectivity
- frontend and API communication
- authentication
- important smoke flows
- environment configuration
- logs and telemetry

Production smoke stays non-destructive, consistent with `QA/09_RELEASE_GATES.md`.

## Working rule

Enterprise remediation and this testing program continue in parallel with AI product work. AI functionality does not skip a security or testing gate. When a slice finishes, update `AI_HANDOFF.md` with what passed, what remains open, and the next route from `AI_MODEL_ROUTING.md`.
