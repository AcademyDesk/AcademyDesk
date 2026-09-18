"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { ThemeToggle } from "@/components/theme-toggle";
import { useEnterpriseShell } from "@/components/enterprise-shell";

const navigationGroups = [
  { label: "Overview", links: [["Dashboard", "/dashboard"], ["Academy profile", "/"], ["Branches", "/branches"], ["Activity", "/activity"], ["Reports", "/reports"]] },
  { label: "People", links: [["Leads", "/leads"], ["Students", "/students"], ["Parents", "/guardians"], ["Teachers", "/teachers"], ["Staff", "/staff"], ["Portal accounts", "/portal-accounts"]] },
  { label: "Academic delivery", links: [["Courses", "/courses"], ["Curriculum", "/curriculum"], ["Batches", "/batches"], ["Enrolments", "/enrollments"], ["Submission review", "/submission-review"], ["Assessments", "/assessments"], ["Resources", "/resources"], ["Music", "/music"]] },
  { label: "Operations", links: [["Schedule", "/schedule"], ["Calendar", "/calendar"], ["Attendance", "/attendance"], ["Leave", "/leave"], ["Make-up classes", "/makeup"], ["Holidays", "/holidays"], ["Events", "/events"], ["Certificates", "/certificates"]] },
  { label: "Finance", links: [["Fee plans", "/fee-plans"], ["Invoices", "/invoices"], ["Payments", "/payments"], ["Fee reminders", "/fee-reminders"], ["Expenses", "/expenses"], ["Finance overview", "/finance"]] },
  { label: "Communication", links: [["Messages", "/communications"], ["Templates", "/message-templates"], ["Contact preferences", "/communication-preferences"], ["Channel settings", "/communication-settings"]] },
  { label: "Role workspaces", links: [["Teacher portal", "/teacher"], ["Family portal", "/portal"]] },
] as const;

export function WorkspaceNav() {
  const isInsideEnterpriseShell = useEnterpriseShell();
  const pathname = usePathname();
  const router = useRouter();
  if (isInsideEnterpriseShell) return null;

  function signOut() {
    window.localStorage.removeItem("academydesk.accessToken");
    window.localStorage.removeItem("academydesk.refreshToken");
    router.push("/login");
  }

  return (
    <header className="enterprise-nav sticky top-0 z-40 px-4 py-3 sm:px-6">
      <div className="mx-auto flex max-w-[1600px] flex-wrap items-center gap-3">
        <Link href="/dashboard" className="mr-2 flex items-center gap-2 font-semibold tracking-tight" data-active={pathname === "/dashboard"}>
          <span className="grid h-8 w-8 place-items-center rounded-lg bg-blue-600 text-sm font-bold text-white">A</span>
          <span>AcademyDesk</span>
          <span className="hidden rounded border px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wider sm:inline">ERP</span>
        </Link>

        <nav className="flex flex-1 flex-wrap items-center gap-x-4 gap-y-2" aria-label="Workspace navigation">
          {navigationGroups.map((group) => (
            <details key={group.label} className="enterprise-nav-group">
              <summary className="text-sm font-medium" data-active={group.links.some(([, href]) => pathname === href)}>{group.label} <span aria-hidden="true">⌄</span></summary>
              <div className="enterprise-nav-menu">
                {group.links.map(([label, href]) => <Link key={href} href={href} data-active={pathname === href} aria-current={pathname === href ? "page" : undefined}>{label}</Link>)}
              </div>
            </details>
          ))}
        </nav>

        <div className="ml-auto flex items-center gap-3">
          <ThemeToggle />
          <Link href="/activity" className="hidden text-sm font-medium md:inline">Activity</Link>
          <button type="button" onClick={signOut} className="enterprise-signout text-sm font-medium">Sign out</button>
        </div>
      </div>
    </header>
  );
}
