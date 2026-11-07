# C15 teacher availability read/save safety — 7 November 2026

Owner Cloud. Status IMPLEMENTED / CLOUD VALIDATED; local acceptance pending.
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

## Source and original reproduction

Implementation `3448a1a117136558c5116e89f3671ffb94a87dcb`; claim `75a4465`.
Latest observed Desktop `8f0a8c7` imports C13/C14. Its Profiles controller
matches this packet's original baseline; only this packet's reader guard differs.
Import only the four claimed paths. Do not overwrite Desktop's shared handoffs
or select this whole branch over its newer finance/restore evidence.

Original26 tests:21 PASS /5 FAIL /zero skips. Three direct read cases throw
NullReferenceException. Admin and Owner HTTP matrices each reproduce PUT500
and fresh GET500 for `[null]`, `[null,null]` and a mixed null/valid list:12
observed HTTP failures. Fresh contexts confirm profile values were persisted
before the failed response. Original logs/TRX remain retained.

The one-line repair ignores null list entries before reading their Day.
Existing valid entries, ordering, duplicate days, case-insensitive keys,
nullable From/To and invalid-shape fallback retain their behavior. Raw JSON
is retained under the existing trim/blank policy; no stored-row normalization
or new write rejection. Scheduling, profile writes and audit policy unchanged.

## Executed checks

Targeted70/70 PASS, zero skips:26 new availability cases plus44 retained
TeacherCreationService and PentaTeacherCreationFields cases. The new matrix
covers21 metadata inputs directly and42 authenticated save/read pairs across
Admin/Owner, plus anonymous401, foreign403, missing404 and Teacher write403.
Snapshots verify both direct-read teachers remain unchanged, exact saved
availability bytes and selected fields, compensation/certification/email
preservation, and denied operations without teacher changes.

HTTP uses actual MVC, Identity login/access tickets, current-account validation
and the academy access filter. Persistence is unique InMemory domain/Identity
providers; migrations/bootstrap/media/PENTA disabled. No configured SQL host,
provider send or model is contacted. These tests do not prove SQL persistence,
concurrency, mandatory audit rollback or physical-device acceptance.

```sh
/workspace/.tools/dotnet/dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter FullyQualifiedName~TeacherAvailabilityMetadataTests
/workspace/.tools/dotnet/dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore -p:UseSharedCompilation=false --filter 'FullyQualifiedName~TeacherAvailabilityMetadataTests|FullyQualifiedName~TeacherCreationServiceTests|FullyQualifiedName~PentaTeacherCreationFieldsTests'
/workspace/.tools/dotnet/dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --no-restore --no-build
git diff --check
node QA/tools/check-production-gates.cjs
```

SDK10.0.403, installed cache, no restore/download. Each test command also uses
run-specific `--logger 'trx;LogFileName=<run>.trx' --results-directory /workspace/c15-teacher-availability-20261107`.
The target rebuilds both assemblies; full API uses those exact binaries with
no intervening source changes. Diff check passes. Production guard still
exits1 for the five P0s. Full API regression2,084/2,084 PASS, zero failed/skipped/unexecuted.

No frontend changed. Reuse C14's exact frontend/build evidence:8 target and6
controlled browser cases pass; full frontend2,016 PASS /3 genuine communication
SQL-evidence FAIL, zero skips; types/build/lint pass with62 existing warnings.
Those three missing cloud receipts remain failures, not fabricated passes.
The current packet neither reruns nor relabels that unchanged-source evidence.

Raw evidence excluded from Git: `/workspace/c15-teacher-availability-20261107`,
including original controller, failure/final logs and TRX, production guard
output and source/assembly/artifact SHA256 manifest. Source hashes:

- Repaired ProfilesController: `636fd0e0a3d453f094f16c1c08bd1995b3768a6df825c101f1215528eda7a0db`
- New tests: `5da77aac8f1c37ac53592794bf35b5606c376c54d11059d3487ba17c66a07768`

## Desktop acceptance and production blockers

1. Import/review these four paths, preserving current profile/audit ownership.
2. On disposable SQL, authenticate Admin/Owner, seed original null/mixed cases,
   PUT and fresh GET. Verify200, exact stored metadata, retained valid slots,
   every unrelated profile/money/Identity row and appropriate attributed audit.
   Repeat denial/no-write and required-audit-failure controls. This null guard
   does not close the separate profile post-save audit gap in BUG-API-0002.
3. Check real teacher-profile selection/edit/readback and linked critical/device
   cases. Keep BUG-API-0006 OPEN until its complete closure criteria pass.

Five P0s, exact-source CI/images/security checks, remaining critical/device,
staging and production recovery remain OPEN. Source-only feature publication
does not trigger `verify.yml`'s restricted push filter or prove remote CI.
No main merge, Azure/environment/public-network changes. Mini/Plus/Pro remain
unqualified; Ultra deferred and model loading remains stopped. Next route:
Sol High for protected SQL/authority acceptance, Medium for settled import/docs.
