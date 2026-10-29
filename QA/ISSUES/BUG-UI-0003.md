# BUG-UI-0003 — Expired permission grants are counted as active

## 25 October 2026 bounded repair — issue remains OPEN

The historical static finding below was reproduced by seven actual-source clock
tests (0/7 baseline), repaired in the Access Review page and retested7/7. Existing
16grant/revoke feedback cases retained PASS. Fresh frontend build/typecheck/lint/
627file export and four fixed-clock synthetic-read browser journeys PASS at390/
1440light-dark; screenshots inspected. Independent source/export/browser/window
qualifier PASS, exact original PENTA services/inventory restored.

UI `11505c82b2d4463e86d925bf4069e17e`; browser
`66af30d7073c40a3928c2e0b5319e45c`; window
`0e2dade20e8c40979df785641e4945db`. Expired/exact-boundary grants no longer
count active; invalid/missing dates fail inactive without crashing; permanent and
revoked precedence, IST time, long-open/focus/visible recovery and cleanup verified.
See `QA/REPORTS/ACCESS_GRANT_EXPIRY_DISPLAY_2026-10-25.md`.

This packet uses synthetic reads/token/time and is presentation-only. Earlier
native server stored-expiry evidence remains tied to its own source/image.
Device clock skew, actual devices and broader release regressions remain OPEN;
no new SQL/server-authorization acceptance or closure is claimed. Original static
metadata below is retained as historical discovery, not the current retest result.

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Moderate misleading access status |
| Priority | P2 |
| Category | UI |
| Module | AUTH |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /access-review |
| API | GET access-grants |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | AUTH-GRANT-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/access-review/page.tsx:215 |
| Class/function | AccessReview grant register |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Show synthetic permanent, future temporary, expired unrevoked and revoked grants. Compare displayed active count/status with actual access at the expiry boundary. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Show synthetic permanent, future temporary, expired unrevoked and revoked grants. Compare displayed active count/status with actual access at the expiry boundary.

## Expected

Active count and status distinguish effective, expired and revoked grants using the same time rule as authorization.

## Actual / evidence

UI checks only revokedAtUtc for active count and action state. Server permission filter also checks IsPermanent or ExpiresAtUtc > UTC now. Expired rows remain counted active.

Source snapshot:

```text
214:               <span>
215:                 {grants.filter((grant) => !grant.revokedAtUtc).length} active
216:               </span>
217:             </header>
```

## Suspected root cause

Display eligibility omits expiry condition.

## Business impact and blast radius

Access-review accuracy; server does exclude expired grants, so this is not evidence of expiry bypass.

## Related / required regression

AUTH-GRANT-001: Fixed-clock browser cases before/at/after expiry, permanent/revoked rows, timezone display and long-open page; compare independently expected effective count.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
