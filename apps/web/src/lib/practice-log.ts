export type PracticeStudent = { id: string; firstName: string; lastName: string; isActive: boolean };
export type PracticeLog = {
  id: string; studentId: string; practiceDate: string; minutesPracticed: number;
  focusArea?: string | null; notes?: string | null; teacherFeedback?: string | null;
  status: "Logged" | "Reviewed";
};
export type PracticeDraft = { studentId: string; practiceDate: string; minutesPracticed: number; focusArea: string; notes: string };
const record = (value: unknown): value is Record<string, unknown> => !!value && typeof value === "object" && !Array.isArray(value);
const optionalText = (value: unknown) => value == null || typeof value === "string";
export function validPracticeDate(value: unknown): value is string {
  if (typeof value !== "string" || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const parsed = new Date(`${value}T00:00:00Z`);
  return Number.isFinite(parsed.getTime()) && parsed.toISOString().slice(0,10) === value;
}
export function validPracticeStudent(value: unknown): value is PracticeStudent {
  return record(value) && typeof value.id === "string" && !!value.id.trim() &&
    typeof value.firstName === "string" && typeof value.lastName === "string" && typeof value.isActive === "boolean";
}
export function validPracticeLog(value: unknown): value is PracticeLog {
  return record(value) && typeof value.id === "string" && !!value.id.trim() && typeof value.studentId === "string" && !!value.studentId.trim() &&
    validPracticeDate(value.practiceDate) && typeof value.minutesPracticed === "number" && Number.isInteger(value.minutesPracticed) &&
    value.minutesPracticed >= 1 && value.minutesPracticed <= 1440 && (value.status === "Logged" || value.status === "Reviewed") &&
    optionalText(value.focusArea) && optionalText(value.notes) && optionalText(value.teacherFeedback);
}
/** Only create-owned fields. Lifecycle/review/academy metadata remains server-owned. */
export function practicePayload(draft: PracticeDraft) {
  return { studentId: draft.studentId, practiceDate: draft.practiceDate || null,
    minutesPracticed: draft.minutesPracticed, focusArea: draft.focusArea, notes: draft.notes };
}
export function practiceDraftError(draft: PracticeDraft, students: PracticeStudent[], todayUtc: string): string | null {
  if (!students.some(student => student.id === draft.studentId && student.isActive)) return "Select an active student from this academy.";
  if (!Number.isInteger(draft.minutesPracticed) || draft.minutesPracticed < 1 || draft.minutesPracticed > 1440) return "Practice time must be between 1 and 1440 whole minutes.";
  if (draft.practiceDate && (!validPracticeDate(draft.practiceDate) || draft.practiceDate > todayUtc)) return "Choose a valid practice date no later than today (UTC).";
  if (draft.focusArea.length > 250 || draft.notes.length > 2000) return "Focus area is limited to 250 characters and notes to 2,000 characters.";
  return null;
}
