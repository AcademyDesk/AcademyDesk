import { validAdmissionDate } from "./penta-enrollment-assistant";

export const portalPentaRoot = "/api/portal/penta";
export type PortalRead = "next-class" | "due-work" | "outstanding-fees";
export type PortalLearner = { id: string; name: string; academic: boolean; finance: boolean };
export type PortalPrompt = { kind: PortalRead; learnerName: string | null };

const object = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
const clean = (v: unknown, length: number): v is string => typeof v === "string" && v.trim().length > 0 &&
  v.length <= length && !/[\u0000-\u001f\u007f-\u009f]/.test(v);
const utc = (v: unknown): v is string => typeof v === "string" && /^\d{4}-\d{2}-\d{2}T(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d{1,7})?Z$/.test(v) && validAdmissionDate(v.slice(0, 10));

const phrases: Record<PortalRead, Set<string>> = {
  "next-class": new Set(["next class", "show my next class", "when is my next class", "show the next class", "when is the next class"]),
  "due-work": new Set(["due work", "show due work", "show my due work", "what work is due", "what homework is due", "upcoming assignments"]),
  "outstanding-fees": new Set(["outstanding fees", "show outstanding fees", "show my outstanding fees", "what fees are outstanding", "what do i owe", "how much do i owe"]),
};

export function portalPrompt(input: string): PortalPrompt | null {
  if (!clean(input, 140)) return null;
  const text = input.trim().replace(/[?.]+$/, "").replace(/ +/g, " ");
  const [phrase, learnerName] = text.split(/ for /i);
  const kind = (Object.keys(phrases) as PortalRead[]).find(x => phrases[x].has(phrase.toLowerCase()));
  if (!kind || (learnerName !== undefined && (!clean(learnerName, 80) || text.split(/ for /i).length !== 2))) return null;
  return { kind, learnerName: learnerName ?? null };
}

export function portalLearners(value: unknown, academyId: string): { learners: PortalLearner[]; hasMore: boolean } {
  if (!object(value) || value.protocol !== "portal-reads.v1" || !id(academyId) || value.academyId !== academyId ||
      typeof value.hasMore !== "boolean" || !Array.isArray(value.learners) || value.learners.length > 10)
    throw Error("Unverified linked learners.");
  const learners = value.learners.map(row => {
    if (!object(row) || Object.keys(row).length !== 4 || !id(row.id) || !clean(row.name, 500) ||
        typeof row.academic !== "boolean" || typeof row.finance !== "boolean") throw Error("Invalid linked learner.");
    return { id: row.id, name: row.name, academic: row.academic, finance: row.finance };
  });
  if (new Set(learners.map(row => row.id)).size !== learners.length) throw Error("Duplicate linked learners.");
  return { learners, hasMore: value.hasMore };
}

export function resolvePortalLearner(prompt: PortalPrompt, learners: PortalLearner[], hasMore: boolean, selectedId: string | null) {
  if (prompt.learnerName) {
    const name = prompt.learnerName.toLowerCase();
    const exact = learners.filter(x => x.name.toLowerCase() === name);
    const matches = exact.length ? exact : learners.filter(x => x.name.toLowerCase().includes(name));
    return !hasMore && matches.length === 1 ? matches[0] : undefined;
  }
  if (selectedId) return learners.find(x => x.id === selectedId);
  return !hasMore && learners.length === 1 ? learners[0] : undefined;
}

type Class = { batchName: string; startUtc: string; endUtc: string; deliveryMode: string };
type Work = { title: string; dueAtUtc: string };
type Fee = { invoiceNumber: string; currency: string; balance: string; dueDate: string };
export type PortalAnswer = { asOfUtc: string } & (
  { kind: "next-class"; rows: Class[] } | { kind: "due-work"; rows: Work[] } | { kind: "outstanding-fees"; rows: Fee[] });

export function portalAnswer(value: unknown, academyId: string, learnerId: string, kind: PortalRead): PortalAnswer {
  if (!object(value) || value.academyId !== academyId || value.studentId !== learnerId || value.kind !== kind ||
      value.sourcePath !== "/portal" || !utc(value.asOfUtc) || !Array.isArray(value.rows) ||
      value.rows.length > (kind === "next-class" ? 1 : 10)) throw Error("Unverified portal answer.");
  for (const row of value.rows) {
    if (!object(row)) throw Error("Invalid answer row.");
    const keys = Object.keys(row).length;
    const valid = kind === "next-class" ? keys === 4 && clean(row.batchName, 500) && utc(row.startUtc) &&
      utc(row.endUtc) && Date.parse(row.endUtc) > Date.parse(row.startUtc) && clean(row.deliveryMode, 40)
      : kind === "due-work" ? keys === 2 && clean(row.title, 500) && utc(row.dueAtUtc)
        : keys === 4 && clean(row.invoiceNumber, 120) && typeof row.currency === "string" && /^[A-Z]{3}$/.test(row.currency) &&
          typeof row.balance === "string" && /^\d{1,22}\.\d{2}$/.test(row.balance) && row.balance !== "0.00" &&
          typeof row.dueDate === "string" && validAdmissionDate(row.dueDate);
    if (!valid) throw Error("Invalid portal answer row.");
  }
  return { kind, rows: value.rows, asOfUtc: value.asOfUtc } as PortalAnswer;
}
