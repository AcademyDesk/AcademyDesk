# BUG-FUNC-0004 — Frontend quality check fails on current baseline

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | RUNTIME-REPRODUCED |
| Final verification | NOT RUN |
| Severity | Major release-quality gap |
| Priority | P1 |
| Category | FUNC |
| Module | SHARED |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | Repository frontend |
| API | N/A |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | QUALITY-LINT-001 |
| Evidence classification | Observed configured lint failure |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | 2 lint executions failed |
| Source | apps/web/package.json:9 |
| Class/function | npm run lint |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | QA/EVIDENCE/logs/eslint.json |
| Screenshot | Not captured on pinned baseline |
| Console logs | EVIDENCE/logs/eslint.json |
| API request | From apps/web run npm run lint; for machine output use -- --format json --output-file ../../QA/EVIDENCE/logs/eslint.json. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

From apps/web run npm run lint; for machine output use -- --format json --output-file ../../QA/EVIDENCE/logs/eslint.json.

## Expected

Configured quality gate completes without errors.

## Actual / evidence

Observed exit 1: 46 errors and 81 warnings. Includes hook effects/purity, explicit any, ARIA and module-variable rules; full file/rule locations in evidence.

Source snapshot:

```text
8:     "start": "next start",
9:     "lint": "eslint"
10:   },
11:   "dependencies": {
```

## Suspected root cause

Existing source does not satisfy configured ESLint rules; individual violations require contextual triage.

## Business impact and blast radius

Frontend CI quality gate; lint is not a count of behavioral test failures.

## Related / required regression

2026-10-01 [Batch preservation checkpoint](../REPORTS/PHASE_2B_BATCH_PRESERVATION_REPAIR.md): targeted Batches/Teachers/helper lint still FAILS with two errors/four warnings. Pre-edit hashes matched HEAD; before/after rule/reason/loader diagnostics unchanged, helper clean. No lint suppression or quality-gate closure. Wider configured lint remains pending.

QUALITY-LINT-001: Rerun identical lint command after targeted fixes; no rule disabling to fabricate a pass.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
