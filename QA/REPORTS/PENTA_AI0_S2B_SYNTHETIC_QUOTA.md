# PENTA AI0-S2b — guarded synthetic quota and approval schema

Date: 2026-10-04. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. This is a bounded continuation of [S2a](PENTA_AI0_S2A_EXECUTION_LEDGER.md), not permission to use a live model or deploy to Azure.

## Change

- Added `PentaApproval` with tenant-linked execution FK, unique execution binding, policy/digest/expiry fields and SQL rowversion. The only enabled synthetic R0 tool records `ApprovalMode=None`; there is **no approval creation/decision endpoint** and no R1/R2/R3 tool. An approval row alone grants nothing.
- Added `PentaUsageReservation` and append-only `PentaUsageEntry`, each tied through composite academy keys. A synthetic call reserves one local work unit, with explicit zero-dollar synthetic price version. Known outcomes reconcile one entry; unknown outcomes retain the reservation without inventing actual usage.
- New claims serialize on the academy SQL row while checking finite UTC-day quotas. Defaults: 20 synthetic calls per actor, 100 per academy. Invalid configured limits fail closed with 503. A new key over quota returns 429 before provider dispatch and without a task/usage write. Matching prior keys still replay their existing outcomes after current authorization.
- These units are **not** tokens or money. Zero synthetic cost must never be reused as pricing for a live provider. Live calls remain disabled until a separately approved model/price version, worst-case token/cost reservation, tenant/user financial budgets and observed-usage reconciliation are implemented and tested.

## Evidence

| Check | Result |
| --- | --- |
| Migration | `AddPentaApprovalAndSyntheticUsage`; disposable SQL applied 84 application and 7 Identity migrations. Design-time EF emitted two pre-existing unrelated decimal-precision warnings. |
| Full API suite | PASS, 1,055/1,055, 0 skipped. Includes unknown-outcome quota retention, numeric and nonnumeric invalid-config fail-closed, replay/ledger checks. |
| SQL harness build | PASS, 0 warnings/errors. |
| Real Identity/HTTP/disposable SQL | Final source run `e18756078110487684eaa6fec8e68753`, exit 0. Concurrent admins at tenant quota: exactly one accepted and one 429; exhausted-key replay still succeeded; user quota isolated actors; cross-tenant approval FK rejected; known usage reconciled; provider exception stayed `OutcomeUnknown`/`UsageUnknown` and did not re-dispatch after restart. Owned database, login and container removed. Earlier runs `8e01241b68f04f9b9bb7e5951729dfed` and `8c3ec973bdaa406298d001c40da533a4` also passed; the latter added the fault/restart case before the final config parser change. |

The SQL concurrency check tests a controlled two-request boundary, not every interleaving or a provider-side exactly-once guarantee. New claims and quota are one SQL transaction; outcome, attempt, known usage and audit are a later atomic save. If that later save fails, the claim remains in-progress and cannot be blindly replayed. Automatic stale-claim reconciliation and audit-store fault injection are still pending. Physical devices, full security gate, and all existing enterprise P0 issues remain open.

## Remaining before live AI

1. Implement priced worst-case model token/cost reservation with versioned approved provider pricing, finite tenant/user monetary budgets, unknown-cost retention and reconciliation; reject unknown pricing.
2. Add a policy-bound R1 prepare/confirm contract only when the first low-impact deterministic domain action is chosen. Validate approval digest, expiry, current actor/policy/state, concurrency and rejection. R2/R3 remain unavailable.
3. Test crash/rollback/audit-store fault and recovery states on real SQL; never infer that a timed-out external effect failed.
4. Complete privacy/provider selection and the established release gates. No external provider, academy business mutation, GitHub push, merge or Azure deployment in this slice.

Next engineering route: Codex / GPT-6.1 Sol / High / Default mode for a tightly scoped S2c priced-budget and recovery contract. Astra only for a genuinely unresolved consequential authority choice; bounded docs/UI can move to Sol Medium or Cursor after contracts settle.
