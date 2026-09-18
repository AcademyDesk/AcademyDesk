"use client";
import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";
type A = { id: string };
type D = {
  activeEnrolments: number;
  attendanceRisk: {
    studentId: string;
    records: number;
    attendanceRate: number;
  }[];
  occupancy: {
    id: string;
    name: string;
    activeEnrolments: number;
    waitlisted: number;
    capacity: number;
    pressure: number;
  }[];
  collections: {
    invoiceNumber: string;
    totalAmount: number;
    dueDate: string;
    daysOverdue: number;
  }[];
  workload: {
    teacherId: string;
    teacherName: string;
    scheduledHours: number;
    sessions: number;
  }[];
};
export default function Intelligence() {
  const [d, setD] = useState<D>();
  const [m, setM] = useState("Loading operational intelligence…");
  useEffect(() => {
    void (async () => {
      try {
        const a: A[] = await (await academyApi("/api/academies")).json();
        if (!a[0]) throw Error();
        setD(
          await (
            await academyApi(`/api/academies/${a[0].id}/admin-intelligence`)
          ).json(),
        );
        setM("");
      } catch {
        setM("Operational intelligence could not be loaded.");
      }
    })();
  }, []);
  return (
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <p>Administration / intelligence</p>
        <h2>Admin intelligence</h2>
        <span>
          Decision signals for intervention, capacity, collections and delivery.
        </span>
      </header>
      {m && <p className="mt-5 text-amber-200">{m}</p>}
      {d && (
        <>
          <section className="mt-5 grid gap-5 md:grid-cols-4">
            <Card t="Active enrolments" v={String(d.activeEnrolments)} />
            <Card t="Attendance risks" v={String(d.attendanceRisk.length)} />
            <Card
              t="Capacity alerts"
              v={String(d.occupancy.filter((x) => x.pressure >= 90).length)}
            />
            <Card t="Overdue invoices" v={String(d.collections.length)} />
          </section>
          <section className="mt-5 grid gap-5 xl:grid-cols-2">
            <Panel t="Attendance intervention queue">
              {d.attendanceRisk.length ? (
                d.attendanceRisk.map((x) => (
                  <p
                    key={x.studentId}
                    className="mt-3 rounded border border-amber-700/60 p-3"
                  >
                    <b>{x.attendanceRate}% attendance</b>
                    <span className="float-right">{x.records} records</span>
                    <br />
                    <Link
                      href={`/student-profile?id=${x.studentId}`}
                      className="text-sm text-cyan-300"
                    >
                      Open student record →
                    </Link>
                  </p>
                ))
              ) : (
                <p className="mt-4 text-slate-400">
                  No attendance-risk learners.
                </p>
              )}
            </Panel>
            <Panel t="Batch capacity pressure">
              {d.occupancy.map((x) => (
                <p
                  key={x.id}
                  className="mt-3 rounded border border-slate-700 p-3"
                >
                  <b>{x.name}</b>
                  <span className="float-right text-cyan-300">
                    {x.pressure}%
                  </span>
                  <br />
                  <small className="text-slate-400">
                    {x.activeEnrolments} active / {x.capacity} capacity
                  </small>
                </p>
              ))}
            </Panel>
          </section>
          <section className="mt-5 grid gap-5 xl:grid-cols-2">
            <Panel t="Collections ageing">
              {d.collections.map((x) => (
                <p
                  key={x.invoiceNumber}
                  className="mt-3 rounded border border-slate-700 p-3"
                >
                  <b>{x.invoiceNumber}</b>
                  <span className="float-right text-amber-300">
                    {x.daysOverdue} days
                  </span>
                  <br />
                  <small className="text-slate-400">
                    ₹{x.totalAmount} · due {x.dueDate}
                  </small>
                </p>
              ))}
            </Panel>
            <Panel t="Teacher workload">
              {d.workload.map((x) => (
                <p
                  key={x.teacherId}
                  className="mt-3 border-b border-slate-800 pb-3"
                >
                  Teacher {x.teacherId.slice(0, 8)}
                  <span className="float-right">
                    {x.scheduledHours} hrs · {x.sessions} sessions
                  </span>
                </p>
              ))}
            </Panel>
          </section>
        </>
      )}
    </main>
  );
}
function Card(p: { t: string; v: string }) {
  return (
    <section className="surface-panel rounded-xl p-5">
      <p className="text-sm text-slate-400">{p.t}</p>
      <b className="mt-2 block text-3xl">{p.v}</b>
    </section>
  );
}
function Panel(p: { t: string; children: React.ReactNode }) {
  return (
    <section className="surface-panel rounded-xl p-5">
      <h3 className="font-semibold">{p.t}</h3>
      {p.children}
    </section>
  );
}
