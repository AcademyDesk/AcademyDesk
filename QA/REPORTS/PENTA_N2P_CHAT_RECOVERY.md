# N2p — PENTA chat recovery

2026-10-08. Local application continuation on `codex/penta-search`, starting HEAD `d3c4ccfd6ea8344a73e390e866b43f512cb51e17`.

## Result

The existing read-only chat now handles a same-tab sign-out while a request is in flight. `clearPortalTokens` emits a workspace-scoped event because browser `storage` events reach other tabs, not the tab performing the removal. PENTA aborts its request, invalidates its generation and clears private in-memory conversation state. Existing cross-tab sign-out handling stays in place.

After asynchronous session creation and receipt parsing, the UI checks both the abort signal and the current generation before submitting a turn or committing a result. A late response cannot populate a signed-out or superseded conversation. A user can stop waiting for an unresolved request, then explicitly start a new conversation; the UI neither retries nor assumes whether the server completed the old read. Network failure and HTTP 409 retain the draft prompt, block further use of that conversation and require an explicit new conversation. A replayed receipt fails the existing request/version validator without appending a turn.

Scope: `apps/web/src/lib/api.ts`, `apps/web/src/components/penta/penta-chat-workspace.tsx`, its CSS module, and the existing synthetic exported-browser fixture. No API/domain/Mini/tool/authority change, history, training or write operation.

## Validation

| Check | Result |
| --- | --- |
| Targeted ESLint | PASS |
| Next.js webpack production export and TypeScript | PASS, 83 static pages |
| Synthetic exported-browser cases | 16/16 PASS |
| Unexpected browser/page errors | 0; expected 410, 409 and network resource logs recorded separately |
| Real Mini, SQL, physical Android/iOS | NOT RUN in this application packet |

The browser fixture used its own ephemeral loopback export server and explicitly synthetic API interception. The six added scenarios cover network interruption with explicit recovery, HTTP 409 stale version with explicit recovery, replayed receipt rejection, cross-tab logout during a held request, actual same-tab sign-out during a held request, and user cancellation of a held request followed by explicit recovery. The same-tab case verifies the local sign-out event was emitted by the real shared sign-out function. Late held responses do not create Student 360 links. Existing ten response-boundary scenarios still pass at 320/390/1440 px, with light and dark themes represented. The runner closed its browser and server.

Final local evidence: `QA/EVIDENCE/penta-chat-contract-1791473594368/result.json`. The initial cross-tab test's incorrect expectation that the chat would remain mounted is retained at `QA/EVIDENCE/penta-chat-contract-1791473366611/failed.json`; the existing workspace frame intentionally replaces the page with a session-ended view. Intermediate passing runs are retained. Evidence remains local/untracked.

This is synthetic UI recovery proof. It does not establish the actual Mini's language accuracy, SQL/source correctness, production authentication, mobile-device acceptance, or enterprise release readiness. Mini 0.1.13/planner-0.12 remains **NOT ACCEPTED** at the reported 86/94 exact gate; the linked SQL and 15-turn live browser acceptance remain blocked. N1/N2, P0 and pre-Azure gates stay open. No stage/commit/push/merge/Azure operation was performed.

## Next route

Codex / Sol High for the next consequential paired backend/frontend integration only after the relevant prerequisite is clear. A separately scoped Mini semantic/model evaluation should address its eight remaining language failures; application UI work can continue independently. Avoid N3 private history or domain-action expansion while the current live-read acceptance is blocked.
