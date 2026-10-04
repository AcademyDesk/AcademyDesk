import Link from "next/link";
import { ALERTS, INBOX_TASKS } from "@/components/penta/penta-data";

const SUGGESTED_QUESTIONS = [
  "Why did revenue drop this month?",
  "Problems with tomorrow's schedule?",
  "Which leads need follow-up today?",
];

export default function PentaCommandCentrePage() {
  const openTasks = INBOX_TASKS.length;
  const critical = ALERTS.filter((alert) => alert.severity === "critical").length;

  return (
    <div className="penta-standard penta-centre">
      <header className="penta-centre-briefing">
        <span className="penta-ai-badge">
          <span aria-hidden="true">✦</span> PENTA AI
        </span>
        <h1>Good morning. {openTasks} things need you today.</h1>
        <p className="penta-subtitle">
          {critical} are urgent — a scheduling conflict tomorrow and overdue fee follow-ups. Everything else is
          routine. Approve what you agree with, or open any item manually.
        </p>
        <div className="penta-prompt-chips">
          {SUGGESTED_QUESTIONS.map((question) => (
            <Link key={question} href="/penta/ask" className="penta-prompt-chip">
              <span aria-hidden="true">✦</span> {question}
            </Link>
          ))}
        </div>
      </header>

      <section className="penta-centre-grid">
        <CentreCard title="Today" meta="3 classes, 1 conflict">
          <ul>
            <li>Guitar Beginners A conflicts with Piano Foundations A tomorrow, 4:00 PM.</li>
            <li>42 students expected across 3 batches, attendance tracking as usual.</li>
          </ul>
          <Link href="/penta/pulse" className="penta-secondary-action">Open Pulse</Link>
        </CentreCard>

        <CentreCard title="Money" meta="₹2.4L overdue · 18 students">
          <ul>
            <li>18 students are 15+ days overdue on fees, oldest at 41 days.</li>
            <li>Teacher payouts for September are on schedule, no anomalies recorded.</li>
          </ul>
          <Link href="/fee-reminders" className="penta-secondary-action">Open Finance</Link>
        </CentreCard>

        <CentreCard title="Growth" meta="6 leads going cold">
          <ul>
            <li>6 trial leads have had no activity in 5+ days.</li>
            <li>Piano and Vocals interest tags are converting above last month's rate.</li>
          </ul>
          <Link href="/leads" className="penta-secondary-action">Open Leads</Link>
        </CentreCard>

        <CentreCard title="PENTA Tasks" meta={`${openTasks} waiting for approval`}>
          <ul>
            {INBOX_TASKS.slice(0, 2).map((task) => (
              <li key={task.id}>{task.title}</li>
            ))}
          </ul>
          <Link href="/penta/inbox" className="penta-primary-action">Review in Inbox</Link>
        </CentreCard>
      </section>
    </div>
  );
}

function CentreCard({ title, meta, children }: { title: string; meta: string; children: React.ReactNode }) {
  return (
    <article className="penta-centre-card">
      <header>
        <h2>{title}</h2>
        <span>{meta}</span>
      </header>
      {children}
    </article>
  );
}
