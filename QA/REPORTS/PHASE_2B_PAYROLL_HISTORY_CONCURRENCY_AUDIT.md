# Phase 2B — Payroll history measured concurrency and audit recovery

Date:2026-10-12 IST. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, application source `315b320631f3e7c8f8704dd4c152c479456cf124`. Existing BUG-DATA-0056 continuation, **OPEN / RETEST**. Sol High for finance/transaction/security/linked acceptance; Sol Medium for settled Manual visual rollout.

## Scope and proof boundary

QA-only successor to [historical identity repair](PHASE_2B_PAYROLL_HISTORY_IDENTITY_REPAIR.md). Add an opt-in race harness and runner guard; retain existing34 history controls, nullable legacy migration proof and monthly/SessionBlock rules without weakening or replacing them. No application, database model/migration, frontend, authority, financial policy or Mini/Core change. Existing unrelated14 tracked changes and private untracked material are excluded from publication.

Each pair holds a marker-validated **owned** SQL profile row with UPDLOCK/HOLDLOCK. Launch the first real authenticated HTTP operation and observe its actual SQL waiter; launch the second, then require **two** actual PayrollProfiles lock commands in the owned database blocking chain (including indirect queued blockers). Only then release the test lock. Task.WhenAll alone is not evidence. Marker validation is reused from the existing owned coordinator. Coordination failure releases the lock, drains launched responses and fails acceptance; requests are not retried. Fifteen-second waiter bounds and the unchanged application limiter remain in force.

Alternate first-launched operation, three repetitions each direction per mode. Submission order is not assumed to guarantee SQL scheduler order: assertions identify the actual serialization outcome from the resulting identity/pay values or inactive rejection, and final acceptance requires both actual outcomes in each mode. This run observed both in every mode. The controlled external blocker is a test device, not production code.

## Results

| Mode | Normal pairs | Actual edit-before-pay | Actual pay-before-edit |
| --- | --- | --- | --- |
| Monthly salary + name/type/link replacement |6 PASS |Staff/replacement, gross2000/deductions200/net1800 |Teacher/original, gross1000/deductions200/net800 |
| SessionBlock + name/type/link/rate/session replacement |6 PASS |Staff/replacement, gross2500/net2300/sessions5 |Teacher/original, gross1000/net800/sessions12 |
| Deactivation |6 PASS |PUT200/POST400; exact active-profile message, no payout/audit for rejection |PUT200/POST201; original active-profile identity/net800 retained afterward |

18 normal pairs +3 overlapped audit-fault pairs +3 same-profile recovered pairs = **24 measured pairs /48 mutation responses**. Successful normal/recovery responses total38, inactive rejection400 total4, audit-fault500 total6; no429. Counts exclude retained fixtures/history and read operations.

Audit INSERT denial applies only to the restricted runtime login's owned AuditLogs table, revalidating the run marker. Each mode's edit and payout are overlapped during denial: both return500 and full five-table state equals before. Permission is restored in finally. Repeat the **same profile and operations** after restoration, again with two SQL waiters: monthly/SessionBlock200/201, deactivation200/400 in observed edit-first recovery. Payout rejection follows the existing exact `Choose an active payroll profile.` message. Rollback/recovery are not merely read checks or a new fixture standing in for failed writes.

For every successful pair, compare canonical all-row/field snapshots in PayrollProfiles, PayrollPayouts, AuditLogs, Invoices and Payments. Expected changes are only requested profile fields, exactly one new payout if accepted, and one audit for each successful mutation. Original profile fields outside the request, all historical payouts, existing audits and foreign/finance sentinels remain exact. Payout name/type/linked IDs/gross/deductions/net/sessions/recording timestamp and every response metadata field must match one complete locked source version. Backdated PaidAtUtc stays distinct from current recording provenance; no real bank transfer is claimed. Old history remains original after edits/deactivation/new payouts.

Audit actor/tenant/entity/action/route/time and exact count are checked. Real GET200 verifies historical/new payout DTOs, no own-row omission or foreign-row leakage, and unchanged five-table state. These snapshots cover five named tables, not the entire database or every Identity side effect.

## Retained verification and receipts

- SqlHarness build:0 warnings/0 errors,35.86s, rebuilt against current local application snapshot.
-66/66 targeted PayrollHistory/PayrollNet/AuditOutcome tests rerun PASS,0 fail/skip,2s. Used the accepted unchanged application test artifact with `--no-build --no-restore`; this batch changes QA harness only, not those tests/application files. This is not a new full-suite build.
-34 guarded native history controls, legacy migration preservation/model parity and14 retained monthly/SessionBlock rules rerun PASS in the combined run. Additional zero-net Paid observation remains POLICY-PENDING, not a fifteenth accepted rule or owner approval.
- Invalid race configuration rejected before SQL/container creation. Race requires Payroll module, `QA_PAYROLL_HISTORY=guarded`, `QA_PAYROLL_RACE=1`. No baseline-diagnostic mode is accepted for races.
- Run `b056963c11194f54b512c976ec80dcaf`, started `2026-10-12T12:48:45.6112143Z`, loopback SQL port53071,108.6s,exit0.89 application/7 Identity migrations; exact DB/context/runtime-login ownership, real Identity login, two-tenant preflight and mismatched-marker cleanup refusal passed.
- Owned database/login/container removed; all nine original container IDs remain. No other container was pruned/restarted or production/customer database touched. No SQL assertion/build failure occurred in this run. An initial read-only hash capture encountered Git warning text; recaptured with stderr excluded, no file change or weakened assertion.
- Private receipts stay untracked in `QA/EVIDENCE/payroll-race-20261012/build.txt`, `tests.txt`, `sql.txt`. Previous baseline/intermediate/repair receipts remain local and unchanged. `.build-check` remains ignored. TypeScript/83-page export and invoice evidence are retained historical results, not rerun or converted into payroll browser proof. Existing missing QA observed-checks evidence blocker is not cleared.

Executed commands from this worktree:

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/payroll-history-sql --no-restore --nologo -v minimal
$env:QA_PAYROLL_HISTORY='guarded'
$env:QA_PAYROLL_RACE='1'
powershell -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module Payroll
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/payroll-history-tests --no-build --no-restore --filter 'FullyQualifiedName~PayrollHistoryTests|FullyQualifiedName~PayrollNetTests|FullyQualifiedName~AuditOutcomeTests' --nologo -v minimal
```

Runner execution used the bundled pwsh explicitly. Synthetic fixtures use invalid-domain accounts; no real credentials are recorded here. A limited added-line secret heuristic and source/scope review are publication checks, not comprehensive security certification.

## Next and limits

**Next Sol High:** linked Payroll browser against authoritative API/SQL: recorded history, honest legacy labels, create/edit/reload/denied/recovery behavior and responsive checkpoints. Then physical Android/iOS, direct/platform/stale/critical, representative existing-database migration/backup/restore/rollback and enterprise release gates. No issue closure from this bounded run.

This is not unbounded concurrency/load, every interleaving/role, duplicate-period/idempotency, client-response-loss or production acceptance. Payroll link validation, sub-cent rules, legacy provenance recovery and zero-net/disbursement policy remain separate approved work. Snapshot Down migration loses new identity data; no downgrade was executed here. No main merge, Azure deployment, new test plan or Mini handoff/model upgrade acceptance.

Manual Orbit + Aura dark/Prism light and all-portal rollout continue separately; AI frontend versioned FW1 remains Mini-owned/not delivered. Mini86/94 remains NOT ACCEPTED.90% eligible-AI/10% Manual is still the product target, not measured readiness. Preserve existing enterprise gates and accepted prior audits.
