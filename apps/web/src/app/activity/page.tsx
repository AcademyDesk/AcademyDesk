"use client";

import { useEffect, useMemo, useState } from "react";
import { academyApi } from "@/lib/api";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { activityMatches, activityTimestamp, validActivityRecord, type ActivityRecord } from "@/lib/activity-view";
export default function ActivityPage() {
  const [items, setItems] = useState<ActivityRecord[]>([]);
  const [message, setMessage] = useState("Loading activity…");
  const [loaded, setLoaded] = useState(false);
  const [failed, setFailed] = useState(false);
  const [clock, setClock] = useState(0);
  const [query, setQuery] = useState("");
  const [entity, setEntity] = useState("");
  const [period, setPeriod] = useState("");
  const [activityDate, setActivityDate] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    let active = true;
    async function load() {
      try {
        const academyResponse = await academyApi("/api/academies", {
          cache: "no-store", signal: controller.signal,
        });
        if (!academyResponse.ok) throw new Error();
        const academies: unknown = await academyResponse.json();
        if (!active) return;
        if (!Array.isArray(academies)) throw new Error();
        if (!academies.length) { setMessage("Create your academy first."); return; }
        const academy = academies[0];
        if (!academy || typeof academy.id !== "string" || !academy.id.trim()) throw new Error();
        const response = await academyApi(
          `/api/academies/${encodeURIComponent(academy.id)}/audit-logs`,
          { cache: "no-store", signal: controller.signal },
        );
        if (!response.ok) throw new Error();
        const records: unknown = await response.json();
        if (!active) return;
        if (!Array.isArray(records) || !records.every(validActivityRecord)) throw new Error();
        setItems(records);
        setClock(Date.now());
        setLoaded(true);
        setMessage("");
      } catch {
        if (!active) return;
        setFailed(true);
        setMessage(
          "Activity could not be loaded. Confirm the API is running and you are signed in.",
        );
      }
    }
    void load();
    return () => { active = false; controller.abort(); };
  }, []);
  useEffect(() => {
    // Snapshot rolling-window time outside render; no background API reads.
    const update = () => { if (document.visibilityState !== "hidden") setClock(Date.now()); };
    const timer = window.setInterval(update, 60000);
    window.addEventListener("focus", update);
    document.addEventListener("visibilitychange", update);
    return () => {
      window.clearInterval(timer);
      window.removeEventListener("focus", update);
      document.removeEventListener("visibilitychange", update);
    };
  }, []);
  const entityTypes = useMemo(
    () => [...new Set(items.map((item) => item.entityType))].sort(),
    [items],
  );
  const visible = useMemo(
    () =>
      items.filter(item => activityMatches(item, { query, entity, period, date: activityDate, now: clock })),
    [items, entity, period, query, activityDate, clock],
  );
  const label = (value: string) =>
    value.replace(/([a-z])([A-Z])/g, "$1 $2").replaceAll("_", " ");
  return (
    <main className="enterprise-settings workspace-activity">
      <header className="workspace-activity-heading">
        <div className="workspace-activity-title">
          <span className="workspace-activity-title-icon" aria-hidden="true">◌</span>
          <div><p>Workspace</p><h1>Activity log</h1><span>Track changes and operational actions across the academy.</span></div>
        </div>
      </header>
        {message && (
          <p role={failed ? "alert" : "status"} className="enterprise-page-state workspace-activity-message">
            {message}
          </p>
        )}
        <section className="workspace-activity-panel">
          <header className="workspace-activity-panel-header">
            <div><p>Audit trail</p><h2>Recent activity</h2></div>
            <span>{loaded ? `${visible.length} matching events` : "Activity unavailable"}</span>
          </header>
          <div className="workspace-activity-filters">
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search activity"
              aria-label="Search activity"
              disabled={!loaded}
              className="field"
            />
            <StandardSelectField
              name="activity-record-type"
              value={entity}
              onChange={setEntity}
              placeholder="All record types"
              label="Record type"
              disabled={!loaded}
              options={entityTypes.map((value) => ({ value, label: label(value) }))}
            />
            <StandardSelectField
              name="activity-period"
              value={period}
              onChange={value => { setPeriod(value); setClock(Date.now()); }}
              placeholder="All time"
              label="Time range"
              disabled={!loaded}
              options={[{ value: "Today", label: "Last 24 hours" }, { value: "7 days", label: "Last 7 days" }, { value: "30 days", label: "Last 30 days" }]}
            />
            <StandardDateField name="activity-date" value={activityDate} onChange={setActivityDate} label="Activity date (IST)" disabled={!loaded} />
            <button
              type="button"
              disabled={!loaded}
              onClick={() => {
                setQuery("");
                setEntity("");
                setPeriod("");
                setActivityDate("");
              }}
              className="workspace-activity-clear"
            >
              Clear filters
            </button>
          </div>
          {!loaded ? (
            <p className="workspace-activity-empty">Activity records are unavailable until the list loads.</p>
          ) : visible.length === 0 ? (
            <p className="workspace-activity-empty">No activity recorded for this view.</p>
          ) : (
            <ul className="workspace-activity-list">
              {visible.map((item) => (
                <li
                  key={item.id}
                >
                  <span className="workspace-activity-dot" aria-hidden="true" />
                  <div>
                    <strong>{label(item.action)}</strong>
                    <small>
                      {label(item.entityType)} ·{" "}
                    {new Intl.DateTimeFormat("en-IN", {
                      dateStyle: "medium",
                      timeStyle: "short",
                      timeZone: "Asia/Kolkata",
                    }).format(new Date(activityTimestamp(item.occurredAtUtc)))}
                    </small>
                    {item.metadataJson && (
                    <details>
                      <summary>
                        View details
                      </summary>
                      <pre>
                        {item.metadataJson}
                      </pre>
                    </details>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
    </main>
  );
}
