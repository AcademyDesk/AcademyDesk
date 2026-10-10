"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardDateField } from "@/components/design-system/controls";

export function AdmissionFeeEditor({ academyId, studentId }: { academyId: string; studentId: string }) {
  return <AdmissionFeeForm key={JSON.stringify([academyId, studentId])} academyId={academyId} studentId={studentId} />;
}

function AdmissionFeeForm({ academyId, studentId }: { academyId: string; studentId: string }) {
  const [amount, setAmount] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [message, setMessage] = useState("Loading admission fee…");
  const [ready, setReady] = useState(false);
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  const mounted = useRef(false);
  useEffect(() => {
    mounted.current = true;
    let active = true;
    void (async () => {
      try {
        if (!academyId || !studentId) throw new Error();
        const response = await academyApi(`/api/academies/${academyId}/students/${studentId}/fee-arrangements/admission-fee`, { cache: "no-store" });
        if (!response.ok) throw new Error();
        const data: unknown = await response.json();
        if (!data || typeof data !== "object" || Array.isArray(data)) throw new Error();
        const details = data as { amount?: unknown; dueDate?: unknown };
        if (details.amount != null && (typeof details.amount !== "number" || !Number.isFinite(details.amount))) throw new Error();
        if (details.dueDate != null && typeof details.dueDate !== "string") throw new Error();
        if (!active) return;
        setAmount(details.amount?.toString() ?? "");
        setDueDate(typeof details.dueDate === "string" ? details.dueDate : "");
        setReady(true);
        setMessage("");
      } catch {
        if (active) setMessage("Admission fee could not be loaded. Refresh the page before saving.");
      }
    })();
    return () => { active = false; mounted.current = false; };
  }, [academyId, studentId]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!ready || !mounted.current || pending.current) return;
    pending.current = true;
    setSaving(true);
    setMessage("");
    const uncertain = "The admission fee change could not be confirmed. Your draft has been retained. Check the student's admission fee before retrying.";
    try {
      const response = await academyApi(`/api/academies/${academyId}/students/${studentId}/fee-arrangements/admission-fee`, {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({ amount: amount ? Number(amount) : null, dueDate: dueDate || null }),
      });
      if (!mounted.current) return;
      setMessage(response.ok ? "Admission fee saved." : response.status >= 500 ? uncertain : "Admission fee could not be saved. Check your access and enter a valid fee and due date. Your draft has been retained.");
    } catch {
      if (mounted.current) setMessage(uncertain);
    } finally {
      pending.current = false;
      if (mounted.current) setSaving(false);
    }
  }

  return (
    <form onSubmit={save} className="student-fees-panel admission-fee-editor">
      <header className="student-fees-panel-header">
        <div><p>One-time fee</p><h3>Admission fee</h3></div>
        <button disabled={!ready || saving} className="enterprise-action-button">
          {saving ? "Saving…" : "Save admission fee"}
        </button>
      </header>
      {message && <p role="status" aria-live="polite" className="enterprise-page-state student-fees-message">{message}</p>}
      <fieldset disabled={!ready || saving} className="mt-4 grid min-w-0 gap-3 border-0 p-0 sm:grid-cols-2">
        <label className="field-label">Admission fee amount<input disabled={!ready || saving} value={amount} onChange={(event) => setAmount(event.target.value)} type="number" min="0" step="0.01" placeholder="Optional amount" /></label>
        <StandardDateField key={saving ? "saving" : "editable"} name="admissionDueDateDisplay" label="Due date" value={dueDate} onChange={(value) => { if (ready && !pending.current) setDueDate(value); }} />
      </fieldset>
    </form>
  );
}
