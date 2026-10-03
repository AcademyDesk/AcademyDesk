// PUT replaces the whole batch. Every compact editor must retain hidden settings.
export type BatchUpdateSource = {
  id: string;
  name: string;
  courseId: string;
  capacity: number;
  isActive: boolean;
  batchCode?: string | null;
  teacherId?: string | null;
  branchId?: string | null;
  waitlistCapacity?: number;
  deliveryMode?: string;
  classType?: string | null;
  sessionMinutes?: number | null;
  sessionsPerWeek?: number | null;
  meetingDaysJson?: string | null;
  meetingLink?: string | null;
  meetingPattern?: string | null;
  roomName?: string | null;
  enrollmentStatus?: string;
  adminNotes?: string | null;
  startDate?: string | null;
  endDate?: string | null;
};

export function batchUpdatePayload(
  batch: BatchUpdateSource,
  overrides: Partial<Omit<BatchUpdateSource, "id">> = {},
) {
  return {
    name: batch.name,
    batchCode: batch.batchCode ?? null,
    courseId: batch.courseId,
    teacherId: batch.teacherId ?? null,
    branchId: batch.branchId ?? null,
    capacity: batch.capacity,
    waitlistCapacity: batch.waitlistCapacity ?? 0,
    deliveryMode: batch.deliveryMode ?? "InPerson",
    classType: batch.classType ?? "Group",
    sessionMinutes: batch.sessionMinutes ?? 60,
    sessionsPerWeek: batch.sessionsPerWeek ?? 1,
    meetingDaysJson: batch.meetingDaysJson ?? null,
    meetingLink: batch.meetingLink ?? null,
    meetingPattern: batch.meetingPattern ?? null,
    roomName: batch.roomName ?? null,
    enrollmentStatus: batch.enrollmentStatus ?? "Open",
    adminNotes: batch.adminNotes ?? null,
    startDate: batch.startDate ?? null,
    endDate: batch.endDate ?? null,
    isActive: batch.isActive,
    ...overrides,
  };
}
