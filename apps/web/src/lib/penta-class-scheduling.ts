import { readPentaJson } from "./penta-onboarding-channel";
import { enrollmentConversation, type EnrollmentKey, type EnrollmentTransport } from "./penta-enrollment-creation";
import { validAdmissionDate } from "./penta-enrollment-assistant";

const createTool = "AcademyDesk.ClassSession.Create";
const moveTool = "AcademyDesk.ClassSession.Reschedule";
const emptyId = "00000000-0000-0000-0000-000000000000";
export const schedulingPointer = "academydesk.penta.class-scheduling-recovery.v1";

export const deliveryModes = ["InPerson", "Online", "Hybrid"] as const;
export type DeliveryMode = typeof deliveryModes[number];
export type ScheduleKind = "create" | "move";
type Batch = { sourceId: string; name: string; teacherId: string | null; branchId: string | null; sourcePath: "/sessions" };
type Session = { sourceId: string; batchId: string; batchName: string; startUtc: string; endUtc: string; status: "Scheduled"; sourcePath: "/sessions" };
export type ScheduleChoice = Batch | Session;
type Lookup = { kind: ScheduleKind; rows: ScheduleChoice[]; hasMore: boolean };
type Plan = { id: string; batchId: string; teacherId: string | null; branchId: string | null; startUtc: string; endUtc: string; deliveryMode: DeliveryMode; roomName: string | null; status: "Scheduled" };
type Receipt = { receiptId: string; auditId: string; classSessionId: string; completedAtUtc: string };
type DraftBase = { requestId: string; revision: 1; contextVersion: 0; expiresAtUtc: string; plan: Plan; source?: Plan; followsBatchTeacher?: boolean; payloadHash: string };
export type ScheduleDraft = DraftBase & (
  { status: "Prepared"; approvalId: null; receipt: null } |
  { status: "Authorized"; approvalId: string; receipt: null } |
  { status: "Succeeded"; approvalId: string; receipt: Receipt });
export type SlotInput = { start: string; end: string; mode: DeliveryMode; location: string };
type Slot = Pick<Plan, "startUtc" | "endUtc" | "deliveryMode" | "roomName">;

const obj = (v: unknown): v is Record<string, unknown> => v !== null && typeof v === "object" && !Array.isArray(v);
const id = (v: unknown): v is string => typeof v === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(v) && v !== emptyId;
const optionalId = (v: unknown) => v === null || id(v);
const cleanText = (v: unknown, max: number): v is string => typeof v === "string" && v.trim().length > 0 && v.length <= max && !/[\u0000-\u001f\u007f-\u009f]/.test(v);
const utc = (v: unknown): v is string => typeof v === "string" && /^\d{4}-\d{2}-\d{2}T(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d{1,7})?Z$/.test(v) && validAdmissionDate(v.slice(0, 10));
const mode = (v: unknown): v is DeliveryMode => deliveryModes.includes(v as DeliveryMode);
export const choiceName = (v: ScheduleChoice) => "batchName" in v ? v.batchName : v.name;

export function schedulingPrompt(text: string): { kind: ScheduleKind; query: string } {
  const match = /^(schedule|reschedule)\s+(?:a\s+)?class\s+for\s+(.+)$/i.exec(text.trim());
  if (!match) throw Error('Try “Schedule a class for Piano” or “Reschedule a class for Piano”.');
  const query = match[2].trim();
  if (!cleanText(query, 80) || query.length < 2) throw Error("Use 2–80 characters for the batch name.");
  return { kind: match[1].toLowerCase() === "schedule" ? "create" : "move", query };
}

export function schedulingLookup(v: unknown, academyId: string, kind: ScheduleKind): Lookup {
  const tool = kind === "create" ? "class-scheduling.batch-options.v1" : "class-session.search.v1";
  if (!obj(v) || v.tool !== tool || v.capability !== "executor" || v.academyId !== academyId || !id(academyId) || !utc(v.asOfUtc) ||
    v.maxRows !== 10 || typeof v.hasMore !== "boolean" || !Array.isArray(v.rows) || v.rows.length > 10) throw Error("Unverified scheduling results.");
  const rows = v.rows.map(row => {
    if (!obj(row) || !id(row.sourceId) || row.sourcePath !== "/sessions") throw Error("Unverified scheduling record.");
    const verified = kind === "create"
      ? Object.keys(row).length === 5 && cleanText(row.name, 500) && optionalId(row.teacherId) && optionalId(row.branchId)
      : Object.keys(row).length === 7 && id(row.batchId) && cleanText(row.batchName, 500) && utc(row.startUtc) &&
        utc(row.endUtc) && Date.parse(row.startUtc) > Date.parse(v.asOfUtc as string) && Date.parse(row.endUtc) > Date.parse(row.startUtc) && row.status === "Scheduled";
    if (!verified) throw Error("Unverified scheduling record.");
    return row as ScheduleChoice;
  });
  if (new Set(rows.map(row => row.sourceId)).size !== rows.length) throw Error("Duplicate scheduling records.");
  return { kind, rows, hasMore: v.hasMore };
}

function validLocation(delivery: DeliveryMode, value: unknown) {
  if (delivery === "InPerson") return value === null || cleanText(value, 120);
  if (!cleanText(value, 120)) return false;
  try { const url = new URL(value); return url.protocol === "https:" && !!url.hostname && !url.username && !url.password; }
  catch { return false; }
}
export function schedulingSlot(input: SlotInput, now: number): Slot {
  const startUtc = input.start + ":00Z", endUtc = input.end + ":00Z";
  if (!utc(startUtc) || !utc(endUtc) || Date.parse(startUtc) <= now || Date.parse(endUtc) <= Date.parse(startUtc))
    throw Error("Choose a future start and a later end, in UTC.");
  const roomName = input.location.trim() || null;
  if (!mode(input.mode) || !validLocation(input.mode, roomName))
    throw Error("Enter a room name or a valid HTTPS meeting link for the selected delivery mode.");
  return { startUtc, endUtc, roomName, deliveryMode: input.mode };
}

function isPlan(v: unknown): v is Plan {
  // Existing Manual sessions can hold legacy location values. They are shown as text.
  return obj(v) && Object.keys(v).length === 9 && (id(v.id) || v.id === emptyId) && id(v.batchId) && optionalId(v.teacherId) &&
    optionalId(v.branchId) && utc(v.startUtc) && utc(v.endUtc) && Date.parse(v.endUtc) > Date.parse(v.startUtc) &&
    mode(v.deliveryMode) && (v.roomName === null || cleanText(v.roomName, 120)) && v.status === "Scheduled";
}
function sameSlot(a: Slot, b: Slot) {
  return Date.parse(a.startUtc) === Date.parse(b.startUtc) && Date.parse(a.endUtc) === Date.parse(b.endUtc) &&
    a.deliveryMode === b.deliveryMode && a.roomName === b.roomName;
}
function samePlan(a?: Plan, b?: Plan) {
  return a && b ? a.id === b.id && a.batchId === b.batchId && a.teacherId === b.teacherId && a.branchId === b.branchId &&
    a.startUtc === b.startUtc && a.endUtc === b.endUtc && sameSlot(a, b) : a === b;
}

export function schedulingDraft(v: unknown, key: EnrollmentKey): ScheduleDraft {
  if (!obj(v) || !id(key.conversationId) || v.requestId !== key.requestId || v.revision !== 1 || v.contextVersion !== 0 ||
    !utc(v.expiresAtUtc) || typeof v.payloadHash !== "string" || !/^[0-9a-f]{64}$/.test(v.payloadHash) ||
    !isPlan(v.plan) || !validLocation(v.plan.deliveryMode, v.plan.roomName)) throw Error("Unverified scheduling review.");
  const p = v.plan, s = v.source;
  if (s === undefined ? p.id !== emptyId || typeof v.followsBatchTeacher !== "boolean" :
    !isPlan(s) || !id(s.id) || s.id !== p.id || s.batchId !== p.batchId || s.teacherId !== p.teacherId || s.branchId !== p.branchId)
    throw Error("Unverified scheduling source.");
  if ((v.status === "Prepared" && v.approvalId === null || v.status === "Authorized" && id(v.approvalId)) && v.receipt === null) return v as ScheduleDraft;
  const r = v.receipt;
  if (v.status !== "Succeeded" || !id(v.approvalId) || !obj(r) || !id(r.receiptId) || !id(r.auditId) ||
    !id(r.classSessionId) || !utc(r.completedAtUtc) || (s !== undefined && r.classSessionId !== p.id)) throw Error("Unverified class receipt.");
  return v as ScheduleDraft;
}

export function readSchedulingPointer(storage: Storage, academyId: string, epoch: string | null): EnrollmentKey | null {
  const raw = storage.getItem(schedulingPointer);
  if (!raw) return null;
  try {
    const v: unknown = JSON.parse(raw);
    if (obj(v) && epoch && v.epoch === epoch && v.academyId === academyId && Object.keys(v).length === 5 &&
      id(v.startRequestId) && id(v.requestId) && (v.conversationId === null || id(v.conversationId)))
      return { startRequestId: v.startRequestId, requestId: v.requestId, conversationId: v.conversationId };
  } catch { /* Invalid pointers carry no authority. */ }
  storage.removeItem(schedulingPointer);
  return null;
}

export type SchedulingState = {
  ready: boolean; pending: boolean; notice: string;
  phase: "loading" | "idle" | "unknown" | "review" | "approved" | "succeeded" | "missing" | "expired" | "unavailable";
  key: EnrollmentKey | null; draft: ScheduleDraft | null; matches: Lookup | null;
};
export const initialSchedulingState: SchedulingState = { ready: false, pending: false, notice: "Checking class scheduling availability…", phase: "loading", key: null, draft: null, matches: null };

class HttpFailure extends Error { constructor(readonly status: number) { super("Class scheduling request could not be verified."); } }
class StorageFailure extends Error {}

export function createClassScheduling(transport: EnrollmentTransport, storage: () => Storage,
  changed: (view: SchedulingState) => void, uuid = () => crypto.randomUUID(), now = Date.now) {
  let state = initialSchedulingState, busy = false, disposed = false, expected: ScheduleDraft | null = null;
  let intent: { selected: ScheduleChoice; slot: Slot } | null = null;
  const current = () => !disposed && !transport.signal.aborted && transport.current();
  function update(patch: Partial<SchedulingState>) { if (current()) { state = { ...state, ...patch }; changed(state); } }
  async function call(path: string, method: "GET" | "PUT" | "POST", body?: unknown, statuses = [200]) {
    if (!current()) throw Error("Session changed.");
    const response = await transport.send(path, method, body);
    if (!current()) throw Error("Session changed.");
    if (response.status === 401 || response.status === 403) {
      update({ ...initialSchedulingState, phase: "unavailable", notice: "Current academy access could not be verified." });
      transport.invalidate();
    }
    if (!statuses.includes(response.status)) {
      await response.body?.cancel().catch(() => {});
      throw new HttpFailure(response.status);
    }
    if (!response.headers.get("cache-control")?.includes("no-store")) throw Error("Class scheduling response may be cached.");
    const data = await readPentaJson(response, transport.signal);
    if (!current()) throw Error("Session changed.");
    return data;
  }
  const draftPath = (key: EnrollmentKey) => "/conversations/" + key.conversationId + "/drafts/" + key.requestId;
  const pointer = () => readSchedulingPointer(storage(), transport.academyId, transport.epoch);
  function persist(key: EnrollmentKey) {
    try {
      const existing = pointer();
      if (!transport.epoch || !id(transport.academyId) || !id(key.startRequestId) || !id(key.requestId) ||
        existing && (existing.startRequestId !== key.startRequestId || existing.requestId !== key.requestId ||
          existing.conversationId !== null && existing.conversationId !== key.conversationId)) throw Error("Conflicting pointer.");
      storage().setItem(schedulingPointer, JSON.stringify({ academyId: transport.academyId, epoch: transport.epoch, ...key }));
    } catch { throw new StorageFailure(); }
  }
  function accept(draft: ScheduleDraft) {
    if (intent) {
      const { selected, slot } = intent;
      const { plan, source } = draft;
      const sourceMatches = "batchName" in selected
        ? source?.id === selected.sourceId && source.batchId === selected.batchId && source.startUtc === selected.startUtc && source.endUtc === selected.endUtc
        : !source && plan.batchId === selected.sourceId && plan.teacherId === selected.teacherId && plan.branchId === selected.branchId;
      if (!sourceMatches || !sameSlot(plan, slot)) throw Error("Scheduling review differs from the request.");
    }
    if (expected && (draft.requestId !== expected.requestId || draft.payloadHash !== expected.payloadHash || draft.expiresAtUtc !== expected.expiresAtUtc ||
      !samePlan(draft.plan, expected.plan) || !samePlan(draft.source, expected.source) || draft.followsBatchTeacher !== expected.followsBatchTeacher ||
      expected.approvalId !== null && draft.approvalId !== expected.approvalId ||
      expected.receipt !== null && JSON.stringify(draft.receipt) !== JSON.stringify(expected.receipt))) throw Error("Saved scheduling result changed.");
    expected = draft;
    update({ draft, phase: draft.status === "Prepared" ? "review" : draft.status === "Authorized" ? "approved" : "succeeded", notice: "" });
  }
  async function run(action: () => Promise<void>) {
    if (busy || !current()) return;
    busy = true; update({ pending: true, notice: "" });
    try { await action(); } catch (err) {
      if (!current()) return;
      if (err instanceof StorageFailure) update({ notice: "The next step was skipped because recovery details could not be saved." });
      else if (err instanceof HttpFailure && err.status === 410 && state.key) update({ phase: "expired", draft: null, notice: "This review expired. Check Manual Sessions before starting again." });
      else if (err instanceof HttpFailure && err.status === 404 && state.key) update({ phase: "missing", draft: null, notice: "A saved review could not be verified. Check Manual Sessions before starting again." });
      else update({ notice: state.key ? "The outcome is unconfirmed. Check this same request before starting another." : "The scheduling request could not be verified. Check the details or open Manual Sessions." });
    } finally { busy = false; update({ pending: false }); }
  }
  async function recover() {
    let key = state.key;
    if (!key || !state.ready) return;
    update({ phase: "unknown", draft: null });
    if (!key.conversationId) {
      key = { ...key, conversationId: enrollmentConversation(await call("/starts/" + key.startRequestId, "GET")) };
      persist(key); update({ key });
    }
    accept(schedulingDraft(await call(draftPath(key), "GET"), key));
  }
  return {
    initialise: () => run(async () => {
      const key = pointer();
      if (key) update({ key });
      const r = await call("/readiness", "GET");
      if (!obj(r) || r.protocol !== "class-scheduling-browser.v1" || r.requiresApproval !== true ||
        !Array.isArray(r.tools) || r.tools.length !== 2 || !r.tools.includes(createTool) || !r.tools.includes(moveTool)) throw Error("Unverified availability.");
      update({ ready: true, phase: "idle" });
      if (key) await recover();
    }).then(() => { if (state.phase === "loading") update({ phase: "unavailable" }); }),
    search: (kind: ScheduleKind, query: string) => run(async () => {
      if (!state.ready || state.key || !cleanText(query, 80) || query.length < 2) return;
      update({ matches: null });
      const data = await call((kind === "create" ? "/batches" : "/sessions") + "?q=" + encodeURIComponent(query), "GET");
      update({ matches: schedulingLookup(data, transport.academyId, kind) });
    }),
    prepare: (selected: ScheduleChoice, input: SlotInput) => run(async () => {
      if (!state.ready || state.key || !state.matches?.rows.includes(selected)) return;
      const slot = schedulingSlot(input, now()), moving = "batchName" in selected;
      const existing = pointer();
      if (existing) { update({ key: existing, phase: "unknown", matches: null, notice: "Check the saved request first." }); return; }
      let key: EnrollmentKey = { startRequestId: uuid(), requestId: uuid(), conversationId: null };
      persist(key);
      intent = { selected, slot };
      update({ key, phase: "unknown", matches: null });
      key = { ...key, conversationId: enrollmentConversation(await call("/conversations/" + key.startRequestId, "PUT", {}, [200, 201])) };
      persist(key); update({ key });
      const value = (v: string) => ({ kind: "text", value: v });
      const fields = {
        [moving ? "SessionId" : "BatchId"]: { kind: "reference", value: selected.sourceId },
        StartUtc: value(slot.startUtc), EndUtc: value(slot.endUtc), DeliveryMode: value(slot.deliveryMode),
        ...(slot.roomName ? { [slot.deliveryMode === "InPerson" ? "RoomName" : "MeetingLink"]: value(slot.roomName) } : {})
      };
      const draft = schedulingDraft(await call(draftPath(key), "PUT", {
        contextVersion: 0, payload: { tool_key: moving ? moveTool : createTool, tool_revision: 1, fields }
      }, [200, 201]), key);
      if (draft.status !== "Prepared") throw Error("A fresh review is required.");
      accept(draft);
    }),
    advance: (review: ScheduleDraft, action: "approve" | "execute") => run(async () => {
      const key = state.key, draft = state.draft;
      if (!state.ready || !key?.conversationId || draft !== review || draft.status !== (action === "approve" ? "Prepared" : "Authorized")) return;
      if (Date.parse(draft.expiresAtUtc) <= now()) throw new HttpFailure(410);
      persist(key);
      update({ phase: "unknown", draft: null });
      const binding = { revision: draft.revision, payloadHash: draft.payloadHash };
      const body = action === "approve" ? { ...binding, intent: draft.source ? "ApproveClassSessionReschedule" : "ApproveClassSessionSchedule" }
        : { ...binding, approvalId: draft.approvalId };
      const next = schedulingDraft(await call(draftPath(key) + "/" + action, "POST", body, [200, 201]), key);
      if (next.status !== (action === "approve" ? "Authorized" : "Succeeded")) throw Error("Unverified class scheduling result.");
      accept(next);
    }),
    recover: () => run(recover),
    close: () => run(async () => {
      if (!state.key || state.phase === "unknown") return;
      try {
        const saved = pointer();
        if (saved && (saved.startRequestId !== state.key.startRequestId || saved.requestId !== state.key.requestId)) throw Error("Different request saved.");
        storage().removeItem(schedulingPointer);
      } catch { throw new StorageFailure(); }
      expected = null; intent = null;
      update({ key: null, draft: null, phase: "idle", matches: null });
    }),
    dispose: () => { disposed = true; expected = null; intent = null; state = initialSchedulingState; },
  };
}
