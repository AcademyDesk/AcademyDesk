"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

type SettingItem = { title: string; description: string; href: string; group: string; state: string };

const settingItems: SettingItem[] = [
  { group: "Organisation", title: "Academy profile", description: "Name, legal identity, time zone, and academy-wide defaults.", href: "/", state: "Configured in Academy profile" },
  { group: "Organisation", title: "Branches", description: "Teaching locations, branch status, and local operations.", href: "/branches", state: "Open branch settings" },
  { group: "Access", title: "Staff and roles", description: "Invite staff, create teacher accounts, and manage access lifecycle.", href: "/staff", state: "Open staff directory" },
  { group: "Access", title: "Portal accounts", description: "Student, guardian, and teacher portal access links.", href: "/portal-accounts", state: "Open portal accounts" },
  { group: "Communications", title: "Channel settings", description: "Configure the academy’s email and WhatsApp delivery channels.", href: "/communication-settings", state: "Open channel settings" },
  { group: "Communications", title: "Message templates", description: "Create approved templates for fees, holidays, attendance, and operations.", href: "/message-templates", state: "Open templates" },
  { group: "Communications", title: "Contact preferences", description: "Respect recipient consent and channel preferences.", href: "/communication-preferences", state: "Open preferences" },
  { group: "Operations", title: "Holiday calendar", description: "Maintain academy closures and regional Indian public holidays.", href: "/holidays", state: "Open holiday calendar" },
  { group: "Operations", title: "Fee reminders", description: "Review fee follow-up workflow and queued reminders.", href: "/fee-reminders", state: "Open reminder workflow" },
  { group: "Governance", title: "Activity and audit", description: "Review operational activity and the academy audit record.", href: "/activity", state: "Open activity log" },
];

const groups = ["All", "Organisation", "Access", "Communications", "Operations", "Governance"];

export default function SettingsPage() {
  const [group, setGroup] = useState("All");
  const [query, setQuery] = useState("");
  const visibleItems = useMemo(() => settingItems.filter((item) => (group === "All" || item.group === group) && `${item.title} ${item.description}`.toLowerCase().includes(query.toLowerCase().trim())), [group, query]);

  return <main className="enterprise-settings">
    <header className="enterprise-page-header"><div><p>Administration / Settings</p><h2>Settings</h2><span>Control your academy configuration, access, communications, and governance from one place.</span></div></header>
    <section className="enterprise-settings-toolbar"><div className="enterprise-settings-tabs" role="tablist" aria-label="Settings categories">{groups.map((item) => <button key={item} type="button" role="tab" aria-selected={group === item} onClick={() => setGroup(item)}>{item}</button>)}</div><label className="enterprise-settings-search"><span aria-hidden="true">⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search settings" aria-label="Search settings" /></label></section>
    <section className="enterprise-settings-grid" aria-label="Settings options">{visibleItems.map((item) => <Link key={item.title} href={item.href} className="enterprise-setting-card"><div><span>{item.group}</span><h3>{item.title}</h3><p>{item.description}</p></div><footer><small>{item.state}</small><b aria-hidden="true">→</b></footer></Link>)}{visibleItems.length === 0 && <p className="enterprise-settings-empty">No settings match that search.</p>}</section>
  </main>;
}
