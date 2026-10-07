import Link from "next/link";
import styles from "./workspace-directory.module.css";

const modules = [
  ["People", "Student, teacher and staff records", [["Students", "/students"], ["Teachers", "/teachers"], ["Staff", "/staff"], ["Portal access", "/portal-accounts"]]],
  ["Classes", "Scheduling, enrollment and attendance", [["Classes & batches", "/batch-setup"], ["Calendar", "/calendar"], ["Enrollments", "/enrollments"], ["Attendance", "/attendance"], ["Make-up classes", "/makeup"]]],
  ["Learning", "Courses, assessments and progress", [["Courses", "/courses"], ["Assignments", "/assignments"], ["Assessments", "/assessments"], ["Certificates", "/certificates"]]],
  ["Finance", "Billing, collections and controls", [["Invoices", "/invoices"], ["Payments", "/payments"], ["Reconciliation", "/finance-reconciliation"], ["Payroll", "/payroll"]]],
  ["Admissions", "Leads and follow-up workflows", [["Leads", "/leads"], ["Trial bookings", "/trial-bookings"], ["Campaigns", "/sales-campaigns"]]],
  ["Communication", "Messages, templates and preferences", [["Messages", "/communications"], ["Templates", "/message-templates"], ["Contact preferences", "/communication-preferences"]]],
  ["Administration", "Academy configuration and governance", [["Academy control", "/admin/control"], ["Branches", "/branches"], ["Compliance", "/compliance"], ["Audit activity", "/activity"]]],
] as const;

export function WorkspaceDirectory({ included }: { included: (href: string) => boolean }) {
  return <main className={styles.page}><header><span>Direct control</span><h1>Your academy workspace</h1><p>Choose a module to work directly. Your role and subscription still govern access.</p></header><div className={styles.grid}>{modules.map(([title, description, links]) => <section key={title}><h2>{title}</h2><p>{description}</p><nav aria-label={`${title} modules`}>{links.map(([label, href]) => included(href) ? <Link href={href} key={href}>{label}<span aria-hidden="true">↗</span></Link> : <span key={href} className={styles.unavailable}>{label}<small>Not in current plan</small></span>)}</nav></section>)}</div></main>;
}
