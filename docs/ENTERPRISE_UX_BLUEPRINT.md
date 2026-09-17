# AcademyDesk Enterprise UX Blueprint

AcademyDesk will use an enterprise application shell rather than independent pages.

## Product direction

The experience is inspired by established enterprise patterns: SAP Fiori's role-based launchpad and shell bar, Microsoft Dynamics 365 workspaces and navigation pane, and Salesforce-style object lists, record pages, and activity history. These patterns keep navigation consistent while allowing each academy role to see only the work relevant to them.

## Application shell

- Persistent left navigation with collapsible module groups.
- Persistent top bar with academy switcher, global search, notifications, help, and user menu.
- Breadcrumbs on every internal page.
- Page header with title, description, primary action, and contextual actions.
- Responsive behavior: full sidebar on desktop, compact rail/tablet, drawer navigation on mobile.
- Keyboard-accessible navigation and command/search entry points.

## Module groups

### Workspace

Dashboard, calendar, tasks, notifications, and activity.

### People

Students, guardians, teachers, staff, portal accounts, and leads.

### Academics

Courses, curriculum, batches, enrollments, sessions, attendance, lesson plans, assignments, assessments, and music progress.

### Finance

Fee plans, invoices, payments, expenses, reminders, and reports.

### Engagement

Events, holidays, certificates, resources, message templates, communication preferences, and delivery history.

### Administration

Academy profile, branches, roles, audit logs, integrations, and subscription settings.

## Page patterns

- Overview page: KPIs, alerts, recent activity, and next actions.
- List page: search, filters, saved views, bulk actions, pagination, and export.
- Record page: summary header, tabs, related records, timeline, and audit trail.
- Workflow page: guided steps with validation, draft state, and confirmation.
- Split view: list on the left and selected record details on the right for high-volume operations.

## Visual language

- Calm dark navy foundation with cyan as the primary action accent.
- Neutral surfaces and restrained status colors for success, warning, error, and information.
- Dense but readable tables; tabular numerals for metrics and finance.
- Consistent spacing, typography, focus states, empty states, loading states, and error states.
- Charts only where they improve a decision: attendance trends, fee collections, enrollment funnel, and practice progress.

## Role-based workspaces

- Owner/Manager: operational overview, finance, people, configuration, and audit.
- Front desk: leads, student onboarding, schedules, attendance support, and communication.
- Teacher: assigned batches, sessions, attendance, lesson plans, assignments, assessments, and practice feedback.
- Student/Guardian: schedule, assignments, results, attendance, practice, certificates, fees, notifications, and events.

## Delivery sequence

1. Establish the shared shell and design tokens.
2. Redesign the owner dashboard and high-volume list pages.
3. Redesign teacher and family portals.
4. Add charts, saved views, bulk actions, and keyboard shortcuts.
5. Apply responsive/mobile behavior across all modules.
6. Complete accessibility, visual regression, and end-to-end testing.
