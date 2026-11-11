"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string };
type Batch = {
  id: string;
  name: string;
  teacherId?: string | null;
  branchId?: string | null;
};
type Teacher = { id: string; firstName: string; lastName: string };
type Branch = { id: string; name: string };
type Session = {
  id: string;
  batchId: string;
  teacherId?: string | null;
  branchId?: string | null;
  startUtc: string;
  endUtc: string;
  deliveryMode: string;
  roomName?: string | null;
  status: string;
};

function formatLocal(value: string) {
  return new Intl.DateTimeFormat("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "Asia/Kolkata",
  }).format(new Date(value));
}

function istInputToUtc(value: string) {
  const withSeconds = value.length === 16 ? `${value}:00` : value;
  return new Date(`${withSeconds}+05:30`).toISOString();
}

type Options = { batches: Batch[]; teachers: Teacher[]; branches: Branch[] };
function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === "object";
}
function labels(value: unknown, fields: string[]): value is Record<string, string>[] {
  return Array.isArray(value) && value.every((x) => isRecord(x) &&
    typeof x.id === "string" && x.id.length > 0 && fields.every((key) => typeof x[key] === "string")) &&
    new Set(value.map((x) => x.id)).size === value.length;
}
function validOptions(value: unknown): value is Options {
  if (!isRecord(value) || !labels(value.batches, ["name"]) ||
      !labels(value.teachers, ["firstName", "lastName"]) || !labels(value.branches, ["name"])) return false;
  const teachers = new Set(value.teachers.map((x) => x.id));
  const branches = new Set(value.branches.map((x) => x.id));
  return value.batches.every((x) => (x.teacherId == null || teachers.has(x.teacherId)) &&
    (x.branchId == null || branches.has(x.branchId)));
}
function validSessions(value: unknown): value is Session[] {
  return labels(value, ["batchId", "startUtc", "endUtc", "deliveryMode", "status"]) && value.every((x) =>
    Number.isFinite(Date.parse(x.startUtc)) && Number.isFinite(Date.parse(x.endUtc)) &&
    ["InPerson", "Online", "Hybrid"].includes(x.deliveryMode) &&
    ["Scheduled", "Completed", "Cancelled", "NoShow"].includes(x.status) &&
    (x.roomName == null || typeof x.roomName === "string") &&
    (x.teacherId == null || typeof x.teacherId === "string") &&
    (x.branchId == null || typeof x.branchId === "string"));
}

export default function SchedulePage() {
  const [academy, setAcademy] = useState<Academy>();
  const [batches, setBatches] = useState<Batch[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [batchId, setBatchId] = useState("");
  const [teacherId, setTeacherId] = useState("");
  const [branchId, setBranchId] = useState("");
  const [startLocal, setStartLocal] = useState("");
  const [endLocal, setEndLocal] = useState("");
  const [deliveryMode, setDeliveryMode] = useState("InPerson");
  const [roomName, setRoomName] = useState("");
  const [message, setMessage] = useState("Loading schedule…");
  const [savingId, setSavingId] = useState<string | null>(null);

  const [phase, setPhase] = useState<"loading" | "ready" | "error">("loading");
  const [readVersion, setReadVersion] = useState(0);
  const mounted = useRef(false);
  const reading = useRef(false);
  const pending = useRef(false);
  const sequence = useRef(0);
  const readyAcademy = useRef("");
  const initializedSelection = useRef(false);
  const ready = phase === "ready" && !!academy;
  const canWrite = () => mounted.current && ready && !reading.current && !pending.current &&
    readVersion === sequence.current && readyAcademy.current === academy?.id;

  async function load(academyId?: string) {
    const version = ++sequence.current;
    readyAcademy.current = "";
    reading.current = true;
    setReadVersion(version);
    setPhase("loading");
    const current = () => mounted.current && sequence.current === version;
    try {
      let id = academyId ?? academy?.id;
      if (!id) {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error("Please sign in with schedule access and reload.");
        const data: unknown = await response.json();
        if (!labels(data, ["name"]) || !data[0]) throw new Error("No academy is available for scheduling.");
        if (!current()) return false;
        id = data[0].id;
        setAcademy({ id, name: data[0].name });
      }
      const [optionsResponse, sessionsResponse] = await Promise.all([
        academyApi(`/api/academies/${id}/sessions/options`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/sessions`, { cache: "no-store" }),
      ]);
      if (!optionsResponse.ok || !sessionsResponse.ok) throw new Error("Schedule records could not be loaded. Check your access and reload.");
      const options: unknown = await optionsResponse.json();
      const records: unknown = await sessionsResponse.json();
      if (!validOptions(options) || !validSessions(records)) throw new Error("Schedule records were incomplete. Reload before making changes.");
      if (!current()) return false;
      setBatches(options.batches);
      setTeachers(options.teachers);
      setBranches(options.branches);
      setSessions(records);
      if (!initializedSelection.current && options.batches.length) {
        const first = options.batches[0];
        setBatchId(first.id);
        setTeacherId(first.teacherId ?? "");
        setBranchId(first.branchId ?? "");
      }
      initializedSelection.current = true;
      readyAcademy.current = id;
      setPhase("ready");
      return true;
    } catch (error) {
      if (current()) {
        setPhase("error");
        setMessage(error instanceof Error ? error.message : "Schedule records could not be loaded. Reload to try again.");
      }
      return false;
    } finally {
      if (current()) reading.current = false;
    }
  }

  function retire() { mounted.current = false; ++sequence.current; readyAcademy.current = ""; }

  useEffect(() => {
    mounted.current = true;
    void load().then((ok) => { if (ok && mounted.current) setMessage(""); });
    return retire;
  }, []);

  async function reload() {
    if (!mounted.current || pending.current || reading.current) return;
    if (await load() && mounted.current) setMessage("");
  }

  async function refusal(response: Response, fallback: string) {
    let detail = "";
    try { const data: unknown = await response.json(); if (isRecord(data) && typeof data.message === "string") detail = data.message; } catch { /* Response may have no JSON body. */ }
    return response.status >= 500 ? "The result could not be confirmed. Check the schedule before retrying." : `${fallback}${detail ? ` ${detail}` : ""}`;
  }

  function applyBatchDefaults(id: string) {
    if (!canWrite() || id === batchId) return;
    setBatchId(id);
    const batch = batches.find((item) => item.id === id);
    setTeacherId(batch?.teacherId ?? "");
    setBranchId(batch?.branchId ?? "");
  }

  async function createSession(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canWrite() || !academy || !batches.some((x) => x.id === batchId) || !startLocal || !endLocal) return;
    if (["Online", "Hybrid"].includes(deliveryMode) && !roomName.trim())
      return setMessage("A meeting link is required for online and hybrid classes.");
    let startUtc: string, endUtc: string;
    try { startUtc = istInputToUtc(startLocal); endUtc = istInputToUtc(endLocal); }
    catch { setMessage("Enter valid start and end times."); return; }
    pending.current = true;
    setSavingId("create");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/sessions`, {
        method: "POST", headers: apiHeaders(true),
        body: JSON.stringify({ batchId, teacherId: teacherId || null, branchId: branchId || null,
          startUtc, endUtc, deliveryMode, roomName: roomName || null }),
      });
      if (!mounted.current) return;
      if (!response.ok) { const notice = await refusal(response, "The session could not be saved."); if (mounted.current) setMessage(notice); return; }
      setStartLocal(""); setEndLocal(""); setRoomName("");
      const refreshed = await load();
      if (mounted.current) setMessage(refreshed ? "Session scheduled." : "Session scheduled, but records could not be refreshed. Reload the schedule before making more changes.");
    } catch {
      if (mounted.current) setMessage("The result could not be confirmed. Check the schedule before retrying.");
    } finally {
      pending.current = false;
      if (mounted.current) setSavingId(null);
    }
  }
  async function updateStatus(session: Session, status: string) {
    if (!canWrite() || !academy || !sessions.includes(session) || !["Scheduled", "Completed", "Cancelled", "NoShow"].includes(status)) return;
    pending.current = true;
    setSavingId(session.id);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/sessions/${session.id}`, {
        method: "PUT", headers: apiHeaders(true),
        body: JSON.stringify({ startUtc: session.startUtc, endUtc: session.endUtc,
          deliveryMode: session.deliveryMode, roomName: session.roomName, status }),
      });
      if (!mounted.current) return;
      if (!response.ok) { const notice = await refusal(response, "The session status could not be updated."); if (mounted.current) setMessage(notice); return; }
      const refreshed = await load();
      if (mounted.current) setMessage(refreshed ? "Session updated." : "Session updated, but records could not be refreshed. Reload the schedule before making more changes.");
    } catch {
      if (mounted.current) setMessage("The result could not be confirmed. Check the schedule before retrying.");
    } finally {
      pending.current = false;
      if (mounted.current) setSavingId(null);
    }
  }

  const batchName = (id: string) =>
    batches.find((batch) => batch.id === id)?.name ?? "Unknown batch";
  const teacherName = (id?: string | null) => {
    const teacher = teachers.find((item) => item.id === id);
    return teacher ? `${teacher.firstName} ${teacher.lastName}` : "Unassigned";
  };
  const branchName = (id?: string | null) =>
    branches.find((branch) => branch.id === id)?.name ?? "No branch";

  return (
    <main className="enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100">
      <WorkspaceNav />
      <div className="mx-auto max-w-6xl px-6 py-10">
        <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">
          Timetable
        </p>
        <h1 className="mt-3 text-4xl font-semibold tracking-tight">
          Class schedule
        </h1>
        <p className="mt-3 text-slate-300">
          Schedule individual classes for your batches. Times use India Standard
          Time (IST) and are stored safely in UTC.
        </p>
        {message && (
          <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">
            {message}
          </p>
        )}
        <button type="button" onClick={reload} disabled={phase === "loading" || savingId !== null}
          className="mt-4 rounded-lg border border-slate-700 px-4 py-2 disabled:opacity-60">Reload schedule records</button>
        <section className="mt-8 grid gap-6 lg:grid-cols-[0.9fr_1.1fr]">
          <form
            onSubmit={createSession}
            className="rounded-2xl border border-slate-800 bg-slate-900 p-6"
          >
            <h2 className="text-xl font-semibold">Schedule a class</h2>
            <fieldset disabled={!ready || savingId !== null}>
            <select
              value={batchId}
              onChange={(event) => applyBatchDefaults(event.target.value)}
              className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
              required
            >
              <option value="">Select batch</option>
              {batches.map((batch) => (
                <option key={batch.id} value={batch.id}>
                  {batch.name}
                </option>
              ))}
            </select>
            <select
              value={teacherId}
              onChange={(event) => setTeacherId(event.target.value)}
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            >
              <option value="">No teacher assigned</option>
              {teachers.map((teacher) => (
                <option key={teacher.id} value={teacher.id}>
                  {teacher.firstName} {teacher.lastName}
                </option>
              ))}
            </select>
            <select
              value={branchId}
              onChange={(event) => setBranchId(event.target.value)}
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            >
              <option value="">No branch assigned</option>
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </select>
            <div className="mt-3 grid gap-3 sm:grid-cols-2">
              <input
                type="datetime-local"
                value={startLocal}
                onChange={(event) => setStartLocal(event.target.value)}
                className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                required
              />
              <input
                type="datetime-local"
                value={endLocal}
                onChange={(event) => setEndLocal(event.target.value)}
                className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                required
              />
            </div>
            <select
              value={deliveryMode}
              onChange={(event) => setDeliveryMode(event.target.value)}
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            >
              <option value="InPerson">In person</option>
              <option value="Online">Online</option>
              <option value="Hybrid">Hybrid</option>
            </select>
            <input
              value={roomName}
              onChange={(event) => setRoomName(event.target.value)}
              required={["Online", "Hybrid"].includes(deliveryMode)}
              placeholder={
                ["Online", "Hybrid"].includes(deliveryMode)
                  ? "Meeting link (required)"
                  : "Room (optional)"
              }
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            />
            <button
              disabled={!ready || savingId !== null || !batches.some((x) => x.id === batchId)}
              className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60"
            >
              Schedule class
            </button>
            </fieldset>
          </form>
          <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
            <h2 className="text-xl font-semibold">Scheduled classes</h2>
            {!ready ? (<p className="mt-6 text-slate-400">{phase === "loading" ? "Loading schedule records…" : "Schedule records unavailable. Reload to try again."}</p>) : sessions.length === 0 ? (
              <p className="mt-6 text-slate-400">No classes scheduled yet.</p>
            ) : (
              <ul className="mt-5 space-y-3">
                {sessions.map((session) => (
                  <li
                    key={session.id}
                    className="rounded-lg border border-slate-700 bg-slate-950 p-4"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="font-medium">
                          {batchName(session.batchId)}
                        </div>
                        <div className="mt-1 text-sm text-cyan-200">
                          {formatLocal(session.startUtc)} –{" "}
                          {new Intl.DateTimeFormat("en-IN", {
                            timeStyle: "short",
                            timeZone: "Asia/Kolkata",
                          }).format(new Date(session.endUtc))}
                        </div>
                        <div className="mt-2 text-sm text-slate-300">
                          {teacherName(session.teacherId)} ·{" "}
                          {branchName(session.branchId)} ·{" "}
                          {session.deliveryMode}
                        </div>
                        {session.roomName && (
                          <div className="mt-1 text-sm text-slate-400">
                            {session.roomName}
                          </div>
                        )}
                      </div>
                      <span className="rounded-full bg-slate-800 px-2 py-1 text-xs text-slate-300">
                        {session.status}
                      </span>
                    </div>
                    <div className="mt-3 flex items-center gap-2">
                      <select
                        value={session.status}
                        onChange={(event) =>
                          void updateStatus(session, event.target.value)
                        }
                        disabled={!ready || savingId !== null}
                        className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm"
                      >
                        <option>Scheduled</option>
                        <option>Completed</option>
                        <option>Cancelled</option>
                        <option>NoShow</option>
                      </select>
                      <span className="text-xs text-slate-500">
                        Update status
                      </span>
                    </div>
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
