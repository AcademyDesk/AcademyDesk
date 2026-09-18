"use client";
import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
export default function StudentOnboardingPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [dob, setDob] = useState("");
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
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <p>Learners & families / onboarding</p>
        <h2>Student onboarding</h2>
        <span>
          One governed intake for learner, Parent, safeguarding contact, and
          portal access.
        </span>
      </header>
      {message && (
        <p className="enterprise-page-state enterprise-page-state-loading">
          {message}
        </p>
      )}
      <form onSubmit={submit} className="mt-5 grid gap-5 xl:grid-cols-2">
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">1. Student identity</h3>
          <div className="learner-form">
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
            <label className="field-label">
              Date of birth (DOB)
              <input
                required
                name="dateOfBirth"
                type="date"
                value={dob}
                onChange={(event) => setDob(event.target.value)}
              />
            </label>
            <input name="studentAddressLine1" placeholder="Student address" />
            <input name="studentCity" placeholder="Student city" />
            <p className="text-sm text-slate-400">
              {minor
                ? "Minor student: Parent details and primary Parent link are required."
                : "Adult student: Parent details are optional."}
            </p>
            <input
              name="studentEmail"
              type="email"
              placeholder="Student email (adult student)"
            />
            <input
              name="studentPhone"
              placeholder="Student phone (adult student)"
            />
          </div>
        </section>
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">2. Primary Parent</h3>
          <div className="learner-form">
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
        </section>
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">3. Student portal access</h3>
          <div className="learner-form">
            <input
              name="studentUserName"
              placeholder="Unique student username"
            />
            <input
              name="studentTemporaryPassword"
              type="password"
              minLength={6}
              placeholder="Temporary password"
            />
          </div>
        </section>
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">4. Parent portal access</h3>
          <div className="learner-form">
            <input name="parentUserName" placeholder="Unique parent username" />
            <input
              name="parentTemporaryPassword"
              type="password"
              minLength={6}
              placeholder="Temporary password"
            />
            <button disabled={saving || !academy}>
              {saving ? "Creating…" : "Complete onboarding"}
            </button>
          </div>
        </section>
      </form>
    </main>
  );
}
