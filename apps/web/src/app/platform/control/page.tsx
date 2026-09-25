"use client";

import Link from "next/link";
import { FormEvent, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { ThemeToggle } from "@/components/theme-toggle";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders, apiUrl, clearPortalTokens } from "@/lib/api";

type Academy = {
  id: string;
  name: string;
  subscriptionPlan: string;
  subscriptionStatus: string;
  subscriptionEndsAtUtc?: string;
  studentLimit: number;
  staffLimit: number;
  enabledModulesJson: string;
};
type Admin = {
  id: string;
  displayName: string;
  email: string;
  isActive: boolean;
  academyName?: string;
};
type Case = {
  id: string;
  academyId: string;
  academyName: string;
  subject: string;
  priority: string;
  status: string;
  description?: string;
  createdAtUtc: string;
};
type Invoice = {
  id: string;
  academyId: string;
  academyName: string;
  invoiceNumber: string;
  amount: number;
  currency: string;
  status: string;
  dueDate: string;
};
type Settings = {
  platformName: string;
  supportEmail?: string;
  defaultCurrency: string;
  defaultTrialDays: number;
  dataRetentionDays: number;
  maintenanceMode: boolean;
  statusMessage?: string;
};
type Audit = {
  id: string;
  action: string;
  actorName: string;
  entityType: string;
  occurredAtUtc: string;
};
type Health = {
  api: string;
  database: string;
  backgroundJobs: string;
  communicationProviders: string;
  maintenanceMode: boolean;
  statusMessage?: string;
};
type Announcement = { id: string; academyId: string; academyName: string; title: string; message: string; createdAtUtc: string; expiresAtUtc?: string | null };
type OwnerSession = { displayName: string; email?: string | null; phoneNumber?: string | null; profileImageUrl?: string | null; roles: string[] };
type AdminModal = "all" | "active" | "inactive" | "academies" | null;
type TenantModal = "all" | "active" | "trial" | "attention" | null;
type InvoiceModal = "all" | "open" | "overdue" | "paid" | null;
type CaseModal = "all" | "open" | "urgent" | "resolved" | null;
type AnnouncementModal = "active" | "recent" | null;

const platformLinks = [
  { label: "Overview", icon: "▦", href: "/platform" },
  {
    label: "Tenant management",
    icon: "◫",
    href: "/platform/control?tab=Tenants",
    tab: "Tenants",
  },
  {
    label: "Academy admins",
    icon: "♙",
    href: "/platform/control?tab=Admins",
    tab: "Admins",
  },
  {
    label: "Billing",
    icon: "₹",
    href: "/platform/control?tab=Billing",
    tab: "Billing",
  },
  {
    label: "Support",
    icon: "?",
    href: "/platform/control?tab=Support",
    tab: "Support",
  },
  { label: "Announcements", icon: "!", href: "/platform/control?tab=Announcements", tab: "Announcements" },
  {
    label: "Settings",
    icon: "⚙",
    href: "/platform/control?tab=Settings",
    tab: "Settings",
  },
  {
    label: "Audit & health",
    icon: "✓",
    href: "/platform/control?tab=Audit%20%26%20health",
    tab: "Audit & health",
  },
] as const;

const platformControlTabs = [
  "Tenants",
  "Admins",
  "Billing",
  "Support",
  "Announcements",
  "Settings",
  "Audit & health",
];

export default function PlatformControlPage() {
  const router = useRouter();
  const [tab, setTab] = useState("Tenants");
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [admins, setAdmins] = useState<Admin[]>([]);
  const [cases, setCases] = useState<Case[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [audit, setAudit] = useState<Audit[]>([]);
  const [settings, setSettings] = useState<Settings>();
  const [health, setHealth] = useState<Health>();
  const [announcements, setAnnouncements] = useState<Announcement[]>([]);
  const [owner, setOwner] = useState<OwnerSession>();
  const [selectedAcademy, setSelectedAcademy] = useState("");
  const [tenantPlan, setTenantPlan] = useState("");
  const [tenantStatus, setTenantStatus] = useState("");
  const [tenantEndsAt, setTenantEndsAt] = useState("");
  const [adminModal, setAdminModal] = useState<AdminModal>(null);
  const [tenantModal, setTenantModal] = useState<TenantModal>(null);
  const [invoiceModal, setInvoiceModal] = useState<InvoiceModal>(null);
  const [caseModal, setCaseModal] = useState<CaseModal>(null);
  const [announcementModal, setAnnouncementModal] = useState<AnnouncementModal>(null);
  const [invoiceAcademyId, setInvoiceAcademyId] = useState("");
  const [invoiceCurrency, setInvoiceCurrency] = useState("INR");
  const [invoicePeriodStart, setInvoicePeriodStart] = useState("");
  const [invoicePeriodEnd, setInvoicePeriodEnd] = useState("");
  const [invoiceDueDate, setInvoiceDueDate] = useState("");
  const [invoiceStatuses, setInvoiceStatuses] = useState<Record<string, string>>({});
  const [supportAcademyId, setSupportAcademyId] = useState("");
  const [supportPriority, setSupportPriority] = useState("Normal");
  const [caseStatuses, setCaseStatuses] = useState<Record<string, string>>({});
  const [announcementAcademyId, setAnnouncementAcademyId] = useState("");
  const [announcementHours, setAnnouncementHours] = useState("24");
  const [settingsCurrency, setSettingsCurrency] = useState("");
  const [message, setMessage] = useState("Loading platform controls…");
  const [busy, setBusy] = useState(false);
  const profileImageInput = useRef<HTMLInputElement>(null);

  async function load() {
    const [a, ad, c, i, s, au, h, ownerResponse, announcementResponse] = await Promise.all([
      academyApi("/api/platform/academies"),
      academyApi("/api/platform/admins"),
      academyApi("/api/platform/support-cases"),
      academyApi("/api/platform/billing-invoices"),
      academyApi("/api/platform/settings"),
      academyApi("/api/platform/audit"),
      academyApi("/api/platform/health"),
      academyApi("/api/auth/session"),
      academyApi("/api/platform/announcements"),
    ]);
    if (!a.ok) throw new Error("Platform Owner access is required.");
    const academyRows = await a.json();
    setAcademies(academyRows);
    setSelectedAcademy((current) => current || academyRows[0]?.id || "");
    if (ad.ok) setAdmins(await ad.json());
    if (c.ok) setCases(await c.json());
    if (i.ok) setInvoices(await i.json());
    if (s.ok) setSettings(await s.json());
    if (au.ok) setAudit(await au.json());
    if (h.ok) setHealth(await h.json());
    if (ownerResponse.ok) setOwner(await ownerResponse.json());
    if (announcementResponse.ok) setAnnouncements(await announcementResponse.json());
    setMessage("");
  }
  useEffect(() => {
    void load().catch((error) => setMessage(error.message));
  }, []);
  const selected = academies.find((academy) => academy.id === selectedAcademy);
  const activeTenants = academies.filter((academy) => academy.subscriptionStatus === "Active").length;
  const trialTenants = academies.filter((academy) => academy.subscriptionStatus === "Trial").length;
  const attentionTenants = academies.filter((academy) => ["Past due", "Cancelled"].includes(academy.subscriptionStatus)).length;
  const tenantModalTitle = tenantModal === "active" ? "Active tenants" : tenantModal === "trial" ? "Trial tenants" : tenantModal === "attention" ? "Tenants needing attention" : "All tenants";
  const tenantModalRecords = tenantModal === "active" ? academies.filter((academy) => academy.subscriptionStatus === "Active") : tenantModal === "trial" ? academies.filter((academy) => academy.subscriptionStatus === "Trial") : tenantModal === "attention" ? academies.filter((academy) => ["Past due", "Cancelled"].includes(academy.subscriptionStatus)) : academies;
  const activeAdmins = admins.filter((admin) => admin.isActive).length;
  const adminAcademies = new Set(admins.map((admin) => admin.academyName).filter(Boolean)).size;
  const adminModalTitle = adminModal === "active" ? "Active academy administrators" : adminModal === "inactive" ? "Deactivated academy administrators" : adminModal === "academies" ? "Academies with administrator access" : "All academy administrators";
  const adminModalRecords = adminModal === "active" ? admins.filter((admin) => admin.isActive) : adminModal === "inactive" ? admins.filter((admin) => !admin.isActive) : admins;
  const openInvoices = invoices.filter((invoice) => !["Paid", "Void"].includes(invoice.status));
  const overdueInvoices = invoices.filter((invoice) => invoice.status === "Overdue");
  const paidInvoices = invoices.filter((invoice) => invoice.status === "Paid");
  const invoiceModalTitle = invoiceModal === "open" ? "Open platform invoices" : invoiceModal === "overdue" ? "Overdue platform invoices" : invoiceModal === "paid" ? "Paid platform invoices" : "All platform invoices";
  const invoiceModalRecords = invoiceModal === "open" ? openInvoices : invoiceModal === "overdue" ? overdueInvoices : invoiceModal === "paid" ? paidInvoices : invoices;
  const openCases = cases.filter((item) => !["Resolved", "Closed"].includes(item.status));
  const urgentCases = cases.filter((item) => ["High", "Urgent"].includes(item.priority) && !["Resolved", "Closed"].includes(item.status));
  const resolvedCases = cases.filter((item) => ["Resolved", "Closed"].includes(item.status));
  const caseModalTitle = caseModal === "open" ? "Open support cases" : caseModal === "urgent" ? "High-priority support cases" : caseModal === "resolved" ? "Resolved support cases" : "All support cases";
  const caseModalRecords = caseModal === "open" ? openCases : caseModal === "urgent" ? urgentCases : caseModal === "resolved" ? resolvedCases : cases;
  const currentTime = Date.now();
  const activeAnnouncements = announcements.filter((item) => item.expiresAtUtc && new Date(item.expiresAtUtc).getTime() > currentTime);
  const announcementModalTitle = announcementModal === "active" ? "Active announcements" : "Recent announcements";
  const announcementModalRecords = announcementModal === "active" ? activeAnnouncements : announcements;
  const controlIcon = tab === "Admins" ? "♙" : tab === "Billing" ? "₹" : tab === "Support" ? "?" : tab === "Announcements" ? "!" : tab === "Settings" ? "⚙" : tab === "Audit & health" ? "✓" : "◫";
  useEffect(() => {
    setTenantPlan(selected?.subscriptionPlan ?? "");
    setTenantStatus(selected?.subscriptionStatus ?? "");
    setTenantEndsAt(selected?.subscriptionEndsAtUtc?.slice(0, 10) ?? "");
  }, [selected]);
  useEffect(() => { setSettingsCurrency(settings?.defaultCurrency ?? ""); }, [settings]);
  async function saveTenant(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    const form = new FormData(event.currentTarget);
    await request(
      `/api/platform/academies/${selected.id}/configuration`,
      "PUT",
      {
        subscriptionPlan: tenantPlan,
        subscriptionStatus: tenantStatus,
        subscriptionEndsAtUtc: tenantEndsAt || null,
      },
      "Tenant configuration saved.",
    );
  }
  async function saveSettings(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await request(
      "/api/platform/settings",
      "PUT",
      {
        platformName: form.get("platformName"),
        supportEmail: form.get("supportEmail") || null,
        defaultCurrency: form.get("currency"),
        defaultTrialDays: Number(form.get("trialDays")),
        dataRetentionDays: Number(form.get("retentionDays")),
        maintenanceMode: form.get("maintenanceMode") === "on",
        statusMessage: form.get("statusMessage") || null,
      },
      "Platform settings saved.",
    );
  }
  async function createCase(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await request(
      "/api/platform/support-cases",
      "POST",
      {
        academyId: form.get("academyId"),
        subject: form.get("subject"),
        priority: form.get("priority"),
        description: form.get("description") || null,
      },
      "Support case created.",
    );
    event.currentTarget.reset();
    setSupportAcademyId("");
    setSupportPriority("Normal");
  }
  async function createInvoice(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await request(
      "/api/platform/billing-invoices",
      "POST",
      {
        academyId: form.get("academyId"),
        invoiceNumber: form.get("invoiceNumber"),
        amount: Number(form.get("amount")),
        currency: form.get("currency"),
        periodStart: form.get("periodStart"),
        periodEnd: form.get("periodEnd"),
        dueDate: form.get("dueDate"),
      },
      "Platform invoice created.",
    );
    event.currentTarget.reset();
    setInvoiceAcademyId("");
    setInvoiceCurrency("INR");
    setInvoicePeriodStart("");
    setInvoicePeriodEnd("");
    setInvoiceDueDate("");
  }
  async function request(
    path: string,
    method: string,
    body: unknown,
    success: string,
  ) {
    setBusy(true);
    setMessage("");
    try {
      const response = await academyApi(path, {
        method,
        headers: apiHeaders(true),
        body: JSON.stringify(body),
      });
      const payload = await response.json().catch(() => null);
      if (!response.ok)
        throw new Error(payload?.message ?? "The change could not be saved.");
      await load();
      setMessage(success);
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "The change could not be saved.",
      );
    } finally {
      setBusy(false);
    }
  }
  async function adminActive(admin: Admin) {
    await request(
      `/api/platform/admins/${admin.id}/active`,
      "PATCH",
      { isActive: !admin.isActive },
      `${admin.displayName} ${admin.isActive ? "deactivated" : "activated"}.`,
    );
  }
  async function adminPassword(admin: Admin) {
    const password = window.prompt(`New password for ${admin.displayName}:`);
    if (password)
      await request(
        `/api/platform/admins/${admin.id}/reset-password`,
        "POST",
        { newPassword: password },
        "Administrator password reset.",
      );
  }
  async function updateCase(item: Case, status: string) {
    await request(
      `/api/platform/support-cases/${item.id}`,
      "PATCH",
      { status, priority: item.priority },
      "Support case updated.",
    );
  }
  async function updateInvoice(item: Invoice, status: string) {
    await request(
      `/api/platform/billing-invoices/${item.id}/status`,
      "PATCH",
      { status },
      "Invoice status updated.",
    );
  }
  async function saveOwnerProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await request("/api/auth/session/profile", "PUT", { displayName: form.get("displayName"), phoneNumber: form.get("phoneNumber") || null }, "Owner profile saved.");
  }
  async function changeOwnerPassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const newPassword = String(form.get("newPassword") || "");
    if (newPassword !== String(form.get("confirmPassword") || "")) return setMessage("New password and confirmation must match.");
    await request("/api/auth/session/change-password", "POST", { currentPassword: form.get("currentPassword"), newPassword }, "Password changed. Use the new password next time you sign in.");
    event.currentTarget.reset();
  }
  async function uploadOwnerImage(event: React.ChangeEvent<HTMLInputElement>) {
    const image = event.target.files?.[0];
    event.target.value = "";
    if (!image) return;
    setBusy(true); setMessage("");
    try {
      const body = new FormData(); body.append("image", image);
      const response = await academyApi("/api/auth/session/profile-image", { method: "POST", body });
      const payload = await response.json().catch(() => null);
      if (!response.ok) throw new Error(payload?.message || "Profile image could not be saved.");
      await load(); setMessage("Profile image saved.");
    } catch (error) { setMessage(error instanceof Error ? error.message : "Profile image could not be saved."); }
    finally { setBusy(false); }
  }
  async function publishAnnouncement(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget); setBusy(true);
    try { const response = await academyApi("/api/platform/announcements", { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ academyId: form.get("academyId"), title: form.get("title"), message: form.get("body"), displayHours: Number(form.get("hours")) }) }); if (!response.ok) { const error = await response.json().catch(() => null); throw new Error(error?.message ?? "Announcement could not be published."); } event.currentTarget.reset(); setAnnouncementAcademyId(""); setAnnouncementHours("24"); await load(); setMessage("Academy admin announcement is now live."); } catch (error) { setMessage(error instanceof Error ? error.message : "Announcement could not be published."); } finally { setBusy(false); }
  }
  function signOut() {
    clearPortalTokens();
    router.push("/login");
  }

  useEffect(() => {
    const requestedTab = new URLSearchParams(window.location.search).get("tab");
    if (requestedTab && platformControlTabs.includes(requestedTab))
      setTab(requestedTab);
  }, []);

  function selectWorkspace(nextTab: (typeof platformControlTabs)[number]) {
    setTab(nextTab);
    router.replace(`/platform/control?tab=${encodeURIComponent(nextTab)}`, { scroll: false });
    document
      .querySelector<HTMLElement>(".platform-control-content")
      ?.scrollTo({ top: 0 });
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
          {platformLinks.map((item) =>
            "tab" in item ? (
              <button
                type="button"
                key={item.label}
                data-active={item.tab === tab}
                onClick={() => selectWorkspace(item.tab)}
              >
                <i>{item.icon}</i>
                {item.label}
              </button>
            ) : (
              <Link href={item.href} key={item.label} data-active={false}>
                <i>{item.icon}</i>
                {item.label}
              </Link>
            ),
          )}
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
                <button type="button" onClick={signOut}>
                  Sign out
                </button>
              </div>
            </details>
          </div>
        </header>
        <div className="platform-content platform-control-content">
          <header className="platform-control-heading">
            <span className="platform-control-icon" aria-hidden="true">{controlIcon}</span>
            <div>
              <p>Platform owner</p>
              <h1 className="platform-workspace-title">{tab === "Tenants" ? "Tenant management" : tab === "Admins" ? "Academy admins" : tab}</h1>
            </div>
          </header>
          {message && (
            <p className="mt-4 rounded-lg border border-slate-700 bg-slate-900 p-3 text-sm text-slate-300">
              {message}
            </p>
          )}
          {tab === "Tenants" && (
            <>
              <section className="platform-tenant-kpis" aria-label="Tenant management summary">
                <button type="button" onClick={() => setTenantModal("all")}><span>Total tenants</span><b>{academies.length}</b><small>Academies on this platform</small></button>
                <button type="button" onClick={() => setTenantModal("active")}><span>Active</span><b>{activeTenants}</b><small>Paid or enabled tenants</small></button>
                <button type="button" onClick={() => setTenantModal("trial")}><span>Trial</span><b>{trialTenants}</b><small>Tenants evaluating the platform</small></button>
                <button type="button" onClick={() => setTenantModal("attention")}><span>Needs attention</span><b>{attentionTenants}</b><small>Past due or cancelled subscriptions</small></button>
              </section>
              <section className="platform-tenant-layout">
                <section className="platform-tenant-panel platform-tenant-register">
                  <header className="platform-control-panel-header"><div><p>Tenant register</p><h2>Academies</h2></div></header>
                  {academies.length ? <ul>{academies.map((academy) => <li key={academy.id}><button type="button" data-selected={academy.id === selectedAcademy} onClick={() => setSelectedAcademy(academy.id)}><span className="platform-tenant-logo">{academy.name.slice(0, 1).toUpperCase()}</span><span className="platform-tenant-register-copy"><b>{academy.name}</b><small>{academy.subscriptionPlan} plan · {academy.studentLimit} students · {academy.staffLimit} staff</small></span><em data-status={academy.subscriptionStatus.toLowerCase().replaceAll(" ", "-")}>{academy.subscriptionStatus}</em></button></li>)}</ul> : <p className="platform-tenant-empty">No tenants have been created yet.</p>}
                </section>
                <section className="platform-tenant-panel platform-tenant-editor">
                  <header className="platform-control-panel-header"><div><p>Selected tenant</p><h2>{selected?.name || "Tenant configuration"}</h2></div></header>
                  <div className="platform-tenant-fields">
                    <label className="platform-tenant-wide"><span>Academy</span><StandardSelectField name="academy" value={selectedAcademy} onChange={setSelectedAcademy} placeholder="Select academy" options={academies.map((academy) => ({ value: academy.id, label: academy.name }))} /></label>
                  </div>
                  {selected ? <form onSubmit={saveTenant} className="platform-tenant-fields">
                    <label><span>Subscription plan</span><StandardSelectField name="plan" value={tenantPlan} onChange={setTenantPlan} placeholder="Choose plan" options={["Trial", "Launch", "Growth", "Professional", "Enterprise"].map((value) => ({ value, label: value }))} /></label>
                    <label><span>Subscription status</span><StandardSelectField name="status" value={tenantStatus} onChange={setTenantStatus} placeholder="Choose status" options={["Trial", "Active", "Past due", "Cancelled"].map((value) => ({ value, label: value }))} /></label>
                    <StandardDateField name="endsAt" value={tenantEndsAt} onChange={setTenantEndsAt} label="Subscription end date" />
                    <section className="platform-tenant-access"><b>Plan-managed capacity</b><div><span><strong>{selected.studentLimit}</strong> Students</span><span><strong>{selected.staffLimit}</strong> Staff</span></div></section>
                    <button disabled={busy} className="enterprise-action-button platform-tenant-action">{busy ? "Saving…" : "Save tenant configuration"}</button>
                  </form> : <p className="platform-tenant-empty">Select an academy to configure its subscription.</p>}
                </section>
              </section>
              {tenantModal && <div className="platform-modal-backdrop" role="presentation"><article className="platform-modal platform-tenant-modal" role="dialog" aria-modal="true" aria-labelledby="tenant-modal-title"><header><div><p>Tenant register</p><h3 id="tenant-modal-title">{tenantModalTitle}</h3></div><button type="button" aria-label="Close tenant details" onClick={() => setTenantModal(null)}>×</button></header>{tenantModalRecords.length ? <ul>{tenantModalRecords.map((academy) => <li key={academy.id}><span>{academy.name.slice(0, 1).toUpperCase()}</span><div><b>{academy.name}</b><small>{academy.subscriptionPlan} plan · {academy.studentLimit} students · {academy.staffLimit} staff</small></div><em data-status={academy.subscriptionStatus.toLowerCase().replaceAll(" ", "-")}>{academy.subscriptionStatus}</em></li>)}</ul> : <p className="platform-tenant-empty">No tenants match this view.</p>}</article></div>}
            </>
          )}
          {tab === "Announcements" && <>
            <section className="platform-announcement-kpis" aria-label="Announcement summary">
              <button type="button" onClick={() => setAnnouncementModal("active")}><span>Active</span><b>{activeAnnouncements.length}</b><small>Currently shown to academy admins</small></button>
              <button type="button" onClick={() => setAnnouncementModal("recent")}><span>Recent</span><b>{announcements.length}</b><small>Published platform messages</small></button>
            </section>
            <section className="platform-announcement-layout">
              <form onSubmit={publishAnnouncement} className="platform-announcement-panel platform-announcement-editor">
                <header className="platform-control-panel-header"><div><p>Publish</p><h2>Academy admin announcement</h2></div></header>
                <div className="platform-announcement-fields">
                  <label className="platform-announcement-wide"><span>Academy</span><StandardSelectField name="academyId" value={announcementAcademyId} onChange={setAnnouncementAcademyId} placeholder="Select academy" options={academies.map((academy) => ({ value: academy.id, label: academy.name }))} /></label>
                  <label className="platform-announcement-wide"><span>Banner title</span><input name="title" required placeholder="Short, clear announcement title" /></label>
                  <label className="platform-announcement-wide"><span>Message</span><textarea name="body" required rows={4} placeholder="Write the announcement shown in the academy admin portal" /></label>
                  <label><span>Display duration</span><StandardSelectField name="hours" value={announcementHours} onChange={setAnnouncementHours} placeholder="Select duration" options={[["1", "1 hour"], ["4", "4 hours"], ["8", "8 hours"], ["24", "24 hours"], ["48", "2 days"], ["72", "3 days"], ["168", "7 days"]].map(([value, label]) => ({ value, label }))} /></label>
                  <div className="platform-announcement-audience"><b>Audience</b><span>Academy administrators</span></div>
                  <button disabled={busy || !announcementAcademyId} className="enterprise-action-button platform-announcement-action">{busy ? "Publishing…" : "Publish announcement"}</button>
                </div>
              </form>
              <section className="platform-announcement-panel platform-announcement-register">
                <header className="platform-control-panel-header"><div><p>Message register</p><h2>Active and recent messages</h2></div></header>
                {announcements.length ? <ul>{announcements.slice(0, 12).map((item) => { const active = Boolean(item.expiresAtUtc && new Date(item.expiresAtUtc).getTime() > currentTime); return <li key={item.id}><span>!</span><div><b>{item.title}</b><small>{item.academyName} · Ends {item.expiresAtUtc ? new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", hour: "numeric", minute: "2-digit" }).format(new Date(item.expiresAtUtc)) : "not set"}</small></div><em data-active={active}>{active ? "Active" : "Ended"}</em></li>; })}</ul> : <p className="platform-announcement-empty">No announcements have been published yet.</p>}
              </section>
            </section>
            {announcementModal && <div className="platform-modal-backdrop" role="presentation"><article className="platform-modal platform-announcement-modal" role="dialog" aria-modal="true" aria-labelledby="announcement-modal-title"><header><div><p>Platform announcements</p><h3 id="announcement-modal-title">{announcementModalTitle}</h3></div><button type="button" aria-label="Close announcement details" onClick={() => setAnnouncementModal(null)}>×</button></header>{announcementModalRecords.length ? <ul>{announcementModalRecords.map((item) => { const active = Boolean(item.expiresAtUtc && new Date(item.expiresAtUtc).getTime() > currentTime); return <li key={item.id}><span>!</span><div><b>{item.title}</b><small>{item.academyName} · {item.message}</small></div><em data-active={active}>{active ? "Active" : "Ended"}</em></li>; })}</ul> : <p className="platform-announcement-empty">No announcements match this view.</p>}</article></div>}
          </>}
          {tab === "Admins" && (
            <>
              <section className="platform-admin-kpis" aria-label="Academy administrator summary">
                <button type="button" onClick={() => setAdminModal("all")}><span>Total administrators</span><b>{admins.length}</b><small>Accounts with academy administration access</small></button>
                <button type="button" onClick={() => setAdminModal("active")}><span>Active</span><b>{activeAdmins}</b><small>Administrators currently able to sign in</small></button>
                <button type="button" onClick={() => setAdminModal("inactive")}><span>Deactivated</span><b>{admins.length - activeAdmins}</b><small>Accounts retained without sign-in access</small></button>
                <button type="button" onClick={() => setAdminModal("academies")}><span>Academies covered</span><b>{adminAcademies}</b><small>Academies with an assigned administrator</small></button>
              </section>
              <section className="platform-admin-panel">
                <header className="platform-control-panel-header"><div><p>Access register</p><h2>Academy administrators</h2></div></header>
                <div className="platform-admin-table-wrap">
                  <table>
                  <thead className="text-slate-400">
                    <tr>
                      <th>Administrator</th>
                      <th>Academy</th>
                      <th>Access status</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {admins.length ? admins.map((admin) => (
                      <tr key={admin.id}>
                        <td>
                          <div className="platform-admin-identity"><span aria-hidden="true">{admin.displayName.slice(0, 1).toUpperCase()}</span><div><b>{admin.displayName}</b><small>{admin.email}</small></div></div>
                        </td>
                        <td>{admin.academyName || "Unassigned"}</td>
                        <td><span className="platform-admin-status" data-active={admin.isActive}>{admin.isActive ? "Active" : "Deactivated"}</span></td>
                        <td><div className="platform-admin-actions"><button type="button" onClick={() => void adminActive(admin)} disabled={busy}>
                            {admin.isActive ? "Deactivate" : "Activate"}
                          </button><button type="button" onClick={() => void adminPassword(admin)} disabled={busy}>Reset password</button></div></td>
                      </tr>
                    )) : <tr><td colSpan={4} className="platform-admin-empty">No academy administrator accounts have been created yet.</td></tr>}
                  </tbody>
                </table>
              </div>
              </section>
              {adminModal && <div className="platform-modal-backdrop" role="presentation"><article className="platform-modal platform-admin-modal" role="dialog" aria-modal="true" aria-labelledby="admin-modal-title"><header><div><p>Academy administrators</p><h3 id="admin-modal-title">{adminModalTitle}</h3></div><button type="button" aria-label="Close administrator details" onClick={() => setAdminModal(null)}>×</button></header>{adminModalRecords.length ? <ul>{adminModalRecords.map((admin) => <li key={admin.id}><span>{admin.displayName.slice(0, 1).toUpperCase()}</span><div><b>{admin.displayName}</b><small>{admin.email} · {admin.academyName || "Unassigned academy"}</small></div><em data-active={admin.isActive}>{admin.isActive ? "Active" : "Deactivated"}</em></li>)}</ul> : <p className="platform-admin-empty">No administrator accounts match this view.</p>}</article></div>}
            </>
          )}
          {tab === "Billing" && (
            <>
              <section className="platform-billing-kpis" aria-label="Platform billing summary">
                <button type="button" onClick={() => setInvoiceModal("all")}><span>All invoices</span><b>{invoices.length}</b><small>Platform billing records</small></button>
                <button type="button" onClick={() => setInvoiceModal("open")}><span>Open invoices</span><b>{openInvoices.length}</b><small>Awaiting collection or review</small></button>
                <button type="button" onClick={() => setInvoiceModal("overdue")}><span>Overdue</span><b>{overdueInvoices.length}</b><small>Invoices requiring follow-up</small></button>
                <button type="button" onClick={() => setInvoiceModal("paid")}><span>Paid</span><b>{paidInvoices.length}</b><small>Collections recorded</small></button>
              </section>
              <section className="platform-billing-layout">
                <form onSubmit={createInvoice} className="platform-billing-panel platform-billing-editor">
                  <header className="platform-control-panel-header"><div><p>Create</p><h2>New platform invoice</h2></div></header>
                  <div className="platform-billing-fields">
                    <label className="platform-billing-wide"><span>Academy</span><StandardSelectField name="academyId" value={invoiceAcademyId} onChange={setInvoiceAcademyId} placeholder="Select academy" options={academies.map((academy) => ({ value: academy.id, label: academy.name }))} /></label>
                    <label><span>Invoice number</span><input name="invoiceNumber" required placeholder="e.g. PLT-2026-001" /></label>
                    <label><span>Amount</span><input name="amount" type="number" min="0" step="0.01" required placeholder="0.00" /></label>
                    <label><span>Currency</span><StandardSelectField name="currency" value={invoiceCurrency} onChange={setInvoiceCurrency} placeholder="Select currency" options={["INR", "USD", "GBP", "EUR"].map((value) => ({ value, label: value }))} /></label>
                    <StandardDateField name="periodStart" value={invoicePeriodStart} onChange={setInvoicePeriodStart} label="Period start" required />
                    <StandardDateField name="periodEnd" value={invoicePeriodEnd} onChange={setInvoicePeriodEnd} label="Period end" required />
                    <StandardDateField name="dueDate" value={invoiceDueDate} onChange={setInvoiceDueDate} label="Due date" required />
                    <button disabled={busy || !invoiceAcademyId || !invoicePeriodStart || !invoicePeriodEnd || !invoiceDueDate} className="enterprise-action-button platform-billing-action">{busy ? "Creating…" : "Create invoice"}</button>
                  </div>
                </form>
                <section className="platform-billing-panel platform-invoice-register">
                  <header className="platform-control-panel-header"><div><p>Invoice register</p><h2>Platform invoices</h2></div></header>
                  {invoices.length ? <ul>{invoices.map((item) => <li key={item.id}><div className="platform-invoice-copy"><b>{item.invoiceNumber}</b><small>{item.academyName} · Due {new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(`${item.dueDate}T12:00:00`))}</small></div><strong>{item.currency} {item.amount.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</strong><div className="platform-invoice-status"><span data-status={item.status.toLowerCase()}>{item.status}</span><StandardSelectField name={`invoice-status-${item.id}`} value={invoiceStatuses[item.id] ?? item.status} onChange={(value) => { setInvoiceStatuses((current) => ({ ...current, [item.id]: value })); void updateInvoice(item, value); }} placeholder="Update status" options={["Draft", "Issued", "Overdue", "Paid", "Void"].map((value) => ({ value, label: value }))} disabled={busy} /></div></li>)}</ul> : <p className="platform-billing-empty">No platform invoices have been created yet.</p>}
                </section>
              </section>
              {invoiceModal && <div className="platform-modal-backdrop" role="presentation"><article className="platform-modal platform-invoice-modal" role="dialog" aria-modal="true" aria-labelledby="invoice-modal-title"><header><div><p>Platform billing</p><h3 id="invoice-modal-title">{invoiceModalTitle}</h3></div><button type="button" aria-label="Close invoice details" onClick={() => setInvoiceModal(null)}>×</button></header>{invoiceModalRecords.length ? <ul>{invoiceModalRecords.map((item) => <li key={item.id}><span>₹</span><div><b>{item.invoiceNumber}</b><small>{item.academyName} · Due {new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(`${item.dueDate}T12:00:00`))}</small></div><strong>{item.currency} {item.amount.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</strong><em data-status={item.status.toLowerCase()}>{item.status}</em></li>)}</ul> : <p className="platform-billing-empty">No invoices match this view.</p>}</article></div>}
            </>
          )}
          {tab === "Support" && (
            <>
              <section className="platform-support-kpis" aria-label="Support case summary">
                <button type="button" onClick={() => setCaseModal("all")}><span>All cases</span><b>{cases.length}</b><small>Platform support records</small></button>
                <button type="button" onClick={() => setCaseModal("open")}><span>Open cases</span><b>{openCases.length}</b><small>Awaiting resolution</small></button>
                <button type="button" onClick={() => setCaseModal("urgent")}><span>High priority</span><b>{urgentCases.length}</b><small>High or urgent cases</small></button>
                <button type="button" onClick={() => setCaseModal("resolved")}><span>Resolved</span><b>{resolvedCases.length}</b><small>Closed support work</small></button>
              </section>
              <section className="platform-support-layout">
                <form onSubmit={createCase} className="platform-support-panel platform-support-editor">
                  <header className="platform-control-panel-header"><div><p>Create</p><h2>Open support case</h2></div></header>
                  <div className="platform-support-fields">
                    <label className="platform-support-wide"><span>Academy</span><StandardSelectField name="academyId" value={supportAcademyId} onChange={setSupportAcademyId} placeholder="Select academy" options={academies.map((academy) => ({ value: academy.id, label: academy.name }))} /></label>
                    <label><span>Subject</span><input name="subject" required placeholder="Brief case title" /></label>
                    <label><span>Priority</span><StandardSelectField name="priority" value={supportPriority} onChange={setSupportPriority} placeholder="Select priority" options={["Low", "Normal", "High", "Urgent"].map((value) => ({ value, label: value }))} /></label>
                    <label className="platform-support-wide"><span>Details</span><textarea name="description" rows={4} placeholder="Describe the issue or requested help" /></label>
                    <button disabled={busy || !supportAcademyId} className="enterprise-action-button platform-support-action">{busy ? "Creating…" : "Create support case"}</button>
                  </div>
                </form>
                <section className="platform-support-panel platform-case-register">
                  <header className="platform-control-panel-header"><div><p>Case register</p><h2>Support cases</h2></div></header>
                  {cases.length ? <ul>{cases.map((item) => <li key={item.id}><div className="platform-case-copy"><b>{item.subject}</b><small>{item.academyName} · {item.description || "No additional details"}</small></div><div className="platform-case-meta"><span data-priority={item.priority.toLowerCase()}>{item.priority}</span><StandardSelectField name={`case-status-${item.id}`} value={caseStatuses[item.id] ?? item.status} onChange={(value) => { setCaseStatuses((current) => ({ ...current, [item.id]: value })); void updateCase(item, value); }} placeholder="Update status" options={["Open", "In progress", "Resolved", "Closed"].map((value) => ({ value, label: value }))} disabled={busy} /></div></li>)}</ul> : <p className="platform-support-empty">No support cases have been created yet.</p>}
                </section>
              </section>
              {caseModal && <div className="platform-modal-backdrop" role="presentation"><article className="platform-modal platform-case-modal" role="dialog" aria-modal="true" aria-labelledby="case-modal-title"><header><div><p>Platform support</p><h3 id="case-modal-title">{caseModalTitle}</h3></div><button type="button" aria-label="Close support case details" onClick={() => setCaseModal(null)}>×</button></header>{caseModalRecords.length ? <ul>{caseModalRecords.map((item) => <li key={item.id}><span>?</span><div><b>{item.subject}</b><small>{item.academyName} · {item.priority} priority</small></div><em data-status={item.status.toLowerCase().replaceAll(" ", "-")}>{item.status}</em></li>)}</ul> : <p className="platform-support-empty">No support cases match this view.</p>}</article></div>}
            </>
          )}
          {tab === "Settings" && settings && (
            <section className="platform-settings-layout">
              <section className="platform-settings-panel platform-owner-settings">
                <header className="platform-control-panel-header"><div><p>Owner account</p><h2>Profile and security</h2></div></header>
                <div className="platform-owner-profile"><button type="button" className="platform-owner-photo" onClick={() => profileImageInput.current?.click()} disabled={busy}>{owner?.profileImageUrl ? <img src={`${apiUrl}${owner.profileImageUrl}`} alt="Owner profile"/> : <span>{owner?.displayName?.[0]?.toUpperCase() || "S"}</span>}<i aria-hidden="true">⌁</i></button><div><b>{owner?.displayName || "Platform Owner"}</b><span>Platform Owner</span></div></div>
                <input ref={profileImageInput} className="sr-only" type="file" accept="image/jpeg,image/png,image/webp" onChange={uploadOwnerImage}/>
                <form onSubmit={saveOwnerProfile} className="platform-owner-form">
                  <label>Display name<input name="displayName" defaultValue={owner?.displayName || ""} required/></label>
                  <label>Sign-in email<input value={owner?.email || ""} readOnly aria-label="Sign-in email"/></label>
                  <label>Phone number <em>Optional</em><input name="phoneNumber" defaultValue={owner?.phoneNumber || ""} placeholder="+91 98765 43210"/></label>
                  <p className="platform-account-meta">Role: <b>{owner?.roles?.join(", ") || "Platform Owner"}</b> · This account has platform-wide access.</p>
                  <button disabled={busy}>Save personal details</button>
                </form>
                <form onSubmit={changeOwnerPassword} className="platform-password-form"><h3>Change password</h3><p>Use your current password to set a new one.</p><label>Current password<input name="currentPassword" type="password" autoComplete="current-password" required/></label><label>New password<input name="newPassword" type="password" autoComplete="new-password" minLength={6} required/></label><label>Confirm new password<input name="confirmPassword" type="password" autoComplete="new-password" minLength={6} required/></label><button disabled={busy}>Update password</button></form>
              </section>
              <form onSubmit={saveSettings} className="platform-settings-panel platform-settings-editor">
                <header className="platform-control-panel-header"><div><p>Platform defaults</p><h2>Platform settings and retention</h2></div></header>
                <div className="platform-settings-fields">
                  <label className="platform-settings-wide"><span>Platform name</span><input name="platformName" defaultValue={settings.platformName} required /></label>
                  <label className="platform-settings-wide"><span>Support email</span><input name="supportEmail" type="email" defaultValue={settings.supportEmail || ""} /></label>
                  <label><span>Default currency</span><StandardSelectField name="currency" value={settingsCurrency} onChange={setSettingsCurrency} placeholder="Select currency" options={["INR", "USD", "GBP", "EUR"].map((value) => ({ value, label: value }))} /></label>
                  <label><span>Default trial days</span><input name="trialDays" type="number" min="1" defaultValue={settings.defaultTrialDays} required /></label>
                  <label><span>Data retention days</span><input name="retentionDays" type="number" min="30" defaultValue={settings.dataRetentionDays} required /></label>
                  <label className="platform-maintenance-toggle"><input name="maintenanceMode" type="checkbox" defaultChecked={settings.maintenanceMode} /><span><b>Maintenance mode</b><small>Temporarily restrict platform access</small></span></label>
                  <label className="platform-settings-wide"><span>Status message</span><textarea name="statusMessage" defaultValue={settings.statusMessage || ""} rows={3} placeholder="Optional maintenance or service status message" /></label>
                  <button disabled={busy} className="enterprise-action-button platform-settings-action">{busy ? "Saving…" : "Save platform settings"}</button>
                </div>
              </form>
            </section>
          )}
          {tab === "Audit & health" && (
            <section className="mt-6 grid gap-5 lg:grid-cols-2">
              <section className="rounded-xl border border-slate-800 bg-slate-900 p-5">
                <h2 className="font-semibold">Operational health</h2>
                <dl className="mt-4 space-y-3 text-sm">
                  <div>
                    <dt className="text-slate-400">API</dt>
                    <dd>{health?.api ?? "Checking"}</dd>
                  </div>
                  <div>
                    <dt className="text-slate-400">Database</dt>
                    <dd>{health?.database ?? "Checking"}</dd>
                  </div>
                  <div>
                    <dt className="text-slate-400">Background jobs</dt>
                    <dd>{health?.backgroundJobs ?? "Checking"}</dd>
                  </div>
                  <div>
                    <dt className="text-slate-400">Communication providers</dt>
                    <dd>{health?.communicationProviders ?? "Checking"}</dd>
                  </div>
                </dl>
              </section>
              <RecordList
                title="Platform audit history"
                records={audit.map((item) => (
                  <div
                    key={item.id}
                    className="border-b border-slate-800 py-3 text-sm"
                  >
                    <b>{item.action}</b>
                    <p className="mt-1 text-slate-400">
                      {item.actorName} · {item.entityType} ·{" "}
                      {new Date(item.occurredAtUtc).toLocaleString()}
                    </p>
                  </div>
                ))}
              />
            </section>
          )}
        </div>
      </section>
    </main>
  );
}

function Field({
  name,
  label,
  type = "text",
  value,
}: {
  name: string;
  label: string;
  type?: string;
  value?: string | number;
}) {
  return (
    <label className="mt-3 block text-sm text-slate-300">
      {label}
      <input
        name={name}
        type={type}
        defaultValue={value}
        required
        className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
      />
    </label>
  );
}
function FieldSelect({
  name,
  label,
  items,
}: {
  name: string;
  label: string;
  items: string[][];
}) {
  return (
    <label className="mt-3 block text-sm text-slate-300">
      {label}
      <select
        name={name}
        className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
      >
        {items.map(([value, label]) => (
          <option key={value} value={value}>
            {label}
          </option>
        ))}
      </select>
    </label>
  );
}
function RecordList({
  title,
  records,
}: {
  title: string;
  records: React.ReactNode[];
}) {
  return (
    <section className="rounded-xl border border-slate-800 bg-slate-900 p-5">
      <h2 className="font-semibold">{title}</h2>
      <div className="mt-4 space-y-3">
        {records.length ? (
          records
        ) : (
          <p className="text-sm text-slate-400">No records yet.</p>
        )}
      </div>
    </section>
  );
}
