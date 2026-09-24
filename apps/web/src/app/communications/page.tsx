"use client";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import {
  StandardDateField,
  StandardSelectField,
  StandardTimeField,
} from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
type Academy = { id: string };
type Person = { id: string; firstName: string; lastName: string };
type Teacher = { id: string; firstName: string; lastName: string };
type Template = {
  id: string;
  channel: string;
  name: string;
  body: string;
  isActive: boolean;
};
type Notification = {
  id: string;
  recipientType: string;
  title: string;
  message: string;
  channel: string;
  status: string;
  failureReason?: string | null;
};
const variablesFrom = (value: string) => [
  ...new Set(
    Array.from(value.matchAll(/\{\{([a-zA-Z0-9_]+)\}\}/g), (match) => match[1]),
  ),
];
export default function CommunicationsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Person[]>([]);
  const [guardians, setGuardians] = useState<Person[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [templates, setTemplates] = useState<Template[]>([]);
  const [items, setItems] = useState<Notification[]>([]);
  const [recipientType, setRecipientType] = useState("Guardian");
  const [recipientId, setRecipientId] = useState("");
  const [templateId, setTemplateId] = useState("");
  const [variables, setVariables] = useState<Record<string, string>>({});
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [channel, setChannel] = useState("InApp");
  const [scheduledDate, setScheduledDate] = useState("");
  const [scheduledTime, setScheduledTime] = useState("");
  const [displayHours, setDisplayHours] = useState("4");
  const [announcementStartDate, setAnnouncementStartDate] = useState("");
  const [announcementStartTime, setAnnouncementStartTime] = useState("09:00");
  const [announcementAudience, setAnnouncementAudience] =
    useState("Student,Teacher");
  const [message, setMessage] = useState("Loading messages…");
  const isAnnouncement = recipientType === "Academy";
  const people = recipientType === "Student" ? students : recipientType === "Teacher" ? teachers : guardians;
  const selectedTemplate = templates.find((item) => item.id === templateId);
  const templateVariables = useMemo(
    () => (selectedTemplate ? variablesFrom(selectedTemplate.body) : []),
    [selectedTemplate],
  );
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const responses = await Promise.all([
      academyApi(`/api/academies/${academyId}/students`),
      academyApi(`/api/academies/${academyId}/guardians`),
      academyApi(`/api/academies/${academyId}/teachers`),
      academyApi(`/api/academies/${academyId}/communication-templates`),
      academyApi(`/api/academies/${academyId}/notifications`),
    ]);
    if (!responses.every((response) => response.ok)) throw Error();
    setStudents(await responses[0].json());
    setGuardians(await responses[1].json());
    setTeachers(await responses[2].json());
    setTemplates(await responses[3].json());
    setItems(await responses[4].json());
    setMessage("");
  }
  useEffect(() => {
    void academyApi("/api/academies")
      .then(async (response) => {
        if (!response.ok) throw Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) throw Error();
        setAcademy(academies[0]);
        await load(academies[0].id);
      })
      .catch(() => setMessage("Messages could not be loaded."));
  }, []);
  function chooseTemplate(id: string) {
    setTemplateId(id);
    const template = templates.find((item) => item.id === id);
    if (!template) return;
    setChannel(template.channel);
    setTitle(template.name);
    setBody(template.body);
    setVariables(
      Object.fromEntries(
        variablesFrom(template.body).map((name) => [name, ""]),
      ),
    );
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || (!isAnnouncement && !recipientId)) return;
    const scheduledAtUtc = isAnnouncement
      ? announcementStartDate
        ? new Date(`${announcementStartDate}T${announcementStartTime}:00`).toISOString()
        : null
      : scheduledDate && scheduledTime
        ? new Date(`${scheduledDate}T${scheduledTime}:00`).toISOString()
        : null;
    const response = await academyApi(
      `/api/academies/${academy.id}/notifications`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          recipientId: isAnnouncement ? null : recipientId,
          recipientType,
          title,
          message: body,
          channel: isAnnouncement ? "InApp" : channel,
          scheduledAtUtc,
          templateId: templateId || null,
          variables: isAnnouncement
            ? { ...variables, audiences: announcementAudience }
            : variables,
          isImportant: isAnnouncement,
          displayHours: isAnnouncement ? Number(displayHours) : null,
        }),
      },
    );
    const result = await response.json().catch(() => null);
    if (!response.ok)
      return setMessage(result?.message ?? "Message could not be queued.");
    setMessage(
      isAnnouncement
        ? `Important announcement will run for ${displayHours} hours.`
        : `Message ${result.status?.toLowerCase() ?? "queued"}.`,
    );
    setRecipientId("");
    setTemplateId("");
    setVariables({});
    setTitle("");
    setBody("");
    setScheduledDate("");
    setScheduledTime("");
    setAnnouncementStartDate("");
    setAnnouncementStartTime("09:00");
    await load();
  }
  const recipientOptions = people.map((person) => ({
    value: person.id,
    label: `${person.firstName} ${person.lastName}`,
  }));
  return (
    <main className="enterprise-settings messages-standard min-h-screen">
      <WorkspaceNav />
      <div className="messages-content">
        <header className="messages-heading">
          <span className="messages-title-icon" aria-hidden="true">
            ✉
          </span>
          <div className="messages-title">
            <p>Engagement</p>
            <h1>Messages</h1>
          </div>
        </header>
        {message && (
          <p className="messages-notice" role="status">
            {message}
          </p>
        )}
        <section className="messages-layout">
          <form onSubmit={create} className="messages-panel messages-compose">
            <header className="messages-panel-header">
              <p>Compose</p>
              <h2>
                {isAnnouncement ? "Portal banner message" : "New message"}
              </h2>
            </header>
            <div className="messages-fields">
              <label>
                Message type
                <StandardSelectField
                  name="recipientType"
                  value={recipientType}
                  onChange={(value) => {
                    setRecipientType(value);
                    setRecipientId("");
                  }}
                  placeholder="Select message type"
                  options={[
                    { value: "Guardian", label: "Parent" },
                    { value: "Student", label: "Student" },
                    { value: "Teacher", label: "Teacher" },
                    { value: "Academy", label: "Portal banner message" },
                  ]}
                />
              </label>
              {isAnnouncement ? (
                <>
                  <label>
                    Show to
                    <StandardSelectField
                      name="announcementAudience"
                      value={announcementAudience}
                      onChange={setAnnouncementAudience}
                      placeholder="Select audience"
                      options={[
                        { value: "Student", label: "Students" },
                        { value: "Teacher", label: "Teachers" },
                        {
                          value: "Student,Teacher",
                          label: "Students and teachers",
                        },
                      ]}
                    />
                  </label>
                  <label>
                    Display duration (hours)
                    <input
                      type="number"
                      min="1"
                      max="168"
                      value={displayHours}
                      onChange={(event) => setDisplayHours(event.target.value)}
                      required
                    />
                  </label>
                  <StandardDateField
                    name="announcementStartDate"
                    value={announcementStartDate}
                    onChange={setAnnouncementStartDate}
                    label="Banner start date"
                  />
                  <StandardTimeField
                    name="announcementStartTime"
                    value={announcementStartTime}
                    onChange={setAnnouncementStartTime}
                    label="Banner start time"
                  />
                </>
              ) : (
                <>
                  <label>
                    Recipient
                    <StandardSelectField
                      name="recipientId"
                      value={recipientId}
                      onChange={setRecipientId}
                      placeholder={`Select ${recipientType.toLowerCase()}`}
                      options={recipientOptions}
                    />
                  </label>
                  <label>
                    Template
                    <StandardSelectField
                      name="templateId"
                      value={templateId}
                      onChange={chooseTemplate}
                      placeholder="Manual message"
                      options={templates
                        .filter((item) => item.isActive)
                        .map((item) => ({
                          value: item.id,
                          label: `${item.channel} · ${item.name}`,
                        }))}
                    />
                  </label>
                  {templateVariables.map((name) => (
                    <label key={name}>
                      {name.replaceAll("_", " ")}
                      <input
                        value={variables[name] ?? ""}
                        onChange={(event) =>
                          setVariables({
                            ...variables,
                            [name]: event.target.value,
                          })
                        }
                        required
                      />
                    </label>
                  ))}
                </>
              )}
              <label className="messages-wide">
                Title
                <input
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                  required
                />
              </label>
              <label className="messages-wide">
                Message
                <textarea
                  value={body}
                  onChange={(event) => setBody(event.target.value)}
                  required
                />
              </label>
              {!isAnnouncement && (
                <>
                  <label>
                    Channel
                    <StandardSelectField
                      name="channel"
                      value={channel}
                      onChange={setChannel}
                      placeholder="Select channel"
                      options={[
                        { value: "InApp", label: "In-app notification" },
                        { value: "Email", label: "Email" },
                        { value: "WhatsApp", label: "WhatsApp" },
                      ]}
                    />
                  </label>
                  <StandardDateField
                    name="scheduledDate"
                    value={scheduledDate}
                    onChange={setScheduledDate}
                    label="Schedule date"
                  />
                  <StandardTimeField
                    name="scheduledTime"
                    value={scheduledTime}
                    onChange={setScheduledTime}
                    label="Schedule time"
                  />
                </>
              )}
              <button
                disabled={!academy || (!isAnnouncement && !recipientId)}
                className="enterprise-action-button messages-wide"
              >
                {isAnnouncement ? "Publish announcement" : "Queue message"}
              </button>
            </div>
          </form>
          <section className="messages-panel messages-log">
            <header className="messages-panel-header">
              <div>
                <p>Activity</p>
                <h2>Message log</h2>
              </div>
              <span>{items.length}</span>
            </header>
            {items.length === 0 ? (
              <p className="messages-empty">No messages yet.</p>
            ) : (
              <ul>
                {items.map((item) => (
                  <li key={item.id}>
                    <div>
                      <b>{item.title}</b>
                      <p>{item.message}</p>
                      <small>
                        {item.channel} · {item.recipientType}
                      </small>
                      {item.failureReason && <em>{item.failureReason}</em>}
                    </div>
                    <span>{item.status}</span>
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
