"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { ThemeToggle } from "@/components/theme-toggle";
import { academyApi, apiHeaders } from "@/lib/api";

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
  outstandingBilling: number;
  recentAudit: {
    id: string;
    action: string;
    actorName: string;
    occurredAtUtc: string;
  }[];
};
type PlatformHealth = {
  api: string;
  database: string;
  communicationProviders: string;
  maintenanceMode: boolean;
};

const platformLinks = [
  { label: "Overview", icon: "▦", href: "/platform" },
  {
    label: "Tenant management",
    icon: "◫",
    href: "/platform/control?tab=Tenants",
  },
  { label: "Academy admins", icon: "♙", href: "/platform/control?tab=Admins" },
  { label: "Billing", icon: "₹", href: "/platform/control?tab=Billing" },
  { label: "Support", icon: "?", href: "/platform/control?tab=Support" },
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
  const [academyName, setAcademyName] = useState("");
  const [legalName, setLegalName] = useState("");
  const [adminUserName, setAdminUserName] = useState("");
  const [adminDisplayName, setAdminDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [onboardingOpen, setOnboardingOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<Notice>({
    text: "Loading academy portfolio…",
    tone: "neutral",
  });
  const [overview, setOverview] = useState<PlatformOverview>();
  const [health, setHealth] = useState<PlatformHealth>();
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

  async function load() {
    const [response, overviewResponse, healthResponse] = await Promise.all([
      academyApi("/api/platform/academies", { cache: "no-store" }),
      academyApi("/api/platform/overview", { cache: "no-store" }),
      academyApi("/api/platform/health", { cache: "no-store" }),
    ]);
    if (!response.ok) throw new Error("Platform Owner access is required.");
    setAcademies(await response.json());
    if (overviewResponse.ok) setOverview(await overviewResponse.json());
    if (healthResponse.ok) setHealth(await healthResponse.json());
    setNotice({ text: "", tone: "neutral" });
  }
  useEffect(() => {
    void load().catch((error) =>
      setNotice({ text: error.message, tone: "error" }),
    );
  }, []);
  async function onboard(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setNotice({
      text: "Creating academy and its initial administrator…",
      tone: "neutral",
    });
    try {
      const response = await academyApi("/api/platform/academies", {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          academyName,
          legalName: legalName || null,
          adminUserName,
          adminDisplayName: adminDisplayName || null,
          password,
          countryCode: "IN",
          timeZone: "Asia/Kolkata",
        }),
      });
      const payload = await response.json().catch(() => null);
      if (!response.ok)
        throw new Error(
          payload?.message ?? "Academy onboarding could not be completed.",
        );
      setAcademyName("");
      setLegalName("");
      setAdminUserName("");
      setAdminDisplayName("");
      setOnboardingOpen(false);
      await load();
      setNotice({
        text: `${payload.name} is ready. Its Academy Admin can sign in now.`,
        tone: "success",
      });
    } catch (error) {
      setNotice({
        text:
          error instanceof Error
            ? error.message
            : "Academy onboarding could not be completed.",
        tone: "error",
      });
    } finally {
      setBusy(false);
    }
  }
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
    localStorage.removeItem("academydesk.accessToken");
    localStorage.removeItem("academydesk.refreshToken");
    router.push("/login");
  }

  return (
    <main className="platform-shell">
      <aside className="platform-sidebar">
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
      <section className="platform-workspace">
        <header className="platform-topbar">
          <div />
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
            <details className="platform-profile">
              <summary aria-label="Open Platform Owner profile menu">
                <span className="platform-avatar">S</span>
              </summary>
              <div className="platform-profile-menu">
                <strong>Shashank</strong>
                <small>Platform Owner</small>
                <button type="button" onClick={signOut}>
                  Sign out
                </button>
              </div>
            </details>
          </div>
        </header>
        <div className="platform-content">
          <section id="overview" className="platform-heading">
            <div>
              <p className="platform-page-eyebrow">Platform / portfolio</p>
              <h2 className="platform-workspace-title">Academies</h2>
              <span>Monitor academy health, commercial activity and tenant access from one workspace.</span>
            </div>
            <button
              type="button"
              className="enterprise-primary-action"
              onClick={() => setOnboardingOpen(true)}
            >
              ＋ Onboard academy
            </button>
          </section>
          <section
            className="platform-kpis"
            aria-label="Platform portfolio summary"
          >
            <article>
              <span>Total academies</span>
              <strong>{academies.length}</strong>
            </article>
            <article>
              <span>Active academies</span>
              <strong>{activeAcademies.length}</strong>
            </article>
            <article>
              <span>Active learners</span>
              <strong>{totalStudents}</strong>
            </article>
            <article>
              <span>Open support</span>
              <strong>{overview?.openSupportCases ?? 0}</strong>
            </article>
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
          {onboardingOpen && (
            <div className="platform-modal-backdrop" role="presentation">
              <article
                className="platform-modal"
                role="dialog"
                aria-modal="true"
                aria-labelledby="onboard-title"
              >
                <header>
                  <h3 id="onboard-title">Onboard academy</h3>
                  <button
                    type="button"
                    aria-label="Close onboarding"
                    onClick={() => setOnboardingOpen(false)}
                  >
                    ×
                  </button>
                </header>
                <form onSubmit={onboard}>
                  <label>
                    Academy name
                    <input
                      value={academyName}
                      onChange={(event) => setAcademyName(event.target.value)}
                      required
                    />
                  </label>
                  <label>
                    Legal business name <em>Optional</em>
                    <input
                      value={legalName}
                      onChange={(event) => setLegalName(event.target.value)}
                    />
                  </label>
                  <label>
                    Admin user name
                    <input
                      value={adminUserName}
                      onChange={(event) => setAdminUserName(event.target.value)}
                      required
                    />
                  </label>
                  <label>
                    Admin display name <em>Optional</em>
                    <input
                      value={adminDisplayName}
                      onChange={(event) =>
                        setAdminDisplayName(event.target.value)
                      }
                    />
                  </label>
                  <label>
                    Temporary password
                    <input
                      type="password"
                      value={password}
                      onChange={(event) => setPassword(event.target.value)}
                      required
                    />
                  </label>
                  <button className="platform-submit" disabled={busy}>
                    {busy ? "Working…" : "Create academy and admin"}
                  </button>
                </form>
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
