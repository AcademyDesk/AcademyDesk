"use client";

import { StandardSelectField } from "@/components/design-system/controls";

export type MeetingProviderDraft = {
  provider: string;
  status: string;
  organizerName: string;
  organizerEmail: string;
  reference: string;
};

const details: Record<string, { reference: string; description: string }> = {
  GoogleWorkspace: {
    reference: "Google OAuth client ID or Calendar ID",
    description:
      "Provider details for Google Calendar and Google Meet.",
  },
  Zoom: {
    reference: "Zoom OAuth client ID or account ID",
    description:
      "Provider details for Zoom meeting integration.",
  },
  Microsoft365: {
    reference: "Microsoft Entra tenant ID or application client ID",
    description:
      "Provider details for Microsoft Outlook and Teams integration.",
  },
};

export function MeetingProviderSettings({
  value,
  onChange,
  onSave,
  disabled,
}: {
  value: MeetingProviderDraft;
  onChange: (value: MeetingProviderDraft) => void;
  onSave: () => void;
  disabled: boolean;
}) {
  const current = details[value.provider] ?? details.GoogleWorkspace;
  return (
    <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6 lg:col-span-2">
      <h2 className="text-xl font-semibold">Meeting provider</h2>
      <div className="mt-5 grid gap-3 md:grid-cols-2">
        <label>
          Provider
          <StandardSelectField
            name="meeting-provider"
            value={value.provider}
            onChange={(provider) => onChange({ ...value, provider })}
            placeholder="Meeting provider"
            options={[
              { value: "GoogleWorkspace", label: "Google Workspace · Meet" },
              { value: "Zoom", label: "Zoom" },
              { value: "Microsoft365", label: "Microsoft 365 · Teams" },
            ]}
          />
        </label>
        <label>
          Configuration status
          <StandardSelectField
            name="meeting-status"
            value={value.status}
            onChange={(status) => onChange({ ...value, status })}
            placeholder="Configuration status"
            options={[
              { value: "NotConfigured", label: "Not configured" },
              { value: "Configured", label: "Details configured" },
              { value: "Disabled", label: "Disabled" },
            ]}
          />
        </label>
        <label>
          Organizer name
          <input
            value={value.organizerName}
            onChange={(event) =>
              onChange({ ...value, organizerName: event.target.value })
            }
          />
        </label>
        <label>
          Organizer email
          <input
            type="email"
            value={value.organizerEmail}
            onChange={(event) =>
              onChange({ ...value, organizerEmail: event.target.value })
            }
            required
          />
        </label>
        <label className="md:col-span-2">
          Provider reference
          <input
            value={value.reference}
            onChange={(event) =>
              onChange({ ...value, reference: event.target.value })
            }
            placeholder={current.reference}
            required
          />
        </label>
      </div>
      <p className="mt-4 text-sm text-slate-400">{current.description} Saving these details does not connect a provider. Secure connection setup and meeting creation are unavailable in this version.</p>
      <button
        type="button"
        disabled={disabled}
        onClick={onSave}
        className="mt-5 rounded-lg bg-cyan-400 px-5 py-2.5 font-semibold text-slate-950 disabled:opacity-60"
      >
        Save meeting provider
      </button>
    </section>
  );
}
