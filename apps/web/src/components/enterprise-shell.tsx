"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { ThemeToggle } from "@/components/theme-toggle";
import { academyApi } from "@/lib/api";

type NavigationItem = readonly [label: string, href: string];
type NavigationGroup = {
  label: string;
  icon: string;
  links: readonly NavigationItem[];
};

const navigationGroups: readonly NavigationGroup[] = [
  {
    label: "Workspace",
    icon: "▦",
    links: [
      ["Overview", "/dashboard"],
      ["Calendar", "/calendar"],
      ["Activity log", "/activity"],
      ["Reports", "/reports"],
    ],
  },
  {
    label: "Students",
    icon: "♙",
    links: [
      ["Overview", "/students"],
      ["Student onboarding", "/student-onboarding"],
    ],
  },
  {
    label: "Teachers",
    icon: "♜",
    links: [
      ["Overview", "/teachers"],
      ["Teacher onboarding", "/teacher-onboarding"],
      ["Lesson plans", "/lesson-plans"],
      ["Assignments", "/assignments"],
      ["Leave", "/leave"],
    ],
  },
  {
    label: "Classes & batches",
    icon: "♫",
    links: [
      ["Overview", "/batches"],
      ["Create class or batch", "/batch-setup"],
      ["Class scheduling", "/schedule"],
      ["Attendance", "/attendance"],
      ["Make-up classes", "/makeup"],
    ],
  },
  {
    label: "Ops team",
    icon: "♞",
    links: [
      ["Staff directory & onboarding", "/staff"],
      ["Portal access", "/portal-accounts"],
      ["Access review", "/access-review"],
    ],
  },
  {
    label: "Admissions",
    icon: "◌",
    links: [
      ["Leads", "/leads"],
      ["Enrolment pipeline", "/enrollments"],
    ],
  },
  {
    label: "Academics",
    icon: "♫",
    links: [
      ["Academic governance", "/academic-governance"],
      ["Academic periods", "/academic-periods"],
      ["Courses", "/courses"],
      ["Curriculum", "/curriculum"],
      ["Batch promotions", "/batch-promotions"],
      ["Submission review", "/submission-review"],
      ["Assessments", "/assessments"],
      ["Assessment policy", "/assessment-governance"],
      ["Music progress", "/music"],
      ["Practice logs", "/practice-logs"],
    ],
  },
  {
    label: "Operations",
    icon: "◷",
    links: [
      ["Holidays", "/holidays"],
      ["Events", "/events"],
      ["Certificates", "/certificates"],
      ["Resources", "/resources"],
    ],
  },
  {
    label: "Finance",
    icon: "₹",
    links: [
      ["Finance overview", "/finance"],
      ["Finance summary", "/finance-summary"],
      ["Finance policy", "/finance-policy"],
      ["Adjustment requests", "/finance-adjustments"],
      ["Finance controls", "/finance-governance"],
      ["Reconciliation", "/finance-reconciliation"],
      ["Fee plans", "/fee-plans"],
      ["Invoices", "/invoices"],
      ["Payments", "/payments"],
      ["Fee reminders", "/fee-reminders"],
      ["Expenses", "/expenses"],
    ],
  },
  {
    label: "Engagement",
    icon: "✦",
    links: [
      ["Messages", "/communications"],
      ["Templates", "/message-templates"],
      ["Contact preferences", "/communication-preferences"],
      ["Channel settings", "/communication-settings"],
    ],
  },
] as const;

const administrationNavigation: readonly NavigationItem[] = [
  ["Academy control", "/admin/control"],
  ["Admin intelligence", "/admin-intelligence"],
  ["Data operations", "/data-operations"],
  ["Access review", "/access-review"],
  ["Work queue", "/work-queue"],
  ["Compliance centre", "/compliance"],
  ["Settings", "/settings"],
  ["Branches", "/branches"],
  ["Activity log", "/activity"],
];
const searchItems: readonly NavigationItem[] = Array.from(
  new Map(
    [...navigationGroups.flatMap((group) => group.links), ...administrationNavigation].map(
      ([label, href]) => [href, [label, href] as NavigationItem],
    ),
  ).values(),
);

type EnterpriseShellProps = {
  academyName?: string;
  userName?: string;
  userRole?: string;
  children: React.ReactNode;
};

const EnterpriseShellContext = createContext(false);
export function useEnterpriseShell() {
  return useContext(EnterpriseShellContext);
}

export function EnterpriseShell({
  academyName,
  userName,
  userRole,
  children,
}: EnterpriseShellProps) {
  const pathname = usePathname();
  const router = useRouter();
  const [searchOpen, setSearchOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [workspaceName, setWorkspaceName] = useState<string>();
  const [account, setAccount] = useState<{
    displayName: string;
    roles: string[];
  }>();
  const financeOnly =
    account?.roles.includes("FinanceUser") &&
    !account.roles.some((role) =>
      ["Owner", "AcademyAdmin", "Manager"].includes(role),
    );
  const activeNavigationGroups = financeOnly
    ? navigationGroups.filter((group) => group.label === "Finance")
    : navigationGroups;
  const activeAdministrationNavigation = financeOnly
    ? []
    : administrationNavigation;
  const activeSearchItems = financeOnly
    ? activeNavigationGroups.flatMap((group) => group.links)
    : searchItems;
  const results = useMemo(
    () =>
      activeSearchItems
        .filter(([label]) =>
          label.toLowerCase().includes(query.trim().toLowerCase()),
        )
        .slice(0, 7),
    [query, financeOnly],
  );
  const resolvedAcademyName =
    academyName || workspaceName || "Academy workspace";
  const resolvedUserName =
    userName || account?.displayName || "Academy administrator";
  const resolvedUserRole = userRole || account?.roles?.[0] || "Academy Admin";
  const initials = (resolvedUserName || "A")
    .split(" ")
    .map((name) => name[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  useEffect(() => {
    void Promise.all([
      academyApi("/api/academies", { cache: "no-store" }),
      academyApi("/api/auth/session", { cache: "no-store" }),
    ])
      .then(async ([academyResponse, sessionResponse]) => {
        if (academyResponse.ok) {
          const academies = await academyResponse.json();
          setWorkspaceName(academies[0]?.name);
        }
        if (sessionResponse.ok) setAccount(await sessionResponse.json());
      })
      .catch(() => undefined);
  }, []);

  function signOut() {
    window.localStorage.removeItem("academydesk.accessToken");
    window.localStorage.removeItem("academydesk.refreshToken");
    router.push("/login");
  }

  return (
    <EnterpriseShellContext.Provider value>
      <div className="enterprise-app-shell">
        <aside className="enterprise-sidebar">
          <Link href="/dashboard" className="enterprise-brand">
            <span>A</span>
            <strong>AcademyDesk</strong>
          </Link>
          <nav
            className="enterprise-nav-section"
            aria-label="AcademyDesk modules"
          >
            <p>{financeOnly ? "Finance workspace" : "Workspace"}</p>
            {activeNavigationGroups.map((group) => (
              <details
                key={group.label}
                className="enterprise-module-group"
                open={group.links.some(([, href]) => href === pathname)}
              >
                <summary>
                  <i aria-hidden="true">{group.icon}</i>
                  <span>{group.label}</span>
                  <b aria-hidden="true">⌄</b>
                </summary>
                <div>
                  {group.links.map(([label, href]) => (
                    <Link
                      key={href}
                      href={href}
                      data-active={pathname === href}
                    >
                      {label}
                    </Link>
                  ))}
                </div>
              </details>
            ))}
          </nav>
          {!financeOnly && (
            <nav
              className="enterprise-nav-section enterprise-administration-section"
              aria-label="Administration"
            >
              <p>Administration</p>
              <details
                className="enterprise-module-group enterprise-administration-group"
                open={activeAdministrationNavigation.some(
                  ([, href]) => href === pathname,
                )}
              >
                <summary>
                  <i aria-hidden="true">⚙</i>
                  <span>Administration controls</span>
                  <b aria-hidden="true">⌄</b>
                </summary>
                <div>
                  {activeAdministrationNavigation.map(([label, href]) => (
                    <Link
                      key={href}
                      href={href}
                      data-active={pathname === href}
                    >
                      {label}
                    </Link>
                  ))}
                </div>
              </details>
            </nav>
          )}
        </aside>
        <section className="enterprise-workspace">
          <header className="enterprise-topbar">
            <div />
            <div className="enterprise-utilities">
              <button
                type="button"
                className="enterprise-icon-button"
                aria-label="Search AcademyDesk"
                onClick={() => setSearchOpen((open) => !open)}
              >
                ⌕
              </button>
              <Link
                href="/communications"
                className="enterprise-icon-button"
                aria-label="Open communications"
              >
                ♧
              </Link>
              <ThemeToggle />
              <details className="enterprise-profile">
                <summary aria-label="Open account menu">
                  <span>{initials}</span>
                </summary>
                <div>
                  <strong>{resolvedUserName}</strong>
                  <small>{resolvedUserRole}</small>
                  <button type="button" onClick={signOut}>
                    Sign out
                  </button>
                </div>
              </details>
            </div>
            {searchOpen && (
              <div className="enterprise-search-panel">
                <input
                  autoFocus
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder="Search modules…"
                  aria-label="Search AcademyDesk modules"
                />
                {results.map(([label, href]) => (
                  <Link
                    key={href}
                    href={href}
                    onClick={() => setSearchOpen(false)}
                  >
                    {label}
                    <span>Go to module →</span>
                  </Link>
                ))}
                {results.length === 0 && <p>No matching modules.</p>}
              </div>
            )}
          </header>
          {children}
        </section>
      </div>
    </EnterpriseShellContext.Provider>
  );
}
