"use client";

import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string };
type Teacher = { id: string; firstName: string; lastName: string; isActive: boolean };
type Compensation = { model?: "Monthly" | "Hourly"; monthlySalary?: number; standardHourlyRate?: number; beginnerHourlyRate?: number; intermediateHourlyRate?: number; advancedHourlyRate?: number; effectiveFrom?: string };
const day = (value?: string) => value?.slice(0, 10) ?? "";

export default function TeacherPaymentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [teacherId, setTeacherId] = useState("");
  const [form, setForm] = useState<Compensation>({ model: "Monthly" });
  const [message, setMessage] = useState("Loading teacher payment details…");
  const [saving, setSaving] = useState(false);
  async function loadCompensation(id: string, academyId = academy?.id) {
    if (!academyId || !id) return;
    const response = await academyApi(`/api/academies/${academyId}/teachers/${id}/compensation`);
    if (!response.ok) throw new Error();
    const value = await response.json() as Compensation;
    setForm({ ...value, model: value.model || "Monthly", effectiveFrom: day(value.effectiveFrom) });
  }
  useEffect(() => { void (async () => { try { const academies = await academyApi("/api/academies"); const current = (await academies.json())[0] as Academy | undefined; if (!current) return setMessage("Create an academy before managing teacher payments."); setAcademy(current); const response = await academyApi(`/api/academies/${current.id}/teachers`); if (!response.ok) throw new Error(); const rows = (await response.json()) as Teacher[]; setTeachers(rows); const selected = rows.find((teacher) => teacher.isActive) ?? rows[0]; if (selected) { setTeacherId(selected.id); await loadCompensation(selected.id, current.id); } setMessage(""); } catch { setMessage("Teacher payment details could not be loaded."); } })(); }, []);
  async function select(id: string) { setTeacherId(id); try { await loadCompensation(id); setMessage(""); } catch { setMessage("Payment details could not be loaded."); } }
  const set = (key: keyof Compensation, value: string) => setForm((current) => ({ ...current, [key]: value }));
  async function save(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!academy || !teacherId) return; setSaving(true); const hourly = form.model === "Hourly"; const payload = { model: form.model, monthlySalary: !hourly && form.monthlySalary ? Number(form.monthlySalary) : null, standardHourlyRate: hourly && form.standardHourlyRate ? Number(form.standardHourlyRate) : null, beginnerHourlyRate: hourly && form.beginnerHourlyRate ? Number(form.beginnerHourlyRate) : null, intermediateHourlyRate: hourly && form.intermediateHourlyRate ? Number(form.intermediateHourlyRate) : null, advancedHourlyRate: hourly && form.advancedHourlyRate ? Number(form.advancedHourlyRate) : null, effectiveFrom: form.effectiveFrom || null }; const response = await academyApi(`/api/academies/${academy.id}/teachers/${teacherId}/compensation`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify(payload) }); const result = await response.json().catch(() => null); setSaving(false); if (!response.ok) return setMessage(result?.message ?? "Payment details could not be saved."); setForm({ ...result, effectiveFrom: day(result.effectiveFrom) }); setMessage("Teacher payment details saved."); }
  return (
    <main className="enterprise-settings teacher-standard teacher-payments-standard">
      <header className="teacher-payments-heading">
        <div className="teacher-payments-title"><span className="teacher-payments-title-icon" aria-hidden="true">₹</span><div><p>Teachers</p><h1>Payment Details</h1></div></div>
      </header>
      {message && <p className="enterprise-page-state teacher-payments-message">{message}</p>}
      <section className="teacher-payments-layout">
        <section className="teacher-payments-teacher-panel">
          <header className="teacher-payments-panel-header"><div><p>Teacher</p><h2>Select teacher</h2></div></header>
          <div className="teacher-payments-selector">
            <StandardSelectField
              name="payment-teacher"
              value={teacherId}
              onChange={(value) => void select(value)}
              placeholder="Select teacher"
              options={teachers.map((teacher) => ({ value: teacher.id, label: `${teacher.firstName} ${teacher.lastName}${teacher.isActive ? "" : " (Inactive)"}` }))}
            />
          </div>
        </section>
        {teacherId && <form onSubmit={save} className="teacher-payments-panel">
          <header className="teacher-payments-panel-header"><div><p>Compensation</p><h2>Payment arrangement</h2></div><button disabled={saving} className="enterprise-action-button">{saving ? "Saving…" : "Save payment details"}</button></header>
          <div className="teacher-payments-fields">
            <div className="field-label"><span>Payment model</span><StandardSelectField name="payment-model" value={form.model ?? "Monthly"} onChange={(value) => set("model", value)} placeholder="Payment model" options={[{ value: "Monthly", label: "Monthly salary" }, { value: "Hourly", label: "Hourly rates" }]} /></div>
            {form.model === "Monthly" ? <label className="field-label">Monthly salary<input required type="number" min="0" step="0.01" value={form.monthlySalary ?? ""} onChange={(event) => set("monthlySalary", event.target.value)} /></label> : <><label className="field-label">Standard hourly rate<input required type="number" min="0" step="0.01" value={form.standardHourlyRate ?? ""} onChange={(event) => set("standardHourlyRate", event.target.value)} /></label><label className="field-label">Beginner hourly rate (optional)<input type="number" min="0" step="0.01" value={form.beginnerHourlyRate ?? ""} onChange={(event) => set("beginnerHourlyRate", event.target.value)} /></label><label className="field-label">Intermediate hourly rate (optional)<input type="number" min="0" step="0.01" value={form.intermediateHourlyRate ?? ""} onChange={(event) => set("intermediateHourlyRate", event.target.value)} /></label><label className="field-label">Advanced hourly rate (optional)<input type="number" min="0" step="0.01" value={form.advancedHourlyRate ?? ""} onChange={(event) => set("advancedHourlyRate", event.target.value)} /></label></>}
            <StandardDateField name="effective-from" label="Effective from" value={form.effectiveFrom ?? ""} onChange={(value) => set("effectiveFrom", value)} />
          </div>
        </form>}
      </section>
    </main>
  );
}
