"use client";

import { useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";

type Profile = {
  studentNumber?: string;
  preferredName?: string;
  gender?: string;
  dateOfBirth?: string;
  admissionDate?: string;
  addressLine1?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  medicalOrAccessibilityNotes?: string;
  adminNotes?: string;
};
type Props = {
  academyId: string;
  studentId: string;
  profile?: Profile;
  onSaved: (profile: Profile) => void;
};
const date = (value?: string) => (value ? value.slice(0, 10) : "");
const fromProfile = (value?: Profile): Required<Profile> => ({
  studentNumber: value?.studentNumber ?? "",
  preferredName: value?.preferredName ?? "",
  gender: value?.gender ?? "",
  dateOfBirth: date(value?.dateOfBirth),
  admissionDate: date(value?.admissionDate),
  addressLine1: value?.addressLine1 ?? "",
  city: value?.city ?? "",
  state: value?.state ?? "",
  postalCode: value?.postalCode ?? "",
  emergencyContactName: value?.emergencyContactName ?? "",
  emergencyContactPhone: value?.emergencyContactPhone ?? "",
  medicalOrAccessibilityNotes: value?.medicalOrAccessibilityNotes ?? "",
  adminNotes: value?.adminNotes ?? "",
});

export function StudentAdminProfile({
  academyId,
  studentId,
  profile,
  onSaved,
}: Props) {
  const [form, setForm] = useState(fromProfile(profile));
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const [formSource, setFormSource] = useState({ profile, studentId, academyId });
  const lifecycle = useRef<{ source: typeof formSource; active: boolean } | null>(null);
  const pending = useRef<object | null>(null);
  if (formSource.profile !== profile || formSource.studentId !== studentId || formSource.academyId !== academyId) {
    setFormSource({ profile, studentId, academyId });
    setForm(fromProfile(profile));
    if (formSource.studentId !== studentId || formSource.academyId !== academyId) setMessage("");
    setSaving(false);
  }
  useEffect(() => {
    const current = { source: formSource, active: true };
    lifecycle.current = current;
    return () => { current.active = false; };
  }, [formSource]);
  const set = (key: keyof Profile, value: string) =>
    setForm((current) => ({ ...current, [key]: value }));
  async function save() {
    const current = lifecycle.current;
    if (!academyId || !studentId || !profile || !current?.active || current.source !== formSource || pending.current === current) return;
    pending.current = current;
    setSaving(true);
    setMessage("");
    try {
      const response = await academyApi(
        `/api/academies/${academyId}/students/${studentId}/profile`,
        {
          method: "PUT",
          headers: apiHeaders(true),
          body: JSON.stringify({
            ...form,
            dateOfBirth: form.dateOfBirth || null,
            admissionDate: form.admissionDate || null,
          }),
        },
      );
      const payload = await response.json().catch(() => null);
      if (!current.active) return;
      if (!response.ok) {
        setMessage(typeof payload?.message === "string" ? payload.message : "The profile could not be saved.");
        return;
      }
      if (!payload || typeof payload !== "object" || Array.isArray(payload)) {
        setMessage("Administrative profile saved. Reload to confirm the latest details.");
        return;
      }
      onSaved(payload);
      setMessage("Administrative profile saved.");
    } catch {
      if (current.active) setMessage("The profile could not be saved. Check the record before retrying.");
    } finally {
      if (pending.current === current) pending.current = null;
      if (current.active) setSaving(false);
    }
  }
  return (
    <section className="surface-panel rounded-xl p-5">
      <header className="student-admin-profile-header">
        <div className="min-w-0">
          <h3 className="font-semibold">Student 360 profile</h3>
        </div>
        <button
          onClick={() => void save()}
          disabled={saving || !profile || !academyId || !studentId}
          className="enterprise-action-button shrink-0 whitespace-nowrap"
        >
          {saving ? "Saving…" : "Save profile"}
        </button>
      </header>
      {message && <p className="mt-3 text-sm text-amber-200">{message}</p>}
      <fieldset disabled={saving || !profile} className="mt-5 grid gap-3 md:grid-cols-2">
        <input
          value={form.studentNumber}
          onChange={(e) => set("studentNumber", e.target.value)}
          placeholder="Student number, e.g. HMA-2026-001"
          className="field"
        />
        <input
          value={form.preferredName}
          onChange={(e) => set("preferredName", e.target.value)}
          placeholder="Preferred name"
          className="field"
        />
        <StandardSelectField
          name="student-gender"
          value={form.gender}
          onChange={(value) => set("gender", value)}
          placeholder="Gender (optional)"
          options={[
            { value: "Female", label: "Female" },
            { value: "Male", label: "Male" },
            { value: "Non-binary", label: "Non-binary" },
            { value: "Prefer not to say", label: "Prefer not to say" },
          ]}
        />
        <StandardDateField name="dateOfBirthDisplay" label="Date of birth" value={form.dateOfBirth} onChange={(value) => set("dateOfBirth", value)} />
        <StandardDateField name="admissionDateDisplay" label="Admission date" value={form.admissionDate} onChange={(value) => set("admissionDate", value)} />
        <input
          value={form.addressLine1}
          onChange={(e) => set("addressLine1", e.target.value)}
          placeholder="Address"
          className="field"
        />
        <input
          value={form.city}
          onChange={(e) => set("city", e.target.value)}
          placeholder="City"
          className="field"
        />
        <input
          value={form.state}
          onChange={(e) => set("state", e.target.value)}
          placeholder="State"
          className="field"
        />
        <input
          value={form.postalCode}
          onChange={(e) => set("postalCode", e.target.value)}
          placeholder="Postal / PIN code"
          className="field"
        />
        <input
          value={form.emergencyContactName}
          onChange={(e) => set("emergencyContactName", e.target.value)}
          placeholder="Emergency contact name"
          className="field"
        />
        <input
          value={form.emergencyContactPhone}
          onChange={(e) => set("emergencyContactPhone", e.target.value)}
          placeholder="Emergency contact phone"
          className="field"
        />
      </fieldset>
      <textarea
        disabled={saving || !profile}
        value={form.medicalOrAccessibilityNotes}
        onChange={(e) => set("medicalOrAccessibilityNotes", e.target.value)}
        placeholder="Medical or accessibility notes — authorised staff only"
        className="field mt-3 min-h-24"
      />
      <textarea
        disabled={saving || !profile}
        value={form.adminNotes}
        onChange={(e) => set("adminNotes", e.target.value)}
        placeholder="Internal admin notes — never shown in portals"
        className="field mt-3 min-h-24"
      />
    </section>
  );
}
