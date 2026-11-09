"use client";
import { FormEvent, useEffect, useRef, useState } from "react";
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
  const [loaded, setLoaded] = useState(false), [loading, setLoading] = useState(true);
  const alive = useRef(true), reading = useRef(false);
  async function readWorkspace(initialResponse?: Response) {
    if (!alive.current || reading.current) return;
    reading.current = true;
    try {
      const response = initialResponse ?? await academyApi("/api/academies", { cache: "no-store" });
      if (!response.ok) throw Error("Meeting settings could not be loaded.");
      const academies: Academy[] = await response.json();
      if (!Array.isArray(academies) || !academies[0] || !academies.every(row => row && typeof row.id === "string")) throw Error("Invalid academy context");
      const current = academies[0];
      const responses = await Promise.all(["students", "teachers", "batches", "enrollments", "communication-settings"].map(path => academyApi(`/api/academies/${current.id}/${path}`, { cache: "no-store" })));
      if (responses.some(response => !response.ok)) throw Error("Meeting settings could not be loaded.");
      const [studentRows, teacherRows, batchRows, enrollmentRows, channelRows]: [Person[], Person[], Batch[], Enrollment[], Channel[]] = await Promise.all([responses[0].json(), responses[1].json(), responses[2].json(), responses[3].json(), responses[4].json()]);
      if (!Array.isArray(studentRows) || !studentRows.every(row => row && typeof row.id === "string") || !Array.isArray(teacherRows) || !teacherRows.every(row => row && typeof row.id === "string")
        || !Array.isArray(batchRows) || !batchRows.every(row => row && typeof row.id === "string" && typeof row.name === "string") || !Array.isArray(enrollmentRows) || !enrollmentRows.every(row => row && typeof row.studentId === "string" && typeof row.batchId === "string" && typeof row.status === "string")
        || !Array.isArray(channelRows) || !channelRows.every(row => row && typeof row.channel === "string" && typeof row.provider === "string" && typeof row.status === "string" && typeof row.hasSecureConnection === "boolean")) throw Error("Invalid meeting response");
      if (!alive.current) return;
      setAcademy(current); setStudents(studentRows); setTeachers(teacherRows); setBatches(batchRows); setEnrollments(enrollmentRows); setChannels(channelRows); setLoaded(true); setMessage("");
    } catch { if (alive.current) setMessage("Meeting settings could not be loaded. Check account access and try again."); }
    finally { reading.current = false; if (alive.current) setLoading(false); }
  }
  function reloadWorkspace() {
    if (!alive.current || reading.current) return;
    setLoaded(false); setLoading(true); setMessage("Loading meeting workspace…"); return readWorkspace();
  }
  useEffect(() => {
    void academyApi("/api/academies", { cache: "no-store" }).then(response => readWorkspace(response)).catch(() => {
      if (alive.current) { setMessage("Meeting settings could not be loaded. Check account access and try again."); setLoading(false); }
    });
  }, []);
  useEffect(() => { alive.current = true; return () => { alive.current = false; }; }, []);
  const connectionRecorded = channels.some(
    (x) =>
      x.channel === "Meeting" &&
      x.provider === provider &&
      x.status === "Configured" && x.hasSecureConnection,
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
  function addStudent(id: string) {
    if (id && !attendeeIds.includes(id)) setAttendeeIds((x) => [...x, id]);
    setStudentToAdd("");
  }
  function submit(event: FormEvent) {
    event.preventDefault();
    setMessage("Meeting creation is unavailable in this version. No invitations were sent.");
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
          <div className="meeting-provider-status">{providerName[provider]} · {loaded && connectionRecorded ? "Connection recorded; creation unavailable" : "Meeting creation unavailable"}</div>
        </header>
        <p className="enterprise-page-state mt-6" role="status">Meeting creation and provider history are unavailable in this version. Saved provider details do not enable meeting execution.</p>
        {message && <p className="enterprise-page-state mt-6" role={!loaded && !loading ? "alert" : "status"}>{message}</p>}
        {!loaded && !loading && <button type="button" className="enterprise-action-button" onClick={() => void reloadWorkspace()}>Try again</button>}
        {loaded && <section className="meeting-links-metrics" aria-label="Meeting link summary">
          <article><span>Eligible classes</span><strong>{eligibleBatchIds.length}</strong><small>Online or Hybrid without a link</small></article>
          <article><span>Selected invitees</span><strong>{attendees.length}</strong><small>Students and additional guests</small></article>
          <article><span>Provider</span><strong>{connectionRecorded ? "Connection recorded" : "Unverified"}</strong><small>{providerName[provider]}</small></article>
        </section>}
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
            <fieldset disabled className="contents"><div className="meeting-links-fields">
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
                disabled
              >
                Meeting creation unavailable
              </button>
            </div></fieldset>
          </form>
          <aside className="meeting-links-help">
            <header><p>Draft preview only</p><h2>Planned meeting settings</h2></header>
            <dl>
              <div><dt>Provider</dt><dd>{providerName[provider]}</dd></div>
              <div><dt>Class</dt><dd>{batches.find((item) => item.id === batchId)?.name || "Not selected"}</dd></div>
              <div><dt>Invitees</dt><dd>{attendees.length} recipient{attendees.length === 1 ? "" : "s"}</dd></div>
              <div><dt>Access</dt><dd>{access === "InviteesOnly" ? "Invitees only" : access === "Organisation" ? "Organisation + invitees" : "Anyone with link"}</dd></div>
              <div><dt>Chat</dt><dd>{chat === "Disabled" ? "Disabled" : chat === "HostsOnly" ? "Hosts only" : "Enabled"}</dd></div>
              <div><dt>Recording</dt><dd>{recording === "Off" ? "Off" : recording === "Auto" ? "Automatic" : "Host decides"}</dd></div>
            </dl>
            <a href="/communication-settings">View provider details</a>
          </aside>
        </section>
        <section className="meeting-history"><header><div><p>History</p><h2>Provider meeting history</h2></div></header><p role="status">Provider meeting history is unavailable. This page cannot verify previously created provider meetings.</p></section>
      </div>
    </main>
  );
}
