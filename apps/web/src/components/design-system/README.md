# AcademyDesk portal visual standard

Use this folder for role-workspace UI so Admin, Teacher, Student, and Platform
screens stay visually consistent.

## Core rules

- Use `StandardPanel` for titled workflow tiles. It provides the icon tile,
  uppercase accent heading, soft header gradient, border, radius, and spacing.
- Use `StandardSectionTitle` for major full-width sections such as timetable,
  attendance, and calendar.
- Page titles use `--ad-page-title-size`: exactly 36px on desktop and 32px on
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
- Every action has a visible label and accessible name.
- Calendars provide previous month, today, next month, selected-day detail, and
  a direct action for applicable online or hybrid sessions.
- Full-month calendars always include the `calendarStandard` contract: a month
  grid plus an agenda, labelled class actions, and subject, time, schedule,
  teacher, and student details. Multiple assigned students use a compact count
  with an explicit show/hide roster action.
- Cards use the shared surface, border, compact spacing, and empty-state text.
- Urgent banners are duration-bound and audience-targeted.

## Adoption order for Admin

1. App shell, sidebar, top bar, and responsive navigation.
2. Headings, panels, KPI cards, actions, and dropdowns.
3. Calendar, filters, tables, status badges, and empty states.
4. Banner/notification presentation, then a visual QA pass.
