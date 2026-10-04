// Fictional, recorded-only sample data for the PENTA foundation UI preview.
// PENTA never invents fees, attendance, schedules, or capacity — only values
// already recorded in AcademyDesk are shown, with a confidence + source note.

export type AreaKey = "pulse" | "executor" | "navigator" | "twin" | "autopilot";

export const AREAS: { key: AreaKey; label: string; question: string; count: number; href: string }[] = [
  { key: "pulse", label: "Pulse", question: "What needs my attention?", count: 3, href: "/penta/pulse" },
  { key: "executor", label: "Executor", question: "What should I do?", count: 2, href: "/penta/ask" },
  { key: "navigator", label: "Navigator", question: "How do we grow?", count: 5, href: "/penta/pulse" },
  { key: "twin", label: "Twin", question: "Why is this happening?", count: 0, href: "/penta/ask" },
  { key: "autopilot", label: "Autopilot", question: "What is being automated?", count: 4, href: "/penta/pulse" },
];

export type NavItem = { label: string; href: string };
export type NavGroup = { label: string; items: NavItem[] };

export const NAV_GROUPS: NavGroup[] = [
  {
    label: "Operate",
    items: [
      { label: "Students", href: "/penta/students" },
      { label: "Teachers", href: "/teachers" },
      { label: "Schedule", href: "/calendar" },
      { label: "Attendance", href: "/attendance" },
      { label: "Academics", href: "/curriculum" },
    ],
  },
  {
    label: "Money",
    items: [
      { label: "Finance", href: "/finance" },
      { label: "Fee plans", href: "/fee-plans" },
      { label: "Invoices", href: "/invoices" },
      { label: "Payroll", href: "/payroll-cycles" },
    ],
  },
  {
    label: "Grow",
    items: [
      { label: "Admissions", href: "/admissions" },
      { label: "Leads", href: "/leads" },
      { label: "Communications", href: "/communications" },
    ],
  },
  {
    label: "Admin",
    items: [
      { label: "Reports", href: "/data-operations" },
      { label: "Branches", href: "/branches" },
      { label: "Settings", href: "/admin" },
    ],
  },
];

export type Severity = "critical" | "warning" | "info";

export type Alert = {
  id: string;
  severity: Severity;
  area: AreaKey;
  title: string;
  detail: string;
  askPrompt: string;
  manualHref: string;
};

export const ALERTS: Alert[] = [
  {
    id: "alt-1",
    severity: "critical",
    area: "pulse",
    title: "3 classes have a teacher conflict tomorrow",
    detail: "Guitar Beginners A, Piano Foundations A, and Vocals Beginners all have overlapping teacher assignments for Oct 5.",
    askPrompt: "Why do tomorrow's classes have conflicts?",
    manualHref: "/calendar",
  },
  {
    id: "alt-2",
    severity: "warning",
    area: "pulse",
    title: "18 students are 15+ days overdue on fees",
    detail: "Combined overdue balance of ₹2.4L across Guitar, Piano, and Violin batches. Oldest overdue: 41 days.",
    askPrompt: "Which students are overdue on fees?",
    manualHref: "/fee-reminders",
  },
  {
    id: "alt-3",
    severity: "info",
    area: "pulse",
    title: "Attendance dropped 9% in Violin Advanced this week",
    detail: "4 of 11 enrolled students missed 2 or more sessions this week compared to a steady pattern last month.",
    askPrompt: "Why did Violin Advanced attendance drop?",
    manualHref: "/attendance",
  },
];

export type InboxTask = {
  id: string;
  priority: "high" | "medium" | "low";
  area: AreaKey;
  title: string;
  reason: string;
  context: string;
  recommendation: string;
};

export const INBOX_TASKS: InboxTask[] = [
  {
    id: "task-1",
    priority: "high",
    area: "pulse",
    title: "Move Aarav Mehta out of the conflicting Guitar slot",
    reason: "Teacher double-booked for Oct 5, 4:00 PM",
    context: "Aarav Mehta · Guitar Beginners A · STU-1042",
    recommendation: "Reassign to Guitar Beginners B, same day, 5:00 PM — same teacher, no capacity issue.",
  },
  {
    id: "task-2",
    priority: "high",
    area: "navigator",
    title: "Send a trial reminder to 6 leads going cold",
    reason: "No activity in 5+ days after a booked trial",
    context: "6 leads across Piano and Vocals interest tags",
    recommendation: "Send the standard trial follow-up template via WhatsApp and email.",
  },
  {
    id: "task-3",
    priority: "medium",
    area: "pulse",
    title: "Draft a fee reminder for 18 overdue students",
    reason: "Overdue 15+ days, no reminder sent in the last 7 days",
    context: "18 students · ₹2.4L combined overdue",
    recommendation: "Send the gentle reminder template; escalate to the firm template only past 30 days.",
  },
  {
    id: "task-4",
    priority: "low",
    area: "autopilot",
    title: "Review the weekly attendance digest automation",
    reason: "Automation ran with 2 failures this week",
    context: "Weekly attendance digest · last run Oct 3, 7:00 AM",
    recommendation: "2 branches failed to receive the digest — check their notification settings.",
  },
];

export type StudentRow = {
  id: string;
  name: string;
  code: string;
  batch: string;
  status: "Active" | "Trial" | "On hold";
  feesStatus: "Current" | "Overdue";
};

export const STUDENTS: StudentRow[] = [
  { id: "stu-1042", name: "Aarav Mehta", code: "STU-1042", batch: "Guitar Beginners A", status: "Active", feesStatus: "Overdue" },
  { id: "stu-1098", name: "Aarav Nair", code: "STU-1098", batch: "Piano Foundations A", status: "Active", feesStatus: "Current" },
  { id: "stu-1103", name: "Ananya Iyer", code: "STU-1103", batch: "Vocals Beginners", status: "Active", feesStatus: "Current" },
  { id: "stu-1130", name: "Meera Krishnan", code: "STU-1130", batch: "Violin Advanced", status: "Active", feesStatus: "Overdue" },
  { id: "stu-1174", name: "Diya Shah", code: "STU-1174", batch: "Drums Beginners", status: "Trial", feesStatus: "Current" },
  { id: "stu-1190", name: "Kabir Rao", code: "STU-1190", batch: "Keyboard Foundations", status: "Active", feesStatus: "Current" },
  { id: "stu-1205", name: "Ishaan Verma", code: "STU-1205", batch: "Guitar Beginners B", status: "On hold", feesStatus: "Overdue" },
  { id: "stu-1221", name: "Riya Kapoor", code: "STU-1221", batch: "Ukulele Beginners", status: "Active", feesStatus: "Current" },
];

export function timestamp() {
  return new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}
