import { useCallback, useEffect, useRef, useState } from "react";
import {
  financeConversation, financeDraftJson, financeDraftPath, financeObject, readFinanceRecovery, saveFinanceRecovery,
  type FinanceDraft, type FinanceDraftChannel, type FinanceDraftKey,
} from "@/lib/penta-finance-draft";

type Draft = FinanceDraft<unknown, unknown>;
export type FinanceDraftOperation<T extends Draft> = {
  noun: string; manual: string; storageKey: string; intent: string;
  parse(value: unknown, key: FinanceDraftKey): T;
  same(expected: T, actual: T): boolean;
  conflicts: Readonly<Record<string, string>>;
  unknownOutcome: string;
};
type RequestState<T> = { key: FinanceDraftKey; draft?: T; expected?: T; attempted: boolean; expired?: boolean };

class FinanceHttpError extends Error {
  constructor(readonly status: number, readonly conflict?: string) {
    super("Finance request could not be verified.");
  }
}

export function useFinanceDraft<T extends Draft>(academyId: string, channel: FinanceDraftChannel, operation: FinanceDraftOperation<T>) {
  const [request, setRequest] = useState<RequestState<T> | null>(null);
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState("");
  const busy = useRef(false);
  const { noun, manual, storageKey, intent, parse, same, conflicts, unknownOutcome } = operation;

  const read = useCallback(async (path: string, verb: "GET" | "PUT" | "POST", body?: unknown) => {
    if (!channel.current()) throw new Error("Current browser session changed.");
    const response = await channel.send(path, verb, body);
    if (!channel.current()) throw new Error("Current browser session changed.");
    if (response.status === 409) {
      const problem = await financeDraftJson(response, channel.signal).catch(() => null);
      const message = financeObject(problem) && typeof problem.message === "string" ? problem.message : "";
      throw new FinanceHttpError(409, Object.hasOwn(conflicts, message) ? conflicts[message] : undefined);
    }
    if (response.status !== 200 && response.status !== 201) {
      await response.body?.cancel().catch(() => {});
      throw new FinanceHttpError(response.status);
    }
    const value = await financeDraftJson(response, channel.signal);
    if (!channel.current()) throw new Error("Current browser session changed.");
    return value;
  }, [channel, conflicts]);

  const recover = useCallback(async (key: FinanceDraftKey, expected?: T) => {
    setRequest({ key, expected, attempted: true });
    try {
      const value = parse(await read(financeDraftPath(key), "GET"), key);
      if (expected && (!same(expected, value) || expected.approvalId !== value.approvalId))
        throw new Error("Finance recovery returned a different draft.");
      if (!channel.current()) return;
      setRequest({ key, draft: value, attempted: true });
      if (value.status === "Authorized") setNotice(`No receipt is available yet. Check again or retry this same approved ${noun}.`);
      else if (value.status === "Prepared") setNotice(`Approval could not be verified. Check this draft or open ${manual}.`);
    } catch (error) {
      if (!channel.current()) return;
      if (error instanceof FinanceHttpError && error.status === 410) {
        setRequest({ key, attempted: true, expired: true });
        setNotice(`This ${noun} draft expired. Verify it in ${manual}.`);
      } else setNotice(`PENTA could not verify the receipt. Check this draft or open ${manual}.`);
    }
  }, [channel, read, parse, same, noun, manual]);

  async function run(action: () => Promise<void>) {
    if (busy.current || !channel.current()) return;
    busy.current = true;
    setPending(true);
    setNotice("");
    try { await action(); }
    catch (error) {
      if (!channel.current()) return;
      if (error instanceof FinanceHttpError && (error.status === 401 || error.status === 403)) {
        setRequest(null);
        setNotice(`Current access could not be verified. Open ${manual}.`);
        return;
      }
      const message = error instanceof FinanceHttpError && error.status === 410
        ? `This ${noun} draft expired. Check ${manual} for its current status.`
        : error instanceof FinanceHttpError && error.conflict
          ? error.conflict
          : `PENTA could not verify this step. Check the invoice or open ${manual}.`;
      setNotice(message);
    } finally {
      busy.current = false;
      if (channel.current()) setPending(false);
    }
  }

  useEffect(() => {
    let mounted = true;
    void Promise.resolve().then(() => {
      if (!mounted || !channel.current()) return;
      let key: FinanceDraftKey | null;
      try { key = readFinanceRecovery(window.sessionStorage, storageKey, academyId, channel.epoch); }
      catch { return; }
      if (!key || busy.current) return;
      busy.current = true;
      setPending(true);
      void recover(key).finally(() => {
        busy.current = false;
        if (channel.current()) setPending(false);
      });
    });
    return () => { mounted = false; };
  }, [academyId, channel, recover, storageKey]);

  async function prepare(payload: unknown, matches: (draft: T) => boolean) {
    const conversationId = financeConversation(await read("/" + crypto.randomUUID(), "PUT"));
    const key = { conversationId, requestId: crypto.randomUUID() };
    const draft = parse(await read(financeDraftPath(key), "PUT", payload), key);
    if (draft.status !== "Prepared" || !matches(draft)) throw new Error("Reviewed terms differ from the request.");
    setRequest({ key, draft, attempted: false });
  }

  async function approve() {
    const reviewed = request?.draft;
    if (!request || reviewed?.status !== "Prepared" || request.attempted) return;
    const result = parse(await read(financeDraftPath(request.key) + "/approve", "POST", {
      revision: reviewed.revision, payloadHash: reviewed.payloadHash, intent,
    }), request.key);
    if (result.status !== "Authorized" || !same(reviewed, result)) throw new Error("Approval differs from the review.");
    setRequest({ key: request.key, draft: result, attempted: false });
  }

  async function execute() {
    const approved = request?.draft;
    if (!request || approved?.status !== "Authorized") return;
    const { key } = request;
    try { saveFinanceRecovery(window.sessionStorage, storageKey, academyId, channel.epoch, key); }
    catch {
      setNotice(`Receipt recovery is unavailable in this tab. Open ${manual}.`);
      return;
    }
    setRequest({ key, expected: approved, attempted: true });
    try {
      const result = parse(await read(financeDraftPath(key) + "/execute", "POST", {
        revision: approved.revision, payloadHash: approved.payloadHash, approvalId: approved.approvalId,
      }), key);
      if (result.status !== "Succeeded" || !same(approved, result) || result.approvalId !== approved.approvalId)
        throw new Error("Receipt does not match the approved draft.");
      setRequest({ key, draft: result, attempted: true });
    } catch (error) {
      if (!channel.current()) return;
      if (error instanceof FinanceHttpError && error.conflict) {
        try {
          window.sessionStorage.removeItem(storageKey);
          setRequest(null);
          setNotice(error.conflict);
          return;
        } catch { /* Keep recovery available if the pointer cannot be cleared. */ }
      }
      setNotice(unknownOutcome);
    }
  }

  function close() {
    if (request?.attempted) {
      try { window.sessionStorage.removeItem(storageKey); }
      catch {
        setNotice("This tab could not clear its receipt recovery details.");
        return false;
      }
    }
    setRequest(null);
    setNotice("");
    return true;
  }

  return { request, pending, notice, setNotice, run, prepare, approve, execute, close,
    check: async () => { if (request) await recover(request.key, request.draft ?? request.expected); } };
}
