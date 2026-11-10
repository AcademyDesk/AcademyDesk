# C29 explicit guardian revocation and inactive-domain access — 10 November 2026

MODEL: Sol High for privacy/authorization/atomicity; Medium for settled docs.
TOOL: Cloud Codex. REPOSITORY: AcademyDesk/AcademyDesk.
BRANCH: codex/cloud-c29-guardian-access-20261110.
BASE:a713a506853733215111d4fb29fc725f14c521d8 (clean published C28).
DESKTOP:7d5c013c7312a91a6bb472e86d3a7d8a4d34c891 initially; final concurrent
29c0a940102e1436304e67c16cd339c1d81204b1 imported with provenance.
TASK: BUG-SEC-0006/0007 explicit per-link revocation and inactive guardian domain
checks on provisioning and portal child access. SOURCE FIXED / Cloud verification complete; source pins recorded;
native SQL/audit/race/device acceptance remains open.

Exact MAY MODIFY paths:

- `apps/api/Controllers/StudentGuardiansController.cs` — explicit portal-access state, revoke/grant validation and existing domain/actor audit opt-in only
- `apps/api/Controllers/PortalAccountsController.cs` — inactive Guardian provisioning guard only; preserve existing Identity/SQL boundary and authorization
- `apps/api/Controllers/PortalController.cs` — active owned guardian checks on Me/children/ParentAccess/profile; null-safe repeated permission reads
- `tests/AcademyDesk.Api.Tests/GuardianAccessLifecycleTests.cs`
- `tests/AcademyDesk.Api.Tests/GuardianProvisioningGuardTests.cs`
- `QA/ISSUES/BUG-SEC-0006.md`
- `QA/ISSUES/BUG-SEC-0007.md`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.json`
- `QA/HANDOFF/PRIORITY_ISSUE_STATUS_2026-11-08.md`
- `QA/HANDOFF/C29_GUARDIAN_ACCESS_2026-11-10.md`

MUST NOT MODIFY: protected PENTA/scheduling/attendance/enrollment, SQL harness or
migrations, shared auth/filter/session/Identity transaction code, guardian update
workflow, frontend/vendor/Mini or unrelated work. Desktop retains regular
scheduling/assigned-Teacher attendance and native SQL/device ownership. No main,
Azure, environment publication/replacement/public-access or model execution.
Preserve the stop on model loading/download/inference/training/activation;
Mini/Plus/Pro remain unqualified and Ultra deferred.

An explicit false revokes the selected guardian's link at every student age;
it clears all subordinate flags and records revoke time. Explicit true preserves
existing grant/subpermission defaults but cannot grant inactive domain records.
Missing/null state rejects; false must never mean a missing value. Preserve the
existing minor guardian requirement/default on initial link/onboarding. Revoking
one guardian must not revoke other guardians, siblings or the Student's access.

Inactive guardian must not provision a new usable account or read child records
with an otherwise active Identity. Recheck current domain state on each request;
do not reactivate guardians or manipulate identity credentials/tokens. Existing
active scoped paths, student access, guardian subpermissions and role/tenant
checks remain. Current provisioning requires matching real SQL/Identity stores;
do not bypass this boundary to manufacture an InMemory HTTP success. Guard-only
direct-controller checks and real portal HTTP tests must be labelled separately.

VALIDATE: original/final HTTP/Identity/InMemory portal/link tests, guard-only
provisioning controller checks, retained family/privacy/media/identity tests and
full API suite; no SQL/audit rollback/concurrency/physical-device claim. Web source
stays unchanged; reuse exact source-bound C28 web evidence with hash proof. Retain
original failures and raw results outside Git; no evidence fabrication/skips.
STOP WHEN: bounded source fix, tests and ordered Desktop SQL/browser/device
handoff committed/pushed with exact feature SHA. Five P0/broader session and
remaining CI/provider/recovery/release gates stay open; no auto-closure.
NEXT ROUTE: Desktop Sol High for protected SQL, Identity/audit races and live role/
portal/device acceptance; Cloud next bounded nonoverlapping non-AI task.


## Source repair and exact behavior

Implementation: `61abb67e8d16ad88f45f9155b7045709c533caf6`. Original path claim
c20363962e387d03c7a702e92578519bdf8031cb was pushed before implementation.
No unrelated source, protected host, schema, frontend or environment changes.

A portal-access PATCH now requires explicit true/false. Missing/null/malformed
state rejects400. Explicit false revokes at every age, clears all four subordinate
flags and grant time, and records revoke time. It is not overridden by the minor
bootstrap rule. The existing initial-link/onboarding minor default is preserved;
no guardian relationship or sibling access is deleted. Regrant retains the
existing boolean subpermission defaults/choices and cannot activate an inactive
student/guardian. A link with foreign domain records is refused404. The domain
write and required actor audit use the existing atomic boundary, including
Platform Owner. Real SQL rollback/race proof remains Desktop-owned.

New Guardian account provisioning requires an active owned Guardian before role
or Identity writes. Existing admin-only authorization, linked Identity/SQL
transaction, role initialization/conflict handling, and Student/Teacher rules are
unchanged. Cloud guard tests do not bypass that SQL boundary and do not claim
full HTTP/SQL provisioning success.

Portal Me/children/current parent-link queries and guardian profile mutation
check the current owned active guardian even when its Identity remains active.
Child list joins also constrain the student's academy. Child details derive
permission flags from the same nonnull link used for authorization instead of
rechecking and potentially defaulting a missing link to unrestricted access.
The Student's own active record keeps its existing access. Repeated academic
permission reads use null-safe denial if a link disappears. Existing private
material enforcement already checks active guardian/link/document permission;
it was inspected and its tests rerun, without editing that protected service.

This repairs server responses after current domain deactivation/revocation; it
does not rotate credentials, change sessions, reactivate guardians, promise
linearizable in-flight SQL behavior or erase records already downloaded by a
previously authorized user. Actual concurrency and role/browser/device behavior
remain part of the existing acceptance program.

## Verification

| Check | Observed result / boundary |
| --- | --- |
| Original new tests |33 total:14 PASS/19 FAIL. HTTP/Identity/InMemory portal/link subset28:11 PASS/17 FAIL. Guard-only provisioning subset5:3 PASS/2 FAIL; inactive guard reaches the missing-real-SQL prerequisite in the original code. That is not an original successful account-creation reproduction. Source snapshots, logs and TRX retained |
| Initial repaired target |86/86 PASS,33 new and53 retained family/media policy/Identity controls |
| Expanded target |95/95 PASS,36 new and59 retained;31 HTTP portal/link cases plus5 direct-controller provisioning guard cases. Cross-academy/anonymous controls and preserved initial minor defaults/subpermissions added; no skip or existing assertion weakened |
| Full API before final Desktop scheduling import |2,389/2,389 PASS;zero failures/skips on C29+Desktop7d5c013 |
| Final combined API | 2,389/2,389 PASS with Desktop29c0a94;zero failures/skips |
| Web/TSX/build/browser/frontend reuse |All164 tracked web source hashes and570 exact export file hashes match C28. Its pinned source db012648e608e1fa5cf7291ce0271af3f4b1407d/publication a713a506853733215111d4fb29fc725f14c521d8 has build/types/lint PASS,29 controlled and24 native synthetic cases PASS;full frontend2,343 PASS/3 missing genuine SQL receipt failures/zero skips. No new frontend/browser/device run is claimed for C29 |
| Release guard |15/15 guard controls PASS;actual guard exit1 retains five P0 and required broader-session records open |
| Hosted CI |UNKNOWN; feature pushes do not trigger verify.yml's main/release-export-security filter. No dispatch, main merge, PR or Azure deployment |

Expanded tests cover minor/adult/exact18th-birthday/unknown DOB, explicit false
with omitted subordinate defaults, regrant with selective permissions, sibling/
other guardian/Student preservation, same-bearer domain deactivate/reactivate,
inactive-guardian Me/children/student/invoice/certificate/leave-list/leave-create/
practice/profile denial, failed request no-write snapshots including Identity,
Owner/Admin/FrontDesk/Platform Owner attributed mutations, unrelated staff,
foreign/missing links and legacy cross-academy domain records, anonymous callers,
missing/null/malformed choices, and unchanged private material/family behavior.

Provisioning guards use direct controllers with actual UserManager/RoleManager/
InMemory stores and a synthetic authenticated creator principal. They verify
inactive/missing/foreign Guardian refusal before writes and unprivileged creator
denial. Active account creation/role/audit commit, rollback, cross-store failure
and races require Desktop's real matching SQL contexts; no fake provider or
transaction is introduced to manufacture a Cloud HTTP success.

Raw originals, compile-fixture failure (missing synthetic certificate verification
code, corrected before original execution), all original/final logs/TRX, exact
C28 web reuse hashes, guard/inventory, publication and source/assembly/export
manifest are retained outside Git at`/workspace/c29-guardian-access-20261110`.
No raw QA/EVIDENCE, credentials, caches, model files or generated export are pushed.

## Ordered Desktop handoff

1. Integrate C29 implementation/result pins or copy exactly its10 claimed paths,
   preserving all newer protected scheduling/attendance, host, SQL and local work.
   Desktop29c0a94 is preserved in this branch. C24–C28's native acceptance remains
   separately queued; don't replace whole newer Desktop files with older files.
   Regenerate both derived inventory files against the integrated current issue
   records; preserve newer Desktop issue/status evidence.
2. On disposable real Identity/HTTP/SQL, run the original SEC0006/0007 scenarios,
   including genuine active Guardian account creation then domain deactivation,
   refused new inactive Guardian account and fresh reactivation control. Verify
   no user/role/membership/credential/audit writes on rejected provisioning.
3. Exercise minor/adult/birthday/unknown DOB explicit revoke, missing intent,
   selective regrant, unlink and one-of-two guardians plus sibling/Student
   controls. Confirm all subordinate fields/timestamps, current next-request
   child/details/document/leave/practice/profile refusal, owned joins and
   retained active guardian/Student behavior; no cached bearer may grant domain
   authority after revocation. Preserve initial minor onboarding/link defaults.
4. Inject required actor-audit failure on portal-access PATCH for Admin/Owner/
   Platform Owner. Fresh SQL link/timestamps/flags and audit must roll back
   together. Retain existing provisioning Identity/domain transaction safeguards
   and inject role/user/audit failures without orphan users or memberships.
5. Run controlled concurrent revoke/regrant, guardian deactivation/provisioning,
   and active request overlap using fresh contexts. Validate intended SQL
   translation, locking/isolation and no partial authority changes; C29 does not
   assert these race cases from InMemory. Review the same-link detail permission
   projection and null-safe denial under concurrent link disappearance.
6. Run live Guardian/Student/Admin/FrontDesk portal and account-management browser
   journeys, revoke/403 recovery, overlays, downloads and physical devices. Native
   host/SQL/browser/critical/documented acceptance is required before closing
   SEC0006/0007. Do not infer it from unchanged synthetic C28 browser results.

## Remaining release and model gates

Both records remain OPEN. The full inventory still tracks120 records
(5 P0/102 P1/13 P2), not120 untouched defects or a completion percentage. Five
P0s and broader-session/all-role/physical-device acceptance, prior returns' live
acceptance, real providers/meetings/delivery, exact-candidate hosted CI/current
security/image checks and staging backup/restore/rollback remain pending.
Do not retry the previously rejected external npm audit disclosure under Continue.

Cloud can continue bounded non-AI defects after publishing this source return.
Desktop retains deterministic regular scheduling/assigned-Teacher attendance and
protected SQL/host/migration/data/device work. The non-AI entry gate is not yet
accepted; model loading/download/inference/training/activation remains stopped.
Mini/Plus/Pro stay unqualified, Ultra deferred and90% eligible conversational
outcome coverage is unaccepted. No main merge, Azure deploy or public-access/
environment publication has occurred.


## Final concurrent Desktop scheduling return

Desktop29c0a940102e1436304e67c16cd339c1d81204b1 was discovered during final
publication checks. Its14 changed paths were imported byte-exact with cherry-pick
provenance, including regular class scheduling service, shared Manual create/
reschedule conflict checks, Testing-only approved PENTA create, native SQL harness,
DATA0005 issue/report and shared status/handoffs. No C29 guardian path overlapped.
A fresh full API run binds this composite source; the earlier2,389-pass result
remains as its own earlier-source receipt. The manifest records both the guardian
implementation and this preserved Desktop pin.

Desktop's retained report records18 schedule form checks,18 retained Manual HTTP/
SQL,43 new scheduling and81 retained make-up cases. Those native SQL results were
not rerun here. Its next owner task is assigned-Teacher attendance; protected
regular reschedule and batch reassignment remain separate. No model qualification
or production gate was closed by this deterministic host return.

Final combined source before documentation pins: `189f0445113ad52476335eb8e35cf7b4a46bfca3`.
The guardian implementation `61abb67e8d16ad88f45f9155b7045709c533caf6` is unchanged within that composite.
