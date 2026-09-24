"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";

type Academy = { id: string; name: string };
type Teacher = { id: string; firstName: string; lastName: string; isActive: boolean };
type Session = { id: string; teacherId?: string | null; batchId: string; startUtc: string; status: string };
type LeaveRequest = { id: string; requesterType: string; teacherId?: string | null; startDate: string; endDate: string; reason: string; status: string };
type Batch = { id: string; name: string };

export default function TeacherOverviewPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [leaveRequests, setLeaveRequests] = useState<LeaveRequest[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [message, setMessage] = useState("Loading teacher overview…");
  useEffect(() => {
    void (async () => {
      try {
        const academies = await academyApi("/api/academies", { cache: "no-store" });
        const current = (await academies.json())[0] as Academy | undefined;
        if (!current) return setMessage("Create an academy before viewing teacher operations.");
        setAcademy(current);
        const [teachersResponse, sessionsResponse, leaveResponse, batchesResponse] = await Promise.all([
          academyApi(`/api/academies/${current.id}/teachers`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/sessions`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/leave-requests`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/batches`, { cache: "no-store" }),
        ]);
        if (!teachersResponse.ok) throw new Error();
        setTeachers(await teachersResponse.json());
        setSessions(sessionsResponse.ok ? await sessionsResponse.json() : []);
        setLeaveRequests(leaveResponse.ok ? await leaveResponse.json() : []);
        setBatches(batchesResponse.ok ? await batchesResponse.json() : []);
        setMessage("");
      } catch { setMessage("Teacher overview could not be loaded."); }
    })();
  }, []);
  const active = teachers.filter((teacher) => teacher.isActive).length;
  const today = indiaDate(new Date());
  const classesToday = sessions.filter((session) => indiaDate(new Date(session.startUtc)) === today && session.status !== "Cancelled");
  const teachersWithClasses = [...new Set(classesToday.map((session) => session.teacherId).filter((id): id is string => Boolean(id)))];
  const teacherLeaveRequests = leaveRequests.filter((request) => request.requesterType === "Teacher");
  const pendingLeaveRequests = teacherLeaveRequests.filter((request) => request.status === "Pending");
  const teachersOnLeave = teacherLeaveRequests.filter((request) => request.status === "Approved" && request.startDate <= today && request.endDate >= today);
  const teacherName = (teacherId?: string | null) => {
    const teacher = teachers.find((item) => item.id === teacherId);
    return teacher ? `${teacher.firstName} ${teacher.lastName}`.trim() : "Teacher";
  };
  const batchName = (batchId: string) => batches.find((batch) => batch.id === batchId)?.name ?? "Class";
  return <main className="enterprise-settings teacher-standard teacher-overview-standard">
    <header className="teacher-overview-heading">
      <div className="teacher-overview-title"><span className="teacher-overview-title-icon" aria-hidden="true">♜</span><div><p>Teachers</p><h1>Teacher overview</h1><span>{academy?.name ?? "Academy"} teaching team and payment setup.</span></div></div><Link href="/teacher-onboarding" className="enterprise-action-button">Add teacher</Link>
    </header>
    {message ? <p className="enterprise-page-state teacher-overview-message">{message}</p> : <><section className="teacher-overview-kpis"><Metric label="Total teachers" value={teachers.length} note={`${active} active · ${teachers.length - active} inactive`} /><Metric label="Payment details due" value={active} note="Review active teacher payment setup" /><Metric label="Teaching team" value={active} note="Available for class assignment" /><Metric label="Next pay cycle" value="1st" note="Monthly settlement review" /></section><section className="teacher-overview-insights-grid" aria-label="Teaching and leave summary"><Insight title="Teachers with classes today" value={teachersWithClasses.length} details={teachersWithClasses.length ? teachersWithClasses.map((teacherId) => `${teacherName(teacherId)} · ${classesToday.filter((session) => session.teacherId === teacherId).map((session) => batchName(session.batchId)).join(", ")}`).join(" · ") : "No teachers have classes scheduled today."} /><Insight title="Leave requests awaiting review" value={pendingLeaveRequests.length} details={pendingLeaveRequests.length ? pendingLeaveRequests.map((request) => `${teacherName(request.teacherId)} · ${request.startDate}`).join(" · ") : "No teacher leave requests awaiting review."} /><Insight title="Teachers on leave today" value={teachersOnLeave.length} details={teachersOnLeave.length ? teachersOnLeave.map((request) => `${teacherName(request.teacherId)} · ${request.reason || "Approved leave"}`).join(" · ") : "No teachers are on approved leave today."} /></section></>}
  </main>;
}
function Metric({ label, value, note }: { label: string; value: string | number; note: string }) { return <article className="teacher-overview-kpi"><span>{label}</span><strong>{value}</strong><small>{note}</small></article>; }
function Insight({ title, value, details }: { title: string; value: string | number; details: string }) { return <article className="teacher-overview-insight"><span>Teachers</span><div><h3>{title}</h3><strong>{value}</strong></div><p>{details}</p></article>; }
function indiaDate(value: Date) { return new Intl.DateTimeFormat("en-CA", { timeZone: "Asia/Kolkata" }).format(value); }
