# BUG-DATA-0011 — Account provisioning ignores role-assignment failure

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | ACCEPTED SOURCE FINDING; all three bounded local failure paths verified |
| Final verification | PARTIAL PASS / OPEN — three failure-path repairs plus bounded native deadlock/conflict responses verified; browser/critical/wider race gates remain |
| Severity | Major identity provisioning integrity |
| Priority | P1 |
| Category | DATA |
| Module | AUTH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /portal-accounts; /platform; /platform/control; direct academy creation API |
| API | POST /api/academies/{academyId}/portal-accounts; POST /api/platform/academies; POST /api/academies |
| Environment | Accepted source review plus bounded owned local SQL/HTTP runs; production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | AUTH-PROVISION-001 |
| Evidence classification | Accepted static finding and bounded real SQL/Identity/HTTP evidence; wider gates remain OPEN |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/api/Controllers/PortalAccountsController.cs:29 |
| Class/function | PortalAccounts.Create; PlatformAcademies.Onboard; Academies.Create |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Historical excerpt below; latest QA/REPORTS/PHASE_2B_PROVISIONING_CONFLICT_REPAIR.md and linked receipts/logs |
| Screenshot | Not captured on pinned baseline |
| Console logs | Native1205/provider-wrapper and final response evidence captured in local SQL logs; browser console not retested here |
| API request | In isolated fixture allow user creation, then inject an IdentityResult failure at role assignment. Capture response and fresh Identity user/role rows. Separately simulate thrown role-store failure and retry. |
| API response | Final seven native collision losers409 (six1205, one2601/UserNameIndex); retry/no-changes messages; unrelated faults remain500 |
| Database before/after | Fresh captured collection snapshots verify exact success/rollback/retry; not every database table |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Local uncommitted PlatformAcademies.Onboard, PortalAccounts.Create and Academies.Create repairs; no push/deploy |
| Retest result | Final23 provisioning-conflict mutations and12 exact-academy reads PASS; six1205/one2601 collision losers409, unrelated faults500; prior self-service32/portal49/platform30 retained |
| Regression result | Final1032 backend tests PASS (1019 existing+13 classifier checks); wider browser/unique-key/bypass/critical gates OPEN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixture allow user creation, then inject an IdentityResult failure at role assignment. Capture response and fresh Identity user/role rows. Separately simulate thrown role-store failure and retry.

## Expected

Success means the requested account and role are durably provisioned; failures cannot falsely report success or leave an unmanaged account without a defined recovery path.

## Actual / evidence

CreateAsync result is checked but AddToRoleAsync result is discarded before unconditional Ok. No rollback/compensation surrounds the two writes; thrown failure can leave the created user persisted. Role creation result is also unchecked. PlatformAcademies.Onboard likewise ignores AcademyAdmin role creation/assignment results after saving the academy and new identity. Academies.Create checks Owner role creation but ignores both existing-user UpdateAsync assignment and AddToRoleAsync results after saving academy, then returns201.

Source snapshot:

```text
28:         if (!create.Succeeded) return BadRequest(new { message = string.Join(" ", create.Errors.Select(x => x.Description)) });
29:         await users.AddToRoleAsync(user, role);
30:         return Ok(new { user.Id, user.Email, role });
31:     }
```

## Suspected root cause

Provisioning is treated as successful after user insertion rather than after all required Identity steps.

## Business impact and blast radius

Student, Guardian, Teacher and initial AcademyAdmin account creation; wrong workspace/session roles and failed retries after partial creation. Does not claim role assignment fails on every normal request.

## Related / required regression

AUTH-PROVISION-001: Failure-injected role-store tests plus real SQL/HTTP happy path, uniqueness and retry controls; assert response truth, exact user/link/role state and no duplicate accounts. Repeat platform onboarding and self-service academy creation with fresh academy/identity/role/audit assertions and failure at role creation/user update/assignment. Verify orphan-academy recovery and concurrent unassigned-user creates.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Bounded local successor — 2026-10-02

[Browser provisioning follow-up](../REPORTS/PHASE_2B_PROVISIONING_BROWSER_RETRY.md): real Student/Teacher success/reset, duplicate400/draft, optional-blank academy201/popup close and new Student/Teacher/Admin routing observed; Owner/Student/Teacher sessions survive other portal logins. QA-only missing owner flag corrected, not a production routing bug. Interrupted host final assertions NOT RUN; independent read-only SQL confirms3 users/roles/typed links and one unique academy. Product unchanged;857 pins, earlier suites not rerun. Exact disposable container stopped/root retained after policy blocked deletion;cleanup/device/recovery/critical/Phase2B/release OPEN. Next native revocation/disabled identity gap, Sol High;Azure untouched.

[Session-refresh request repair](../REPORTS/PHASE_2B_SESSION_REFRESH.md): shared frontend helper formerly replayed expired headers after successful refresh.37 controlled checks/11 native Identity-HTTP requests PASS, including renewed Admin POST persisting exactly one typed Student account/membership/audit; native Admin/Teacher and expired refresh branches verified.16 prior provisioning-page controls PASS; API controllers/prior UI unchanged.813 entries retained, owned SQL/bridge cleaned; no browser acceptance/backend recount/Azure change. New BUG-AUTH-REFRESH-001 and BUG-DATA-0011/Phase2B/release OPEN; next simultaneous refresh coordination, then pending visible provisioning/routing, Sol High.

[Provisioning feedback safety](../REPORTS/PHASE_2B_PROVISIONING_UI_FEEDBACK.md): two local UI pages only; Portal Access blocks duplicate submits, handles uncertain transport/rejection with draft retention and accessible messages, validates person lookups before enabling. Platform Overview distinguishes committed create from failed refresh.16/16 controlled actual TSX handler checks and TypeScript PASS; no backend recount. Browser bridge/preflight ready but browser-control timeouts prevented every interaction: zero browser cases accepted, zero intended SQL accounts/academy; aborted required assertions retained, owned fixture cleaned.788 predecessor entries pinned; APIs/auth/schema/HEAD/dev services/Azure unchanged. BUG-DATA-0011/Phase2B/release OPEN. Next session-refresh/request-header behavior, then pending browser feedback/routing; Sol High, no repeated accepted audit.

[Provisioning conflict response repair](../REPORTS/PHASE_2B_PROVISIONING_CONFLICT_REPAIR.md): legacy classification captures seven native1205/EF-wrapper deadlocks; final captures six1205 and one2601/UserNameIndex/DbUpdateException. Narrow role/user-Create catches inspect the actual provider cause and return409 retry/no-changes while retaining rollback. Final23 mutations/12 exact-academy reads PASS; all seven losers409, five first-role retries succeed, duplicate replays409/400 unchanged, unrelated51006 faults remain500/no writes. Final1032 backend PASS;13 new classifier guards. UserNameIndex2601 verified once in real SQL; RoleNameIndex unique/2627/wrong-index guards unit-only. Two interrupted diagnostic assertions/intermediate direct-only helper artifacts retained/excluded; four owned SQL runs cleaned.741 predecessor entries retained; Azure/commit/dev DB/services unchanged. BUG-DATA-0011/Phase2B/release OPEN; next bounded provisioning browser feedback/role refresh, Sol High. Supersedes older next-task statements.

[Provisioning concurrency gap checks](../REPORTS/PHASE_2B_PROVISIONING_CONCURRENCY.md): application unchanged; seven controlled simultaneous native INSERT pairs (five first-role paths, platform/Guardian normalized duplicate submissions),21 mutation requests including retries/replays and12 exact-academy reads PASS for atomic integrity. Five first-role losers retry successfully; two duplicate replays reject without changes. Seven initial losers still return generic500: response handling OPEN, SQL error classification required before narrow recovery/conflict repair. Five roles/five academies/ten new users/two existing-user associations/12 memberships/ten audits, no partial loser. Owned SQL exit0/cleanup complete; subsequent QA-only wrapper trailing-command error retained and corrected statically, no repeated SQL run.727 accepted entries retained; predecessor1019 backend results not rerun/recounted. BUG-DATA-0011/Phase2B/release OPEN. Next collision classification and response repair, Sol High; supersedes older next-task statements.

[Self-service academy provisioning repair](../REPORTS/PHASE_2B_ACADEMY_PROVISIONING_REPAIR.md): explicit same-store transaction with checked existing-user update/Owner assignment; preserves pre-existing Owner membership and other original guards/defaults/roles/audit behavior.30 sequential HTTP-SQL cases plus one controlled concurrent pair PASS (32 mutations), four exact original-token own-academy reads and1019 existing backend rerun PASS.11 academies/creator assignments,10 new memberships, one role, no new users/audits;20 sequential no-write rejections. Native concurrency stamp gives one201/one400 for two overlapping same-account requests with no orphan academy; replay409. Initial seven-case QA-only JSON-encoding assertion failure retained/corrected/excluded; both disposable runs cleaned. All three routes now have bounded local repairs, but BUG-DATA-0011/Phase2B/release remain OPEN for other concurrency/browser/critical/commit ambiguity and broader policy. Next remaining provisioning concurrency gaps, Sol High. This supersedes older next-task statements below.

[Portal account provisioning repair](../REPORTS/PHASE_2B_PORTAL_PROVISIONING_REPAIR.md): checked role creation/assignment and explicit Identity/domain/success-audit transaction opt-in, plus controller-local fallback for the unchanged platform-owner bypass.49 final real HTTP-SQL cases PASS:15 complete users/memberships, two new roles,13 normal success audits, two preserved audit-bypass successes and34 unchanged rejection snapshots. Student/Guardian/Teacher assignment-result, membership SQL and audit SQL failures each have same-login retries; missing-role creation/user faults/native password failures, exact typed links/defaults/roles/actor-route audits, uniqueness/auth boundaries and bypass rollback included. Six separate real login/exact-academy GET controls PASS;1019 existing backend rerun PASS. One initial48-case QA-only blank-password assertion failure retained/corrected/excluded; both disposable runs cleaned, evidence retained. Product guards/person policy/filter/helper/UI/schema/Azure unchanged. BUG-DATA-0011 remains OPEN for Academies.Create, browser/critical/concurrency/commit ambiguity and broader policy. Next Academies.Create, Sol High. This checkpoint supersedes the older next-task statement below.

[Platform onboarding provisioning repair](../REPORTS/PHASE_2B_PLATFORM_PROVISIONING_REPAIR.md): checked role-creation and assignment results; one same-store SQL transaction covers role, academy, user, membership and success audit. Any unsuccessful Identity result or thrown write disposes the uncommitted boundary; success commits before201. Existing SQL enlistment helper reused without changes. Saved trial-duration logic, optional defaults, normalization, role metadata, owner flag authorization and all other controller methods retained. No schema/UI/normal dev DB/service/Azure/commit changes.

30/30 final Identity/HTTP/fresh SQL cases PASS:7x201,10x400,7x500,2x409,3x403,1x401. Missing-role returned role failure, role INSERT SQL fault, invalid password, user INSERT SQL fault, returned assignment failure after persisted user/staged membership, membership INSERT SQL fault and success-audit INSERT SQL fault all leave identical captured snapshots. A shared-login retry after these seven failures succeeds and creates the role exactly once. Existing-role invalid-password/user/assignment/audit failures each have successful same-login retries; existing rows/role and finance snapshots preserved. Seven successes produce exactly seven academies/admins/memberships and seven actor-linked success audits, with only one new role.23 rejections unchanged. Duplicate normalized login, required-field/malformed binding, owner flag/anonymous checks and optional-null bare-login success included. New admin real login200 and scoped academy read200 are separate access controls, not counted in the30. Separately1019 existing backend tests rerun; no new unit tests. One isolated build/run passed, no failed attempts; owned SQL/root/container/port cleaned.

Accepted static finding is the baseline; no new baseline reproduction or repeat Astra audit. BUG-DATA-0011/AUTH-PROVISION-001/Phase2B/release remain OPEN: PortalAccounts.Create and Academies.Create are not repaired by this slice; their existing-user updates/role integrity, concurrent creates, browser feedback/device flow, cancellation/commit/transport ambiguity, all-linked and full critical regression remain separate. Next: PortalAccounts.Create atomic role/account provisioning, Sol High.
