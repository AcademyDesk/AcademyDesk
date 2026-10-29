# Frontend callback and warning repair — 21 October 2026

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Continues the existing quality queue;
does not restart enterprise testing or claim AI/application completion.

## Delivered

- Removed all 14 hook-dependency warnings and eight unused declarations/bindings.
  Fifteen application pages touched; no lint rule/config/package suppression.
- Stabilized read callbacks in Batches, Guardians, Platform Control, Teacher,
  Guardian/Teacher profiles, Teacher Payments, Staff, Teachers, Schedule,
  Invoices, Payments and the Student/Parent portal. Effect dependencies now name
  those callbacks without tying initial discovery to mutable drafts/selections.
- Record/academy IDs are explicit arguments rather than captured default values.
  Functional initial selections preserve a current draft; initial schedule batch
  defaults are requested explicitly. Post-save refresh still uses current scope.
- Parent portal refresh is bound to the currently selected child, not the child
  from initial discovery. Profile/payment record switches do not rediscover the
  academy or reset to the first record. Existing cancellation guards are retained.
- Removed four unused rendering helpers, an unused tenant FormData binding and
  unused tuple/state bindings; day removal no longer creates an unused variable.
  No API, domain, permission, finance calculation, approval or model policy changed.

## Verified

- Full configured ESLint: **0 errors / 14 warnings, exit 0**. All remaining warnings
  are `@next/next/no-img-element`; prior checkpoint was 0 errors / 36 warnings.
  `.build-check/session-resume/lint-warning-final.json`.
- Next webpack production build, TypeScript and **87/87 static routes PASS**:
  `.build-check/session-resume/warning-build.log`.
- Repository diff checks and parsing of all 15 affected/new JavaScript QA tools
  PASS. Only the final removal of a surplus EOF blank line followed the build;
  no executable application source changed after that export.
- **455 selected checks PASS, zero fail/skip/cancel**:
  `.build-check/session-resume/warning-regression-verified.log`.
  Includes 23 new dependency-aware callback/scope checks, retained read cancellation,
  batch payload/feedback, schedule defaults/outcomes, compensation zero/null,
  invoice/payment safety, ledger/lookups/settings, Teacher and notification tests.
  This selection overlaps the earlier 478 checks: do not sum the two totals.
- **196 exported-browser cases PASS**, not 196 real server/role/device tests:

| Receipt under `QA/EVIDENCE/` | Cases | Boundary |
| --- | ---: | --- |
| `portal-read-state-1792583686781/result.json` | 36 | Guardians, Platform Control, Teacher; loaded/denied/malformed; both themes, 320/1440px |
| `catalog-read-lifecycle-1792583719007/result.json` | 24 | Courses/Batches; switch/clear/late failure; both themes, 320/1440px |
| `schedule-feedback-browser-1792583744316/result.json` | 12 | Actual create/update handlers with synthetic writes; normal/rejected/readback failure; both themes, 320/1440px |
| `finance-document-preview-1792583787196/result.json` | 112 | Existing invoice/payslip preview templates, normal/long branding, both themes, 320/390/768/1440px |
| `profile-callback-1792583846705/result.json` | 12 | Guardian/Teacher profile and Teacher Payments; typed draft retained, visible selector switches to second record, no rediscovery/writes; both themes, 320/1440px |

The new profile browser uses real visible native/shared controls, not hidden-value
assignment. Browser API responses and tokens are synthetic. No real academy data,
SQL, server authorization, tenant isolation or physical Android/iOS acceptance.
Schedule POST/PUTs are intercepted fixture operations, not real persisted writes.

## Test-maintenance evidence, not concealed failures

- Initial controlled fixtures lacked `useCallback` and failed before assertions.
  Added callback support; dependency-aware fixtures memoize by dependency slot and
  reset only the hook cursor. Existing payload/outcome assertions remain intact.
- Functional initial-state setters required the finance lookup/settings adapters to
  execute functional updates rather than store the function as the selected ID.
- First new invoice draft test addressed the invoice-list state slot instead of
  amount; corrected that fixture index, not the application array contract.
- Broader selection initially produced 390/391 PASS: the old visual-preservation
  test froze Finance's `downloadExport` to the pre-CSV-repair HEAD. That handler
  intentionally changed in the already accepted CSV recovery checkpoint. It is
  now covered directly by loading the existing 22 actual-handler recovery tests
  from the preservation runner (MIME, duplicate/body-wait guards, errors, truthful
  status and URL cleanup), rather than reverting the repair or skipping coverage.
  All other frozen declarations remain compared. No Finance source change here.
- Initial/adapted logs are retained in `.build-check/session-resume/`; the final
  455-check receipt is the accepted result, not an edited initial log.

## Still open

The 14 image warnings cover document branding/signatures, avatars and private
image previews. No image implementation or security behavior changed in this
batch. Authenticated/blob URLs, revocation, printing and cross-origin behavior
need scoped verification before any image-optimization migration. Do not silence
these warnings globally or introduce an unauthenticated optimizer as a shortcut.

BUG-FUNC-0004 remains OPEN for review/critical regression/closure. BUG-DATA-0001,
model qualification, linked host/domain/security, physical-device and enterprise/
release gates remain OPEN. The 11 broad E0–E10 packages are not 11 small fixes.
No production readiness percentage or deployment date follows from this batch.

Main remains `f7af512f884ca5fe8ac3ddb287c852492592e709`, untouched. Staging empty;
QA/EVIDENCE untracked/local; `.build-check` ignored. Docker unchanged: 28 containers,
41 volumes, only the original two PENTA services running. No commit, push, Azure,
database, model download/activation/training, resource restart or new testing plan.

Next: Medium for bounded image/visual work; High for private-model qualification,
paired PENTA handoff and consequential host/domain/security integration. No actual
model switch is implied. Do not rerun an unchanged failed model or activate it.
