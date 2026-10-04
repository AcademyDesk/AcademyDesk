# PENTA AI0-S2c — priced budget and conservative recovery contract

Date: 2026-10-04. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/enterprise-p0-continuation`. Local, default-off PENTA pilot only; no approved live provider or customer-data disclosure.

## Change

- Added an **internal-only** priced reservation service. It requires a nonempty provider/model/price version, positive bounded six-decimal USD input/output rates, finite user/academy daily caps and bounded worst-case input/output tokens. An unknown or invalid policy is rejected before any reservation. The test quote `qa.fake` is intentionally not a live price or product setting.
- The SQL academy-row lock serializes a priced claim, the tenant/user cost sums and reservation insertion. The worst-case cost is rounded **up** to six decimal USD places. Reserved or unknown usage consumes the estimate; a reconciled row consumes observed actual cost. Price rates and token limits are stored with each reservation so a later pricing change cannot silently reprice it. Reconciliation checks the snapshot, rejects over-limit observations, writes one actual entry and audit atomically, and is idempotent.
- A deliberate stale-claim recovery operation marks an `Executing` task/execution as `OutcomeUnknown`, retains its reservation as `UsageUnknown`, and records one attempt and audit in one SQL transaction. It requires a UTC cutoff at least five minutes old; it never calls the provider or asserts that the previous effect failed. The synthetic completion path now takes the same academy lock and declines to overwrite a recovered terminal state.
- No new HTTP endpoint exposes priced reservation or recovery. Existing synthetic quota, R0 policy, default-off behavior and no-action scope remain unchanged. This is **not** an approval workflow or authorization to invoke a model.

## Evidence

| Check | Result |
| --- | --- |
| Full API suite | PASS 1,055/1,055, 0 skipped. |
| SQL harness build | PASS, 0 warnings/errors. |
| EF model | `has-pending-model-changes`: none. New migration `AddPentaPricedBudgetSnapshot`; two unrelated pre-existing precision warnings remain. |
| Real Identity/HTTP/disposable SQL | Final run `5a521f6c085a457aba4c3bda2e5e9a6f` PASS, 85 application + 7 Identity migrations. Two concurrent 0.6 USD claims against a 1 USD daily cap admitted exactly one; missing version denied; unknown reservation blocked another claim; observed 0.3 USD reconciled once and released only the unused 0.3; mismatched price and excess tokens denied. Synthetic replay/tenant quota/role checks retained. |
| Fault and recovery | Test-only EF command interceptor failed an audit write without SQL DDL privileges. Reservation plus audit transaction rolled back. A separate completion-audit fault left the committed claim `Executing` with reserved usage but no success/attempt/audit; restart replay did not redispatch. Deliberate stale recovery wrote `OutcomeUnknown`/`UsageUnknown` plus audit/attempt, and replay remained 502 without dispatch. A held fake-provider call finishing *after* recovery could not overwrite unknown state or release usage. |
| Cleanup | Run-owned database, login and container removed; negative mismatched-owner cleanup guard passed. `QA/EVIDENCE` stayed local. |

The first fault attempt used a SQL trigger, but the run-owned least-privilege login correctly denied trigger creation. It was replaced with scoped test-only command interception; the failed setup run was not counted as a pass.

## Still open

- Approved provider/account/region, privacy/data-processing and retention terms, exact production pricing/catalog source, live budget configuration/ownership and actual model adapter are **not selected**. No customer data or live model calls are allowed yet.
- Reconciliation needs the eventual provider's trustworthy usage event and reconciliation identifier. An observation above reserved tokens/cost remains unresolved, not charged below the estimate or silently retried. Budget policy changes across a day need product governance before live use.
- Recovery is deliberate, not an automated lease monitor. A time cutoff alone cannot prove the host is dead; a live call beyond the cutoff could still finish. The pilot tests the guarded finalization path, but production recovery needs host/lease and external-effect reconciliation policy. No blind retry.
- R1 actionable approval, security acceptance, physical-device/browser coverage, enterprise P0 issues and all pre-Azure release gates remain open. Do not merge, push or deploy from this report.

Next route: Codex / GPT-6.1 Sol / High / Default mode only for the next security-sensitive PENTA authority or recovery boundary. Sol Medium/Cursor for bounded documentation and UI once the contract is approved; Astra only for an unresolved consequential design choice.
