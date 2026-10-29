export type ActivityRecord = {
  id: string;
  action: string;
  entityType: string;
  metadataJson?: string | null;
  occurredAtUtc: string;
};

export function validActivityRecord(value: unknown): value is ActivityRecord {
  if (!value || typeof value !== "object" || Array.isArray(value)) return false;
  const row = value as Record<string, unknown>;
  return [row.id, row.action, row.entityType].every(value => typeof value === "string" && value.trim().length > 0)
    && typeof row.occurredAtUtc === "string"
    && Number.isFinite(activityTimestamp(row.occurredAtUtc))
    && (row.metadataJson == null || typeof row.metadataJson === "string");
}

// SQL-backed DateTime can serialize without a suffix. This endpoint's
// OccurredAtUtc contract is UTC; do not reinterpret it in the device timezone.
export function activityTimestamp(value: string): number {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})?$/.test(value)) return NaN;
  return Date.parse(/(?:Z|[+-]\d{2}:\d{2})$/.test(value) ? value : `${value}Z`);
}

// The same civil timezone as the existing displayed time, not the device zone
// or a locale-dependent date-string order.
export function activityDateKey(value: string): string {
  const parts = new Intl.DateTimeFormat("en-GB", {
    timeZone: "Asia/Kolkata", year: "numeric", month: "2-digit", day: "2-digit",
  }).formatToParts(new Date(activityTimestamp(value)));
  const part = (type: Intl.DateTimeFormatPartTypes) => parts.find(part => part.type === type)?.value;
  return `${part("year")}-${part("month")}-${part("day")}`;
}

export function activityMatches(row: ActivityRecord, filters: {
  query: string; entity: string; period: string; date: string; now: number;
}): boolean {
  const duration = filters.period === "Today" ? 86400000
    : filters.period === "7 days" ? 604800000 : filters.period === "30 days" ? 2592000000 : 0;
  const age = filters.now - activityTimestamp(row.occurredAtUtc);
  return (!filters.entity || row.entityType === filters.entity)
    && (!filters.date || activityDateKey(row.occurredAtUtc) === filters.date)
    && (!filters.period || (duration > 0 && age >= 0 && age <= duration))
    && `${row.action} ${row.entityType} ${row.metadataJson ?? ""}`.toLowerCase().includes(filters.query.toLowerCase());
}
