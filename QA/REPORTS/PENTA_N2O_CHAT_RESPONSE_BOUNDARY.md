# N2o — application chat response boundary

2026-10-08. Local application slice complete; **Mini language acceptance and production release remain BLOCKED/OPEN**.

## Continuation and scope

The owner returned from Mini improvement and requested Academy Desk continuation. Read the updated Mini/Academy handoffs and the intentionally local N2n report (`QA/REPORTS/PENTA_MINI_N2N_DECODER_CONSTRAINTS.md`): local Mini 0.1.13/planner-0.12 prevents the tested generation contradictions, but its 86/94 exact candidate is **NOT ACCEPTED**. On the identical old subset it passed 80/86 versus N2m 60/86. Six semantic misses and two deterministic policy false denials remain. These are inherited reported results, not fresh inference or independently repeated N2n acceptance in this task.

Continue independent application safeguards rather than repeat the completed audit, rerun unchanged benchmarks or bypass language prerequisites. Academy branch `codex/penta-search`, HEAD `d3c4ccfd6ea8344a73e390e866b43f512cb51e17`; Mini HEAD `a50bbd354ec0e8938064ad2d753c1d4beccfa0fe`; main HEAD `f7af512f884ca5fe8ac3ddb287c852492592e709`. Existing dirty work is preserved. No stage/commit/push/merge/Azure or credential/customer-database change.

## Reproduced frontend gap and repair

The prior inline receipt validator throws for null rows and null balances. It also accepts missing/malformed context, mismatched capability, inconsistent counts/pagination, missing source/scope, and result rows on an ERROR. Such responses are not a valid verified read, even when transport succeeds.

- Add dependency-free `apps/web/src/lib/penta-chat-contract.ts`, accepting unknown JSON and validating before it reaches React state.
- Match conversation/request/version and the initiating capability, provider and protocol. Check typed bounded context filters, displayed IDs/order and selected identity.
- Validate result kind, source/scope, timestamps, rows, exact bounded-page count/pagination, distinct currencies, finite non-negative amounts and the existing Student 360 destination.
- Check initial session expiry shape, same-academy boot context, usable timezone and the existing readiness contract. Invalid timezone no longer reaches the rendering formatter.
- Explain HTTP 410 as conversation expiry with explicit new-conversation/manual recovery. Preserve blocking after transport/contract failure, prompt text, explicit submission and no automatic retries.

This is a defensive **rendering boundary**, not backend authorization, proof of truthful model intent, cryptographic attestation, or new permission. It does not correct a structurally valid wrong query. No API/domain/tool/permission/model/prompt changes, new writes/history/streaming or N3 expansion.

## Validation

| Check | Result |
| --- | --- |
| Original HEAD receipt guard, synthetic baseline | 21/41 pass; 20 fail, including two TypeErrors |
| New receipt, account, context, session and readiness checks | 57/57 pass |
| TypeScript | PASS after correcting two narrowing errors in the new guard |
| Targeted ESLint, changed application files | PASS, no diagnostics |
| Production webpack/static export | PASS, all 83 pages |
| Actual exported frontend with synthetic intercepted API | 10/10 scenarios, 320/390/1440px and light/dark represented |
| Script syntax and git diff whitespace | PASS |
| Real Mini/SQL/browser acceptance | NOT RUN: current language prerequisite fails |
| Physical Android/iOS, accessibility audit, full enterprise/API regression | NOT RUN; no closure claimed |

Synthetic browser cases: valid source read, null row, null balance, missing filters, wrong capability, rows on ERROR, invalid session, invalid timezone, HTTP 410, and explicit new-conversation recovery after a malformed reply. Invalid results never render a Student 360 link; invalid session/context prevents subsequent turn submission. Exact synthetic create/turn counts prove no automatic resubmission in these cases. All cases check horizontal overflow. Mobile valid/error screenshots were visually inspected.

The fixture serves the actual export on an ephemeral loopback port and intercepts explicitly synthetic API responses. It does not log in, call Mini, connect to SQL or allow external requests. Local previews/browser contexts are closed by the runner. No pre-existing SQL/Mini container is touched.

Evidence is local/untracked:

- `QA/EVIDENCE/penta-chat-contract-units-20261008/baseline.json` and `final.json` preserve named unit outcomes.
- `QA/EVIDENCE/penta-chat-contract-1791472451498/result.json` and screenshots record the successful browser run.
- Initial browser readiness selector timeout is retained at `penta-chat-contract-1791472312908/failed.json`. Correcting an exact-text selector to match the label including its nested subtitle required no product change.
- The next fixture run completed all scenarios but incorrectly rejected Chromium's expected HTTP 410 resource log; retained at `penta-chat-contract-1791472394742/failed.json` with screenshot. Final fixture separately retains exactly that known status/path/log; unexpected console/page errors are still rejected. Final run has zero unexpected errors, **not zero HTTP error logs**.

The baseline runner explicitly reads the unchanged original HEAD guard; it does not reset the working tree. No production test expectation, Mini evaluator or existing linked runner was weakened. The new fixture's setup fixes are not model-language retries.

## Next route and limits

Codex / Sol High for consequential integration. Next independent application slice: qualify network interruption, stale/replayed response and logout/in-flight recovery in the existing conversation UI using synthetic fixtures, without enabling history/new tools or repeating Mini inference. Mini semantic repair/stronger private-model evaluation remains a separately scoped Mini task; no automatic model download, training, replacement or hosted fallback.

The existing language gate, prepared three original plus three context SQL journeys, 15-turn real browser acceptance, device/privacy/capacity/P0/security and pre-Azure gates remain open. The conversational five-capability, 90% eligible AI/10% manual goal is preserved, not declared achieved. This slice is local only and does not establish production readiness.
