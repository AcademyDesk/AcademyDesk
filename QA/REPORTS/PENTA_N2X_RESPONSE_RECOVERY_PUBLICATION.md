# N2x — response and recovery feature-branch publication

2026-10-09. Academy Desk `codex/penta-search`, starting HEAD `7b0b16dac08f94c93fd5cdfeb38547a5c02db7ce`.

## Result and ownership

Reviewed the latest Academy and Mini handoffs before continuing. Mini's returned contract is ready but its engine is blocked: saved 86/94 language acceptance, source 0.1.14/planner-0.12, normal service 0.1.13, executable protocol 0.1. No accepted engine update was available. No model download, activation, tuning, source edit or unchanged inference rerun was performed.

The unblocked delivery gap was the previously completed but uncommitted [N2o response boundary](PENTA_N2O_CHAT_RESPONSE_BOUNDARY.md) and [N2p recovery](PENTA_N2P_CHAT_RECOVERY.md). Those existing application changes are selectively published together with their original tests/reports and the shared-session fixture repair described below. N2w result presentation and N2v design remain intact; this does not repeat their implementation.

The rendering boundary validates unknown response values before React state: account/context/session/readiness, receipt identity/version/capability, bounded typed result rows, source destinations, counts/pagination and currencies. It fails closed on malformed or unverifiable replies. This is not backend authorization or proof that a structurally valid model query matches the user's intent.

Recovery includes workspace-scoped same-tab logout after token removal, existing cross-tab logout, invalidation of late/superseded responses, HTTP 410/409/network/replay recovery and explicit Stop waiting. Stopping the browser wait does not assert that the server cancelled the read. No automatic resubmission, new tool, private history, write operation, protocol or intelligence expansion is enabled.

## Regression gap and repair

The first shared-session run against the local changes passed logout 9/18, refresh 37/37 and concurrency 17/20. Failing VM fixtures did not provide CustomEvent, so the real shared sign-out function raised ReferenceError when emitting its new same-tab notification. This was a missing browser primitive in the fixture, not a reason to suppress the application notification.

Added native CustomEvent and window event dispatch support to the logout/concurrency fixtures. All existing assertions were retained. Four additional workspace tests verify that logout notifies exactly once after its own token removal, preserves other workspaces and does not dispatch a same-tab event to a peer window. Cross-tab StorageEvent assertions remain separate and unchanged. Final shared-session result: logout 22/22, refresh 37/37, concurrency 20/20; **79/79 PASS**, zero failures/skips/cancellations.

## Validation on the preserved working snapshot

| Check | Result |
| --- | --- |
| `npm exec tsc -- --noEmit` | PASS |
| Targeted ESLint: chat, result view, response contract, shared API helper | PASS, no diagnostics |
| `npm run build -- --webpack` | PASS, 83 static pages |
| `node QA/tools/penta-chat-contract.test.cjs` | 57/57 PASS |
| `node QA/tools/penta-chat-contract-browser.cjs` | 16/16 synthetic exported-browser cases PASS |
| `node QA/tools/penta-design-browser.cjs` | 6/6 viewport/theme combinations PASS: 320/768/1440px, light/dark |
| Three existing shared-session runners with native fixture event support | 79/79 PASS |
| Live Mini, linked SQL, full API suite, physical Android/iOS | NOT RUN; no new acceptance claimed |

Recovery browser evidence: `QA/EVIDENCE/penta-chat-contract-1791541224597/result.json`; zero unexpected browser errors. Deliberate HTTP 410, HTTP 409 and network resource errors remain explicitly recorded, not hidden. Design evidence: `QA/EVIDENCE/penta-design-1791541234875/result.json` and screenshots, covering help, result/empty/error/comparison/capped tables, source order/currencies/plain text, explicit choice without auto-send, contained scrolling and manual switching. Primary-action contrast is 5.38 light / 8.38 dark.

Initial shared-session failure summary: `QA/EVIDENCE/penta-n2x-publication-20261009/event-fixture-baseline.json`. Final summary is beside it. These are summaries; full terminal TAP output is retained in the Codex conversation, not represented as a saved raw trace. QA/EVIDENCE remains intentionally local and untracked. Browser runners used synthetic interception and owned ephemeral loopback servers; they closed their own browser/server. No SQL or other container was removed.

## Publication boundary

Include only the chat/response contract/shared logout/CSS changes, response unit/browser runners, two repaired session fixtures, original N2o/N2p reports, this report and narrow N2x handoff/QA/roadmap entries. Preserve and exclude all other dirty backend/provenance/Mini/private-spec work. The checks ran on the preserved dirty snapshot; they do not qualify or publish excluded work. No credential or customer/production database change. Normal feature-branch commit/push only, no force push, main merge, Azure workflow dispatch or deployment.

## Next and open gates

The existing N2u outgoing Mini handoff remains controlling for a qualified engine return. Use Sol High there for the bounded engine/resource decision and exact acceptance criteria; use Sol Medium for settled independent host/UI work. Academy resumes the original actual-model, disposable SQL, security/source and 15-turn browser gate only after a qualified return. Owner Command Centre, governed actions/approvals/history/feedback, enterprise/device/privacy/regression and pre-Azure gates remain open. No READY TO VIEW, production readiness, issue closure or measured 90% AI claim.
