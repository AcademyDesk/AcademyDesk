# Holiday DELETE routing — bounded repair and native SQL acceptance

Date: 2026-10-11. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, starting HEAD `d6215bc5d983dee779baa9c2a18a57708bbaa0e2`.
Issue: [BUG-API-0008](../ISSUES/BUG-API-0008.md), OPEN. Continue the existing E0 queue; no audit restart or new testing strategy.

## Reproduction and repair

Actual MVC discovery found both `HolidayDeleteController.Delete` and `HolidaysController.Delete` for the same constrained DELETE URL. Four tests failed on starting application source: one uniqueness test (two candidates), three HTTP cases returning synthetic 500 only when the actual routing middleware throws `AmbiguousMatchException`. This is a TestServer/EF InMemory reproduction, not a production or SQL observation.

Added `[NonController]` to the retained legacy HolidayDelete class. No file deleted, no new endpoint, route/payload/controller business logic changes or permissions added. `HolidaysController.Delete` remains canonical. Both controller names retain existing Certificates module mapping; the existing academy access/audit filters remain untouched.

## Evidence and limits

- `QA/EVIDENCE/holiday-route-20261011/baseline.trx`: 0 PASS /4 FAIL; expected ambiguous routing/endpoint duplication.
- `final.trx`: 4/4 PASS: one canonical MVC DELETE, local204/exact foreign sentinel preservation/repeat404, foreign and missing record404/no deletion.
- `regression.trx` and final rebuilt `regression-final.trx`: 40/40 PASS, comprising4 new routing tests +36 retained AuditOutcome tests. Not40 real SQL cases and not the entire API suite.
- SqlHarness build with `.build-check/holiday-route-sql`: 0 errors/0 warnings. Initial compile failed because the framework ambiguity exception type is internal; exact fully qualified exception-name filter corrected that without general exception masking. Test host uses current HostBuilder/TestServer APIs after removing initial obsolete-host warnings.
- New `HolidayRoute` module reuses the existing guarded disposable-SQL runner. It specifies11 endpoint/SQL/Identity/HTTP checks: canonical discovery; anonymous/Teacher/foreign actor/foreign route/foreign record/missing record no-write; suspended academy/module-disabled no-write; local204/exact row/audit; repeat404/no-write. Every status assertion rejects429. No existing module weakened.
- Initial native attempt blocked: Docker engine pipe `dockerDesktopLinuxEngine` unavailable. Installed local Docker Desktop launch requested; daemon later became ready. Windows PowerShell then rejected the script under its execution policy before creating any fixture. Both failures retained; no global policy change.
- **Native acceptance 11/11 PASS**, run `33433a0001e14ce0b93ca29077806214`, captured in `QA/EVIDENCE/holiday-route-20261011/sql-final-2.log`. Real Identity/HTTP/disposable SQL: one canonical endpoint; anonymous401, Teacher403, foreign actor/route403, foreign/missing record404, suspended/module-disabled403 all leave holidays/audits unchanged; authorized204 removes exactly the local row, retains the foreign sentinel and persists the actor audit; repeat404 makes no changes. No429.
- Runner exit0, elapsed32.2 seconds;88 application+7 Identity migrations; mismatched-marker cleanup refused; exact run-owned database/login/container removed. Docker inventory confirms this run's container absent, two PENTA services and prior retained containers untouched. Successful ordinary audit persistence is proven; audit-fault atomicity is not tested in this packet.

## Reproduce without rewriting the fix

With the local Docker engine available (use installed PowerShell7, without changing global execution policy):

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/holiday-route-sql --nologo -v quiet
pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module HolidayRoute
```

From this worktree only. Expected11/11,88 application+7 Identity migrations and exact runner-owned database/login/container cleanup. SQL baseline mode `QA_HOLIDAY_ROUTE_BASELINE=1` is only for the pinned duplicate application source; it expects two candidates plus ambiguity and unchanged holiday/audit snapshots, not a repaired-source pass. It has NOT been run here.

## Publication and next work

Native acceptance is complete; this scoped packet is prepared for a logical feature-branch commit and normal push under the standing publication directive. Existing Mini dirty work, continuity32/28 lines, QA/EVIDENCE and interrupted Platform overlays remain excluded/preserved. No main edits, merge, Azure, production data/credentials or Mini contract changes. Earlier frontend/build/domain proofs retained, not rerun as part of this backend packet. Issue/browser/physical-device/full-critical/release gates remain OPEN. Publication SHA/result is reported by the final handoff, not invented here before the commit.

Owner reaffirmed the new Manual frontend must match PENTA AI: Orbit + Aura dark /Prism light across Platform/Admin/Teacher/Student, with reusable responsive foundations. AI frontend is owned by the PENTA AI project; Academy owns matching Manual visuals and host integration. Earlier standalone-Aurora wording is historical, not authority to ignore this directive. Interrupted overlay work is not declared accepted or a completed redesign.

Next Sol High: existing unverified metadata-shape BUG-API-0004 or next higher-risk reproducer based on current evidence; no repeat of completed finance/onboarding fixes. Sol High for the cross-portal visual foundation/host contract, Medium for settled rollout. No Mini handoff necessary for this route repair.
