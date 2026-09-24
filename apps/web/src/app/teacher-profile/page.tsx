"use client";

import { useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField, StandardDetailModal, StandardInteractiveTile, StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string };
type Teacher = { id: string; firstName: string; lastName: string };
type AssignedBatch = { id: string; batchName: string; courseName: string; isActive: boolean; completedSessions: number; pendingSessions: number; currentCycleCompleted: number; currentCyclePending: number };
type TeacherClass = { id: string; batchId: string; batchName: string; startUtc: string; endUtc: string; deliveryMode: string; roomName?: string | null; status: string };
type AssignedStudent = { id: string; name: string; batchName: string; subject: string };
type Availability = { day: string; from?: string | null; to?: string | null };
type Profile = {
  specialties?: string;
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
  students?: AssignedStudent[];
  availability?: Availability[];
  batches?: AssignedBatch[];
  classes?: TeacherClass[];
  leaveRequests?: unknown[];
};
const count = (rows?: unknown[]) => rows?.length ?? 0;
const day = (value?: string) => value?.slice(0, 10) ?? "";
const availabilityDays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

export default function TeacherProfilePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [teacherId, setTeacherId] = useState("");
  const [profile, setProfile] = useState<Profile>();
  const [form, setForm] = useState<Record<string, string>>({});
  const [message, setMessage] = useState("Loading teaching team…");
  const [saving, setSaving] = useState(false);
  const [detail, setDetail] = useState<"batches" | "sessions" | "students" | null>(null);
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
      specialties: value.specialties ?? "",
      availabilityJson: JSON.stringify(value.availability ?? []),
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
  const availability = readAvailability(form.availabilityJson);
  function updateAvailability(day: string, enabled: boolean, key?: "from" | "to", value?: string) {
    const current = availability.filter((slot) => slot.day !== day);
    const existing = availability.find((slot) => slot.day === day);
    if (enabled) current.push({ day, from: key === "from" ? value ?? "" : existing?.from ?? "", to: key === "to" ? value ?? "" : existing?.to ?? "" });
    set("availabilityJson", JSON.stringify(current));
  }
  return (
    <main className="enterprise-settings teacher-standard teacher-profile-standard">
      <header className="teacher-profile-heading">
        <div className="teacher-profile-title"><span className="teacher-profile-title-icon" aria-hidden="true">◎</span><div><p>Teachers</p><h1>360</h1></div></div>
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
            <StandardInteractiveTile label="Assigned batches" value={count(profile.batches)} detail="View batch and cycle details" onClick={() => setDetail("batches")} className="teacher-profile-summary-tile" />
            <StandardInteractiveTile label="Class sessions" value={count(profile.classes)} detail="View completed and pending sessions" onClick={() => setDetail("sessions")} className="teacher-profile-summary-tile" />
            <article className="teacher-profile-summary-tile">
              <span>Leave requests</span>
              <strong>
                {count(profile.leaveRequests)}
              </strong>
            </article>
            <StandardInteractiveTile label="Students assigned" value={count(profile.students)} detail="View students and their subjects" onClick={() => setDetail("students")} className="teacher-profile-summary-tile" />
          </section>
          <section className="teacher-profile-teaching-card">
            <header><div><p>Teaching details</p><h2>Subjects and availability</h2></div></header>
            <div className="teacher-profile-teaching-grid">
              <article><span>Subjects they can teach</span><input value={form.specialties ?? ""} onChange={(event) => set("specialties", event.target.value)} placeholder="e.g. Piano, Keyboard" aria-label="Subjects this teacher can teach" /></article>
              <article><span>Subjects currently assigned</span><p>{profile.subjects?.join(" · ") || "No current subject assignments"}</p></article>
              <article><span>Availability</span><div className="teacher-profile-availability-editor">{availabilityDays.map((availabilityDay) => { const slot = availability.find((item) => item.day === availabilityDay); return <div key={availabilityDay}><label><input type="checkbox" checked={Boolean(slot)} onChange={(event) => updateAvailability(availabilityDay, event.target.checked)} />{availabilityDay}</label><input type="time" value={slot?.from ?? ""} disabled={!slot} onChange={(event) => updateAvailability(availabilityDay, true, "from", event.target.value)} aria-label={`${availabilityDay} available from`} /><input type="time" value={slot?.to ?? ""} disabled={!slot} onChange={(event) => updateAvailability(availabilityDay, true, "to", event.target.value)} aria-label={`${availabilityDay} available to`} /></div>; })}</div></article>
            </div>
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
          {detail === "batches" && <StandardDetailModal eyebrow="Teacher 360" title="Assigned batches" onClose={() => setDetail(null)}>
            {profile.batches?.length ? <div className="teacher-profile-detail-list">{profile.batches.map((batch) => <article key={batch.id}><header><div><b>{batch.batchName}</b><small>{batch.courseName} · {batch.isActive ? "Active" : "Inactive"}</small></div><em>{batch.completedSessions} completed</em></header><div className="teacher-profile-cycle"><span>All sessions</span><b>{batch.completedSessions} completed · {batch.pendingSessions} pending</b></div><div className="teacher-profile-cycle"><span>Current cycle</span><b>{batch.currentCycleCompleted} completed · {batch.currentCyclePending} pending</b></div></article>)}</div> : <p className="teacher-profile-detail-empty">No batches are assigned to this teacher.</p>}
          </StandardDetailModal>}
          {detail === "sessions" && <StandardDetailModal eyebrow="Teacher 360" title="Class sessions" onClose={() => setDetail(null)}>
            {profile.classes?.length ? <div className="teacher-profile-detail-list">{profile.classes.map((session) => <article key={session.id}><header><div><b>{session.batchName}</b><small>{formatSessionDate(session.startUtc)} · {formatSessionTime(session.startUtc)}–{formatSessionTime(session.endUtc)} · {session.deliveryMode}{session.roomName ? ` · ${session.roomName}` : ""}</small></div><em data-completed={session.status === "Completed"}>{session.status}</em></header></article>)}</div> : <p className="teacher-profile-detail-empty">No class sessions are recorded for this teacher.</p>}
          </StandardDetailModal>}
          {detail === "students" && <StandardDetailModal eyebrow="Teacher 360" title="Students assigned" onClose={() => setDetail(null)}>
            {profile.students?.length ? <div className="teacher-profile-detail-list">{profile.students.map((student) => <article key={`${student.id}-${student.batchName}`}><header><div><b>{student.name}</b><small>{student.subject} · {student.batchName}</small></div></header></article>)}</div> : <p className="teacher-profile-detail-empty">No active students are assigned to this teacher.</p>}
          </StandardDetailModal>}
        </>
      )}
    </main>
  );
}

function formatSessionDate(value: string) { return new Intl.DateTimeFormat("en-IN", { day: "numeric", month: "short", year: "numeric", timeZone: "Asia/Kolkata" }).format(new Date(value)); }
function formatSessionTime(value: string) { return new Intl.DateTimeFormat("en-IN", { hour: "numeric", minute: "2-digit", timeZone: "Asia/Kolkata" }).format(new Date(value)); }
function readAvailability(value?: string) { try { const rows = JSON.parse(value ?? "[]") as Availability[]; return Array.isArray(rows) ? rows.filter((slot) => slot?.day) : []; } catch { return []; } }
