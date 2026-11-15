import { validAdmissionDate } from "./penta-enrollment-assistant";

export type PlatformRead = "health" | "onboarding" | "support";
const phrases: Record<PlatformRead, Set<string>> = {
  health: new Set(["platform health", "show platform health", "system status", "show system status", "is the platform operational"]),
  onboarding: new Set(["onboarding status", "show onboarding status", "show academy onboarding", "academy onboarding progress"]),
  support: new Set(["support queue", "show support queue", "show open support cases", "how many support cases are open"]),
};
const object = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const count = (v: unknown): v is number => typeof v === "number" && Number.isSafeInteger(v) && v >= 0;
export function platformPrompt(text: string): PlatformRead | null {
  if (text.length > 120 || /[\u0000-\u001f\u007f-\u009f]/.test(text)) return null;
  const request = text.trim().replace(/[?.]+$/, "").replace(/ +/g, " ").toLowerCase();
  return (Object.keys(phrases) as PlatformRead[]).find(kind => phrases[kind].has(request)) ?? null;
}
export function platformReady(v: unknown) {
  return object(v) && v.protocol === "platform-reads.v1" && v.scope === "PlatformOwner";
}
const asOf = (v: unknown): v is string => typeof v === "string" &&
  /^\d{4}-\d{2}-\d{2}T(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d{1,7})?Z$/.test(v) && validAdmissionDate(v.slice(0, 10));

export function platformAnswer(v: unknown, kind: PlatformRead) {
  if (!object(v) || v.scope !== "PlatformOwner" || v.kind !== kind || v.sourcePath !== "/platform/control" || !asOf(v.asOfUtc) ||
      !object(v.metrics)) throw Error("Unverified platform answer.");
  const m = v.metrics, keys = Object.keys(m).length;
  let items: [string, string | number][];
  if (kind === "health" && keys === 3 && m.api === "Operational" && m.database === "Operational" && typeof m.maintenanceMode === "boolean")
    items = [["API", m.api], ["Database", m.database], ["Maintenance", m.maintenanceMode ? "On" : "Off"]];
  else if (kind === "onboarding" && keys === 4 && count(m.academyCount) && count(m.withProfile) && count(m.inProgress) && count(m.completed) &&
    m.withProfile <= m.academyCount && m.inProgress <= m.withProfile && m.completed <= m.withProfile)
    items = [["Academies", m.academyCount], ["With onboarding profiles", m.withProfile], ["In progress", m.inProgress], ["Marked complete", m.completed]];
  else if (kind === "support" && keys === 2 && count(m.open) && count(m.highOrUrgent) && m.highOrUrgent <= m.open)
    items = [["Open support cases", m.open], ["High or urgent", m.highOrUrgent]];
  else throw Error("Unverified platform metrics.");
  return { kind, asOfUtc: v.asOfUtc, items };
}
