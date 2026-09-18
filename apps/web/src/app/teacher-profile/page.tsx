"use client";

import { useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Teacher = { id: string; firstName: string; lastName: string };
type Profile = {
  employeeCode?: string;
  preferredName?: string;
  employmentType?: string;
  joiningDate?: string;
  qualifications?: string;
  addressLine1?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  adminNotes?: string;
  batches?: unknown[];
  classes?: unknown[];
  leaveRequests?: unknown[];
};
const count = (rows?: unknown[]) => rows?.length ?? 0;
const day = (value?: string) => value?.slice(0, 10) ?? "";

export default function TeacherProfilePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [teacherId, setTeacherId] = useState("");
  const [profile, setProfile] = useState<Profile>();
  const [form, setForm] = useState<Record<string, string>>({});
  const [message, setMessage] = useState("Loading teaching team…");
  const [saving, setSaving] = useState(false);
  function prepare(value: Profile) {
    setForm({
      employeeCode: value.employeeCode ?? "",
      preferredName: value.preferredName ?? "",
      employmentType: value.employmentType ?? "",
      joiningDate: day(value.joiningDate),
      qualifications: value.qualifications ?? "",
      addressLine1: value.addressLine1 ?? "",
      city: value.city ?? "",
      state: value.state ?? "",
      postalCode: value.postalCode ?? "",
      emergencyContactName: value.emergencyContactName ?? "",
      emergencyContactPhone: value.emergencyContactPhone ?? "",
      adminNotes: value.adminNotes ?? "",
    });
  }
  async function load(id: string, academyId = academy?.id) {
    if (!academyId || !id) return;
    const response = await academyApi(
      `/api/academies/${academyId}/teachers/${id}/profile`,
    );
    if (!response.ok) throw new Error();
    const value: Profile = await response.json();
    setProfile(value);
    prepare(value);
  }
  useEffect(() => {
    void (async () => {
      try {
        const academies: Academy[] = await (
          await academyApi("/api/academies")
        ).json();
        if (!academies[0])
          return setMessage(
            "Create an academy before managing the teaching team.",
          );
        setAcademy(academies[0]);
        const rows: Teacher[] = await (
          await academyApi(`/api/academies/${academies[0].id}/teachers`)
        ).json();
        setTeachers(rows);
        const requestedId = new URLSearchParams(window.location.search).get(
          "teacherId",
        );
        const selected =
          rows.find((teacher) => teacher.id === requestedId) ?? rows[0];
        if (selected) {
          setTeacherId(selected.id);
          await load(selected.id, academies[0].id);
        }
        setMessage("");
      } catch {
        setMessage("Teacher records could not be loaded.");
      }
    })();
  }, []);
  async function select(id: string) {
    setTeacherId(id);
    try {
      await load(id);
      setMessage("");
    } catch {
      setMessage("The teacher record could not be loaded.");
    }
  }
  async function save() {
    if (!academy || !teacherId) return;
    setSaving(true);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/teachers/${teacherId}/profile`,
        {
          method: "PUT",
          headers: apiHeaders(true),
          body: JSON.stringify({
            ...form,
            joiningDate: form.joiningDate || null,
          }),
        },
      );
      const value = await response.json().catch(() => null);
      if (!response.ok)
        throw new Error(value?.message ?? "The profile could not be saved.");
      setProfile(value);
      prepare(value);
      setMessage("Teacher administrative profile saved.");
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
  const set = (key: string, value: string) =>
    setForm((current) => ({ ...current, [key]: value }));
  return (
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <p>Team & access / record</p>
        <h2>Teacher record</h2>
        <span>
          A governed staff record for credentialing, assignment capacity, and
          operational duty of care.
        </span>
      </header>
      <section className="enterprise-settings-toolbar">
        <select
          value={teacherId}
          onChange={(e) => void select(e.target.value)}
          className="field max-w-md"
        >
          <option value="">Select teacher</option>
          {teachers.map((teacher) => (
            <option key={teacher.id} value={teacher.id}>
              {teacher.firstName} {teacher.lastName}
            </option>
          ))}
        </select>
      </section>
      {message && (
        <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/30 p-3 text-sm text-amber-100">
          {message}
        </p>
      )}
      {profile && (
        <>
          <section className="mt-5 grid gap-4 md:grid-cols-3">
            <div className="surface-panel rounded-xl p-5">
              <p className="text-sm text-slate-400">Assigned batches</p>
              <strong className="mt-2 block text-3xl">
                {count(profile.batches)}
              </strong>
            </div>
            <div className="surface-panel rounded-xl p-5">
              <p className="text-sm text-slate-400">Class sessions</p>
              <strong className="mt-2 block text-3xl">
                {count(profile.classes)}
              </strong>
            </div>
            <div className="surface-panel rounded-xl p-5">
              <p className="text-sm text-slate-400">Leave requests</p>
              <strong className="mt-2 block text-3xl">
                {count(profile.leaveRequests)}
              </strong>
            </div>
          </section>
          <section className="surface-panel mt-5 rounded-xl p-5">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="font-semibold">
                  Administrative teacher profile
                </h3>
                <p className="mt-1 text-sm text-slate-400">
                  Employment, qualifications, emergency contact and internal
                  operational notes.
                </p>
              </div>
              <button
                onClick={() => void save()}
                disabled={saving}
                className="rounded-lg bg-cyan-400 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60"
              >
                {saving ? "Saving…" : "Save profile"}
              </button>
            </div>
            <div className="mt-5 grid gap-3 md:grid-cols-2">
              <input
                value={form.employeeCode ?? ""}
                onChange={(e) => set("employeeCode", e.target.value)}
                placeholder="Employee code"
                className="field"
              />
              <input
                value={form.preferredName ?? ""}
                onChange={(e) => set("preferredName", e.target.value)}
                placeholder="Preferred name"
                className="field"
              />
              <select
                value={form.employmentType ?? ""}
                onChange={(e) => set("employmentType", e.target.value)}
                className="field"
              >
                <option value="">Employment type</option>
                <option>Full-time</option>
                <option>Part-time</option>
                <option>Contract</option>
                <option>Visiting faculty</option>
              </select>
              <label className="text-sm text-slate-400">
                Joining date
                <input
                  type="date"
                  value={form.joiningDate ?? ""}
                  onChange={(e) => set("joiningDate", e.target.value)}
                  className="field mt-1"
                />
              </label>
              <input
                value={form.addressLine1 ?? ""}
                onChange={(e) => set("addressLine1", e.target.value)}
                placeholder="Address"
                className="field"
              />
              <input
                value={form.city ?? ""}
                onChange={(e) => set("city", e.target.value)}
                placeholder="City"
                className="field"
              />
              <input
                value={form.state ?? ""}
                onChange={(e) => set("state", e.target.value)}
                placeholder="State"
                className="field"
              />
              <input
                value={form.postalCode ?? ""}
                onChange={(e) => set("postalCode", e.target.value)}
                placeholder="Postal / PIN code"
                className="field"
              />
              <input
                value={form.emergencyContactName ?? ""}
                onChange={(e) => set("emergencyContactName", e.target.value)}
                placeholder="Emergency contact name"
                className="field"
              />
              <input
                value={form.emergencyContactPhone ?? ""}
                onChange={(e) => set("emergencyContactPhone", e.target.value)}
                placeholder="Emergency contact phone"
                className="field"
              />
            </div>
            <textarea
              value={form.qualifications ?? ""}
              onChange={(e) => set("qualifications", e.target.value)}
              placeholder="Qualifications, certifications and background"
              className="field mt-3 min-h-24"
            />
            <textarea
              value={form.adminNotes ?? ""}
              onChange={(e) => set("adminNotes", e.target.value)}
              placeholder="Internal operational notes — not visible in teacher portal"
              className="field mt-3 min-h-24"
            />
          </section>
        </>
      )}
    </main>
  );
}
