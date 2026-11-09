// Booking and display use the same explicit IST wall clock in every browser zone.
export function trialBookingUtc(date: string, time: string): string | null {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || !/^([01]\d|2[0-3]):[0-5]\d$/.test(time)) return null;
  const calendar = new Date(`${date}T00:00:00Z`);
  if (!Number.isFinite(calendar.getTime()) || calendar.toISOString().slice(0, 10) !== date) return null;
  const instant = new Date(`${date}T${time}:00+05:30`);
  return Number.isFinite(instant.getTime()) ? instant.toISOString() : null;
}
