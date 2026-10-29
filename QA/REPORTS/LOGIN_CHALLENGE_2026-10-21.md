# Native challenge sign-in UI —21 October2026

Academy `D:\AcademyDesk-codex-p0`,branch `codex/penta-search`,HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4` unchanged. Sol High.
This extends the existing authentication acceptance program; not a new plan.

## Delivered

The real login form now handles Identity's exact `401 / RequiresTwoFactor`
challenge. It offers authenticator or unused recovery-code entry, sends only the
selected native proof field, and never treats the challenge as authentication.
No token save/profile read occurs until native login succeeds with a valid token
shape. The authoritative profile must still pass existing workspace validation.

Same-tick duplicate submissions are guarded through the whole login/profile
flight. Invalid/missing tokens reject before a profile request. A429 gives clear
wait guidance; failures expose no raw server details and do not clear other
workspace sessions. Errors are accessible alerts. Challenge switching clears the
previous proof; code clears after successful sign-in. No code/password is added
to URLs, application storage, analytics or QA receipts. Existing password and
scoped AI-first landing behavior are retained. This is sign-in, not MFA enrollment
or a new security/role/lockout policy.

Native request fields/challenge matched to the installed framework's primary
[ASP.NET Core10.0.12 source](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.12/src/Identity/Core/src/IdentityApiEndpointRouteBuilderExtensions.cs).

## Accepted checks

- New actual-handler/render regression:before10/22 PASS,12FAIL;after22/22PASS.
  Retained `.build-check/session-resume/login-before.tsx` and `login-baseline.log`.
- Combined session/login/workspace regressions **172/172PASS**, zero failures,
  skips/cancellations. `.build-check/session-resume/combined-tests.log`.
  The prior130 is a subset, not another130unique cases.
- TypeScript/webpack export PASS,87/87outputs. Targeted lint zero errors;one
  existing shell-image warning remains, not suppressed.
- **28/28 actual exported React browser journeys PASS**:320/1440×light/dark×
  authenticator/recovery/invalid/429/network/malformedtokens/missingproof.
  No automatic retry;no tokens on failed/challenged sign-in;other Admin session
  preserved;successful synthetic Teacher routing to AI entry;no page errors or
  horizontal overflow. Mobile dark authenticator/light recovery screenshots
  inspected. `QA/EVIDENCE/login-challenge-1792560329849/result.json`.
- Browser transport and credentials are synthetic, not linked SQL/Identity,
  real authenticator hardware,2FA enrollment or physical Android/iOS acceptance.

## Native SQL follow-up —bounded PASS

Final successor run `1b8e69db5e7a4795928b710f0a83b65d` PASS,exit0,116.7s
including61seconds of normal rate-window pacing. **61new assertions/34selected
HTTP requests**,existing36native revocation cases,tenant/private-file controls
PASS.102application/7Identitymigrations,360routes/349controller method-routes/
10Identityroutes;both resolved contexts match the exact owned target/runtime login.
Actual MFA setup/current proof,missing/invalid proof,recovery login/single-use
replay,partial-cookie-only401,inactive account issuance/refresh/access denials,
five-attempt lockout and post-expiry recovery verified against fresh SQL/stores.
Ordered original identity/membership/token and selected domain/audit rows unchanged.

Lockout-only existing bearer access200 is an **observation of retained policy**,
not approval of universal session revocation. No stamp/policy change was invented.
The row-specific synthetic expiry setup is not real wall-clock timeout acceptance.
Required wrong-marker cleanup refused;exact successful-run database/login/container
removed. `.build-check/identity-challenge-sql/paced-run.log` is final evidence.
Earlier NOT ACCEPTED/RAM-blocked notices below are historical,not the final result.

Added opt-in `QA_IDENTITY_CHALLENGE_CHECK=1` only with the existing
`SessionRevocationRepair` module. Wrapper rejects wrong module/value before SQL
creation;runner independently validates it. Existing private-file and36native
revocation checks remain before the additive challenge checks,unchanged.

`IdentityChallengeRegression.cs` uses fresh synthetic accounts,real native
Identity endpoints/current lockout options,authenticator proof/recovery replay,
inactive-account denials and ordered fresh SQL/store preservation checks.
Secrets are compared in memory,never printed. It does not change app policy,
stamp checks or rate limits. Isolated build passes0warnings/errors with existing
cached packages only. No new dependency download.

Three incomplete native runs are retained,**not passed**:

1. `c28391c5dce54634a556b04f7d6ada97`:48s,original36native checks PASS;first MFA
   key creation correctly invalidates old access. Fixture incorrectly reused it
   for an expected400validation check and received401. Corrected to assert stale
   access401 and perform a new native login,not weaken host validation.
2. `3154daa981764883b9963e634262f019`:45.2s,original36PASS;key/setup/proof/SQL
   checks proceeded,then fixture incorrectly prohibited every Set-Cookie header
   on401. Native Identity can issue a **partial two-factor challenge cookie**.
   Corrected oracle permits only the configured partial scheme cookie and then
   requires that cookie alone receive401 on protected session;completed-session
   cookies remain forbidden. Tests use a cookie-disabled client for explicit
   request isolation. At that checkpoint the corrected source built but had no
   native pass yet. Logs `initial-run.log`,`corrected-run.log` in
   `.build-check/identity-challenge-sql` are retained.

3. `e1f16f4e7a6d4d2ca8dae7945f18815e`:41.5s;MFA/recovery/inactive checks
   progressed,then the combined retained+new suite reached the normal120/minute
   limiter,returning429 on the locked-password check. Retained `rate-limited-run.log`.
   Final successor waits61seconds **before** its additive challenge slice;no limiter
   override/IP spoof/request retry. All final assertions stay strict.

All three exact run-owned containers were stopped after label/name/loopback ownership
checks and retained with their synthetic databases/evidence for investigation.
No failed teardown was called successful. No original container/volume removed.
After releasing only the owned idle compiler server,an intermediate retry was blocked
before container creation by the unchanged3GiB host-RAM guard. No Docker restart,
pruning,unrelated process termination or guard relaxation.

Reproduce only after memory gate passes:
`dotnet build QA/tools/SqlHarness/SqlHarness.csproj --no-restore --artifacts-path .build-check/identity-challenge-sql`
then set the exact opt-in and invoke existing
`pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module SessionRevocationRepair`.
Restore prior environment afterward. Original session/role/physical/device/
enterprise/release gates and BUG-DATA-0001 remain OPEN. No model/training/default,
main/stage/commit/push/Azure/production operation.
