# Cross-tab session acceptance with live Identity and SQL

Desktop window `766d19effa4a44e4957a49a66f77e55c`, SQL run
`630db06a18434bd9b30edf2f7925c2a5`, tested application source
`9c9658132446124b70c53467e940d9769cf97d44`. The exact web tree matched the
C22 export source, and all 658 artifact hashes matched the retained manifest.
The compiled harness and its relevant source matched the recorded prior
artifact. Only QA and handoff files changed for this session batch.

**15 real Edge browser checks passed** across 64 actual HTTP responses,
without rate limits or uncaught page errors. The test used normal UI logins,
the existing loopback bridge, real ASP.NET Identity and disposable SQL.
The browser used no injected tokens, mocked responses or synthetic storage events.

| Added case | Observed result |
| --- | --- |
| Manual Admin sign-out | The open PENTA tab received a trusted storage event, lost its Admin pair, hid protected workflows and showed Session ended without a reload. |
| Peer login | A fresh login in the Manual tab published a new login epoch. That tab could use its PENTA entry while the old PENTA tab stayed signed out. |
| Explicit recovery | Sign in again opened the login page in the old tab. After native login, the verified PENTA controls returned. |
| Platform password change | The other Platform AI tab showed Session ended and lost its Platform pair after the native password change. SQL verified security-stamp rotation, and the old access, refresh and password were rejected. |

The original 11 browser checks passed in the same window. Native SQL also
confirmed the Admin was inactive, there was exactly one attributed deactivation
audit, old access returned 403, login and refresh returned 401, and the unrelated
owner retained access. Original user and role counts were preserved.
The existing controlled logout and refresh-lock suites separately passed 77/77.

The first attempt timed out in readiness around a system-clock change. The
runner switched to a monotonic deadline and waited for a complete readiness
line. The second proved token removal and hidden controls but expected the
entry-specific sign-in heading. The shared boundary intentionally shows
Session ended and stays locked after a peer login. The final assertions
cover that behavior and the explicit recovery action. All windows restored
the two original PENTA services, preserved Docker inventory and left the PENTA
health endpoint at 200. Raw records remain local.

This is scoped desktop evidence for `SECURITY-SESSION-001`. Remaining role
journeys, physical devices, live overlapping refresh with real Identity, broader
critical workflows, exact hosted CI and staging acceptance stay open. The
release guard stays blocked.
