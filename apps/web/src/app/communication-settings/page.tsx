"use client";
import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
import { MeetingProviderDraft, MeetingProviderSettings } from "@/components/meeting-provider-settings";

type Academy = { id: string };
type ChannelName = "Email" | "WhatsApp" | "Meeting";
type Channel = { channel: ChannelName; provider: string; status: string; senderName: string | null; senderAddress: string | null; replyToAddress: string | null; phoneNumber: string | null; externalAccountReference: string | null; messagesEnabled: boolean; hasSecureConnection: boolean };
type Draft = { provider: string; status: string; senderName: string; senderAddress: string; replyToAddress: string; phoneNumber: string; externalAccountReference: string; messagesEnabled: boolean };
type ChannelPayload = Omit<Draft, "replyToAddress" | "phoneNumber"> & { replyToAddress: string | null; phoneNumber: string | null };
const fresh = (provider: string): Draft => ({ provider, status: "NotConfigured", senderName: "", senderAddress: "", replyToAddress: "", phoneNumber: "", externalAccountReference: "", messagesEnabled: false });
const toDraft = (row: Channel | undefined, provider: string): Draft => row ? ({ provider: row.provider, status: row.status, senderName: row.senderName ?? "", senderAddress: row.senderAddress ?? "", replyToAddress: row.replyToAddress ?? "", phoneNumber: row.phoneNumber ?? "", externalAccountReference: row.externalAccountReference ?? "", messagesEnabled: row.messagesEnabled }) : fresh(provider);
function isChannel(value: unknown): value is Channel {
  if (!value || typeof value !== "object" || Array.isArray(value)) return false;
  const row = value as Partial<Channel>;
  return ["Email", "WhatsApp", "Meeting"].includes(row.channel ?? "") && typeof row.provider === "string" && typeof row.status === "string" && typeof row.messagesEnabled === "boolean" && typeof row.hasSecureConnection === "boolean" &&
    [row.senderName, row.senderAddress, row.replyToAddress, row.phoneNumber, row.externalAccountReference].every((field) => field === null || typeof field === "string");
}
const meetingBlank: MeetingProviderDraft = { provider: "GoogleWorkspace", status: "NotConfigured", organizerName: "", organizerEmail: "", reference: "" };
const toMeetingDraft = (row: Channel): MeetingProviderDraft => ({ provider: row.provider, status: row.status, organizerName: row.senderName ?? "", organizerEmail: row.senderAddress ?? "", reference: row.externalAccountReference ?? "" });
// Normalize only fields still equal to the submitted snapshot; never erase newer edits.
function reconcileDraft<T extends object>(current: T, submitted: T, saved: T): T {
  const next = { ...saved };
  for (const key of Object.keys(current) as (keyof T)[]) if (current[key] !== submitted[key]) next[key] = current[key];
  return next;
}

export default function CommunicationSettingsPage() {
  const [academy, setAcademy] = useState<Academy>(); const [items, setItems] = useState<Channel[]>([]); const [email, setEmail] = useState(fresh("GoogleWorkspace")); const [whatsApp, setWhatsApp] = useState(fresh("MetaCloudApi")); const [meeting, setMeeting] = useState(meetingBlank); const [message, setMessage] = useState("Loading channel settings…");
  const [loaded, setLoaded] = useState(false);
  const [notices, setNotices] = useState<Partial<Record<ChannelName, string>>>({});
  const [pending, setPending] = useState<Record<ChannelName, boolean>>({ Email: false, WhatsApp: false, Meeting: false });
  const pendingChannels = useRef(new Set<ChannelName>());
  const revisions = useRef<Record<ChannelName, number>>({ Email: 0, WhatsApp: 0, Meeting: 0 });
  const notice = (channel: ChannelName, value: string) => setNotices((current) => ({ ...current, [channel]: value }));
  const editSender = (channel: "Email" | "WhatsApp", value: Draft) => { revisions.current[channel]++; if (channel === "Email") setEmail(value); else setWhatsApp(value); };
  const editMeeting = (value: MeetingProviderDraft) => { revisions.current.Meeting++; setMeeting(value); };
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        const academies: Academy[] = await response.json();
        if (!response.ok || !academies[0]) throw new Error();
        setAcademy(academies[0]);
        const settingsResponse = await academyApi(`/api/academies/${academies[0].id}/communication-settings`, { cache: "no-store" });
        if (!settingsResponse.ok) throw new Error("Channel settings could not be loaded.");
        const rows: unknown = await settingsResponse.json();
        if (!Array.isArray(rows) || !rows.every(isChannel) || new Set(rows.map((row) => row.channel)).size !== rows.length)
          throw new Error("Channel settings response is incomplete. Reload before saving.");
        setItems(rows);
        setEmail(toDraft(rows.find((row) => row.channel === "Email"), "GoogleWorkspace"));
        setWhatsApp(toDraft(rows.find((row) => row.channel === "WhatsApp"), "MetaCloudApi"));
        const row = rows.find((item) => item.channel === "Meeting");
        setMeeting(row ? toMeetingDraft(row) : meetingBlank);
        setLoaded(true);
        setMessage("");
      } catch {
        setMessage("Settings could not be loaded. Confirm the API is running on port 5092.");
      }
    })();
  }, []);
  async function persist(channel: ChannelName, payload: ChannelPayload, applySaved: (row: Channel) => void) {
    if (!academy || !loaded) return notice(channel, "Load complete channel settings before saving. Your entries are kept.");
    if (pendingChannels.current.has(channel)) return;
    pendingChannels.current.add(channel);
    const revision = revisions.current[channel];
    setPending((current) => ({ ...current, [channel]: true }));
    notice(channel, `Saving ${channel} settings…`);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/communication-settings/${channel}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify(payload) });
      const result: unknown = await response.json().catch(() => null);
      if (!response.ok) {
        const detail = result && typeof result === "object" && "message" in result && typeof result.message === "string" ? result.message : "Check the details and try again.";
        return notice(channel, response.status >= 500 ? `${channel} save could not be confirmed. Your entries are kept; check saved settings before retrying.` : `${channel} settings could not be saved. ${detail} Your entries are kept.`);
      }
      if (!isChannel(result) || result.channel !== channel) {
        setLoaded(false);
        return notice(channel, `${channel} save could not be confirmed because the response was incomplete. Your entries are kept. Reload to check saved settings before saving again.`);
      }
      setItems((current) => current.filter((item) => item.channel !== channel).concat(result));
      applySaved(result);
      notice(channel, `${channel} settings saved.${revisions.current[channel] !== revision ? " Newer edits made while saving are not yet saved." : ""}`);
    } catch {
      notice(channel, `${channel} save could not be confirmed. Your entries are kept; check saved settings before retrying.`);
    } finally {
      pendingChannels.current.delete(channel);
      setPending((current) => ({ ...current, [channel]: false }));
    }
  }
  async function save(channel: "Email" | "WhatsApp", submitted: Draft) {
    await persist(channel, submitted, (row) => {
      const saved = toDraft(row, submitted.provider);
      if (channel === "Email") setEmail((current) => reconcileDraft(current, submitted, saved));
      else setWhatsApp((current) => reconcileDraft(current, submitted, saved));
    });
  }
  async function saveMeeting() {
    const submitted = meeting;
    const saved = items.find((item) => item.channel === "Meeting");
    await persist("Meeting", { provider: submitted.provider, status: submitted.status, senderName: submitted.organizerName, senderAddress: submitted.organizerEmail, replyToAddress: saved?.replyToAddress ?? null, phoneNumber: saved?.phoneNumber ?? null, externalAccountReference: submitted.reference, messagesEnabled: saved?.messagesEnabled ?? false },
      (row) => setMeeting((current) => reconcileDraft(current, submitted, toMeetingDraft(row))));
  }
  const fields = "w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2";
  const senderForm = (channel: "Email" | "WhatsApp", draft: Draft) => <form onSubmit={(event: FormEvent) => { event.preventDefault(); void save(channel, draft); }} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">{channel === "Email" ? "Email sender" : "WhatsApp sender"}</h2><div className="mt-5 grid gap-3"><select value={draft.provider} onChange={(event) => editSender(channel, { ...draft, provider: event.target.value })} className={fields}>{channel === "Email" ? <><option value="GoogleWorkspace">Google Workspace</option><option value="Microsoft365">Microsoft 365</option><option value="SMTP">Custom SMTP</option></> : <><option value="MetaCloudApi">Meta WhatsApp Cloud API</option><option value="BSP">Business solution provider</option></>}</select><input value={draft.senderName} onChange={(event) => editSender(channel, { ...draft, senderName: event.target.value })} placeholder="Display name" className={fields} />{channel === "Email" ? <input type="email" value={draft.senderAddress} onChange={(event) => editSender(channel, { ...draft, senderAddress: event.target.value })} placeholder="Sender email" className={fields} /> : <input value={draft.phoneNumber} onChange={(event) => editSender(channel, { ...draft, phoneNumber: event.target.value })} placeholder="Dedicated WhatsApp number" className={fields} />}</div><button disabled={!academy || !loaded || pending[channel]} className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 disabled:opacity-60">{pending[channel] ? "Saving…" : `Save ${channel.toLowerCase()} settings`}</button>{notices[channel] && <p role="status" aria-live="polite" data-channel-notice={channel} className="mt-3 text-sm">{notices[channel]}</p>}</form>;
  return <main className="enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="mx-auto max-w-6xl px-6 py-10"><p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">Engagement</p><h1 className="mt-3 text-4xl font-semibold tracking-tight">Channel settings</h1><p className="mt-3 max-w-3xl text-slate-300">Configure academy communication channels and the provider that creates online class meetings.</p>{message && <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">{message}</p>}<div className="mt-8 grid gap-6 lg:grid-cols-2">{senderForm("Email", email)}{senderForm("WhatsApp", whatsApp)}<div className="lg:col-span-2"><MeetingProviderSettings value={meeting} onChange={editMeeting} onSave={() => void saveMeeting()} disabled={!academy || !loaded || pending.Meeting} />{notices.Meeting && <p role="status" aria-live="polite" data-channel-notice="Meeting" className="mt-3 text-sm">{notices.Meeting}</p>}</div></div><section className="mt-6 rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Secure connection</h2><p className="mt-2 text-sm text-slate-300">{items.some((item) => item.hasSecureConnection) ? "A secure provider connection is recorded." : "Save the provider information first. OAuth client secrets and access tokens are connected securely in the next step."}</p></section></div></main>;
}
