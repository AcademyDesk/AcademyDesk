"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardDateField, StandardSelectField, StandardTimeField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Person = { id: string; firstName?: string; lastName?: string; name?: string };
type Makeup = { id: string; studentId: string; batchId: string; startUtc: string; deliveryMode: string; venue?: string; meetingLink?: string; usesNextScheduledClass: boolean; status: string };
const toUtc = (value: string) => new Date(`${value.length === 16 ? `${value}:00` : value}+05:30`).toISOString();
const dateTime = (value: string) => new Intl.DateTimeFormat("en-IN", { dateStyle: "medium", timeStyle: "short", timeZone: "Asia/Kolkata" }).format(new Date(value));

async function fetchWorkspace(academyId: string): Promise<{ students: Person[]; batches: Person[]; teachers: Person[]; makeups: Makeup[] }> {
  const responses = await Promise.all(["students", "batches", "teachers", "makeup-classes"].map((path) => academyApi(`/api/academies/${academyId}/${path}`, { cache: "no-store" })));
  if (responses.some((response) => !response.ok)) throw new Error();
  return { students: await responses[0].json(), batches: await responses[1].json(), teachers: await responses[2].json(), makeups: await responses[3].json() };
}

export default function MakeupPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Person[]>([]); const [batches, setBatches] = useState<Person[]>([]); const [teachers, setTeachers] = useState<Person[]>([]); const [makeups, setMakeups] = useState<Makeup[]>([]);
  const [studentId, setStudentId] = useState(""); const [batchId, setBatchId] = useState(""); const [teacherId, setTeacherId] = useState(""); const [mode, setMode] = useState<"Manual" | "NextScheduled">("Manual"); const [start, setStart] = useState(""); const [deliveryMode, setDeliveryMode] = useState("Offline"); const [venue, setVenue] = useState(""); const [meetingLink, setMeetingLink] = useState(""); const [message, setMessage] = useState("Loading make-up classes…"); const [saving, setSaving] = useState(false);
  const pendingWrite = useRef(false);
  const name = (items: Person[], id?: string) => { const item = items.find((row) => row.id === id); return item?.name ?? (`${item?.firstName ?? ""} ${item?.lastName ?? ""}`.trim() || "Unknown"); };
  const setDate = (date: string) => setStart(date ? `${date}T${start.slice(11) || "09:00"}` : "");

  async function load() {
    if (!academy) return;
    const data = await fetchWorkspace(academy.id);
    setStudents(data.students); setBatches(data.batches); setTeachers(data.teachers); setMakeups(data.makeups);
  }

  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        const academies: Academy[] = await response.json();
        if (!response.ok || !academies[0]) throw new Error();
        setAcademy(academies[0]);
        const data = await fetchWorkspace(academies[0].id);
        setStudents(data.students); setBatches(data.batches); setTeachers(data.teachers); setMakeups(data.makeups);
        setMessage("");
      } catch { setMessage("Make-up classes could not be loaded."); }
    }
    void initialise();
  }, []);

  async function refreshAfterSave(success: string) {
    setMessage(success);
    try { await load(); }
    catch { setMessage(`${success} The list could not be refreshed; refresh to see the saved record and do not submit it again.`); }
  }

  async function writeFailure(response: Response, fallback: string) {
    const result = await response.json().catch(() => null);
    const detail = typeof result?.message === "string" ? result.message.trim() : "";
    setMessage(response.status >= 500
      ? `${detail ? `${detail} ` : ""}The make-up change could not be confirmed; check the saved record before retrying.`
      : detail || fallback);
  }

  async function create(event: FormEvent) {
    event.preventDefault();
    if (pendingWrite.current) return;
    if (!academy || !studentId || !batchId) return setMessage("Select a student and class or batch.");
    if (mode === "Manual" && !start) return setMessage("Select a make-up date and time.");
    if (mode === "Manual" && ["Online", "Hybrid"].includes(deliveryMode) && !meetingLink.trim()) return setMessage("A meeting link is required for online and hybrid make-up classes.");
    pendingWrite.current = true; setSaving(true); setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/makeup-classes`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, batchId, teacherId: teacherId || null, startUtc: mode === "Manual" ? toUtc(start) : null, deliveryMode: mode === "Manual" ? deliveryMode : "Offline", venue: mode === "Manual" && deliveryMode === "Offline" ? venue.trim() || null : null, meetingLink: mode === "Manual" && ["Online", "Hybrid"].includes(deliveryMode) ? meetingLink.trim() || null : null, useNextScheduledClass: mode === "NextScheduled", notes: null }) });
      if (!response.ok) return await writeFailure(response, "The make-up class could not be scheduled.");
      setStart(""); setVenue(""); setMeetingLink("");
      await refreshAfterSave("Make-up class scheduled.");
    } catch { setMessage("The make-up change could not be confirmed; check the saved record before retrying."); }
    finally { pendingWrite.current = false; setSaving(false); }
  }

  async function updateStatus(item: Makeup, status: string) {
    if (!academy || pendingWrite.current) return;
    pendingWrite.current = true; setSaving(true); setMessage("");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/makeup-classes/${item.id}`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ status }) });
      if (!response.ok) return await writeFailure(response, "The make-up status could not be updated.");
      await refreshAfterSave(`Make-up status updated to ${status}.`);
    } catch { setMessage("The make-up change could not be confirmed; check the saved record before retrying."); }
    finally { pendingWrite.current = false; setSaving(false); }
  }

  return <main className="enterprise-settings enterprise-legacy-standard makeup-standard min-h-screen bg-slate-950 text-slate-100"><WorkspaceNav /><div className="makeup-content mx-auto max-w-6xl px-6 py-10">
    <header className="makeup-heading"><div className="makeup-title"><span className="makeup-title-icon" aria-hidden="true">↺</span><div><p>Class &amp; batch</p><h1>Make-up classes</h1></div></div></header>
    {message && <p role="status" aria-live="polite" className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-100">{message}</p>}
    <section className="makeup-layout"><form onSubmit={create} className="makeup-panel"><header className="makeup-panel-header"><div><p>Schedule</p><h2>Schedule make-up</h2></div></header><div className="makeup-fields">
      <StandardSelectField name="student" disabled={saving} value={studentId} onChange={setStudentId} placeholder="Select student" options={students.map((item) => ({ value: item.id, label: name(students, item.id) }))} />
      <StandardSelectField name="batch" disabled={saving} value={batchId} onChange={setBatchId} placeholder="Select class or batch" options={batches.map((item) => ({ value: item.id, label: name(batches, item.id) }))} />
      <StandardSelectField name="teacher" disabled={saving} value={teacherId} onChange={setTeacherId} placeholder="Use class teacher" options={teachers.map((item) => ({ value: item.id, label: name(teachers, item.id) }))} />
      <StandardSelectField name="scheduling-mode" disabled={saving} value={mode} onChange={(value) => setMode(value as "Manual" | "NextScheduled")} placeholder="Scheduling method" options={[{ value: "Manual", label: "Choose date and time" }, { value: "NextScheduled", label: "Use next scheduled class" }]} />
      {mode === "Manual" ? <fieldset disabled={saving} className="makeup-date-time m-0 min-w-0 border-0 p-0"><StandardDateField name="makeup-date" label="Make-up date" value={start.slice(0, 10)} onChange={setDate} required /><StandardTimeField name="makeup-time" label="Time (IST)" value={start.slice(11, 16) || "09:00"} onChange={(time) => setStart(`${start.slice(0, 10) || new Date().toISOString().slice(0, 10)}T${time}`)} intervalMinutes={15} /></fieldset> : <p className="makeup-next-session">The next scheduled class will be used.</p>}
      {mode === "Manual" && <StandardSelectField name="delivery-mode" disabled={saving} value={deliveryMode} onChange={setDeliveryMode} placeholder="Delivery mode" options={[{ value: "Offline", label: "Offline" }, { value: "Online", label: "Online" }, { value: "Hybrid", label: "Hybrid" }]} />}
      {mode === "Manual" && deliveryMode === "Offline" && <input disabled={saving} value={venue} onChange={(event) => setVenue(event.target.value)} placeholder="Room or venue (optional)" />}
      {mode === "Manual" && ["Online", "Hybrid"].includes(deliveryMode) && <input disabled={saving} value={meetingLink} onChange={(event) => setMeetingLink(event.target.value)} placeholder="Meeting link" required />}
      <button disabled={saving} className="enterprise-action-button makeup-create">{saving ? "Scheduling…" : "Schedule make-up"}</button>
    </div></form><section className="makeup-panel makeup-directory"><header className="makeup-panel-header"><div><p>Directory</p><h2>Planned make-ups</h2></div><span>{makeups.length} records</span></header>{makeups.length === 0 ? <p className="makeup-empty">No make-up classes scheduled.</p> : <ul>{makeups.map((item) => <li key={item.id}><div><b>{name(students, item.studentId)} · {name(batches, item.batchId)}</b><small>{item.usesNextScheduledClass ? "Next scheduled class" : dateTime(item.startUtc)} · {item.deliveryMode}</small><small>{item.meetingLink || item.venue || "Venue not set"}</small></div><StandardSelectField name={`makeup-status-${item.id}`} disabled={saving} value={item.status} onChange={(status) => updateStatus(item, status)} placeholder="Status" options={["Scheduled", "Completed", "Cancelled"].map((status) => ({ value: status, label: status }))} /></li>)}</ul>}</section></section>
  </div></main>;
}
