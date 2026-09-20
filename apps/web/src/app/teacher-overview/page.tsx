"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { academyApi } from "@/lib/api";

type Academy = { id: string; name: string };
type Teacher = { id: string; isActive: boolean };

export default function TeacherOverviewPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [message, setMessage] = useState("Loading teacher overview…");
  useEffect(() => {
    void (async () => {
      try {
        const academies = await academyApi("/api/academies", { cache: "no-store" });
        const current = (await academies.json())[0] as Academy | undefined;
        if (!current) return setMessage("Create an academy before viewing teacher operations.");
        setAcademy(current);
        const response = await academyApi(`/api/academies/${current.id}/teachers`, { cache: "no-store" });
        if (!response.ok) throw new Error();
        setTeachers(await response.json());
        setMessage("");
      } catch { setMessage("Teacher overview could not be loaded."); }
    })();
  }, []);
  const active = teachers.filter((teacher) => teacher.isActive).length;
  return <main className="enterprise-settings teacher-standard teacher-overview-standard">
    <header className="enterprise-page-header">
      <div className="flex flex-wrap items-end justify-between gap-4"><div><p>Teachers / overview</p><h2>Teacher overview</h2><span>{academy?.name ?? "Academy"} teaching team and payment setup.</span></div><Link href="/teacher-onboarding" className="enterprise-action-button">Add teacher</Link></div>
    </header>
    {message ? <p className="enterprise-page-state">{message}</p> : <><section className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4"><Metric label="Total teachers" value={teachers.length} note={`${active} active · ${teachers.length - active} inactive`} /><Metric label="Payment details due" value={active} note="Review active teacher payment setup" /><Metric label="Teaching team" value={active} note="Available for class assignment" /><Metric label="Next pay cycle" value="1st" note="Monthly settlement review" /></section><section className="mt-5 grid gap-5 xl:grid-cols-3"><Action href="/teacher-onboarding" title="Teacher onboarding" text="Create the governed teacher record." /><Action href="/teachers" title="Teacher management" text="Manage active status and core details." /><Action href="/teacher-payments" title="Teacher payment details" text="Maintain salary and hourly rate rules." /></section></>}
  </main>;
}
function Metric({ label, value, note }: { label: string; value: string | number; note: string }) { return <article className="surface-panel rounded-xl p-5"><p className="text-sm text-slate-400">{label}</p><strong className="mt-3 block text-3xl">{value}</strong><small className="mt-2 block text-sm text-slate-400">{note}</small></article>; }
function Action({ href, title, text }: { href: string; title: string; text: string }) { return <Link href={href} className="enterprise-setting-card"><span>Teachers</span><h3>{title}</h3><p>{text}</p></Link>; }
