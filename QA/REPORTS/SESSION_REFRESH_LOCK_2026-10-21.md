# Cross-tab refresh coordination — 21 October 2026

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Continues the accepted session queue;
does not replace the enterprise program or qualify PENTA models.

## Bounded implementation

Actual `api.ts` now uses a per-workspace native Web Lock for explicit epoch-bearing
sessions when available. A waiting tab rechecks the login epoch/generation and
credential pair after acquiring the lock. It reuses a successful peer rotation
within that same epoch rather than submitting the former refresh token again.
Late original401 responses also use the coordination path. Lock names contain
only the workspace, never credentials, actor IDs or tenant data.

Queue acquisition has a10-second bound, removed when granted. Cancellation removes
the waiting request, not the other tab's lock/flight. Failed lock acquisition fails
closed, never falls back to an unlocked duplicate. Browser closure releases native
locks. Unsupported browsers and legacy epoch-less sessions retain the old same-realm
path. Legacy peer rotations are not inferred to be the same account. Server remains
authoritative for every replay; no403/409/429/500 retry and no replay401 loop.

**Not an atomic login/logout/refresh storage transaction.** Explicit login/signout
and external token writers are not participants in this lock. Their narrow
check/write interleaving remains OPEN. A pending response whose replacement has
already completed is still rejected by the retained epoch fence. A hostile
same-origin script is outside this UI marker/lock boundary. No server revocation,
physical mobile/frozen/BFCache, browser-version breadth or full release acceptance.
Denied cross-tab refreshes serialize but may each return401; only successful
same-epoch renewal is coalesced. A granted, hung network refresh is not bounded by
the queue timer; waiters fail closed after their own acquisition timeout.

No production backend/schema/token lifetime/permission/model changes. Manual and
AI share the existing helper; this is auth plumbing, not new conversational actions.

## Evidence

- Old source: four selected two-realm cases all FAIL with two simultaneous refreshes
  instead of one. Retained `.build-check/session-resume/session-cross-tab-red.log`.
- **219 distinct focused checks PASS**,31new +188retained. Separate real helper
  realms/synthetic shared storage/lock adapter. Covers four workspaces, delayed401,
  identical-pair replacement/logout, workspace separation, pending/owner abort,
  rejection/timeout, retry, denied renewal and replay401, no unsupported status retry,
  unsupported API and legacy behavior. A newly written legacy assertion initially
  expected two successes; corrected to the existing200/401 pair after demonstrating
  the retained helper's token-pair fence. No original assertion weakened.
- **29 native Edge/browser Web Locks probe contexts PASS**, all four workspaces:
  overlap, late401, queued abort, holder-tab close, replacement, workspace separation,
  unsupported API; one additional actual10-second queue timeout/recovery. Actual
  helper, minimal QA probe HTML and intercepted synthetic APIs, NOT React/SQL proof.
  Initial28-case run preceded final guard refinements; final29 only counted here.
- Retained **24 exported React/login/native StorageEvent cases PASS**, plus
  **80 exported resume cases PASS**.104distinct exported contexts, not208 by adding
  predecessor runs. The first resume run stopped19pass/1fail: server-rendered export
  cards were treated as ready before initial Reports/shell effects finished. Runner
  now waits for actual Reports loaded state and resolved synthetic account before
  measuring resume-only traffic. Same no-extra-read assertion retained; diagnostics
  now show offending paths. Failed receipt retained, not accepted as a pass.
- **Full lint0errors/0warnings, TypeScript/webpack87-route export PASS** on final
  production helper. No lint suppressions/config/dependency changes.
- Harness build0warnings/0errors,6.22s, cached assets/no downloads.
- Native SQL run **`cba591c5e6784d8aab88db30386f9c16` PASS**,exit0/53.3s:
  **25retained native HTTP requests/four same-realm groups** unchanged, exactly one
  Student user/membership/typed academy link/success audit; PLUS **22native browser
  HTTP requests/five additive groups** using actual helper+native Edge locks+real
  Identity/SQL responses through the owned loopback bridge:

| Group | Requests | Refreshes | Result |
| --- | ---: | ---: | --- |
| Admin overlap |5|1|both200 after renewal|
| Teacher overlap |5|1|both200 after renewal|
| Admin late401 |5|1|both200; peer receipt reused|
| Teacher expired refresh |4|2|both401, zero replays|
| Admin completed replacement |3|1|both401, zero replays|

47native requests total for these clients, not including fixture/control logins.
No429, retry relaxation or forged browser response. Native tests use minimal probe
HTML, NOT native login React UI/physical-device acceptance. Real session response
workspace/academy validated; no credentials/profile PII persisted in receipts.
Original C# SQL assertions and45-second client bound unchanged. JS add-on is opt-in
`QA_REFRESH_LOCK_BROWSER=1`; runner rejects wrong module/value before SQL creation.
The runner initially filtered the REFRESHLOCK log prefix; receipt5/5 and exit0
independently verified, then additive log forwarding fixed. No SQL rerun needed for
that output-only fix. First Windows PowerShell invocation failed execution policy
before creating SQL; bundled pwsh ran under its existing policy, no policy changed.

Runtime inventory360/exact scoped DbContexts/login, health/login/tenant controls,
102application+7Identity migrations, negative cleanup-marker rejection and owned
database/login/container removal PASS. Available RAM guard>=3GiB passed. Container
`academydesk-qa-cba591c5e6784d8aab88db30386f9c16` independently absent;29containers/
41volumes remain, only original two PENTA services running. No restart/prune.

## Receipts and pins

All logs `.build-check/session-resume/session-cross-tab-*`; local evidence untracked.

| Receipt | SHA256 |
| --- | --- |
| `QA/EVIDENCE/session-cross-tab-1792605938165/result.json` | `18D7AA46C9CD9864BAD72FE852BB25B352F2477FABC315CB2D887928B98D365F` |
| `QA/EVIDENCE/session-cross-tab-native-1792606140154/result.json` | `C18AE60C086497BAC09246BEBAD3AE29831189E0625C199CC1403CE95587D03F` |
| `QA/EVIDENCE/session-epoch-1792606035015/result.json` | `C6540F5B093D1DF2DDF19FB5BC697DE42C10072A90BEA8617FC6035259D3B6CB` |
| `QA/EVIDENCE/session-resume-1792606158443/result.json` | `A9019FD8CFDEB8E9EB205D2E9D7D99AFCB911D7C0875AD758CEC12DC3CCFA5EA` |

Final helper SHA256`22CB3F56BFE61F9595CD5ABAD918988D4DC9E24F2B757C0889CE67D4A9A8586A`.
W3C Web Locks contract consulted: https://www.w3.org/TR/web-locks/ . Cooperative
same-storage-bucket locking is not server authorization or cross-device coordination.

## Next / unchanged gates

Stay High for consequential host/privacy/session gates; Medium for settled UI/docs.
Next inspect PENTA operation-channel ownership against the new explicit-login marker;
do not infer that a mounted shell fence independently secures every detached channel.
BUG-AUTH-REFRESH-001/BUG-AUTH-REVOCATION-001 and SECURITY-SESSION-001 remain OPEN at
broader acceptance. BUG-DATA-0001 and11broad delivery packages remain OPEN.
Model fresh6/12/exposed7/12 NOT ACCEPTED;76data candidates pending/zero eligible,
independent reviewer designation/review and scoped experiment authority still missing.
No commit/stage/push/main/Mini/Azure/acquisition/training/activation/deployment.
