# AcademyDesk portal visual standard

Use this folder for role-workspace UI so Admin, Teacher, Student, and Platform
screens stay visually consistent.

## Core rules

- Use `StandardPanel` for titled workflow tiles. It provides the icon tile,
  uppercase accent heading, soft header gradient, border, radius, and spacing.
- Use `StandardSectionTitle` for major full-width sections such as timetable,
  attendance, and calendar.
- Page titles use `--ad-page-title-size`: exactly 30px on desktop and 28px on
  mobile. Do not introduce a larger page heading on an individual screen.
- Use `StandardAction` for labelled actions. Primary is for the one main action;
  secondary is for supporting actions.
- Use `StandardKpi` for label/value/detail summary tiles.
- Use `standardClasses` rather than inventing parallel layout classes.
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
