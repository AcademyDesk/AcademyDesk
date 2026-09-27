"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { ThemeToggle } from "@/components/theme-toggle";
import { academyApi, apiHeaders, apiUrl, clearPortalTokens } from "@/lib/api";

type Academy = {
  id: string;
  name: string;
  legalName?: string;
  countryCode: string;
  timeZone: string;
  isActive: boolean;
  branches: number;
  students: number;
};
type Notice = { text: string; tone: "success" | "error" | "neutral" };
type PlatformOverview = {
  openSupportCases: number;
  totalBilled: number;
  collectedBilling: number;
  outstandingBilling: number;
  overdueInvoices: number;
  recentAudit: {
    id: string;
    action: string;
    actorName: string;
    occurredAtUtc: string;
  }[];
};
const money = (value: number) => new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(value);
type PlatformHealth = {
  api: string;
  database: string;
  communicationProviders: string;
  maintenanceMode: boolean;
};
type OverviewModal = "academies" | "activeAcademies" | "learners" | "support" | "billed" | "collected" | "outstanding" | null;
type OwnerSession = { displayName: string; email?: string | null; profileImageUrl?: string | null };

const platformLinks = [
  { label: "Overview", icon: "▦", href: "/platform" },
  {
    label: "Tenant management",
    icon: "◫",
    href: "/platform/control?tab=Tenants",
  },
  { label: "Tenant Onboarding", icon: "✦", href: "/platform/control?tab=Tenant%20onboarding" },
  { label: "Tenant Information", icon: "ⓘ", href: "/platform/control?tab=Tenant%20information" },
  { label: "Academy Admins", icon: "♙", href: "/platform/control?tab=Admins" },
  { label: "Billing", icon: "₹", href: "/platform/control?tab=Billing" },
  { label: "Support", icon: "?", href: "/platform/control?tab=Support" },
  { label: "Activity logs", icon: "☷", href: "/platform/control?tab=Activity%20logs" },
  { label: "Settings", icon: "⚙", href: "/platform/control?tab=Settings" },
  {
    label: "Audit & health",
    icon: "✓",
    href: "/platform/control?tab=Audit%20%26%20health",
  },
] as const;

export default function PlatformPage() {
  const router = useRouter();
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [query, setQuery] = useState("");
  const [overviewModal, setOverviewModal] = useState<OverviewModal>(null);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<Notice>({
    text: "Loading academy portfolio…",
    tone: "neutral",
  });
  const [overview, setOverview] = useState<PlatformOverview>();
  const [health, setHealth] = useState<PlatformHealth>();
  const [owner, setOwner] = useState<OwnerSession>();
  const filteredAcademies = useMemo(
    () =>
      academies.filter(
        (academy) =>
          academy.name.toLowerCase().includes(query.toLowerCase()) ||
          academy.legalName?.toLowerCase().includes(query.toLowerCase()),
      ),
    [academies, query],
  );
  const activeAcademies = academies.filter((academy) => academy.isActive);
  const totalStudents = academies.reduce(
    (total, academy) => total + academy.students,
    0,
  );
  const overviewTiles = [
    { key: "academies" as const, label: "Total academies", value: academies.length, detail: "Academies in the platform portfolio" },
    { key: "activeAcademies" as const, label: "Active academies", value: activeAcademies.length, detail: "Academies currently able to operate" },
    { key: "learners" as const, label: "Active learners", value: totalStudents, detail: "Learners across all academies" },
    { key: "support" as const, label: "Open support", value: overview?.openSupportCases ?? 0, detail: "Support cases needing attention" },
    { key: "billed" as const, label: "Platform billed", value: money(overview?.totalBilled ?? 0), detail: "Issued platform invoices", className: "platform-finance-kpi" },
    { key: "collected" as const, label: "Collected", value: money(overview?.collectedBilling ?? 0), detail: "Paid platform invoices", className: "platform-finance-kpi collected" },
    { key: "outstanding" as const, label: "Outstanding", value: money(overview?.outstandingBilling ?? 0), detail: `${overview?.overdueInvoices ?? 0} overdue invoice${overview?.overdueInvoices === 1 ? "" : "s"}`, className: "platform-finance-kpi outstanding" },
  ];
  const selectedOverviewTile = overviewTiles.find((tile) => tile.key === overviewModal);

  async function load() {
    const [response, overviewResponse, healthResponse, ownerResponse] = await Promise.all([
      academyApi("/api/platform/academies", { cache: "no-store" }),
      academyApi("/api/platform/overview", { cache: "no-store" }),
      academyApi("/api/platform/health", { cache: "no-store" }),
      academyApi("/api/auth/session", { cache: "no-store" }),
    ]);
    if (!response.ok) throw new Error("Platform Owner access is required.");
    setAcademies(await response.json());
    if (overviewResponse.ok) setOverview(await overviewResponse.json());
    if (healthResponse.ok) setHealth(await healthResponse.json());
    if (ownerResponse.ok) setOwner(await ownerResponse.json());
    setNotice({ text: "", tone: "neutral" });
  }
  useEffect(() => {
    void load().catch((error) =>
      setNotice({ text: error.message, tone: "error" }),
    );
  }, []);
  async function setStatus(academy: Academy) {
    setBusy(true);
    try {
      const response = await academyApi(
        `/api/platform/academies/${academy.id}/status`,
        {
          method: "PATCH",
          headers: apiHeaders(true),
          body: JSON.stringify({ isActive: !academy.isActive }),
        },
      );
      if (!response.ok) throw new Error();
      await load();
      setNotice({
        text: `${academy.name} is now ${academy.isActive ? "inactive" : "active"}.`,
        tone: "success",
      });
    } catch {
      setNotice({
        text: "The academy status could not be changed.",
        tone: "error",
      });
    } finally {
      setBusy(false);
    }
  }
  function signOut() {
    clearPortalTokens();
    router.push("/login");
  }

  return (
    <main className="enterprise-app-shell platform-shell">
      <aside className="enterprise-sidebar platform-sidebar">
        <div className="enterprise-brand">
          <span>A</span>
          <strong>AcademyDesk</strong>
        </div>
        <nav className="platform-nav" aria-label="Platform navigation">
          <p>Platform</p>
          {platformLinks.map((item) => (
            <Link
              href={item.href}
              key={item.label}
              data-active={item.label === "Overview"}
            >
              <i>{item.icon}</i>
              {item.label}
            </Link>
          ))}
        </nav>
      </aside>
      <section className="enterprise-workspace platform-workspace">
        <header className="enterprise-topbar platform-topbar">
          <div className="platform-topbar-context">
            <span>Platform Owner</span>
            <strong>{owner?.displayName || "Platform Owner"}</strong>
          </div>
          <div className="enterprise-utilities">
            <button
              type="button"
              className="enterprise-icon-button"
              aria-label="Search academy portfolio"
              onClick={() => document.getElementById("academy-search")?.focus()}
            >
              ⌕
            </button>
            <button
              type="button"
              className="enterprise-icon-button"
              aria-label="Platform notifications"
            >
              ♧
            </button>
            <ThemeToggle />
            <details className="platform-profile enterprise-profile">
              <summary className="enterprise-profile-trigger" aria-label="Open Platform Owner profile menu">
                {owner?.profileImageUrl ? <img className="platform-avatar-image" src={`${apiUrl}${owner.profileImageUrl}`} alt="Profile"/> : <span className="platform-avatar">{owner?.displayName?.[0]?.toUpperCase() || "S"}</span>}
                <span className="platform-topbar-owner-copy"><b>{owner?.displayName || "Platform Owner"}</b><small>Platform Owner</small></span>
              </summary>
              <div className="platform-profile-menu">
                <strong>{owner?.displayName || "Platform Owner"}</strong>
                <small>Platform Owner</small>
                {owner?.email && <small>{owner.email}</small>}
                <button type="button" onClick={signOut}>
                  Sign out
                </button>
              </div>
            </details>
          </div>
        </header>
        <div className="platform-content">
          <section id="overview" className="platform-heading">
            <div className="platform-title-group">
              <span className="platform-title-icon" aria-hidden="true">◈</span>
              <div>
                <p className="platform-page-eyebrow">Platform owner</p>
                <h1 className="platform-workspace-title">Overview</h1>
              </div>
            </div>
            <Link
              className="enterprise-primary-action"
              href="/platform/control?tab=Tenant%20onboarding&start=true"
            >
              ＋ Onboard academy
            </Link>
          </section>
          <section
            className="platform-kpis"
            aria-label="Platform portfolio summary"
          >
            {overviewTiles.map((tile) => <button key={tile.key} type="button" className={tile.className} onClick={() => setOverviewModal(tile.key)} aria-haspopup="dialog"><span>{tile.label}</span><strong>{tile.value}</strong><small>{tile.detail}</small></button>)}
          </section>
          <section className="platform-main-grid">
            <article
              id="portfolio"
              className="platform-panel platform-portfolio"
            >
              <header>
                <div>
                  <h3>Academies</h3>
                </div>
                <input
                  id="academy-search"
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder="Search academies"
                  aria-label="Search academies"
                />
              </header>
              <div className="platform-table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Academy</th>
                      <th>Status</th>
                      <th>Branches</th>
                      <th>Learners</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredAcademies.length ? (
                      filteredAcademies.map((academy) => (
                        <tr key={academy.id}>
                          <td>
                            <strong>{academy.name}</strong>
                            <small>
                              {academy.legalName ||
                                `${academy.countryCode} · ${academy.timeZone}`}
                            </small>
                          </td>
                          <td>
                            <span
                              className={
                                academy.isActive
                                  ? "platform-status active"
                                  : "platform-status"
                              }
                            >
                              {academy.isActive ? "Active" : "Inactive"}
                            </span>
                          </td>
                          <td>{academy.branches}</td>
                          <td>{academy.students}</td>
                          <td>
                            <button
                              type="button"
                              disabled={busy}
                              onClick={() => void setStatus(academy)}
                            >
                              {academy.isActive ? "Suspend" : "Activate"}
                            </button>
                          </td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={5} className="platform-empty">
                          No academy matches this search.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </article>
          </section>
          {overviewModal && selectedOverviewTile && (
            <div className="platform-modal-backdrop" role="presentation" onMouseDown={() => setOverviewModal(null)}>
              <article className="platform-modal platform-overview-modal" role="dialog" aria-modal="true" aria-labelledby="overview-tile-title" onMouseDown={(event) => event.stopPropagation()}>
                <header><div><p>{selectedOverviewTile.label}</p><h3 id="overview-tile-title">{selectedOverviewTile.value}</h3></div><button type="button" aria-label="Close detail" onClick={() => setOverviewModal(null)}>×</button></header>
                {(["academies", "activeAcademies", "learners"] as const).includes(overviewModal as "academies" | "activeAcademies" | "learners") ? <ul className="platform-overview-detail-list">{(overviewModal === "activeAcademies" ? activeAcademies : academies).map((academy) => <li key={academy.id}><div><b>{academy.name}</b><small>{academy.branches} branch{academy.branches === 1 ? "" : "es"} · {academy.students} learner{academy.students === 1 ? "" : "s"}</small></div><span className={academy.isActive ? "platform-status active" : "platform-status"}>{academy.isActive ? "Active" : "Inactive"}</span></li>)}{!academies.length && <li><small>No academies recorded yet.</small></li>}</ul> : <div className="platform-overview-detail-summary"><p>{selectedOverviewTile.detail}</p>{overviewModal === "support" && <Link href="/platform/control?tab=Support" onClick={() => setOverviewModal(null)}>Open Support</Link>}{(["billed", "collected", "outstanding"] as const).includes(overviewModal as "billed" | "collected" | "outstanding") && <Link href="/platform/control?tab=Billing" onClick={() => setOverviewModal(null)}>Open Billing</Link>}</div>}
              </article>
            </div>
          )}
          <section id="governance" className="platform-governance">
            <div>
              <span>Platform health</span>
              <strong>
                API: {health?.api ?? "Checking"} · Database:{" "}
                {health?.database ?? "Checking"}
              </strong>
            </div>
            <div>
              <span>Communication</span>
              <strong>{health?.communicationProviders ?? "Checking"}</strong>
            </div>
            <div>
              <span>Billing exposure</span>
              <strong>₹{overview?.outstandingBilling ?? 0}</strong>
            </div>
          </section>
          {notice.text && (
            <div className={`platform-notice ${notice.tone}`} role="status">
              {notice.text}
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
