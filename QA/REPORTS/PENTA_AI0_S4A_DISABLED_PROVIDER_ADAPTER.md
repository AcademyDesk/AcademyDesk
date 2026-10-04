# PENTA AI0-S4a — disabled synthetic provider adapter

Date: 2026-10-04. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. This is a transport/evaluation boundary, **not a live AI integration or provider approval**.

## Scope

- Added a provider-neutral `IPentaModelProvider` contract and a candidate OpenAI Responses adapter. No controller, PENTA turn, read projection, task execution or background worker invokes it. Registration alone cannot send a request.
- The adapter accepts **no prompt, user ID, academy ID, academy record or tool list** from callers. It can only send a fixed code-owned synthetic connectivity probe. It requires `Penta:Enabled`, `Penta:Provider:Enabled` and `Penta:Provider:SyntheticProbeApproved`, a configured model and server-side API key, and a Development/Testing environment. All flags default false/absent in the application configuration. No real key was supplied or used in this slice.
- The request fixes the HTTPS Responses endpoint, `store: false`, no tools, `tool_choice: none`, a 64-token output cap, strict JSON schema and a 15-second deadline. Automatic redirects are disabled. The response body is capped at 16 KiB; output shape, completion status, probe value and usage token counts are validated before a success result. No provider text, prompt, key or remote error body is logged or returned.
- Refusal, incomplete output, malformed output, configuration/auth errors, rate limit, other rejection and unknown transport/server outcomes have distinct safe statuses. The adapter never auto-retries an uncertain outcome. It is not yet connected to the priced budget, execution ledger or a circuit breaker; those are mandatory before any production-capable model route.
- Official OpenAI documentation confirms that Responses uses `text.format` for Structured Outputs and that `store` otherwise defaults to true ([Responses migration guide](https://developers.openai.com/api/docs/guides/migrate-to-responses), [Responses create reference](https://developers.openai.com/api/reference/cli/resources/responses/methods/create)). `store: false` is **not** a general zero-retention guarantee: default abuse-monitoring retention and eligibility for modified/zero-data-retention controls still require a separate privacy decision ([OpenAI data controls](https://developers.openai.com/api/docs/guides/your-data)).

## Validation

| Check | Result |
| --- | --- |
| Fake-transport adapter tests | PASS 23/23. Independent environment/feature/approval gates; invalid configuration; fixed no-tool/stateless/schema request; 401/403/429/400/408/503 mapping; malformed, incomplete, tool-call, refusal, oversized and transport-fault results. No internet/API key used. |
| Full API suite | PASS 1,082/1,082, 0 skipped. |
| Real Identity/HTTP/disposable SQL regression | `PentaFoundation` run `9fff3c04e12f4b3a917f80ae6baa8a30` PASS, 86 application and 7 Identity migrations. Existing gateway, priced budget/recovery, synthetic approval, role/tenant and ledger checks stayed green. This run did not contact OpenAI. |
| Cleanup | Guarded runner removed its own SQL database, login and container; no other containers pruned. `QA/EVIDENCE` remains local. |

## Open gates

The business owner has not selected/approved a provider, model, external processing terms, region, retention category or content policy. No live-model call, external synthetic call, customer-data disclosure, secret provisioning, priced usage reconciliation, circuit-breaker/load test, browser/device acceptance or enterprise release approval occurred. The adapter is therefore a **partial AI0-S4 foundation**, not an enabled pilot. A future caller must first bind current actor/academy authority, minimum-field disclosure, budget reservation, audit, no-blind-retry recovery and a kill switch; merely turning on flags is insufficient for a customer-data path.

Next: AI0-S4b privacy/provider decision and budget-bound orchestration for an explicitly approved synthetic probe, or AI1-S1 minimum-field source-verified read expansion while external calls remain disabled. Route authority/privacy work through Codex/Sol High; use Sol Medium/Cursor for bounded docs/UI, Astra only for a consequential unresolved decision. No push, merge or Azure deployment.
