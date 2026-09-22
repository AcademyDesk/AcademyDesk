import type { ButtonHTMLAttributes, HTMLAttributes, ReactNode } from "react";

/**
 * Shared portal visual primitives. Use these before adding one-off card, title,
 * action, or field styling to a role workspace.
 */
export function StandardPanel({ title, icon = "◌", children, className = "" }: { title: string; icon?: string; children: ReactNode; className?: string }) {
  return <section className={`teacher-action-panel ${className}`.trim()}><header><i aria-hidden="true">{icon}</i><h3>{title}</h3></header><div className="learner-list">{children}</div></section>;
}

export function StandardSectionTitle({ title, icon = "◌" }: { title: string; icon?: string }) {
  return <header className="teacher-section-heading"><div><i aria-hidden="true">{icon}</i><h2>{title}</h2></div></header>;
}

export function StandardAction({ children, variant = "primary", className = "", ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" }) {
  const style = variant === "primary" ? "enterprise-action-button" : "enterprise-action-button enterprise-action-button-secondary";
  return <button type="button" className={`${style} ${className}`.trim()} {...props}>{children}</button>;
}

export function StandardKpi({ label, value, detail, ...props }: { label: string; value: string; detail: string } & HTMLAttributes<HTMLElement>) {
  return <article className="learner-kpi" {...props}><span>{label}</span><b>{value}</b><small>{detail}</small></article>;
}

export const standardClasses = {
  shell: "enterprise-app-shell",
  sidebar: "enterprise-sidebar",
  topbar: "enterprise-topbar",
  content: "learner-content",
  twoColumnGrid: "learner-tab-grid",
  fullWidth: "learner-grid-wide",
  filterBar: "learner-filter-bar",
  compactRow: "learner-row",
  dropdown: "teacher-dropdown",
  calendar: "teacher-calendar-panel",
} as const;
