# C15 teacher availability on disposable SQL — 8 November 2026

Desktop verified the [C15 availability repair](../HANDOFF/C15_TEACHER_AVAILABILITY_2026-11-07.md) on
the integrated source at `e9a0946` and confirmed a separate audit-integrity defect.
The checks used the actual API, real synthetic Identity accounts and SQL Server.
Both runs used the same acceptance cases.

| Check | Unchanged C15 | After the action-level transaction |
| --- | --- | --- |
| Legacy null and mixed slots | The legacy GET returned an empty list. Valid slots and nullable times survived PUT and fresh GET. | Same behavior and exact stored JSON. |
| Actor audit on a successful write | Academy Admin and Academy Owner each had one attributed audit. Platform Owner had none. | Each role had one audit with the correct tenant, actor and route. |
| Mandatory audit INSERT fails | Academy Admin and Academy Owner got 500 with the profile change persisted. Platform Owner got 200 and bypassed the injected fault. | Each role reached the fault once, received 500, and left the captured SQL state unchanged. |
| Refusals | Teacher and foreign Admin got 403, anonymous got 401, absent or foreign teacher IDs got 404 and a duplicate employee code got 409. No captured writes. | Same results and no captured writes. |
| Total | 9/13 | 13/13 |

Only `ProfilesController.UpdateTeacherProfile` opts in to the existing domain
transaction and the Platform Owner audit path. The SQL comparisons cover all
teacher rows, including compensation and the same-tenant and foreign fixtures,
plus captured Identity roles and links, ledger rows and audits. The C15 reader,
other profile actions and access rules are preserved.

Baseline SQL `145d8d5a952d4fb895ba43ec89c0c070`. Repaired SQL
`083f4dcc56004b4e9255b41d4b415772`. The repaired run verified 85 application and
seven Identity migrations and removed its owned database and login. The
guarded wrapper removed only the owned SQL containers and verified the original
Docker inventory and PENTA services. The full API suite passed 2,087 tests with zero failures or skips. Raw logs and source hashes stay local.

Reproduce with the existing disposable SQL runner after building
`QA/tools/SqlHarness/SqlHarness.csproj`.

```powershell
powershell -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module TeacherAvailability
```

The teacher profile device and critical cases, the wider profile/audit matrix,
and the existing release gates remain open. Keep both
[BUG-API-0006](../ISSUES/BUG-API-0006.md) and
[BUG-API-0002](../ISSUES/BUG-API-0002.md) open.
