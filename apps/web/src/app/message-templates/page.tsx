"use client";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
type Academy = { id: string };
type Template = {
  id: string;
  channel: string;
  name: string;
  templateKey: string;
  category: string;
  templateGroup: string;
  status: string;
  language: string;
  providerTemplateName?: string | null;
  subject?: string | null;
  body: string;
  isActive: boolean;
};
type Starter = {
  id: string;
  channel: string;
  templateGroup: string;
  name: string;
  templateKey: string;
  category: string;
  subject?: string | null;
  body: string;
};
type Draft = {
  channel: string;
  name: string;
  templateKey: string;
  category: string;
  templateGroup: string;
  status: string;
  language: string;
  providerTemplateName: string;
  subject: string;
  body: string;
  isActive: boolean;
};
const initialDraft: Draft = {
  channel: "WhatsApp",
  name: "",
  templateKey: "",
  category: "Utility",
  templateGroup: "General",
  status: "Draft",
  language: "en",
  providerTemplateName: "",
  subject: "",
  body: "",
  isActive: true,
};
export default function MessageTemplatesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [items, setItems] = useState<Template[]>([]);
  const [catalogue, setCatalogue] = useState<Starter[]>([]);
  const [selectedChannel, setSelectedChannel] = useState<"WhatsApp" | "Email">(
    "WhatsApp",
  );
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [message, setMessage] = useState("Loading templates…");
  const [form, setForm] = useState<Draft>(initialDraft);
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const [templateResponse, catalogueResponse] = await Promise.all([
      academyApi(`/api/academies/${academyId}/communication-templates`),
      academyApi(
        `/api/academies/${academyId}/communication-templates/starter-templates`,
      ),
    ]);
    if (!templateResponse.ok || !catalogueResponse.ok) throw Error();
    setItems(await templateResponse.json());
    setCatalogue(await catalogueResponse.json());
  }
  useEffect(() => {
    void academyApi("/api/academies")
      .then(async (response) => {
        if (!response.ok) throw Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) throw Error();
        setAcademy(academies[0]);
        await load(academies[0].id);
        setMessage("");
      })
      .catch(() => setMessage("Templates could not be loaded."));
  }, []);
  const availableStarters = useMemo(
    () => catalogue.filter((item) => item.channel === selectedChannel),
    [catalogue, selectedChannel],
  );
  const groupedStarters = useMemo(
    () =>
      Object.entries(
        Object.groupBy(availableStarters, (item) => item.templateGroup),
      ),
    [availableStarters],
  );
  function toggle(id: string) {
    setSelectedIds((current) =>
      current.includes(id)
        ? current.filter((value) => value !== id)
        : [...current, id],
    );
  }
  function toggleGroup(groupItems: Starter[]) {
    const ids = groupItems.map((item) => item.id);
    setSelectedIds((current) =>
      ids.every((id) => current.includes(id))
        ? current.filter((id) => !ids.includes(id))
        : [...new Set([...current, ...ids])],
    );
  }
  async function addSelected() {
    if (!academy || !selectedIds.length) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/communication-templates/starter-templates`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({ templateIds: selectedIds }),
      },
    );
    const result = await response.json().catch(() => null);
    if (!response.ok)
      return setMessage(
        result?.message ?? "Selected templates could not be added.",
      );
    setSelectedIds([]);
    setMessage(result.message);
    await load();
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/communication-templates`,
      { method: "POST", headers: apiHeaders(true), body: JSON.stringify(form) },
    );
    const result = await response.json().catch(() => null);
    if (!response.ok)
      return setMessage(result?.message ?? "Template could not be saved.");
    setForm(initialDraft);
    setMessage("Template saved.");
    await load();
  }
  return (
    <main className="enterprise-settings templates-standard min-h-screen">
      <WorkspaceNav />
      <div className="templates-content">
        <header className="templates-heading">
          <span className="templates-title-icon" aria-hidden="true">
            ▤
          </span>
          <div className="templates-title">
            <p>Engagement</p>
            <h1>Templates</h1>
          </div>
        </header>
        {message && (
          <p className="templates-notice" role="status">
            {message}
          </p>
        )}
        <section className="templates-panel templates-catalogue">
          <header className="templates-panel-header">
            <div>
              <p>Library</p>
              <h2>Starter templates</h2>
            </div>
            <button
              type="button"
              className="enterprise-action-button"
              disabled={!academy || !selectedIds.length}
              onClick={addSelected}
            >
              Add selected ({selectedIds.length})
            </button>
          </header>
          <div className="templates-channel">
            <button
              type="button"
              data-active={selectedChannel === "WhatsApp"}
              onClick={() => setSelectedChannel("WhatsApp")}
            >
              WhatsApp
            </button>
            <button
              type="button"
              data-active={selectedChannel === "Email"}
              onClick={() => setSelectedChannel("Email")}
            >
              Email
            </button>
          </div>
          <div className="templates-groups">
            {groupedStarters.map(([group, values]) => {
              const groupItems = values ?? [];
              const allSelected = groupItems.every((item) =>
                selectedIds.includes(item.id),
              );
              return (
                <section key={group}>
                  <header>
                    <h3>{group}</h3>
                    <button
                      type="button"
                      onClick={() => toggleGroup(groupItems)}
                    >
                      {allSelected ? "Clear group" : "Select group"}
                    </button>
                  </header>
                  {groupItems.map((item) => (
                    <label key={item.id}>
                      <input
                        type="checkbox"
                        checked={selectedIds.includes(item.id)}
                        onChange={() => toggle(item.id)}
                      />
                      <span>
                        <b>{item.name}</b>
                        <small>
                          {item.category} · {item.templateKey}
                        </small>
                      </span>
                    </label>
                  ))}
                </section>
              );
            })}
          </div>
        </section>
        <section className="templates-layout">
          <form onSubmit={create} className="templates-panel">
            <header className="templates-panel-header">
              <div>
                <p>Create</p>
                <h2>Custom template</h2>
              </div>
            </header>
            <div className="templates-fields">
              <label>
                Channel
                <StandardSelectField
                  name="channel"
                  value={form.channel}
                  onChange={(value) => setForm({ ...form, channel: value })}
                  placeholder="Select channel"
                  options={[
                    { value: "WhatsApp", label: "WhatsApp" },
                    { value: "Email", label: "Email" },
                  ]}
                />
              </label>
              <label>
                Group
                <StandardSelectField
                  name="templateGroup"
                  value={form.templateGroup}
                  onChange={(value) =>
                    setForm({ ...form, templateGroup: value })
                  }
                  placeholder="Select group"
                  options={[
                    "Finance",
                    "Admissions",
                    "Classes",
                    "Academic",
                    "Academy updates",
                    "General",
                  ].map((value) => ({ value, label: value }))}
                />
              </label>
              <label>
                Name
                <input
                  value={form.name}
                  onChange={(event) =>
                    setForm({ ...form, name: event.target.value })
                  }
                  required
                />
              </label>
              <label>
                Template key
                <input
                  value={form.templateKey}
                  onChange={(event) =>
                    setForm({ ...form, templateKey: event.target.value })
                  }
                  required
                />
              </label>
              <label>
                Category
                <StandardSelectField
                  name="category"
                  value={form.category}
                  onChange={(value) => setForm({ ...form, category: value })}
                  placeholder="Select category"
                  options={[
                    "Utility",
                    "Marketing",
                    "Authentication",
                    "Transactional",
                  ].map((value) => ({ value, label: value }))}
                />
              </label>
              <label>
                Status
                <StandardSelectField
                  name="status"
                  value={form.status}
                  onChange={(value) => setForm({ ...form, status: value })}
                  placeholder="Select status"
                  options={["Draft", "Approved", "Disabled"].map((value) => ({
                    value,
                    label: value,
                  }))}
                />
              </label>
              <label>
                Language
                <input
                  value={form.language}
                  onChange={(event) =>
                    setForm({ ...form, language: event.target.value })
                  }
                />
              </label>
              <label>
                Provider template
                <input
                  value={form.providerTemplateName}
                  onChange={(event) =>
                    setForm({
                      ...form,
                      providerTemplateName: event.target.value,
                    })
                  }
                />
              </label>
              <label className="templates-wide">
                Email subject
                <input
                  value={form.subject}
                  onChange={(event) =>
                    setForm({ ...form, subject: event.target.value })
                  }
                />
              </label>
              <label className="templates-wide">
                Template body
                <textarea
                  value={form.body}
                  onChange={(event) =>
                    setForm({ ...form, body: event.target.value })
                  }
                  required
                />
              </label>
              <label className="templates-check templates-wide">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(event) =>
                    setForm({ ...form, isActive: event.target.checked })
                  }
                />
                Available for use
              </label>
              <button
                disabled={!academy}
                className="enterprise-action-button templates-wide"
              >
                Save template
              </button>
            </div>
          </form>
          <section className="templates-panel templates-register">
            <header className="templates-panel-header">
              <div>
                <p>Library</p>
                <h2>Saved templates</h2>
              </div>
              <span>{items.length}</span>
            </header>
            {items.length === 0 ? (
              <p className="templates-empty">No templates added yet.</p>
            ) : (
              <ul>
                {items.map((item) => (
                  <li key={item.id}>
                    <div>
                      <b>{item.name}</b>
                      <small>
                        {item.channel} · {item.templateGroup} · {item.status}
                      </small>
                      <p>{item.body}</p>
                    </div>
                    <span>{item.isActive ? "Active" : "Inactive"}</span>
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
