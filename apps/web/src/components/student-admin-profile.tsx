"use client";

import { useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

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
  useEffect(() => setForm(fromProfile(profile)), [profile, studentId]);
  const set = (key: keyof Profile, value: string) =>
    setForm((current) => ({ ...current, [key]: value }));
  async function save() {
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
      if (!response.ok)
        throw new Error(payload?.message ?? "The profile could not be saved.");
      onSaved(payload);
      setMessage("Administrative profile saved.");
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "The profile could not be saved.",
      );
    } finally {
      setSaving(false);
    }
  }
  return (
    <section className="surface-panel rounded-xl p-5">
      <div className="flex items-start justify-between gap-4">
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
      </div>
      {message && <p className="mt-3 text-sm text-amber-200">{message}</p>}
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
        <select
          value={form.gender}
          onChange={(e) => set("gender", e.target.value)}
          className="field"
        >
          <option value="">Gender (optional)</option>
          <option>Female</option>
          <option>Male</option>
          <option>Non-binary</option>
          <option>Prefer not to say</option>
        </select>
        <label className="text-sm text-slate-400">
          Date of birth
          <input
            type="date"
            value={form.dateOfBirth}
            onChange={(e) => set("dateOfBirth", e.target.value)}
            className="field mt-1"
          />
        </label>
        <label className="text-sm text-slate-400">
          Admission date
          <input
            type="date"
            value={form.admissionDate}
            onChange={(e) => set("admissionDate", e.target.value)}
            className="field mt-1"
          />
        </label>
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
    </section>
  );
}
