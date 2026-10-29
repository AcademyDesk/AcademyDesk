/** Events currently share the calendar's explicit IST contract, not device time. */
export const EVENT_TIME_ZONE = "Asia/Kolkata";
export const EVENT_TIME_LABEL = "IST (UTC+05:30)";

/** Reject incomplete/impossible civil values instead of normalizing them. */
export function eventTimeToUtc(date: string, time: string): string | null {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || !/^(?:[01]\d|2[0-3]):[0-5]\d$/.test(time)) return null;
  const civil = `${date}T${time}:00`;
  const instant = new Date(`${civil}+05:30`);
  if (!Number.isFinite(instant.getTime())) return null;
  const roundTrip = new Date(instant.getTime() + 330 * 60_000).toISOString();
  if (roundTrip.slice(0, 19) !== civil) return null;
  return instant.toISOString();
}
