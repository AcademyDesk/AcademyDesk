# AcademyDesk portal visual standard

Use this folder for role-workspace UI so Admin, Teacher, Student, and Platform
screens stay visually consistent.

## Core rules

- Use `StandardPanel` for titled workflow tiles. It provides the icon tile,
  uppercase accent heading, soft header gradient, border, radius, and spacing.
- Use `StandardSectionTitle` for major full-width sections such as timetable,
  attendance, and calendar.
- Use `StandardAction` for labelled actions. Primary is for the one main action;
  secondary is for supporting actions.
- Use `StandardKpi` for label/value/detail summary tiles.
- Use `standardClasses` rather than inventing parallel layout classes.

## Interaction standards

- Menus close when the user clicks outside them.
- Every action has a visible label and accessible name.
- Calendars provide previous month, today, next month, selected-day detail, and
  a direct action for applicable online or hybrid sessions.
- Cards use the shared surface, border, compact spacing, and empty-state text.
- Urgent banners are duration-bound and audience-targeted.

## Adoption order for Admin

1. App shell, sidebar, top bar, and responsive navigation.
2. Headings, panels, KPI cards, actions, and dropdowns.
3. Calendar, filters, tables, status badges, and empty states.
4. Banner/notification presentation, then a visual QA pass.
