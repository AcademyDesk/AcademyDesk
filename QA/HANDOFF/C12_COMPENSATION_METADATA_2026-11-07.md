# C12 compensation metadata read safety — 7 November 2026

Owner: Cloud Codex. Status: CLAIMED, implementation and validation pending.
Branch: `codex/cloud-c12-compensation-metadata-20261107`.
Base: `3ffaa9edb409fee3984cd0e3525ee6a18390a3cd`, clean checkout verified.
Latest inspected Desktop claim: `8963869`, private media/payment/restore retest;
latest integration tip `3ffaa9e` includes the C11 return.

Exact claimed paths:

- `apps/api/Controllers/TeacherCompensationController.cs`
- `tests/AcademyDesk.Api.Tests/TeacherCompensationMetadataTests.cs`
- `QA/ISSUES/BUG-API-0004.md`
- `QA/HANDOFF/C12_COMPENSATION_METADATA_2026-11-07.md`

Reproduce valid non-object compensation JSON through the public controller GET,
then make its metadata lookup safe without changing legacy object aliases,
zero/null preservation, save/audit behavior, payroll rules or authorization.
Validate original root shapes, valid objects, missing/foreign teacher routing,
read-only preservation and relevant API regressions using existing dependencies.
Attempt the installed full API suite without downloading packages or contacting
the configured SQL host. InMemory/controller results are not SQL/HTTP authority
acceptance. Where practical use the existing test-host patterns for HTTP checks.

Portal announcement readers are part of the same issue but NOT claimed: Desktop
owns current portal/private-media retesting. Return an exact change request and
malformed-metadata cases instead of modifying its active controller. The issue
remains OPEN until that reader and all linked release checks are accepted.

No shared handoff overwrite: this task report is the bounded handoff for the
integrator to summarize in `AI_HANDOFF.md`. No main merge, Azure deployment,
environment changes, customer database or model loading/activation.

Next route: Sol High for local protected HTTP/SQL and portal metadata work;
Sol Medium for the settled read guard/report. Publication SHA follows validation.
