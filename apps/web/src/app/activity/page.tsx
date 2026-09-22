"use client";

import { useEffect, useMemo, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
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
    <main className="enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100">
      <WorkspaceNav />
      <div className="mx-auto max-w-4xl px-6 py-10">
        <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">
          Administration
        </p>
        <h1 className="mt-3 text-4xl font-semibold tracking-tight">
          Activity log
        </h1>
        {message && (
          <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">
            {message}
          </p>
        )}
        <section className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-6">
          <div className="grid gap-3 md:grid-cols-[1fr_180px_150px]">
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
          <div className="mt-4 flex items-center justify-between text-sm text-slate-400">
            <span>{visible.length} matching events</span>
            <button
              onClick={() => {
                setQuery("");
                setEntity("All");
                setPeriod("All");
              }}
              className="text-cyan-300"
            >
              Clear filters
            </button>
          </div>
          {visible.length === 0 ? (
            <p className="text-slate-400">No activity recorded yet.</p>
          ) : (
            <ul className="space-y-3">
              {visible.map((item) => (
                <li
                  key={item.id}
                  className="rounded-lg border border-slate-700 bg-slate-950 p-4"
                >
                  <div className="font-medium">{label(item.action)}</div>
                  <div className="mt-1 text-sm text-slate-400">
                    {label(item.entityType)} ·{" "}
                    {new Intl.DateTimeFormat("en-IN", {
                      dateStyle: "medium",
                      timeStyle: "short",
                      timeZone: "Asia/Kolkata",
                    }).format(new Date(item.occurredAtUtc))}
                  </div>
                  {item.metadataJson && (
                    <details className="mt-2 text-xs text-slate-500">
                      <summary className="cursor-pointer text-slate-400">
                        View details
                      </summary>
                      <pre className="mt-2 overflow-auto whitespace-pre-wrap">
                        {item.metadataJson}
                      </pre>
                    </details>
                  )}
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </main>
  );
}
