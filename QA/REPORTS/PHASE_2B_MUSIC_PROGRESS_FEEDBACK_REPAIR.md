# Phase2B — Music save, status and recovery feedback

2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, parent `647cd7fc19385828e67cc14168e0ae97b9ba4d0c`.

## Bounded repair

Continue [SQL/linked verification](PHASE_2B_MUSIC_PROGRESS_SQL_LINKED.md), not a new audit. The observed two same-tick status requests are now prevented by a synchronous page-wide ref guard, held through write and readback. Add-piece/assignment use the same guard, so opposing writes and refresh cannot race locally. Native fieldsets and custom selects disable until a checked workspace is ready and while saving/refreshing.

All original POST/PATCH fields, optional-null conversions, date/notes and `score ?? null` remain unchanged. Captured form values reset only after a successful response and only if still matching; unrelated or newer drafts survive. No controller, SQL schema, permissions, finance rules, model or PENTA contract change.

Durable polite notices distinguish confirmed save, rejected save, confirmed save with failed readback, and unconfirmed network/5xx outcome. Unknown outcomes are not retried automatically. Failed/malformed readback and uncertain writes disable saving until an explicit uncached **Refresh workspace** succeeds; this is observation, not cancellation, rollback, idempotency or proof that an unknown write did not occur. Initial-load/read version and component-lifetime guards reject superseded or unmounted responses. A captured status handler from an older local row cannot overwrite newer local readback.

## Verification

- 51 new actual-TSX controlled checks PASS, plus20 retained score cases and one strengthened strict same-tick one-request case:72 total PASS. Frozen parent under the same51 new checks:1 PASS/50 FAIL (assertions, not50 distinct defects). Covers all three actions, exact payload/reset, durable success,400/403/409/429 rejection,500/503/network uncertainty, delayed write/readback duplicates/opposing writes, unmount, changed drafts, malformed/denied initial data, explicit recovery, superseded initial read and uncached lists.
- The old characterization expecting two writes was deliberately replaced with the stricter one-write requirement; no runner/API/SQL acceptance assertion weakened. A fixture initially held the first four reads, including recovery, and failed one test; holding only the intended original request corrected it. Assertions retained.
- TypeScript and production webpack export83/83 PASS. Target music lint0 errors/0 warnings after scoping the loader and cleanup; no suppression. Initial root-directory lint invocation lacked the web config, and a later lint process stalled during the interrupted chat; its exact owned session was interrupted and bounded lint/typecheck rerun. These were tooling failures, not silent passes.
- 48 new production-export synthetic browser cases PASS:320/1440px × light/dark × piece/assignment/status × normal/readback-failure/rejected/uncertain-but-committed. Real DOM held-request disabled states, opposing native submits/no duplicate, payload/draft/feedback and explicit recovery checks.48 original zero/positive/null saved/denied browser cases retained/rerun PASS. Local receipts: `QA/EVIDENCE/music-feedback-browser-1791729311552/result.json` and `QA/EVIDENCE/music-progress-zero-1791729368970/result.json`. Mobile light/dark success screenshots visually inspected; no clipped register/controls observed.
- Existing MusicProgress owned SQL harness rerun unchanged:17 real Identity/HTTP/SQL controls PASS, then four linked export-to-real-API/SQL status journeys/eight PATCH200 PASS with new pending/polite-success checks. Fresh score0/date/notes, authoritative GET/reload, foreign sentinel/count, no429/console/network/overflow retained. Linked receipt `QA/EVIDENCE/music-progress-live-1791729433748/result.json`; full run log `QA/EVIDENCE/music-progress-feedback-20261011/sql-linked-final.txt`.
- SQL run `83e076c03e464885af711f813b4b147e`, loopback60986,214.6 seconds, exit0.88 application/7 Identity migrations and restricted login verified; mismatched-marker negative cleanup refused. Only exact owned database/login/container removed. Same two running PENTA and seven older stopped containers preserved; no pruning.

## Limits and next route

BUG-DATA-0054 remains OPEN/RETEST. Local pending/read ownership is not server-side cross-tab/other-actor optimistic concurrency, idempotency or preservation of concurrently edited metadata. Linked positive/null status variants, real linked creation/assignment fault/recovery, physical Android/iOS, wider role/device/critical/release gates remain open. Full enterprise/API suite and unrelated Mini/Platform work were not rerun or accepted by this frontend batch. Prior build/harness evidence remains recorded, not a new whole-product production verdict.

Next **Sol High**: existing BUG-DATA-0055 authoritative invoice paid-status/ledger-integrity reproduction and conditional repair. **Sol Medium**: settled Manual visual rollout only after shared contracts; Orbit + Aura dark /Prism light and versioned PENTA FW1 return remain pending. No Mini project switch needed for this page-local repair; Mini86/94 qualification and the90% eligible-AI/10% Manual product target remain separate, not accepted coverage.

Publish only the validated feature scope. Older32/28 handoff/QA additions, twelve unrelated tracked diffs and local evidence preserved. No main edit/merge, production data/credentials, Azure, new testing plan or issue closure.
