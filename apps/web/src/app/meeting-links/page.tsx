"use client";
import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  StandardDateField,
  StandardSelectField,
  StandardTimeField,
} from "@/components/design-system/controls";
import { academyApi } from "@/lib/api";
type Academy = { id: string; timeZone?: string };
type Person = {
  id: string;
  firstName?: string;
  lastName?: string;
  email?: string;
};
type Batch = {
  id: string;
  name: string;
  deliveryMode?: string;
  meetingLink?: string;
};
type Enrollment = { studentId: string; batchId: string; status: string };
type Channel = {
  channel: string;
  provider: string;
  hasSecureConnection: boolean;
};
type Meeting = {
  id: string;
  title: string;
  provider: string;
  createdAt: string;
  attendeeEmails: string[];
  start: string;
  joinUrl?: string;
  status: string;
};
type Provider = "GoogleWorkspace" | "Zoom" | "Microsoft365";
const providerName: Record<Provider, string> = {
  GoogleWorkspace: "Google Meet",
  Zoom: "Zoom",
  Microsoft365: "Microsoft Teams",
};
const name = (p: Person) =>
  `${p.firstName ?? ""} ${p.lastName ?? ""}`.trim() || "Unnamed";

const hasMeetingUrl = (meetingLink?: string) =>
  /^https?:\/\//i.test(meetingLink?.trim() ?? "");
export default function MeetingLinksPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Person[]>([]);
  const [teachers, setTeachers] = useState<Person[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [channels, setChannels] = useState<Channel[]>([]);
  const [meetings] = useState<Meeting[]>([]);
  const [provider, setProvider] = useState<Provider>("GoogleWorkspace");
  const [title, setTitle] = useState("");
  const [batchId, setBatchId] = useState("");
  const [teacherId, setTeacherId] = useState("");
  const [studentToAdd, setStudentToAdd] = useState("");
  const [attendeeIds, setAttendeeIds] = useState<string[]>([]);
  const [guestEmail, setGuestEmail] = useState("");
  const [date, setDate] = useState("");
  const [time, setTime] = useState("17:00");
  const [duration, setDuration] = useState("60");
  const [recurrence, setRecurrence] = useState("Once");
  const [access, setAccess] = useState("InviteesOnly");
  const [chat, setChat] = useState("Disabled");
  const [recording, setRecording] = useState("Off");
  const [message, setMessage] = useState("Loading meeting workspace…");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [emailFilter, setEmailFilter] = useState("");
  useEffect(() => {
    void (async () => {
      try {
        const r = await academyApi("/api/academies", { cache: "no-store" });
        const academies: Academy[] = await r.json();
        if (!r.ok || !academies[0]) throw new Error();
        const current = academies[0];
        setAcademy(current);
        const rs = await Promise.all(
          [
            "students",
            "teachers",
            "batches",
            "enrollments",
            "communication-settings",
          ].map((p) =>
            academyApi(`/api/academies/${current.id}/${p}`, {
              cache: "no-store",
            }),
          ),
        );
        if (rs.some((x) => !x.ok)) throw new Error();
        setStudents(await rs[0].json());
        setTeachers(await rs[1].json());
        setBatches(await rs[2].json());
        setEnrollments(await rs[3].json());
        setChannels(await rs[4].json());
        setMessage("");
      } catch {
        setMessage(
          "Meeting settings could not be loaded. Confirm the API is running on port 5092.",
        );
      }
    })();
  }, []);
  const connected = channels.some(
    (x) =>
      x.channel === "Meeting" &&
      x.provider === provider &&
      x.hasSecureConnection,
  );
  const eligibleBatchIds = batches
    .filter(
      (b) =>
        ["Online", "Hybrid"].includes(b.deliveryMode ?? "") &&
        !hasMeetingUrl(b.meetingLink),
    )
    .map((b) => b.id);
  const eligibleStudents = students.filter((s) =>
    enrollments.some(
      (e) =>
        e.studentId === s.id &&
        e.status === "Active" &&
        eligibleBatchIds.includes(e.batchId) &&
        (!batchId || e.batchId === batchId),
    ),
  );
  const selectedStudents = students.filter((s) => attendeeIds.includes(s.id));
  const attendees = [
    ...selectedStudents.map((s) => s.email).filter(Boolean),
    ...guestEmail
      .split(",")
      .map((x) => x.trim())
      .filter(Boolean),
  ];
  const filtered = useMemo(
    () =>
      meetings.filter(
        (m) =>
          (!from || m.createdAt.slice(0, 10) >= from) &&
          (!to || m.createdAt.slice(0, 10) <= to) &&
          (!emailFilter ||
            m.attendeeEmails.some((e) =>
              e.toLowerCase().includes(emailFilter.toLowerCase()),
            )),
      ),
    [meetings, from, to, emailFilter],
  );
  function addStudent(id: string) {
    if (id && !attendeeIds.includes(id)) setAttendeeIds((x) => [...x, id]);
    setStudentToAdd("");
  }
  function edit(meeting: Meeting) {
    setTitle(meeting.title);
    setProvider(meeting.provider as Provider);
    setDate(meeting.start.slice(0, 10));
    setTime(meeting.start.slice(11, 16));
    setMessage(
      `Editing ${meeting.title}. Change any detail, then save once the provider connection is active.`,
    );
  }
  function submit(e: FormEvent) {
    e.preventDefault();
    if (!connected)
      return setMessage(
        `Connect ${providerName[provider]} in Engagement → Channel settings before creating a link.`,
      );
    setMessage(
      "Meeting creation will send the selected settings to the connected provider.",
    );
  }
  return (
    <main className="enterprise-settings meeting-links-standard min-h-screen">
      <div className="meeting-links-content">
        <header className="meeting-links-heading">
          <div className="meeting-links-title">
            <span className="meeting-links-title-icon" aria-hidden="true">↗</span>
            <div>
              <p>Class &amp; Batch</p>
              <h1>Meeting Links</h1>
            </div>
          </div>
          <div
            className={`meeting-provider-status ${connected ? "is-connected" : ""}`}
          >
            {connected
              ? `${providerName[provider]} connected`
              : `${providerName[provider]} needs connection`}
          </div>
        </header>
        {message && <p className="enterprise-page-state mt-6">{message}</p>}
        <section className="meeting-links-metrics" aria-label="Meeting link summary">
          <article><span>Eligible classes</span><strong>{eligibleBatchIds.length}</strong><small>Online or Hybrid without a link</small></article>
          <article><span>Selected invitees</span><strong>{attendees.length}</strong><small>Students and additional guests</small></article>
          <article><span>Provider</span><strong>{connected ? "Connected" : "Action needed"}</strong><small>{providerName[provider]}</small></article>
        </section>
        <section className="meeting-links-layout">
          <form onSubmit={submit} className="meeting-links-panel">
            <header>
              <div>
                <p>Create</p>
                <h2>New meeting link</h2>
              </div>
              <StandardSelectField
                name="provider"
                value={provider}
                onChange={(x) => setProvider(x as Provider)}
                placeholder="Meeting provider"
                options={(Object.keys(providerName) as Provider[]).map((x) => ({
                  value: x,
                  label: providerName[x],
                }))}
              />
            </header>
            <div className="meeting-links-fields">
              <label>
                <span>Subject / meeting title</span>
                <input
                  required
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="e.g. Piano foundations — Week 2"
                />
              </label>
              <div className="meeting-links-two">
                <StandardSelectField
                  name="batch"
                  value={batchId}
                  onChange={(x) => {
                    setBatchId(x);
                    setAttendeeIds([]);
                  }}
                  placeholder="Online or Hybrid batch without a link"
                  options={batches
                    .filter((b) => eligibleBatchIds.includes(b.id))
                    .map((b) => ({ value: b.id, label: b.name }))}
                />
                <StandardSelectField
                  name="teacher"
                  value={teacherId}
                  onChange={setTeacherId}
                  placeholder="Organizer / teacher"
                  options={teachers.map((t) => ({
                    value: t.id,
                    label: `${name(t)}${t.email ? ` · ${t.email}` : ""}`,
                  }))}
                />
              </div>
              <label>
                <span>Students to invite</span>
                <StandardSelectField
                  name="invite-student"
                  value={studentToAdd}
                  onChange={addStudent}
                  placeholder={
                    batchId
                      ? "Select enrolled student"
                      : "Select an eligible batch first"
                  }
                  options={eligibleStudents
                    .filter((s) => !attendeeIds.includes(s.id))
                    .map((s) => ({
                      value: s.id,
                      label: `${name(s)}${s.email ? ` · ${s.email}` : ""}`,
                    }))}
                />
                <small>
                  Only active students in eligible Online/Hybrid batches without
                  a meeting link are shown.
                </small>
              </label>
              {selectedStudents.length > 0 && (
                <div className="meeting-attendee-chips">
                  {selectedStudents.map((s) => (
                    <button
                      type="button"
                      key={s.id}
                      onClick={() =>
                        setAttendeeIds((x) => x.filter((id) => id !== s.id))
                      }
                    >
                      {name(s)} ×
                    </button>
                  ))}
                </div>
              )}
              <label>
                <span>Additional attendee emails</span>
                <input
                  value={guestEmail}
                  onChange={(e) => setGuestEmail(e.target.value)}
                  placeholder="parent@example.com, guest@example.com"
                />
              </label>
              <div className="meeting-links-three">
                <StandardDateField
                  name="date"
                  label="Date"
                  value={date}
                  onChange={setDate}
                  required
                />
                <StandardTimeField
                  name="start-time"
                  label="Start time"
                  value={time}
                  onChange={setTime}
                />
                <label className="meeting-duration-field">
                  <span>Duration</span>
                  <StandardSelectField
                    name="duration"
                    value={duration}
                    onChange={setDuration}
                    placeholder="Select duration"
                    options={["30", "45", "60", "90", "120"].map((x) => ({
                      value: x,
                      label: `${x} minutes`,
                    }))}
                  />
                </label>
              </div>
              <div className="meeting-links-two">
                <StandardSelectField
                  name="recurrence"
                  value={recurrence}
                  onChange={setRecurrence}
                  placeholder="Schedule"
                  options={[
                    { value: "Once", label: "One-time class" },
                    { value: "Weekly", label: "Weekly" },
                    { value: "Custom", label: "Custom recurrence" },
                  ]}
                />
                <StandardSelectField
                  name="access"
                  value={access}
                  onChange={setAccess}
                  placeholder="Who can join"
                  options={[
                    { value: "InviteesOnly", label: "Invitees only" },
                    {
                      value: "Organisation",
                      label: "Organisation and invitees",
                    },
                    { value: "Open", label: "Anyone with the link" },
                  ]}
                />
              </div>
              {recurrence !== "Once" && (
                <label>
                  <span>Recurring days and end rule</span>
                  <input placeholder="e.g. Tuesday, Thursday · until 31 Dec 2026" />
                </label>
              )}
              <section className="meeting-restrictions">
                <h3>Meeting controls</h3>
                <div className="meeting-links-two">
                  <StandardSelectField
                    name="chat"
                    value={chat}
                    onChange={setChat}
                    placeholder="In-meeting chat"
                    options={[
                      { value: "Disabled", label: "Chat disabled" },
                      { value: "HostsOnly", label: "Hosts only" },
                      { value: "Enabled", label: "Everyone" },
                    ]}
                  />
                  <StandardSelectField
                    name="recording"
                    value={recording}
                    onChange={setRecording}
                    placeholder="Recording"
                    options={[
                      { value: "Off", label: "Recording off" },
                      { value: "Auto", label: "Record automatically" },
                      { value: "Host", label: "Host decides" },
                    ]}
                  />
                </div>
              </section>
              <button
                className="enterprise-action-button"
                disabled={!connected}
              >
                {connected
                  ? "Create meeting link"
                  : `Connect ${providerName[provider]} first`}
              </button>
            </div>
          </form>
          <aside className="meeting-links-help">
            <header><p>Ready to create</p><h2>Meeting summary</h2></header>
            <dl>
              <div><dt>Provider</dt><dd>{providerName[provider]}</dd></div>
              <div><dt>Class</dt><dd>{batches.find((item) => item.id === batchId)?.name || "Not selected"}</dd></div>
              <div><dt>Invitees</dt><dd>{attendees.length} recipient{attendees.length === 1 ? "" : "s"}</dd></div>
              <div><dt>Access</dt><dd>{access === "InviteesOnly" ? "Invitees only" : access === "Organisation" ? "Organisation + invitees" : "Anyone with link"}</dd></div>
              <div><dt>Chat</dt><dd>{chat === "Disabled" ? "Disabled" : chat === "HostsOnly" ? "Hosts only" : "Enabled"}</dd></div>
              <div><dt>Recording</dt><dd>{recording === "Off" ? "Off" : recording === "Auto" ? "Automatic" : "Host decides"}</dd></div>
            </dl>
            {!connected && <a href="/communication-settings">Connect provider</a>}
          </aside>
        </section>
        <section className="meeting-history">
          <header>
            <div>
              <p>History</p>
              <h2>Created meeting links</h2>
            </div>
            <div className="meeting-history-filters">
              <StandardDateField
                name="created-from"
                label="Created from"
                value={from}
                onChange={setFrom}
              />
              <StandardDateField
                name="created-to"
                label="Created to"
                value={to}
                onChange={setTo}
              />
              <input
                value={emailFilter}
                onChange={(e) => setEmailFilter(e.target.value)}
                placeholder="Filter attendee email"
              />
            </div>
          </header>
          {filtered.length ? (
            <ul>
              {filtered.map((m) => (
                <li key={m.id}>
                  <div>
                    <b>{m.title}</b>
                    <small>
                      {providerName[m.provider as Provider]} · {m.start} ·{" "}
                      {m.attendeeEmails.length} attendees
                    </small>
                  </div>
                  <div className="meeting-history-actions">
                    {m.joinUrl && <a href={m.joinUrl}>Open meeting</a>}
                    <button type="button" onClick={() => edit(m)}>
                      Edit details
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          ) : (
            <p>
              No meeting links have been created yet. When created, use Edit
              details to change the date, time, recurrence, invitees,
              restrictions, or any other setting.
            </p>
          )}
        </section>
      </div>
    </main>
  );
}
