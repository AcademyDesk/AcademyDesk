# BUG-DATA-0053 — Finance Policy save silently resets a saved payslip theme

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
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
| Evidence classification | Static trace; runtime reproduction pending |
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
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

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
