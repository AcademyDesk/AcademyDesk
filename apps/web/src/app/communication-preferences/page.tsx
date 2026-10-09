"use client";
import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
type Academy = { id: string };
type Person = {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
};
type Preference = {
  recipientId: string;
  recipientType: string;
  emailAllowed: boolean;
  whatsAppAllowed: boolean;
  marketingAllowed: boolean;
  notes?: string | null;
};
async function fetchWorkspace(academyId: string) {
  const responses = await Promise.all([
    academyApi(`/api/academies/${academyId}/communication-preferences/recipients`),
    academyApi(`/api/academies/${academyId}/communication-preferences`),
  ]);
  if (!responses.every((response) => response.ok)) throw Error();
  const recipients = await responses[0].json();
  const savedPreferences = await responses[1].json();
  const validPeople = (value: unknown): value is Person[] => Array.isArray(value) && value.every((person) =>
    person && typeof person.id === "string" && typeof person.firstName === "string" && typeof person.lastName === "string" &&
    (person.email == null || typeof person.email === "string"));
  if (!validPeople(recipients?.students) || !validPeople(recipients?.guardians) || !Array.isArray(savedPreferences)) throw Error("Invalid preference lookup response.");
  return { students: recipients.students, guardians: recipients.guardians, preferences: savedPreferences as Preference[] };
}
export default function CommunicationPreferencesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Person[]>([]);
  const [guardians, setGuardians] = useState<Person[]>([]);
  const [preferences, setPreferences] = useState<Preference[]>([]);
  const [recipientType, setRecipientType] = useState("Guardian");
  const [recipientId, setRecipientId] = useState("");
  const [emailAllowed, setEmailAllowed] = useState(false);
  const [whatsAppAllowed, setWhatsAppAllowed] = useState(false);
  const [marketingAllowed, setMarketingAllowed] = useState(false);
  const [notes, setNotes] = useState("");
  const [message, setMessage] = useState("Loading contact preferences…");
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  useEffect(() => {
    void academyApi("/api/academies")
      .then(async (response) => {
        if (!response.ok) throw Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) throw Error();
        setAcademy(academies[0]);
        const workspace = await fetchWorkspace(academies[0].id);
        setStudents(workspace.students);
        setGuardians(workspace.guardians);
        setPreferences(workspace.preferences);
        setMessage("");
      })
      .catch(() => setMessage("Preferences could not be loaded."));
  }, []);
  const people = recipientType === "Guardian" ? guardians : students;
  function applyPreference(selected?: Preference) {
    setEmailAllowed(selected?.emailAllowed ?? false);
    setWhatsAppAllowed(selected?.whatsAppAllowed ?? false);
    setMarketingAllowed(selected?.marketingAllowed ?? false);
    setNotes(selected?.notes ?? "");
  }
  function chooseRecipient(id: string, type = recipientType) {
    setRecipientId(id);
    applyPreference(preferences.find((item) => item.recipientType === type && item.recipientId === id));
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending.current) return;
    if (!academy || !recipientId) return;
    pending.current = true;
    setSaving(true);
    setMessage("");
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/communication-preferences/${recipientType}/${recipientId}`,
        {
          method: "PUT",
          headers: apiHeaders(true),
          body: JSON.stringify({
            emailAllowed,
            whatsAppAllowed,
            marketingAllowed,
            notes,
          }),
        },
      );
      const result = await response.json().catch(() => null);
      if (!response.ok) {
        const detail = typeof result?.message === "string" && result.message.trim()
          ? result.message.trim() : "Preference could not be saved.";
        return setMessage(response.status >= 500
          ? `Preference save could not be confirmed. Reload to check saved preferences before retrying. ${detail}`
          : detail);
      }
      try {
        const workspace = await fetchWorkspace(academy.id);
        setStudents(workspace.students);
        setGuardians(workspace.guardians);
        setPreferences(workspace.preferences);
        applyPreference(workspace.preferences.find((item) => item.recipientType === recipientType && item.recipientId === recipientId));
        setMessage("Contact preferences saved.");
      } catch {
        setMessage("Contact preferences saved. Saved preferences could not be refreshed; do not repeat the save. Reload to check the saved preferences.");
      }
    } catch {
      setMessage("Preference save could not be confirmed. Reload to check saved preferences before retrying.");
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }
  return (
    <main className="enterprise-settings preferences-standard min-h-screen">
      <WorkspaceNav />
      <div className="preferences-content">
        <header className="preferences-heading">
          <span className="preferences-title-icon" aria-hidden="true">
            ✓
          </span>
          <div className="preferences-title">
            <p>Engagement</p>
            <h1>Contact preferences</h1>
          </div>
        </header>
        {message && (
          <p className="preferences-notice" role="status" aria-live="polite">
            {message}
          </p>
        )}
        <form onSubmit={save} className="preferences-panel">
          <header className="preferences-panel-header">
            <p>Consent</p>
            <h2>Communication permissions</h2>
          </header>
          <fieldset className="preferences-fields m-0 min-w-0 border-0 p-0" disabled={saving}>
            <label>
              Contact type
              <StandardSelectField
                name="recipientType"
                value={recipientType}
                onChange={(value) => {
                  setRecipientType(value);
                  chooseRecipient("", value);
                }}
                placeholder="Select contact type"
                options={[
                  { value: "Guardian", label: "Parent" },
                  { value: "Student", label: "Student" },
                ]}
              />
            </label>
            <label>
              Contact
              <StandardSelectField
                name="recipientId"
                value={recipientId}
                onChange={chooseRecipient}
                placeholder={`Select ${recipientType.toLowerCase()}`}
                options={people.map((person) => ({
                  value: person.id,
                  label: `${person.firstName} ${person.lastName}${person.email ? ` · ${person.email}` : ""}`,
                }))}
              />
            </label>
            {recipientId && (
              <>
                <section className="preferences-options">
                  <label>
                    <input
                      type="checkbox"
                      checked={emailAllowed}
                      onChange={(event) =>
                        setEmailAllowed(event.target.checked)
                      }
                    />
                    <span>
                      <b>Email</b>
                      <small>Consent recorded</small>
                    </span>
                  </label>
                  <label>
                    <input
                      type="checkbox"
                      checked={whatsAppAllowed}
                      onChange={(event) =>
                        setWhatsAppAllowed(event.target.checked)
                      }
                    />
                    <span>
                      <b>WhatsApp</b>
                      <small>Consent recorded</small>
                    </span>
                  </label>
                  <label>
                    <input
                      type="checkbox"
                      checked={marketingAllowed}
                      onChange={(event) =>
                        setMarketingAllowed(event.target.checked)
                      }
                    />
                    <span>
                      <b>Marketing</b>
                      <small>Promotional messages allowed</small>
                    </span>
                  </label>
                </section>
                <label className="preferences-wide">
                  Consent notes
                  <textarea
                    aria-label="Consent notes"
                    value={notes}
                    onChange={(event) => setNotes(event.target.value)}
                  />
                </label>
              </>
            )}
            <button
              disabled={saving || !academy || !recipientId}
              className="enterprise-action-button preferences-wide"
            >
              {saving ? "Saving…" : "Save preferences"}
            </button>
          </fieldset>
        </form>
      </div>
    </main>
  );
}
