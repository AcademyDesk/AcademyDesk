# PENTA AI0-S1 — disabled synthetic foundation

Date: 2026-10-04. Worktree: `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`, source base `9265e72`. Scope: first bounded implementation slice from [ADR PENTA-001](../../PENTA_ARCHITECTURE_DECISION.md). This report does not close an enterprise issue or authorize Azure.

## Delivered

- `apps/api/Controllers/PentaController.cs`: authenticated `POST /api/academies/{academyId}/penta/turns` with request-size/text bounds, explicit capability validation and unknown-property rejection.
- `apps/api/Intelligence/Penta/PentaFoundation.cs`: five canonical capability identifiers, feature/environment gate, current-account/academy/role pilot policy, fixed synthetic dispatcher and response validation. Synthetic responses read or change no academy business record; no real provider or dynamic tool calls exist.
- `apps/api/Program.cs`: scoped policy/dispatcher and fixed provider registration. Missing `Penta:Enabled` is off. A true flag works only in Development/Testing; the route remains unavailable in Production.
- Three targeted API tests and a guarded real HTTP/disposable SQL module covering Identity, two academies, wrong role, platform owner, inactive academy/account, default-off flag, tampered/oversized request, malformed provider, result and generic audit. The existing QA run guard and ownership cleanup remain in force.

The current academy action filter remains in the route pipeline. Its successful synthetic POST adds one generic metadata audit row; denied and malformed operations add none. The dedicated PENTA execution/audit owner is deferred to AI0-S2 before any business write. The response correlation ID is provisional, not a durable task or completed domain action.

## Validation

| Check | Result |
| --- | --- |
| API build | PASS, 0 warnings/errors. |
| Final full API suite | PASS, 1,051/1,051, 0 skipped, including final inactive-account and Production-environment flag checks. |
| Targeted PENTA tests | PASS, 7/7 including four environment/flag cases. |
| SQL harness build | PASS, 0 warnings/errors. |
| Run-owned real Identity/HTTP/SQL PENTA module | PASS, run `fa30408d8666477ab0d0e24d742d32dd`; two academy admins, denial cases, synthetic output and audit. 82 application and 7 Identity migrations applied in the disposable database. Exit 0; run-owned database/login/container removed. |

The first SQL run `37bb022bb5f34d9fa9e912bbff5fc00e` also exited 0 and removed its owned resources; the final run printed the dedicated PENTA assertion summary and included the inactive-account case. The native TestServer cannot exercise Kestrel's request-size feature, so the text bound is tested there and the real-host body limit remains for later runtime validation. SQL/HTTP proves the relevant role/tenant path; it does not certify all legacy routes, physical devices or production configuration.

## Open boundaries and next route

No live student read, model connection, approval, execution ledger, budget reservation, workflow worker, UI or schema migration was enabled. The five existing P0 issues and pre-Azure release gates remain open. [AI0-S2](../../PENTA_ARCHITECTURE_DECISION.md#13-implementation-slices-and-exact-next-task) adds durable task/execution/approval/usage state, idempotency and shared audit ownership; it needs real SQL concurrency, fault and restart evidence before any low-impact business action can be considered.

Next engineering route: Codex / GPT-6.1 Sol / High reasoning / Default mode for AI0-S2, split into schema/state and budget/approval sub-slices if needed. Cursor/Sol Medium can handle bounded docs/UI after the contract is stable. No GitHub push or Azure deployment occurred in AI0-S1.
