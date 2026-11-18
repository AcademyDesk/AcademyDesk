# C50 — broad cloud regression batch

Cloud / Sol High, 17 November 2026, Asia/Calcutta.
Branch: `codex/cloud-c50-regression-batch-20261117`.
Clean base: current Desktop
`2b286995d47895403542961e9463739ebb8c1298`, freshly fetched.

## Ownership before integration and testing

Only these paths may change:

- `apps/web/src/components/private-material-actions.tsx`: reviewed C49 bytes.
- `QA/tools/private-material-actions-browser.cjs`: reviewed C49 browser runner.
- `QA/HANDOFF/C49_PRIVATE_PREVIEW_SCOPE_2026-11-17.md`: original C49 handoff.
- this C50 task-specific result/handoff.

### Scope amendment after the first complete browser batch

The first20-suite run completed19 PASS /1 FAIL. All eight failing cases are
in `QA/tools/manual-controls-browser.cjs`: its old adjustment fixture supplies
only numeric `amount` and omits current server fields `amountExact` and
`currency`. The current UI correctly disables approval when exact financial
terms cannot be verified. No mutation was sent in those failing cases.

Cloud additionally claims `QA/tools/manual-controls-browser.cjs` only to update
its synthetic producer to the existing `FinanceAdjustmentSummary` contract and
retain all original approval/rejection assertions. Add explicit missing-exact-
amount/invalid-currency no-write controls so the safety refusal stays tested.
No product financial behavior, UI guard, source DTO or API will be changed.
Desktop's latest60036cfb changes do not overlap this runner.

Integrate C49 final commit `ed252d3d631b85fc1374fa2e6e19f4983716e3a0`
into the newer Desktop source. C48 is already integrated there. No new
application behavior or test weakening is planned. Preserve prior branches,
dirty work, raw evidence and all current source/lock/model/vendor boundaries.

The user explicitly requested a large test batch. Run fresh full backend and
frontend suites, types/lint, exact-lock production build, dependency advisory
checks and existing release guards. Run all20 available self-contained CDP
browser suites in two concurrent lanes against the exact built source, with
owned loopback fixtures/profiles. Preserve each previous evidence directory
before execution and collect every suite result, including failures.
Mini deterministic checks remain no-model; existing original-evidence failures
must stay visible and separate from application failures.

Historical Windows/session runners that require absent protected SQL, original
fixture ownership or source-bound receipts must remain not-run prerequisites.
No substitute fixture may be labelled as their real acceptance. Desktop owns
the protected native SQL, physical devices, staging and private-model work.

No main merge, Azure deployment, production database access, environment
publication, public-access change or model download/loading/training/inference/
activation. Mini/Plus/Pro remain unqualified; Ultra deferred;90% is unmeasured.
All six critical release acceptance records remain open.

## Completed results — 17 November 2026 UTC

The full requested cloud batch is complete. This result is source-bound to
application commit `fbc1d0d5167cb7fdac78e60c2ab2c15ee1f1854c` (C49 applied
on the Desktop base above). Subsequent changes are this report and the bounded
Manual browser fixture correction; `apps/` and `tests/` remain byte-identical
to that tested commit. SDK 10.0.403, Node24.19.0, installed lockfile
Next16.4.5 / React19.2.8 and system Chromium were used.

| Check | Fresh result | Limit |
| --- | --- | --- |
| Full backend | **2,493 PASS, 0 failures/skips** | Synthetic test suite; real SQL acceptance separate |
| Full frontend, 92 files | **2,559 PASS / 3 FAIL**, 2,562 total | Three original private communication receipts unavailable here |
| Self-contained Chromium suites | **20/20 suites; 601/601 groups PASS** | Intercepted synthetic loopback API contracts |
| Additional native download probe | **4/4 groups PASS** | Real Chromium disk save, synthetic API |
| Full Mini deterministic suite | **1,738 PASS / 13 FAIL / 63 setup errors / 1 inherited skip** | All 76 nonpasses are FileNotFoundError for absent original private archives; 5 subtests also pass |
| TypeScript | PASS | Current application source |
| Lint | 0 errors, 61 inherited warnings | Warnings remain visible |
| Exact-lock production webpack export | PASS | 90 static segment aliases; no deployment |
| SQL harness compile | PASS, 0 warnings/errors | No database or migration execution |
| npm dependency advisory audit | 0 reported vulnerabilities | Advisory result at execution time |
| NuGet transitive advisory audit | No vulnerable packages reported | Both API and test projects queried successfully |
| Production-gate checker tests | 15 PASS | Included within frontend totals; do not add twice |
| Actual production-gate decision | BLOCKED / exit1 | Six critical acceptance records remain open |
| Legacy QA evidence validator | NOT COMPLETED / ENOENT | Original `QA/EVIDENCE/logs/observed-checks.json` absent |

Mini parent-case total is1,815; the JUnit XML includes five passing subtests
and therefore1,820 entries. The inherited skip needs a pinned offline native
proof binary. These checks did not acquire, load, train, run or activate a model.

### Backend initial failures and secure rerun

The initial full backend run was2,491 PASS /2 FAIL. The two protected key-ring
persistence/certificate-rotation cases refused temporary storage because this
managed workspace now has `.git` markers at both `/tmp` and `/workspace`.
The application correctly prohibits a persistent key ring under Git ancestry.
Those first failures and TRX files are retained.

Provisioning an owned random0700 directory under non-Git `/var/tmp`, setting
`TMPDIR` only for the test process, and rerunning the entire already-built
suite produced2,493 PASS. The temporary synthetic-key directory was removed
on completion, with receipt `removed:true, exit:0`. No Git marker, security
rule, application code, key policy or production key was changed.

### Browser failure fixed without changing financial behavior

The initial browser batch was19 passing suites and one failing suite:
591 passing groups and eight failed Manual finance cases. The synthetic
adjustment producer predated the current DTO; missing `amountExact` and
`currency` made the correct UI refuse to enable approval. No PATCH was sent.

After publishing the scope amendment, only the Manual test fixture was
updated to the current server response, retaining all existing approve,
reject, cancellation and403 assertions. Added an exact `USD 10.00` display
assertion plus two explicit missing-amount/invalid-currency cases proving
approval remains disabled with no dialog, write or fixture commit. All17
Manual cases now PASS. The other19 suites already passed and their source
was unchanged; no unnecessary rerun replaces that original evidence.

Prior browser evidence directories were copied before execution. Both the
original failure summary and corrected17-case result remain available.
The final aggregate replaces only the corrected suite and includes its two
new refusal cases. This is20 passing suites /601 passing groups.

| Browser suite | Passing groups |
| --- | ---: |
| `academic-inputs-browser.cjs` | 30 |
| `compensation-browser.cjs` | 44 |
| `dashboard-currency-summary-browser.cjs` | 14 |
| `dashboard-detail-availability-browser.cjs` | 26 |
| `expenses-recovery-browser.cjs` | 24 |
| `family-finance-browser.cjs` | 24 |
| `fee-reminder-eligibility-browser.cjs` | 6 |
| `finance-batch-browser.cjs` | 49 |
| `finance-currency-summary-browser.cjs` | 16 |
| `manual-controls-browser.cjs` | 17 |
| `music-progress-browser.cjs` | 42 |
| `penta-enrollment-assistant-browser.cjs` | 32 |
| `penta-enrollment-creation-browser.cjs` | 52 |
| `penta-enrollment-lifecycle-browser.cjs` | 104 |
| `penta-entry-browser.cjs` | 24 |
| `platform-currency-overview-browser.cjs` | 10 |
| `private-material-actions-browser.cjs` | 16 |
| `reports-finance-browser.cjs` | 14 |
| `status-navigation-browser.cjs` | 49 |
| `support-triage-browser.cjs` | 8 |

The separate four-group actual Chromium probe saved68MiB (71,303,168 bytes)
through the native download manager and verified its SHA256. It also passed
partial-response refusal, pending-read abort/lock release, and oversized-ticket
refusal. Chromium sandbox remained enabled; owned profile and fixture process
were cleaned up. The probe and20-suite batch do not qualify live Identity,
SQL, storage provider, physical devices or production account journeys.

### Missing originals remain failures, not replacement acceptance

The three frontend failures are the Email, WhatsApp and Meeting historical
real HTTP draft receipts in `communication-drafts.test.cjs`; all require
`QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log`. They remain failures.
Mini's76 failures/errors each identify absent original private archive paths,
including the protected laptop operations archives. Desktop has reported local
recovery of originals; this cloud checkout has not received those raw archives.
No original was fabricated, altered, substituted, skipped or committed.
The legacy `validate.cjs` additionally requires the absent original
`QA/EVIDENCE/logs/observed-checks.json` and therefore cannot validate the register
here. Historical Windows/session runners with protected prerequisites were
not executed or represented as completed acceptance.

## Exact published Desktop CI and urgent handoff

Freshly fetched Desktop remains
`60036cfb51345870a6999c6b3fee96d034b4472e`. The application `apps/` and `tests/`
trees match this batch. Its existing C49 integration is already present there.
GitHub run [44062528045](https://github.com/AcademyDesk/AcademyDesk/actions/runs/44062528045)
is **completed / failure**, with both jobs failed:

1. **PENTA protected onboarding and private Core transport**:
   failed at `Require access to pinned private Core source`. GitHub annotation:
   `Configure PENTA_SOURCE_READ_TOKEN with read-only contents access to AcademyDesk/PentaMini.`
   Repository access in this cloud chat does not provide the separate Actions
   secret. Do not remove this requirement or label the job passed.
2. **API, SQL, private media and frontend verification**:
   failed at `Guarded finance, tenant access and private-file SQL checks`.
   GitHub annotations identify `PayrollReconciliationRegression.cs:169,173`
   and `Program.cs:114,560`. Line169 requires two observed exact-resource SQL
   lock waiters and one201/one409; the annotation alone does not disclose the
   observed waiters/status values or establish a timing versus product cause.
   Raw hosted log downloads were denied by the storage redirect (403);
   check/job metadata and annotations were read successfully. Preserve and
   inspect the exact failed hosted payroll receipt on Desktop before changing
   anything. This job is failed even though the cloud synthetic suite passes.

Desktop owns that protected SQL investigation and validation; no overlapping
SQL/harness/migration edits were made here. Highest next acceptance work is:
resolve the exact hosted payroll failure with source-bound evidence, restore
read-only private Core access for Actions, and finish native protected-host
acceptance plus the required original communication/archive evidence.
Desktop's new Guardian revocation proof is a bounded result, not closure of
the entire private-media or session acceptance record.

All six release blockers remain open: `BUG-DATA-0001` collections,
`BUG-DATA-0002` adjustment consumers, `BUG-DATA-0003` payroll,
`BUG-DATA-0010` void/restoration, `BUG-SEC-0001` private media, and
`BUG-AUTH-REVOCATION-001` broader sessions. Mini/Plus/Pro are not qualified,
Ultra is deferred, and90% autonomous completion remains unmeasured.

### Import and preservation

Desktop already integrated the same reviewed C49 product/helper bytes.
Import only the final C50 fixture/report commit into its feature branch;
do not replay C49. No root tracker or shared governance file was edited.
Against the claimed base,1,812 unclaimed tracked files and all16 vendor files
are byte-exact. All33 frozen Mini files match their original recorded hashes;
Mini is clean at `14c11fcf74ce51c4d790b0fd6db74ae2aab25cf8`. Application manifests
and lockfiles are unchanged. Prior branches, original logs, working copies
and previous evidence were preserved. No main merge, deployment, environment
publication or model activation occurred.

## Raw evidence and integrity receipts

Private raw run directory: `/workspace/c50-regression-batch-20261117`.
It retains the original failing backend/browser runs as well as corrected
results, per-suite stdout/evidence, TRX, JUnit, build/lint/audit output, missing-
prerequisite detail, source preservation and GitHub job/annotation metadata.
No private operational archive or synthetic key material is added to Git.
The committed report records results; these hashes bind its local raw receipts:

| Receipt relative to the private run directory | SHA256 |
| --- | --- |
| `backend.log` | `73b7387a84838d3afb96e96de547150854db6250a1d200f99bb6bccea3dac2d4` |
| `backend-private-root.log` | `0fcbcac29219bddeca9b81dcd31fffc35b4d3773b9ef4637f22920f86bdc1c4e` |
| `private-test-root.json` | `e3a745fd8fbab05f35219d10220ecd327aa449460b7b95f9095918781bddd78e` |
| `frontend-summary.json` | `d8f53270ecb6d4c81bcfa0aef43a8e05e9b0d4e1a930ffde85d5bcb508a3bd03` |
| `browser-summary.json` | `097305d63a4d787a98b4a3a9a3f8128ac615b8e91097bf40181fdf12992c617d` |
| `browser-summary-final.json` | `bdae6edf7b7d4dece1d3d28693ad65775930aa5fc29557d93ddb283aa448f8c0` |
| `manual-controls-corrected.log` | `d716033ab4c8cdddedece1d00ae4c48141aaaace8f0757212290b960323877ae` |
| `native-browser-result.json` | `9035a86694987c49f8e76eb1b1e29b96990d15a98aa8b13ae63de0338c7fb004` |
| `mini-tests.xml` | `8d03f7864be3af44e1985a350a9333f5a39a4ec8ace3e86255dcbd6616c09a87` |
| `mini-prerequisites.json` | `66f0e4a28e941d4e09d7c519c6ac9d43124152ae00003d72d989a84fc3e80c8c` |
| `npm-audit.json` | `757f281f275d8b38c76804ad08f260353f9b6bd4f525ac1d0e9b21714aaf694b` |
| `nuget-audit.json` | `dca7be4077eb34b17fe01f1ed7b64a5f9c9329622180f7f6d44b7cd0d65e64f9` |
| `source-integrity-final.json` | `44bcc01f2d851f1693c55858416289a37b0f9d51ffb65f8a6d971f4301ca89b5` |
| `desktop-ci-jobs.json` | `915242ed6f72a2cc86842732a3b5f0ca7f7b47b5592421cfcca2d2bda255a2ef` |
