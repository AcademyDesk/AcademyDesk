# Template creation and starter-import recovery

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. Bounded functional continuation of the communications presentation slice.

## Implemented

- Creation and starter import share synchronous ready/pending/review/snapshot guards. Same-turn duplicate handlers, cross-operation writes and refresh during a pending write/readback cannot submit another mutation.
- Atomic checked template/catalogue loading validates array and displayed-row shapes before committing either list. Unavailable data is distinguished from a successfully loaded empty register; forms fail closed and explicit refresh can recover initial failures.
- Confirmed HTTP success remains success even with a null/unparseable response body. Import no longer dereferences `result.message` on null. A failed readback says the template was saved/imported but the register could not refresh; it does not imply the write failed.
- HTTP 500+,408 and network uncertainty retain drafts/import selections, block another write and require deliberate successful register refresh and user review. No automatic POST retry. Old snapshot callbacks remain invalid after refresh.
- Confirmed creation resets only the captured draft revision. Native and shared-selector changes advance that revision, so newer drafts are not erased. Unmounted completions do not reset inputs. Starter IDs are checked against the loaded catalogue; existing group/set-selection and exact JSON contracts remain intact.
- Visible controls disable consistently while unavailable/pending/review-required, including custom selectors. Existing optional strings, channel/status/category/group values and server authorization remain unchanged. Backend remains authoritative; no new permissions, sending or model behavior.

## Verification

- Production webpack build, TypeScript and 86-page static export PASS; page ESLint zero errors/warnings. Pinned shared vendor six-file check PASS. Source/script parsing and scoped whitespace checks PASS.
- 193 focused tests PASS, zero failures, five explicit historical hash checks skipped. Fifty-seven new actual-handler recovery tests cover exact bodies, both operations, pending write/readback, cross-operation and stale callbacks,400/401/403/404/409/429,500/408/network uncertainty, malformed/null successful and error bodies, malformed/unavailable reads, manual recovery, unmount, newer native/selector drafts and group selection.
- Historical visual snapshot is retained unmodified. Five template function-hash assertions are explicitly superseded by those functional tests because these handlers intentionally changed; ten other communications/preference declaration hashes still pass. No new hashes blessed over the old baseline. Existing channel/template compatibility, consent and feedback assertions still pass. The first new test run used incorrect hook-state indices; harness indices corrected, no application behavior changed to satisfy them.
- 162 final synthetic browser cases PASS:108 template recovery cases (create/import × nine scenarios × three widths × two themes), plus54 communications/template/preferences visual-state regressions. Template POST bodies checked exactly; duplicate native submits during pending/review are rejected. Failure/refresh recovery, null success, draft retention,44px actions and overflow/page errors checked. Local fixtures simulate server commits before uncertain responses; they are not real SQL persistence proof.
- Receipts: `QA/EVIDENCE/template-recovery-1792055499549/result.json` (108), `QA/EVIDENCE/engagement-visual-1792055524011/result.json` (54). Both record source hashes; phone recovery screenshots inspected. No app-source edits after final build. Earlier32-message/32-preference outcome browser evidence retained at unchanged page source,not counted as fresh reruns here.

## Limits and continuation

This is client-side duplicate/recovery protection, not durable server idempotency or a multi-tab/concurrent-client guarantee. Refresh does not automatically prove an uncertain write absent; guidance explicitly requires checking the register. Server transactions/authorization remain authoritative. No real delivery/provider/SQL/RBAC/tenant/physical-device/whole-module acceptance claimed. Existing messages/preferences context and uncertainty follow-ups remain in the enterprise queue. No issue or release gate closed.

Next Sol Medium: communication settings and remaining Manual portal presentation. Use Sol High for consequential authority/finance/context/runtime/PENTA integration work. Existing PENTA UI2/Mini/domain conversational and E9–E10 release gates remain OPEN. No Mini contract change/project switch required.

No backend/schema/Mini/vendor/main/staging/commit/push/Azure/production database/container change. Prior dirty work and local evidence preserved. Owner delivery sequence remains complete agreed PENTA+Manual app → authorized go-live → Hayansh → Academy Desk product website after first customer live;90% AI is still a target,not measured delivery.
