"use client";
import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField } from "@/components/standard-date-field";
import { StandardSelectField } from "@/components/standard-select-field";

type Academy = { id: string };
export default function StudentOnboardingPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [dob, setDob] = useState("");
  const [admissionDate, setAdmissionDate] = useState("");
  const [gender, setGender] = useState("");
  const [message, setMessage] = useState("Loading onboarding…");
  const [saving, setSaving] = useState(false);
  const minor =
    !!dob &&
    new Date(dob) >
      new Date(new Date().setFullYear(new Date().getFullYear() - 18));
  useEffect(() => {
    void academyApi("/api/academies")
      .then(async (response) => {
        const rows: Academy[] = await response.json();
        setAcademy(rows[0]);
        setMessage(rows[0] ? "" : "Create an academy first.");
      })
      .catch(() => setMessage("Onboarding could not be loaded."));
  }, []);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    setSaving(true);
    const form = new FormData(event.currentTarget);
    const body = Object.fromEntries(form) as Record<string, unknown>;
    for (const name of [
      "allowParentPortalAccess",
      "allowAcademicProgress",
      "allowFinance",
      "allowDocuments",
      "allowLeave",
    ])
      body[name] = form.get(name) === "on";
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/student-onboarding`,
        {
          method: "POST",
          headers: apiHeaders(true),
          body: JSON.stringify(body),
        },
      );
      const result = await response.json().catch(() => null);
      if (!response.ok)
        throw new Error(result?.message || "Onboarding could not be saved.");
      event.currentTarget.reset();
      setDob("");
      setAdmissionDate("");
      setGender("");
      setMessage(
        `Student onboarded. ${result.parentAccountCreated ? "Parent account created." : ""} ${result.studentAccountCreated ? "Student account created." : ""}`,
      );
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "Onboarding could not be saved.",
      );
    } finally {
      setSaving(false);
    }
  }
  return (
    <main className="enterprise-settings student-onboarding-standard">
      <header className="student-onboarding-heading">
        <div className="student-onboarding-title">
          <span className="student-onboarding-title-icon" aria-hidden="true">+</span>
          <div><p>Students</p><h1>Student onboarding</h1><span>One governed intake for student, parent, safeguarding contact, and portal access.</span></div>
        </div>
      </header>
      {message && (
        <p className="enterprise-page-state enterprise-page-state-loading">
          {message}
        </p>
      )}
      <form onSubmit={submit} className="student-onboarding-form">
        <section className="surface-panel onboarding-section">
          <header className="onboarding-section-header">
            <span>01</span>
            <div><h3>Student identity</h3><p>Required admission record</p></div>
          </header>
          <div className="onboarding-fields onboarding-fields-2">
            <input
              required
              name="studentFirstName"
              placeholder="Student first name"
            />
            <input
              required
              name="studentLastName"
              placeholder="Student last name"
            />
            <input name="studentNumber" placeholder="Student number (optional)" />
            <input name="preferredName" placeholder="Preferred name (optional)" />
            <StandardSelectField name="gender" value={gender} onChange={setGender} placeholder="Gender (optional)" options={[{ value: "Female", label: "Female" }, { value: "Male", label: "Male" }, { value: "Non-binary", label: "Non-binary" }, { value: "Prefer not to say", label: "Prefer not to say" }]} />
            <StandardDateField required name="dateOfBirth" label="Date of birth (DOB)" value={dob} onChange={setDob} />
            <StandardDateField name="admissionDate" label="Admission date" value={admissionDate} onChange={setAdmissionDate} />
          </div>
        </section>
        <section className="surface-panel onboarding-section">
          <header className="onboarding-section-header">
            <span>02</span>
            <div><h3>Student contact</h3><p>Contact and address details</p></div>
          </header>
          <div className="onboarding-fields onboarding-fields-2">
            <input
              name="studentEmail"
              type="email"
              placeholder="Student email (optional)"
            />
            <input
              name="studentPhone"
              placeholder="Student phone (optional)"
            />
            <input name="studentAddressLine1" placeholder="Student address" />
            <input name="studentCity" placeholder="City" />
            <input name="studentState" placeholder="State" />
            <input name="studentPostalCode" placeholder="Postal / PIN code" />
          </div>
        </section>
        <section className="surface-panel onboarding-section">
          <header className="onboarding-section-header">
            <span>03</span>
            <div><h3>Care & wellbeing</h3><p>Visible only to authorised staff</p></div>
          </header>
          <div className="onboarding-fields onboarding-fields-2">
            <input name="emergencyContactName" placeholder="Emergency contact name" />
            <input name="emergencyContactPhone" placeholder="Emergency contact phone" />
            <textarea className="onboarding-span-all" name="medicalOrAccessibilityNotes" placeholder="Medical or accessibility notes" />
            <p className="onboarding-helper onboarding-span-all">
              {minor
                ? "Minor student: Parent details and a primary Parent link are required."
                : "Adult student: Parent details are optional."}
            </p>
          </div>
        </section>
        <section className="surface-panel onboarding-section xl:col-span-2">
          <header className="onboarding-section-header">
            <span>04</span>
            <div><h3>Primary Parent</h3><p>Required for a minor; optional for an adult student</p></div>
          </header>
          <div className="onboarding-parent-layout">
            <div className="onboarding-fields onboarding-fields-2">
            <input
              required={minor}
              name="parentFirstName"
              placeholder="Parent first name"
            />
            <input
              required={minor}
              name="parentLastName"
              placeholder="Parent last name"
            />
            <input
              required={minor}
              name="parentEmail"
              type="email"
              placeholder="Parent email"
            />
            <input name="parentPhone" placeholder="Parent phone" />
            <input name="parentAddressLine1" placeholder="Parent address" />
            <input name="parentCity" placeholder="City" />
            <input name="relationship" defaultValue="Parent" />
            </div>
            <div className="onboarding-permissions">
              <p>Parent portal permissions</p>
            <label className="consent-check">
              <input
                name="allowParentPortalAccess"
                type="checkbox"
                defaultChecked={minor}
                disabled={minor}
              />{" "}
              Allow Parent portal access{" "}
              {minor
                ? "(automatically granted for a minor)"
                : "(adult student consent)"}
            </label>
            <label className="consent-check">
              <input
                name="allowAcademicProgress"
                type="checkbox"
                defaultChecked
              />{" "}
              Academic progress
            </label>
            <label className="consent-check">
              <input name="allowFinance" type="checkbox" defaultChecked /> Fees
              and receipts
            </label>
            <label className="consent-check">
              <input name="allowDocuments" type="checkbox" defaultChecked />{" "}
              Documents and certificates
            </label>
            <label className="consent-check">
              <input name="allowLeave" type="checkbox" defaultChecked /> Leave
              requests
            </label>
            </div>
          </div>
        </section>
        <section className="surface-panel onboarding-submit xl:col-span-2">
          <button
            disabled={saving || !academy}
          className="student-onboarding-submit"
          >
            {saving ? "Creating…" : "Complete onboarding"}
          </button>
        </section>
      </form>
    </main>
  );
}
