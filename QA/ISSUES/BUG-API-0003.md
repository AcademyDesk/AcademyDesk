# BUG-API-0003 — Token refresh retry can reuse the expired Authorization header

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major session reliability |
| Priority | P1 |
| Category | API |
| Module | SHARED |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | All portals using apiHeaders(true) |
| API | POST /api/auth/refresh and original write |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | AUTH-002 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/lib/api.ts:62 |
| Class/function | academyApi request / refresh |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Expire access token, keep valid refresh token. Submit a write passing headers: apiHeaders(true). Capture original, refresh and retried request headers with values redacted. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Expire access token, keep valid refresh token. Submit a write passing headers: apiHeaders(true). Capture original, refresh and retried request headers with values redacted.

## Expected

Retry uses the newly issued access token once; form submission completes or preserves input with a clear error.

## Actual / evidence

request(newToken) builds Authorization then spreads init.headers last. Existing Authorization in init.headers overwrites refreshed token.

Source snapshot:

```text
61:     ...init,
62:     headers: { ...apiHeaders(), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...init.headers },
63:   });
64:   const response = await request();
```

## Suspected root cause

Header merge order preserves stale caller token.

## Business impact and blast radius

Most JSON form writes after token expiry, all workspace session variants.

## Related / required regression

AUTH-002: Mocked fetch with expired→refresh→retry; assert new-token identity without recording token values.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
