// Course PUT replaces all settings. Compact editors must retain the full summary.
export type CourseUpdateSource = {
  id: string;
  name: string;
  courseCode: string | null;
  academyType: string;
  subjectArea: string | null;
  level: string | null;
  description: string | null;
  durationMonths: number | null;
  weeklySessions: number | null;
  sessionMinutes: number | null;
  minimumAge: number | null;
  maximumAge: number | null;
  deliveryMode: string | null;
  prerequisites: string | null;
  learningOutcomes: string | null;
  isPublished: boolean;
  isActive: boolean;
};

export function courseUpdatePayload(
  course: CourseUpdateSource,
  overrides: Partial<Pick<CourseUpdateSource, "name" | "academyType" | "level" | "isActive">> = {},
) {
  return {
    name: course.name,
    courseCode: course.courseCode,
    academyType: course.academyType,
    subjectArea: course.subjectArea,
    level: course.level,
    description: course.description,
    durationMonths: course.durationMonths,
    weeklySessions: course.weeklySessions,
    sessionMinutes: course.sessionMinutes,
    minimumAge: course.minimumAge,
    maximumAge: course.maximumAge,
    deliveryMode: course.deliveryMode,
    prerequisites: course.prerequisites,
    learningOutcomes: course.learningOutcomes,
    isPublished: course.isPublished,
    isActive: course.isActive,
    ...overrides,
  };
}
