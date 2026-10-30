# Shared student onboarding integration — 30 October 2026

## Scope and source

Worktree `D:\AcademyDesk-penta-student`; branch
`codex/penta-student-service-integration-20261029`; base published teacher
`c248912041ac55fbc84ea640fa5b355a54a4e45a`.
Owner's30October continuation explicitly authorizes committing/pushing validated
work and its office-laptop handoff. No main merge or Azure release is authorized.

Integrated existing source from Academy preservation commit
`6887e6eb26e88f8fc99eee7a00c267a14c12883f`: shared student domain service/rules,
Manual controller, strict non-credential field adapter, retained service/field
unit tests, and the21atomic HTTP plus7direct-service SQL regressions. Existing
protected-conversation regressions remain preserved on that source branch;
they are not imported, deleted or represented as accepted in this domain slice.

## Resulting behavior

- Manual student creation and future host-authorized callers share deterministic
  validation, optional/minor/guardian rules and SQL/Identity unit of work.
- The host owns the commit and required audit. Account/role/link or audit failure
  rolls back student, guardian, relationship, Identity user and membership rows.
- The service requires an authenticated principal and current stored active
  same-academy Owner/Admin authority. SQL holds academy/actor/membership/role
  checks through the host commit. Claims and stale tracked identities cannot
  preserve revoked permissions. Existing platform-marked actors still require
  same-academy membership; no cross-academy student/account capability is added.
- Current branch/module and student-number validation runs under the academy
  lock shared by Manual and direct service callers. Duplicate racing saves yield
  one200/one400 and one committed student/audit, without accounts or429.
- Adult guardian details without an account do not require an email. An actual
  requested parent account requires an email before any writes; missing/blank
  values return a clear400 rather than entering the null Identity email path.
- Created-account flags describe actual account creation. The existing Manual
  request record and response field names are retained. Blank student numbers
  normalize to null rather than conflicting as an empty unique value.
-29strict conversational fields exclude the four secure account inputs and all
  role/tenant/approval authority fields. The tool-name constant is decoupled from
  the not-yet-imported persistent draft service; no tool registration/AI dispatch
  is enabled. Future import must reuse this constant and the already-extracted
  PentaOnboardingBranchChoice record, avoiding duplicate declarations.

## Source-bound validation

1. First window `c47faf81014b475fa797cecde97c92c7`:1597API,0failed/0skipped;
   SQL `941c48bae939412bb0678837c96aa868`,47checks,5mixed races. Exact original
   services/images/API200/full Docker inventory and owned cleanup restored.
2. Null-email correction added9unit and3actual SQL cases. Window
   `d7e09b8194724075b164291aaa45ddf6` completed the full unfiltered1606API tests
   and harness build,0errors/warnings, but its SQL run
   `becd08cca6874d9c8d1301753915e779` was interrupted overnight. It has no accepted
   SQL completion or restoration result. Preserve this incomplete evidence.
3.30October explicitly inspected recovery: recorded PID9040 absent, exact retained
   lease nonce/scope verified and archived intact; no foreign process killed or
   guessed stale lock deleted. Both exact original services were restarted.
   Interrupted run-owned container is retained stopped for investigation; no
   claim of its database cleanup. No other containers/volumes/images were pruned.
4. Successor window `a7c520047e1245eba28c556ae0a0e822` reuses only the successful
  1606API TRX after exact application/test hash equality and counter checks.
   SQL harness rebuilt. Fresh SQL `ff5e776ca0274a02bc32c7ff89fb7416` PASS:
  21retained atomic HTTP +7retained direct service +22additive checks=50,
   including5mixed Manual/service races, current authority/tenant denial,
   optional/guardian semantics, three null/blank parent-account cases and
   actual SQL/Identity/membership/audit injected-failure rollback.
  84application/7Identity migrations, runtime privileges, ownership-marker
   negative cleanup, run-owned database/login/container cleanup PASS.
   Final exact original service IDs/images/running/healthy,API200 and full Docker
   inventory restoration PASS; shared validation lease released.
5.14production-gate guard fixtures, PowerShell parser, workflow YAML/module
   parsing and diff checks PASS. Focused new-source secret scan found no probable
   live secret; explicit disposable synthetic fixture values reviewed.

No schema/entity/migration, finance, teacher, web, model, dependency or production
configuration change. The current-source test subset is not an addition to, or
replacement for, the larger preserved local3183test program. No fresh GitHub CI
or browser/physical-device result is claimed for this branch. QA/EVIDENCE,
.build-check, secrets/model weights remain local and are excluded from commit.

## Integration/release still open

Another profile's `codex/release-export-security` advanced from10a60ee to
`a63e392` with8commits: payroll print, role-session/recovery/export verification,
assessment/attendance generation safety and work-item scope/assignee validation.
These are preserved and fetched, not included in this student checkpoint. Join
those changes coherently and retain both sets of checks before a combined release.

Next: protected host/session/draft/approval/execution/receipt/recovery and a
compatible combined migration path, followed by connected PENTA/Manual UI and
remaining operational cohorts. Domain service success is not a committed chat
receipt, model qualification or90%conversational coverage.

Mini source c3c61b9 remains available; its cloud CI40804103447 failed with
26failed/1651passed/1skipped/60errors. Do not hide this with skips or fabricated
historical evidence. Semantic qualification remains NOT ACCEPTED; explicit stop
before candidate model-weight loading remains. No experiment, paid compute,
activation, main merge, customer database, Azure deployment or issue closure.
Continue Sol High for consequential authority/transaction integration; Sol Medium
for settled UI/documentation.
