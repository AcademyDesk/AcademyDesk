# Phase 2B — Payroll historical identity bounded repair

Date:2026-10-12 IST. Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`; source parent `3f40fd04d1e118d16866e0a0f1c1eb7b6364337d`. Existing enterprise continuation, not an audit restart. BUG-DATA-0056 remains **OPEN / RETEST**. Sol High for finance/transaction/security acceptance; Sol Medium for settled Manual visual rollout.

## Frozen reproduction before application changes

An opt-in real Identity/TestServer/SQL harness exercised the original API before repair. A Teacher profile named Synthetic Worker A recorded gross1000/deductions200/net800. Renaming its profile changed the existing GET row to Worker B; changing type/link changed Teacher to Staff; test-only removal of the owned profile hid the SQL payout from GET. Those three historical acceptance checks failed. Money, payslip, period/reference/time remained unchanged; deactivation alone retained visibility and rejected new payout400.

Baseline run `05e1ea2aca0e4bf8a40558bf17275426`, started `2026-10-11T19:39:17.6721507Z`, SQL loopback port62042,81.6s,88 application/7 Identity migrations.24 supporting controls PASS, three historical failures **REPRODUCED / NOT ACCEPTED**. Diagnostic exit0 means expected defects were observed and cleanup passed, not repaired acceptance. Unrelated dirty Mini code was present in the local build; this batch neither accepts nor publishes it.

## Implementation and truthful legacy behavior

- Five nullable payout columns: worker-name/type snapshots, TeacherId/StaffUserId snapshots and server `IdentityRecordedAtUtc`. New payout values come from the server-resolved same-academy profile; request/model identity is not authoritative. Linked IDs are not added to the public payout DTO.
- Profile edit and payout share a profile-first SQL `UPDLOCK,HOLDLOCK` query. They use the existing finance-filter transaction/audit in normal API requests, or own a SQL transaction when none exists. Identity and effective payout parameters come from one locked profile state; inactive validation happens after acquiring it. No financial formulas, authority, permitted tools or payroll policy change.
- Payout GET is filtered by **payout.AcademyId**, no join to a mutable/current profile. Complete recorded name/type/timestamp renders `PayoutSnapshot`. Legacy or incomplete identity renders `Historical identity not recorded`, `Unknown (legacy)` and `LegacyUnverified`. Old/orphan rows remain visible within their own academy; a foreign profile link is never a reason to expose that profile's identity.
- Additive response fields are `IdentitySource` and `IdentityRecordedAtUtc`. The latter is normalized as UTC on reads and is recording-time provenance, not proof that money was transferred or that backdated PaidAtUtc reflects a verified bank event. Existing payout metadata and money remain unchanged. The existing Manual payroll row renders the name/type labels; no frontend file was changed and no new browser acceptance is claimed.
- The migration `20261012121216_PreservePayrollPayoutIdentity` adds exactly five nullable columns, no defaults/backfill/data replacement/money/FK change. Model and generated snapshot match. Historical identity is not reconstructed from today's profiles. Existing application paths do not edit snapshots; this does not make SQL administrator changes impossible.

## Verification and receipts

| Gate | Result |
| --- | --- |
| Retained PayrollNet/AuditOutcome tests before repair |52 PASS (console receipt; not a saved log) |
| Final targeted unit/controller/model/migration tests |66/66 PASS:14 new +52 retained;0 fail/skip,3s |
| Isolated final SqlHarness build |0 warnings,0 errors,8.81s |
| TypeScript using installed web compiler |PASS |
| Final real SQL history controls |34/34 PASS; zero historical acceptance failures |
| Previous-schema legacy migration preservation |PASS: exact pre-column orphan payout values, five NULL fields, model parity |
| Retained monthly/SessionBlock SQL rules |14 PASS; additional zero-net Paid observation stays POLICY-PENDING |
| Wider suite / browser / physical devices / measured concurrency |NOT RUN in this batch |

The14 new tests cover Teacher/Staff snapshots, recording timestamp distinct from backdated PaidAtUtc, four profile changes, orphan visibility, four incomplete legacy cases, payout-tenant scope, model parity and exactly-five-nullable-column migration shape. Retained monthly/SessionBlock gross/session/deduction boundaries are unchanged; zero-net Paid policy is not silently approved.

The34 native controls cover exact new payout identity/money and audits; rename/type/link/deactivation; retained old identity/metadata after later payouts and owned profile removal; four local legacy/current/orphan/foreign-link/incomplete fixtures; positive foreign payout sentinel exclusion; anonymous401, Teacher403, foreignAdmin403, localAdmin foreign-route403, missing edit404/payout400, inactive payout400. Rejected/read operations preserve canonical all-row/field snapshots across **PayrollProfiles, PayrollPayouts, AuditLogs, Invoices, Payments**. Successful writes change only the intended row plus one exact actor/tenant/action/route audit. These snapshots are scoped to five tables, not every database table.

Audit INSERT is denied only to the restricted runtime login on the marked owned AuditLogs table. Same-action profile PUT and payout POST return500 with full rollback. Permission restoration occurs in finally; subsequent successful profile edits and a new Staff payout net900 produce one row and audit, while original Teacher payout net800 and legacy/foreign/finance records remain intact. Reads also recover. No limiter disablement or429 occurred.

| Run | Started UTC / loopback SQL port | Outcome |
| --- | --- | --- |
| Frozen `05e1ea2aca0e4bf8a40558bf17275426` |2026-10-11T19:39:17.6721507Z /62042 |24 supporting controls +3 reproduced historical failures;81.6s,88/7 migrations; diagnostic only |
| First fixed `5882c7fe54e14e6b9b168eb0dea3df74` |2026-10-12T12:16:49.1613290Z /56149 |32 guarded controls + migration proof;58s,89/7 migrations |
| Final strengthened `0932e043f3714333a106f29b5de235e1` |2026-10-12T12:21:49.4167056Z /53163 |34 guarded controls + migration proof;57.9s,89/7 migrations,exit0 |

All three runs verified exact DB/context/runtime-login ownership, two-tenant preflight and mismatched-marker cleanup rejection. Their own databases/logins/containers were removed; nine pre-existing container IDs remain. No other container was pruned/restarted, no production/customer DB was used.

Private local receipts: `QA/EVIDENCE/payroll-history-20261012/sql-baseline.txt`, `sql-final.txt`, `sql-confirmed.txt`, `tests-final.txt`, `build-final.txt`, `build-confirmed.txt` and initial build receipts. They remain intentionally untracked. Initial harness compilation used the wrong IdentityDbContext namespace and failed twice; corrected harness build passed before baseline, not an application defect. EF generation reported inherited PassingPercent/Invoice.AdjustedAmount precision warnings; final harness build is0/0. First fixed output had confusing baseline-oriented PASS labels and an overly broad migration-NOT-RUN summary; final harness corrects wording, retains assertions and adds successful post-fault payout recovery. Intermediate receipts were not erased or overwritten.

Reproduction commands (from this worktree; no secrets required in the report):

```powershell
dotnet build QA/tools/SqlHarness/SqlHarness.csproj --artifacts-path .build-check/payroll-history-sql --no-restore --nologo -v minimal
$env:QA_PAYROLL_HISTORY='guarded'
powershell -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module Payroll
dotnet test tests/AcademyDesk.Api.Tests/AcademyDesk.Api.Tests.csproj --artifacts-path .build-check/payroll-history-tests --filter 'FullyQualifiedName~PayrollHistoryTests|FullyQualifiedName~PayrollNetTests|FullyQualifiedName~AuditOutcomeTests'
node apps/web/node_modules/typescript/bin/tsc --noEmit --project apps/web/tsconfig.json
```

Executed runner used bundled pwsh explicitly. `QA_PAYROLL_HISTORY=baseline` is only for frozen pre-repair artifacts; it is not an acceptance mode. Runner validates mode before creating SQL and uses the dedicated compiled artifact path. EF generation used safe Testing configuration without connecting to a database; only disposable runs applied migrations. This batch did not rerun frontend production export or entire enterprise tests; previous83-page export and invoice111/45/20+9/linked evidence remain historical, not new payroll proof. The older QA validation evidence blocker (`QA/EVIDENCE/logs/observed-checks.json` absent) is not cleared here.

## Remaining gates / next batch

1. **Sol High next:** measured SQL-overlap profile rename/type/link/pay-parameter update vs payout, and deactivation vs payout; validate consistent identity/money and audit/rollback in actual overlapping schedules. Row-lock source alone is not measured race acceptance.
2. Linked Payroll browser with authoritative API/SQL, Admin/denied-role/tenant/stale/recovery/legacy-label states, physical Android/iOS, direct/platform and critical regressions. No production DELETE route is added; profile removal fixture is test-only owned SQL.
3. Representative existing-database migration, backup/restore/downgrade/release validation. Up preservation here is bounded; Down drops identity columns and loses snapshot data, so downgrade needs explicit backup/release review. No downgrade was executed.
4. Provenance-backed legacy identity recovery (if approved), profile-link validation, duplicate-period/idempotency/sub-cent rules and zero-net/disbursement policy review are separate work, not solved or authorized by this snapshot repair.

BUG-DATA-0056 stays OPEN. No real salary transfer, main merge, Azure deployment, PENTA/Core contract change, model upgrade acceptance or new testing plan. Manual Orbit + Aura dark/Prism light foundation and all-portal rollout continue separately; AI frontend is Mini-owned FW1, not delivered. Latest Mini86/94 remains NOT ACCEPTED.90% eligible-AI/10% Manual is the product target, not a readiness percentage. Preserve the existing enterprise gates and unrelated14 tracked changes/private evidence; only this scoped feature checkpoint is eligible for normal feature publication.
