"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = {
  id: string;
  name: string;
  legalName?: string;
  countryCode: string;
  timeZone: string;
  isActive: boolean;
};
type ControlCard = {
  title: string;
  description: string;
  href: string;
  action: string;
};
const groups: { title: string; cards: ControlCard[] }[] = [
  {
    title: "People, access & safeguarding",
    cards: [
      {
        title: "Team and staff access",
        description:
          "Create staff accounts, link teacher records, reset access and manage roles.",
        href: "/staff",
        action: "Open staff →",
      },
      {
        title: "Teachers and teaching profiles",
        description:
          "Manage teachers, specialisms and their teaching assignments.",
        href: "/teachers",
        action: "Open teachers →",
      },
      {
        title: "Family and portal access",
        description:
          "Link parents and create student or parent portal accounts.",
        href: "/portal-accounts",
        action: "Open portal accounts →",
      },
      {
        title: "Student records",
        description:
          "Manage student records and connected academic information.",
        href: "/students",
        action: "Open student management →",
      },
    ],
  },
  {
    title: "Academic delivery & operations",
    cards: [
      {
        title: "Program and curriculum",
        description:
          "Control courses, curriculum modules, lesson plans and learning resources.",
        href: "/courses",
        action: "Open academics →",
      },
      {
        title: "Batches and enrolments",
        description:
          "Place students, control batch capacity and keep academic participation current.",
        href: "/enrollments",
        action: "Open enrolments →",
      },
      {
        title: "Schedule and attendance",
        description:
          "Run the daily timetable, attendance, leave, make-ups and holidays.",
        href: "/schedule",
        action: "Open schedule →",
      },
      {
        title: "Assessment and certificates",
        description:
          "Manage assessments, results, music progress and achievement records.",
        href: "/assessments",
        action: "Open assessments →",
      },
    ],
  },
  {
    title: "Finance, engagement & governance",
    cards: [
      {
        title: "Finance controls",
        description:
          "Set fee plans, manage invoices, payments, reminders and expenses.",
        href: "/finance",
        action: "Open finance →",
      },
      {
        title: "Communication controls",
        description:
          "Configure channels, templates, preferences and operational messages.",
        href: "/communication-settings",
        action: "Open communications →",
      },
      {
        title: "Branches and academy calendar",
        description:
          "Manage branches, regional holidays, events and operating calendar.",
        href: "/branches",
        action: "Open branches →",
      },
      {
        title: "Audit and reports",
        description:
          "Review changes, exports and academy-wide operating reports.",
        href: "/reports",
        action: "Open reporting →",
      },
    ],
  },
];

export default function AcademyControlPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [message, setMessage] = useState("Loading academy controls…");
  const [saving, setSaving] = useState(false);
  useEffect(() => {
    void academyApi("/api/academies", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) throw new Error();
        const rows: Academy[] = await response.json();
        if (!rows[0]) throw new Error();
        setAcademy(rows[0]);
        setMessage("");
      })
      .catch(() => setMessage("Academy controls could not be loaded."));
  }, []);
  async function saveProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const form = new FormData(event.currentTarget);
    setSaving(true);
    try {
      const response = await academyApi(`/api/academies/${academy.id}`, {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          name: form.get("name"),
          legalName: form.get("legalName") || null,
          countryCode: form.get("countryCode"),
          timeZone: form.get("timeZone"),
        }),
      });
      if (!response.ok) throw new Error();
      const updated = await response.json();
      setAcademy(updated);
      setMessage("Academy profile saved.");
    } catch {
      setMessage("The academy profile could not be saved.");
    } finally {
      setSaving(false);
    }
  }
  return (
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <p>Administration / Academy control</p>
        <h2>Academy control centre</h2>
        <span>
          One place for the academy administrator to control the people,
          delivery, finance, communications, and governance of this academy.
        </span>
      </header>
      {message && (
        <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-100">
          {message}
        </p>
      )}
      <section className="mt-6 grid gap-5 xl:grid-cols-[.8fr_1.2fr]">
        <form onSubmit={saveProfile} className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">Academy profile</h3>
          <p className="mt-1 text-sm text-slate-400">
            This applies only to your academy; Platform Owner controls SaaS
            tenancy.
          </p>
          {academy && (
            <>
              <label className="mt-5 block text-sm">
                Academy name
                <input
                  name="name"
                  required
                  defaultValue={academy.name}
                  className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
                />
              </label>
              <label className="mt-3 block text-sm">
                Legal business name
                <input
                  name="legalName"
                  defaultValue={academy.legalName || ""}
                  className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
                />
              </label>
              <label className="mt-3 block text-sm">
                Country code
                <input
                  name="countryCode"
                  required
                  defaultValue={academy.countryCode}
                  className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
                />
              </label>
              <label className="mt-3 block text-sm">
                Time zone
                <input
                  name="timeZone"
                  required
                  defaultValue={academy.timeZone}
                  className="mt-1 w-full rounded border border-slate-700 bg-slate-950 p-2"
                />
              </label>
              <button
                disabled={saving}
                className="mt-5 w-full rounded bg-cyan-400 p-2.5 font-semibold text-slate-950"
              >
                {saving ? "Saving…" : "Save academy profile"}
              </button>
            </>
          )}
        </form>
        <section className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">Administrator guardrails</h3>
          <div className="mt-4 grid gap-3 text-sm">
            <p className="rounded border border-slate-700 bg-slate-950 p-3">
              <b>Tenant boundary:</b> this workspace manages only{" "}
              {academy?.name ?? "your academy"}. Subscription, platform-level
              support and cross-tenant settings belong to Platform Owner.
            </p>
            <p className="rounded border border-slate-700 bg-slate-950 p-3">
              <b>Auditability:</b> review academy activity after sensitive
              staff, finance, communication and operational changes.
            </p>
            <p className="rounded border border-slate-700 bg-slate-950 p-3">
              <b>Operating order:</b> create student → enrol to batch → schedule
              class → mark attendance → invoice and communicate.
            </p>
            <Link href="/activity" className="text-cyan-300">
              Academy activity log
            </Link>
          </div>
        </section>
      </section>
      {groups.map((group) => (
        <section key={group.title} className="mt-7">
          <h3 className="text-lg font-semibold">{group.title}</h3>
          <div className="enterprise-settings-grid">
            {group.cards.map((card) => (
              <Link
                key={card.title}
                href={card.href}
                className="enterprise-setting-card"
              >
                <div>
                  <span>Academy admin</span>
                  <h3>{card.title}</h3>
                  <p>{card.description}</p>
                </div>
                <footer>
                  <small>Scoped to this academy</small>
                  <b>{card.action}</b>
                </footer>
              </Link>
            ))}
          </div>
        </section>
      ))}
    </main>
  );
}
