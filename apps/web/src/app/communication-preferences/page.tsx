"use client";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
type Academy = { id: string };
type Person = {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phone?: string | null;
};
type Preference = {
  recipientId: string;
  recipientType: string;
  emailAllowed: boolean;
  whatsAppAllowed: boolean;
  marketingAllowed: boolean;
  notes?: string | null;
};
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
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const responses = await Promise.all([
      academyApi(`/api/academies/${academyId}/students`),
      academyApi(`/api/academies/${academyId}/guardians`),
      academyApi(`/api/academies/${academyId}/communication-preferences`),
    ]);
    if (!responses.every((response) => response.ok)) throw Error();
    setStudents(await responses[0].json());
    setGuardians(await responses[1].json());
    setPreferences(await responses[2].json());
    setMessage("");
  }
  useEffect(() => {
    void academyApi("/api/academies")
      .then(async (response) => {
        if (!response.ok) throw Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) throw Error();
        setAcademy(academies[0]);
        await load(academies[0].id);
      })
      .catch(() => setMessage("Preferences could not be loaded."));
  }, []);
  const people = recipientType === "Guardian" ? guardians : students;
  const selected = useMemo(
    () =>
      preferences.find(
        (item) =>
          item.recipientType === recipientType &&
          item.recipientId === recipientId,
      ),
    [preferences, recipientType, recipientId],
  );
  useEffect(() => {
    setEmailAllowed(selected?.emailAllowed ?? false);
    setWhatsAppAllowed(selected?.whatsAppAllowed ?? false);
    setMarketingAllowed(selected?.marketingAllowed ?? false);
    setNotes(selected?.notes ?? "");
  }, [selected]);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !recipientId) return;
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
    if (!response.ok)
      return setMessage(result?.message ?? "Preference could not be saved.");
    setMessage("Contact preferences saved.");
    await load();
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
          <p className="preferences-notice" role="status">
            {message}
          </p>
        )}
        <form onSubmit={save} className="preferences-panel">
          <header className="preferences-panel-header">
            <p>Consent</p>
            <h2>Communication permissions</h2>
          </header>
          <div className="preferences-fields">
            <label>
              Contact type
              <StandardSelectField
                name="recipientType"
                value={recipientType}
                onChange={(value) => {
                  setRecipientType(value);
                  setRecipientId("");
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
                onChange={setRecipientId}
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
                    value={notes}
                    onChange={(event) => setNotes(event.target.value)}
                  />
                </label>
              </>
            )}
            <button
              disabled={!academy || !recipientId}
              className="enterprise-action-button preferences-wide"
            >
              Save preferences
            </button>
          </div>
        </form>
      </div>
    </main>
  );
}
