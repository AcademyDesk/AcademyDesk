# Master test plan

The client is Next.js App Router / React / TypeScript, statically exported to Azure Static Web Apps. The server is ASP.NET Core 10 with EF Core SQL Server and ASP.NET Identity. Flutter, Dart, widget tests and RenderFlex do not apply. Their equivalents here are React component tests, browser DOM/layout tests and screenshots.

## Test layers and automation boundaries

| Area | Classification | Lowest reliable layer / concrete scope |
| --- | --- | --- |
| Amounts, balances, payroll, capacity, date ranges | FULLY AUTOMATABLE | Pure rule tests if extracted later; current controller tests plus SQL integration for concurrency. Test reconciliation, adjustment and net-pay rules first. |
| Optional inputs / required validation | FULLY AUTOMATABLE | HTTP model-binding + DTO contract tests, then one representative React submit for each form handler. DateOnly null versus empty string is a required distinction. |
| API status/body/serialization | FULLY AUTOMATABLE | ASP.NET WebApplicationFactory through the full pipeline; exact request and response shape from INVENTORY/CONTRACTS.md. |
| Persistence, transactions, unique indexes, joins | FULLY AUTOMATABLE | Disposable SQL Server with actual migrations for BOTH contexts. EF InMemory cannot substitute. Fresh context reads after commit. |
| Role, tenant and linked-student isolation | FULLY AUTOMATABLE | Full HTTP pipeline, real seeded identities and forbidden direct calls; include static uploads. |
| React forms, duplicate submit, notices | MOSTLY AUTOMATABLE | Proposed Vitest + Testing Library for isolated components; Playwright for actual native form events, async API and route transition. Not installed yet. |
| Scroll, dropdowns, modal positioning | MOSTLY AUTOMATABLE | Proposed Playwright Chromium/Firefox/WebKit with measured trigger/menu rectangles, nested scrolling and hit-testing. Human touch-device confirmation remains. |
| E2E workflows | MOSTLY AUTOMATABLE | Small critical suite using synthetic API+SQL data. Assert UI → request → DB → response → refreshed UI. Avoid duplicating every numeric rule here. |
| Screenshots / typography / themes | PARTIALLY AUTOMATABLE | Deterministic screenshot comparison only after human approval. Pixel equality alone cannot approve a poor layout. |
| iOS/Android keyboard, browser chrome, mic/camera, downloads | PARTIALLY AUTOMATABLE | Emulated viewports plus physical Safari/Chrome tests. WebKit emulation is not proof of real iOS behavior. |
| Aesthetics, professional appearance, intuitive workflow | HUMAN REVIEW REQUIRED | Review each portal with business users; never report an automated aesthetic PASS. |
| Failure/timeout/offline/partial save | MOSTLY AUTOMATABLE | Deterministic request interruption and DB fault injection in disposable environment, plus usability review of recovery message. |
| Security / vulnerability review | MOSTLY AUTOMATABLE | Dependency scanning + input/isolation assertions; threat-model review remains manual. |
| Performance/load | MOSTLY AUTOMATABLE | SQL query telemetry + workload runner in isolated staging. Thresholds: BASELINE REQUIRED. |
| Deployment, migration, backup/restore | PARTIALLY AUTOMATABLE | Readiness smoke + restore to new disposable DB + row/constraint verification. Operator review of storage, retention and recovery time required. |

## Coverage design

Use [generated matrix](03_TEST_MATRIX.md) as the stable backlog. Every controller action gets a contract/persistence scenario. Every native form gets async/validation/feedback coverage. Controlled editors outside native forms are in the field and action inventories and are covered by their API case and page-state family; they must receive component-specific cases during Phase 2. A source occurrence is not a runtime test count.

For a meaningful write, capture the entered synthetic value, exact outgoing JSON/form-data, model-binding result, guard decisions, expected relational changes, fresh DB read, serialized body/content type and client state after reload. Assert no extra records and no changes in tenant B. Test empty body where the action intentionally returns Ok()/204; never require JSON from a documented no-body operation. For object-returning creates, a null body, missing ID or wrong type is a contract failure even if SQL committed.

Inspect each optional field in [field catalog](INVENTORY/FORMS_AND_FIELDS.md), its request contract and database constraints. Use null/omitted/empty/whitespace for strings; null/omitted/valid/malformed for optional DateOnly/Guid/number; do not blindly convert every blank string or test irrelevant negative values on text. Conditional rules override unconditional optional labels (minor students require guardian identity; payroll model determines amount fields). Test string maximum at N-1/N/N+1 using the actual EF HasMaxLength value. Test decimal precision from mappings, not invented ranges. JSON columns require blank, null, valid object/array, literal `null`, scalar and malformed cases where consumed.

All screens receive applicable loading/empty/loaded/error/forbidden/session-expired states. Saving screens additionally cover field validation, in-flight disablement, successful durable confirmation, failure retaining input, and unknown outcome after a lost response. Filter/search screens add searching/no results; upload screens add progress/interruption. Do not invent offline synchronization: no offline queue/service worker is implemented. Expected offline behavior is recoverable error and preserved input.

## Infrastructure sequence (design only)

1. Keep xUnit/coverlet; add full-pipeline HTTP factory and disposable SQL Server test host. Disable development/production bootstrap in that host through test configuration, not production edits during this phase.
2. Implement a fail-closed environment guard: approved local/staging host, dedicated DB name prefix `AcademyDesk_QA_`, test-run marker, isolated credentials. Never accept an arbitrary production connection string for reset.
3. Add synthetic identity factory for two academies, all twelve named roles, custom permissions, linked/unlinked student/guardian/teacher records, active/inactive users and subscription variants.
4. Add component/browser tooling after review; stable accessible locators first, `data-testid` only when semantic selectors cannot disambiguate. Do not add thousands of cosmetic IDs.
5. Publish TRX/JUnit, per-test JSON failure records, screenshots, console/network logs and DB before/after summaries. Link TEST ID and ISSUE ID. Run manifest captures commit + dirty hashes + dependencies + migrations + browser versions.
6. Establish performance baselines and approve visuals; then enable [release gates](09_RELEASE_GATES.md).

## Report contract

Each run records run ID, UTC start/end and duration, commit, dirty-source hashes, environment, fixture version, total/passed/failed/skipped/blocked counts, failed cases by TEST priority P0–P3, new issues, known issues reproduced, previously failing cases now passing. One failing case may reference multiple issues; do not double-count test totals. Failure includes expected/actual, reproduction, source/function, endpoint, DB effect, evidence paths and browser/viewport where relevant. Findings from static review are reported separately from failed executed tests.

## What Phase 1 cannot establish

Source scanning is not complete semantic proof. Dynamic tab/conditional render combinations, physical-device behavior, Azure SQL schema drift, infrastructure backups and runtime role denial have not been verified. The local API was unavailable when its OpenAPI endpoint was checked. Do not start its ordinary seeder without a test database. No app/UI smoke PASS is claimed. No new dependencies or hundreds of executable tests are installed in this phase.
