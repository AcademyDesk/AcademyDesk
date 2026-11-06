# Current built UI and disposable Identity/SQL session browser acceptance

Status: PASS for the bounded current-source session journey; claimed before implementation, 6 November 2026. Continue from C6 source
checkpoint `2e8c13ababfa4852ddb1c05c09d4ce2800a7e092`; do not restart the audit.

Scope: add a current-source QA browser client alongside the preserved historical
`session-browser-acceptance.cjs`. Use the existing unchanged BrowserSession SQL
harness, real UI login, native API responses and the exact-version built static
artifact. Retain active Admin login, Owner login, UI deactivation/readback, revoked
Admin access/login, rejected password retention, successful password change and
fresh login/return path checks. No fake responses or injected session tokens.

The test must bind the owned run marker, loopback origins, current application
source and artifact manifest. Stop its exact disposable host, remove only its own
SQL container and restore the exact two original PENTA services even on failure.
Retain the 3 GiB admission guard and all failed receipts. No main, Azure, customer
database, new dependency, application authorization change or model experiment.

This does not close C6's separate financial approval/Teacher assigned-batch SQL
roundtrips, physical device gates, remaining role/workflow or model qualification.
Sol High for this live authentication/session integration; Medium for settled docs.


## Source-bound acceptance

Successful local window `85efb14276ac4387927bb3ab2c158133`, actual SQL run
`4c47b6fc1051421bb0b5c3d4cd25034b`, at source
`2e8c13ababfa4852ddb1c05c09d4ce2800a7e092`:

- **11 real browser checks PASS**, 51 observed native API responses, no HTTP 429,
  zero uncaught page errors. Edge 156.0.4314.58, sandbox enabled, 1440x1000 desktop.
- Admin UI login lands at `/penta`; Owner UI login lands at `/platform/ai`.
  Manual control pages remain usable. This proves entry, not AI tool activation.
- Owner deactivation shows success, Deactivated state and Activate action; these
  persist after UI reload. The pre-existing Admin browser receives native session
  403; another login receives native 401 and visible stable error.
- Password mismatch sends no POST. Incorrect current password receives 400 and
  retains the form/session. A confirmed change receives 200, removes the Platform
  pair and returns to login with an accessible success notice and empty password.
  The new password logs in through the UI and returns to Platform control.
- SQL independently verifies inactive Identity state, exactly one attributed
  platform audit, password security-stamp rotation, old access/refresh/password
  rejection, unrelated Owner access and preserved original user/role counts.
  Seven additional native revocation controls pass alongside the browser journey.
- Runtime inventories bind both DbContexts to the exact owned SQL login/target;
  two actual tenant logins retain own reads and deny foreign GET/POST without a
  cross-tenant write. Application migrations 85 / Identity migrations 7.
- The 658 built artifact hashes and three current app-source hashes are bound in
  the per-run proof. Existing compiled harness reused only after unchanged backend,
  API-test and harness source checks. DLL SHA256:
  `ca9c0075f5fbef799a92407175a506f9350f499bd287ff4298d8790faa069ea6`.
- Browser requests only continue to the two explicit loopback origins. No response
  mock, token injection, customer database, current model request or downloaded
  browser/package is involved. Six invalid-origin preflights fail before launch.

Actual SQL run-owned database/login and exact container removed. Full Docker
container/volume/network/image inventory unchanged; exact two original PENTA
service IDs/images restored, API health 200, lease released. Original main and dirty
working trees remain preserved. No Azure/model/production activation occurred.
Two synthetic-data screenshots (deactivation and password-success login) were
visually inspected; screenshots and raw receipts stay local in ignored output.

## Retained setup failure

First window `7d33b801cfc84a8ca66ad4424a26b465`, SQL
`b2bd0b6291c541d484a1107c8c92a4f8`, stopped before the browser: Docker required
setting the owned container's swap limit along with its memory limit. Cleanup's
short/full container-ID comparison also refused removal. Both failures remain in
the original receipt. A separately recorded exact-ID/label/name/loopback ownership
check removed only that owned container and proved full inventory recovery.
The private driver now sets both limits and requests full IDs. The fresh successor
passed all checks with a 2 GiB / two-CPU SQL container and no cleanup errors. No
application code, harness assertion, authorization rule, RAM guard, test skip or
machine privilege was changed to pass this run.

## Reproduction contract and remaining work

`QA/tools/session-browser-current.cjs` is a new configurable successor. The old
historical driver is untouched. A coordinator must first acquire the existing
exclusive local validation lease, retain the 3 GiB start guard, serve the verified
static export and start the unchanged `-Module BrowserSession` harness. Then set:

- `QA_SESSION_BROWSER_ORIGIN`, `QA_SESSION_BROWSER_API_ORIGIN`: distinct canonical
  unprivileged HTTP 127.0.0.1 origins, matching compiled API origin and SQL CORS.
- `QA_SESSION_BROWSER_RUN`, `QA_SESSION_BROWSER_SQL_LOG`,
  `QA_SESSION_BROWSER_FIXTURE_ROOT`: exact fresh READY run/log and owned marker.
- `QA_SESSION_BROWSER_OUTPUT`: a new empty owned local evidence directory.
- `QA_SESSION_BROWSER_PROOF`: JSON binding run/origins/current source SHA, the
  three source hashes, source/export roots, every artifact path/hash, and
  `responsesMocked: false`.
- `QA_SESSION_BROWSER_PLAYWRIGHT`, `QA_BROWSER_EXECUTABLE`: already installed
  local Playwright module and Chromium/Edge executable; no implicit downloads.

Run `node QA/tools/session-browser-current.cjs`. Signal the exact owned stop marker
in a coordinator finally block, await native SQL acceptance/cleanup, remove only
an ownership-verified remaining container on failure and restore original services.
Do not reuse a mutated fixture or substitute a synthetic transport for this gate.

This packet closes only the named local session journey on this source/artifact.
It does not close every authentication issue or release gate. Physical devices,
other roles/lifetimes/cross-tab adversarial cases, C6 finance/Teacher SQL roundtrips,
C7 compensation SQL, current images/CI/audits, privacy/restore/staging and model
qualification remain open. Mini/Plus/Pro are unqualified; Ultra deferred. The
owner's stop before candidate weight loading/training/activation remains in force.
Next: integrate the disjoint returned C7 compensation QA packet, then remaining
live role/domain acceptance. Sol High for security/SQL; Medium for settled QA/docs.
