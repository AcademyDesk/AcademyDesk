"use client";

import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardSelectField, StandardDateTimeField } from "@/components/design-system/controls";
import { OnboardingField } from "@/components/design-system/onboarding-field";
import styles from "@/components/design-system/scheduling-workspace.module.css";

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
  const saving = useRef(false);

  const load = useCallback(async (id: string | undefined, confirmation = "", applyInitialBatch = false) => {
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
    if (applyInitialBatch && batchData.length) {
      const initialBatch = batchData[0];
      setBatchId(initialBatch.id);
      setTeacherId(initialBatch.teacherId ?? "");
      setBranchId(initialBatch.branchId ?? "");
    }
    setMessage(confirmation);
  }, []);

  async function refreshAfterSave(confirmation: string) {
    setMessage(confirmation);
    try {
      await load(academy?.id, confirmation, !batchId);
    } catch {
      setMessage(`${confirmation} The schedule could not be refreshed. Reload to view the latest classes; do not submit the saved action again.`);
    }
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
        await load(academies[0].id, "", true);
      } catch {
        setMessage(
          "The schedule could not be loaded. Confirm the API is running on port 5092.",
        );
      }
    }
    void initialise();
  }, [load]);

  function applyBatchDefaults(id: string) {
    if (saving.current) return;
    if (id === batchId) return;
    setBatchId(id);
    const batch = batches.find((item) => item.id === id);
    setTeacherId(batch?.teacherId ?? "");
    setBranchId(batch?.branchId ?? "");
  }

  async function createSession(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (saving.current) return;
    if (!academy || !batchId || !startLocal || !endLocal) return;
    if (["Online", "Hybrid"].includes(deliveryMode) && !roomName.trim())
      return setMessage(
        "A meeting link is required for online and hybrid classes.",
      );
    saving.current = true;
    setSavingId("create");
    setMessage("Saving class…");
    try {
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
      if (!response.ok) {
        setMessage(response.status >= 500
          ? "The save could not be confirmed. Your details are retained. Check the schedule before trying again."
          : "The session could not be saved. Ensure the end time is after the start time.");
        return;
      }
      setStartLocal("");
      setEndLocal("");
      setRoomName("");
      await refreshAfterSave("Class scheduled successfully.");
    } catch {
      setMessage("The save could not be confirmed. Your details are retained. Check the schedule before trying again.");
    } finally {
      saving.current = false;
      setSavingId(null);
    }
  }
  async function updateStatus(session: Session, status: string) {
    if (!academy || saving.current) return;
    saving.current = true;
    setSavingId(session.id);
    setMessage("Updating class status…");
    try {
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
      if (!response.ok) {
        setMessage(response.status >= 500
          ? "The status update could not be confirmed. Check the schedule before trying again."
          : "The session status could not be updated.");
        return;
      }
      await refreshAfterSave(`Class status updated to ${status}.`);
    } catch {
      setMessage("The status update could not be confirmed. Check the schedule before trying again.");
    } finally {
      saving.current = false;
      setSavingId(null);
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
    <main className={`enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100 ${styles.page}`}>
      <WorkspaceNav />
      <div className={styles.scheduleBody}>
        <header className={styles.scheduleHeading}><span aria-hidden="true">▦</span><div><p>Class &amp; batch</p><h1>Class schedule</h1></div></header>
        {message && (
          <p role="status" aria-live="polite" aria-atomic="true" className="mt-6 rounded-lg border border-slate-700 bg-slate-900 p-4 text-sm text-slate-100">
            {message}
          </p>
        )}
        <section className={styles.scheduleLayout}>
          <form
            onSubmit={createSession}
            className={styles.scheduleForm}
            aria-busy={savingId === "create"}
          >
            <h2 className="text-xl font-semibold">Schedule a class</h2>
            <StandardSelectField
              name="schedule-batch" label="Batch (required)" placeholder="Select batch"
              value={batchId}
              disabled={savingId !== null}
              onChange={applyBatchDefaults}
              options={batches.map(batch => ({ value: batch.id, label: batch.name }))}
            />
            <StandardSelectField
              name="schedule-teacher" label="Teacher (optional)" placeholder="No teacher assigned"
              value={teacherId}
              disabled={savingId !== null}
              onChange={setTeacherId}
              options={teachers.map(teacher => ({ value: teacher.id, label: `${teacher.firstName} ${teacher.lastName}` }))}
            />
            <StandardSelectField
              name="schedule-branch" label="Branch (optional)" placeholder="No branch assigned"
              value={branchId}
              disabled={savingId !== null}
              onChange={setBranchId}
              options={branches.map(branch => ({ value: branch.id, label: branch.name }))}
            />
            <div className="mt-3 grid gap-3 sm:grid-cols-2">
              <StandardDateTimeField name="schedule-start" label="Start (IST)"
                value={startLocal}
                disabled={savingId !== null}
                onChange={setStartLocal}
              />
              <StandardDateTimeField name="schedule-end" label="End (IST)"
                value={endLocal}
                disabled={savingId !== null}
                onChange={setEndLocal}
              />
            </div>
            <StandardSelectField
              name="schedule-mode" label="Delivery mode" placeholder="Select delivery mode"
              value={deliveryMode}
              disabled={savingId !== null}
              onChange={setDeliveryMode}
              options={[{value:"InPerson",label:"Offline"},{value:"Online",label:"Online"},{value:"Hybrid",label:"Hybrid"}]}
            />
            <OnboardingField label={["Online", "Hybrid"].includes(deliveryMode) ? "Meeting link (required)" : "Room (optional)"}><input
              value={roomName}
              disabled={savingId !== null}
              aria-label={["Online", "Hybrid"].includes(deliveryMode) ? "Meeting link" : "Room"}
              onChange={(event) => setRoomName(event.target.value)}
              required={["Online", "Hybrid"].includes(deliveryMode)}
              placeholder={
                ["Online", "Hybrid"].includes(deliveryMode)
                  ? "Meeting link (required)"
                  : "Room (optional)"
              }
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            /></OnboardingField>
            <button
              disabled={!academy || batches.length === 0 || savingId !== null}
              className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60"
            >
              {savingId === "create" ? "Saving class…" : "Schedule class"}
            </button>
          </form>
          <section className={styles.sessionPanel}>
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
                      <StandardSelectField
                        name={`session-status-${session.id}`} label={`Status for ${batchName(session.batchId)}`} placeholder="Select class status"
                        value={session.status}
                        onChange={(value) =>
                          { if (value) void updateStatus(session, value); }
                        }
                        disabled={savingId !== null}
                        options={["Scheduled", "Completed", "Cancelled", "NoShow"].map(status => ({value:status,label:status}))}
                      />
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
