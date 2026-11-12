# Protected enrollment browser contract on SQL — 12 November 2026

Desktop base `264c1f5c3ba9960c610054369330f3299868b26a`.
Six source/test files and the original C38 handoff were copied from
Cloud `e8385f58b23de757928f99481c33ae3dd582b851` and verified against
their Git blobs. The legacy enrollment fixtures retain their original checks.

The new native tests run the browser API contract through real Identity,
HTTP and disposable SQL. Readiness is side-effect-free. Start requires a
current permitted actor and exact request key. A successful start records
one purpose-bound conversation and start audit. Recovery reads the same
conversation. The prepare, authorize and execute steps use the existing
enrollment admission service and require current scope.

## Verification

- API and harness builds passed with zero warnings and errors.
- 153 focused API tests passed.
- 15 new native test groups passed. They cover flag and authority refusals,
  missing start, same-key start races, recovery, purpose and actor binding,
  version tampering, expiry, daily quotas, current role revocation, exact
  Active and Waitlisted admission, stale capacity, and required audit rollback.
- Concurrent execution returned one creation and one replay, with an
  identical receipt, one enrollment and one audit. GET recovered that receipt
  without changing SQL state.
- 109 retained native test groups passed across admission, preview, create
  and lifecycle. This includes 15 mixed Manual/service races.
- Disposable SQL run `a516f50b15f54c6daf43a53bf0958acc`, window `71b1c5f1f3534d9a90acb0850ad2dd1e`.
  The runner removed its owned SQL resources, restored both PENTA services,
  and verified HTTP 200 and the original Docker inventory.

The first window reached the expiry case and returned HTTP 401 because
the fixture had advanced the bearer clock along with the conversation
clock. The retest obtained fresh bearer credentials at that time and
verified HTTP 410 from the conversation. The C38 application source was
unchanged by that fixture correction.

Cloud C39 owns the browser approval and recovery UI. At
`29c12181c8e6ed93921c6c93faa5a87b02f9a108` it has published a browser
UTC serialization change. The UI still needs real
browser acceptance with this contract, including interruption before
dispatch and after commit. Production remains blocked by six hard gates.
