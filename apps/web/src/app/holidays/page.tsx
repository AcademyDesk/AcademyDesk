"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardDateField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Holiday = { id: string; name: string; holidayDate: string };

async function fetchHolidays(academyId: string): Promise<Holiday[]> {
  const response = await academyApi(`/api/academies/${academyId}/holidays`);
  if (!response.ok) throw new Error();
  return response.json();
}
type FormState = {
  name: string;
  holidayDate: string;
  notes: string;
  isClosed: boolean;
  scope: string;
};
export default function HolidaysPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [holidays, setHolidays] = useState<Holiday[]>([]);
  const [form, setForm] = useState<FormState>({
    name: "",
    holidayDate: "",
    notes: "",
    isClosed: true,
    scope: "Custom",
  });
  const [message, setMessage] = useState("Loading holidays…");
  const [saving, setSaving] = useState(false);
  const pendingWrite = useRef(false);
  useEffect(() => {
    void academyApi("/api/academies")
      .then(async (response) => {
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create an academy first.");
        setAcademy(academies[0]);
        setHolidays(await fetchHolidays(academies[0].id));
        setMessage("");
      })
      .catch(() => setMessage("Holidays could not be loaded."));
  }, []);

  async function write(url: string, init: RequestInit, success: string, fallback: string, clearForm = false) {
    if (!academy || pendingWrite.current) return;
    pendingWrite.current = true; setSaving(true); setMessage("");
    try {
      const response = await academyApi(url, init);
      if (!response.ok) {
        const result = await response.json().catch(() => null);
        const detail = typeof result?.message === "string" ? result.message.trim() : "";
        setMessage(response.status >= 500
          ? `${detail ? `${detail} ` : ""}The holiday change could not be confirmed; check the saved record before retrying.`
          : detail || fallback);
        return;
      }
      if (clearForm) setForm({ name: "", holidayDate: "", notes: "", isClosed: true, scope: "Custom" });
      setMessage(success);
      try { setHolidays(await fetchHolidays(academy.id)); }
      catch { setMessage(`${success} The register could not be refreshed; refresh to see the saved record and do not repeat the action.`); }
    } catch { setMessage("The holiday change could not be confirmed; check the saved record before retrying."); }
    finally { pendingWrite.current = false; setSaving(false); }
  }
  async function addDefaults() {
    if (!academy) return;
    await write(
      `/api/academies/${academy.id}/holidays/india-2026-defaults`,
      { method: "POST", headers: apiHeaders(true) },
      "Official India holidays added.", "Official holidays could not be added.",
    );
  }
  async function create(event: FormEvent) {
    event.preventDefault();
    if (!academy || !form.name || !form.holidayDate) return;
    await write(`/api/academies/${academy.id}/holidays`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify(form),
    }, "Holiday added.", "Holiday could not be added.", true);
  }
  async function remove(id: string) {
    if (!academy) return;
    await write(
      `/api/academies/${academy.id}/holidays/${id}`,
      { method: "DELETE", headers: apiHeaders() },
      "Holiday removed.", "Holiday could not be removed.",
    );
  }
  return (
    <main className="enterprise-settings holidays-standard min-h-screen">
      <WorkspaceNav />
      <div className="holidays-content mx-auto max-w-5xl px-6 py-10">
        <header className="holidays-heading">
          <div className="holidays-title">
            <span className="holidays-title-icon" aria-hidden="true">
              ◷
            </span>
            <div>
              <p>Academy experience</p>
              <h1>Holidays</h1>
            </div>
          </div>
          <button
            type="button"
            onClick={() => addDefaults()}
            disabled={!academy || saving}
          >
            Add India holidays 2026
          </button>
        </header>
        {message && (
          <p role="status" aria-live="polite" className="enterprise-page-state holidays-message">{message}</p>
        )}
        <section className="holidays-layout">
          <form onSubmit={create} className="holidays-panel">
            <header className="holidays-panel-header">
              <div>
                <p>Calendar closure</p>
                <h2>Add holiday</h2>
              </div>
            </header>
            <div className="holidays-fields">
              <label>
                <span>Holiday name</span>
                <input
                  disabled={saving}
                  required
                  value={form.name}
                  onChange={(event) =>
                    setForm({ ...form, name: event.target.value })
                  }
                  placeholder="Custom or regional holiday"
                />
              </label>
              <fieldset disabled={saving} className="m-0 min-w-0 border-0 p-0"><StandardDateField
                name="holiday-date"
                label="Date"
                value={form.holidayDate}
                onChange={(holidayDate) => setForm({ ...form, holidayDate })}
                required
              /></fieldset>
              <button disabled={saving} className="enterprise-action-button holidays-action">
                {saving ? "Saving…" : "Add holiday"}
              </button>
            </div>
          </form>
          <section className="holidays-panel holidays-register">
            <header className="holidays-panel-header">
              <div>
                <p>Calendar closure</p>
                <h2>Holiday register</h2>
              </div>
              <span>{holidays.length} dates</span>
            </header>
            {holidays.length === 0 ? (
              <p className="holidays-empty">No holidays added yet.</p>
            ) : (
              <ul>
                {holidays.map((holiday) => (
                  <li key={holiday.id}>
                    <div>
                      <b>{holiday.name}</b>
                      <small>
                        {new Intl.DateTimeFormat("en-IN", {
                          dateStyle: "medium",
                          timeZone: "Asia/Kolkata",
                        }).format(new Date(`${holiday.holidayDate}T12:00:00`))}
                      </small>
                    </div>
                    <button
                      type="button"
                      disabled={saving}
                      onClick={() => remove(holiday.id)}
                    >
                      Remove
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
