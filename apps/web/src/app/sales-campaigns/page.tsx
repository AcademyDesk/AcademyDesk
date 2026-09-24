"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import {
  StandardDateField,
  StandardSelectField,
} from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Campaign = {
  id: string;
  name: string;
  channel: string;
  startDate: string;
  endDate?: string | null;
  budget: number;
  status: string;
};
const statuses = ["Draft", "Active", "Paused", "Completed"];
const channels = [
  "WhatsApp",
  "Instagram",
  "Website",
  "Email",
  "Referral",
  "Other",
];

export default function SalesCampaignsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [campaigns, setCampaigns] = useState<Campaign[]>([]);
  const [message, setMessage] = useState("Loading campaigns…");
  const [name, setName] = useState("");
  const [channel, setChannel] = useState("WhatsApp");
  const [startDate, setStartDate] = useState(
    new Date().toLocaleDateString("en-CA", { timeZone: "Asia/Kolkata" }),
  );
  const [budget, setBudget] = useState("");
  const [status, setStatus] = useState("Draft");
  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const response = await academyApi(
      `/api/academies/${id}/sales-marketing/campaigns`,
      { cache: "no-store" },
    );
    if (!response.ok) throw new Error();
    setCampaigns(await response.json());
    setMessage("");
  }
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies", {
          cache: "no-store",
        });
        const academies: Academy[] = await response.json();
        if (!response.ok || !academies[0]) throw new Error();
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setMessage(
          "Campaigns could not be loaded. Please sign in and restart the API if needed.",
        );
      }
    })();
  }, []);
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/sales-marketing/campaigns`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          name,
          channel,
          startDate,
          budget: Number(budget || 0),
          status,
        }),
      },
    );
    if (!response.ok)
      return setMessage("Enter a campaign name and valid budget.");
    setName("");
    setBudget("");
    await load();
  }
  async function update(campaign: Campaign, nextStatus: string) {
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/sales-marketing/campaigns/${campaign.id}`,
      {
        method: "PATCH",
        headers: apiHeaders(true),
        body: JSON.stringify({
          status: nextStatus,
          budget: campaign.budget,
          endDate: campaign.endDate || null,
        }),
      },
    );
    if (!response.ok)
      return setMessage("Campaign status could not be updated.");
    await load();
  }
  return (
    <main className="enterprise-settings campaigns-standard min-h-screen">
      <WorkspaceNav />
      <div className="campaigns-content mx-auto max-w-6xl px-6 py-10">
        <header className="campaigns-heading">
          <div className="campaigns-title">
            <span className="campaigns-title-icon" aria-hidden="true">
              ◌
            </span>
            <div>
              <p>Sales &amp; marketing</p>
              <h1>Campaigns</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state campaigns-message">{message}</p>
        )}
        <section className="campaigns-layout">
          <form onSubmit={create} className="campaigns-panel">
            <header className="campaigns-panel-header">
              <div>
                <p>New campaign</p>
                <h2>Create campaign</h2>
              </div>
            </header>
            <div className="campaigns-fields">
              <label>
                <span>Campaign name</span>
                <input
                  required
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  placeholder="Campaign name"
                />
              </label>
              <div className="campaigns-fields-two">
                <label className="campaigns-control-field">
                  <span>Channel</span>
                  <StandardSelectField
                    name="channel"
                    value={channel}
                    onChange={setChannel}
                    placeholder="Channel"
                    options={channels.map((value) => ({ value, label: value }))}
                  />
                </label>
                <StandardDateField
                  name="start-date"
                  label="Start date"
                  value={startDate}
                  onChange={setStartDate}
                  required
                />
              </div>
              <div className="campaigns-fields-two">
                <label>
                  <span>Budget</span>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={budget}
                    onChange={(event) => setBudget(event.target.value)}
                    placeholder="₹ Amount"
                  />
                </label>
                <label className="campaigns-control-field">
                  <span>Status</span>
                  <StandardSelectField
                    name="status"
                    value={status}
                    onChange={setStatus}
                    placeholder="Status"
                    options={statuses.map((value) => ({ value, label: value }))}
                  />
                </label>
              </div>
              <button
                className="enterprise-action-button campaigns-create-button"
                disabled={!academy}
              >
                Create campaign
              </button>
            </div>
          </form>
          <section className="campaigns-panel campaigns-list-panel">
            <header className="campaigns-panel-header">
              <div>
                <p>Campaign register</p>
                <h2>Campaigns</h2>
              </div>
              <span>{campaigns.length} total</span>
            </header>
            {campaigns.length === 0 ? (
              <p className="campaigns-empty">No campaigns yet.</p>
            ) : (
              <ul>
                {campaigns.map((campaign) => (
                  <li key={campaign.id}>
                    <div>
                      <b>{campaign.name}</b>
                      <small>
                        {campaign.channel} · Starts{" "}
                        {new Intl.DateTimeFormat("en-IN", {
                          dateStyle: "medium",
                          timeZone: "Asia/Kolkata",
                        }).format(
                          new Date(`${campaign.startDate}T00:00:00+05:30`),
                        )}{" "}
                        · ₹{campaign.budget.toLocaleString("en-IN")}
                      </small>
                    </div>
                    <StandardSelectField
                      name={`campaign-status-${campaign.id}`}
                      value={campaign.status}
                      onChange={(nextStatus) =>
                        void update(campaign, nextStatus)
                      }
                      placeholder="Status"
                      options={statuses.map((value) => ({
                        value,
                        label: value,
                      }))}
                    />
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
