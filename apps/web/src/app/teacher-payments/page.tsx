"use client";

import { FormEvent, useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
import { useRecordEditor } from "@/lib/use-record-editor";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string };
type Teacher = { id: string; firstName: string; lastName: string; isActive: boolean };
type Compensation = { model?: "Monthly" | "Hourly"; monthlySalary?: number | null; standardHourlyRate?: number | null; beginnerHourlyRate?: number | null; intermediateHourlyRate?: number | null; advancedHourlyRate?: number | null; effectiveFrom?: string | null };
const day = (value?: string) => value?.slice(0, 10) ?? "";
const prepare = (value: Compensation) => ({
  model: value.model ?? "Monthly", monthlySalary: value.monthlySalary?.toString() ?? "",
  standardHourlyRate: value.standardHourlyRate?.toString() ?? "", beginnerHourlyRate: value.beginnerHourlyRate?.toString() ?? "",
  intermediateHourlyRate: value.intermediateHourlyRate?.toString() ?? "", advancedHourlyRate: value.advancedHourlyRate?.toString() ?? "",
  effectiveFrom: day(value.effectiveFrom ?? undefined),
});
const optionalNumber = (value: string) => value === "" ? null : Number(value);

export default function TeacherPaymentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [teacherId, setTeacherId] = useState("");
  const [message, setMessage] = useState("Loading teacher payment details…");
  const editor = useRecordEditor(academy && teacherId ? `/api/academies/${academy.id}/teachers/${teacherId}/compensation` : "", prepare, "Payment details could not be loaded.");
  const form = editor.draft ?? prepare({});
  const saving = editor.saving;
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const academies = await academyApi("/api/academies");
        if (!academies.ok) throw new Error();
        const current = (await academies.json())[0] as Academy | undefined;
        if (!active) return;
        if (!current) return setMessage("Create an academy before managing teacher payments.");
        setAcademy(current);
        const response = await academyApi(`/api/academies/${current.id}/teachers`);
        if (!response.ok) throw new Error();
        const rows = (await response.json()) as Teacher[];
        if (!active) return;
        setTeachers(rows);
        const selected = rows.find((teacher) => teacher.isActive) ?? rows[0];
        if (selected) setTeacherId(selected.id);
        setMessage("");
      } catch {
        if (active) setMessage("Teacher payment details could not be loaded.");
      }
    })();
    return () => { active = false; };
  }, []);
  function select(id: string) { setTeacherId(id); setMessage(""); }
  const set = (key: keyof typeof form, value: string) => editor.update((current) => ({ ...current, [key]: value }));
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const hourly = form.model === "Hourly";
    const payload = {
      model: form.model, monthlySalary: !hourly ? optionalNumber(form.monthlySalary) : null,
      standardHourlyRate: hourly ? optionalNumber(form.standardHourlyRate) : null,
      beginnerHourlyRate: hourly ? optionalNumber(form.beginnerHourlyRate) : null,
      intermediateHourlyRate: hourly ? optionalNumber(form.intermediateHourlyRate) : null,
      advancedHourlyRate: hourly ? optionalNumber(form.advancedHourlyRate) : null,
      effectiveFrom: form.effectiveFrom || null,
    };
    await editor.save(payload, "Teacher payment details saved.", "Payment details could not be saved. Check the record before retrying.");
  }
  const notice = message || (editor.loading ? "Loading teacher payment details…" : editor.message);
  return (
    <main className="enterprise-settings teacher-standard teacher-payments-standard">
      <header className="teacher-payments-heading">
        <div className="teacher-payments-title"><span className="teacher-payments-title-icon" aria-hidden="true">₹</span><div><p>Teachers</p><h1>Payment Details</h1></div></div>
      </header>
      {notice && <p className="enterprise-page-state teacher-payments-message">{notice}</p>}
      {editor.failed && <button onClick={editor.reload} className="enterprise-action-button mt-3">Retry loading payment details</button>}
      <section className="teacher-payments-toolbar">
        <StandardSelectField
          name="payment-teacher"
          value={teacherId}
          onChange={(value) => void select(value)}
          placeholder="Select teacher"
          options={teachers.map((teacher) => ({ value: teacher.id, label: `${teacher.firstName} ${teacher.lastName}${teacher.isActive ? "" : " (Inactive)"}` }))}
        />
      </section>
      <section className="teacher-payments-layout">
        {teacherId && <form onSubmit={save} className="teacher-payments-panel">
          <header className="teacher-payments-panel-header"><div><p>Compensation</p><h2>Payment arrangement</h2></div><button disabled={saving || !editor.ready} className="enterprise-action-button">{saving ? "Saving…" : "Save payment details"}</button></header>
          <fieldset disabled={saving || !editor.ready} className="teacher-payments-fields">
            <div className="field-label"><span>Payment model</span><StandardSelectField name="payment-model" value={form.model ?? "Monthly"} onChange={(value) => set("model", value)} placeholder="Payment model" options={[{ value: "Monthly", label: "Monthly salary" }, { value: "Hourly", label: "Hourly rates" }]} /></div>
            {form.model === "Monthly" ? <label className="field-label">Monthly salary<input required type="number" min="0" step="0.01" value={form.monthlySalary ?? ""} onChange={(event) => set("monthlySalary", event.target.value)} /></label> : <><label className="field-label">Standard hourly rate<input required type="number" min="0" step="0.01" value={form.standardHourlyRate ?? ""} onChange={(event) => set("standardHourlyRate", event.target.value)} /></label><label className="field-label">Beginner hourly rate (optional)<input type="number" min="0" step="0.01" value={form.beginnerHourlyRate ?? ""} onChange={(event) => set("beginnerHourlyRate", event.target.value)} /></label><label className="field-label">Intermediate hourly rate (optional)<input type="number" min="0" step="0.01" value={form.intermediateHourlyRate ?? ""} onChange={(event) => set("intermediateHourlyRate", event.target.value)} /></label><label className="field-label">Advanced hourly rate (optional)<input type="number" min="0" step="0.01" value={form.advancedHourlyRate ?? ""} onChange={(event) => set("advancedHourlyRate", event.target.value)} /></label></>}
            <StandardDateField name="effective-from" label="Effective from" value={form.effectiveFrom ?? ""} onChange={(value) => set("effectiveFrom", value)} />
          </fieldset>
        </form>}
      </section>
    </main>
  );
}
