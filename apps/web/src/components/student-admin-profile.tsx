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
  return <ProfileEditor key={JSON.stringify([academyId, studentId])} academyId={academyId} studentId={studentId} profile={profile} onSaved={onSaved} />;
}

function ProfileEditor({
  academyId,
  studentId,
  profile,
  onSaved,
}: Props) {
  const [form, setForm] = useState(fromProfile(profile));
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  const mounted = useRef(false);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  const set = (key: keyof Profile, value: string) =>
    { if (!pending.current) setForm((current) => ({ ...current, [key]: value })); };
  async function save() {
    if (!mounted.current || pending.current || !academyId || !studentId) return;
    pending.current = true;
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
      if (!mounted.current) return;
      if (!response.ok) {
        setMessage(response.status >= 500 ? "The profile change could not be confirmed. Your draft has been retained. Check the student record before retrying." : "The profile could not be saved. Check your access and entered details. Your draft has been retained.");
        return;
      }
      setMessage("Administrative profile saved.");
      try {
        const payload: unknown = await response.json();
        if (!payload || typeof payload !== "object" || Array.isArray(payload)) throw new Error();
        const keys = Object.keys(fromProfile());
        if (keys.some((key) => { const value = (payload as Record<string, unknown>)[key]; return !(key in payload) || value != null && typeof value !== "string"; })) throw new Error();
        if (!mounted.current) return;
        onSaved(payload as Profile);
      } catch {
        if (mounted.current) setMessage("Administrative profile saved. The updated profile could not be displayed; do not repeat the save. Refresh the student record to see the latest data.");
      }
    } catch {
      if (mounted.current) setMessage("The profile change could not be confirmed. Your draft has been retained. Check the student record before retrying.");
    } finally {
      pending.current = false;
      if (mounted.current) setSaving(false);
    }
  }
  return (
    <section className="surface-panel rounded-xl p-5 student-admin-profile-editor">
      <header className="student-admin-profile-header">
        <div className="min-w-0">
          <h3 className="font-semibold">Student 360 profile</h3>
        </div>
        <button
          onClick={() => void save()}
          disabled={saving}
          className="enterprise-action-button shrink-0 whitespace-nowrap"
        >
          {saving ? "Saving…" : "Save profile"}
        </button>
      </header>
      {message && <p role="status" aria-live="polite" className="mt-3 text-sm text-amber-200">{message}</p>}
      <fieldset disabled={saving} className="min-w-0 border-0 p-0">
      <div className="mt-5 grid gap-3 md:grid-cols-2">
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
          disabled={saving}
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
        <StandardDateField key={saving ? "dob-saving" : "dob-editable"} name="dateOfBirthDisplay" label="Date of birth" value={form.dateOfBirth} onChange={(value) => set("dateOfBirth", value)} />
        <StandardDateField key={saving ? "admission-saving" : "admission-editable"} name="admissionDateDisplay" label="Admission date" value={form.admissionDate} onChange={(value) => set("admissionDate", value)} />
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
      </div>
      <textarea
        value={form.medicalOrAccessibilityNotes}
        onChange={(e) => set("medicalOrAccessibilityNotes", e.target.value)}
        placeholder="Medical or accessibility notes — authorised staff only"
        className="field mt-3 min-h-24"
      />
      <textarea
        value={form.adminNotes}
        onChange={(e) => set("adminNotes", e.target.value)}
        placeholder="Internal admin notes — never shown in portals"
        className="field mt-3 min-h-24"
      />
      </fieldset>
    </section>
  );
}
