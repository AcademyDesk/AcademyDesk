"use client";

import { useEffect, useState } from "react";
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
  return (
    <main className="min-h-screen bg-slate-950 text-slate-100">
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
          {items.length === 0 ? (
            <p className="text-slate-400">No activity recorded yet.</p>
          ) : (
            <ul className="space-y-3">
              {items.map((item) => (
                <li
                  key={item.id}
                  className="rounded-lg border border-slate-700 bg-slate-950 p-4"
                >
                  <div className="font-medium">{item.action}</div>
                  <div className="mt-1 text-sm text-slate-400">
                    {item.entityType} ·{" "}
                    {new Intl.DateTimeFormat("en-IN", {
                      dateStyle: "medium",
                      timeStyle: "short",
                    }).format(new Date(item.occurredAtUtc))}
                  </div>
                  {item.metadataJson && (
                    <div className="mt-2 break-all text-xs text-slate-500">
                      {item.metadataJson}
                    </div>
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
