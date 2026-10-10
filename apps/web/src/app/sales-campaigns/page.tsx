"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
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

async function fetchCampaigns(academyId: string): Promise<Campaign[]> {
  const response = await academyApi(
    `/api/academies/${academyId}/sales-marketing/campaigns`,
    { cache: "no-store" },
  );
  if (!response.ok) throw new Error();
  const data: unknown = await response.json();
  if (!Array.isArray(data)) throw new Error();
  return data;
}

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
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) throw new Error();
        const data = await fetchCampaigns(academies[0].id);
        if (!active) return;
        setAcademy(academies[0]);
        setCampaigns(data);
        setMessage("");
      } catch {
        if (!active) return;
        setMessage(
          "Campaigns could not be loaded. Please refresh or contact your administrator.",
        );
      }
    })();
    return () => { active = false; };
  }, []);

  async function mutate(
    action: () => Promise<Response>,
    success: string,
    failure: string,
    reset?: () => void,
  ) {
    if (!academy || pending.current) return;
    pending.current = true;
    setSaving(true);
    setMessage("");
    const uncertain = "The campaign change could not be confirmed. Your draft has been retained. Check the campaign register before retrying.";
    try {
      const response = await action();
      if (!response.ok) {
        setMessage(response.status >= 500 ? uncertain : failure);
        return;
      }
      reset?.();
      setMessage(success);
      try {
        setCampaigns(await fetchCampaigns(academy.id));
      } catch {
        setMessage(`${success} The campaign register could not be refreshed; do not repeat the action. Refresh the page to see the latest data.`);
      }
    } catch {
      setMessage(uncertain);
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    await mutate(() => academyApi(
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
    ), "Campaign created.", "Campaign could not be created. Enter a campaign name and valid budget. Your draft has been retained.", () => {
      setName("");
      setBudget("");
    });
  }
  async function update(campaign: Campaign, nextStatus: string) {
    if (!academy) return;
    await mutate(() => academyApi(
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
    ), `${campaign.name} moved to ${nextStatus}.`, "Campaign status could not be updated. Your draft has been retained.");
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
          <p role="status" aria-live="polite" className="enterprise-page-state campaigns-message">{message}</p>
        )}
        <section className="campaigns-layout">
          <form onSubmit={create} className="campaigns-panel">
            <header className="campaigns-panel-header">
              <div>
                <p>New campaign</p>
                <h2>Create campaign</h2>
              </div>
            </header>
            <fieldset className="campaigns-fields" disabled={!academy || saving} aria-busy={saving} style={{ border: 0, margin: 0, minWidth: 0 }}>
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
                    disabled={!academy || saving}
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
                    disabled={!academy || saving}
                    value={status}
                    onChange={setStatus}
                    placeholder="Status"
                    options={statuses.map((value) => ({ value, label: value }))}
                  />
                </label>
              </div>
              <button
                className="enterprise-action-button campaigns-create-button"
                disabled={!academy || saving}
              >
                {saving ? "Saving…" : "Create campaign"}
              </button>
            </fieldset>
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
                      disabled={!academy || saving}
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
