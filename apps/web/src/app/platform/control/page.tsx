"use client";

import Link from "next/link";
import { FormEvent, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { ThemeToggle } from "@/components/theme-toggle";
import { academyApi, apiHeaders, apiUrl } from "@/lib/api";

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
type OwnerSession = { displayName: string; email?: string | null; phoneNumber?: string | null; profileImageUrl?: string | null; roles: string[] };

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
  const [owner, setOwner] = useState<OwnerSession>();
  const [selectedAcademy, setSelectedAcademy] = useState("");
  const [message, setMessage] = useState("Loading platform controls…");
  const [busy, setBusy] = useState(false);
  const profileImageInput = useRef<HTMLInputElement>(null);

  async function load() {
    const [a, ad, c, i, s, au, h, ownerResponse] = await Promise.all([
      academyApi("/api/platform/academies"),
      academyApi("/api/platform/admins"),
      academyApi("/api/platform/support-cases"),
      academyApi("/api/platform/billing-invoices"),
      academyApi("/api/platform/settings"),
      academyApi("/api/platform/audit"),
      academyApi("/api/platform/health"),
      academyApi("/api/auth/session"),
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
    setMessage("");
  }
  useEffect(() => {
    void load().catch((error) => setMessage(error.message));
  }, []);
  const selected = academies.find((academy) => academy.id === selectedAcademy);
  async function saveTenant(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    const form = new FormData(event.currentTarget);
    await request(
      `/api/platform/academies/${selected.id}/configuration`,
      "PUT",
      {
        subscriptionPlan: form.get("plan"),
        subscriptionStatus: form.get("status"),
        subscriptionEndsAtUtc: form.get("endsAt") || null,
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
  function signOut() {
    localStorage.removeItem("academydesk.accessToken");
    localStorage.removeItem("academydesk.refreshToken");
    router.push("/login");
  }

  useEffect(() => {
    const requestedTab = new URLSearchParams(window.location.search).get("tab");
    if (requestedTab && platformControlTabs.includes(requestedTab))
      setTab(requestedTab);
  }, []);

  function selectWorkspace(nextTab: (typeof platformControlTabs)[number]) {
    setTab(nextTab);
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
          <div className="platform-topbar-context" aria-hidden="true" />
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
            <p>Platform / control centre</p>
            <h1 className="platform-workspace-title">{tab}</h1>
            <span>Manage tenant configuration, support, billing and platform operations with controlled access.</span>
          </header>
          {message && (
            <p className="mt-4 rounded-lg border border-slate-700 bg-slate-900 p-3 text-sm text-slate-300">
              {message}
            </p>
          )}
          {tab === "Tenants" && (
            <section className="mt-6 grid gap-5 lg:grid-cols-[1fr_.9fr]">
              <section className="rounded-xl border border-slate-800 bg-slate-900 p-5">
                <h2 className="font-semibold">Tenant configuration</h2>
                <select
                  className="mt-4 w-full rounded border border-slate-700 bg-slate-950 p-2"
                  value={selectedAcademy}
                  onChange={(event) => setSelectedAcademy(event.target.value)}
                >
                  {academies.map((academy) => (
                    <option value={academy.id} key={academy.id}>
                      {academy.name}
                    </option>
                  ))}
                </select>
                {selected && (
                  <form
                    onSubmit={saveTenant}
                    className="mt-4 grid gap-3 sm:grid-cols-2"
                  >
                    <label>
                      Plan
                      <select
                        name="plan"
                        defaultValue={selected.subscriptionPlan}
                        className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
                      >
                        <option>Trial</option>
                        <option>Launch</option>
                        <option>Growth</option>
                        <option>Professional</option>
                        <option>Enterprise</option>
                      </select>
                    </label>
                    <label>
                      Status
                      <select
                        name="status"
                        defaultValue={selected.subscriptionStatus}
                        className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
                      >
                        <option>Trial</option>
                        <option>Active</option>
                        <option>Past due</option>
                        <option>Cancelled</option>
                      </select>
                    </label>
                    <section className="sm:col-span-2 rounded border border-cyan-800/60 bg-cyan-950/20 p-3 text-sm text-slate-300">
                      <b className="text-cyan-200">Plan-managed access</b>
                      <p className="mt-1">Selecting a plan automatically applies its student and staff limits, plus the included modules. Academy admins cannot change these entitlements.</p>
                      <p className="mt-2 text-cyan-100">Current allowance: {selected.studentLimit} students · {selected.staffLimit} staff</p>
                    </section>
                    <button
                      disabled={busy}
                      className="rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950 sm:col-span-2"
                    >
                      Save tenant configuration
                    </button>
                  </form>
                )}
              </section>
              <section className="rounded-xl border border-slate-800 bg-slate-900 p-5">
                <h2 className="font-semibold">Subscription controls</h2>
                <ul className="mt-4 space-y-3 text-sm text-slate-300">
                  {academies.map((academy) => (
                    <li
                      key={academy.id}
                      className="rounded border border-slate-700 bg-slate-950 p-3"
                    >
                      <b>{academy.name}</b>
                      <span className="ml-2 text-cyan-300">
                        {academy.subscriptionPlan} ·{" "}
                        {academy.subscriptionStatus}
                      </span>
                      <p className="mt-1 text-slate-400">
                        {academy.studentLimit} learners · {academy.staffLimit}{" "}
                        staff
                      </p>
                    </li>
                  ))}
                </ul>
              </section>
            </section>
          )}
          {tab === "Admins" && (
            <section className="mt-6 rounded-xl border border-slate-800 bg-slate-900 p-5">
              <h2 className="font-semibold">Academy administrators</h2>
              <div className="mt-4 overflow-auto">
                <table className="w-full text-left text-sm">
                  <thead className="text-slate-400">
                    <tr>
                      <th className="p-2">Administrator</th>
                      <th className="p-2">Academy</th>
                      <th className="p-2">Status</th>
                      <th className="p-2"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {admins.map((admin) => (
                      <tr key={admin.id} className="border-t border-slate-800">
                        <td className="p-2">
                          <b>{admin.displayName}</b>
                          <small className="block text-slate-400">
                            {admin.email}
                          </small>
                        </td>
                        <td className="p-2">{admin.academyName}</td>
                        <td className="p-2">
                          {admin.isActive ? "Active" : "Deactivated"}
                        </td>
                        <td className="p-2">
                          <button
                            onClick={() => void adminActive(admin)}
                            className="mr-2 text-cyan-300"
                          >
                            {admin.isActive ? "Deactivate" : "Activate"}
                          </button>
                          <button
                            onClick={() => void adminPassword(admin)}
                            className="text-cyan-300"
                          >
                            Reset password
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}
          {tab === "Billing" && (
            <section className="mt-6 grid gap-5 lg:grid-cols-[.8fr_1.2fr]">
              <form
                onSubmit={createInvoice}
                className="rounded-xl border border-slate-800 bg-slate-900 p-5"
              >
                <h2 className="font-semibold">Create platform invoice</h2>
                <FieldSelect
                  name="academyId"
                  label="Academy"
                  items={academies.map((a) => [a.id, a.name])}
                />
                <Field name="invoiceNumber" label="Invoice number" />
                <Field name="amount" label="Amount" type="number" />
                <Field name="currency" label="Currency" value="INR" />
                <Field name="periodStart" label="Period start" type="date" />
                <Field name="periodEnd" label="Period end" type="date" />
                <Field name="dueDate" label="Due date" type="date" />
                <button
                  disabled={busy}
                  className="mt-4 w-full rounded bg-cyan-400 p-2 font-semibold text-slate-950"
                >
                  Create invoice
                </button>
              </form>
              <RecordList
                title="Platform invoices"
                records={invoices.map((item) => (
                  <div
                    key={item.id}
                    className="rounded border border-slate-700 bg-slate-950 p-3"
                  >
                    <b>{item.invoiceNumber}</b> · {item.academyName}
                    <span className="float-right">
                      {item.currency} {item.amount}
                    </span>
                    <div className="mt-2">
                      <select
                        value={item.status}
                        onChange={(e) =>
                          void updateInvoice(item, e.target.value)
                        }
                        className="rounded border border-slate-700 bg-slate-900 p-1 text-xs"
                      >
                        <option>Draft</option>
                        <option>Issued</option>
                        <option>Overdue</option>
                        <option>Paid</option>
                        <option>Void</option>
                      </select>
                    </div>
                  </div>
                ))}
              />
            </section>
          )}
          {tab === "Support" && (
            <section className="mt-6 grid gap-5 lg:grid-cols-[.8fr_1.2fr]">
              <form
                onSubmit={createCase}
                className="rounded-xl border border-slate-800 bg-slate-900 p-5"
              >
                <h2 className="font-semibold">Open support case</h2>
                <FieldSelect
                  name="academyId"
                  label="Academy"
                  items={academies.map((a) => [a.id, a.name])}
                />
                <Field name="subject" label="Subject" />
                <FieldSelect
                  name="priority"
                  label="Priority"
                  items={[
                    ["Low", "Low"],
                    ["Normal", "Normal"],
                    ["High", "High"],
                    ["Urgent", "Urgent"],
                  ]}
                />
                <label className="mt-3 block text-sm text-slate-300">
                  Description
                  <textarea
                    name="description"
                    className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
                    rows={4}
                  />
                </label>
                <button
                  disabled={busy}
                  className="mt-4 w-full rounded bg-cyan-400 p-2 font-semibold text-slate-950"
                >
                  Create case
                </button>
              </form>
              <RecordList
                title="Support cases"
                records={cases.map((item) => (
                  <div
                    key={item.id}
                    className="rounded border border-slate-700 bg-slate-950 p-3"
                  >
                    <b>{item.subject}</b>
                    <span className="float-right text-cyan-300">
                      {item.priority}
                    </span>
                    <p className="mt-1 text-sm text-slate-400">
                      {item.academyName} ·{" "}
                      {item.description || "No description"}
                    </p>
                    <select
                      value={item.status}
                      onChange={(e) => void updateCase(item, e.target.value)}
                      className="mt-2 rounded border border-slate-700 bg-slate-900 p-1 text-xs"
                    >
                      <option>Open</option>
                      <option>In progress</option>
                      <option>Resolved</option>
                      <option>Closed</option>
                    </select>
                  </div>
                ))}
              />
            </section>
          )}
          {tab === "Settings" && settings && (
            <section className="mt-6 grid gap-5 xl:grid-cols-[.9fr_1.1fr]">
              <section className="platform-owner-settings rounded-xl border border-slate-800 bg-slate-900 p-5">
                <div className="platform-owner-profile"><button type="button" className="platform-owner-photo" onClick={() => profileImageInput.current?.click()} disabled={busy}>{owner?.profileImageUrl ? <img src={`${apiUrl}${owner.profileImageUrl}`} alt="Owner profile"/> : <span>{owner?.displayName?.[0]?.toUpperCase() || "S"}</span>}<i aria-hidden="true">⌁</i></button><div><h2 className="font-semibold">My owner account</h2><p>Personal identity, security and sign-in details for this Platform Owner.</p></div></div>
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
              <form
                onSubmit={saveSettings}
                className="rounded-xl border border-slate-800 bg-slate-900 p-5"
              >
              <h2 className="font-semibold">Platform settings and retention</h2><p className="platform-settings-intro">Set the defaults that apply when new academies are provisioned.</p>
              <Field
                name="platformName"
                label="Platform name"
                value={settings.platformName}
              />
              <Field
                name="supportEmail"
                label="Support email"
                value={settings.supportEmail || ""}
              />
              <Field
                name="currency"
                label="Default currency"
                value={settings.defaultCurrency}
              />
              <Field
                name="trialDays"
                label="Default trial days"
                type="number"
                value={settings.defaultTrialDays}
              />
              <Field
                name="retentionDays"
                label="Data retention days"
                type="number"
                value={settings.dataRetentionDays}
              />
              <label className="mt-3 flex gap-2 text-sm">
                <input
                  name="maintenanceMode"
                  type="checkbox"
                  defaultChecked={settings.maintenanceMode}
                />{" "}
                Maintenance mode
              </label>
              <label className="mt-3 block text-sm text-slate-300">
                Status message
                <textarea
                  name="statusMessage"
                  defaultValue={settings.statusMessage || ""}
                  className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
                  rows={3}
                />
              </label>
              <button
                disabled={busy}
                className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950"
              >
                Save platform settings
              </button>
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
