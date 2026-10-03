# BUG-FUNC-0010 — Maintenance mode setting does not restrict platform access

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major ineffective operational control |
| Priority | P1 |
| Category | FUNC |
| Module | PLATFORM |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /platform/control?tab=Settings |
| API | PUT /api/platform/settings; ordinary application requests |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PLATFORM-MAINTENANCE-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/platform/control/page.tsx:835 |
| Class/function | SaveSettings / access pipeline |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures as platform owner save maintenanceMode=true. As an ordinary authorized academy user request a previously permitted read and mutation. Compare false control and owner recovery access. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixtures as platform owner save maintenanceMode=true. As an ordinary authorized academy user request a previously permitted read and mutation. Compare false control and owner recovery access.

## Expected

Maintenance behavior matches the UI promise to restrict access, with a defined owner recovery path and clear user message.

## Actual / evidence

Flag is saved and returned by Health, but source search finds no maintenance-mode gate in application access middleware/controllers or frontend navigation. Existing permissions continue unchanged. Runtime NOT RUN.

Source snapshot:

```text
834:                   <label><span>Data retention days</span><input name="retentionDays" type="number" min="30" defaultValue={settings.dataRetentionDays} required /></label>
835:                   <label className="platform-maintenance-toggle"><input name="maintenanceMode" type="checkbox" defaultChecked={settings.maintenanceMode} /><span><b>Maintenance mode</b><small>Temporarily restrict platform access</small></span></label>
836:                   <label className="platform-settings-wide"><span>Status message</span><textarea name="statusMessage" defaultValue={settings.statusMessage || ""} rows={3} placeholder="Optional maintenance or service status message" /></label>
837:                   <button disabled={busy} className="enterprise-action-button platform-settings-action">{busy ? "Saving…" : "Save platform settings"}</button>
```

## Suspected root cause

Configuration and reporting were implemented without the promised enforcement path.

## Business impact and blast radius

Operations relying on the maintenance toggle to prevent use during maintenance; not an authentication bypass.

## Related / required regression

PLATFORM-MAINTENANCE-001: Real HTTP and browser checks for enabled/disabled maintenance, permitted recovery actors and approved read/write policy, fresh settings reads and existing sessions; preserve owner recovery and normal authorization.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
