# Teacher direct-route lifecycle repair — 2026-10-10

Issue: [BUG-SEC-0003](../ISSUES/BUG-SEC-0003.md), SECURITY-LIFECYCLE-001, E0 rank1.
**OPEN; bounded repair verified, not enterprise closure.**
Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `2b270758fef94d09bb3714de8e008896ff54d552`.

## Reproduction before application changes

Fresh labelled loopback SQL Server, both migration sets, real Identity login and HTTP controller pipeline; isolated runtime login and run-owned upload/storage folders. Existing tenant/anonymous controls unchanged. No development/Azure/customer database or credentials; no external delivery or model calls.

Baseline run `a629539092e44af7bf4d7fbb28120b40`:26 diagnostic cases. With the original active Identity and its existing signed token, both academy suspension and domain Teacher deactivation left all four direct actions available:

| State | GET resources | GET classroom-activity | POST note | POST upload |
| --- | --- | --- | --- | --- |
| Suspended academy | 200 | 200 | 200 + persisted row/notification | 200 + persisted row/notification/file |
| Inactive domain Teacher | 200 | 200 | 200 + persisted row/notification | 200 + persisted row/notification/file |

The academy-scoped resource route returned403 for suspension; `/api/teacher/me` returned403 for inactive Teacher. Neither endpoint had to be called before a direct request. This upgrades the accepted static finding to runtime-confirmed. Diagnostic baseline exit0 means the explicitly unsafe baseline was classified, **not** security acceptance. Default new module runs strict denial assertions.

## Narrow host enforcement

[TeacherPortalAccessFilter](../../apps/api/Security/TeacherPortalAccessFilter.cs) is attached at controller level using TypeFilter, so direct calls do not depend on visiting Me first. Before a teacher action executes it resolves the authenticated Identity and requires:

- Active current Identity with a linked academy and Teacher.
- Current persisted Teacher role, not the role claim in an older signed token.
- An active domain Teacher belonging to that exact server-resolved academy.
- An existing active academy.

Denied filter requests return403 with a generic understandable message, without revealing another tenant's records. Unknown Identity returns401. Existing middleware stamp/account checks remain. No PlatformOwner bypass or permission expansion. Batch/student scope, current legacy upload formats/size, notification rules and successful response contracts are unchanged. The normal academy filter and private-file access service are not rewritten.

This is an entry authorization check, not a claim that concurrent lifecycle revocation is locked atomically with every subsequent mutation. Subscription/module policy and inactive-batch policy were not invented here.

## Fixed evidence

Final run `7696f6a8fd8847d5919a5a6b185440de`: **62/62 strict native Identity/SQL/HTTP cases PASS**, no HTTP429 substitution. The four reviewed endpoints retain active/restored200 controls and now return403 for suspension, inactive domain Teacher, missing/foreign domain linkage, inactive Identity and removed current Teacher role with retained linkage/token. Anonymous401 and original foreign/unassigned contracts remain (resource list200 empty; classroom403; invalid batch note/upload400). Me/profile/progress/calendar additionally verify active200 and suspended/inactive403.

Every read/denial compares fresh SQL resources, notifications, audits, students, batches and enrollments plus recursive run-owned web/storage file hashes for no change. Successful notes/uploads verify exactly one new own-tenant row and recipient notification; upload adds exactly one file with exact synthetic bytes and matching response ID. Existing/foreign data and files are preserved. Own reads include the sentinel and exclude foreign/unassigned sentinels.

Local, intentionally uncommitted structured evidence:

- `QA/EVIDENCE/teacher-lifecycle/a629539092e44af7bf4d7fbb28120b40/results.json`
- `QA/EVIDENCE/teacher-lifecycle/7696f6a8fd8847d5919a5a6b185440de/results.json`

Independent unchanged private-media run `be629735e6734e0884c8326bf1a44cfd`: SECURITY-FILE-001 control PASS and **29 additional class-material HTTP regressions PASS**, including byte/range/HEAD integrity, anonymous/cross-tenant denial, domain/role/account/academy revocation and public-asset preservation. **77 targeted existing resource/media unit tests PASS**, no skipped tests. Application/harness builds0 warnings/0 errors; runner PowerShell parsing and diff checks PASS. Both SQL hosts apply88 application/7 Identity migrations and verify database-scoped non-admin runtime privileges.

## Test-fixture failures retained in the record

- Initial harness compile failed on an awaited byte-array comparison invoking a span overload. Split the awaited read from the comparison; successful rebuild followed. No application change for this.
- Windows PowerShell blocked the first script launch under its execution policy, before provisioning. Used process-only `-ExecutionPolicy Bypass` for the inspected local runner; no persistent policy/configuration change.
- First fixed run `d349ff3d07f549a9bf9fc61375a9ac57` reached61 passing cases but its final active calendar request omitted mandatory year/month, so returned400. Corrected only the fixture to `year=2026&month=10`; reran all62 cases in a fresh owned SQL container. This failed run is not counted as accepted.
- Successful runners verified ownership and removed only their own database/login/container. The failed fixed-run container was separately inspected for exact name, run label and loopback1433 binding, then stopped/removed. This disposed only synthetic failed-run data; source and evidence retained. Existing `pentaaimodels-api-1` and `pentaaimodels-inference-1` were not stopped/pruned/modified.

## Reproduce the strict gate

From this worktree, with Docker running:

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/teacher-lifecycle-sql
powershell -NoProfile -ExecutionPolicy Bypass -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module TeacherLifecycle
```

`QA_TEACHER_LIFECYCLE_BASELINE` must be unset for acceptance. Its value1 is only for reproducing the historical unsafe implementation; never certify its diagnostic output as a fixed gate. No existing regression, rate limiter, authentication or tenant guard was weakened.

## Remaining gates / next route

Live Teacher browser denial/restoration UX and physical Android/iOS, broader teacher mutations, module entitlement policy, related student/submission routes, concurrent revocation and full critical/release acceptance remain unverified here. Blob/browser/full enterprise suites were not rerun. No Mini inference/training/shared frontend code change; no security contract change or Mini handoff required for this host-only repair. Existing FW1 shared frontend handoff remains prepared, not delivered cross-chat. Main, production/Azure and unrelated Mini work unchanged.

NEXT **Sol High**, Academy Desk E0 rank2 **BUG-SEC-0006** minor-guardian explicit portal revocation reproduction, then conditional repair. **Sol Medium** remains suitable for the seven remaining feedback routes (Trial Bookings next) and settled visual implementation. No model/project switch claimed. Enterprise/Mini/release issues stay OPEN.
