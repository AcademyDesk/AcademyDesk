"use client";
import { FormEvent, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { OnboardingField } from "@/components/design-system/onboarding-field";
import styles from "@/components/design-system/onboarding.module.css";

type Academy = { id: string };
export default function StudentOnboardingPage() {
  const router = useRouter();
  const [academy, setAcademy] = useState<Academy>();
  const [dob, setDob] = useState("");
  const [admissionDate, setAdmissionDate] = useState("");
  const [gender, setGender] = useState("");
  const [message, setMessage] = useState("Loading onboarding…");
  const [saving, setSaving] = useState(false);
  const [created, setCreated] = useState(false);
  const [parentPortalAccess, setParentPortalAccess] = useState(false);
  const pending = useRef(false);
  const completed = useRef(false);
  const active = useRef(true);
  const minor =
    !!dob &&
    new Date(dob) >
      new Date(new Date().setFullYear(new Date().getFullYear() - 18));
  useEffect(() => {
    active.current = true;
    void academyApi("/api/academies", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error();
        const rows: Academy[] = await response.json();
        if (!Array.isArray(rows) || (rows.length && (typeof rows[0]?.id !== "string" || !rows[0].id.trim()))) throw new Error();
        if (!active.current) return;
        setAcademy(rows[0]);
        setMessage(rows[0] ? "" : "Create an academy first.");
      })
      .catch(() => { if (active.current) setMessage("Onboarding could not be loaded. Please refresh or contact your administrator."); });
    return () => { active.current = false; };
  }, []);
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || pending.current || completed.current || !active.current) return;
    pending.current = true;
    setSaving(true);
    setMessage("Creating student…");
    const submittedForm = event.currentTarget;
    const form = new FormData(submittedForm);
    const body = Object.fromEntries(form) as Record<string, unknown>;
    body.admissionDate = form.get("admissionDate") || null;
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
      if (!active.current) return;
      if (!response.ok) {
        setMessage(response.status >= 500
          ? "Student creation could not be confirmed. Your details have been retained. Check Student Management before retrying."
          : `${typeof result?.message === "string" && result.message.trim() ? result.message : "Onboarding could not be saved."} Your details have been retained.`);
        return;
      }
      completed.current = true;
      setCreated(true);
      setMessage("Student created successfully. Opening Student Management…");
      if (typeof result?.id !== "string" || !result.id.trim()) {
        setMessage("Student created successfully, but the record link could not be loaded. Open Student Management to check the saved record; do not repeat onboarding.");
        return;
      }
      submittedForm.reset();
      setDob("");
      setAdmissionDate("");
      setGender("");
      setParentPortalAccess(false);
      router.push(`/student-management?studentId=${encodeURIComponent(result.id)}&notice=student-created`);
    } catch {
      if (active.current) setMessage(completed.current
        ? "Student created successfully, but Student Management could not be opened. Open it to check the saved record; do not repeat onboarding."
        : "Student creation could not be confirmed. Your details have been retained. Check Student Management before retrying.");
    } finally {
      if (active.current) setSaving(false);
      pending.current = false;
    }
  }
  return (
    <main className={`enterprise-settings student-onboarding-standard ${styles.page}`}>
      <header className="student-onboarding-heading">
        <div className="student-onboarding-title">
          <span className="student-onboarding-title-icon" aria-hidden="true">+</span>
          <div><p>Students</p><h1>Onboarding</h1></div>
        </div>
      </header>
      {message && (
        <p role="status" aria-live="polite" aria-atomic="true" className="enterprise-page-state enterprise-page-state-loading">
          {message}
        </p>
      )}
      {created && <a href="/student-management">Open Student Management</a>}
      <form onSubmit={submit} aria-busy={saving} className="student-onboarding-form">
        <fieldset className="contents" disabled={saving || !academy || created} aria-label="Student onboarding details">
        <section className="surface-panel onboarding-section">
          <header className="onboarding-section-header">
            <span>01</span>
            <div><h3>Student identity</h3><p>Required admission record</p></div>
          </header>
          <div className="onboarding-fields onboarding-fields-2">
            <OnboardingField label="Student first name (required)"><input
              required
              name="studentFirstName"
              placeholder="Student first name"
            /></OnboardingField>
            <OnboardingField label="Student last name (required)"><input
              required
              name="studentLastName"
              placeholder="Student last name"
            /></OnboardingField>
            <OnboardingField label="Student number (optional)"><input name="studentNumber" placeholder="Student number (optional)" /></OnboardingField>
            <OnboardingField label="Preferred name (optional)"><input name="preferredName" placeholder="Preferred name (optional)" /></OnboardingField>
            <StandardSelectField label="Gender (optional)" name="gender" value={gender} onChange={setGender} placeholder="Gender (optional)" options={[{ value: "Female", label: "Female" }, { value: "Male", label: "Male" }, { value: "Non-binary", label: "Non-binary" }, { value: "Prefer not to say", label: "Prefer not to say" }]} />
            <StandardDateField required name="dateOfBirth" label="Date of birth (DOB) (required)" value={dob} onChange={setDob} />
            <StandardDateField name="admissionDate" label="Admission date (optional)" value={admissionDate} onChange={setAdmissionDate} />
          </div>
        </section>
        <section className="surface-panel onboarding-section">
          <header className="onboarding-section-header">
            <span>02</span>
            <div><h3>Student contact</h3><p>Contact and address details</p></div>
          </header>
          <div className="onboarding-fields onboarding-fields-2">
            <OnboardingField label="Student email (optional)"><input
              name="studentEmail"
              type="email"
              placeholder="Student email (optional)"
            /></OnboardingField>
            <OnboardingField label="Student phone (optional)"><input
              name="studentPhone"
              type="tel"
              placeholder="Student phone (optional)"
            /></OnboardingField>
            <OnboardingField label="Student address (optional)"><input name="studentAddressLine1" placeholder="Student address" /></OnboardingField>
            <OnboardingField label="City (optional)"><input name="studentCity" placeholder="City" /></OnboardingField>
            <OnboardingField label="State (optional)"><input name="studentState" placeholder="State" /></OnboardingField>
            <OnboardingField label="Postal / PIN code (optional)"><input name="studentPostalCode" placeholder="Postal / PIN code" /></OnboardingField>
          </div>
        </section>
        <section className="surface-panel onboarding-section">
          <header className="onboarding-section-header">
            <span>03</span>
            <div><h3>Care & wellbeing</h3><p>Visible only to authorised staff</p></div>
          </header>
          <div className="onboarding-fields onboarding-fields-2">
            <OnboardingField label="Emergency contact name (optional)"><input name="emergencyContactName" placeholder="Emergency contact name" /></OnboardingField>
            <OnboardingField label="Emergency contact phone (optional)"><input type="tel" name="emergencyContactPhone" placeholder="Emergency contact phone" /></OnboardingField>
            <OnboardingField wide label="Medical or accessibility notes (optional)"><textarea name="medicalOrAccessibilityNotes" placeholder="Medical or accessibility notes" /></OnboardingField>
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
            <OnboardingField label={`Parent first name (${minor ? "required" : "optional"})`}><input
              required={minor}
              name="parentFirstName"
              placeholder="Parent first name"
            /></OnboardingField>
            <OnboardingField label={`Parent last name (${minor ? "required" : "optional"})`}><input
              required={minor}
              name="parentLastName"
              placeholder="Parent last name"
            /></OnboardingField>
            <OnboardingField label={`Parent email (${minor ? "required" : "optional"})`}><input
              required={minor}
              name="parentEmail"
              type="email"
              placeholder="Parent email"
            /></OnboardingField>
            <OnboardingField label="Parent phone (optional)"><input type="tel" name="parentPhone" placeholder="Parent phone" /></OnboardingField>
            <OnboardingField label="Parent address (optional)"><input name="parentAddressLine1" placeholder="Parent address" /></OnboardingField>
            <OnboardingField label="Parent city (optional)"><input name="parentCity" placeholder="City" /></OnboardingField>
            <OnboardingField label="Relationship"><input name="relationship" defaultValue="Parent" /></OnboardingField>
            </div>
            <div className="onboarding-permissions">
              <p>Parent portal permissions</p>
            <label className="consent-check">
              <input
                name="allowParentPortalAccess"
                type="checkbox"
                checked={minor || parentPortalAccess}
                onChange={(event) => setParentPortalAccess(event.target.checked)}
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
            disabled={saving || !academy || created}
          className="student-onboarding-submit"
          >
            {saving ? "Creating…" : "Complete onboarding"}
          </button>
        </section>
        </fieldset>
      </form>
    </main>
  );
}
