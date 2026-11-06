# Cloud C6 Manual financial/Teacher controls — 6 November 2026

Status: SOURCE FIXES / CONTROLLED VALIDATION COMPLETE; live acceptance OPEN.
Base83dc3aa7e8390f7af99a4f1e648ddfc18363d4dc;
Desktop L2 integration/local SQL report read first. No source overlap with its
published C3/C4 protected-host/browser packet.

Exact cloud-owned paths:

- `apps/web/src/app/finance-governance/page.tsx`
- `apps/web/src/app/teacher/page.tsx`
- `QA/tools/finance-governance.test.cjs`
- `QA/tools/teacher-publication-contract.test.cjs`
- `QA/tools/manual-controls-browser.cjs`
- `QA/ISSUES/BUG-FUNC-0005.md`
- `QA/ISSUES/BUG-API-0005.md`
- `QA/HANDOFF/C6_MANUAL_CONTROLS_2026-11-06.md`

Fix existing P1cancellation/JSON-publication contracts, including Teacher
assessment's optional-date serialization. Preserve optional empty approval notes,
required rejection reasons, backend/tenant/role/finance authority, and form data
on unconfirmed saves. Reuse preserved Teacher contract tests from6887e6e.
Do not wholesale import its unrelated Manual page or superseded finance policy.

Validation: reproduce actual current TSX handler failures before fixes, controlled
native FormData and real Chromium tests, exact-installed-lock build/types/lint,
full frontend regression with cloud's original SQL-evidence prerequisite failures
retained. No genuine local SQL/browser/device/production closure is inferred.

No backend/API/SQL/migration/PENTA-session/provider/model/registry, shared status
pointer/index/workflow or package/lock edit. No new package/base-image download,
remote npm audit retry, environment change, model loading/training/inference/
activation, main merge or Azure deployment. Original issue history remains;
final issue acceptance stays OPEN until the existing live closure gates pass.

## Returned source

Implementation `1ec22e733696fa84e4dbf947da463fbe4a2d123a` on
`codex/cloud-c6-manual-controls-20261106`. Five bounded implementation/test files;
this report and two issue records supply the source-bound handoff. No shared
integrator pointer/index or Desktop-claimed file was edited.

- BUG-FUNC-0005: native prompt cancellation returns before any decision request,
  saving-state change or refresh. Explicit OKwith empty optional approval note
  remains allowed; rejection still needs a nonblank reason. Financial/domain/
  permission rules and endpoint are unchanged.
- BUG-API-0005: Teacher assignment/assessment flags become actual JSON booleans;
  blank assessment date becomes null and filled dates retain existing UTC
  conversion. Invalid publication/date values send no request. The maximum-score
  input accepts fractional values supported by the existing decimal DTO, fixing
  an additional native integer-step blocker found during browser reproduction.
- Teacher create captures the submitted form before await and resets it only on
  success. Denial retains it; connection interruption produces an unconfirmed
  message, retains form data and never automatically resubmits. No new idempotency
  or backend receipt/SQL claim is made by that UI behavior.

Reuse the preserved Teacher contract tests from6887e6e, not that branch's older
full page/finance policy. No new package or lockfile/dependency change.

## Actual baseline and final evidence

| Check | Result / exact boundary |
| --- | --- |
| Pre-fix finance actual TSX |14PASS/2FAIL: approve Cancel mutated; reject Cancel incorrectly reported validation |
| Pre-fix Teacher actual TSX |7PASS/5FAIL in12preserved cases: string publication flags and empty optional date |
| Preserved pre-fix browser export |C4export570file hashes all match; app source equivalent to83dc3aa on these paths |
| Pre-fix Chromium/native prompts/DOM |15cases:7PASS/8FAIL; zero runtime exceptions; failures retained, not acceptance |
| Final finance handler suite |**16PASS**, including seven new decision cases |
| Final Teacher native FormData suite |**20PASS**:12preserved +8new malformed-input/network/no-replay cases |
| Final real Chromium |**15/15PASS**, zero runtime/console errors; all APIs intercepted, no model/live writes |
| Actual compiled API DTO JSON |**7captured Teacher payloads accepted /9original boolean-string/empty-date variants rejected**; no HTTP/Identity/SQL execution |
| Next16.4.3 build, webpack |PASS;89static generation entries; postbuild87flat payloads unchanged |
| TypeScript / ESLint |PASS /0errors,63existing warnings |
| Full cloud frontend |**1921tests:1918PASS/3FAIL/0skips/cancelled**,69files; runner remains exit1 |
| Source boundaries |No API/domain/harness/SQL/migration, PENTA/session/provider/model, registry, workflow or package/lock edit |

Cloud's three failures remain genuine missing
`QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log` receipt cases.
Desktop83dc3aa separately produced an actual48case communication SQL/Identity/HTTP
run and passed those local cases; it did not transfer the raw receipt to cloud.
Those local results are not substituted into cloud's current counts. New cases
all pass, and the existing Windows symlink privilege prerequisite stays separate.

Native browser coverage: eight financial cases exercise actual JavaScript prompt
opening/dismissal/affirmation and request absence/exact payloads; seven Teacher
cases use real custom assigned-batch selection, DOM/native FormData and browser
validation, with false/true publication, blank/filled dates and fractional score.
A deliberate connection-loss fixture checks unconfirmed form retention and one
request. This is controlled browser behavior, not a live server/persistence or
physical keyboard/device qualification. Prompt dismissal's null value is shared
by Cancel/Escape; physical Escape interaction was not independently measured.

DTO checks read those exact browser-captured JSON bodies using the already-built
unchanged `AcademyDesk.Api.dll` and System.Text.Json web defaults. They exercise
TeacherCreateAssignmentRequest/TeacherCreateAssessmentRequest/
TeacherCreateLessonPlanRequest, including decimal12.5and nullable/UTC dates.
The comparison tool runs outside the repository with existing SDK/runtime; it
never starts the application or a database, restores dependencies or changes a
DTO. No custom bool/date converter is registered in current Program.cs. The
assembly hash/source boundary and control logs are retained; API schema agreement
is not endpoint/domain/authorization/SQL acceptance.

Artifacts `/workspace/c6-manual-controls-20261106`: before/after handler logs,
pre-fix artifact hash receipt, baseline/final native-browser traces and captured
synthetic payloads, compiled-DTO comparison source/log/hash, build/lint/full suite
summary and implementation hashes. Detailed frontend logs stay in ignored
`.build-check/frontend-tests`; native browser receipts preserve previous results
before each new RUNNING/PASS/FAIL state. No original local evidence, customer data,
compiled artifact, dependency binary or credential is committed.

## Parallel Desktop status and next acceptance

Latest published Academy checkpoint observed during C6:
`a465c57a575c1cf0e2ec6811c146b416ce02c705`. Desktop owns current native session SQL
helper work in QA/REPORTS/LOCAL_NATIVE_SESSION_INTEGRATION_2026-11-06.md:
session-refresh-http.cjs and refresh-concurrency-http.cjs. Cloud did not touch them
or api.ts, protected PENTA UI, SQL harness, original services or shared pointers.

Desktop also returned Mini `a789f1aef75a5bcd5cf0b0a21ed1a29c830cb4e4`:
read-only C5v1genuine Windows/Linux62record results match; Linux109controls PASS;
Windows105PASS/four missing-symlink-privilege failures retained. Original11input
hashes/33frozen bytes remain intact. This resolves the **v1genuine-input** gate,
not the old two historical Linux cases or model qualification. No original is
rewritten and no source/training/host/model authority is promoted.

Desktop next, when its current packet is complete:
1. Review/cherry-pick only C6implementation1ec22e7and reconcile this report/issue
   updates. Existing C3/C4 are already integrated; do not reapply them.
2. Verify current same-tenant permitted financial approval/rejection and Teacher
   assigned-batch HTTP/SQL behavior using existing guarded synthetic-data harnesses:
   canceled actions make no mutation/row/status/audit-write changes; deliberate
   actions preserve exact server-authorized payload and result; invalid/denied/
   foreign-tenant/batch cases remain blocked. Verify exact-one-row saves and form
   reset/retention against real outcomes, with current-source role/session context.
3. Retain the original issue closure protocol and critical/device/security gates.
   Both P1records stay OPEN until that live acceptance is complete; no fake receipt,
   assertion reduction or synthetic backend substitute may close them.

Actual protected browser/all-role workflows, original full model qualification,
current image/runtime/CI/audits, privacy/device/P0/DB/restore/staging/release and
90%AI outcome coverage remain separate gates. The npm audit disclosure request
is still pending after earlier automatic-review rejection; it was not retried.
No new packages/images, main merge, Azure deployment, model acquisition/loading/
training/inference/activation or environment/private-access policy change.
Next route: Sol High for source-bound integration/security acceptance; Sol Medium
for settled docs/visual work. Mini/Plus/Pro remain unqualified; Ultra deferred.
