# Academy Desk product vision

Academy Desk should evolve into an **AI-native academy operating system**.

The target is not traditional ERP plus a chatbot.

## Target experience

Approximately 90% of operation should be AI-assisted or automated. Approximately 10% should remain traditional or manual controls for precision, fallback, administration, and governance.

The existing ERP is not discarded. The existing Next.js frontend, ASP.NET Core backend, database, APIs, permissions, business rules, and workflows remain the deterministic platform and control foundation.

The AI layer sits above that trusted domain platform.

Initial market is music academies. The architecture should be capable of supporting other instructor-led academies, such as dance and arts.

## AI interaction model

The intended experience includes:

- an Academy AI command interface
- a conversational workspace or side panel
- context-aware actions
- a proactive morning briefing
- an AI task inbox
- an owner command centre
- automation status
- human approval when appropriate

The product should not be reduced to a generic chat window.

## Core AI capabilities

### 1. AI Operator

Natural-language operational execution.

Examples:

- find student information
- add or update controlled student information
- manage enrollment workflows
- assist with batches
- scheduling actions
- attendance workflows
- communications
- operational questions

### 2. Intelligent scheduling

- schedule assistance
- availability reasoning
- conflict detection
- rescheduling
- deterministic rules
- approval-controlled actions

### 3. Fees and finance intelligence

- explain balances
- explain dues
- surface anomalies
- finance insights
- payment-related assistance

Uncontrolled autonomous financial mutations are not allowed.

### 4. Teacher and payroll intelligence

- teacher workload insights
- attendance and session information
- payroll explanation
- operational assistance

### 5. CRM, admissions, and growth

- lead management
- admissions assistance
- follow-up workflows
- lead qualification
- sales and marketing support

### 6. WhatsApp AI

AI-assisted WhatsApp customer interactions for sales, marketing, and operational communication, subject to appropriate approvals and policies.

### 7. Student and learning intelligence

- progress summaries
- practice recommendations
- teacher and student insights
- achievement and progress interpretation

### 8. Owner command centre

A proactive view of:

- issues
- pending actions
- operational risks
- financial signals
- scheduling conflicts
- growth activity
- staff and student items needing attention

### 9. Automation and Autopilot

Rule-governed automation that can execute approved repetitive operational tasks.

### 10. Enterprise platform

All AI functionality must continue respecting:

- tenant isolation
- RBAC
- auditability
- security
- deterministic business logic
- cost controls
- observability

## Conceptual AI agents

Preserve this multi-agent product direction:

| Agent | Role |
| --- | --- |
| Pulse | Proactive academy intelligence, briefings, and alerts. |
| Operator | Operational execution and admin assistance. |
| Growth | Admissions, CRM, marketing, and growth intelligence. |
| Twin | Academy intelligence that understands operational patterns and provides deeper contextual insight. |
| Autopilot | Controlled automation of approved recurring and repetitive operations. |

These may share one underlying intelligence and orchestration platform. They do not require separate duplicated AI infrastructure.

## AI architecture principles

The LLM must never directly write production database records.

Target conceptual flow:

```text
UI
→ AI Gateway / Orchestrator
→ Intent / Model
→ Tool Registry
→ Policy / Approval Layer
→ Existing Domain Services
→ Database / Queue / Messaging / Notifications
```

Use:

- typed tools
- server-side validation
- deterministic business rules
- RBAC
- tenant checks
- approval gates
- audit trails
- observability
- a usage and cost ledger
- configurable AI provider and model routing

AI suggestions do not override server-side business rules.

## Initial controlled AI MVP

Initial implementation stays controlled.

Examples:

- admin AI command interface
- read and search tools
- confirmed student and enrollment writes
- communication drafting
- preview before send where required
- audit visibility
- AI usage and cost monitoring
- feature flags
- budget controls

Avoid initially:

- autonomous destructive actions
- uncontrolled deletion
- autonomous financial changes
- uncontrolled rate changes
- credential changes
- an unrestricted student-facing chatbot
- direct database writes from an LLM

## Delivery direction

Deliver in phases. Do not attempt the entire AI platform at once.

Foundation first: AI readiness, gateway, tool contracts, permissions, and auditing.

Then progressively introduce:

- read and search assistant
- controlled operational writes
- communication assistance
- Pulse, briefings, and task inbox
- scheduling intelligence
- finance insights
- student intelligence
- grounded knowledge
- controlled Autopilot
- Growth capabilities
- Twin and deeper intelligence
- a richer AI-native frontend
- voice and document intelligence where justified

Existing enterprise remediation continues in parallel. Security and testing are not traded away to accelerate AI functionality. The continuing test program is `ENTERPRISE_TESTING_ROADMAP.md`.
