"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { createContext, useContext, useEffect, useMemo, useRef, useState } from "react";
import { ThemeToggle } from "@/components/theme-toggle";
import { academyApi, apiUrl, clearPortalTokens } from "@/lib/api";

type NavigationItem = readonly [label: string, href: string];
type AcademySubscription = {
  subscriptionPlan?: string;
  subscriptionStatus?: string;
  enabledModulesJson?: string;
};
type NavigationGroup = {
  label: string;
  icon: string;
  links?: readonly NavigationItem[];
  sections?: readonly { label: string; links: readonly NavigationItem[] }[];
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
      ["Overview", "/student-overview"],
      ["Onboarding", "/student-onboarding"],
      ["Management", "/students"],
      ["Fee Details", "/student-fees"],
      ["360", "/student-management"],
    ],
  },
  {
    label: "Teachers",
    icon: "♜",
    links: [
      ["Overview", "/teacher-overview"],
      ["Onboarding", "/teacher-onboarding"],
      ["Management", "/teachers"],
      ["Payment Details", "/teacher-payments"],
      ["360", "/teacher-profile"],
    ],
  },
  {
    label: "Class & Batch",
    icon: "♫",
    links: [
      ["Overview", "/batches"],
      ["Create Class / Batch", "/batch-setup"],
      ["Attendance", "/attendance"],
      ["Make-up classes", "/makeup"],
      ["Meeting links", "/meeting-links"],
    ],
  },
  {
    label: "Operations & Workforce",
    icon: "♞",
    links: [
      ["Staff directory & onboarding", "/staff"],
      ["Portal access", "/portal-accounts"],
      ["Access review", "/access-review"],
    ],
  },
  {
    label: "Sales & Marketing",
    icon: "◌",
    links: [
      ["Sales overview", "/sales-marketing"],
      ["Leads", "/leads"],
      ["Campaigns", "/sales-campaigns"],
      ["Lead sources", "/sales-marketing?view=sources"],
      ["Follow-ups", "/sales-marketing?view=follow-ups"],
      ["Conversion dashboard", "/sales-marketing?view=conversion"],
      ["Referral tracking", "/sales-marketing?view=referrals"],
      ["Trial-class bookings", "/trial-bookings"],
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
    ],
  },
  {
    label: "Academy Experience",
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
    sections: [
      { label: "Overview & controls", links: [["Finance overview", "/finance"], ["Finance summary", "/finance-summary"], ["Finance controls", "/finance-governance"], ["Finance policy", "/finance-policy"]] },
      { label: "Billing & collections", links: [["Fee plans", "/fee-plans"], ["Invoices", "/invoices"], ["Payments", "/payments"], ["Fee reminders", "/fee-reminders"], ["Adjustment requests", "/finance-adjustments"], ["Reconciliation", "/finance-reconciliation"]] },
      { label: "Payroll & expenses", links: [["Teacher & staff payouts", "/payroll"], ["Expenses", "/expenses"]] },
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
  ["Platform billing & support", "/platform-services"],
  ["Admin intelligence", "/admin-intelligence"],
  ["Data operations", "/data-operations"],
  ["Work queue", "/work-queue"],
  ["Compliance centre", "/compliance"],
  ["Branches", "/branches"],
  ["Activity log", "/activity"],
];
const searchItems: readonly NavigationItem[] = Array.from(
  new Map(
    [...navigationGroups.flatMap((group) => group.links ?? group.sections?.flatMap((section) => section.links) ?? []), ...administrationNavigation].map(
      ([label, href]) => [href, [label, href] as NavigationItem],
    ),
  ).values(),
);

const routeModule = (href: string) => {
  const path = href.split("?")[0];
  if (["/sales-marketing", "/leads", "/sales-campaigns", "/trial-bookings"].includes(path)) return "Sales";
  if (["/communications", "/message-templates", "/communication-preferences", "/communication-settings"].includes(path)) return "Engagement";
  if (["/academic-governance", "/academic-periods", "/courses", "/curriculum", "/batch-promotions", "/submission-review", "/assessments", "/assessment-governance", "/music"].includes(path)) return "AcademicGovernance";
  if (["/holidays", "/events", "/certificates", "/resources"].includes(path)) return "Certificates";
  if (["/branches"].includes(path)) return "MultiBranch";
  if (["/access-review", "/data-operations", "/compliance"].includes(path)) return "AccessGovernance";
  if (path.startsWith("/finance")) return path === "/finance-governance" || path === "/finance-adjustments" ? "FinanceControls" : "Finance";
  if (["/fee-plans", "/invoices", "/payments", "/fee-reminders", "/payroll", "/expenses"].includes(path)) return "Finance";
  return "Core";
};

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
  const searchParams = useSearchParams();
  const router = useRouter();
  const [searchOpen, setSearchOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [workspaceName, setWorkspaceName] = useState<string>();
  const [subscription, setSubscription] = useState<AcademySubscription>();
  const [upgradeModule, setUpgradeModule] = useState<string>();
  const [account, setAccount] = useState<{
    displayName: string;
    roles: string[];
    profileImageUrl?: string | null;
  }>();
  const [profileOpen, setProfileOpen] = useState(false);
  const [profileMessage, setProfileMessage] = useState("");
  const [announcements, setAnnouncements] = useState<{ id: string; title: string; message: string }[]>([]);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const profileRef = useRef<HTMLDivElement>(null);
  const notificationsRef = useRef<HTMLDivElement>(null);
  const mobileWorkspaceNavRef = useRef<HTMLElement>(null);
  const profileImageInputRef = useRef<HTMLInputElement>(null);
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
    ? activeNavigationGroups.flatMap((group) => group.links ?? group.sections?.flatMap((section) => section.links) ?? [])
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
  const profileImageUrl = account?.profileImageUrl
    ? account.profileImageUrl.startsWith("http")
      ? account.profileImageUrl
      : `${apiUrl}${account.profileImageUrl}`
    : undefined;

  useEffect(() => {
    void Promise.all([
      academyApi("/api/academies", { cache: "no-store" }),
      academyApi("/api/auth/session", { cache: "no-store" }),
    ])
      .then(async ([academyResponse, sessionResponse]) => {
        if (academyResponse.ok) {
          const academies: (AcademySubscription & { name?: string })[] = await academyResponse.json();
          setWorkspaceName(academies[0]?.name);
          setSubscription(academies[0]);
        }
        if (sessionResponse.ok) setAccount(await sessionResponse.json());
      })
      .catch(() => undefined);
  }, []);
  useEffect(() => {
    let mounted = true;
    const loadAnnouncements = async () => {
      const response = await academyApi("/api/portal/announcements", { cache: "no-store" }).catch(() => undefined);
      if (!mounted || !response?.ok) return;
      const next = await response.json().catch(() => null);
      if (mounted && Array.isArray(next)) setAnnouncements(next);
    };
    void loadAnnouncements();
    const interval = window.setInterval(() => void loadAnnouncements(), 30_000);
    window.addEventListener("focus", loadAnnouncements);
    return () => {
      mounted = false;
      window.clearInterval(interval);
      window.removeEventListener("focus", loadAnnouncements);
    };
  }, []);

  const enabledModules = useMemo(() => {
    try {
      return new Set<string>(JSON.parse(subscription?.enabledModulesJson || "[\"Core\"]"));
    } catch {
      return new Set<string>(["Core"]);
    }
  }, [subscription?.enabledModulesJson]);
  const moduleIncluded = (href: string) => {
    const module = routeModule(href);
    return module === "Core" || enabledModules.has(module);
  };
  const currentModuleIncluded = moduleIncluded(pathname);
  const moduleLabel = (module: string) => ({
    Sales: "Sales & Marketing", Engagement: "Engagement", Finance: "Finance",
    FinanceControls: "Finance controls", AcademicGovernance: "Academics",
    Certificates: "Academy Experience", MultiBranch: "Multi-branch management",
    AccessGovernance: "Access governance",
  }[module] || module);
  const subscriptionLabel = subscription?.subscriptionPlan || "your current";

  function isNavigationActive(href: string) {
    const [path, query] = href.split("?");
    if (pathname !== path) return false;
    if (!query) return searchParams.size === 0;
    const expected = new URLSearchParams(query);
    return Array.from(expected.entries()).every(
      ([key, value]) => searchParams.get(key) === value,
    );
  }

  const currentNavigationLabel = (() => {
    for (const group of activeNavigationGroups) {
      for (const item of group.links ?? []) {
        if (isNavigationActive(item[1])) return item[0];
      }
      for (const section of group.sections ?? []) {
        for (const item of section.links) {
          if (isNavigationActive(item[1])) return item[0];
        }
      }
    }
    for (const item of activeAdministrationNavigation) {
      if (isNavigationActive(item[1])) return item[0];
    }
    return "Browse workspace";
  })();

  function LockedNavigationItem({ item }: { item: NavigationItem }) {
    const [label, href] = item;
    const included = moduleIncluded(href);
    if (included) return <Link href={href} data-active={isNavigationActive(href)}>{label}</Link>;
    return <button type="button" className="enterprise-locked-link" onClick={() => setUpgradeModule(routeModule(href))} aria-label={`${label} requires an upgrade`}><span>{label}</span><i aria-hidden="true">⌁</i></button>;
  }

  useEffect(() => {
    if (!profileOpen) return;
    function closeOnOutsidePress(event: MouseEvent) {
      if (!profileRef.current?.contains(event.target as Node))
        setProfileOpen(false);
    }
    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === "Escape") setProfileOpen(false);
    }
    document.addEventListener("mousedown", closeOnOutsidePress);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("mousedown", closeOnOutsidePress);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [profileOpen]);

  useEffect(() => {
    if (!notificationsOpen) return;
    const closeOnOutsidePress = (event: MouseEvent) => { if (!notificationsRef.current?.contains(event.target as Node)) setNotificationsOpen(false); };
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === "Escape") setNotificationsOpen(false); };
    document.addEventListener("mousedown", closeOnOutsidePress); document.addEventListener("keydown", closeOnEscape);
    return () => { document.removeEventListener("mousedown", closeOnOutsidePress); document.removeEventListener("keydown", closeOnEscape); };
  }, [notificationsOpen]);

  useEffect(() => setProfileOpen(false), [pathname]);
  useEffect(() => setNotificationsOpen(false), [pathname]);
  useEffect(() => {
    const menu = mobileWorkspaceNavRef.current?.querySelector("details");
    if (menu) menu.open = false;
  }, [pathname, searchParams]);

  async function uploadProfileImage(event: React.ChangeEvent<HTMLInputElement>) {
    const image = event.target.files?.[0];
    event.target.value = "";
    if (!image) return;
    setProfileMessage("Uploading image…");
    const body = new FormData();
    body.append("image", image);
    const response = await academyApi("/api/auth/session/profile-image", {
      method: "POST",
      body,
    });
    const result = await response.json().catch(() => null);
    if (!response.ok)
      return setProfileMessage(result?.message ?? "Profile image could not be saved.");
    setAccount((current) =>
      current ? { ...current, profileImageUrl: result.profileImageUrl } : current,
    );
    setProfileMessage("");
    setProfileOpen(false);
  }

  function signOut() {
    clearPortalTokens();
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
                open={(group.links ?? group.sections?.flatMap((section) => section.links) ?? []).some(([, href]) => isNavigationActive(href))}
              >
                <summary>
                  <i aria-hidden="true">{group.icon}</i>
                  <span>{group.label}</span>
                  <b aria-hidden="true">⌄</b>
                </summary>
                <div>
                  {group.sections?.map((section) => <section key={section.label} className="enterprise-nav-subgroup"><p>{section.label}</p>{section.links.map((item) => <LockedNavigationItem key={item[1]} item={item} />)}</section>)}
                  {(group.links ?? []).map((item) => <LockedNavigationItem key={item[1]} item={item} />)}
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
                open={activeAdministrationNavigation.some(([, href]) => isNavigationActive(href))}
              >
                <summary>
                  <i aria-hidden="true">⚙</i>
                  <span>Administration controls</span>
                  <b aria-hidden="true">⌄</b>
                </summary>
                <div>
                  {activeAdministrationNavigation.map((item) => <LockedNavigationItem key={item[1]} item={item} />)}
                </div>
              </details>
            </nav>
          )}
        </aside>
        <section className="enterprise-workspace" data-route={pathname}>
          <header className="enterprise-topbar">
            <div className="enterprise-admin-context">
              <span>{resolvedUserRole}</span>
              <strong>{resolvedUserName}</strong>
            </div>
            <div className="enterprise-utilities">
              <button
                type="button"
                className="enterprise-icon-button"
                aria-label="Search AcademyDesk"
                onClick={() => setSearchOpen((open) => !open)}
              >
                ⌕
              </button>
              {moduleIncluded("/communications") ? <Link
                href="/communications"
                className="enterprise-icon-button"
                aria-label="Open communications"
              >
                ♧
              </Link> : <button
                type="button"
                className="enterprise-icon-button enterprise-icon-button-locked"
                aria-label="Communications requires an upgrade"
                onClick={() => setUpgradeModule("Engagement")}
              >
                ♧
              </button>}
              <div className="enterprise-notifications" ref={notificationsRef}>
                <button type="button" className="enterprise-icon-button" aria-label="Open notifications" aria-expanded={notificationsOpen} onClick={() => setNotificationsOpen(open => !open)}>♢{announcements.length > 0 && <em>{announcements.length > 9 ? "9+" : announcements.length}</em>}</button>
                {notificationsOpen && <section className="enterprise-notification-menu" role="menu"><header><strong>Notifications</strong><small>{announcements.length ? `${announcements.length} important` : "All caught up"}</small></header>{announcements.length ? announcements.map(item => <article key={item.id}><b>{item.title}</b><span>{item.message}</span></article>) : <p>No notifications yet.</p>}</section>}
              </div>
              <ThemeToggle />
              <div className="enterprise-profile" ref={profileRef}>
                <button
                  type="button"
                  className="enterprise-profile-trigger"
                  aria-label="Open account menu"
                  aria-expanded={profileOpen}
                  onClick={() => {
                    setProfileMessage("");
                    setProfileOpen((open) => !open);
                  }}
                >
                  {profileImageUrl ? (
                    <img src={profileImageUrl} alt="Profile" />
                  ) : (
                    <span>{initials}</span>
                  )}
                </button>
                {profileOpen && <div className="enterprise-profile-menu" role="menu">
                  <strong>{resolvedUserName}</strong>
                  <small>{resolvedUserRole}</small>
                  <button
                    type="button"
                    className="enterprise-profile-menu-action"
                    onClick={() => profileImageInputRef.current?.click()}
                  >
                    Edit profile picture
                  </button>
                  {profileMessage && <small role="status">{profileMessage}</small>}
                  <button type="button" className="enterprise-profile-menu-action" onClick={signOut}>
                    Sign out
                  </button>
                </div>}
                <input
                  ref={profileImageInputRef}
                  className="sr-only"
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  onChange={uploadProfileImage}
                />
              </div>
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
                {results.map(([label, href]) => moduleIncluded(href) ? (
                  <Link key={href} href={href} onClick={() => setSearchOpen(false)}>{label}<span>Go to module →</span></Link>
                ) : (
                  <button key={href} type="button" onClick={() => { setSearchOpen(false); setUpgradeModule(routeModule(href)); }}>{label}<span>Upgrade required</span></button>
                ))}
                {results.length === 0 && <p>No matching modules.</p>}
              </div>
            )}
          </header>
          <nav
            ref={mobileWorkspaceNavRef}
            className="enterprise-mobile-workspace-nav"
            aria-label="Academy workspace navigation"
            onClick={(event) => {
              const target = event.target as HTMLElement;
              if (!target.closest("a, button")) return;
              const menu = mobileWorkspaceNavRef.current?.querySelector("details");
              if (menu) menu.open = false;
            }}
          >
            <details>
              <summary>
                <span>{currentNavigationLabel}</span>
                <b aria-hidden="true">⌄</b>
              </summary>
              <div className="enterprise-mobile-workspace-menu">
                {activeNavigationGroups.map((group) => (
                  <section key={group.label}>
                    <h2><i aria-hidden="true">{group.icon}</i>{group.label}</h2>
                    {group.sections?.map((section) => (
                      <div className="enterprise-mobile-workspace-subgroup" key={section.label}>
                        <p>{section.label}</p>
                        {section.links.map((item) => <LockedNavigationItem key={item[1]} item={item} />)}
                      </div>
                    ))}
                    {(group.links ?? []).map((item) => <LockedNavigationItem key={item[1]} item={item} />)}
                  </section>
                ))}
                {activeAdministrationNavigation.length > 0 && <section>
                  <h2><i aria-hidden="true">⚙</i>Administration</h2>
                  {activeAdministrationNavigation.map((item) => <LockedNavigationItem key={item[1]} item={item} />)}
                </section>}
              </div>
            </details>
          </nav>
          {announcements.length > 0 && <div className="learner-announcement enterprise-admin-announcement" role="status"><span>Important</span><div><p>{announcements.map(item => `${item.title}: ${item.message}`).join("   •   ")}   •   {announcements.map(item => `${item.title}: ${item.message}`).join("   •   ")}</p></div></div>}
          <div className={currentModuleIncluded ? undefined : "enterprise-locked-content"} aria-disabled={!currentModuleIncluded}>
            {children}
            {!currentModuleIncluded && <button type="button" className="enterprise-locked-content-overlay" onClick={() => setUpgradeModule(routeModule(pathname))} aria-label="Upgrade to use this feature"><span>Preview only · Upgrade to use this feature</span></button>}
          </div>
          {upgradeModule && <div className="enterprise-upgrade-backdrop" role="presentation" onMouseDown={() => setUpgradeModule(undefined)}><section className="enterprise-upgrade-dialog" role="dialog" aria-modal="true" aria-labelledby="upgrade-title" onMouseDown={(event) => event.stopPropagation()}><span className="enterprise-upgrade-icon" aria-hidden="true">✦</span><p>PLAN UPGRADE</p><h2 id="upgrade-title">Unlock {moduleLabel(upgradeModule)}</h2><span>{moduleLabel(upgradeModule)} is not included with the {subscriptionLabel} plan. Your academy administrator can upgrade the subscription to activate it.</span><div><button type="button" className="enterprise-action-button enterprise-action-button-secondary" onClick={() => setUpgradeModule(undefined)}>Keep browsing</button><Link href="/admin/control" className="enterprise-action-button" onClick={() => setUpgradeModule(undefined)}>View subscription</Link></div></section></div>}
        </section>
      </div>
    </EnterpriseShellContext.Provider>
  );
}
