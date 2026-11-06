# BUG-DATA-0050 — Teacher compensation resave converts stored zero rates to null

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | CONTROLLED-TSX-REPRODUCED / partial repair; current controlled Chromium roundtrip PASS |
| Final verification | Controlled TSX and Chromium checks PASS; live HTTP/SQL, device and critical acceptance pending |
| Severity | Major compensation round-trip integrity |
| Priority | P1 |
| Category | DATA |
| Module | TEACHER |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /teacher-payments |
| API | GET/PUT /api/academies/{academyId}/teachers/{teacherId}/compensation |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | Controlled Chromium 320/1440 CSS-pixel light/dark checks PASS; physical devices NOT RUN |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | TEACHER-COMPENSATION-ZERO-001 |
| Evidence classification | Historical static/TSX reproduction; current real Chromium with synthetic APIs PASS; live persistence pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/teacher-payments/page.tsx:29 |
| Class/function | loadCompensation / set / save |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Historical source excerpt below; current bounded C7 report linked below |
| Screenshot | Historical baseline not captured; two current controlled screenshots retained locally with C7 manifest |
| Console logs | Current controlled Chromium: zero application console errors/runtime exceptions; live retest pending |
| API request | In isolated fixtures save Monthly salary 0 or Hourly standard rate 0, reload and save without editing that field. Separately use positive standard rate with optional beginner/intermediate/advanced rates 0, reload and resave. Compare freshly typed string zero and untouched numeric zero. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Development-branch repair; see latest dated checkpoint/report |
| Retest result | C7: 44 native Chromium groups PASS; existing 51 value-preservation and 47 editor-isolation checks PASS |
| Regression result | C7 relevant checks PASS; no full-suite rerun. Historical frontend counts below remain source-bound |
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

## Cloud controlled browser acceptance — 2026-11-06

Implementation `cab563444ec9a8effe74e339725abbe13f930d2c` adds only a native
Chromium QA runner. The existing compensation page and shared record editor
needed no changes. On the C6 exact-lock static export, all **44 browser groups
PASS**: ten zero/null/positive/negative/required scenarios at 320 and 1440 CSS
pixels in light and dark themes, plus four pending/reversed/failed teacher-read
and late-save response controls. Valid cases reload and resave; exact outgoing
numeric/null JSON is checked. There are 49 intercepted synthetic PUT requests
and no live financial requests. Invalid required/negative inputs send no PUT.

All existing 51 value-preservation checks (including the 37 compensation checks)
and 47 record-editor-isolation checks PASS, zero failures/skips. Chromium used
its normal sandbox, with no application console errors/runtime exceptions and
verified owned-profile cleanup. Screenshots and source/export hashes remain in
excluded local artifacts. These are controlled browser results with synthetic
API state, not fresh SQL/Identity or payroll acceptance. The original static
source snapshot and historical results above are preserved.

Remain **OPEN** for real authorized GET/PUT and freshly reloaded CompensationJson,
server-side concurrent writes, downstream payroll, physical-device and required
critical-regression acceptance. A valid zero compensation rate does not permit a
new zero-net payroll payout; that rejection policy remains unchanged. See the
[C7 handoff](../HANDOFF/C7_COMPENSATION_BROWSER_2026-11-06.md) for exact source,
commands, observed results and Desktop tasks.
