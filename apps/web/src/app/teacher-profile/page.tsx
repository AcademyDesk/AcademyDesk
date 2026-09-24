"use client";

import { type ReactNode, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string };
type Teacher = { id: string; firstName: string; lastName: string };
type AssignedBatch = { id: string; batchName: string; courseName: string; isActive: boolean; completedSessions: number; pendingSessions: number; currentCycleCompleted: number; currentCyclePending: number };
type TeacherClass = { id: string; batchId: string; batchName: string; startUtc: string; endUtc: string; deliveryMode: string; roomName?: string | null; status: string };
type Profile = {
  employeeCode?: string;
  preferredName?: string;
  employmentType?: string;
  dateOfBirth?: string;
  joiningDate?: string;
  qualifications?: string;
  addressLine1?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  adminNotes?: string;
  subjects?: string[];
  batches?: AssignedBatch[];
  classes?: TeacherClass[];
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
  const [detail, setDetail] = useState<"batches" | "sessions" | null>(null);
  function prepare(value: Profile) {
    setForm({
      employeeCode: value.employeeCode ?? "",
      preferredName: value.preferredName ?? "",
      employmentType: value.employmentType ?? "",
      dateOfBirth: day(value.dateOfBirth),
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
            dateOfBirth: form.dateOfBirth || null,
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
    <main className="enterprise-settings teacher-standard teacher-profile-standard">
      <header className="teacher-profile-heading">
        <div className="teacher-profile-title"><span className="teacher-profile-title-icon" aria-hidden="true">◎</span><div><p>Teachers</p><h1>Teacher 360</h1></div></div>
      </header>
      <section className="teacher-profile-toolbar">
        <StandardSelectField
          name="teacher-record"
          value={teacherId}
          onChange={(value) => void select(value)}
          placeholder="Select teacher"
          options={teachers.map((teacher) => ({ value: teacher.id, label: `${teacher.firstName} ${teacher.lastName}` }))}
        />
      </section>
      {message && (
        <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/30 p-3 text-sm text-amber-100">
          {message}
        </p>
      )}
      {profile && (
        <>
          <section className="teacher-profile-summary-grid">
            <button type="button" onClick={() => setDetail("batches")} className="teacher-profile-summary-tile teacher-profile-summary-action">
              <span>Assigned batches</span>
              <strong>
                {count(profile.batches)}
              </strong>
              <small>View batch and cycle details</small>
            </button>
            <button type="button" onClick={() => setDetail("sessions")} className="teacher-profile-summary-tile teacher-profile-summary-action">
              <span>Class sessions</span>
              <strong>
                {count(profile.classes)}
              </strong>
              <small>View completed and pending sessions</small>
            </button>
            <article className="teacher-profile-summary-tile">
              <span>Leave requests</span>
              <strong>
                {count(profile.leaveRequests)}
              </strong>
            </article>
            <article className="teacher-profile-summary-tile teacher-profile-subjects">
              <span>Subjects taught</span>
              <strong>{count(profile.subjects)}</strong>
              <small>{profile.subjects?.join(" · ") || "No subjects assigned"}</small>
            </article>
          </section>
          <section className="teacher-profile-card">
            <header className="teacher-profile-card-header">
              <div><p>Profile</p><h2>Teacher profile</h2></div>
              <button
                onClick={() => void save()}
                disabled={saving}
                className="enterprise-action-button"
              >
                {saving ? "Saving…" : "Save profile"}
              </button>
            </header>
            <div className="teacher-profile-fields">
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
              <StandardSelectField
                name="teacher-employment-type"
                value={form.employmentType ?? ""}
                onChange={(value) => set("employmentType", value)}
                placeholder="Employment type"
                options={[{ value: "Full-time", label: "Full-time" }, { value: "Part-time", label: "Part-time" }, { value: "Contract", label: "Contract" }, { value: "Visiting faculty", label: "Visiting faculty" }]}
              />
              <StandardDateField name="teacher-date-of-birth" label="Date of birth (DOB)" value={form.dateOfBirth ?? ""} onChange={(value) => set("dateOfBirth", value)} />
              <StandardDateField name="teacher-joining-date" label="Joining date" value={form.joiningDate ?? ""} onChange={(value) => set("joiningDate", value)} />
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
              className="field teacher-profile-notes"
            />
            <textarea
              value={form.adminNotes ?? ""}
              onChange={(e) => set("adminNotes", e.target.value)}
              placeholder="Internal operational notes — not visible in teacher portal"
              className="field teacher-profile-notes"
            />
          </section>
          {detail === "batches" && <TeacherDetailModal title="Assigned batches" onClose={() => setDetail(null)}>
            {profile.batches?.length ? <div className="teacher-profile-detail-list">{profile.batches.map((batch) => <article key={batch.id}><header><div><b>{batch.batchName}</b><small>{batch.courseName} · {batch.isActive ? "Active" : "Inactive"}</small></div><em>{batch.completedSessions} completed</em></header><div className="teacher-profile-cycle"><span>All sessions</span><b>{batch.completedSessions} completed · {batch.pendingSessions} pending</b></div><div className="teacher-profile-cycle"><span>Current cycle</span><b>{batch.currentCycleCompleted} completed · {batch.currentCyclePending} pending</b></div></article>)}</div> : <p className="teacher-profile-detail-empty">No batches are assigned to this teacher.</p>}
          </TeacherDetailModal>}
          {detail === "sessions" && <TeacherDetailModal title="Class sessions" onClose={() => setDetail(null)}>
            {profile.classes?.length ? <div className="teacher-profile-detail-list">{profile.classes.map((session) => <article key={session.id}><header><div><b>{session.batchName}</b><small>{formatSessionDate(session.startUtc)} · {formatSessionTime(session.startUtc)}–{formatSessionTime(session.endUtc)} · {session.deliveryMode}{session.roomName ? ` · ${session.roomName}` : ""}</small></div><em data-completed={session.status === "Completed"}>{session.status}</em></header></article>)}</div> : <p className="teacher-profile-detail-empty">No class sessions are recorded for this teacher.</p>}
          </TeacherDetailModal>}
        </>
      )}
    </main>
  );
}

function TeacherDetailModal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  return <div className="teacher-profile-detail-modal" role="dialog" aria-modal="true" aria-label={title} onMouseDown={onClose}><article onMouseDown={(event) => event.stopPropagation()}><header><div><p>Teacher 360</p><h2>{title}</h2></div><button type="button" onClick={onClose} aria-label="Close details">×</button></header>{children}</article></div>;
}
function formatSessionDate(value: string) { return new Intl.DateTimeFormat("en-IN", { day: "numeric", month: "short", year: "numeric", timeZone: "Asia/Kolkata" }).format(new Date(value)); }
function formatSessionTime(value: string) { return new Intl.DateTimeFormat("en-IN", { hour: "numeric", minute: "2-digit", timeZone: "Asia/Kolkata" }).format(new Date(value)); }
