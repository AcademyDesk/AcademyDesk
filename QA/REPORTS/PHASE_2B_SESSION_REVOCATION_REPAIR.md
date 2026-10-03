# Phase 2B — targeted authentication revocation repair

2026-10-02. **LOCAL BOUNDED PASS; broader SECURITY-SESSION-001 and release OPEN.** The four previously reproduced failures are repaired. One fresh owned SQL fixture passed 36 strict native session observations with zero policy gaps. These 36 include additional controls and exclude preflight, tenant and private-material requests. The original two-run diagnostic and its binaries remain historical evidence.

## Result and implementation

- Inactive native password login and refresh now return 401. Inactive original access remains 403. Native cookie login also rejects the inactive account without issuing a cookie.
- Successful native password change rotates the existing Identity stamp. Previous access and cookie sessions now return 401; old refresh and old-password login are rejected. New-password login, access and refresh succeed. Rejected password changes preserve the old session and stamp.
- Deleted-account tokens now receive 401 on both session and protected academy-list routes. No data leak was claimed by the original diagnostic.
- Active/unrelated sessions, expired access/refresh rejection, malformed refresh rejection, teacher denial on an admin route, tenant separation, and private-resource access controls pass.

`AcademySignInManager` adds IsActive checks through CanSignInAsync and the user/stamp validation overload, delegating the existing framework checks to the base implementation. Registration replaces the SignInManager service; native Identity endpoints are retained. The authenticated middleware checks missing identities and stale stamps on each request, keeping inactive-access 403. No token lifetime, schema, migration, role/module permission or private-download handler change.

The native refresh endpoint uses SignInManager stamp validation; native password and two-factor sign-in use its existing pre-sign-in check. Primary sources matched to package 10.0.12: [Identity endpoints](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.12/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs), [SignInManager](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.12/src/Identity/Core/src/SignInManager.cs). Password, confirmation, lockout and 2FA implementations remain inherited; exhaustive 2FA/lockout acceptance is not inferred.

The Platform password-change form now captures the form before awaiting, preserves inputs/session on failure, resets only on success, clears Platform credentials, and replaces the route with the login page showing an accessible new-password notice. It avoids the old protected reload with already-revoked credentials. Five controlled actual-handler/render checks pass; this is not browser acceptance. Other workspace tokens are retained locally and any stale credentials are rejected by the backend.

## Actual verification

| Check | Result |
| --- | --- |
| Isolated SQL harness/API build | PASS, 0 warnings/errors, 39.50s |
| Native SQL session cases | 36/36 PASS, original four gaps now rejected |
| Real HTTP preflight and two-tenant controls | PASS; cross-tenant reads/writes rejected, no forbidden SQL write |
| Existing private-material HTTP/SQL regression invoked before session cases | Completed successfully; authorized exact bytes, anonymous/cross-tenant GET and Range denials recorded; existing role/entity/recipient controls also executed |
| Backend test suite on changed API | 1,032/1,032 PASS, 0 skipped |
| Handler checks | 42/42 PASS: 5 new password-recovery checks plus 37 existing refresh checks |
| TypeScript | PASS, exit 0 |
| SQL preservation inside session slice | Original identity/role/tenant/typed links/active flags/counts preserved, synthetic deleted row absent, audit count unchanged; Teacher password/stamp deliberately changed |

Fresh run `ff81333c6f8945df801f69fb17e0fd17`, loopback SQL port 61926, wrapper started 2026-10-02T14:52:17Z and completed exit 0 in 75.1s. Native endpoint inventory remains 313 routes, application/Identity migrations 82/7, endpoint digest unchanged. Private-file fixture creation precedes the session preservation snapshot; that snapshot does not claim no application writes in the entire run. The wrapper preserves selected private-file summary lines, not every media-case line; the unchanged private regression throws on unexpected observations before reaching the session checks.

Guarded cleanup refused the negative mismatched-marker case, then removed the owned database/login/container/root. Independent inspection: container absent, root absent, zero listeners at 61926. Older blocked browser fixture remains stopped/retained; no deletion retry. Normal development services/databases and Azure unchanged. HEAD remains 20bb6047f9edf733ac8e2a226621cc582ec54b3c; no commit, push or deployment.

## Boundaries and next step

This fixes the four observed server integration gaps. Immediate account/stamp revalidation adds a stamp comparison to the existing per-request account lookup. Native access 8h and refresh 30d contracts remain asserted. No revocation cache is introduced.

Deactivation/reactivation retains existing behavior: disabling blocks issuance and access while inactive, but re-enabling without rotating the stamp makes pre-disable credentials usable again. The fixture confirms this observation; it is not acceptance of a permanent revocation policy. Platform-disable UI actions, password-change browser recovery, cross-tab behavior, exhaustive cookie/2FA/lockout flows, download-ticket breadth, staging headers and full critical journeys remain open.

Next bounded task: real browser password-change recovery plus platform account-disable action, with durable failure/success feedback and native/SQL confirmation. Any permanent revoke-on-disable policy decision should be explicit before changing reactivation behavior. Continue with agreed Sol High.

[Native log](../EVIDENCE/logs/phase-2b-session-revocation-repair-sql.log), [backend](../EVIDENCE/logs/phase-2b-session-revocation-repair-backend.log), [handlers](../EVIDENCE/logs/phase-2b-session-revocation-repair-handlers.log), [build](../EVIDENCE/logs/phase-2b-session-revocation-repair-build.log), [types](../EVIDENCE/logs/phase-2b-session-revocation-repair-types.log), [cleanup](../EVIDENCE/session-revocation-repair-cleanup.json), [before](../EVIDENCE/session-revocation-repair-before.json), [receipt](../EVIDENCE/session-revocation-repair-source-receipt.json), [strict regression](../tools/SqlHarness/SessionRevocationRepairRegression.cs), [UI checks](../tools/session-revocation-repair-ui.test.cjs), [predecessor diagnostic](PHASE_2B_SESSION_REVOCATION_DIAGNOSTIC.md).
