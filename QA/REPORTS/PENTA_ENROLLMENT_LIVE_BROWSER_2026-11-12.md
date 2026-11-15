# Live PENTA enrollment approval and recovery — 12 November 2026

Desktop base `83452a129c1a7315922dbeeaf1b50de3d62cbdac`.
Fifteen enrollment paths match Cloud C39
`94a4f99678abea7b23c050988952e2fa347fab94`, including C37 search and
eligibility and the browser UTC correction. The desktop workspace frame
adds the enrollment route to its existing standalone pages.

## Verified on the laptop

- 195 targeted UI and role-entry checks passed. Next.js, TypeScript,
  static export, targeted lint and the API/harness builds passed.
- 68 focused API tests and 124 native HTTP/SQL test groups passed. The
  latter include 15 browser-host groups and 109 retained enrollment groups.
- Six live browser groups passed. An Academy Admin created an Active
  enrollment at 390 px. An Owner created a Waitlisted enrollment at 1440 px.
- The test dropped prepare and approval responses after save, then dropped
  the execution request before dispatch. Reload recovered the authorized
  operation by GET. An explicit retry sent the same request and the test
  dropped its response after commit. Reload returned the original receipt.
- SQL confirmed exactly two new enrollments, each with one actor-bound
  session and draft and one audit per transition. The recovery pointer
  contains identifiers only and clears when the user selects Done.

The first isolated UI run lacked the shared test helper. The next browser
run stopped at HTTP 429 because the preceding quota test had seeded the
same disposable academy. The final retry reused the verified frontend build
after matching all 197 web source hashes and removed only the 100 synthetic
quota rows after checking the quota. No threshold or product rule changed.

The final run `635d9843d6aa4811923808673462cc91` in window
`2438a19516684f47b86dea876818540b` removed the owned SQL resources
and restored both PENTA services with the original inventory and HTTP 200.

C40/C41 status and transfer, physical-device acceptance and model
qualification remain open. The production checker still lists six blocked
records.
