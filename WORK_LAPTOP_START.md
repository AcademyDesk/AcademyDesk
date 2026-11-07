# Academy Desk — continue from the work laptop or another profile

## Local P0 finance concurrency — 7 November 2026

- [x] FinanceIntegrity passed 12 cases, including six real SQL races and three
  audit rollbacks. Payroll passed 23 reconciliation checks, including five
  deterministic races, two audit rollbacks and migration controls.
- [x] All 14 production guard tests passed. The guard still blocks the five
  P0 records pending their remaining acceptance.
- [ ] Release-commit CI and images, critical browser and physical-device
  coverage, and staging restore and rollback are pending. The guarded deploy
  workflow is in the candidate branch. The older `main` workflow lacks it.

[Local finance and release evidence](QA/REPORTS/LOCAL_P0_FINANCE_CONCURRENCY_2026-11-07.md)

## C12 cloud compensation return integrated — 7 November 2026

- [x] Imported the four verified C12 files at `b7c9f84`. Compensation reads
  now handle non-object metadata without changing stored values.
- [x] Reused Cloud's 36 targeted and 2,027 full API passes on the matching
  application and API test trees.
- [ ] Portal announcement metadata, disposable-SQL readback and linked
  acceptance remain open. Local P0 finance concurrency and payroll take priority.

[Cloud C12 verification](QA/HANDOFF/C12_COMPENSATION_METADATA_2026-11-07.md)

## Local P0 release retest — 7 November 2026

- [x] Current private-media backend passed the original leak check, 29 legacy
  HTTP cases and 75 private Blob assertions.
- [x] Payment status passed void, evidence and 12 restoration controls.
  Disposable SQL backup and restore passed all six checks on 82 tables.
- [ ] The five P0 issue records, device/critical checks, release-commit CI and
  images, and staging restore/rollback remain open. Sol High for the next
  runtime and authorization work. Refresh Cloud ownership first.

[Local P0 release retest](QA/REPORTS/LOCAL_P0_RELEASE_ACCEPTANCE_2026-11-07.md)

## C11 cloud status and navigation return integrated — 7 November 2026

- [x] Imported the ten C11 paths at `fe0b673`. The frontend matches Cloud's
  tested source for grant expiry display, Platform status recovery and Sales
  Follow-ups navigation.
- [x] Reused Cloud's 28 controlled checks and 49 Chromium cases. Its full
  frontend result remains 2,008 passes and three missing communication
  receipt failures. The prior local communication proof is recorded separately.
- [ ] C11 still requires live role/tenant and mutation evidence and device
  acceptance. The P0 release queue takes priority over lower-risk follow-ups.

[Local C11 integration report](QA/REPORTS/LOCAL_C11_SOURCE_INTEGRATION_2026-11-07.md)

## C10 cloud academic return integrated — 7 November 2026

- [x] Imported the 14 verified C10 files at `6e09af3`. The integrated frontend
  matches the tested Cloud tree.
- [x] Reused Cloud's 58 targeted controls and 30 controlled Chromium cases for
  calendar, dates, curriculum and subject fees. Cloud's full suite recorded
  1,980 passes and three missing genuine communication SQL receipt failures.
- [x] Local C10 passed 19 real academic API/SQL checks and repaired the
  curriculum create guard. The candidate passed all 1,991 API tests.
- [x] The three evidence-dependent communication checks passed locally.
  The Windows full frontend run was 1,982/1,983 with one file-symlink
  capability failure. Cloud passed that unchanged case on Linux.
- [ ] Five open P0 records still block production. Broader critical/device
  checks, current verification workflow and image proof, and staged
  rollback/restore are pending. Refresh Cloud ownership before claiming a P0.

[Local academic and release validation](QA/REPORTS/LOCAL_C10_ACADEMIC_ACCEPTANCE_2026-11-07.md)

[Local C10 integration report](QA/REPORTS/LOCAL_C10_SOURCE_INTEGRATION_2026-11-07.md)

## C9 cloud finance return integrated — 7 November 2026

- [x] Imported the verified 17-path C9 return at `ff662d0`. The integrated
  frontend matches Cloud's tested source. All 118 queue records were preserved.
- [x] Reused Cloud's 49 controlled browser cases, handler checks and build
  evidence. Its three missing SQL receipt failures remain recorded.
- [x] Current backend retains the C8 acceptance of 25 SQL controls and 1,991
  API tests. C9 introduces frontend and QA source only.
- [x] C9 Payments passed four live Chromium, Identity and SQL recovery checks.
  A lost 201 left exactly two rows, 1,000 collected and one attributed audit.
  Neither a second form submit nor reload replayed the payment.
- [x] Invoice, payroll profile, payout and reconciliation passed eight live
  Chromium, Identity and SQL checks. Each committed once, produced one attributed
  audit and reloaded the saved state without replaying the financial write.
- [ ] Broader critical and device acceptance, current images, CI and release
  gates remain open. Next local packet is C10 linked API/SQL. Cloud C11 owns
  access display, platform status and sales navigation. Refresh its claim first.

[Live invoice, payroll and reconciliation verification](QA/REPORTS/LOCAL_C9_FINANCE_RECOVERY_SQL_2026-11-07.md)

[Live Payments verification](QA/REPORTS/LOCAL_C9_PAYMENT_RECOVERY_SQL_2026-11-07.md)

[Local C9 integration report](QA/REPORTS/LOCAL_C9_SOURCE_INTEGRATION_2026-11-07.md)

## C8 cloud frontend and local music progress SQL — 7 November 2026

- [x] Integrated the five C8 return paths and matched Cloud's tested frontend.
  Reused its controlled browser, handler and build evidence.
- [x] Local SQL reproduced and repaired the music status audit gap. All 25
  native controls passed, including both actor rollbacks and exact score readback.
- [x] Full API regression passed 1,991 tests. Owned SQL cleanup, original PENTA
  services, inventory and lease release verified.
- [ ] Live browser recovery, independent-client behavior and critical and device
  closure remain open. Cloud published C9 source at `ff662d0` for separate import.

[Local C8 acceptance report](QA/REPORTS/LOCAL_C8_MUSIC_PROGRESS_SQL_2026-11-07.md)

## C6 live finance decisions and Teacher forms — 7 November 2026

- [x] Finance rejections require a reason at the API. Platform Owner decisions
  use the existing atomic save and audit boundary. Both actors passed rollback.
- [x] 41 native HTTP and SQL controls passed, covering reconciled balances,
  attributed audits, Teacher form values, and role and batch denials.
- [x] 13 live browser and SQL phases passed. Prompt cancellation, one-row saves,
  form reset and retention, and one committed lost-response case verified.
- [x] Full API regression passed all 1,989 tests. Owned SQL cleanup, Docker
  inventory, PENTA services, API health 200 and lease release verified.
- [ ] Critical and device closure, current images and CI, remaining release
  workflows and model qualification are pending. Sol High for runtime work.

[Live C6 acceptance report](QA/REPORTS/LOCAL_C6_LIVE_CONTROLS_2026-11-06.md)


## Compensation live SQL and required-audit repair — 6 November 2026

- [x] Genuine C7 browser bodies now pass 28 real Identity/HTTP/fresh-SQL checks:
  zero/null/decimal/date resaves, invalid/Teacher/foreign denials, exact audits,
  unrelated teachers/Identity/money preservation and both actor audit rollbacks.
- [x] Confirmed baseline defect retained: Admin saved despite audit500; Platform
  Owner bypassed audit. Save now uses the existing atomic boundary including
  Platform Owner; no permission, payroll/zero-net, payload or schema change.
- [x] Fresh harness build zero warnings/errors; full API 1,986 PASS, zero failed/
  skipped. Exact owned SQL cleanup, services/images/API200/inventory/lease and
  original main status preservation PASS. No frontend rebuild was needed.
- [ ] Both compensation issues stay OPEN for remaining closure gates. C6 live
  finance approval/Teacher forms, other roles/workflows/P0/privacy/device/restore,
  current images/CI/audits/staging, model qualification and release remain OPEN.

Read QA/REPORTS/LOCAL_COMPENSATION_SQL_2026-11-06.md. Next local packet is C6 live
finance approval and Teacher assigned-batch HTTP/SQL acceptance; Cloud should
choose disjoint claims after this checkpoint. Do not restart accepted audits or
repeat C7 browser coverage. Sol High for financial/security SQL; Medium for settled
visuals/docs. Keep raw evidence local and candidate model loading/training/activation
stopped. No main merge/push, production DB or Azure deployment is authorized here.

## Returned C7 compensation browser packet accepted locally — 6 November 2026

- [x] Three disjoint cloud QA files integrated byte-exact; no app/API/editor change.
- [x] 44 real Edge groups PASS: 40 zero/null/value/theme/viewport cases and four
  late/failed selection controls; 49 intercepted synthetic PUTs, zero runtime errors.
- [x] Existing 51 value-preservation + 47 editor-isolation tests PASS. All 658 built
  artifacts unchanged; exact original services/API200/inventory/lease restored.
- [ ] BUG-DATA-0050 remains OPEN: live compensation SQL/audit/payroll/device and
  server-contention acceptance are separate, alongside remaining release gates.

Read QA/REPORTS/LOCAL_C7_COMPENSATION_BROWSER_2026-11-06.md. Continue live domain
acceptance rather than rerunning the accepted browser matrix. Sol High for those
security/SQL boundaries; Medium for settled visuals/docs. No main/Azure/model work.

## Current built UI / real Identity and SQL session journey — 6 November 2026

- [x] Eleven real browser checks and 51 native API responses PASS, no HTTP 429 or
  uncaught page errors; no response mocks or injected tokens. AI-first entry and
  Manual return paths accepted; no AI tool/model activation claim.
- [x] Deactivate/readback/revoked Admin access and password error/success/new-login
  behavior accepted. SQL confirms Identity state, one audit, stamp revocation and
  user/role preservation; foreign-tenant GET/POST denied. Migrations 85 / 7.
- [x] Bound 658 artifacts/current source; exact disposable SQL cleanup, full Docker
  inventory, both original services/images/API200 and lease restoration PASS.
- [ ] Remaining roles/domain/device/security/current images/CI/privacy/restore/
  model/release gates stay OPEN. Prior setup failure and recovery are retained.

Read QA/REPORTS/LOCAL_CURRENT_SESSION_BROWSER_2026-11-06.md. Next integrate only
the three disjoint Cloud C7 compensation QA paths; preserve the historical driver,
C6 live acceptance and candidate model stop. Sol High for live security/SQL;
Medium for settled browser QA and docs. No main/Azure change.

## Cloud C6 Manual fixes integrated and validated — 6 November 2026

- [x] Finance Cancel sends no mutation; Teacher publication/date/fractional-score
  payload and save recovery fixes integrated from cloud 1ec22e7. No backend change.
- [x] Actual TSX contracts 36 PASS; exact-version build, postbuild, types and lint
  PASS (zero errors / 63 warnings). Manual browser 15 and PENTA browser 24 PASS;
  API responses intercepted, zero runtime errors. No live SQL/model claim.
- [x] Full frontend 1,920 PASS / one Windows file-symlink EPERM / zero skips.
  Missing supporting test inputs restored unchanged; first failures retained.
- [x] Both original PENTA service identities/images, API 200, inventory and lease
  restoration PASS. Source-bound details and limitations in the report below.
- [ ] Both Manual issues remain OPEN. Actual protected browser/Identity/SQL,
  remaining role/workflow/security/device/build/CI/model and release gates remain.

Read QA/REPORTS/LOCAL_C6_MANUAL_CONTROLS_2026-11-06.md. Next local packet is the
current built UI plus disposable Identity/SQL session browser acceptance. No audit
restart, original dirty-tree overwrite, candidate model loading or main/Azure work.
Use Sol High for that integration; Medium for settled docs/visual implementation.

## Isolated pinned frontend now validated — 6 November 2026

- [x] D: candidate uses exact locked Next16.4.3/Zod4.6.7;358installed versions match.
  Six public tarballs80,940,650bytes/SHA512verified; original cache unchanged.
- [x] Webpack/static export,TypeScript,ESLint PASS (0errors/63warnings);Windows
  postbuild87segments/86aliases PASS. Cache-derived setup, not fresh npm ci/audit.
- [x] Real browser24groups PASS/zero runtime exceptions; fixtures intercept all
  API responses, including six synthetic mutations. No live SQL/model acceptance.
- [x] Exact original services/images/API200/full inventory/lease restoration PASS.
- [ ] Production-configured artifact/images/CI/audits, real host/browser coupling,
  remaining roles/workflows/privacy/P0/device/restore/release and model stay OPEN.

Read QA/REPORTS/LOCAL_PINNED_WEB_VALIDATION_2026-11-06.md. Only browser QA runner
config/cleanup changed; application and dependency lock are untouched. No Azure.

## Current-client native session SQL completed — 6 November 2026

- [x] Existing native clients now exercise explicit-login epochs/events while
  retaining every status/count/no-replay/one-write/SQL audit check.
- [x] SessionRefresh11helper HTTP requests and RefreshConcurrency25/four overlaps
  PASS against real owned Identity/SQL; one saved typed Student per fixture.
- [x] Initial second-module RAM refusal retained; successor reran only pending
  concurrency with matching source hashes. Both service/inventory restores PASS.
- [ ] Real browser/UI/cross-tab Web Locks/lifetime/device and remaining model/
  exact-lock build/CI/privacy/P0/restore/staging/release acceptance remain OPEN.

Read QA/REPORTS/LOCAL_NATIVE_SESSION_INTEGRATION_2026-11-06.md. Only QA native
clients changed; application/SQL harness and previous source-bound tests unchanged.
No new package/model/main/Azure action. Keep raw QA/EVIDENCE local.

## Version1 genuine original verification returned — 6 November 2026

- [x] Mini returna789f1a published/verified; genuine Windows/Linux62-record outputs
  match. Linux109controlsPASS; Windows105PASS/four missing symlink privileges.
- [x] Original11input hashes/33frozen bytes preserved; no source authority or
  model promotion. Historical two Linux cases are retained unchanged.
- [x] Exact original services/images healthy/API200/inventory preserved.
- [ ] Real protected browser, remaining role/workflow/security/CI/build/model/
  release gates remain OPEN. Candidate loading/training/activation stop remains.

Read QA/REPORTS/LOCAL_C5_ORIGINAL_BINDING_2026-11-06.md. Next local claim is
QA/REPORTS/LOCAL_NATIVE_SESSION_INTEGRATION_2026-11-06.md for current C3.2 native
refresh/concurrency SQL integration; no app/harness rewrite or audit restart.

## Cloud conversation/container source integrated — 6 November 2026

- [x] C3.2+C4 returned66implementation paths copied byte-exact;12vendor pins pass.
  Protected student/teacher UI adapters and login-epoch/session controls restored.
- [x] Fresh48case Identity/HTTP/SQL communication roundtrip PASS. All three missing
  receipt cases now pass locally; existing communication-drafts48/48PASS.
- [x] Full current frontend1,893PASS/one Windows symlink EPERMfailure/zero skips;
  no assertion weakened. Nginx synthetic component10HTTP checks plus query PASS.
- [x] Owned SQL/web resources removed; original services/images healthy/API200,
  complete inventories and lease release PASS. Backend/API1,984source unchanged.
- [ ] Exact-lock full image/current CI/audits, actual protected host/browser/device,
  remaining all-role/workflow/security/release and Mini qualification stay OPEN.

Read QA/REPORTS/LOCAL_L2_CONVERSATION_CONTAINER_2026-11-06.md for exact sources,
run/failure/receipt boundaries. Cloud lacks local raw receipts; do not claim its
three evidence cases passed or publish QA/EVIDENCE. No main/Azure/model activation.

## Current continuation — 6 November 2026 (supersedes old next-task instructions)

Fetch `codex/penta-host-integration-20261030`; integrated code checkpoint
`be3093a92ba9db1d822c33ecb55374665fcced73` contains the protected host fixes and
byte-exact cloud C3.1 role-entry source. Fetch the actual current branch tip;
this documentation-only continuation may be newer. Preserve main and dirty trees.
Mini return: `codex/local-c1-evidence-return-20261105` at
`e55e5b9622283f7be8c34f7a717369a8efd73a7d` (tested source216550e).

Completed since the historical 5November start:
- [x] L1 real migration/foundation/student/teacher SQL results with their recorded
  source boundaries; do not redo the enterprise audit.
- [x] Default-host lease singleton and eight actual-composition controls; current
  API1,984PASS/zero fail/skip. No interpreter/model activation.
- [x] C1 genuine local-input return: Linux1,738PASS/two strict provenance failures/
  one inherited skip; separate Windows39metadata/24native/six actual-kernel checks.
  Original input/packet hashes and33frozen checkout paths preserved.
- [x] C2 static359declarations/81controllers match current source; static only.
- [x] C3.1 copied exactly from d0d7967/cloud reportcc5c8de. Local routing/login93
  PASS and separate session93PASS. Entry honestly says conversations unavailable.

Next ownership, without overlapping source edits:
1. Cloud C3.2: claim protected conversation/context/session paths before restoring
   real student/teacher draft/approval/execute/receipt UI. Preserve host authority;
   do not mount fixtures or present unavailable tools as live conversation.
2. Cloud C4: exact-lock production build/audit/CI. Local cached Next16.4.1 is not
   locked16.4.3; no wrong-version local build or unauthorized download substitute.
   Actual production Dockerfile restore remains a prerequisite despite the cached
   runtime smoke. Three genuine communication-evidence frontend cases stay open.
3. Cloud C5: versioned portable exact-original-parent/source binding or retain the
   original Windows-only review. Two Linux review cases remain failing; do not
   rewrite originals/expected hashes or normalize authority implicitly.
4. Local L2: actual isolated role/session/Manual-fallback browser validation once
   pinned build dependencies are available; preserve source-bound existing SQL.
5. Continue current remaining workflows/all roles, privacy/security/device/P0,
   DB/integration/restore/staging/release program. Main/Azure remain untouched.

Read the three6November reports in QA/REPORTS: LOCAL_C1_EVIDENCE_RETURN,
PENTA_HOST_COMPOSITION and LOCAL_C3_ROLE_ENTRY_INTEGRATION. Original cloud C1/C2
and source-bound C3.1 results are not active model/AI acceptance. Mini/Plus/Pro
remain unqualified; Ultra deferred; candidate loading/training/inference/activation
stop remains. Sol High for security/contracts/integration; Sol Medium for settled
visual/docs work. No new test plan or frontend rewrite from zero.

The older numbered starting tasks below are history where they conflict with
this section. GitHub is the durable exchange; no chat/laptop live sync is claimed.

## Current start — 5 November 2026

Local L1 host SQL module results are now available in
QA/REPORTS/PENTA_HOST_SQL_QUALIFICATION_2026-11-05.md: migration/foundation/student
PASS plus corrected teacher-only successor PASS, services restored. Actual model,
current full CI and release remain OPEN. Cloud C1/C2 continue; no audit restart.

Use CLOUD_CODEX_START.md and CLOUD_LOCAL_WORK_PLAN.md from this published feature
checkpoint. They supersede the older single-task continuation prompt below with
the owner's daytime-cloud / evening-local split. Confirm the remote SHA before
starting. The protected host source remains NOT QUALIFIED. Cloud first takes
pure Core portability and read-only action coverage; local owns protected host
SQL. No cloud task was automatically launched or directly messaged.

Updated30October2026. GitHub source is available; the original laptop's running
services, ignored evidence, dependencies and model caches are not transferred.

## Verified published checkpoints

| Repository | Branch | Commit |
| --- | --- | --- |
| AcademyDesk/AcademyDesk | codex/penta-teacher-service-integration-20261029 | c248912041ac55fbc84ea640fa5b355a54a4e45a |
| AcademyDesk/AcademyDesk | codex/penta-enrollment-integration-20261029 | 4e707629f23a9ef14a92186890fcf85b65c7d85b |
| AcademyDesk/AcademyDesk | codex/penta-contract-integration-20261029 | d44aea500dc14f050e50c78923b0435119b892aa |
| AcademyDesk/AcademyDesk | codex/finance-cloud-reconcile-20261029 | a74a9e44385441a885056ad485229f30b77118d6 |
| AcademyDesk/AcademyDesk | codex/cloud-profile-sync | 00d0d00f0264bb913065aa040a9f0ef0967771b6 |
| AcademyDesk/AcademyDesk | codex/penta-local-source-20261029 | 6887e6eb26e88f8fc99eee7a00c267a14c12883f |
| AcademyDesk/PentaMini | codex/mini-local-source-20261029 | c3c61b9ef3f239035c8b8f46649c66997e98f2b6 |

The first five integration commits form a chain. The original application/Mini
preservation branches hold the remaining source; they must not be merged wholesale.
Accepted main is unchanged atf7af512. Do not mistake a preservation branch for
the integrated application or a production release.

Teacher publication has been independently fetched and verified atc248912.
The next student-service checkpoint is
`codex/penta-student-service-integration-20261029` fromc248912, with1606API and
50SQL/HTTP/service checks including5mixed duplicate races. Owner authorized its
publication on30October. Fetch and verify the actual remote branch/SHA before
using it; do not infer publication from this document alone.

The teacher-service continuation is on
`codex/penta-teacher-service-integration-20261029`, based on4e70762. Fetch and
verify whether this branch has been published and read its actual handoff and
validation report. Do not assume that this document itself grants publication
or that an intermediate running validation passed. If not published,4e70762 is
the last verified integration checkpoint listed here.

## Current source continuation and resource block

The qualified integration base is64b222d oncodex/cloud-penta-reconcile-20261030.
A newer SOURCE CHECKPOINT,NOT QUALIFIED iscodex/penta-host-integration-20261030.
Fetch and verify it if present to retrieve all remaining host work. It contains
1976passing API source and an additive12table migration with4actual migration
checks/foundation SQL passed. Final private bridge/student/teacher SQL is still
unaccepted:bridge fixture was restored/configured, but3GiB guard refused the
latest retest. Production writes stay Testing-only. No model was qualified.

Do not rerun accepted audits or replace this source with an older branch. Runtime,
models, ignored evidence, secrets and local dependency caches do not transfer.
Read AI_HANDOFF.md and PENTA_HOST_IMPLEMENTATION_STATUS.md for exact pending work.

## Exact continuation prompt

```text
Continue Academy Desk from the verified published codex/penta-host-integration-20261030
SOURCE CHECKPOINT if present; otherwise use qualified codex/cloud-penta-reconcile-20261030
at64b222d. Record exact branch/SHA/status and preserve uncommitted work. Read AGENTS,
AI_HANDOFF,AI_CROSS_PROFILE_HANDOFF,AI_MODEL_ROUTING,AI_PRODUCT_VISION,
ENTERPRISE_TESTING_ROADMAP,WORK_LAPTOP_START and PENTA_HOST_IMPLEMENTATION_STATUS.

This source already includes student13ed932,teacherc248912,enrollment4e70762,
financea74a9e4,contractsd44aea5and payroll00d0d00, plus the other profile's8cloud
commits througha63e392. Do not reapply them wholesale. The retained source
snapshots are Academy6887e6e and Mini c3c61b9. Main and Azure are unchanged.

Finish only unaccepted host validation first: configure QA_PENTA_BRIDGE_SOURCE
with the actual pinned Mini checkout and QA_PENTA_BRIDGE_PYTHON with an available
Python>=3.12. Use the existing pure Core requirements and scripted fixture, no
real model/generation. Current fixtures pass no customer data and reject ML
runtime imports. The original machine needs3GiB free RAM; Docker restart needs
its pending separate approval. Do not weaken guards or stop unknown apps. Original
machine service-stop windows do not authorize stopping office-laptop services.

Build/run the existing SqlHarness PentaHostMigration,PentaFoundation,
PentaOnboardingProtected and PentaTeacherProtected modules on fresh owned SQL.
QA_PENTA_HOST_SECURITY=1 activates fake-planner security; QA_PENTA_MINI=0 keeps
real/optional model tests off. Reuse only exact source-bound passing evidence;
retain failures, full assertions, owned cleanup and exact service restoration.
API1976is a combined subset, not a replacement for the preserved3183test program.

Student/teacher writes stay Testing-only. Complete the actual qualified
interpreter/host connection,remaining workflow cohorts and PENTA/Manual frontend
integration/all-role/device acceptance next. Owner stop before candidate loading
stands: no training/inference experiment/activation/downloads/paid compute/Azure.
Mini semantic result is NOT ACCEPTED; full Mini cloud CI previously failed.
Private paired-Core CI may need PENTA_SOURCE_READ_TOKEN; do not create credentials
or change repository settings without authorization. No fabricated receipts or
hidden test skips. Finish exact-source web/CI/security/privacy/migration/restore/
critical/staging/release gates before proposing production. Website stays deferred.

Stay onSol High for consequential host/authority/transaction work;Sol Medium for
settled UI/docs. Publish reviewed source to a feature branch under the owner's
current authorization. Never merge main,force-push or deploy implicitly.
```
