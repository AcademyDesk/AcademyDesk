"use client";

import { StandardSelectField } from "@/components/design-system/controls";

export type MeetingProviderDraft = { provider: string; status: string; organizerName: string; organizerEmail: string; reference: string };

const details: Record<string, { reference: string; description: string }> = {
  GoogleWorkspace: { reference: "Google OAuth client ID or Calendar ID", description: "Google Calendar creates the event and Google Meet creates the class link." },
  Zoom: { reference: "Zoom OAuth client ID or account ID", description: "Zoom OAuth or server-to-server OAuth creates scheduled and recurring class meetings." },
  Microsoft365: { reference: "Microsoft Entra tenant ID or application client ID", description: "Microsoft Graph creates Outlook events and Microsoft Teams meeting links." },
};

export function MeetingProviderSettings({ value, onChange, onSave, disabled }: { value: MeetingProviderDraft; onChange: (value: MeetingProviderDraft) => void; onSave: () => void; disabled: boolean }) {
  const current = details[value.provider] ?? details.GoogleWorkspace;
  return <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6 lg:col-span-2"><h2 className="text-xl font-semibold">Meeting provider</h2><p className="mt-2 text-sm text-slate-400">Set up the service used for online and hybrid class links.</p><div className="mt-5 grid gap-3 md:grid-cols-2"><StandardSelectField name="meeting-provider" value={value.provider} onChange={(provider) => onChange({ ...value, provider })} placeholder="Meeting provider" options={[{ value: "GoogleWorkspace", label: "Google Workspace · Meet" }, { value: "Zoom", label: "Zoom" }, { value: "Microsoft365", label: "Microsoft 365 · Teams" }]} /><StandardSelectField name="meeting-status" value={value.status} onChange={(status) => onChange({ ...value, status })} placeholder="Connection status" options={[{ value: "NotConfigured", label: "Not configured" }, { value: "Configured", label: "Ready to connect" }, { value: "Disabled", label: "Disabled" }]} /><input value={value.organizerName} onChange={(event) => onChange({ ...value, organizerName: event.target.value })} placeholder="Organizer name" /><input type="email" value={value.organizerEmail} onChange={(event) => onChange({ ...value, organizerEmail: event.target.value })} placeholder="Organizer email" required /><input className="md:col-span-2" value={value.reference} onChange={(event) => onChange({ ...value, reference: event.target.value })} placeholder={current.reference} required /></div><p className="mt-4 text-sm text-slate-400">{current.description}</p><p className="mt-2 text-xs text-slate-500">Client secrets are never entered here. They are supplied through a secure OAuth connection, not stored in normal settings.</p><button type="button" disabled={disabled} onClick={onSave} className="mt-5 rounded-lg bg-cyan-400 px-5 py-2.5 font-semibold text-slate-950 disabled:opacity-60">Save meeting provider</button></section>;
}
