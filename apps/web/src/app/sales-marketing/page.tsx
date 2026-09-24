"use client";

import Link from "next/link";
import { Suspense, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi } from "@/lib/api";

type Academy = { id: string };
type Lead = {
  id: string;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  source: string;
  stage: string;
  followUpAtUtc?: string | null;
  convertedStudentId?: string | null;
};
type View = "overview" | "sources" | "follow-ups" | "conversion" | "referrals";
const titles: Record<View, string> = {
  overview: "Sales Overview",
  sources: "Lead Sources",
  "follow-ups": "Follow-ups",
  conversion: "Conversion Dashboard",
  referrals: "Referral Tracking",
};

function SalesMarketingContent() {
  const [leads, setLeads] = useState<Lead[]>([]);
  const [message, setMessage] = useState("Loading sales data…");
  const searchParams = useSearchParams();
  const candidate = searchParams.get("view") as View | null;
  const view: View = candidate && candidate in titles ? candidate : "overview";

  useEffect(() => {
    void (async () => {
      try {
        const academyResponse = await academyApi("/api/academies", {
          cache: "no-store",
        });
        const academies: Academy[] = await academyResponse.json();
        if (!academyResponse.ok || !academies[0]) throw new Error();
        const leadResponse = await academyApi(
          `/api/academies/${academies[0].id}/leads`,
          { cache: "no-store" },
        );
        if (!leadResponse.ok) throw new Error();
        setLeads(await leadResponse.json());
        setMessage("");
      } catch {
        setMessage(
          "Sales data could not be loaded. Please sign in and restart the API if needed.",
        );
      }
    })();
  }, []);
  const sources = useMemo(
    () =>
      Object.entries(
        leads.reduce<Record<string, number>>(
          (result, lead) => ({
            ...result,
            [lead.source || "Not set"]:
              (result[lead.source || "Not set"] ?? 0) + 1,
          }),
          {},
        ),
      ).sort((a, b) => b[1] - a[1]),
    [leads],
  );
  const now = new Date();
  const due = leads
    .filter((lead) => lead.followUpAtUtc && new Date(lead.followUpAtUtc) <= now)
    .sort(
      (a, b) =>
        new Date(a.followUpAtUtc!).getTime() -
        new Date(b.followUpAtUtc!).getTime(),
    );
  const referrals = leads.filter((lead) => lead.source === "Referral");
  const converted = leads.filter(
    (lead) => lead.convertedStudentId || lead.stage === "Converted",
  );
  const trialBooked = leads.filter((lead) => lead.stage === "TrialBooked");
  const contact = (lead: Lead) =>
    lead.email || lead.phone || "No contact details";
  const leadTiles = [
    ["Leads", leads.length, "/leads"],
    ["Follow-ups due", due.length, "/sales-marketing?view=follow-ups"],
    ["Trial bookings", trialBooked.length, "/trial-bookings"],
    ["Converted", converted.length, "/sales-marketing?view=conversion"],
  ];

  const leadCards = (
    items: Lead[],
    detail: (lead: Lead) => string,
    empty: string,
  ) =>
    items.length ? (
      <div className="sales-record-list">
        {items.map((lead) => (
          <Link key={lead.id} href="/leads">
            <b>{lead.fullName}</b>
            <small>{detail(lead)}</small>
          </Link>
        ))}
      </div>
    ) : (
      <p className="sales-empty">{empty}</p>
    );
  const sourceCards = (empty: string) =>
    sources.length ? (
      <div className="sales-source-grid">
        {sources.map(([source, count]) => (
          <Link key={source} href="/leads">
            <b>{source}</b>
            <span>
              {count} lead{count === 1 ? "" : "s"}
            </span>
          </Link>
        ))}
      </div>
    ) : (
      <p className="sales-empty">{empty}</p>
    );

  return (
    <main className="enterprise-settings sales-standard min-h-screen">
      <WorkspaceNav />
      <div className="sales-content mx-auto max-w-6xl px-6 py-10">
        <header className="sales-heading">
          <div className="sales-title">
            <span className="sales-title-icon" aria-hidden="true">
              ◌
            </span>
            <div>
              <p>Sales &amp; marketing</p>
              <h1>{titles[view]}</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state sales-message">{message}</p>
        )}
        {view === "overview" && (
          <>
            <section className="sales-tile-grid">
              {leadTiles.map(([name, value, href]) => (
                <Link
                  key={String(name)}
                  href={String(href)}
                  className="sales-tile"
                >
                  <span>{name}</span>
                  <strong>{value}</strong>
                </Link>
              ))}
            </section>
            <section className="sales-panel sales-sources-panel">
              <header className="sales-panel-header">
                <div>
                  <p>Pipeline</p>
                  <h2>Lead sources and referrals</h2>
                </div>
                <Link href="/sales-marketing?view=sources">View sources</Link>
              </header>
              {sourceCards("No leads recorded yet.")}
            </section>
          </>
        )}
        {view === "sources" && (
          <section className="sales-panel sales-full-panel">
            <header className="sales-panel-header">
              <div>
                <p>Pipeline</p>
                <h2>Lead sources</h2>
              </div>
            </header>
            {sourceCards("No sources to report yet.")}
          </section>
        )}
        {view === "follow-ups" && (
          <section className="sales-panel sales-full-panel">
            <header className="sales-panel-header">
              <div>
                <p>Pipeline</p>
                <h2>Follow-ups due</h2>
              </div>
            </header>
            {leadCards(
              due,
              (lead) =>
                `Due ${new Intl.DateTimeFormat("en-IN", { dateStyle: "medium", timeStyle: "short", timeZone: "Asia/Kolkata" }).format(new Date(lead.followUpAtUtc!))} IST · ${contact(lead)}`,
              "No follow-ups are due.",
            )}
          </section>
        )}
        {view === "conversion" && (
          <>
            <section className="sales-tile-grid sales-tile-grid-three">
              {[
                ["Total leads", leads.length],
                ["Converted", converted.length],
                [
                  "Conversion rate",
                  leads.length
                    ? `${Math.round((converted.length / leads.length) * 100)}%`
                    : "0%",
                ],
              ].map(([name, value]) => (
                <div key={String(name)} className="sales-tile">
                  <span>{name}</span>
                  <strong>{value}</strong>
                </div>
              ))}
            </section>
            <section className="sales-panel sales-full-panel">
              <header className="sales-panel-header">
                <div>
                  <p>Pipeline</p>
                  <h2>Conversion readiness</h2>
                </div>
                <Link href="/trial-bookings">Open trial bookings</Link>
              </header>
              <p className="sales-copy">
                {trialBooked.length} lead
                {trialBooked.length === 1 ? " is" : "s are"} currently booked
                for a trial class.
              </p>
            </section>
          </>
        )}
        {view === "referrals" && (
          <section className="sales-panel sales-full-panel">
            <header className="sales-panel-header">
              <div>
                <p>Pipeline</p>
                <h2>Referral leads</h2>
              </div>
            </header>
            {leadCards(
              referrals,
              (lead) => `${lead.stage} · ${contact(lead)}`,
              "No referral leads recorded yet.",
            )}
          </section>
        )}
      </div>
    </main>
  );
}

export default function SalesMarketingPage() {
  return (
    <Suspense
      fallback={
        <main className="enterprise-settings sales-standard">
          <p className="enterprise-page-state">Loading sales data…</p>
        </main>
      }
    >
      <SalesMarketingContent />
    </Suspense>
  );
}
