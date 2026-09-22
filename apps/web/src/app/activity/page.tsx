"use client";

import { useEffect, useMemo, useState } from "react";
import { academyApi } from "@/lib/api";
type Academy = { id: string };
type Audit = {
  id: string;
  action: string;
  entityType: string;
  metadataJson?: string | null;
  occurredAtUtc: string;
};
export default function ActivityPage() {
  const [items, setItems] = useState<Audit[]>([]);
  const [message, setMessage] = useState("Loading activity…");
  const [query, setQuery] = useState("");
  const [entity, setEntity] = useState("All");
  const [period, setPeriod] = useState("All");
  useEffect(() => {
    async function load() {
      try {
        const academyResponse = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (!academyResponse.ok) throw new Error();
        const academies: Academy[] = await academyResponse.json();
        if (!academies[0]) return setMessage("Create your academy first.");
        const response = await academyApi(
          `/api/academies/${academies[0].id}/audit-logs`,
          { cache: "no-store" },
        );
        if (!response.ok) throw new Error();
        setItems(await response.json());
        setMessage("");
      } catch {
        setMessage(
          "Activity could not be loaded. Confirm the API is running and you are signed in.",
        );
      }
    }
    void load();
  }, []);
  const entityTypes = useMemo(
    () => [...new Set(items.map((item) => item.entityType))].sort(),
    [items],
  );
  const visible = useMemo(
    () =>
      items.filter((item) => {
        const searchable =
          `${item.action} ${item.entityType} ${item.metadataJson ?? ""}`.toLowerCase();
        const age = Date.now() - new Date(item.occurredAtUtc).getTime();
        const periodMatch =
          period === "All" ||
          (period === "Today" && age <= 86400000) ||
          (period === "7 days" && age <= 604800000) ||
          (period === "30 days" && age <= 2592000000);
        return (
          (entity === "All" || item.entityType === entity) &&
          periodMatch &&
          searchable.includes(query.toLowerCase())
        );
      }),
    [items, entity, period, query],
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
          <p className="enterprise-page-state enterprise-page-state-loading workspace-activity-message">
            {message}
          </p>
        )}
        <section className="workspace-activity-panel">
          <header className="workspace-activity-panel-header">
            <div><p>Audit trail</p><h2>Recent activity</h2></div>
            <span>{visible.length} matching events</span>
          </header>
          <div className="workspace-activity-filters">
            <input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search activity"
              className="field"
            />
            <select
              value={entity}
              onChange={(event) => setEntity(event.target.value)}
              className="field"
            >
              <option>All</option>
              {entityTypes.map((value) => (
                <option key={value}>{value}</option>
              ))}
            </select>
            <select
              value={period}
              onChange={(event) => setPeriod(event.target.value)}
              className="field"
            >
              <option>All</option>
              <option>Today</option>
              <option>7 days</option>
              <option>30 days</option>
            </select>
          </div>
          <div className="workspace-activity-filter-summary">
            <span>Use search, record type, or period to narrow the audit trail.</span>
            <button
              onClick={() => {
                setQuery("");
                setEntity("All");
                setPeriod("All");
              }}
              className="workspace-activity-clear"
            >
              Clear filters
            </button>
          </div>
          {visible.length === 0 ? (
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
                    }).format(new Date(item.occurredAtUtc))}
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
