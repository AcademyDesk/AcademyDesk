# BUG-UI-0003 — Expired permission grants are counted as active

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Controlled TSX/Chromium RUNTIME-REPRODUCED; C11 partial repair PASS |
| Final verification | C11 controlled checks PASS; original real-role/host/critical/device closure pending |
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


## C11 status/navigation batch — 7 November 2026

The register now counts only unrevoked permanent grants or temporary grants whose valid expiry is strictly after the display clock. Rows distinguish Active, Expired, Revoked and invalid expiry; existing revoke actions remain available for unrevoked rows. The read snapshot captures its client timestamp, and cancellable browser timers update at the next expiry without refetching or mutating grants. Native fixed-clock cases before/at expiry and existing past-expiry controls run in four timezones. This does not change or qualify server authorization, clock skew, grant creation or revocation.

Implementation `88a49a989d8957c5d299acecae2aaaf710eb5986` on
`codex/cloud-c11-status-navigation-20261107`. Combined batch **28 targeted tests
PASS**, **49 native Chromium groups PASS**, zero runtime/console/interceptor
errors; 24 intercepted synthetic status PATCH requests and no live writes.
Unchanged-source baseline: five PASS/23FAIL in the initial controlled tests and
ten PASS/39FAIL in49native groups. Full final frontend **2,008PASS / three
missing-genuine-SQL-receipt FAIL / zero skips**, build/types/lint PASS (zero lint
errors/62warnings). Counts are shared across the batch, not added per issue.

Remain **OPEN** for all original issue-specific module/critical/real-role/device
and release requirements. See [C11 source and acceptance handoff](../HANDOFF/C11_STATUS_NAVIGATION_2026-11-07.md).
