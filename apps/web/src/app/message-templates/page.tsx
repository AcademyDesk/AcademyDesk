"use client";
import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import visual from "@/components/design-system/engagement-workspace.module.css";
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
  const [ready, setReady] = useState(false);
  const [saving, setSaving] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const guard = useRef({ ready: false, pending: false, review: false, snapshot: 0, draft: 0, selection: 0 });
  const mounted = useRef(false);
  const snapshot = guard.current.snapshot;
  const blocked = !ready || saving || needsReview;
  function changeDraft() { guard.current.draft++; }
  function editDraft(next: Draft) { changeDraft(); setForm(next); }
  function requireReview() { guard.current.review = true; setNeedsReview(true); }
  const load = useCallback(async (academyId: string) => {
    const [templateResponse, catalogueResponse] = await Promise.all([
      academyApi(`/api/academies/${academyId}/communication-templates`),
      academyApi(
        `/api/academies/${academyId}/communication-templates/starter-templates`,
      ),
    ]);
    if (!templateResponse.ok || !catalogueResponse.ok) throw Error();
    const [rows, starters] = await Promise.all([templateResponse.json(), catalogueResponse.json()]);
    const strings = (row: Record<string, unknown>, keys: string[]) => row && keys.every(key => typeof row[key] === "string");
    if (!Array.isArray(rows) || !Array.isArray(starters) ||
      !rows.every(row => strings(row, ["id", "channel", "name", "templateGroup", "status", "body"]) && typeof row.isActive === "boolean") ||
      !starters.every(row => strings(row, ["id", "channel", "name", "templateGroup", "templateKey", "category", "body"]))) throw Error();
    if (!mounted.current) return;
    setItems(rows); setCatalogue(starters);
    guard.current.ready = true; guard.current.snapshot++; setReady(true);
  }, []);
  useEffect(() => {
    mounted.current = true;
    void academyApi("/api/academies")
      .then(async (response) => {
        if (!response.ok) throw Error();
        const academies: Academy[] = await response.json();
        if (!Array.isArray(academies) || !academies[0]?.id) throw Error();
        if (!mounted.current) return;
        setAcademy(academies[0]);
        await load(academies[0].id);
        if (mounted.current) setMessage("");
      })
      .catch(() => { if (mounted.current) setMessage("Templates could not be loaded."); });
    return () => { mounted.current = false; };
  }, [load]);
  async function refreshRegister() {
    if (!mounted.current || guard.current.pending) return;
    guard.current.pending = true; guard.current.ready = false; guard.current.snapshot++; setReady(false); setSaving(true);
    try {
      let id = academy?.id;
      if (!id) {
        const response = await academyApi("/api/academies");
        const accounts: Academy[] = await response.json();
        if (!response.ok || !Array.isArray(accounts) || !accounts[0]?.id) throw Error();
        id = accounts[0].id;
        if (!mounted.current) return;
        setAcademy(accounts[0]);
      }
      await load(id);
      if (mounted.current) { guard.current.review = false; setNeedsReview(false); setMessage("Template register refreshed. Check for the previous template or import before trying again."); }
    } catch { if (mounted.current) { requireReview(); setMessage("The template register could not be refreshed. Do not repeat an unconfirmed save or import."); } }
    finally { guard.current.pending = false; if (mounted.current) setSaving(false); }
  }
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
    if (!guard.current.ready || guard.current.pending || guard.current.review || snapshot !== guard.current.snapshot || !catalogue.some(item => item.id === id)) return;
    guard.current.selection++;
    setSelectedIds((current) =>
      current.includes(id)
        ? current.filter((value) => value !== id)
        : [...current, id],
    );
  }
  function toggleGroup(groupItems: Starter[]) {
    if (!guard.current.ready || guard.current.pending || guard.current.review || snapshot !== guard.current.snapshot || groupItems.some(item => !catalogue.some(row => row.id === item.id))) return;
    guard.current.selection++;
    const ids = groupItems.map((item) => item.id);
    setSelectedIds((current) =>
      ids.every((id) => current.includes(id))
        ? current.filter((id) => !ids.includes(id))
        : [...new Set([...current, ...ids])],
    );
  }
  async function addSelected() {
    if (!mounted.current || !academy || !guard.current.ready || guard.current.pending || guard.current.review || snapshot !== guard.current.snapshot || !selectedIds.length) return;
    if (selectedIds.some(id => !catalogue.some(item => item.id === id))) { setMessage("Refresh the register and select available starter templates."); return; }
    const captured = { ids: [...selectedIds], revision: guard.current.selection };
    guard.current.pending = true; setSaving(true); setMessage("Adding selected templates…");
    let confirmed = false;
    try {
    const response = await academyApi(
      `/api/academies/${academy.id}/communication-templates/starter-templates`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({ templateIds: captured.ids }),
      },
    );
    const result = await response.json().catch(() => null);
    if (!mounted.current) return;
    if (!response.ok) {
      if (response.status >= 500 || response.status === 408) { requireReview(); setMessage("Template import could not be confirmed. Your selection is retained. Refresh and check the register before retrying."); }
      else setMessage(typeof result?.message === "string" && result.message.trim() ? result.message : "Selected templates could not be added.");
      return;
    }
    confirmed = true;
    if (guard.current.selection === captured.revision) setSelectedIds([]);
    setMessage(typeof result?.message === "string" && result.message.trim() ? result.message : "Selected templates added.");
    await load(academy.id);
    } catch { if (mounted.current) { requireReview(); setMessage(confirmed ? "Selected templates added. The register could not be refreshed; do not repeat this import. Refresh the template register to continue." : "Template import could not be confirmed. Your selection is retained. Refresh and check the register before retrying."); } }
    finally { guard.current.pending = false; if (mounted.current) setSaving(false); }
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!mounted.current || !academy || !guard.current.ready || guard.current.pending || guard.current.review || snapshot !== guard.current.snapshot) return;
    if (!form.name.trim() || !form.templateKey.trim() || !form.body.trim()) { setMessage("Provide a name, template key and template body."); return; }
    const captured = { form: { ...form }, revision: guard.current.draft };
    guard.current.pending = true; setSaving(true); setMessage("Saving template…");
    let confirmed = false;
    try {
    const response = await academyApi(
      `/api/academies/${academy.id}/communication-templates`,
      { method: "POST", headers: apiHeaders(true), body: JSON.stringify(captured.form) },
    );
    const result = await response.json().catch(() => null);
    if (!mounted.current) return;
    if (!response.ok) {
      if (response.status >= 500 || response.status === 408) { requireReview(); setMessage("Template save could not be confirmed. Your draft is retained. Refresh and check the register before retrying."); }
      else setMessage(typeof result?.message === "string" && result.message.trim() ? result.message : "Template could not be saved.");
      return;
    }
    confirmed = true;
    if (guard.current.draft === captured.revision) setForm(initialDraft);
    setMessage("Template saved.");
    await load(academy.id);
    } catch { if (mounted.current) { requireReview(); setMessage(confirmed ? "Template saved. The register could not be refreshed; do not repeat this save. Refresh the template register to continue." : "Template save could not be confirmed. Your draft is retained. Refresh and check the register before retrying."); } }
    finally { guard.current.pending = false; if (mounted.current) setSaving(false); }
  }
  return (
    <main className={`enterprise-settings templates-standard min-h-screen ${visual.page}`}>
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
        <button type="button" className="enterprise-action-button" disabled={saving} onClick={refreshRegister}>Refresh template register</button>
        <fieldset disabled={blocked} className={visual.catalogue} aria-busy={saving}>
        <section className="templates-panel templates-catalogue">
          <header className="templates-panel-header">
            <div>
              <p>Library</p>
              <h2>Starter templates</h2>
            </div>
            <button
              type="button"
              className="enterprise-action-button"
              disabled={blocked || !academy || !selectedIds.length}
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
        </fieldset>
        <section className="templates-layout">
          <form onSubmit={create} onChangeCapture={changeDraft} aria-busy={saving} className="templates-panel">
            <header className="templates-panel-header">
              <div>
                <p>Create</p>
                <h2>Custom template</h2>
              </div>
            </header>
            <fieldset disabled={blocked} className={`templates-fields ${visual.fields}`}>
              <div>
                <StandardSelectField label="Channel"
                  name="channel"
                  value={form.channel}
                  disabled={blocked}
                  onChange={(value) => editDraft({ ...form, channel: value })}
                  placeholder="Select channel"
                  options={[
                    { value: "WhatsApp", label: "WhatsApp" },
                    { value: "Email", label: "Email" },
                  ]}
                />
              </div>
              <div>
                <StandardSelectField label="Group"
                  name="templateGroup"
                  disabled={blocked}
                  value={form.templateGroup}
                  onChange={(value) =>
                    editDraft({ ...form, templateGroup: value })
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
              </div>
              <label>
                Name
                <input
                  value={form.name}
                  onChange={(event) =>
                    editDraft({ ...form, name: event.target.value })
                  }
                  required
                />
              </label>
              <label>
                Template key
                <input
                  value={form.templateKey}
                  onChange={(event) =>
                    editDraft({ ...form, templateKey: event.target.value })
                  }
                  required
                />
              </label>
              <div>
                <StandardSelectField label="Category"
                  name="category"
                  value={form.category}
                  disabled={blocked}
                  onChange={(value) => editDraft({ ...form, category: value })}
                  placeholder="Select category"
                  options={[
                    "Utility",
                    "Marketing",
                    "Authentication",
                    "Transactional",
                  ].map((value) => ({ value, label: value }))}
                />
              </div>
              <div>
                <StandardSelectField label="Status"
                  name="status"
                  value={form.status}
                  disabled={blocked}
                  onChange={(value) => editDraft({ ...form, status: value })}
                  placeholder="Select status"
                  options={["Draft", "Approved", "Disabled"].map((value) => ({
                    value,
                    label: value,
                  }))}
                />
              </div>
              <label>
                Language
                <input
                  value={form.language}
                  onChange={(event) =>
                    editDraft({ ...form, language: event.target.value })
                  }
                />
              </label>
              <label>
                Provider template
                <input
                  value={form.providerTemplateName}
                  onChange={(event) =>
                    editDraft({
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
                    editDraft({ ...form, subject: event.target.value })
                  }
                />
              </label>
              <label className="templates-wide">
                Template body
                <textarea
                  value={form.body}
                  onChange={(event) =>
                    editDraft({ ...form, body: event.target.value })
                  }
                  required
                />
              </label>
              <label className="templates-check templates-wide">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(event) =>
                    editDraft({ ...form, isActive: event.target.checked })
                  }
                />
                Available for use
              </label>
              <button
                disabled={blocked || !academy}
                className="enterprise-action-button templates-wide"
              >
                Save template
              </button>
            </fieldset>
          </form>
          <section className="templates-panel templates-register">
            <header className="templates-panel-header">
              <div>
                <p>Library</p>
                <h2>Saved templates</h2>
              </div>
              <span>{ready ? items.length : "Unavailable"}</span>
            </header>
            {!ready ? <p className="templates-empty">Template register unavailable. Refresh to load templates.</p> : items.length === 0 ? (
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
