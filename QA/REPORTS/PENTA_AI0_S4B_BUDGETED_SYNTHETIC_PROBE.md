# PENTA AI0-S4b — internal budgeted synthetic probe

Date: 2026-10-04. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. This is a fake-provider foundation test, **not approval for external AI calls**.

## Change

- Added an internal-only coordinator that is neither registered in DI nor exposed by a controller or worker. It requires Development/Testing and four independent flags, plus exact provider/model identity. Its only payload is the code-owned synthetic probe from S4a.
- Before one provider dispatch, the SQL budget must atomically reserve worst-case USD using the existing versioned policy. The execution must be an executing `penta.synthetic-model-probe.v1` claim; another tool or an existing reservation cannot be used.
- A successful bounded response commits usage reconciliation, execution/task completion, attempt and audit in one SQL transaction. Any non-success or missing/out-of-bounds usage is `OutcomeUnknown`: the full estimate stays held, with no automatic retry. An audit failure rolls back completion, leaving the durable claim reserved for deliberate recovery. The provider's text and credentials are never stored in the ledger.
- No production pricing, real provider key, external call, customer-data disclosure, user-facing endpoint, automatic dispatch or academy mutation was added.

## Validation

| Check | Result |
| --- | --- |
| API and SQL-harness builds | PASS, zero warnings/errors. |
| Existing S4a fake HTTP transport tests | PASS 23/23. |
| Full API suite | PASS 1,082/1,082; zero skipped. |
| Disposable real Identity/HTTP/SQL `PentaFoundation` | PASS, run `c7caa731a1934fd2ba8683ddf6233853`. Disabled and identity-mismatch gates made zero calls; exact-match fake provider dispatched once only after reservation; observed 33 input/8 output tokens reconciled once; ambiguous response held its full reservation; third call was exhausted before dispatch; completion audit injection rolled back outcome and usage. Earlier PENTA priced, recovery, approval, role/tenant regressions stayed green. Run-owned database, login and container removed. |

## Open gates

The owner has not selected/approved provider, model, region, retention/abuse-monitoring terms, data classes, operational price schedule or external-call policy. `store:false` controls Responses storage behavior but is not alone a zero-retention agreement ([OpenAI data controls](https://developers.openai.com/api/docs/guides/your-data)). The orchestration contract does not yet bind current authenticated actor authority or a production route and must not be treated as a live-ready AI gateway. Real-provider fault/load/circuit-breaker work, P0 issues, browser/device and pre-Azure release gates remain open. No push, merge or Azure deployment.

Next: get an explicit privacy/provider decision, then design a current-actor, academy-scoped, minimum-field caller and independent live safety review; or continue AI1-S1 read projections while all external calls remain off. Sol High/Codex for this authority boundary; Sol Medium/Cursor for bounded UI/docs; Astra only for a consequential unresolved architecture/security question.
