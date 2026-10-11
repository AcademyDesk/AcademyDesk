# BUG-DATA-0053 — Finance Policy save silently resets a saved payslip theme

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | Ordinary/stale guards and initial editor loading bounded-repaired; SQL audit rollback + linked browser verified; broader acceptance OPEN |
| Final verification | SQL24 controls +5 races/8 linked journeys PASS;11 Governance/6 Policy handlers/36+16 synthetic/type/export PASS;67 API units retained; physical/critical/wider OPEN |
| Severity | Major finance-settings integrity |
| Priority | P1 |
| Category | DATA |
| Module | FINANCE |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /finance-policy |
| API | PUT /api/academies/{academyId}/finance-governance/settings |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | FINANCE-POLICY-PRESERVE-001 |
| Evidence classification | Handler reproduced; real Identity/HTTP/SQL retest and separate synthetic DOM; stale-write failure reproduced |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/finance-policy/page.tsx:14 |
| Class/function | save / FinanceGovernance.Save |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In an isolated academy set PayslipTemplateKey to Compact or Professional through the document-settings editor or API. Open Finance Policy, change only tax label, save, then read the settings row in a fresh SQL context and compare both themes. Repeat with Standard control, failed PUT, and a concurrent document edit. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Scoped ordinary-save repair publication, 2026-10-11; see feature history, not issue closure |
| Retest result | Stale overwrite409;24 native controls +5 races/8 linked journeys/audit rollback PASS; physical/critical/wider gates OPEN / RETEST |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Current audit/linked verification — 2026-10-11

[Report](../REPORTS/PHASE_2B_FINANCE_SETTINGS_AUDIT_LINKED.md): run2c9f9d27a0fa4cb89ab4a3e1a4bfb939,24 sequential controls/five strict200/409 races PASS; same-action insertion/update audit500 rollback leaves settings/audits/version unchanged, recovery200 one success audit. Eight linked production-export/real Identity HTTP SQL journeys,32 PUTs=24HTTP200/8HTTP409/no429, native draft retained/explicit refresh/recovery/version/all11 fields/audit actors/foreign sentinel verified. Initial Governance edit prevented hydration; readiness guard and four delayed real-GET browser controls fix it.11 final handlers vs frozen9/11,6 Policy/36+16 synthetic/types/export83 fresh PASS;67 API units retained, Governance3 inherited warnings.88/7 migrations/exact owned DB/login/container cleanup; both failed runs retained in report, exact owned containers removed only. Mobile document preview still clips internally; overlong tax-label500 FINANCE-SETTINGS-LENGTH-001 needs separate400/no-write validation. OPEN/RETEST for physical Android/iOS, full critical, wider permissions/visual/release; no enterprise completion/main/Azure/Mini change.

## Prior stale-guard verification — 2026-10-11

[Verification report](../REPORTS/PHASE_2B_FINANCE_SETTINGS_STALE_GUARD.md): Docker normal stop/start recovered, no reset/prune/older-container deletion. Real Identity/HTTP/SQL runb418248d40d2479d843a80c2414e6699:21 controls plus5/5 simultaneous pairs PASS (first absent settings row); exactly200/409, one row/one audit/unmixed winner fields/fresh SQL+GET equality/no429;88/7 migrations/exact run-owned cleanup. Current-version resets/themes and rejected authority/no-write preserved. Retained67 unit/6 handler/36+16 synthetic/types/export83 PASS, Governance3 inherited lint warnings. Earlier blocked/unfixed checkpoints below historical. Scoped feature publication only, no main/Azure/Mini. Issue OPEN for same-action audit-fault, linked browser/physical devices/full critical; no full release acceptance.

## Earlier stale-guard checkpoint (historical)

[Local report](../REPORTS/PHASE_2B_FINANCE_SETTINGS_STALE_GUARD.md) supersedes earlier “unfixed” wording for local source only. GET/PUT versions, stable-academy SQL lock/atomic success audit and both-editor draft recovery implemented.67 targeted unit tests,6 retained handler,36 synthetic exported-browser (including4 open-draft/approval-list refresh),16 retained theme cases,types/harness build/export83 PASS. Governance lint0 errors/3 inherited warnings; Policy0/0. **Real SQL21 sequential controls +5 guarded pairs BLOCKED/NOT RUN**, Docker socket inaccessible. Restart approval requested; no reset/prune/data deletion/run-owned SQL resource. Older clients missing version receive428; stale/manipulated version409. Original explicit NULL/omitted reset remains with current version; both editors/API must release together after gates. No commit/push/main/Azure/Mini change. OPEN/RETEST: native concurrency, same-action SQL audit-fault, linked browser/device/full critical pending. Historical failure and previous native ordinary-save receipts below retained, not current guard passes.

## Exact reproduction (historical)

In an isolated academy set PayslipTemplateKey to Compact or Professional through the document-settings editor or API. Open Finance Policy, change only tax label, save, then read the settings row in a fresh SQL context and compare both themes. Repeat with Standard control, failed PUT, and a concurrent document edit.

## Expected

Editing tax or invoice policy preserves the stored payslip theme unless the user explicitly changes that theme. Rejected writes leave every setting unchanged.

## Actual / evidence

Finance Policy renders no input named payslipTemplateKey but constructs the replacement PUT from form.get("payslipTemplateKey"), which is null. FinanceGovernance.Save accepts null and assigns Standard. A successful ordinary policy save therefore overwrites Compact/Professional. Static source trace only; runtime NOT RUN.

Source snapshot:

```text
13:   useEffect(() => { void (async () => { try { const academies: Academy[] = await (await academyApi("/api/academies")).json(); if (!academies[0]) throw Error(); setAcademy(academies[0]); setSettings(await (await academyApi(`/api/academies/${academies[0].id}/finance-governance/settings`)).json()); setMessage(""); } catch { setMessage("Finance policy could not be loaded."); } })(); }, []);
14:   async function save(event: React.FormEvent<HTMLFormElement>) { event.preventDefault(); if (!academy) return; const form = new FormData(event.currentTarget); const response = await academyApi(`/api/academies/${academy.id}/finance-governance/settings`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ taxRegistrationNumber: form.get("taxRegistrationNumber"), taxLabel: form.get("taxLabel"), taxRatePercent: Number(form.get("taxRatePercent")), defaultPaymentTermsDays: Number(form.get("defaultPaymentTermsDays")), taxInclusivePricing: form.get("taxInclusivePricing") === "on", invoiceLogoUrl: form.get("invoiceLogoUrl"), invoiceAuthorityName: form.get("invoiceAuthorityName"), invoiceAuthorityTitle: form.get("invoiceAuthorityTitle"), invoiceSignatureUrl: form.get("invoiceSignatureUrl"), invoiceTemplateKey: form.get("invoiceTemplateKey"), payslipTemplateKey: form.get("payslipTemplateKey") }) }); if (!response.ok) return setMessage("Finance policy could not be saved."); setSettings(await response.json()); setMessage("Finance policy saved."); }
15:   return <main className="enterprise-settings finance-module"><header className="enterprise-page-header"><p>Finance / policy</p><h2>Tax and payment policy</h2><span>Set tax rules and branded invoice details used when printing or saving as PDF.</span></header>{message && <p className="enterprise-settings-empty mt-5">{message}</p>}{settings && <form onSubmit={save} className="surface-panel mt-5 max-w-3xl rounded-xl p-5"><h3>Invoice defaults</h3><div className="mt-4 grid gap-3"><input name="taxRegistrationNumber" defaultValue={settings.taxRegistrationNumber} className="field" placeholder="GST registration number"/><input name="taxLabel" defaultValue={settings.taxLabel} className="field" placeholder="Tax label"/><div className="grid gap-3 md:grid-cols-2"><label className="field-label">Tax rate (%)<input name="taxRatePercent" type="number" step="0.01" defaultValue={settings.taxRatePercent} className="field"/></label><label className="field-label">Default payment terms (days)<input name="defaultPaymentTermsDays" type="number" defaultValue={settings.defaultPaymentTermsDays} className="field"/></label></div><label className="consent-check"><input name="taxInclusivePricing" type="checkbox" defaultChecked={settings.taxInclusivePricing}/> Tax-inclusive pricing</label></div><h3 className="mt-7">Invoice branding and authority</h3><div className="mt-4 grid gap-3 md:grid-cols-2"><label className="field-label md:col-span-2">Academy logo URL<input name="invoiceLogoUrl" type="url" defaultValue={settings.invoiceLogoUrl ?? ""} className="field" placeholder="https://…/academy-logo.png"/></label><label className="field-label">Authorised signatory name<input name="invoiceAuthorityName" defaultValue={settings.invoiceAuthorityName ?? ""} className="field" placeholder="e.g. Kavya Sharma"/></label><label className="field-label">Designation<input name="invoiceAuthorityTitle" defaultValue={settings.invoiceAuthorityTitle ?? ""} className="field" placeholder="e.g. Finance Manager"/></label><label className="field-label">Signature image URL<input name="invoiceSignatureUrl" type="url" defaultValue={settings.invoiceSignatureUrl ?? ""} className="field" placeholder="https://…/signature.png"/></label><label className="field-label">Invoice theme<select name="invoiceTemplateKey" defaultValue={settings.invoiceTemplateKey || "Classic"} className="field"><option>Classic</option><option>Modern</option><option>Minimal</option><option>Formal</option></select></label></div><button className="enterprise-action-button mt-5">Save invoice settings</button></form>}</main>;
16: }
```

## Suspected root cause

A full settings replacement includes a FormData lookup for a field absent from this form; the server treats omission as a request to reset.

## Business impact and blast radius

Academies using a nondefault payslip theme; saving unrelated tax, payment terms or invoice branding can change the payroll document setting.

## Related / required regression

FINANCE-POLICY-PRESERVE-001: Browser + real HTTP/SQL baseline Compact/Professional, policy edit, fresh-row equality for untouched payslip theme and changed intended field; null/omitted direct API contract, rejected no-write, second editor and concurrency controls.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.

## Local checkpoint — 2026-10-11

[Report](../REPORTS/PHASE_2B_FINANCE_POLICY_THEME_PRESERVATION.md): frozen0/6 handler PASS, loaded-theme preservation6/6 PASS, types/target lint PASS. Original API NULL/omitted reset semantics untouched. Issue OPEN; real HTTP/SQL/browser proof, second-editor/stale concurrency (not fixed), physical-device/full critical still pending. No commit/push/Azure in this checkpoint.

## Verification successor — 2026-10-11

Earlier static/native-NOT-RUN fields and source snapshot above are historical. Final6 handler/16 synthetic exported-browser checks PASS; themes preserve exact payload, rejected draft retained, mobile/desktop light/dark no unexpected console/overflow. Native run59358d6cf5574380a4510f976c42bd83:15 ordinary/reset/rejection/authority controls plus3 stale-reproduction PUTs, fresh SQL/GET equality, no429,88/7 migrations/exact owned cleanup. NULL/omitted reset semantics retained. **Stale Compact still overwrites newer Professional with200**; no CAS/version protection implemented. OPEN/RETEST, not18 guarded concurrent successes. Separate browser/SQL, not linked/device/full-critical acceptance. Post-run Docker inventory unavailable; runner cleanup receipt retained. Next Sol High both-editor stale-settings guard/strict SQL races; retain ordinary-save repair and wider feedback/Manual visual queue. See report for evidence/scoped publication; no main/Mini/Azure.
