"use client";

import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string };
type SubjectEntry = { subject: string; certification: string };
const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

export default function TeacherOnboardingPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [employmentType, setEmploymentType] = useState("Full-time");
  const [subjects, setSubjects] = useState<SubjectEntry[]>([{ subject: "", certification: "" }]);
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [joiningDate, setJoiningDate] = useState("");
  const [message, setMessage] = useState("Loading teacher onboarding…");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    void academyApi("/api/academies")
      .then(async (response) => {
        const rows: Academy[] = await response.json();
        setAcademy(rows[0]);
        setMessage(rows[0] ? "" : "Create an academy first.");
      })
      .catch(() => setMessage("Teacher onboarding could not be loaded."));
  }, []);

  function updateSubject(index: number, key: keyof SubjectEntry, value: string) {
    setSubjects((current) => current.map((entry, entryIndex) => entryIndex === index ? { ...entry, [key]: value } : entry));
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const subjectEntries = subjects
      .map(({ subject, certification }) => ({ subject: subject.trim(), certification: certification.trim() || null }))
      .filter(({ subject }) => Boolean(subject));
    if (!subjectEntries.length) {
      setMessage("Add at least one subject taught.");
      return;
    }

    setSaving(true);
    const submittedForm = event.currentTarget;
    const form = new FormData(submittedForm);
    const availability = days
      .map((day) => ({
        day,
        available: form.get(`available-${day}`) === "on",
        from: form.get(`from-${day}`),
        to: form.get(`to-${day}`),
      }))
      .filter((row) => row.available);
    const body = Object.fromEntries(form) as Record<string, unknown>;
    body.dateOfBirth = form.get("dateOfBirth") || null;
    body.joiningDate = form.get("joiningDate") || null;
    body.specialties = subjectEntries.map(({ subject }) => subject).join(", ");
    body.availabilityJson = JSON.stringify(availability);
    body.certificationsJson = JSON.stringify(subjectEntries);

    try {
      const response = await academyApi(`/api/academies/${academy.id}/teachers`, {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify(body),
      });
      const result = await response.json().catch(() => null);
      if (!response.ok) throw new Error(result?.message ?? "Teacher could not be created.");
      submittedForm.reset();
      setEmploymentType("Full-time");
      setSubjects([{ subject: "", certification: "" }]);
      setDateOfBirth("");
      setJoiningDate("");
      setMessage("Teacher onboarded. Add payment details from the Teacher payment details section.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Teacher could not be created.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <main className="enterprise-settings teacher-standard teacher-onboarding-standard">
      <header className="teacher-onboarding-heading"><div className="teacher-onboarding-title"><span className="teacher-onboarding-title-icon" aria-hidden="true">+</span><div><p>Teachers</p><h1>Onboarding</h1><span>Create the teaching record, subjects, availability, and employment details in one intake.</span></div></div></header>
      {message && <p className="enterprise-page-state enterprise-page-state-loading teacher-onboarding-message">{message}</p>}
      <form onSubmit={submit} className="teacher-onboarding-form">
        <section className="surface-panel onboarding-section">
          <div className="onboarding-section-header"><span>01</span><div><h3>Personal details</h3></div></div>
          <div className="onboarding-fields onboarding-fields-2">
            <input required name="firstName" placeholder="First name" />
            <input required name="lastName" placeholder="Last name" />
            <input required type="email" name="email" placeholder="Email" />
            <input name="phone" placeholder="Phone" />
            <input className="onboarding-span-all" name="addressLine1" placeholder="Address" />
            <input name="city" placeholder="City" />
            <input name="state" placeholder="State" />
            <input name="postalCode" placeholder="Postal / PIN code" />
          </div>
        </section>

        <section className="surface-panel onboarding-section">
          <div className="onboarding-section-header"><span>02</span><div><h3>Professional details</h3></div></div>
          <div className="onboarding-fields">
            <input name="qualifications" placeholder="Highest degree / qualification" />
            <div className="teacher-subjects onboarding-span-all">
              <div className="teacher-subjects-header"><label>Subjects taught</label><button type="button" className="teacher-inline-button" onClick={() => setSubjects((current) => [...current, { subject: "", certification: "" }])}>Add subject</button></div>
              {subjects.map((entry, index) => <div className="teacher-subject-row" key={`subject-${index}`}>
                <input required value={entry.subject} onChange={(event) => updateSubject(index, "subject", event.target.value)} placeholder="Subject, e.g. Piano" aria-label={`Subject ${index + 1}`} />
                <input value={entry.certification} onChange={(event) => updateSubject(index, "certification", event.target.value)} placeholder="Certification (optional)" aria-label={`Certification for subject ${index + 1}`} />
                {subjects.length > 1 && <button type="button" className="teacher-remove-button" onClick={() => setSubjects((current) => current.filter((_, subjectIndex) => subjectIndex !== index))}>Remove</button>}
              </div>)}
            </div>
            <StandardSelectField
              name="employmentType"
              value={employmentType}
              onChange={setEmploymentType}
              placeholder="Employment type"
              options={[
                { value: "Full-time", label: "Full-time" },
                { value: "Part-time", label: "Part-time" },
                { value: "Contract", label: "Contract" },
              ]}
            />
            <StandardDateField name="dateOfBirth" label="Date of birth (DOB)" value={dateOfBirth} onChange={setDateOfBirth} />
            <StandardDateField name="joiningDate" label="Joining date" value={joiningDate} onChange={setJoiningDate} />
          </div>
        </section>

        <section className="surface-panel onboarding-section">
          <div className="onboarding-section-header"><span>03</span><div><h3>Availability</h3></div></div>
          <div className="availability-list">
            {days.map((day) => <div key={day} className="availability-row"><label className="consent-check"><input type="checkbox" name={`available-${day}`} />{day}</label><input type="time" name={`from-${day}`} aria-label={`${day} start time`} /><input type="time" name={`to-${day}`} aria-label={`${day} end time`} /></div>)}
          </div>
        </section>

        <div className="teacher-onboarding-action"><button className="teacher-onboarding-submit" disabled={saving || !academy}>{saving ? "Saving…" : "Complete teacher onboarding"}</button></div>
      </form>
    </main>
  );
}
