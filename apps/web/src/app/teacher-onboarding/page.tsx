"use client";

import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
type Academy = { id: string };
const days = [
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
  "Sunday",
];

export default function TeacherOnboardingPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [employmentType, setEmploymentType] = useState("Full-time");
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
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    setSaving(true);
    const form = new FormData(event.currentTarget);
    const availability = days
      .map((day) => ({
        day,
        available: form.get(`available-${day}`) === "on",
        from: form.get(`from-${day}`),
        to: form.get(`to-${day}`),
      }))
      .filter((row) => row.available);
    const compensation = {
      model: form.get("payModel"),
      baseMonthlySalary: form.get("baseMonthlySalary"),
      baseHourlyRate: form.get("baseHourlyRate"),
      beginnerHourlyRate: form.get("beginnerHourlyRate"),
      intermediateHourlyRate: form.get("intermediateHourlyRate"),
      advancedHourlyRate: form.get("advancedHourlyRate"),
      effectiveFrom: form.get("effectiveFrom"),
    };
    const body = Object.fromEntries(form);
    body.availabilityJson = JSON.stringify(availability);
    body.compensationJson = JSON.stringify(compensation);
    body.certificationsJson = JSON.stringify([
      {
        specialty: form.get("certificationSpecialty"),
        highestCertification: form.get("highestCertification"),
      },
    ]);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/teachers`,
        {
          method: "POST",
          headers: apiHeaders(true),
          body: JSON.stringify(body),
        },
      );
      const result = await response.json().catch(() => null);
      if (!response.ok)
        throw new Error(result?.message ?? "Teacher could not be created.");
      event.currentTarget.reset();
      setEmploymentType("Full-time");
      setMessage(
        "Teacher onboarded. Availability and compensation history are now recorded.",
      );
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "Teacher could not be created.",
      );
    } finally {
      setSaving(false);
    }
  }
  return (
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <h2>Teacher onboarding</h2>
      </header>
      {message && (
        <p className="enterprise-page-state enterprise-page-state-loading">
          {message}
        </p>
      )}
      <form onSubmit={submit} className="mt-5 grid gap-5 xl:grid-cols-2">
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">1. Personal details</h3>
          <div className="learner-form">
            <input required name="firstName" placeholder="First name" />
            <input required name="lastName" placeholder="Last name" />
            <input required type="email" name="email" placeholder="Email" />
            <input name="phone" placeholder="Phone" />
            <input name="addressLine1" placeholder="Address" />
            <input name="city" placeholder="City" />
            <input name="state" placeholder="State" />
            <input name="postalCode" placeholder="PIN code" />
          </div>
        </section>
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">2. Professional details</h3>
          <div className="learner-form">
            <input
              name="specialties"
              placeholder="Specialties, e.g. Piano, Vocal"
            />
            <input name="qualifications" placeholder="Highest qualification" />
            <input
              name="certificationSpecialty"
              placeholder="Certification specialty"
            />
            <input
              name="highestCertification"
              placeholder="Highest certification"
            />
            <select
              name="employmentType"
              value={employmentType}
              onChange={(event) => setEmploymentType(event.target.value)}
            >
              <option>Full-time</option>
              <option>Part-time</option>
              <option>Contract</option>
            </select>
            <input type="date" name="joiningDate" aria-label="Joining date" />
          </div>
        </section>
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">3. Availability</h3>
          <p className="mt-1 text-sm text-slate-400">
            Set working hours for each available day.
          </p>
          <div className="mt-4 space-y-2">
            {days.map((day) => (
              <div key={day} className="availability-row">
                <label className="consent-check">
                  <input type="checkbox" name={`available-${day}`} />
                  {day}
                </label>
                <input
                  type="time"
                  name={`from-${day}`}
                  aria-label={`${day} start time`}
                />
                <input
                  type="time"
                  name={`to-${day}`}
                  aria-label={`${day} end time`}
                />
              </div>
            ))}
          </div>
        </section>
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">4. Compensation</h3>
          <div className="learner-form">
            <select name="payModel">
              <option value="Monthly">Monthly salary</option>
              <option value="Hourly">Single hourly rate</option>
              <option value="LevelHourly">Hourly rate by teaching level</option>
            </select>
            <input
              name="baseMonthlySalary"
              type="number"
              min="0"
              placeholder="Monthly salary"
            />
            <input
              name="baseHourlyRate"
              type="number"
              min="0"
              placeholder="Single hourly rate"
            />
            <input
              name="beginnerHourlyRate"
              type="number"
              min="0"
              placeholder="Beginner hourly rate"
            />
            <input
              name="intermediateHourlyRate"
              type="number"
              min="0"
              placeholder="Intermediate hourly rate"
            />
            <input
              name="advancedHourlyRate"
              type="number"
              min="0"
              placeholder="Advanced hourly rate"
            />
            <label className="field-label">
              Effective from
              <input type="date" name="effectiveFrom" />
            </label>
            <button disabled={saving || !academy}>
              {saving ? "Saving…" : "Complete teacher onboarding"}
            </button>
          </div>
        </section>
      </form>
    </main>
  );
}
