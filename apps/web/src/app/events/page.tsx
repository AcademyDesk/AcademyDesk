"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import {
  StandardDateField,
  StandardSelectField,
  StandardTimeField,
} from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Branch = { id: string; name: string };
type Event = {
  id: string;
  title: string;
  type: string;
  branchId?: string | null;
  startUtc: string;
  endUtc: string;
  venue?: string | null;
  capacity?: number | null;
  status: string;
  notes?: string | null;
};
const eventTypes = ["Recital", "Workshop", "Exam", "Concert", "Masterclass"];
async function fetchWorkspace(academyId: string) {
  const [eventResponse, branchResponse] = await Promise.all([
    academyApi(`/api/academies/${academyId}/events`),
    academyApi(`/api/academies/${academyId}/branches`),
  ]);
  if (!eventResponse.ok || !branchResponse.ok) throw new Error();
  return {
    events: (await eventResponse.json()) as Event[],
    branches: (await branchResponse.json()) as Branch[],
  };
}
export default function EventsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [branches, setBranches] = useState<Branch[]>([]);
  const [events, setEvents] = useState<Event[]>([]);
  const [title, setTitle] = useState("");
  const [type, setType] = useState("Recital");
  const [branchId, setBranchId] = useState("");
  const [startDate, setStartDate] = useState("");
  const [startTime, setStartTime] = useState("10:00");
  const [endDate, setEndDate] = useState("");
  const [endTime, setEndTime] = useState("11:00");
  const [venue, setVenue] = useState("");
  const [message, setMessage] = useState("Loading events…");
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create your academy first.");
        setAcademy(academies[0]);
        const workspace = await fetchWorkspace(academies[0].id);
        setEvents(workspace.events);
        setBranches(workspace.branches);
        setMessage("");
      } catch {
        setMessage("Events could not be loaded.");
      }
    })();
  }, []);
  async function create(event: FormEvent) {
    event.preventDefault();
    if (pending.current) return;
    if (!academy || !startDate || !endDate)
      return setMessage("Enter a title, start, and end time.");
    pending.current = true;
    setSaving(true);
    setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/events`, {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          title,
          type,
          branchId: branchId || null,
          startUtc: new Date(`${startDate}T${startTime}:00`).toISOString(),
          endUtc: new Date(`${endDate}T${endTime}:00`).toISOString(),
          venue: venue || null,
          capacity: null,
          notes: null,
        }),
      });
      if (!response.ok) {
        let detail = "Enter a title and valid start/end times.";
        try {
          const body = await response.json();
          if (typeof body?.message === "string" && body.message.trim()) {
            detail = body.message.trim();
          }
        } catch {
          // Rejections can have empty or non-JSON bodies.
        }
        return setMessage(response.status >= 500
          ? `Event save could not be confirmed. Check upcoming events before retrying. ${detail}`
          : detail);
      }
      setTitle("");
      setStartDate("");
      setEndDate("");
      setVenue("");
      try {
        const workspace = await fetchWorkspace(academy.id);
        setEvents(workspace.events);
        setBranches(workspace.branches);
        setMessage("Event planned.");
      } catch {
        setMessage("Event planned. Upcoming events could not be refreshed; do not repeat the save. Refresh the page to check the saved event.");
      }
    } catch {
      setMessage("Event save could not be confirmed. Check upcoming events before retrying.");
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }
  return (
    <main className="enterprise-settings events-standard min-h-screen">
      <WorkspaceNav />
      <div className="events-content mx-auto max-w-6xl px-6 py-10">
        <header className="events-heading">
          <div className="events-title">
            <span className="events-title-icon" aria-hidden="true">
              ◷
            </span>
            <div>
              <p>Academy experience</p>
              <h1>Events</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state events-message" role="status" aria-live="polite">{message}</p>
        )}
        <section className="events-layout">
          <form onSubmit={create} className="events-panel">
            <header className="events-panel-header">
              <div>
                <p>Event planning</p>
                <h2>Plan event</h2>
              </div>
            </header>
            <div className="events-fields">
              <label>
                <span>Event title</span>
                <input
                  value={title}
                  disabled={saving}
                  onChange={(event) => setTitle(event.target.value)}
                  placeholder="Event title"
                  required
                />
              </label>
              <StandardSelectField
                name="event-type"
                disabled={saving}
                value={type}
                onChange={setType}
                placeholder="Event type"
                options={eventTypes.map((value) => ({ value, label: value }))}
              />
              <StandardSelectField
                name="branch"
                disabled={saving}
                value={branchId}
                onChange={setBranchId}
                placeholder="No branch"
                options={branches.map((branch) => ({
                  value: branch.id,
                  label: branch.name,
                }))}
              />
              <fieldset className="events-date-time m-0 min-w-0 border-0 p-0" disabled={saving}>
                <StandardDateField
                  name="start-date"
                  label="Start date"
                  value={startDate}
                  onChange={setStartDate}
                  required
                />
                <StandardTimeField
                  name="start-time"
                  label="Start time"
                  value={startTime}
                  onChange={setStartTime}
                />
              </fieldset>
              <fieldset className="events-date-time m-0 min-w-0 border-0 p-0" disabled={saving}>
                <StandardDateField
                  name="end-date"
                  label="End date"
                  value={endDate}
                  onChange={setEndDate}
                  required
                />
                <StandardTimeField
                  name="end-time"
                  label="End time"
                  value={endTime}
                  onChange={setEndTime}
                />
              </fieldset>
              <label>
                <span>Venue or meeting link</span>
                <input
                  value={venue}
                  disabled={saving}
                  onChange={(event) => setVenue(event.target.value)}
                  placeholder="Venue or meeting link"
                />
              </label>
              <button className="enterprise-action-button events-action" disabled={saving}>
                {saving ? "Saving…" : "Plan event"}
              </button>
            </div>
          </form>
          <section className="events-panel events-register">
            <header className="events-panel-header">
              <div>
                <p>Event planning</p>
                <h2>Upcoming events</h2>
              </div>
              <span>{events.length} events</span>
            </header>
            {events.length === 0 ? (
              <p className="events-empty">No events planned.</p>
            ) : (
              <ul>
                {events.map((item) => (
                  <li key={item.id}>
                    <div>
                      <b>{item.title}</b>
                      <small>
                        {item.type} ·{" "}
                        {new Intl.DateTimeFormat("en-IN", {
                          dateStyle: "medium",
                          timeStyle: "short",
                          timeZone: "Asia/Kolkata",
                        }).format(new Date(item.startUtc))}
                      </small>
                      <small>
                        {item.venue || "Venue not set"} · {item.status}
                      </small>
                    </div>
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
