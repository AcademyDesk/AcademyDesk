"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
import { StandardDetailModal, StandardInteractiveTile } from "@/components/design-system/controls";

type Academy = { id: string; name: string };
type Teacher = { id: string; firstName: string; lastName: string; specialties?: string | null; isActive: boolean };
type Session = { id: string; teacherId?: string | null; batchId: string; startUtc: string; status: string };
type LeaveRequest = { id: string; requesterType: string; teacherId?: string | null; startDate: string; endDate: string; reason: string; status: string };
type Batch = { id: string; name: string };
type PayrollPayout = { workerName: string; periodLabel: string; netAmount: number; status: string; paidAtUtc: string };

export default function TeacherOverviewPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [sessions, setSessions] = useState<Session[]>([]);
  const [leaveRequests, setLeaveRequests] = useState<LeaveRequest[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [payroll, setPayroll] = useState<PayrollPayout[]>([]);
  const [detail, setDetail] = useState<"teachers" | "due" | "team" | "upcoming" | null>(null);
  const [message, setMessage] = useState("Loading teacher overview…");
  useEffect(() => {
    void (async () => {
      try {
        const academies = await academyApi("/api/academies", { cache: "no-store" });
        const current = (await academies.json())[0] as Academy | undefined;
        if (!current) return setMessage("Create an academy before viewing teacher operations.");
        setAcademy(current);
        const [teachersResponse, sessionsResponse, leaveResponse, batchesResponse, payrollResponse] = await Promise.all([
          academyApi(`/api/academies/${current.id}/teachers`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/sessions`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/leave-requests`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/batches`, { cache: "no-store" }),
          academyApi(`/api/academies/${current.id}/payroll/payouts`, { cache: "no-store" }),
        ]);
        if (!teachersResponse.ok) throw new Error();
        setTeachers(await teachersResponse.json());
        setSessions(sessionsResponse.ok ? await sessionsResponse.json() : []);
        setLeaveRequests(leaveResponse.ok ? await leaveResponse.json() : []);
        setBatches(batchesResponse.ok ? await batchesResponse.json() : []);
        setPayroll(payrollResponse.ok ? await payrollResponse.json() : []);
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
  const pendingPayroll = payroll.filter((payout) => payout.status !== "Paid");
  const money = (value: number) => `₹${value.toLocaleString("en-IN")}`;
  return <main className="enterprise-settings teacher-standard teacher-overview-standard">
    <header className="teacher-overview-heading">
      <div className="teacher-overview-title"><span className="teacher-overview-title-icon" aria-hidden="true">♜</span><div><p>Teachers</p><h1>Teacher overview</h1><span>{academy?.name ?? "Academy"} teaching team and payment setup.</span></div></div><Link href="/teacher-onboarding" className="enterprise-action-button">Add teacher</Link>
    </header>
    {message ? <p className="enterprise-page-state teacher-overview-message">{message}</p> : <><section className="teacher-overview-kpis"><StandardInteractiveTile label="Total teachers" value={teachers.length} detail={`${active} active · ${teachers.length - active} inactive`} onClick={() => setDetail("teachers")} className="teacher-overview-kpi" /><StandardInteractiveTile label="Payment details due" value={pendingPayroll.length} detail="View teacher payments due" onClick={() => setDetail("due")} className="teacher-overview-kpi" /><StandardInteractiveTile label="Teaching team" value={active} detail="View teachers and subjects" onClick={() => setDetail("team")} className="teacher-overview-kpi" /><StandardInteractiveTile label="Next pay cycle" value="Upcoming" detail="View upcoming teacher payments" onClick={() => setDetail("upcoming")} className="teacher-overview-kpi" /></section><section className="teacher-overview-insights-grid" aria-label="Teaching and leave summary"><Insight title="Teachers with classes today" value={teachersWithClasses.length} details={teachersWithClasses.length ? teachersWithClasses.map((teacherId) => `${teacherName(teacherId)} · ${classesToday.filter((session) => session.teacherId === teacherId).map((session) => batchName(session.batchId)).join(", ")}`).join(" · ") : "No teachers have classes scheduled today."} /><Insight title="Leave requests awaiting review" value={pendingLeaveRequests.length} details={pendingLeaveRequests.length ? pendingLeaveRequests.map((request) => `${teacherName(request.teacherId)} · ${request.startDate}`).join(" · ") : "No teacher leave requests awaiting review."} /><Insight title="Teachers on leave today" value={teachersOnLeave.length} details={teachersOnLeave.length ? teachersOnLeave.map((request) => `${teacherName(request.teacherId)} · ${request.reason || "Approved leave"}`).join(" · ") : "No teachers are on approved leave today."} /></section></>}
    {detail === "teachers" && <StandardDetailModal eyebrow="Teacher overview" title="Teachers" onClose={() => setDetail(null)}><TeacherDetails rows={teachers.map((teacher) => [`${teacher.firstName} ${teacher.lastName}`, teacher.isActive ? "Active" : "Inactive"])} empty="No teachers found." /></StandardDetailModal>}
    {detail === "due" && <StandardDetailModal eyebrow="Teacher overview" title="Teacher payments due" onClose={() => setDetail(null)}><TeacherDetails rows={pendingPayroll.map((payout) => [payout.workerName, `${payout.periodLabel} · ${money(payout.netAmount)} · ${payout.status}`])} empty="No teacher payments are due." /></StandardDetailModal>}
    {detail === "team" && <StandardDetailModal eyebrow="Teacher overview" title="Teaching team" onClose={() => setDetail(null)}><TeacherDetails rows={teachers.filter((teacher) => teacher.isActive).map((teacher) => [`${teacher.firstName} ${teacher.lastName}`, teacher.specialties || "Subjects not recorded"])} empty="No active teachers." /></StandardDetailModal>}
    {detail === "upcoming" && <StandardDetailModal eyebrow="Teacher overview" title="Upcoming teacher payments" onClose={() => setDetail(null)}><TeacherDetails rows={pendingPayroll.map((payout) => [payout.workerName, `${payout.periodLabel} · ${money(payout.netAmount)}`])} empty="No upcoming teacher payments." /></StandardDetailModal>}
  </main>;
}
function Insight({ title, value, details }: { title: string; value: string | number; details: string }) { return <article className="teacher-overview-insight"><span>Teachers</span><div><h3>{title}</h3><strong>{value}</strong></div><p>{details}</p></article>; }
function TeacherDetails({ rows, empty }: { rows: [string, string][]; empty: string }) { return rows.length ? <div className="standard-detail-list">{rows.map(([title, detail], index) => <article key={`${title}-${index}`}><b>{title}</b><small>{detail}</small></article>)}</div> : <p className="standard-detail-empty">{empty}</p>; }
function indiaDate(value: Date) { return new Intl.DateTimeFormat("en-CA", { timeZone: "Asia/Kolkata" }).format(value); }
