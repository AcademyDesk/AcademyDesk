# Developer CI and verified-artifact promotion — 26 October 2026

## Teacher Core floor advancement — 27 October 2026

Actual unfiltered3,065/3,065, zero skips. Artifact floor now3,065;33focused
artifact/workflow guards PASS, including refusing the older3,049receipt for a
new candidate. Historical smaller counts below remain valid for their source.
No GitHub execution/clean Linux install/release-image/staging/production approval.
See `PENTA_TEACHER_CORE_2026-10-27.md` and its linked c8ed13746d9c4cfbbcdfa2b4632f9cc2
qualifier; native UI qualification reuses that exact API source.

## Teacher workflow test-floor advancement — 27 October 2026

Current full unfiltered API suite3,049/3,049, zero skips; artifact floor now3,049.
All32artifact/workflow guard cases PASS, including rejecting the older3,040-test
baseline as a new candidate. Prior counts below remain source-bound history.
No GitHub/Linux workflow or release/deployment approval follows. See
`PENTA_TEACHER_WORKFLOW_2026-10-27.md` and final linked qualifier
`QA/EVIDENCE/penta-teacher-qualified-a686184fde8b440ba279a87eaa9fa258/`.

## Accepted test-floor advancement — 26 October 2026

After the teacher shared-service change, the actual unfiltered suite passes
3,040/3,040 with zero skips. The existing artifact guard floor is now3,040;
31focused artifact/workflow checks PASS, including refusing the older2,995-test
report as a new candidate. Earlier30-test/2,995 acceptance below is historical,
not silently rerun or relabeled. GitHub/Linux CI, image/staging/release acceptance
remain unperformed. Linked actual TRX/SQL/current source/CI guard evidence:
`QA/EVIDENCE/teacher-creation-qualified-f37a333136c4476a9aa513ec83cf3c87/`.
See `TEACHER_CREATION_SHARED_SERVICE_2026-10-26.md` for the backend scope.

## Implemented batch

The existing manually dispatched Azure workflow previously changed Azure/API
resources before building the web portal, and ran no API tests, lint or explicit
TypeScript checks. A web build failure could leave an API-only update. This batch
adds a reusable developer verification workflow and promotes its exact API image
and web export instead of rebuilding after verification. It does not authorize
or perform a deployment, establish production readiness, or replace the existing
enterprise testing program.

- [x] `.github/workflows/verify.yml` runs on main-targeted pull requests, explicit
  dispatch and local reusable-workflow calls. Read-only repository permission;
  no inherited/production secrets, deployment environment, Azure login, registry
  push, real database, model load or training.
- [x] API job restores/builds the existing solution, runs the existing unfiltered
  API suite and requires all passing/zero skipped with the retained **2,995-test
  minimum**. It builds/saves one candidate Docker image and binds its archive
  hash and passing counters to the exact clean source SHA, rejecting tracked and
  untracked source edits. This is the
  standard Dockerfile build, not a relabelled old offline image.
- [x] Web job uses locked `npm ci`, lint with zero warnings, TypeScript, these
  release-guard tests and the actual static build/postbuild. The candidate export
  is bound to the exact source SHA, API origin and complete file-size/hash list.
- [x] API/web jobs run independently in parallel with bounded timeouts. Deploy
  needs the complete reusable workflow to succeed, then downloads artifacts from
  that same workflow run and checks source, hashes, shape and web API origin
  **before Azure login or mutation**. Missing/changed/wrong-origin/placeholder
  artifacts fail closed. Deployment loads/promotes the tested image and copies
  the checked export; it does not rebuild either artifact.
- [x] Actions use immutable official commit refs. Explicit Bash defaults preserve
  pipeline failure semantics. Deployment remains manual, uses its existing
  production environment and has no always/continue-on-error bypass.
- [x] Test diagnostics retained even on API failure; candidate artifact retention
  is seven days. No QA/EVIDENCE upload, production credentials or test/customer
  database is required by verification. Artifact manifests explicitly record
  `developerChecksOnly:true`, `stagingApproved:false`, `productionApproved:false`.

## Actual local validation, not GitHub execution

- **30/30 focused Node tests PASS, zero skips.** Includes all-pass vs filtered/
  failed/skipped/aborted/timed-out TRX, result/counter mismatch, external-entity/
  duplicate counter rejection, payload/hash/source/origin tampering, extra/missing
  files, symlink refusal, duplicate manifest rows, forged approval flags, PR origin
  promotion refusal and workflow dependency/secrets/rebuild/dispatch/pipefail
  bypass regressions, plus dirty/untracked/incorrect-source rejection. Both actual
  YAML files parse via existing locked js-yaml.
  Final current-source receipt/TAP:
  `QA/EVIDENCE/ci-release-gates-17da7469caa44ea4970a0157da7a23de/result.json`
  and `checks.tap`. The receipt pins all five workflow/guard/test source files.
  Earlier28-test evidence is retained in its separate owned local folder; it
  does not substitute for the final30-test source-bound result.
- **17/17 actual workflow run scripts passed existing Git Bash `-n` syntax
  validation.** Commands were not executed. Result:
  `.build-check/ci-shell-2ce4a63a13c84d78b06a00a30e84c208/result.json`.
- The new counter reader also reconciled the retained **actual 2,995/2,995,
  zero-skipped TRX**, not just generated fixtures:
  `QA/EVIDENCE/admission-build-window-c79968927c2a4ab085830a58e7075220/admission.trx`,
  SHA-256 `c360ed84eeded5def7f79f440515811b3b751e97144fb364b1fc9e6348c8f621`.
  That suite was **not rerun** or presented as new application evidence.
- Node syntax and `git diff --check` PASS. Local synthetic artifact fixtures are
  retained under ignored `.build-check/ci-artifact-guards-*`; no app/customer
  records or containers were created, modified or removed by this batch.
- **NOT RUN:** GitHub-hosted jobs/action runtime, fresh Linux suite, standard
  Docker build, fresh web build, registry promotion or Azure deployment. The
  workflow files/guards are still local uncommitted work, so no CI run or branch
  protection/settings change is claimed. No unchanged application suite rerun.

Repeat focused checks:
`node --test QA/tools/ci-release-artifacts.test.cjs QA/tools/ci-workflow.test.cjs`.

## Current launch blockers and boundaries

This advances the developer/release infrastructure portion of the existing four
batches; it does not close E0–E10, existing P0 issues or staging/production gates.
Manifests are source/content consistency records, not signed independent
attestations, vulnerability certification or authorization to deploy.

1. **AI qualification:** exact setup/import/acquisition prerequisite passed, but
   current fresh6/12/exposed7/12 full-plan model is NOT ACCEPTED. Owner answered
   “Keep the completed setup; stop before model loading” to
   `request_user_input_async:call_32c44a4a21f7483297c8cd5b447a794c:0`.
   Preserve that boundary; urgency does not authorize load/train/activation.
2. **Connected conversational host cohorts:** reusable actual authorization,
   dialogue/draft/approval/domain/receipt/recovery and all required families still
   need completion/qualification. Two read tools and the synthetic/Testing-only
   onboarding foundation do not equal90%AI. Existing domain backend is reused.
3. **Customer-critical acceptance:** current role/tenant/finance/privacy/data-loss/
   false-success controls, Manual fallback and actual device/provider workflows
   must have accepted evidence. Retain already qualified slices; no broad restart.
4. **Exact release:** GitHub acceptance and actual new CI, staging runtime/SQL/
   migration/key protection/security/restore/load/device/rollback and deployment
   revision smoke remain. In particular the existing deployment configuration
   still enables startup migrations and bootstrap handling: this batch did not
   silently change those production behaviors or establish an approved schema/
   credential lifecycle. Resolve them under the existing release gates before
   running production deployment. No credentials/settings were inspected here.

Next engineering route: **Sol High** for real authenticated operational host
integration and release/migration/security decisions; Sol Medium for settled UI.
Do not claim that the refused model-load test is now approved or ask repeatedly
without new owner direction. Production remains **NOT READY**; no date or90%
coverage claim. No commit/push/main/model/paid-compute/Azure changes this batch.

Workflow mechanism references checked against primary GitHub docs:
[reusable workflows](https://docs.github.com/en/actions/how-tos/reuse-automations/reuse-workflows)
and [workflow dependency syntax](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idneeds).
Action SHAs resolved read-only from their official GitHub refs; Azure/login's
annotated v2 tag was dereferenced to its commit. No tool/package was installed.
