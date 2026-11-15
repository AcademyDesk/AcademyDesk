import { readPentaJson } from "./penta-onboarding-channel";
import { validAdmissionDate } from "./penta-enrollment-assistant";

export const attendanceRoot = "/api/teacher/penta/attendance";
export const attendancePointer = "academydesk.penta.teacher-attendance-recovery.v1";
export type AttendanceStatus = "Present" | "Absent";
export type AttendanceClass = { id: string; batchId: string; batchName: string; startUtc: string };
export type AttendanceLearner = { id: string; name: string };
type Options<T> = { rows: T[]; hasMore: boolean };
type Change = { studentId: string; status: AttendanceStatus; notes: string | null; inAppMessage: string };
type Receipt = { id: string; auditId: string; attendanceId: string; markedAtUtc: string; inAppQueued: 1 };
type Draft = { requestId: string; classSessionId: string; batchId: string; sessionStartUtc: string; expiresAtUtc: string;
  payloadHash: string; change: Change } & ({ status: "Prepared"; receipt: null } | { status: "Succeeded"; receipt: Receipt });
type Pointer = { epoch: string; academyId: string; sessionId: string; requestId: string };
type Transport = { academyId: string; epoch: string; signal: AbortSignal; current(): boolean; invalidate(): void;
  send(path: string, method: "GET" | "PUT" | "POST", body?: unknown): Promise<Response> };

const obj = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== "00000000-0000-0000-0000-000000000000";
const clean = (v: unknown, max: number): v is string => typeof v === "string" && v.length <= max && !/[\u0000-\u001f\u007f-\u009f]/.test(v);
const utc = (v: unknown): v is string => typeof v === "string" && /^\d{4}-\d{2}-\d{2}T(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d{1,7})?Z$/.test(v) && validAdmissionDate(v.slice(0, 10));
const isStatus = (v: unknown): v is AttendanceStatus => v === "Present" || v === "Absent";

export function attendancePrompt(text: string): string {
  const match = /^(?:mark|take|record) attendance for (.+)$/i.exec(text.trim());
  if (!match) throw Error("Try “Mark attendance for Piano”.");
  return attendanceQuery(match[1]);
}
export function attendanceQuery(text: string): string {
  const q = text.trim();
  if (!clean(q, 80) || q.length < 2) throw Error("Enter 2–80 characters of the name.");
  return q;
}
function options<T extends { id: string }>(value: unknown, academy: string, sessionId: string | null,
  parse: (row: Record<string, unknown>) => T | null): Options<T> {
  if (!obj(value) || value.academyId !== academy || value.sessionId !== sessionId || typeof value.hasMore !== "boolean" ||
    !Array.isArray(value.rows) || value.rows.length > 10) throw Error("Invalid attendance options.");
  const rows = value.rows.map(row => {
    const parsed = obj(row) ? parse(row) : null;
    if (!parsed) throw Error("Invalid attendance option.");
    return parsed;
  });
  if (new Set(rows.map(row => row.id)).size !== rows.length) throw Error("Duplicate attendance options.");
  return { rows, hasMore: value.hasMore };
}
export function attendanceClasses(value: unknown, academy: string) {
  return options<AttendanceClass>(value, academy, null, row => Object.keys(row).length === 4 &&
    id(row.id) && id(row.batchId) && clean(row.batchName, 500) && !!row.batchName.trim() && utc(row.startUtc)
    ? { id: row.id, batchId: row.batchId, batchName: row.batchName, startUtc: row.startUtc } : null);
}
export function attendanceRoster(value: unknown, academy: string, sessionId: string) {
  return options<AttendanceLearner>(value, academy, sessionId, row => Object.keys(row).length === 2 && id(row.id) &&
    clean(row.name, 500) && !!row.name.trim() ? { id: row.id, name: row.name } : null);
}

export function attendanceDraft(value: unknown, pointer: Pointer): Draft {
  if (!obj(value) || value.requestId !== pointer.requestId || value.classSessionId !== pointer.sessionId || !id(value.batchId) ||
      !utc(value.sessionStartUtc) || !utc(value.expiresAtUtc) || typeof value.payloadHash !== "string" ||
      !/^[0-9a-f]{64}$/.test(value.payloadHash) || !obj(value.change) || !id(value.change.studentId) ||
      !isStatus(value.change.status) || (value.change.notes !== null && !clean(value.change.notes, 500)) ||
      value.change.inAppMessage !== "Your attendance for the class has been marked " + value.change.status + ".")
    throw Error("Invalid reviewed attendance.");
  if (value.status === "Prepared" && value.receipt === null) return value as Draft;
  const receipt = value.receipt;
  if (value.status !== "Succeeded" || !obj(receipt) || !id(receipt.id) || !id(receipt.auditId) ||
      !id(receipt.attendanceId) || !utc(receipt.markedAtUtc) || receipt.inAppQueued !== 1)
    throw Error("Invalid attendance receipt.");
  return value as Draft;
}
function pointerFrom(storage: Storage, academy: string, epoch: string): Pointer | null {
  const raw = storage.getItem(attendancePointer);
  if (!raw) return null;
  try {
    const p: unknown = JSON.parse(raw);
    if (obj(p) && Object.keys(p).length === 4 && p.epoch === epoch && p.academyId === academy &&
        id(p.sessionId) && id(p.requestId)) return { academyId: academy, epoch, sessionId: p.sessionId, requestId: p.requestId };
  } catch { /* Old or invalid records carry no authority. */ }
  storage.removeItem(attendancePointer);
  return null;
}

export type AttendanceView = {
  phase: "loading" | "unavailable" | "idle" | "unknown" | "review" | "succeeded" | "closed";
  ready: boolean; pending: boolean; notice: string; classes: Options<AttendanceClass> | null; selected: AttendanceClass | null;
  roster: Options<AttendanceLearner> | null; draft: Draft | null; pointer: Pointer | null;
};
export const initialAttendanceView: AttendanceView = { phase: "loading", ready: false, pending: false, notice: "Checking attendance access…",
  classes: null, selected: null, roster: null, draft: null, pointer: null };
class RequestError extends Error { constructor(readonly code: number) { super("Attendance response refused."); } }
class StorageError extends Error {}

export function createTeacherAttendance(channel: Transport, storage: () => Storage, changed: (view: AttendanceView) => void,
  uuid = () => crypto.randomUUID(), now = Date.now) {
  let view = initialAttendanceView, busy = false, disposed = false, previous: Draft | null = null;
  let expected: { session: AttendanceClass; learner: AttendanceLearner; status: AttendanceStatus; notes: string | null } | null = null;
  const current = () => !disposed && !channel.signal.aborted && channel.current();
  const patch = (fields: Partial<AttendanceView>) => { if (current()) { view = { ...view, ...fields }; changed(view); } };
  const key = () => pointerFrom(storage(), channel.academyId, channel.epoch);
  const path = (p: Pointer) => "/sessions/" + p.sessionId + "/drafts/" + p.requestId;

  function persist(p: Pointer) {
    try {
      const saved = key();
      if (saved && (saved.requestId !== p.requestId || saved.sessionId !== p.sessionId)) throw Error("Different request already saved.");
      storage().setItem(attendancePointer, JSON.stringify(p));
    } catch { throw new StorageError(); }
  }
  async function call(path: string, method: "GET" | "PUT" | "POST", body?: unknown) {
    if (!current()) throw Error("Session changed.");
    const response = await channel.send(path, method, body);
    if (!current()) throw Error("Session changed.");
    if (response.status === 401 || response.status === 403) {
      await response.body?.cancel().catch(() => {});
      channel.invalidate();
      throw new RequestError(response.status);
    }
    if (response.status !== 200 && (method === "GET" || response.status !== 201)) {
      await response.body?.cancel().catch(() => {});
      throw new RequestError(response.status);
    }
    if (!response.headers.get("cache-control")?.includes("no-store")) throw Error("Attendance response may be cached.");
    const value = await readPentaJson(response, channel.signal);
    if (!current()) throw Error("Session changed.");
    return value;
  }

  function accept(draft: Draft) {
    const c = draft.change;
    if (expected && (draft.classSessionId !== expected.session.id || draft.batchId !== expected.session.batchId ||
      draft.sessionStartUtc !== expected.session.startUtc || c.studentId !== expected.learner.id ||
      c.status !== expected.status || c.notes !== expected.notes)) throw Error("Attendance differs from the review.");
    if (previous && (previous.payloadHash !== draft.payloadHash || previous.expiresAtUtc !== draft.expiresAtUtc ||
      previous.sessionStartUtc !== draft.sessionStartUtc || previous.batchId !== draft.batchId ||
      previous.change.studentId !== c.studentId || previous.change.status !== c.status || previous.change.notes !== c.notes ||
      previous.receipt && JSON.stringify(previous.receipt) !== JSON.stringify(draft.receipt))) throw Error("Attendance result changed.");
    previous = draft;
    patch({ phase: draft.status === "Prepared" ? "review" : "succeeded", draft, notice: "" });
  }
  async function run(action: () => Promise<void>, recovering = false) {
    if (busy || !current()) return;
    busy = true; patch({ pending: true, notice: "" });
    try { await action(); } catch (error) {
      if (!current()) return;
      if (error instanceof StorageError) patch({ notice: "Nothing was sent because recovery details could not be saved." });
      else if (view.pointer && recovering && error instanceof RequestError && [404, 409, 410].includes(error.code))
        patch({ phase: "closed", draft: null, notice: "This request could not be recovered. Check the class in Manual Attendance before starting again." });
      else patch({ notice: view.pointer ? "The outcome is unconfirmed. Check this same request before starting another." :
        "Attendance could not be verified. Check the name or open Manual Attendance." });
    } finally { busy = false; patch({ pending: false }); }
  }
  async function recover() {
    const p = view.pointer;
    if (!p || !view.ready) return;
    patch({ phase: "unknown", draft: null });
    accept(attendanceDraft(await call(path(p), "GET"), p));
  }
  return {
    initialise: () => run(async () => {
      const p = key();
      if (p) patch({ pointer: p, phase: "unknown" });
      const r = await call("/readiness", "GET");
      if (!obj(r) || r.protocol !== "teacher-attendance.v1" || r.academyId !== channel.academyId || r.requiresConfirmation !== true)
        throw Error("Attendance contract unavailable.");
      patch({ ready: true, phase: "idle" });
      if (p) await recover();
    }, true).then(() => { if (!view.ready) patch({ phase: "unavailable" }); }),
    searchClasses: (q: string) => run(async () => {
      if (!view.ready || view.pointer) return;
      patch({ classes: null, selected: null, roster: null });
      patch({ classes: attendanceClasses(await call("/sessions?q=" + encodeURIComponent(attendanceQuery(q)), "GET"), channel.academyId) });
    }),
    selectClass: (selected: AttendanceClass) => {
      if (!busy && view.classes?.rows.includes(selected) && !view.pointer) patch({ selected, roster: null });
    },
    searchLearners: (q: string) => run(async () => {
      const session = view.selected;
      if (!session || view.pointer) return;
      patch({ roster: null });
      const result = await call("/sessions/" + session.id + "/roster?q=" + encodeURIComponent(attendanceQuery(q)), "GET");
      patch({ roster: attendanceRoster(result, channel.academyId, session.id) });
    }),
    prepare: (learner: AttendanceLearner, status: AttendanceStatus, notes: string) => run(async () => {
      const session = view.selected;
      if (!session || view.pointer || !view.roster?.rows.includes(learner) || !isStatus(status) || !clean(notes, 500)) return;
      const existing = key();
      if (existing) { patch({ pointer: existing, phase: "unknown", notice: "Check the saved attendance request first." }); return; }
      const p = { academyId: channel.academyId, epoch: channel.epoch, sessionId: session.id, requestId: uuid() };
      persist(p);
      const trimmed = notes.trim() || null;
      expected = { session, learner, status, notes: trimmed };
      patch({ pointer: p, phase: "unknown", classes: null, roster: null });
      const draft = attendanceDraft(await call(path(p), "PUT", {
        payload: { tool_key: "AcademyDesk.TeacherAttendance.Mark", tool_revision: 1, fields: {
          StudentId: { kind: "reference", value: learner.id }, Status: { kind: "text", value: status },
          ...(trimmed ? { Notes: { kind: "text", value: trimmed } } : {})
        } }
      }), p);
      if (draft.status !== "Prepared") throw Error("Attendance review required.");
      accept(draft);
    }),
    confirm: (review: Draft) => run(async () => {
      const p = view.pointer;
      if (!p || review !== view.draft || review.status !== "Prepared") return;
      if (Date.parse(review.expiresAtUtc) <= now()) {
        patch({ phase: "unknown", draft: null, notice: "This review has expired. Check the saved request." }); return;
      }
      persist(p); patch({ phase: "unknown", draft: null });
      const draft = attendanceDraft(await call(path(p) + "/confirm", "POST", {
        payloadHash: review.payloadHash, intent: "ConfirmTeacherAttendanceAndQueueInApp"
      }), p);
      if (draft.status !== "Succeeded") throw Error("Verified receipt required.");
      accept(draft);
    }),
    recover: () => run(recover, true),
    clear: () => run(async () => {
      const p = view.pointer;
      if (!p || !["review", "succeeded", "closed"].includes(view.phase)) return;
      try {
        const saved = key();
        if (saved && (saved.requestId !== p.requestId || saved.sessionId !== p.sessionId)) throw Error("Different request saved.");
        storage().removeItem(attendancePointer);
      } catch { throw new StorageError(); }
      previous = null; expected = null;
      patch({ ...initialAttendanceView, ready: true, phase: "idle", pending: true, notice: "" });
    }),
    dispose: () => { disposed = true; previous = null; expected = null; view = initialAttendanceView; },
  };
}
