# PENTA AI0-S2a — synthetic execution ledger

Date: 2026-10-04. Branch: `codex/enterprise-p0-continuation`. This is the next bounded slice after [AI0-S1](PENTA_AI0_S1_FOUNDATION.md), not the whole AI0-S2 acceptance gate or an Azure release.

## Implemented

- Added tenant-scoped `PentaTask`, `PentaExecution` and `PentaAttempt` tables and migration. Composite tenant foreign keys prevent cross-academy task/execution links. A SQL unique key claims `(academy, actor, tool-version, idempotency-key)` once.
- The synthetic turn now records its claim before provider dispatch, commits a validated outcome, attempt and one PENTA-owned audit event together, and returns task/execution IDs. No prompt text is persisted; only a SHA-256 fingerprint of canonical capability/text arguments is stored to detect changed arguments. This fingerprint is not encryption and is not suitable for sensitive live prompts without a privacy review.
- A matching retry replays the recorded success, returns 202 while the original call is running, or returns the recorded failure/unknown state without dispatching again. Changed arguments with the same key return 409. A caller should supply a stable `Idempotency-Key` (printable ASCII, at most 128 chars); absent keys are generated per request and cannot protect a client retry.
- `GET .../penta/tasks/{taskId}` reads only the current actor's task after the same feature/role/tenant checks. The generic academy filter still enforces academy access but no longer owns PENTA's audit row.
- This remains Development/Testing only, explicitly disabled by default, and `diagnostic.synthetic.v1` is the only tool. It reads or changes no academy business records and makes no external model call.

## Evidence

| Check | Result |
| --- | --- |
| Migration generation and SQL-harness build | PASS; migration `AddPentaExecutionLedger`; build 0 warnings/errors. EF design-time emitted two pre-existing decimal-precision warnings unrelated to these new tables. |
| Targeted API suite | PASS, 8/8, including repeat/conflict and exact ledger/audit counts. |
| Full API suite | PASS, 1,052/1,052, 0 skipped. |
| Disposable SQL/HTTP/Identity | Initial run `050ad224376b48ab9165de2a327379d1` PASS: 83 app migrations and 7 Identity migrations; tenant/role/default-off denial; replay/conflict; concurrent 202/200 with one provider dispatch, one execution and attempt. Owned SQL database/login/container removed. |
| Restart replay retest | PASS, final run `5a6ed02e876944469b3f99aca387f46d`: a newly started host replayed the same SQL-backed outcome with no further provider call; 83 app/7 Identity migrations, exit 0. Owned SQL database/login/container removed. |

The concurrent check holds the first fake provider response until the second request observes the in-progress row, then confirms completion and replay. It does not prove every collision interleaving, crash point, audit-store fault, or external side-effect exactly-once semantics. If completion persistence fails, the durable claim stays non-replayable instead of blindly running again; reconciliation/recovery is pending.

## Remaining gate and route

AI0-S2 is **not complete**. Next: approval and usage-reservation schema/policy, finite tenant/user budget enforcement, explicit state transitions and fault/rollback/restart/concurrency tests, all against synthetic-only work. No real academy mutation or live provider until later gates. P0 issues and full pre-Azure release gates stay open. No main/merge/GitHub push/Azure deployment from this slice.

Next model: GPT-6.1 Sol, High reasoning, Codex Default mode for AI0-S2b. Astra High only if a new consequential authority/security decision cannot be resolved under the selected ADR; Luna/medium for bounded documentation after the policy contract is fixed.
