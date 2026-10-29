"use client";

import { StandardSelectField } from "@/components/design-system/controls";
import visual from "@/components/design-system/engagement-workspace.module.css";

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
      "Google Calendar creates the event and Google Meet creates the class link.",
  },
  Zoom: {
    reference: "Zoom OAuth client ID or account ID",
    description:
      "Zoom OAuth or server-to-server OAuth creates scheduled and recurring class meetings.",
  },
  Microsoft365: {
    reference: "Microsoft Entra tenant ID or application client ID",
    description:
      "Microsoft Graph creates Outlook events and Microsoft Teams meeting links.",
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
    <section className={visual.panel}>
      <header className={visual.panelHeader}><p>Online classes</p><h2>Meeting provider</h2></header>
      <div className={visual.meetingFields}>
        <div>
          <StandardSelectField
            label="Provider"
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
        </div>
        <div>
          <StandardSelectField
            label="Connection status"
            name="meeting-status"
            value={value.status}
            onChange={(status) => onChange({ ...value, status })}
            placeholder="Connection status"
            options={[
              { value: "NotConfigured", label: "Not configured" },
              { value: "Configured", label: "Ready to connect" },
              { value: "Disabled", label: "Disabled" },
            ]}
          />
        </div>
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
        <label className={visual.wide}>
          Connection reference
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
      <p className={visual.help}>This form stores one provider draft, not three connected accounts. {current.description} Saving these details alone does not establish an authenticated connection.</p>
      <button
        type="button"
        disabled={disabled}
        onClick={onSave}
        className={`enterprise-action-button ${visual.meetingSave}`}
      >
        Save meeting provider
      </button>
    </section>
  );
}
