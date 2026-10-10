"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { StandardDateField, StandardSelectField, StandardTimeField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string }; type Staff = { id: string; displayName: string; roles: string[] }; type Work = { id: string; type: string; title: string; description?: string; priority: string; status: string; assignedUserId?: string; dueAtUtc?: string };
const filters = ["All", "Open", "InProgress", "Completed", "Cancelled"];
async function fetchWorkspace(status: string) {
  const academyResponse = await academyApi("/api/academies");
  if (!academyResponse.ok) throw Error();
  const academies: Academy[] = await academyResponse.json();
  if (!academies[0]) throw Error();
  const [workResponse, staffResponse] = await Promise.all([
    academyApi(`/api/academies/${academies[0].id}/admin-work-items${status === "All" ? "" : `?status=${status}`}`),
    academyApi(`/api/academies/${academies[0].id}/staff`),
  ]);
  if (!workResponse.ok) throw Error();
  return { academy: academies[0], work: await workResponse.json() as Work[],
    people: staffResponse.ok ? await staffResponse.json() as Staff[] : [], staffAvailable: staffResponse.ok };
}
export default function WorkQueue() {
  const [academy, setAcademy] = useState<Academy>(); const [items, setItems] = useState<Work[]>([]); const [staff, setStaff] = useState<Staff[]>([]); const [filter, setFilter] = useState("All"); const [message, setMessage] = useState("Loading operational work queue…"); const [type, setType] = useState("Operations"); const [priority, setPriority] = useState("Normal"); const [assignedUserId, setAssignedUserId] = useState(""); const [dueDate, setDueDate] = useState(""); const [dueTime, setDueTime] = useState("09:00");
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  const loadVersion = useRef(0);
  async function load(status: string, success = "") {
    const version = ++loadVersion.current;
    try {
      const workspace = await fetchWorkspace(status);
      if (version !== loadVersion.current) return;
      setAcademy(workspace.academy); setItems(workspace.work); setStaff(workspace.people);
      setMessage(success || (workspace.staffAvailable ? "" : "Staff options could not be refreshed. Work items are available."));
      if (success && !workspace.staffAvailable) setMessage(`${success} Staff options could not be refreshed; do not repeat the save.`);
    } catch {
      if (version === loadVersion.current) setMessage(success
        ? `${success} Operational work queue could not be refreshed; do not repeat the save. Refresh the page to check the saved work item.`
        : "Operational work queue could not be loaded.");
    }
  }
  useEffect(() => {
    const versionRef = loadVersion;
    const version = ++versionRef.current;
    void fetchWorkspace("All").then(workspace => {
      if (version !== versionRef.current) return;
      setAcademy(workspace.academy); setItems(workspace.work); setStaff(workspace.people);
      setMessage(workspace.staffAvailable ? "" : "Staff options could not be refreshed. Work items are available.");
    }).catch(() => {
      if (version === versionRef.current) setMessage("Operational work queue could not be loaded.");
    });
    return () => { ++versionRef.current; };
  }, []);
  async function mutate(action: () => Promise<Response>, success: string, reset?: () => void) {
    if (pending.current || !academy) return;
    pending.current = true; ++loadVersion.current;
    setSaving(true); setMessage("");
    try {
      const response = await action();
      if (!response.ok) return setMessage(response.status >= 500
        ? "Work item save could not be confirmed. Check the queue before retrying."
        : "Work item could not be saved. Your entries have been retained.");
      reset?.();
      setMessage(success);
      await load(filter, success);
    } catch {
      setMessage("Work item save could not be confirmed. Check the queue before retrying.");
    } finally { pending.current = false; setSaving(false); }
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending.current || !academy) return;
    const element = event.currentTarget;
    const form = new FormData(element);
    await mutate(() => academyApi(`/api/academies/${academy.id}/admin-work-items`, {
      method: "POST", headers: apiHeaders(true), body: JSON.stringify({
        type, title: form.get("title"), description: form.get("description") || null,
        priority, entityType: null, entityId: null, assignedUserId: assignedUserId || null,
        dueAtUtc: dueDate ? new Date(`${dueDate}T${dueTime}:00`).toISOString() : null,
      }),
    }), "Work item added to the operational queue.", () => {
      element.reset(); setAssignedUserId(""); setDueDate(""); setDueTime("09:00");
    });
  }
  async function move(item: Work, status: string) {
    if (!academy) return;
    await mutate(() => academyApi(`/api/academies/${academy.id}/admin-work-items/${item.id}/status`, {
      method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ status }),
    }), `Work item moved to ${status}.`);
  }
  const highPriority = items.filter(item => item.priority === "Critical" || item.priority === "High").length;
  return <main className="enterprise-settings work-queue-standard"><header className="work-queue-heading"><span className="work-queue-icon" aria-hidden="true">✓</span><div><p>Administration</p><h1>Operational work queue</h1></div></header>{message && <p className="work-queue-notice" role="status" aria-live="polite">{message}</p>}<section className="work-queue-kpis"><article><span>Open in view</span><b>{items.length}</b><small>Items matching the current queue filter.</small></article><article><span>High priority</span><b>{highPriority}</b><small>High or critical actions requiring attention.</small></article><article><span>Active team</span><b>{staff.length}</b><small>Staff members available for assignment.</small></article></section><section className="work-queue-layout"><form onSubmit={create} className="work-queue-panel"><header className="work-queue-panel-header"><div><p>New action</p><h2>Create operational action</h2></div></header><fieldset disabled={saving} aria-busy={saving} className="work-queue-fields" style={{ border: 0, margin: 0, minWidth: 0 }}><label className="work-queue-wide"><span>Action title</span><input required name="title" className="field" placeholder="Action title" /></label><label><span>Area</span><StandardSelectField name="type" value={type} onChange={setType} placeholder="Choose area" options={["Compliance", "Admissions", "Finance", "Academic", "People", "Operations"].map(value => ({ value, label: value }))} /></label><label><span>Priority</span><StandardSelectField name="priority" value={priority} onChange={setPriority} placeholder="Choose priority" options={["Low", "Normal", "High", "Critical"].map(value => ({ value, label: value }))} /></label><label className="work-queue-wide"><span>Owner</span><StandardSelectField name="assignedUserId" value={assignedUserId} onChange={setAssignedUserId} placeholder="Unassigned" options={staff.map(person => ({ value: person.id, label: `${person.displayName} · ${person.roles.join(", ")}` }))} /></label><StandardDateField name="dueDate" value={dueDate} onChange={setDueDate} label="Due date" /><StandardTimeField name="dueTime" value={dueTime} onChange={setDueTime} label="Due time" /><label className="work-queue-wide"><span>Context and next action</span><textarea name="description" className="field" placeholder="Context, owner expectation and next action" /></label><button className="enterprise-action-button work-queue-wide">{saving ? "Saving…" : "Add to queue"}</button></fieldset></form><section className="work-queue-panel"><header className="work-queue-panel-header"><div><p>Queue controls</p><h2>Actions requiring attention</h2></div><div className="work-queue-filter"><StandardSelectField name="workQueueFilter" disabled={saving} value={filter} onChange={value => { if (pending.current) return; setFilter(value); void load(value); }} placeholder="Filter queue" options={filters.map(value => ({ value, label: value === "InProgress" ? "In progress" : value }))} /></div></header><ul className="work-queue-list">{items.map(item => <li key={item.id}><div><span>{item.type} · {item.priority}</span><h3>{item.title}</h3>{item.description && <p>{item.description}</p>}<small>Owner: {staff.find(person => person.id === item.assignedUserId)?.displayName || "Unassigned"} · Due: {item.dueAtUtc ? new Date(item.dueAtUtc).toLocaleString("en-IN") : "No due date"}</small></div><div className="work-queue-status"><StandardSelectField name={`status-${item.id}`} disabled={saving} value={item.status} onChange={value => void move(item, value)} placeholder="Set status" options={[{ value: "Open", label: "Open" }, { value: "InProgress", label: "In progress" }, { value: "Completed", label: "Completed" }, { value: "Cancelled", label: "Cancelled" }]} /></div></li>)}{!items.length && <p className="work-queue-empty">No work items in this view.</p>}</ul></section></section></main>;
}
