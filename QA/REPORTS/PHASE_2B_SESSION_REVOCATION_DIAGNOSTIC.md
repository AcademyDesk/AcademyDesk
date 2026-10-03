# Phase 2B — native session-revocation gap reproduction

2026-10-02. **DIAGNOSTIC COMPLETE; SECURITY ACCEPTANCE FAIL / OPEN.** Two independent fresh owned SQL fixtures reproduced the same four policy failures. Thirty selected native HTTP requests/cases per run (60 executions, not 60 unique scenarios); 26 expected observations and four failed policy gates per run. Fixture/preflight requests and SQL assertions are additional controls, not included in 30. Wrapper exit0 means diagnostics and owned cleanup completed, not that revocation passed. No product repair in this slice.

## Observed results

| Case | Actual, both runs | Verdict / limit |
| --- | --- | --- |
| Disabled Admin's original access token | session403 | Existing inactive-user access gate works |
| Disabled Admin login with correct password | login200 / issued token pair; resulting session403 | FAIL: disabled identity can obtain credentials, not demonstrated access to protected data |
| Disabled Admin's existing refresh credential, no Authorization header | refresh200 / issued pair; resulting session403 | FAIL: disabled identity can renew credentials |
| Wrong-current-password change |400; stamp unchanged; old access/refresh200 | Correct rejected-change control |
| Native successful Teacher password change |200 / success message; persisted stamp rotated | Correct change control |
| Teacher's pre-change access token after stamp rotation | session200 | FAIL: access credential remains accepted despite successful password change |
| Teacher's pre-change refresh / old-password login |401 /401 | Correct revocation/credential controls |
| New-password login / new access / new refresh |200 /200 /200 | New session remains usable |
| Deleted synthetic Admin's access / refresh / login | session401 /refresh401 /login401 | Correct endpoint-specific denials; not universal revocation |
| Deleted Admin's old access at protected GET /api/academies |200 | FAIL: authentication still accepts deleted identity outside session controller. List controller's null-user fallback is an empty array; this diagnostic asserts status, not response content or a data leak |
| Unrelated active account during each scenario |200 | No indiscriminate global revocation observed |
| Re-enabled Admin's original access token |200 | Observation only; reactivation revocation policy is not accepted/decided here |

Actual native production Identity login, refresh, password-change and protected controller paths run under the existing guarded Testing factory with SQL Server2022. Tokens, password hashes and security stamps stay in memory; no secrets are written to evidence. Disabling/re-enabling and deletion use scoped UserManager only on the synthetic fixture; this is NOT browser/platform-disable-action acceptance. Successful password change uses the real HTTP controller. No fake authentication, expiry rewrites, clocks/lifetime changes, production DB or Azure credentials.

Fresh-context SQL checks confirm IsActive persistence, no stamp rotation from the fixture toggle/rejected password change, successful password-change stamp rotation, deleted row absent, original final identity counts/role memberships/academy/typed links/active flags preserved and unchanged application audit count. These compare the listed associations/counts, not all database columns/tables. Password/stamp mutation is intentional for the Teacher fixture. No UI, cookie, role-removal, lockout, private-media ticket, physical-device, cross-tab, concurrency or full critical-suite acceptance is inferred.

## Cause and bounded next repair

Application middleware checks only `user?.IsActive == false`: missing user and stale security stamp are not rejected there. AuthSession.Current independently rejects missing user, while Academies.List returns its empty fallback. Anonymous login/refresh are not covered by authenticated-only inactive checks. Local account-disable controllers persist IsActive without rotating the stamp.

Matched package source10.0.12: Identity refresh validates expiration and the security stamp, then creates a fresh principal; it does not evaluate AcademyDesk's custom IsActive. Login delegates to SignInManager. [Official Identity endpoint source](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.12/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs). The opaque bearer handler validates token protection/expiry and accepts the ticket without a database stamp/user lookup. [Official bearer handler source](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.12/src/Security/Authentication/BearerToken/src/BearerTokenHandler.cs). These sources explain the native observations; this is an application integration gap, not a claim that the framework promised custom account revocation.

Next bounded implementation: fail closed for missing/stale identities on authenticated requests, deny inactive credential issuance/refresh using Identity extension points with native behavior retained, then rerun these original failures and targeted active/expiry/tenant/role/private-file controls. Preserve Identity cookie/2FA/lockout/stamp/expiry semantics, existing roles/module filters, access8h/refresh30days contracts and multi-workspace helper behavior; no wholesale custom authentication rewrite. Decide and record password-change UI session recovery and deactivate/reactivate credential policy before closing those broader gates. Sol High remains the agreed model for this targeted repair; Astra review is for a concrete security-policy/design ambiguity, not a repeat audit.

## Runs, preservation and cleanup

- HEAD unchanged:20bb6047f9edf733ac8e2a226621cc582ec54b3c.883 retained predecessor pins; only QA dispatcher/runner and declared tracker successors changed among pinned files. New diagnostic/report/issue/evidence/validator are QA-only. All application code, both schemas/migrations, factory/guard and accepted predecessor evidence/binaries remain unchanged. Prior57 helper/16 UI/1032 backend results retained, not rerun/recounted.
- Build passed,0 warnings/errors,95.46s; new isolated `.build-check/session-revocation-sql`, no previous artifact overwritten.
- First run:e924636900a04e6e89edb66d2d1932bc, loopback SQL62261, started14:38:22.5433838Z, wrapper53.8s, exit0.
- Repeat:2b518ef159a8472588c69037a65b2f55, loopback SQL62280, started14:39:38.5749481Z, wrapper40.3s, exit0. Same30 observations, same four failures.
- Both native inventories313 routes,82 application/7 Identity migrations, same endpoint digest. Strict negative mismatched-marker cleanup refused. Guarded SQL DB/login and labelled containers/run-owned roots removed; independent inspection confirms neither root/container exists and zero SQL listeners. Only disposable synthetic QA data removed, not recoverable through these fixtures; evidence/source/binaries retained.
- Prior interrupted provisioning fixture remains stopped/retained after the earlier tool cleanup block; no deletion workaround or retry. Normal dev services/databases and Azure unchanged. No commit/push/deploy. Phase2B/release OPEN.

[First native log](../EVIDENCE/logs/phase-2b-session-revocation-sql.log), [fresh repeat](../EVIDENCE/logs/phase-2b-session-revocation-repeat-sql.log), [build](../EVIDENCE/logs/phase-2b-session-revocation-build.log), [cleanup](../EVIDENCE/session-revocation-cleanup.json), [before manifest](../EVIDENCE/session-revocation-before.json), [source receipt](../EVIDENCE/session-revocation-source-receipt.json), [diagnostic](../tools/SqlHarness/SessionRevocationDiagnostic.cs), [validator](../tools/validate-session-revocation.cjs), [issue](../ISSUES/BUG-AUTH-REVOCATION-001.md).
