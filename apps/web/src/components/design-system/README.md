# AcademyDesk portal visual standard

Use this folder for role-workspace UI so Admin, Teacher, Student, and Platform
screens stay visually consistent.

## Core rules

Fee plans/reminders now consume the scoped finance-workspace module:valid shared selector labels,44px wrapping actions,responsive intake/register/history and padded durable notices. Reminder figures must come from server invoice balance;never reconstruct gross minus Completed-only payments or mix currencies. Queue receipts/statuses are not delivery guarantees.

Invoice/payment screens consume finance-workspace.module.css and persistent shared selectors/optional year-month date control. Preview responsive rules are screen-only;preserve print templates/server ledger. Financial retry/recovery safeguards remain a separate High task.

- Use `StandardPanel` for titled workflow tiles. It provides the icon tile,
  uppercase accent heading, soft header gradient, border, radius, and spacing.
- Use `StandardSectionTitle` for major full-width sections such as timetable,
  attendance, and calendar.
- Page titles use `--ad-page-title-size`: exactly 30px on desktop and 28px on
  mobile. Do not introduce a larger page heading on an individual screen.
- Use `StandardAction` for labelled actions. Primary is for the one main action;
  secondary is for supporting actions.
- Use `StandardKpi` for label/value/detail summary tiles.
- Student/teacher intake native fields use `OnboardingField` for persistent
  labels and the scoped `onboarding.module.css` for responsive field/section
  spacing. Keep the existing field names, required flags and save handlers.
  `StandardSelectField` supports an optional persistent `label` independently
  of the chosen value. Teacher availability uses `StandardTimeField` with
  `intervalMinutes={1}` and `allowClear`: preserve exact HH:mm values and blank
  optional times rather than rounding them to the default 15-minute interval.
- Use `standardClasses` rather than inventing parallel layout classes.
- Class/batch/calendar/schedule host styling lives in the scoped
  `scheduling-workspace.module.css`. Keep calendar month scrolling within its
  named focusable region and retain the mobile agenda. Put page titles in a
  semantic header: old legacy `h1 + p` helper rules must not hide live status.
  Batch selected-day required times use `StandardTimeField` with `required`
  and one-minute precision. Schedule uses `StandardDateTimeField` for required
  local wall-clock composition; its caller retains timezone conversion.
  The required-value proxy blocks blank/partial submission, focuses the visible
  trigger and resets warnings after completion. Disable visible pickers and
  validation together while saving; never replace server/domain validation.
  This bounded adoption does not prove every app date/time field is migrated.
- Directory/profile pages share the scoped `records-workspace.module.css`:
  labelled phone record cards retain table semantics, action targets stay44px,
  and profile editors reuse persistent native-field labels. Preserve host data,
  request shapes and authorization; responsive styling is not a release signoff.
- Enrolment/attendance/leave use scoped `operations-workspace.module.css` for
  persistent labels, responsive intake/register/roster and44px actions. Shared
  selects can opt into `required`; optional selectors remain optional. Preserve
  lifecycle review/reason/save, session-scoped notes and authoritative decisions.
  Required leave dates keep native submit validation through the shared value
  proxy while the visible control remains the year-capable host date picker.
- Make-up/holidays/events also consume the scoped operations layout. Preserve
  make-up Offline room vs Online/Hybrid link and next-scheduled inheritance.
  Use exact-minute shared clocks and persistent optional/required labels.
  Do not change timezones as a styling edit: make-up and Events now use explicit
  IST input/display. Events uses its validated civil-time helper, not device-local
  Date parsing. Existing stored UTC values are not reinterpreted or migrated.
  General configurable academy timezone/DST support remains a separate task.
- Finance workspace/summary/expenses use scoped `finance-workspace.module.css`:
  wrapping summary cards,44px actions and a named keyboard-scroll collections
  table. Keep exact amounts, separate currencies and existing handlers intact.
  A failed finance load is unavailable data, not proof of zero balances or no
  overdue invoices. Expense review/pending states disable the date trigger too.
- Use `StandardSelectField` from `design-system/controls` for every dropdown
  that needs the AcademyDesk menu visual. Do not use a native `<select>` for
  those controls: the standard menu has the shared blue selection treatment
  and closes when the user clicks outside it.
- Use `StandardDateField` from `design-system/controls` for every visible date
  input. Do not expose the browser-native date popup; the standard picker has
  AcademyDesk month navigation, selected/today states, and outside-click close.

## Interaction standards

- Menus close when the user clicks outside them.
- Standard select/date/time popups use the host `AnchoredPopup` native popover
  top layer. Keep their DOM under the host theme; do not portal to `body` or
  replace positioning with page-local z-index offsets. Placement follows the
  trigger, scroll/resize and visual viewport, with bounded internal scrolling.
  Escape closes the popup and restores trigger focus. Select options support
  arrow/Home/End navigation and close after selection. Long labels must wrap.
- Date pickers retain direct month/year selection and date-only payloads.
  Time pickers retain the existing interval and 24-hour payload, with an explicit
  Done action. Time choices, dropdown options, calendar navigation/footer and
  modal close actions have 44px minimum height; calendar day cells remain a
  compact seven-column grid (38px minimum height), not universal 44px targets.
  These are host-owned controls consuming PENTA tokens, not vendor-source edits.
- Every action has a visible label and accessible name.
- Calendars provide previous month, today, next month, selected-day detail, and
  a direct action for applicable online or hybrid sessions.
- Full-month calendars always include the `calendarStandard` contract: a month
  grid plus an agenda, labelled class actions, and subject, time, schedule,
  teacher, and student details. Multiple assigned students use a compact count
  with an explicit show/hide roster action.
- Cards use the shared surface, border, compact spacing, and empty-state text.
- Use `StandardInteractiveTile` only for a tile with useful operational detail
  behind it. Its click must open `StandardDetailModal`, not navigate without a
  visible action. The pop-up has an explicit close action and also closes when
  the user clicks outside it.
- `StandardDetailModal` uses the native modal top layer, not a page-local
  z-index workaround. It provides inert background controls, Escape dismissal,
  initial close focus, opener focus return, restored body scrolling, a 44px
  close target and viewport-bounded content scrolling with a sticky header.
  Its CSS module uses existing host tokens; it is not a released PENTA package.
  Preserve the host-provided title, content and close callback. Device and
  screen-reader acceptance remain separate from exported-browser checks.
- Urgent banners are duration-bound and audience-targeted.
- Admin pages do not show decorative helper sentences below a page title. Keep
  form labels, data captions, error/status messages, and operational metadata;
  remove only explanatory page or tile copy that does not help complete work.

## Adoption order for Admin

1. App shell, sidebar, top bar, and responsive navigation.
2. Headings, panels, KPI cards, actions, and dropdowns.
3. Calendar, filters, tables, status badges, and empty states.
4. Banner/notification presentation, then a visual QA pass.
