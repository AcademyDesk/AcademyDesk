# PENTA Teacher attendance browser acceptance

15 November 2026 · Windows desktop · `codex/penta-host-integration-20261030`

The Teacher workspace supports the guided request “Mark attendance for
Piano”. The host verifies the linked active Teacher account, limits class
search to current session assignments and limits roster search to active
enrollments in the selected academy and batch. The browser shows the exact
class, learner, mark, note and in-app notice. The teacher confirms before
the existing transaction saves the mark and queues the notice.

The new search endpoints reuse the existing current-actor and assignment
boundary. Lookup responses are private and bounded to ten rows. An audited
read returns only the fields needed to select a class or learner. The
existing write path still verifies roster membership, the reviewed digest
and current attendance. The browser saves only scoped recovery identifiers
and uses GET after a reload. Manual Attendance remains available.

Cloud C35 covers the separate delegated Academy Admin attendance workspace.
This Teacher packet reused the protected Teacher attendance service without
reapplying C35 changes.

## Verification

| Check | Result |
| --- | --- |
| Frontend production build, TypeScript and static postbuild | Passed using 199 verified frontend inputs and 682 static files. |
| .NET API and harness builds | Passed. |
| Teacher and shared prompt client tests | 12 passed, including malformed options, cached readiness, exact bindings and lost confirmation. |
| Native HTTP/SQL | 57 checks passed, including 16 Manual notes, 34 assigned-Teacher and 7 new readiness/options checks. |
| Live Teacher browser | 3 groups passed at 390px. The incomplete request asked for a batch. A class assigned to another teacher and a paused enrollment were excluded. The exact mark and notice were displayed before confirmation. |
| Receipt recovery | The confirmation committed, then the response was dropped. Reload read the exact receipt with no repeated POST. SQL contained one Absent mark, one queued in-app notice and one assigned-Teacher audit. |
| Cleanup | The run-owned SQL container, database and runtime login were removed. Both original PENTA containers were restored and the local API returned 200. |

Validation window `1a4a92d0dd7a42d3a26aedfe591026ac`, SQL run
`719a9d2426d8478eb87d30665bf9d894`, harness SHA-256
`ed230210527c29d85aa184180c9ab0c2ff92a63bcad0073eb59eb9d6b0b74bc0`.
Docker Desktop was started after initial inspection found the Linux engine
stopped. The completed run preserved Docker inventory.

## Production acceptance

The flow remains a Testing-only guided conversation. Physical devices and
private-model interpretation still need acceptance. The 15 release guard
tests pass. The live guard continues to report six open critical records.
The launch handoff records the remaining role, model and release evidence.

[Production acceptance handoff](../HANDOFF/PENTA_PRODUCTION_ACCEPTANCE_2026-11-15.md)
