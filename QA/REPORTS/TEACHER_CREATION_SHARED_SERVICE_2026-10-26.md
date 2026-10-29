# Teacher creation: shared authorized service — 26 October 2026

## Result

Completed the existing teacher-create backend extraction and an **inactive**
typed PENTA field adapter. Manual POST now uses the same independently authorized
domain service available to a future reviewed conversational caller. This is not
an enabled teacher chat workflow, model qualification or production approval.

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Existing dirty work is preserved;
no commit, push, merge, main checkout edit or Azure action occurred.

## Implementation

- `TeacherCreationService` resolves the current database actor, academy and role
  membership independently of principal/model role claims. Relational execution
  requires the same SQL Server domain/Identity transaction. Academy, current
  identity and role records are locked through the caller's transaction.
  A supplied known user ID without an authenticated principal is rejected too.
- The existing controller delegates to the service and explicitly enlists
  Identity and Platform Owner mutations in the existing atomic/audit filter.
  Mandatory audit failure rolls back the teacher instead of returning success.
- Existing Manual required-name/branch validation, optional null/blank fields,
  metadata, response/location and account-creation behavior are preserved. No
  duplicate policy, compensation rules or new required fields were invented.
- `PentaTeacherCreationFields` validates the existing operational envelope and
  14 bounded typed fields. Partial drafts may omit names; execution requires
  required names. Unknown/credential/role/complex metadata fields, malformed
  types, dates, references, duplicate keys and oversized values fail closed.
  Parsing does not authorize, approve, deduplicate or execute anything.
- No catalog, model, public AI endpoint or dispatch registration was added.
  A future host caller still owns scoped approvals, duplicate/replay protection,
  atomic audit/commit and an authoritative persisted conversation receipt.

## Actual validation

Bounded window `f6184cea089b4ff0a7395c5a046fe645`:

- Full unfiltered API build/test: **3,040 passed, zero failed/skipped**, 17-second
  reported test duration. This includes the prior 2,995 baseline and 45 additional
  service, field-parser and explicit atomicity assertions/cases.
- SQL harness rebuilt with cached packages and no restore. Native run
  `46b63aadcb024445b6b0bb1cab6bd077`: all **15** new SQL/HTTP/shared-service
  cases passed, together with all **45** retained availability cases. These are
  separate integration checks, not extra xUnit test counts.
- Actual anonymous/Teacher/foreign-tenant refusals, missing name/branch 400,
  unguarded direct SQL service refusal, optional empty Manual create, metadata
  preservation, Platform Owner cross-academy audited creation, injected mandatory
  audit failure rollback, typed candidate using the same shared service under an
  explicit host transaction/audit, and old-bearer inactive-actor refusal passed. Three additional direct-service SQL
  denials commit their refusal transactions and compare state: unauthenticated
  known administrator ID, authenticated Teacher claiming Owner, and removed
  administrator membership despite Owner claims. No HTTP filter masks these checks.
- Exactly four new synthetic teachers were recorded by these success paths;
  no portal accounts, model inference or AI-tool activation. Denials/audit fault
  compare scoped teacher/identity/role/audit/student/invoice/payment/draft state.
- 102 application and seven Identity migrations ran in disposable SQL; scoped
  login and negative cleanup ownership control passed. Only the run-owned SQL
  database/login/container were removed.
- The approved window stopped only the two original PENTA services, admitted
  above the unchanged 3 GiB RAM guard, then restored their exact IDs/images and
  health. Full Docker container/volume/network/image inventories were preserved;
  exclusive validation lease released. Current API source pins match the run.
- A separate read-only evidence reconciliation verifies actual TRX counters,
  SQL receipts, source timestamps/hashes, cleanup, live exact original services,
  lease release and the unchanged main HEAD/status fingerprint.

Local evidence (intentionally uncommitted):

1. `QA/EVIDENCE/teacher-creation-window-f6184cea089b4ff0a7395c5a046fe645/`
2. `QA/EVIDENCE/teacher-creation-sql-46b63aadcb024445b6b0bb1cab6bd077/result.json`
3. `QA/EVIDENCE/teacher-creation-qualified-f37a333136c4476a9aa513ec83cf3c87/result.json`

Earlier failed windows are retained: `34e0fc303f5741b2abbc51b6a51f5e12`
stopped on an ambiguous test namespace, corrected with an explicit alias;
`863eb2e07aa441fe8ded06580c4ccb55` passed 3,037 of 3,038 but found the old
assertion that teacher creation must not enlist Identity. That assertion now
requires enlistment and an added Platform Owner atomicity case. Neither failure
is presented as a pass; both restored the original services/inventory.

The intermediate 3,039-test/12-SQL-case run
`597524e5639c48f29f23d0b64ffc9826` and its SQL run
`80f91e5cb9cb42f7a97cd432ed95b2fb` remain accepted historical evidence for their
source. Final review then added explicit service authentication and the three
direct SQL denials above; only the final current-source run qualifies this code.

The existing CI floor now requires 3,040 passing, zero-skipped API tests. All
31 artifact/workflow guards pass locally, including refusal of the earlier
2,995-test report as a current candidate. Its historical acceptance is unchanged;
no GitHub workflow was run. The final evidence directory retains the guard TAP
and current guard/source hashes.

## Remaining path and limits

- [x] Shared current-authority teacher-create service and Manual reuse.
- [x] Strict inactive typed adapter and actual SQL/audit rollback proof.
- [ ] Complete teacher conversational draft/revision, duplicate policy/checks,
  scoped approval, uncertain-response recovery and receipt workflow; register
  only after host and engine qualification. Complex metadata stays Manual until
  dedicated reviewed conversational workflows exist.
- [ ] Qualified Mini and connected remaining workflow cohorts; no 90% coverage
  claim. Existing model score remains 6/12 fresh, 7/12 exposed, NOT ACCEPTED.
- [ ] All-role/Manual/customer-critical/browser/device acceptance and exact
  GitHub/runtime/migration/security/restore/staging/release gates.

No new browser/device, frontend build, release Docker image or GitHub CI execution
was claimed by this backend-only batch. The previous API image proofs are bound
to their earlier source; they do not qualify this new service's release image.
Existing issues remain OPEN. Owner's explicit stop-before-model-loading remains
in force: no loading, training, inference, activation, downloads or paid compute.

Next route: **Sol High** for protected conversational host integration; **Sol
Medium** once behavior/contracts are settled for bounded UI and documentation.
Continue the existing enterprise program, without repeating accepted audits or
expanding the completed reload-recovery slice.
