# Phase 2B — Finance Governance Collections access and feedback

Historical checkpoint. The subsequent [remaining Collections balance repair](PHASE_2B_COLLECTIONS_BALANCE_REPAIR.md) supersedes the gross-exposure limitation and current source/assembly counts below. Original access/feedback evidence remains retained.

2026-10-01. **Bounded local repair PASS: API 201/201, controlled frontend 22/22, two final fresh SQL runs ×82 cases.** BUG-FUNC-0006, BUG-FUNC-0003 and Phase 2B remain OPEN. Agreed Sol High; accepted Astra finding reused, no repeat audit. No commit/push/Azure deployment or normal development database changes.

## Bounded change

[FinanceGovernanceController](../../apps/api/Controllers/FinanceGovernanceController.cs) adds `GET collection-tasks` and `PATCH collection-tasks/{id}` under the existing finance-governance route. Existing `finance.manage`, FinanceControls module, active-user/academy and tenant rules apply; Admin/Owner retain existing override after tenant/module checks. Generic AdminWorkItems permission remains unmapped/restricted. No global filter/role/module mapping or schema change.

Both queries require **task.AcademyId == route academy and task.Type == Collections**. The nine-field DTO exposes id/type/title/description/priority/status/dueAtUtc/escalationStage/promisedPaymentDate, not academy/assignee/entity-link/completion metadata. GET uses AsNoTracking and orders due/priority/ID. It retains same-academy manually created Collections tasks and closed rows, matching the old type-filtered contract; the existing page filters completed/cancelled rows out. No new invoice-link or assignee-only business policy is inferred.

PATCH accepts only existing `CollectionStateRequest`: four exact escalation stages and an optional promise date. It modifies **only stage/promise**. Missing/foreign/non-Collections IDs return 404, invalid stages/dates 400. Unknown JSON assignment/type/academy/status/title/entity fields cannot overpost the DTO. Assignment, entity links, status, completion and other fields are preserved. Existing finance transaction/audit boundary now covers this PATCH; no new shared boundary code.

[Finance Governance page](../../apps/web/src/app/finance-governance/page.tsx) replaces only the generic collection register GET/PATCH dependencies. `load` also accepts a success notice: follow-up creation, escalation update and adjustment decision keep confirmation after refresh. A confirmed save followed by failed refresh reports both success and a refresh warning, not a failed save or blank notice. Follow-up creation returns to the register by closing its selected-invoice form. Failed writes retain error guidance. Document settings already had separate confirmation and are unchanged. No global form/visual redesign.

## Reproduction and verification

[Before application hashes](PHASE_2B_GOVERNANCE_BEFORE_SOURCE_SNAPSHOT.json) pin the two edited sources. [Controlled actual-TSX tests](../tools/finance-governance.test.cjs) before edits: **nine tests, one PASS/eight target expectations FAIL**, [corrected baseline](../EVIDENCE/logs/phase-2b-governance-before-client-corrected.log). Four route expectations fail against the old generic GET/PATCH. Two success-message tests with an allowed admin lookup isolate immediate clearing to empty; two failed-refresh controls lose the confirmed-save notice. These are controlled handlers, not eight distinct browser-reproduced bugs. The [initial baseline](../EVIDENCE/logs/phase-2b-governance-before-client.log) is retained but its generic-denial fixture did not isolate notice clearing, so it is not the authoritative notice baseline.

Final nine Governance cases cover allowed/denied initial loading, successful/denied escalation save, nullable date, visible confirmation, confirmed save with failed refresh, and follow-up/approval confirmation/reset under successful/failed refresh. Combined with the prior 13 lookup/payment controls: [22/22 PASS](../EVIDENCE/logs/phase-2b-governance-client.log). Controlled hooks/API/FormData/window doubles execute application TSX, not live React/browser/native-device/end-to-end click proof.

[Fourteen API units](../../tests/AcademyDesk.Api.Tests/CollectionTaskTests.cs) cover tenant/type/DTO minimization/no tracking, four stages, optional clearing, five invalid stages and three wrong-target guards. Isolated InMemory is not an authorization/transaction substitute. Full [API suite 201/201 PASS](../EVIDENCE/logs/phase-2b-governance-api-tests.log). [Final harness build](../EVIDENCE/logs/phase-2b-governance-final-build.log) zero warnings/errors; [frontend typecheck](../EVIDENCE/logs/phase-2b-governance-typecheck.log) exit 0/no diagnostics; targeted [lint](../EVIDENCE/logs/phase-2b-governance-lint.log) zero errors/**three existing warnings** (effect dependency/two img). Full frontend lint/production build not run.

[Real Identity/HTTP/SQL module](../tools/SqlHarness/FinanceGovernanceRegression.cs), **82 primary cases per run**:

- Ten actors × GET/PATCH (20): Admin/Owner/FinanceUser/custom finance role allowed; Teacher/Student/FrontDesk/Operations/student-only custom/no-finance custom denied. Four FinanceUser prerequisites all 200.
- FinanceUser/custom generic GET/PATCH remain 403 (four); Admin/Finance foreign-route GET/PATCH 403 (four); foreign/non-Collections/missing target 404 (three).
- Five invalid stages 400; four valid stages; nullable/omitted dates and overposting (two); malformed date/missing stage/malformed GUID controls (three); anonymous GET/PATCH 401 (two). Completed/cancelled metadata-preservation controls (two), not new reopening-policy approval.
- Grant absent/valid/wrong-academy/expired/revoked × read/edit (ten); Admin/FinanceUser FinanceControls disabled/Controls-only enabled × read/edit (eight); inactive academy/user read/edit and reactivated token (five).
- Actual FinanceUser follow-up create/readback (two), audit INSERT-denial edit/create rollback and recovered edit/create (four).

Successful reads assert the exact nine-field own-type ordered projection against fresh SQL, including nullable notes/dates and closed-row status. Successful edits compare the **entire captured task table**, permitting only the chosen row's stage/promise changes; unrelated/foreign tasks and assignment/entity/status/completion fields remain identical. Creation verifies one own invoice-linked unassigned task, its response ID and no unrelated changes. Each accepted write adds exactly one current actor/tenant/method/route audit. Read/rejection/fault snapshots preserve captured people/branches/academies/settings/grants, selected Identity user/role/link metadata, work items/audits and invoice/payment/adjustment/payroll/notification state. Credentials/uncaptured tables are excluded; no all-database claim.

Fault injection changes INSERT permission only on owned AuditLogs after marker validation. PATCH and existing follow-up POST each return 500 with all captured state unchanged, then succeed after permission restoration. This verifies new-action enlistment in the unchanged finance boundary; not distributed transactions, cancellation, commit/response-loss, idempotency or full critical-suite acceptance.

| Stage | Run / loopback SQL / UTC start / elapsed | Evidence |
| --- | --- | --- |
| Preliminary | `c8cb57adcfa74129847505fe0b399d7c` /50975 /08:16:22.4877962 /82.8 s | [log](../EVIDENCE/logs/phase-2b-governance-run1.log): 82 PASS, cleanup exit 0 |
| Final 1 | `f7aa396ab0074b40b011e9d4286424e0` /53011 /08:19:07.9935387 /91.2 s | [log](../EVIDENCE/logs/phase-2b-governance-final-run1.log): 82 PASS, cleanup exit 0 |
| Final 2 | `de15cfcb5fec4c3ea859854bc436d10a` /58856 /08:21:04.7972105 /87.7 s | [log](../EVIDENCE/logs/phase-2b-governance-final-run2.log): 82 PASS, cleanup exit 0 |

After preliminary PASS, the QA list comparison was tightened to obtain expected ordered rows through SQL, avoiding CLR Guid-vs-SQL uniqueidentifier tie-order differences. Preliminary is retained separately, not one of the two final identical-source runs. Application sources did not change. Health/real login/two-tenant preflight are secondary controls, not double-counted. Actual runtime inventory **310 routes/299 controller method-routes/10 Identity method-routes**, digest `EB8940A5BC276EBA1E4273A2DAB59629964475CB2C58486244B8DB4F873E55BC`; two new actions explain the increment. Static Astra inventories remain historical, not regenerated. Migrations remain 80 application/7 Identity; stores use exact owned SQL/runtime login.

## Capture, cleanup and limits

[114-record snapshot](PHASE_2B_GOVERNANCE_SOURCE_SNAPSHOT.json): three previous captures change (Governance controller, QA entry/wrapper); **106 previous captures unchanged**. Five additions: existing Governance page newly captured, three new test/module files, scoped validator. Exactly two application files change this slice; unrelated dirty work preserved. HEAD `20bb6047f9edf733ac8e2a226621cc582ec54b3c` unchanged. Final API SHA256 `e3d1b720e3869bfb6458dfd06c8e5fb5ed4291aefc6f97302ebc832748180c5d`, QA harness `556172d27457a2fa48e85ea7579367956d5eb397593dfdfefcf3738b1616a8c0`. These are bounded captures, not all repository files.

[Scoped validator](../tools/validate-finance-governance.cjs) checks hashes/HEAD/before capture/logs/links and independent cleanup, [log](../EVIDENCE/logs/phase-2b-governance-validation.log). Cleanup rejects a false ownership marker and removes only each run-owned database/login/root/container. Synthetic fixtures are reproducible; normal dev/Azure/older retained resources untouched. No listening frontend/backend started, machine policy change, commit or deployment.

BUG-FUNC-0006 remains OPEN pending actual browser/critical acceptance and the earlier Finance-only Invoice settings coupling. BUG-FUNC-0003 remains OPEN: only this Governance feedback variant repaired, not its many other forms. Existing generic-work-item assignment/entity validation, future-only promise-date policy, deep free-text classification, completed-task mutation policy, scale/races/platform-owner bypass/global response-loss/duplicate-submit/device/visual gates not certified. Gross Collections invoice exposure, currency grouping, restoration/zero-net/media/release issues remain separate and unfixed.

## Next bounded task

Accepted **BUG-DATA-0017**, Collections exposure showing gross invoice value instead of remaining collectible balance. Reuse canonical applied-adjustment/collected-payment rules; reproduce its specific case and repair with same-currency controls. **Sol High** remains appropriate. No repeat Astra audit, policy invention or Azure deployment. Browser acceptance and other phase gates remain queued.
