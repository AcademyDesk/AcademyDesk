# PENTA Platform Owner conversation

15 November 2026 · Windows desktop · `codex/penta-host-integration-20261030`

The Platform Owner workspace supports three guided reads. Health verifies
the API, linked SQL and current maintenance setting. Onboarding shows
aggregate profile counts, and support shows open and high-priority counts.
The response is bounded to the requested summary. It contains no tenant
contacts, support messages or learner records. The user can open Manual
Platform Control for the underlying permitted work.

The Testing-only, default-off endpoints require the current enabled
Identity account and its authoritative Platform Owner flag. Each successful
answer and its actor audit commit in the same SQL transaction. Readiness
performs no writes. The browser validates private responses, clears
superseded results and reacts to session changes.

## Validation

| Check | Result |
| --- | --- |
| Next.js production build, TypeScript and postbuild | Passed using 203 verified frontend inputs and 682 static files. |
| .NET API and SQL harness builds | Passed. |
| Platform and learner client tests | 8 passed. |
| Native Identity, HTTP and SQL | 55 checks passed, comprising 51 retained platform-billing checks and 4 new status checks. Anonymous, Academy Admin and a role label without the Owner flag could not access platform data. |
| Live browser | Three groups passed at 390px for health, onboarding and support. An unsupported learner-data request gave guidance and cleared the prior result. SQL held three actor audits and unchanged onboarding and support records. |
| Existing role-entry browser regression | 24 groups passed with intercepted test responses in Chrome, including Platform fallback at 390px and 1280px. The harness now records evidence before profile cleanup and ignores the exact late CDP response after a deliberate tab close. |
| SQL cleanup | Removed the run-owned SQL container, database and login. Restored both original PENTA services and verified HTTP 200. |

The first role-entry attempts timed out starting Edge and left temporary test
profiles on D:. The execution tool blocked removal under its cleanup policy.
The Chrome retry completed the browser cases and its own profile cleanup.

Validation window `9200ad933416414a8bb43cf5ed788367`, SQL run
`7a958a964cc14325bb3b8da826662efb`, harness SHA-256
`5387665ef2cd2fd3808480697d526d72f8128cc955c9aa0966912c30e53e72a9`.

The four portals now have a verified guided PENTA workflow under their own
access boundaries. Private model qualification, measured conversational
coverage, staging, devices and the six critical release records remain open.
[Production acceptance](../HANDOFF/PENTA_PRODUCTION_ACCEPTANCE_2026-11-15.md)
