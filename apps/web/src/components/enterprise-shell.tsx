"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { ThemeToggle } from "@/components/theme-toggle";

const primaryNavigation = [
  ["Overview", "/dashboard", "▦"],
  ["Students", "/students", "♙"],
  ["Academics", "/courses", "♫"],
  ["Schedule", "/schedule", "◷"],
  ["Finance", "/finance", "₹"],
] as const;

const administrationNavigation = [
  ["Settings", "/", "⚙"],
  ["Activity log", "/activity", "◌"],
] as const;

const searchItems = [
  ...primaryNavigation,
  ...administrationNavigation,
  ["Teachers", "/teachers", ""], ["Batches", "/batches", ""], ["Attendance", "/attendance", ""],
  ["Communications", "/communications", ""], ["Reports", "/reports", ""], ["Holidays", "/holidays", ""],
] as const;

type EnterpriseShellProps = {
  academyName?: string;
  userName?: string;
  userRole?: string;
  children: React.ReactNode;
};

export function EnterpriseShell({ academyName, userName = "Academy owner", userRole = "Owner", children }: EnterpriseShellProps) {
  const pathname = usePathname();
  const router = useRouter();
  const [searchOpen, setSearchOpen] = useState(false);
  const [query, setQuery] = useState("");
  const results = useMemo(() => searchItems.filter(([label]) => label.toLowerCase().includes(query.trim().toLowerCase())).slice(0, 7), [query]);
  const initials = (userName || "A").split(" ").map((name) => name[0]).join("").slice(0, 2).toUpperCase();

  function signOut() {
    window.localStorage.removeItem("academydesk.accessToken");
    window.localStorage.removeItem("academydesk.refreshToken");
    router.push("/login");
  }

  return <div className="enterprise-app-shell">
    <aside className="enterprise-sidebar">
      <Link href="/dashboard" className="enterprise-brand"><span>A</span><strong>AcademyDesk</strong></Link>
      <div className="enterprise-nav-section"><p>Workspace</p>{primaryNavigation.map(([label, href, icon]) => <Link key={href} href={href} data-active={pathname === href}><i aria-hidden="true">{icon}</i>{label}</Link>)}</div>
      <div className="enterprise-nav-section enterprise-nav-section-bottom"><p>Administration</p>{administrationNavigation.map(([label, href, icon]) => <Link key={href} href={href} data-active={pathname === href}><i aria-hidden="true">{icon}</i>{label}</Link>)}</div>
    </aside>
    <section className="enterprise-workspace">
      <header className="enterprise-topbar">
        <div><p>Workspace / Overview</p><h1>{academyName || "Academy workspace"}</h1></div>
        <div className="enterprise-utilities">
          <button type="button" className="enterprise-icon-button" aria-label="Search AcademyDesk" onClick={() => setSearchOpen((open) => !open)}>⌕</button>
          <Link href="/communications" className="enterprise-icon-button" aria-label="Open communications">♧</Link>
          <ThemeToggle />
          <details className="enterprise-profile"><summary aria-label="Open account menu"><span>{initials}</span></summary><div><strong>{userName}</strong><small>{userRole}</small><button type="button" onClick={signOut}>Sign out</button></div></details>
        </div>
        {searchOpen && <div className="enterprise-search-panel"><input autoFocus value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search modules…" aria-label="Search AcademyDesk modules" />{results.map(([label, href]) => <Link key={href} href={href} onClick={() => setSearchOpen(false)}>{label}<span>Go to module →</span></Link>)}{results.length === 0 && <p>No matching modules.</p>}</div>}
      </header>
      {children}
    </section>
  </div>;
}
