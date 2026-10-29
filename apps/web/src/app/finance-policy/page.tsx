"use client";

import { useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import visual from "@/components/design-system/finance-controls.module.css";
import { StandardFormSelect } from "@/components/design-system/form-fields";

type Academy = { id: string };
type Settings = { taxRegistrationNumber: string; taxLabel: string; taxRatePercent: number; defaultPaymentTermsDays: number; taxInclusivePricing: boolean; invoiceLogoUrl?: string; invoiceAuthorityName?: string; invoiceAuthorityTitle?: string; invoiceSignatureUrl?: string; invoiceTemplateKey: string; payslipTemplateKey: string; settingsVersion?: string };

export default function FinancePolicy() {
  const [academy, setAcademy] = useState<Academy>();
  const [settings, setSettings] = useState<Settings>();
  const [message, setMessage] = useState("Loading finance policy…");
  const savePending = useRef(false);
  const [saving, setSaving] = useState(false);
  useEffect(() => { void (async () => { try { const academyResponse = await academyApi("/api/academies", { cache: "no-store" }); if (!academyResponse.ok) throw Error(); const academies: Academy[] = await academyResponse.json(); if (!academies[0]) throw Error(); setAcademy(academies[0]); const settingsResponse = await academyApi(`/api/academies/${academies[0].id}/finance-governance/settings`, { cache: "no-store" }); if (!settingsResponse.ok) throw Error(); const loaded: Settings = await settingsResponse.json(); if (!loaded || typeof loaded !== "object" || typeof loaded.taxLabel !== "string" || !Number.isFinite(loaded.taxRatePercent)) throw Error(); setSettings(loaded); setMessage(""); } catch { setMessage("Finance policy could not be loaded."); } })(); }, []);
  async function save(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy || !settings || savePending.current) return;
    if (!settings.settingsVersion) return setMessage("Refresh settings before saving. Your draft has not been saved.");
    const form = new FormData(event.currentTarget); savePending.current = true; setSaving(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/finance-governance/settings`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ taxRegistrationNumber: form.get("taxRegistrationNumber"), taxLabel: form.get("taxLabel"), taxRatePercent: Number(form.get("taxRatePercent")), defaultPaymentTermsDays: Number(form.get("defaultPaymentTermsDays")), taxInclusivePricing: form.get("taxInclusivePricing") === "on", invoiceLogoUrl: form.get("invoiceLogoUrl"), invoiceAuthorityName: form.get("invoiceAuthorityName"), invoiceAuthorityTitle: form.get("invoiceAuthorityTitle"), invoiceSignatureUrl: form.get("invoiceSignatureUrl"), invoiceTemplateKey: form.get("invoiceTemplateKey"), payslipTemplateKey: settings.payslipTemplateKey , settingsVersion: settings.settingsVersion }) });
      if (response.status === 409) return setMessage("Settings changed in another window. Your draft has not been saved. Refresh this page and review the latest settings before saving again.");
      if (response.status === 428) return setMessage("Refresh settings before saving. Your draft has not been saved.");
      if (response.status === 400) {
        const rejected: unknown = await response.json().catch(() => null);
        const reason = rejected && typeof rejected === "object" && "message" in rejected ? rejected.message : null;
        if (reason === "Tax label must not exceed 30 characters. Your draft has not been saved." || reason === "Tax registration number must not exceed 50 characters. Your draft has not been saved.") return setMessage(reason);
      }
      if (!response.ok) return setMessage("Finance policy could not be saved.");
      setSettings(await response.json()); setMessage("Finance policy saved.");
    } catch { setMessage("The save could not be confirmed. Check the latest settings before retrying; your draft is retained."); }
    finally { savePending.current = false; setSaving(false); }
  }
  return <main className={`enterprise-settings finance-module ${visual.page}`}><header className="enterprise-page-header"><p>Finance / policy</p><h1>Tax and payment policy</h1><span>Set tax rules and branded invoice details used when printing or saving as PDF.</span></header>{message && <p role="status" className="enterprise-settings-empty mt-5">{message}</p>}{settings && <form onSubmit={save} className="surface-panel mt-5 max-w-3xl rounded-xl p-5"><fieldset disabled={saving}><h3>Invoice defaults</h3><div className="mt-4 grid gap-3"><label className={visual.policyLabel}>Tax registration number (optional)<input name="taxRegistrationNumber" defaultValue={settings.taxRegistrationNumber} className="field" placeholder="GST registration number"/></label><label className={visual.policyLabel}>Tax label<input name="taxLabel" defaultValue={settings.taxLabel} className="field" placeholder="Tax label"/></label><div className="grid gap-3 md:grid-cols-2"><label className="field-label">Tax rate (%)<input name="taxRatePercent" type="number" step="0.01" defaultValue={settings.taxRatePercent} className="field"/></label><label className="field-label">Default payment terms (days)<input name="defaultPaymentTermsDays" type="number" defaultValue={settings.defaultPaymentTermsDays} className="field"/></label></div><label className="consent-check"><input name="taxInclusivePricing" type="checkbox" defaultChecked={settings.taxInclusivePricing}/> Tax-inclusive pricing</label></div><h3 className="mt-7">Invoice branding and authority</h3><div className="mt-4 grid gap-3 md:grid-cols-2"><label className="field-label md:col-span-2">Academy logo URL<input name="invoiceLogoUrl" type="url" defaultValue={settings.invoiceLogoUrl ?? ""} className="field" placeholder="https://…/academy-logo.png"/></label><label className="field-label">Authorised signatory name<input name="invoiceAuthorityName" defaultValue={settings.invoiceAuthorityName ?? ""} className="field" placeholder="e.g. Kavya Sharma"/></label><label className="field-label">Designation<input name="invoiceAuthorityTitle" defaultValue={settings.invoiceAuthorityTitle ?? ""} className="field" placeholder="e.g. Finance Manager"/></label><label className="field-label">Signature image URL<input name="invoiceSignatureUrl" type="url" defaultValue={settings.invoiceSignatureUrl ?? ""} className="field" placeholder="https://…/signature.png"/></label><StandardFormSelect name="invoiceTemplateKey" label="Invoice theme" defaultValue={settings.invoiceTemplateKey || "Classic"} options={["Classic", "Modern", "Minimal", "Formal"]} disabled={saving}/></div><button disabled={saving} className="enterprise-action-button mt-5">{saving ? "Saving…" : "Save invoice settings"}</button></fieldset></form>}</main>;
}
