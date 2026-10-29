# M1 — actual HTTP / Identity / disposable SQL readiness regression

2026-10-15. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, HEAD `b1f8cb6`. Continuation of [M1 implementation](MEETING_READINESS_M1.md), not a new enterprise test plan.

## Changes and execution

- Added `QA/tools/SqlHarness/MeetingReadinessRegression.cs`; integrated `MeetingReadiness` into the existing runner and strict switch allowlist/dispatch. Prior harness/runner dirty work retained. Existing assertions/cases were not relaxed. New module uses the existing guarded factory, authenticated Identity logins, least-privileged runtime SQL user, both migration sets and ownership-verified teardown.
- Isolated build: `dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/meeting-readiness-sql --verbosity quiet`. Final receipt-enabled rebuild used `--no-restore`;zero errors, four NuGet advisory warnings. No production application-source changes in this batch.
- Windows PowerShell5 `-File` was blocked by that shell's execution policy before running the script; no SQL container was created by that attempt. Used the current configured PowerShell7 shell (`RemoteSigned`) with `& ./QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module MeetingReadiness`;no execution policy changed or bypass flag used.
- First successful run `284686d70bdd4895993c85961b15322f` passed all guards and teardown. Its meeting lines were filtered by the existing runner's output allowlist;added the `MEETING` prefix and a per-case/hash JSON receipt,then rebuilt and repeated in a new owned container. Not counted as42distinct scenarios.

## Final evidence

Run `e52ee05e9f6e4208ac0b34fd780577c3`, loopback SQL port55055, database `AcademyDesk_QA_e52ee05e9f6e4208ac0b34fd780577c3`, container `academydesk-qa-e52ee05e9f6e4208ac0b34fd780577c3`.

Receipt: `QA/EVIDENCE/meeting-readiness-sql-e52ee05e9f6e4208ac0b34fd780577c3/result.json`. Contains21case labels/statuses,unchanged channel/audit/notification snapshot assertions and source SHA256 values. No access tickets,passwords,connection strings or private fixture field values recorded.

Final acceptance PASS:21/21cases,no429,runner exit0 in42.7seconds anddatabase/login/container teardown confirmed. Cases cover own Owner/Admin200,foreign route/actor403,anonymous/malformed bearer401,Teacher/Student/Guardian/Manager/FinanceUser/custom communications grant403,platform-owned andforeign flag bypass403,revoked Owner role with unchanged old ticket403,disabled/malformed Engagement entitlement403,inactive academy403,arbitrary provider text still unavailable200,server tenant change403 andinactive account403. Every allowed response has three fixed unavailable providers,all five operations false,empty supported settings,no-store andno private fields. Every GET keeps domain channel/audit/notification snapshots unchanged.

Existing general realHTTP controls also check health,anonymous protection,real login,bearer access andtwo-academy student isolation. Runtime inventory327routes/316controller method-routes/10Identity method-routes,hash `D0366C3B8FD1DD8819CAA5629B669B3CF98AFCF5CDB5FEF45749FDB5EA9A3069`. Existing migrations90application/7Identity andruntime-login scope checks retained. No new schema migration introduced.

## Security findings and limits

Fresh isolated restore reported NU1902/NU1903 for transitive `System.Security.Cryptography.Pkcs/9.0.11`. Resolved assets identify parent `Microsoft.Data.SqlClient/6.1.6`. Advisory IDs from NuGet: `GHSA-2fmv-rv2c-59x7` (moderate) and `GHSA-846f-f764-76mx` (high). These warnings are NOT suppressed,not remediated andnot explained away by passing endpoint tests. Dependency review/compatible patched resolution andregression remain open before production;do not extend the earlier cached no-restore API0warning claim to this fresh dependency build. No dependency/lockfile edit in this batch.

This endpoint remains read-only/unavailable. No OAuth/provider calls,invitations,meeting creation/history implementation,frontend enablement,PENTA tool activation orlive customer data. Prior87unit/HTTP-boundary tests andmeeting18/30visual evidence retained,not rerun here. SQL readiness proof is not a complete provider,integration,device,security orenterprise release pass. No issue orrelease gate closed globally.

## Next

Sol High:M2 typed deterministic preview/strict DTO validation andversion-bound approval/durable-operation design,while provider dispatch stays disabled. Track dependency advisories as an explicit pre-Azure security item. M3 qualified secure provider/M4 Manual+PENTA wiring andexisting academic/portal visuals/UI2/Mini/device/E9–E10 gates remain pending. No Mini contract change orproject switch needed for host-domain work.

No main/staging/commit/push/Azure orproduction database change. Preserve all earlier dirty work/QA evidence. Only exact owned disposable database/login/container andguard-verified host scratch paths may be removed;no pruning. Existing `pentaaimodels-api-1` and `pentaaimodels-inference-1` remain out ofscope. Owner full app→authorized go-live→Hayansh→Academy Desk website order retained;90%AI remains a target,not measured coverage.
