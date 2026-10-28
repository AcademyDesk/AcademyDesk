"use client";

import { useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type RecordState<Data, Draft> = {
  url: string;
  generation: object;
  phase: "loading" | "ready" | "error";
  data?: Data;
  draft?: Draft;
  saving: boolean;
  message: string;
};

const initial = <Data, Draft>(url: string): RecordState<Data, Draft> => ({
  url, generation: {}, phase: "loading", saving: false, message: "",
});
const isRecord = (value: unknown): value is Record<string, unknown> =>
  value !== null && typeof value === "object" && !Array.isArray(value);

// A draft belongs to one successful load, including when A -> B -> A requests overlap.
export function useRecordEditor<Data extends object, Draft extends object>(
  url: string,
  prepare: (data: Data) => Draft,
  loadFailure: string,
) {
  const [record, setRecord] = useState(() => initial<Data, Draft>(url));
  const mounted = useRef(false);
  const pendingSave = useRef<object | null>(null);
  if (record.url !== url) setRecord(initial<Data, Draft>(url));

  useEffect(() => {
    mounted.current = true;
    return () => { mounted.current = false; };
  }, []);

  const generation = record.generation;
  useEffect(() => {
    if (!url) return;
    let active = true;
    void (async () => {
      try {
        const response = await academyApi(url, { cache: "no-store" });
        if (!response.ok) throw new Error();
        const data: unknown = await response.json();
        if (!isRecord(data)) throw new Error();
        if (active) setRecord(current => current.generation === generation
          ? { ...current, phase: "ready", data: data as Data, draft: prepare(data as Data), message: "" }
          : current);
      } catch {
        if (active) setRecord(current => current.generation === generation
          ? { ...current, phase: "error", message: loadFailure } : current);
      }
    })();
    return () => { active = false; };
  }, [url, generation, prepare, loadFailure]);

  function update(change: (draft: Draft) => Draft) {
    setRecord(current => current.generation === generation && current.phase === "ready" && !current.saving && current.draft
      ? { ...current, draft: change(current.draft) } : current);
  }

  function reload() {
    if (pendingSave.current === generation) return;
    setRecord(current => current.generation === generation ? initial<Data, Draft>(url) : current);
  }

  function acceptSaved(value: Data) {
    if (!mounted.current) return;
    setRecord(current => {
      if (current.generation !== generation || current.phase !== "ready") return current;
      const data = { ...current.data, ...value } as Data;
      return { ...current, data, draft: prepare(data) };
    });
  }

  async function save(payload: object, success: string, failure: string) {
    if (!url || record.url !== url || record.phase !== "ready" || !mounted.current || pendingSave.current === generation) return;
    pendingSave.current = generation;
    setRecord(current => current.generation === generation ? { ...current, saving: true, message: "" } : current);
    try {
      const response = await academyApi(url, {
        method: "PUT", headers: apiHeaders(true), body: JSON.stringify(payload),
      });
      const value: unknown = await response.json().catch(() => null);
      if (!mounted.current) return;
      setRecord(current => {
        if (current.generation !== generation) return current;
        if (!response.ok) return { ...current, message: isRecord(value) && typeof value.message === "string" ? value.message : failure };
        if (!isRecord(value)) return { ...current, message: `${success} Reload to confirm the latest details.` };
        const data = { ...current.data, ...value } as Data;
        return { ...current, data, draft: prepare(data), message: success };
      });
    } catch {
      if (mounted.current) setRecord(current => current.generation === generation
        ? { ...current, message: failure } : current);
    } finally {
      if (pendingSave.current === generation) pendingSave.current = null;
      if (mounted.current) setRecord(current => current.generation === generation
        ? { ...current, saving: false } : current);
    }
  }

  return {
    data: record.data, draft: record.draft, saving: record.saving,
    loading: Boolean(url) && record.phase === "loading", failed: record.phase === "error",
    ready: Boolean(url) && record.phase === "ready", message: record.message,
    update, reload, save, acceptSaved,
  };
}
