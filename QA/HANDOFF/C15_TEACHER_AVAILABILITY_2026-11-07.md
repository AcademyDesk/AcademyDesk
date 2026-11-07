# C15 teacher availability read/save safety — 7 November 2026

Owner Cloud. Status CLAIMED; implementation/tests pending.
Branch `codex/cloud-c15-teacher-availability-20261107`.
Clean base `19f235c090c8d621249b7994dbd6480737fd70f7`, confirmed locally.
Latest inspected Desktop `9269c64`: finance/payroll release evidence complete;
no active competing ProfilesController claim found. Preserve its SQL/evidence.

Exact paths:

- `apps/api/Controllers/ProfilesController.cs` — ReadAvailability only
- `tests/AcademyDesk.Api.Tests/TeacherAvailabilityMetadataTests.cs`
- `QA/ISSUES/BUG-API-0006.md`
- `QA/HANDOFF/C15_TEACHER_AVAILABILITY_2026-11-07.md`

Reproduce the original persisted-null-entry GET and PUT-then-fresh-GET defect.
Make existing metadata safe to project while preserving valid day objects,
raw stored metadata, profile fields, tenant scope and existing write/audit policy.
Do not silently normalize records, change accepted metadata or scheduling rules.
Use actual controllers and authenticated HTTP with unique InMemory providers,
disabled migrations/bootstrap/media/PENTA, existing installed dependencies.
Retain original failures, verify repaired matrix and full API once after final
source. No frontend change: reuse the unchanged C14 frontend/build evidence.

Desktop owns SQL/persistence/atomic-audit/device acceptance. This report is the
bounded handoff instead of editing shared integration files. Preserve main,
Azure, environment settings, local work and model-loading stop. No new packages,
images, external sends or configured SQL connections. Sol High for runtime/
authority acceptance; Medium for the settled null guard and import/docs.
