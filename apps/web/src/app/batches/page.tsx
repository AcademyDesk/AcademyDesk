"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { StandardDetailModal, StandardInteractiveTile } from "@/components/design-system/interactive";

type Academy = { id: string; name: string };
type Course = { id: string; name: string; academyType: string };
type Teacher = { id: string; firstName: string; lastName: string };
type Branch = { id: string; name: string };
type Batch = {
  id: string;
  name: string;
  batchCode?: string | null;
  courseId: string;
  teacherId?: string | null;
  branchId?: string | null;
  capacity: number;
  waitlistCapacity?: number;
  deliveryMode?: string;
  meetingPattern?: string | null;
  roomName?: string | null;
  enrollmentStatus?: string;
  activeEnrolments?: number;
  startDate?: string | null;
  endDate?: string | null;
  isActive: boolean;
};

function Metric({
  label,
  value,
  note,
  onClick,
}: {
  label: string;
  value: number;
  note?: string;
  onClick: () => void;
}) {
  return <StandardInteractiveTile className="batch-overview-kpi" label={label} value={value} detail={note ?? "View details"} onClick={onClick} />;
}

export default function BatchesPage() {
  const pathname = usePathname();
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [academyId, setAcademyId] = useState("");
  const [courses, setCourses] = useState<Course[]>([]);
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [courseId, setCourseId] = useState("");
  const [branchId, setBranchId] = useState("");
  const [name, setName] = useState("");
  const [capacity, setCapacity] = useState("10");
  const [batchCode, setBatchCode] = useState("");
  const [waitlistCapacity, setWaitlistCapacity] = useState("0");
  const [deliveryMode, setDeliveryMode] = useState("InPerson");
  const [classType, setClassType] = useState("Group");
  const [sessionMinutes, setSessionMinutes] = useState("60");
  const [sessionsPerWeek, setSessionsPerWeek] = useState("1");
  const [meetingLink, setMeetingLink] = useState("");
  const [meetingDays, setMeetingDays] = useState<string[]>([]);
  const [meetingTimes, setMeetingTimes] = useState<Record<string, string>>({});
  const [roomName, setRoomName] = useState("");
  const [enrollmentStatus, setEnrollmentStatus] = useState("Open");
  const [startDate, setStartDate] = useState("");
  const [message, setMessage] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editCourseId, setEditCourseId] = useState("");
  const [editTeacherId, setEditTeacherId] = useState("");
  const [editBranchId, setEditBranchId] = useState("");
  const [editCapacity, setEditCapacity] = useState("10");
  const [editStartDate, setEditStartDate] = useState("");
  const [savingId, setSavingId] = useState<string | null>(null);
  const [detail, setDetail] = useState<"classes" | "teachers" | "capacity" | null>(null);

  async function loadAcademies() {
    const response = await academyApi("/api/academies", { cache: "no-store" });
    if (!response.ok) throw new Error();
    const data: Academy[] = await response.json();
    setAcademies(data);
    if (!academyId && data.length) setAcademyId(data[0].id);
  }

  async function loadWorkspace(id: string) {
    if (!id) return;
    const [courseResponse, teacherResponse, branchResponse, batchResponse] =
      await Promise.all([
        academyApi(`/api/academies/${id}/courses`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/teachers`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/branches`, { cache: "no-store" }),
        academyApi(`/api/academies/${id}/batches`, { cache: "no-store" }),
      ]);
    if (
      ![courseResponse, teacherResponse, branchResponse, batchResponse].every(
        (response) => response.ok,
      )
    )
      throw new Error();
    const courseData: Course[] = await courseResponse.json();
    setCourses(courseData);
    setTeachers(await teacherResponse.json());
    setBranches(await branchResponse.json());
    setBatches(await batchResponse.json());
    if (!courseId && courseData.length) setCourseId(courseData[0].id);
  }

  useEffect(() => {
    void loadAcademies().catch(() =>
      setMessage(
        "Please sign in and make sure the AcademyDesk API is running.",
      ),
    );
  }, []);
  useEffect(() => {
    void loadWorkspace(academyId).catch(() =>
      setMessage("Batch information could not be loaded."),
    );
  }, [academyId]);

  async function createBatch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academyId || !courseId) {
      setMessage("Select an academy and course before creating the batch.");
      return;
    }
    const response = await academyApi(`/api/academies/${academyId}/batches`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify({
        name,
        batchCode: batchCode || null,
        courseId,
        teacherId: null,
        branchId: branchId || null,
        capacity: Number(capacity),
        waitlistCapacity: Number(waitlistCapacity),
        deliveryMode,
        classType,
        sessionMinutes: Number(sessionMinutes),
        sessionsPerWeek: Number(sessionsPerWeek),
        meetingDaysJson: JSON.stringify(meetingDays.map((day) => ({ day, startTime: meetingTimes[day] }))),
        meetingLink: meetingLink || null,
        meetingPattern: meetingDays.map((day) => `${day} ${meetingTimes[day] ?? ""}`.trim()).join(" · ") || null,
        roomName: roomName || null,
        enrollmentStatus,
        adminNotes: null,
        startDate: startDate || null,
        endDate: null,
      }),
    });
    if (!response.ok)
      return setMessage(
        "The batch could not be saved. Check the course, teacher, and date fields.",
      );
    setName("");
    setBatchCode("");
    setBranchId("");
    setWaitlistCapacity("0");
    setDeliveryMode("InPerson");
    setMeetingTimes({});
    setRoomName("");
    setEnrollmentStatus("Open");
    setStartDate("");
    setMessage("");
    await loadWorkspace(academyId);
  }
  function beginEdit(batch: Batch) {
    setEditingId(batch.id);
    setEditName(batch.name);
    setEditCourseId(batch.courseId);
    setEditTeacherId(batch.teacherId ?? "");
    setEditBranchId(batch.branchId ?? "");
    setEditCapacity(String(batch.capacity));
    setEditStartDate(batch.startDate ?? "");
  }
  async function saveBatch(batch: Batch) {
    if (!editName.trim() || !editCourseId)
      return setMessage("Batch name and course are required.");
    setSavingId(batch.id);
    const response = await academyApi(
      `/api/academies/${academyId}/batches/${batch.id}`,
      {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          name: editName,
          courseId: editCourseId,
          teacherId: editTeacherId || null,
          branchId: editBranchId || null,
          capacity: Number(editCapacity),
          startDate: editStartDate || null,
          endDate: null,
          isActive: batch.isActive,
        }),
      },
    );
    setSavingId(null);
    if (!response.ok) return setMessage("The batch could not be updated.");
    setEditingId(null);
    setMessage("Batch updated.");
    await loadWorkspace(academyId);
  }
  async function toggleActive(batch: Batch) {
    setSavingId(batch.id);
    const response = await academyApi(
      `/api/academies/${academyId}/batches/${batch.id}`,
      {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          name: batch.name,
          courseId: batch.courseId,
          teacherId: batch.teacherId,
          branchId: batch.branchId,
          capacity: batch.capacity,
          startDate: batch.startDate,
          endDate: batch.endDate,
          isActive: !batch.isActive,
        }),
      },
    );
    setSavingId(null);
    if (!response.ok)
      return setMessage("The batch status could not be updated.");
    setMessage(
      batch.isActive ? "Batch marked inactive." : "Batch reactivated.",
    );
    await loadWorkspace(academyId);
  }

  const teacherName = (id?: string | null) => {
    const teacher = teachers.find((item) => item.id === id);
    return teacher ? `${teacher.firstName} ${teacher.lastName}` : "Unassigned";
  };
  const branchName = (id?: string | null) =>
    branches.find((branch) => branch.id === id)?.name ?? "No branch";

  if (pathname === "/batches") {
    const active = batches.filter((batch) => batch.isActive);
    const unassigned = active.filter((batch) => !batch.teacherId);
    const assigned = active.filter((batch) => batch.teacherId);
    const oneToOne = active.filter((batch) => batch.capacity === 1);
    const groupClasses = active.filter((batch) => batch.capacity > 1);
    const capacityGaps = groupClasses.filter((batch) => (batch.activeEnrolments ?? 0) < batch.capacity);
    const courseName = (id: string) => courses.find((course) => course.id === id)?.name ?? "Subject not set";
    const detailRows = (rows: Batch[], mode: "classes" | "teachers" | "capacity") => <div className="standard-detail-list">{rows.map((batch) => <div key={batch.id}><strong>{batch.name}</strong><span>{mode === "teachers" ? teacherName(batch.teacherId) : mode === "capacity" ? `${batch.activeEnrolments ?? 0} of ${batch.capacity} filled · ${batch.capacity - (batch.activeEnrolments ?? 0)} student${batch.capacity - (batch.activeEnrolments ?? 0) === 1 ? "" : "s"} needed` : `${courseName(batch.courseId)} · ${batch.capacity === 1 ? "1:1" : `Group · capacity ${batch.capacity}`} · ${batch.activeEnrolments ?? 0} students · ${teacherName(batch.teacherId)}`}</span></div>)}</div>;
    return (
      <main className="enterprise-settings enterprise-legacy-standard batch-overview-standard min-h-screen bg-slate-950 text-slate-100">
        <WorkspaceNav />
        <div className="batch-overview-content mx-auto max-w-6xl px-6 py-10">
          <header className="batch-overview-heading">
            <div className="batch-overview-title"><span className="batch-overview-title-icon" aria-hidden="true">♫</span><div><p>Class &amp; batch</p><h1>Overview</h1></div></div>
            <Link
              href="/batch-setup"
              className="enterprise-action-button"
            >
              Create Class / Batch
            </Link>
          </header>
          {message && (
            <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">
              {message}
            </p>
          )}
          <section className="batch-overview-kpis">
            <Metric label="Active classes" value={active.length} note={`${oneToOne.length} 1:1 · ${groupClasses.length} group`} onClick={() => setDetail("classes")} />
            <Metric
              label="Teacher assignment"
              value={assigned.length}
              note={`${unassigned.length} unassigned`}
              onClick={() => setDetail("teachers")}
            />
            <Metric
              label="Capacity"
              value={capacityGaps.length}
              note="Classes with places open"
              onClick={() => setDetail("capacity")}
            />
          </section>
          {detail === "classes" && <StandardDetailModal eyebrow="Class & batch" title="Active classes" onClose={() => setDetail(null)}><div className="standard-detail-content"><section className="standard-detail-group"><h3>1:1 classes · {oneToOne.length}</h3>{oneToOne.length ? detailRows(oneToOne, "classes") : <p className="standard-detail-empty">No active 1:1 classes.</p>}</section><section className="standard-detail-group"><h3>Group classes · {groupClasses.length}</h3>{groupClasses.length ? detailRows(groupClasses, "classes") : <p className="standard-detail-empty">No active group classes.</p>}</section></div></StandardDetailModal>}
          {detail === "teachers" && <StandardDetailModal eyebrow="Class & batch" title="Teaching assignment" onClose={() => setDetail(null)}><div className="standard-detail-content"><section className="standard-detail-group"><h3>Assigned · {assigned.length}</h3>{assigned.length ? detailRows(assigned, "teachers") : <p className="standard-detail-empty">No teachers are assigned.</p>}</section><section className="standard-detail-group"><h3>Unassigned · {unassigned.length}</h3>{unassigned.length ? detailRows(unassigned, "teachers") : <p className="standard-detail-empty">Every active class has a teacher.</p>}</section></div></StandardDetailModal>}
          {detail === "capacity" && <StandardDetailModal eyebrow="Class & batch" title="Classes with places open" onClose={() => setDetail(null)}><div className="standard-detail-content">{capacityGaps.length ? detailRows(capacityGaps, "capacity") : <p className="standard-detail-empty">There are no group classes with open places.</p>}</div></StandardDetailModal>}
          <section className="batch-overview-panel">
            <header className="batch-overview-panel-header"><div><p>Delivery</p><h2>Active classes and batches</h2></div><span>{active.length} active</span></header>
            {active.length ? (
              <ul className="batch-overview-list">
                {active.map((batch) => (
                  <li
                    key={batch.id}
                    className="batch-overview-row"
                  >
                    <div>
                      <b>{batch.name}</b>
                      <p>
                        {teacherName(batch.teacherId)} ·{" "}
                        {batch.activeEnrolments ?? 0}/{batch.capacity} enrolled
                        · {batch.deliveryMode ?? "Offline"}
                      </p>
                    </div>
                    <Link href="/batch-setup" className="batch-overview-row-action">
                      Manage class
                    </Link>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="batch-overview-empty">No active classes or batches.</p>
            )}
          </section>
        </div>
      </main>
    );
  }

  return (
    <main className="enterprise-settings enterprise-legacy-standard batch-setup-standard min-h-screen bg-slate-950 text-slate-100">
      <WorkspaceNav />
      <div className="batch-setup-content mx-auto max-w-6xl px-6 py-10">
        <header className="batch-setup-heading"><div className="batch-setup-title"><span className="batch-setup-title-icon" aria-hidden="true">+</span><div><p>Class &amp; batch</p><h1>Create Class / Batch</h1></div></div></header>
        {message && (
          <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">
            {message}
          </p>
        )}
        {academies.length === 0 ? (
          <p className="mt-8 rounded-lg border border-dashed border-slate-700 p-8 text-center text-slate-400">
            Create an academy and course first.
          </p>
        ) : (
          <>
            <section className="batch-setup-academy">
              <span>Academy</span>
            <StandardSelectField
              name="academy"
              value={academyId}
              onChange={setAcademyId}
              placeholder="Select academy"
              options={academies.map((academy) => ({ value: academy.id, label: academy.name }))}
            />
            </section>
            <section className="batch-setup-layout">
              <form onSubmit={createBatch} className="batch-setup-panel">
                <header className="batch-setup-panel-header"><div><p>Setup</p><h2>Create Class / Batch</h2></div></header>
                <div className="batch-setup-form-fields">
                <StandardSelectField
                  name="course"
                  value={courseId}
                  onChange={setCourseId}
                  placeholder="Select course"
                  options={courses.map((course) => ({ value: course.id, label: `${course.name} · ${course.academyType}` }))}
                />
                <input
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  placeholder="Batch name, e.g. Piano Level 1 – Evening"
                  className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                  required
                />
                <input
                  value={batchCode}
                  onChange={(event) => setBatchCode(event.target.value)}
                  placeholder="Batch code, e.g. PNO-L1-EVE"
                  className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                />
                <StandardSelectField
                  name="branch"
                  value={branchId}
                  onChange={setBranchId}
                  placeholder="No branch assigned"
                  options={branches.map((branch) => ({ value: branch.id, label: branch.name }))}
                />
                <input
                  type="number"
                  min="1"
                  max="1000"
                  value={capacity}
                  onChange={(event) => setCapacity(event.target.value)}
                  placeholder="Capacity"
                  className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                  required
                />
                <div className="mt-3 grid gap-3 sm:grid-cols-2">
                  <StandardSelectField
                    name="class-type"
                    value={classType}
                    onChange={setClassType}
                    placeholder="Class type"
                    options={[{ value: "Group", label: "Group class" }, { value: "OneToOne", label: "1:1 class" }]}
                  />
                  <StandardSelectField
                    name="delivery-mode"
                    value={deliveryMode}
                    onChange={setDeliveryMode}
                    placeholder="Delivery mode"
                    options={[{ value: "InPerson", label: "Offline" }, { value: "Online", label: "Online" }, { value: "Hybrid", label: "Hybrid" }]}
                  />
                </div>
                <fieldset className="mt-3">
                  <legend className="text-sm text-slate-400">
                    Teaching days
                  </legend>
                  <div className="mt-2 flex flex-wrap gap-2">
                    {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map(
                      (day) => (
                        <label key={day} className="consent-check">
                          <input
                            type="checkbox"
                            checked={meetingDays.includes(day)}
                            onChange={() =>
                              setMeetingDays((days) => {
                                if (days.includes(day)) {
                                  setMeetingTimes((times) => { const { [day]: _, ...remaining } = times; return remaining; });
                                  return days.filter((item) => item !== day);
                                }
                                return [...days, day];
                              })
                            }
                          />
                          {day}
                        </label>
                      ),
                    )}
                  </div>
                </fieldset>
                {meetingDays.length > 0 && (
                  <div className="mt-3 grid gap-3 sm:grid-cols-2">
                    {meetingDays.map((day) => (
                      <label key={day} className="text-sm text-slate-300">
                        {day} class time
                        <input
                          required
                          type="time"
                          value={meetingTimes[day] ?? ""}
                          onChange={(event) => setMeetingTimes((times) => ({ ...times, [day]: event.target.value }))}
                          className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                        />
                      </label>
                    ))}
                  </div>
                )}
                <div className="mt-3 grid gap-3 sm:grid-cols-2">
                  <input
                    type="number"
                    min="15"
                    step="15"
                    value={sessionMinutes}
                    onChange={(event) => setSessionMinutes(event.target.value)}
                    placeholder="Class duration (minutes)"
                    className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                  />
                  <input
                    type="number"
                    min="1"
                    max="7"
                    value={sessionsPerWeek}
                    onChange={(event) => setSessionsPerWeek(event.target.value)}
                    placeholder="Sessions per week"
                    className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                  />
                </div>
                {["Online", "Hybrid"].includes(deliveryMode) && (
                  <input
                    value={meetingLink}
                    onChange={(event) => setMeetingLink(event.target.value)}
                    required
                    placeholder="Meeting link (required)"
                    className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                  />
                )}
                <div className="mt-3">
                  <input
                    value={roomName}
                    onChange={(event) => setRoomName(event.target.value)}
                    placeholder="Room / online location"
                    className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                  />
                </div>
                <input
                  type="number"
                  min="0"
                  max="1000"
                  value={waitlistCapacity}
                  onChange={(event) => setWaitlistCapacity(event.target.value)}
                  placeholder="Waitlist capacity"
                  className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                />
                <StandardDateField name="batch-start-date" label="Batch start date" value={startDate} onChange={setStartDate} />
                <button className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300">
                  Create batch
                </button>
                </div>
              </form>
              <section className="batch-setup-panel batch-directory-panel">
                <header className="batch-setup-panel-header"><div><p>Directory</p><h2>Batches</h2></div><span>{batches.length} records</span></header>
                {batches.length === 0 ? (
                  <p className="mt-6 text-slate-400">No batches yet.</p>
                ) : (
                  <ul className="mt-4 space-y-3">
                    {batches.map((batch) =>
                      editingId === batch.id ? (
                        <li
                          key={batch.id}
                          className="rounded-lg border border-cyan-700/60 bg-slate-950 p-4"
                        >
                          <div className="grid gap-2">
                            <input
                              value={editName}
                              onChange={(e) => setEditName(e.target.value)}
                              className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                            />
                            <StandardSelectField
                              name="edit-course"
                              value={editCourseId}
                              onChange={setEditCourseId}
                              placeholder="Select course"
                              options={courses.map((course) => ({ value: course.id, label: course.name }))}
                            />
                            <StandardSelectField
                              name="edit-teacher"
                              value={editTeacherId}
                              onChange={setEditTeacherId}
                              placeholder="No teacher"
                              options={teachers.map((teacher) => ({ value: teacher.id, label: `${teacher.firstName} ${teacher.lastName}` }))}
                            />
                            <StandardSelectField
                              name="edit-branch"
                              value={editBranchId}
                              onChange={setEditBranchId}
                              placeholder="No branch"
                              options={branches.map((branch) => ({ value: branch.id, label: branch.name }))}
                            />
                            <input
                              type="number"
                              min="1"
                              max="1000"
                              value={editCapacity}
                              onChange={(e) => setEditCapacity(e.target.value)}
                              className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                            />
                            <StandardDateField name="edit-batch-start-date" label="Batch start date" value={editStartDate} onChange={setEditStartDate} />
                          </div>
                          <div className="mt-3 flex gap-2">
                            <button
                              onClick={() => void saveBatch(batch)}
                              disabled={savingId === batch.id}
                              className="rounded-lg bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950"
                            >
                              Save
                            </button>
                            <button
                              onClick={() => setEditingId(null)}
                              className="rounded-lg border border-slate-700 px-3 py-2 text-sm"
                            >
                              Cancel
                            </button>
                          </div>
                        </li>
                      ) : (
                        <li
                          key={batch.id}
                          className="rounded-lg border border-slate-700 bg-slate-950 p-4"
                        >
                          <div className="flex items-start justify-between">
                            <div>
                              <div className="font-medium">{batch.name}</div>
                              <div className="mt-2 text-sm text-slate-300">
                                {teacherName(batch.teacherId)} ·{" "}
                                {branchName(batch.branchId)}
                              </div>
                              <div className="mt-1 text-sm text-slate-400">
                                Capacity: {batch.capacity}
                                {batch.startDate
                                  ? ` · Starts ${batch.startDate}`
                                  : ""}
                              </div>
                            </div>
                            <span
                              className={`rounded-full px-2 py-1 text-xs ${batch.isActive ? "bg-emerald-950 text-emerald-300" : "bg-slate-800 text-slate-400"}`}
                            >
                              {batch.isActive ? "Active" : "Inactive"}
                            </span>
                          </div>
                          <div className="mt-3 flex gap-2">
                            <button
                              onClick={() => beginEdit(batch)}
                              className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm"
                            >
                              Edit
                            </button>
                            <button
                              onClick={() => void toggleActive(batch)}
                              disabled={savingId === batch.id}
                              className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm"
                            >
                              {batch.isActive ? "Deactivate" : "Reactivate"}
                            </button>
                          </div>
                        </li>
                      ),
                    )}
                  </ul>
                )}
              </section>
            </section>
          </>
        )}
      </div>
    </main>
  );
}
