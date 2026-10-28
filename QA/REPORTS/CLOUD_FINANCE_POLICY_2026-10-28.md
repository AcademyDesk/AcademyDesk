# Approved first-release finance policies — 2026-10-28

MODEL: Session Codex model; no override or delegation.
TOOL: Codex managed-cloud terminal.
REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/release-export-security; base dae8e8367240effb0b461ae21079906358451ec1.
ENVIRONMENT: Isolated synthetic controller/InMemory fixtures; SQL harness compiled.
TASK: Implement the user's explicit payroll/restoration decisions for critical
BUG-DATA-0003 and BUG-DATA-0010.
MAY MODIFY: Payroll/payment controllers, focused unit/guarded SQL cases and QA.
MUST NOT MODIFY: Customer records, schema, RBAC, unrelated business policies.
VALIDATE: Reproduce new policy gaps; exact accepted/rejected ledger states;
full API regression; compile the existing guarded SQL harness.
STOP WHEN: Tested policy increment pushed; real SQL/critical limits recorded.
NEXT ROUTE: Codex for CI-backed real SQL/private-file and production-gate checks.

## User decisions and behavior

The user explicitly selected:

1. **Block zero-net payouts for the first release.** Deductions must now be
   strictly less than effective gross. Equality and greater deductions reject
   before a payout is saved. Existing negative-deduction, positive-gross, active
   profile, model applicability, session and tenant rules remain.
2. **Allow validated voided-payment restoration with balance recalculation.**
   Both the Completed status action and dedicated reconciliation action validate
   the same-tenant invoice, positive stored amount, matching currency and active
   invoice lifecycle. Restored money plus other Completed/Reconciled collections
   cannot exceed TotalAmount minus approved AdjustedAmount. Other Voided amounts
   are excluded. Accepted restoration recomputes Paid/PartiallyPaid; cancelled,
   missing/foreign, invalid and overbalance cases leave captured rows unchanged.
   Dedicated reconciliation still requires and records its trimmed reference and
   timestamp; generic reconciliation remains denied. Historic reconciliation
   evidence is retained by the Completed path.

No bank/provider was contacted. Existing financial action/audit transaction
behavior is retained. This increment does not add serialization across concurrent
invoice/payment writers or claim exactly-once requests.

## Evidence

- Fresh baseline: original full API 1,073 PASS after retrying outside the cloud
  sandbox, which blocks MSBuild named pipes.
- Initial approved-policy suite: 51 tests, 33 FAIL/18 PASS. Added 32 restoration
  cases plus three payroll equality boundaries to the existing payroll suite.
- After repair: all 51 focused tests PASS. Full API **1,108 PASS**, zero failed or
  skipped. Rejected unit cases reload persisted InMemory rows before comparison;
  this is not SQL transaction/concurrency or full HTTP authorization evidence.
- Guarded SQL harness compiles: zero warnings/errors, isolated output
  .build-check/production-sql/. Payroll SQL expectations now enforce zero-net
  rejection. Added 12 restoration HTTP/SQL cases and strict full-settlement
  restoration readback. SQL execution is pending; compilation is not a pass.
- Frontend source/dependencies unchanged in this increment. Prior1,095/1,098
  frontend result retained, including three missing historical SQL-log failures.
- Ignored logs: .build-check/production-api-baseline.log,
  finance-policy-before.log, finance-policy-after.log,
  finance-policy-full-api.log, production-sql-build.log.

## Access and remaining release work

The user also authorized needed access. Added only `api.github.com` to the
existing saved restricted-network draft, preserving existing domains and all
repository/credential bindings. Save confirmed with requires_publish=true.
The new GitHub API rule and prior SQL/model CDN rules are still unapplied to
the observed runtime. The user must review/save and publish through environment
settings to activate them. No environment publication was performed here.

GitHub API inspection returned Forbidden under the enforced host policy; no
Actions result is claimed. Real SQL/migrations/rollback, concurrent collections,
authorization/audit behavior on the new restoration paths, browser/device and
full critical acceptance remain OPEN. Neither affected P0 is closed by unit tests.
No configured application DB, customer record, Azure deployment or main merge
was used. Recurring feature-branch pushes remain authorized.
