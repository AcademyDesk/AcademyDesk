/**
 * Canonical interactive controls for AcademyDesk role workspaces.
 *
 * Import from this module when standardising a page so dropdown and date
 * interactions always have the same surface, menu, outside-click behaviour,
 * keyboard semantics, and visual language.
 */
export { StandardDateField } from "../standard-date-field";
export { StandardSelectField } from "../standard-select-field";
export { StandardDetailModal, StandardInteractiveTile } from "./interactive";

/** Shared contract for full-month schedule views. */
export const calendarStandard = {
  monthNavigation: ["Previous month", "Today", "Next month"],
  requiredViews: ["Month grid", "Month agenda"],
  classDetails: ["Subject", "Time", "Schedule", "Teacher", "Students"],
  classActions: ["Join class", "Open class"],
  multiStudentDisclosure: "Show or hide the roster when more than one student is assigned.",
} as const;
