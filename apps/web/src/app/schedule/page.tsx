"use client";

import { FormEvent, useEffect, useState } from "react";
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

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [batchResponse, teacherResponse, branchResponse, sessionResponse] =
      await Promise.all([
        academyApi(`/api/academies/${id}/batches`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/teachers`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/branches`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/sessions`, { cache: "no-store" }),
      ]);
    if (
      ![batchResponse, teacherResponse, branchResponse, sessionResponse].every(
        (response) => response.ok,
      )
    )
      throw new Error();
    const batchData: Batch[] = await batchResponse.json();
    setBatches(batchData);
    setTeachers(await teacherResponse.json());
    setBranches(await branchResponse.json());
    setSessions(await sessionResponse.json());
    if (!batchId && batchData.length) {
      const initialBatch = batchData[0];
      setBatchId(initialBatch.id);
      setTeacherId(initialBatch.teacherId ?? "");
      setBranchId(initialBatch.branchId ?? "");
    }
    setMessage("");
  }

  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (response.status === 401)
          return setMessage("Please sign in before opening the schedule.");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0])
          return setMessage(
            "Create an academy, course, and batch before scheduling a class.",
          );
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setMessage(
          "The schedule could not be loaded. Confirm the API is running on port 5092.",
        );
      }
    }
    void initialise();
  }, []);

  function applyBatchDefaults(id: string) {
    if (id === batchId) return;
    setBatchId(id);
    const batch = batches.find((item) => item.id === id);
    setTeacherId(batch?.teacherId ?? "");
    setBranchId(batch?.branchId ?? "");
  }

  async function createSession(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !batchId || !startLocal || !endLocal) return;
    if (["Online", "Hybrid"].includes(deliveryMode) && !roomName.trim())
      return setMessage(
        "A meeting link is required for online and hybrid classes.",
      );
    const response = await academyApi(`/api/academies/${academy.id}/sessions`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify({
        batchId,
        teacherId: teacherId || null,
        branchId: branchId || null,
        startUtc: istInputToUtc(startLocal),
        endUtc: istInputToUtc(endLocal),
        deliveryMode,
        roomName: roomName || null,
      }),
    });
    if (!response.ok)
      return setMessage(
        "The session could not be saved. Ensure the end time is after the start time.",
      );
    setStartLocal("");
    setEndLocal("");
    setRoomName("");
    setMessage("");
    await load();
  }
  async function updateStatus(session: Session, status: string) {
    if (!academy) return;
    setSavingId(session.id);
    const response = await academyApi(
      `/api/academies/${academy.id}/sessions/${session.id}`,
      {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          startUtc: session.startUtc,
          endUtc: session.endUtc,
          deliveryMode: session.deliveryMode,
          roomName: session.roomName,
          status,
        }),
      },
    );
    setSavingId(null);
    if (!response.ok)
      return setMessage("The session status could not be updated.");
    setMessage("Session updated.");
    await load();
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
        <section className="mt-8 grid gap-6 lg:grid-cols-[0.9fr_1.1fr]">
          <form
            onSubmit={createSession}
            className="rounded-2xl border border-slate-800 bg-slate-900 p-6"
          >
            <h2 className="text-xl font-semibold">Schedule a class</h2>
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
              disabled={!academy || batches.length === 0}
              className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60"
            >
              Schedule class
            </button>
          </form>
          <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
            <h2 className="text-xl font-semibold">Scheduled classes</h2>
            {sessions.length === 0 ? (
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
                        disabled={savingId === session.id}
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
