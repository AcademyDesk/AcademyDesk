# BUG-DATA-0050 — Teacher compensation resave converts stored zero rates to null

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONTROLLED-TSX-REPRODUCED / partial local repair |
| Final verification | Partial controlled checks PASS; SQL/browser/critical acceptance pending |
| Severity | Major compensation round-trip integrity |
| Priority | P1 |
| Category | DATA |
| Module | TEACHER |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher-payments |
| API | GET/PUT /api/academies/{academyId}/teachers/{teacherId}/compensation |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | TEACHER-COMPENSATION-ZERO-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/teacher-payments/page.tsx:29 |
| Class/function | loadCompensation / set / save |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In isolated fixtures save Monthly salary 0 or Hourly standard rate 0, reload and save without editing that field. Separately use positive standard rate with optional beginner/intermediate/advanced rates 0, reload and resave. Compare freshly typed string zero and untouched numeric zero. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Development-branch repair; see latest dated checkpoint/report |
| Retest result | Bounded controlled checks PASS; see latest dated checkpoint |
| Regression result | Current frontend1,095/1,098 PASS; three historical SQL-evidence failures |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In isolated fixtures save Monthly salary 0 or Hourly standard rate 0, reload and save without editing that field. Separately use positive standard rate with optional beginner/intermediate/advanced rates 0, reload and resave. Compare freshly typed string zero and untouched numeric zero.

## Expected

A valid persisted zero round-trips as zero; omitted optional rates remain null. An unchanged resave neither fails required-rate validation nor silently clears optional zero rates.

## Actual / evidence

GET returns numeric rates and loadCompensation keeps those numbers. save tests truthiness before Number conversion, so numeric 0 becomes null while input string zero becomes 0. Save API allows zero but rejects null primary rates; optional null rates are accepted and replace existing zero in CompensationJson. Runtime NOT RUN.

Source snapshot:

```text
28:   const set = (key: keyof Compensation, value: string) => setForm((current) => ({ ...current, [key]: value }));
29:   async function save(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!academy || !teacherId) return; setSaving(true); const hourly = form.model === "Hourly"; const payload = { model: form.model, monthlySalary: !hourly && form.monthlySalary ? Number(form.monthlySalary) : null, standardHourlyRate: hourly && form.standardHourlyRate ? Number(form.standardHourlyRate) : null, beginnerHourlyRate: hourly && form.beginnerHourlyRate ? Number(form.beginnerHourlyRate) : null, intermediateHourlyRate: hourly && form.intermediateHourlyRate ? Number(form.intermediateHourlyRate) : null, advancedHourlyRate: hourly && form.advancedHourlyRate ? Number(form.advancedHourlyRate) : null, effectiveFrom: form.effectiveFrom || null }; const response = await academyApi(`/api/academies/${academy.id}/teachers/${teacherId}/compensation`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify(payload) }); const result = await response.json().catch(() => null); setSaving(false); if (!response.ok) return setMessage(result?.message ?? "Payment details could not be saved."); setForm({ ...result, effectiveFrom: day(result.effectiveFrom) }); setMessage("Teacher payment details saved."); }
30:   return (
31:     <main className="enterprise-settings teacher-standard teacher-payments-standard">
```

## Suspected root cause

Payload construction conflates numeric zero with empty optional input after response hydration.

## Business impact and blast radius

Unchanged zero primary rates cause rejected saves; zero optional hourly bands can be silently cleared. Downstream payroll fallback behavior is not established by this finding.

## Related / required regression

TEACHER-COMPENSATION-ZERO-001: Browser plus full HTTP/SQL first-save/reload/resave matrix for all five rates: blank/null/typed zero/loaded zero/positive/negative. Compare exact outgoing JSON, response and freshly reloaded CompensationJson; preserve zero versus absence. Reuse TEACHER-PAY-001 and TEACHER-FORM-002 for model transitions and optional cases.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Cloud controlled repair — 2026-10-28

Zero/null serialization failures reproduced in actual-TSX request tests. Editable
rates now normalize to strings and explicitly distinguish blank from zero; all
five numeric rates preserve zero across hydration and repeated saves. Existing
Monthly/Hourly applicability and optional-null behavior remain. The shared
loaded-record guard also prevents pending/old teacher loads and saves from
replacing a new teacher's editor, with retry/duplicate/transport recovery.

All 37 compensation checks PASS; no payroll arithmetic, API validation or actual
financial action changed. Full frontend1,095/1,098 PASS with three unchanged
missing historical SQL-evidence failures. TypeScript/lint/build PASS. Remain OPEN
for actual HTTP/SQL/browser/device, concurrent server writes, downstream payroll
and critical regression. See [value-preservation report](../REPORTS/CLOUD_VALUE_PRESERVATION_2026-10-28.md).
