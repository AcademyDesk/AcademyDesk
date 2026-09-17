"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";

const links = [
  ["Dashboard", "/dashboard"],
  ["Academy", "/"],
  ["Leads", "/leads"],
  ["Music", "/music"],
  ["Practice logs", "/practice-logs"],
  ["Events", "/events"],
  ["Certificates", "/certificates"],
  ["Branches", "/branches"],
  ["Students", "/students"],
  ["Student 360", "/student-profile"],
  ["Guardians", "/guardians"],
  ["Family 360", "/guardian-profile"],
  ["Portal accounts", "/portal-accounts"],
  ["Teachers", "/teachers"],
  ["Teacher 360", "/teacher-profile"],
  ["Staff", "/staff"],
  ["Teacher portal", "/teacher"],
  ["Courses", "/courses"],
  ["Curriculum", "/curriculum"],
  ["Lesson plans", "/lesson-plans"],
  ["Batches", "/batches"],
  ["Enrolments", "/enrollments"],
  ["Schedule", "/schedule"],
  ["Calendar", "/calendar"],
  ["Attendance", "/attendance"],
  ["Leave", "/leave"],
  ["Make-up", "/makeup"],
  ["Assignments", "/assignments"],
  ["Resources", "/resources"],
  ["Assessments", "/assessments"],
  ["Messages", "/communications"],
  ["Communication settings", "/communication-settings"],
  ["Message templates", "/message-templates"],
  ["Contact preferences", "/communication-preferences"],
  ["Fees", "/fee-plans"],
  ["Invoices", "/invoices"],
  ["Payments", "/payments"],
  ["Reminders", "/fee-reminders"],
  ["Expenses", "/expenses"],
  ["Finance", "/finance"],
  ["Activity", "/activity"],
  ["Reports", "/reports"],
] as const;

export function WorkspaceNav() {
  const pathname = usePathname();
  const router = useRouter();

  function signOut() {
    window.localStorage.removeItem("academydesk.accessToken");
    window.localStorage.removeItem("academydesk.refreshToken");
    router.push("/login");
  }

  return (
    <header className="border-b border-slate-800 bg-slate-950/90 px-6 py-4 text-slate-100 backdrop-blur">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-7 gap-y-3">
        <Link href="/dashboard" className="font-semibold tracking-tight text-cyan-300">AcademyDesk</Link>
        <nav className="flex flex-1 flex-wrap gap-x-4 gap-y-2 text-sm text-slate-300">
          {links.map(([label, href]) => (
            <Link key={href} href={href} className={pathname === href ? "font-medium text-white" : "hover:text-white"}>{label}</Link>
          ))}
        </nav>
        <button onClick={signOut} className="text-sm text-slate-300 hover:text-white">Sign out</button>
      </div>
    </header>
  );
}
