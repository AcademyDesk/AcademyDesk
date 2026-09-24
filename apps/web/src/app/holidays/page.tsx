"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardDateField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Holiday = { id: string; name: string; holidayDate: string };
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
  async function load(item?: Academy) {
    const current = item ?? academy;
    if (!current) return;
    const response = await academyApi(`/api/academies/${current.id}/holidays`);
    if (!response.ok) throw new Error();
    setHolidays(await response.json());
    setMessage("");
  }
  useEffect(() => {
    void academyApi("/api/academies")
      .then(async (response) => {
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create an academy first.");
        setAcademy(academies[0]);
        await load(academies[0]);
      })
      .catch(() => setMessage("Holidays could not be loaded."));
  }, []);
  async function addDefaults() {
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/holidays/india-2026-defaults`,
      { method: "POST", headers: apiHeaders(true) },
    );
    if (!response.ok)
      return setMessage("Official holidays could not be added.");
    setMessage("Official India holidays added.");
    await load();
  }
  async function create(event: FormEvent) {
    event.preventDefault();
    if (!academy || !form.name || !form.holidayDate) return;
    const response = await academyApi(`/api/academies/${academy.id}/holidays`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify(form),
    });
    if (!response.ok) return setMessage("Holiday could not be added.");
    setForm({
      name: "",
      holidayDate: "",
      notes: "",
      isClosed: true,
      scope: "Custom",
    });
    await load();
  }
  async function remove(id: string) {
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/holidays/${id}`,
      { method: "DELETE", headers: apiHeaders() },
    );
    if (!response.ok) return setMessage("Holiday could not be removed.");
    await load();
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
            onClick={() => void addDefaults()}
            disabled={!academy}
          >
            Add India holidays 2026
          </button>
        </header>
        {message && (
          <p className="enterprise-page-state holidays-message">{message}</p>
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
                  required
                  value={form.name}
                  onChange={(event) =>
                    setForm({ ...form, name: event.target.value })
                  }
                  placeholder="Custom or regional holiday"
                />
              </label>
              <StandardDateField
                name="holiday-date"
                label="Date"
                value={form.holidayDate}
                onChange={(holidayDate) => setForm({ ...form, holidayDate })}
                required
              />
              <button className="enterprise-action-button holidays-action">
                Add holiday
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
                      onClick={() => void remove(holiday.id)}
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
