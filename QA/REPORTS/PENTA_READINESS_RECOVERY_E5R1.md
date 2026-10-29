# E5-R1 — existing PENTA backend reuse and explicit readiness recovery

2026-10-16. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, preserved HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4` plus prior work. Continues the existing E3/E5 integration queue, not a backend rewrite, replacement audit, reduced-scope launch or production acceptance.

## Reuse, not restart

The current product chat already uses authenticated session lookup, server-resolved academy context, Mini health, conversation creation and typed conversation turns. Existing ERP application/domain services, authorization, finance calculations, transaction/audit guards and engine contracts remain intact. This batch changes only the chat component, its scoped presentation and additive browser checks. No API, schema, domain service, engine, provider registration or permission change.

The reusable PENTA foundation is already pinned in Academy Desk. Upstream `chat-visual-preview@0.2.1-fw1.ui2` remains a synthetic interaction prototype, not an authenticated production integration. Do not import its demo identities/history/action states or unlock tools by copying presentation. See [foundation integration](BATCH_2_PENTA_FOUNDATION_INTEGRATION.md) and the existing Mini N2u handoff. Latest recorded N2r qualification remains 86/94 NOT ACCEPTED; this frontend fix does not accept a different engine.

## Defect and implemented correction

Boot assigned academy context for valid Degraded/Unavailable health responses. Send, prompt suggestions and the composer were previously enabled by context alone. They now also require verified Available readiness; the submit handler independently applies the same guard. The tools rail no longer advertises usable tools during failed readiness or a blocked conversation.

An explicit availability panel keeps Manual navigation available while AI checks fail. Recheck performs fresh authenticated session/context/readiness reads, starts a clean conversation and clears draft/history with visible explanatory copy. It never automatically creates a conversation, submits/replays a prompt or retries an uncertain turn. Existing sign-out/abort/generation fences remain. Changed roles are rejected before another Mini readiness request. A failed context check says unverified rather than indefinitely checking; successful explicit recovery returns focus to the enabled composer.

Readiness is a current UI preflight, not an authorization grant or a guarantee that the next request succeeds. The host must still authorize every conversation/turn/tool request. A later turn failure retains the existing blocked/explicit-new-conversation behaviour. No polling, background automation, writes or additional role tools are enabled.

## Verification

- TypeScript and component-scoped ESLint passed before the final copy/focus refinement; production rebuild and final browser receipts are recorded below after completion.
- Existing focused chat contract 57/57 and workspace/role-entry contract 20/20 passed. No existing assertion was removed or relaxed.
- Initial production build/static export passed 86/86; synthetic exported-browser contract 22/22 (16 retained, six new) and design 6/6 passed. Initial local receipts: `QA/EVIDENCE/penta-chat-contract-1792157302421/result.json`, `QA/EVIDENCE/penta-design-1792157310517/result.json`. Screenshot review prompted the final truthful failed-access label/focus refinements; these initial receipts are not claimed to execute those refinements.
- Six additive browser cases cover Unavailable, Degraded, malformed health, unavailable recovery, degraded recovery and a role change on recheck. They check disabled composer/Send/suggestion, Manual route, no automatic readiness retry, no conversation/turn POST before explicit send, New conversation not bypassing readiness, fresh auth on recheck, changed-role rejection, touch target and horizontal containment. Existing invalid receipts, replay, cancellation, session expiry, network failure and sign-out checks remain.
- All browser API responses are intercepted **synthetic** data. Live authenticated SQL/Mini/provider/device tests are **NOT RUN** here. Prior 1,132 focused backend /755 SQL checkpoint evidence is retained, not re-executed or claimed current whole-app acceptance. No persistence/backend change warrants an identical SQL run for this batch.

## Remaining execution path

### Final verification — supersedes initial UI receipts above

- Final webpack production build including TypeScript/static export **86/86 PASS**. Final component-scoped ESLint **0 errors/0 warnings**.
- Final exported-browser contract **22/22 PASS**, `QA/EVIDENCE/penta-chat-contract-1792157407369/result.json`. Includes final failed-access label/focus checks; zero unexpected console/page errors, only the three pre-existing deliberate 410/409/network transport logs retained. Final screenshot reviewed at 320px dark.
- Final design browser **6/6 PASS**, `QA/EVIDENCE/penta-design-1792157417627/result.json`: 320/768/1440 widths in both themes, existing interaction/containment/contrast checks retained. These are desktop-browser emulations, not physical Android/iOS acceptance.
- Final changed-source SHA256: chat `A31236FDD3973C14170ACE57E71EDDC23543D835E82F748945D2D1F722B064E7`; CSS `5DC711B4C39B0C252CC4F0BE8CE7AA8CFEF92EEF9F46D9A9AB12E36D8DD58FBF`; browser harness `C7D727F120F9F55CB73BF7C20345DCFDAFC4139CDE84E2250D36E2C1FF7110A3`.
- Changed-file diff check passed. Zero staged files; worktree HEAD unchanged; primary HEAD unchanged `f7af512f884ca5fe8ac3ddb287c852492592e709`. `QA/EVIDENCE` remains local/untracked and `.build-check` ignored. No SQL container, real provider or production resources touched, no main/Mini/stage/commit/push/Azure change. Existing unrelated work preserved.

### Ordered pending work

- [x] Existing backend and enterprise safeguards retained; no rebuild from scratch.
- [x] Readiness gating and explicit safe recovery implemented.
- [ ] Finish authenticated UI2 composition and remaining Manual screen/theme/state parity from the existing per-portal ledger.
- [ ] Accept the pinned Mini return through the existing evaluation and linked SQL/browser gate; do not bypass it with synthetic success.
- [ ] Complete authorized conversational workflows beyond the current Owner/Admin student/fee read pilot, followed by source-backed intelligence and governed automation. Platform/Teacher/Student entries do not imply available tools.
- [ ] Qualify real provider setup/consent/reconciliation/operations, then complete the existing enterprise/device/security/operational/exact-release gates and obtain explicit publication/Azure approval.

Next route: Sol High for authenticated UI2/context/domain integration; Sol Medium for settled presentation-only parity after contracts are fixed. No automatic model or project switch. Full app → accepted/authorized go-live → Hayansh onboarding → product website remains the delivery order. Production remains NOT READY; no unsupported date or whole-app completion percentage.
