"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { StandardSelectField } from "@/components/design-system/controls";
type Item = {
  id: string;
  subjectName: string;
  amount: number;
  frequency: string;
  effectiveFrom: string;
  isActive: boolean;
};

export function StudentFeeArrangements({
  academyId,
  studentId,
}: {
  academyId: string;
  studentId: string;
}) {
  return <FeeArrangementEditor key={JSON.stringify([academyId, studentId])} academyId={academyId} studentId={studentId} />;
}

async function fetchArrangements(academyId: string, studentId: string) {
  const response = await academyApi(
    `/api/academies/${academyId}/students/${studentId}/fee-arrangements`,
    { cache: "no-store" },
  );
  if (!response.ok) throw new Error();
  const data: unknown = await response.json();
  if (!Array.isArray(data)) throw new Error();
  return data as Item[];
}

function FeeArrangementEditor({ academyId, studentId }: { academyId: string; studentId: string }) {
  const [items, setItems] = useState<Item[]>([]);
  const [subjectName, setSubjectName] = useState("");
  const [amount, setAmount] = useState("");
  const [frequency, setFrequency] = useState("Monthly");
  const [message, setMessage] = useState("Loading subject fees…");
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
        const data = await fetchArrangements(academyId, studentId);
        if (!active) return;
        setItems(data);
        setReady(true);
        setMessage("");
      } catch {
        if (active) setMessage("Subject fees could not be loaded. Refresh the page before adding a fee.");
      }
    })();
    return () => { active = false; mounted.current = false; };
  }, [academyId, studentId]);
  async function add(event: FormEvent) {
    event.preventDefault();
    if (!ready || !mounted.current || pending.current) return;
    pending.current = true;
    setSaving(true);
    setMessage("");
    const uncertain = "The fee addition could not be confirmed. Your draft has been retained. Check the subject fees before retrying.";
    try {
      const response = await academyApi(
        `/api/academies/${academyId}/students/${studentId}/fee-arrangements`,
        {
          method: "POST",
          headers: apiHeaders(true),
          body: JSON.stringify({
            subjectName,
            amount: Number(amount),
            frequency,
            effectiveFrom: new Date().toISOString().slice(0, 10),
          }),
        },
      );
      if (!mounted.current) return;
      if (!response.ok) {
        setMessage(response.status >= 500 ? uncertain : "Fee arrangement could not be added. Check your access, subject and positive fee. Your draft has been retained.");
        return;
      }
      setSubjectName((current) => current === subjectName ? "" : current);
      setAmount((current) => current === amount ? "" : current);
      setMessage("Fee arrangement added.");
      try {
        const data = await fetchArrangements(academyId, studentId);
        if (mounted.current) setItems(data);
      } catch {
        if (mounted.current) setMessage("Fee arrangement added. Subject fees could not be refreshed; do not repeat the action. Refresh the page to see the latest data.");
      }
    } catch {
      if (mounted.current) setMessage(uncertain);
    } finally {
      pending.current = false;
      if (mounted.current) setSaving(false);
    }
  }
  return (
    <section className="surface-panel rounded-xl p-5 student-fee-arrangements">
      <header className="flex items-center justify-between">
        <h3 className="font-semibold">Subject fees</h3>
        <span className="text-sm text-slate-400">{ready ? `${items.length} active` : "Not loaded"}</span>
      </header>
      {message && <p role="status" aria-live="polite" className="mt-3 text-sm text-amber-200">{message}</p>}
      <ul className="mt-4 divide-y divide-slate-800">
        {items.length ? (
          items.map((item) => (
            <li
              key={item.id}
              className="flex items-center justify-between gap-3 py-3 text-sm"
            >
              <span>
                <b>{item.subjectName}</b>
                <small className="ml-2 text-slate-400">{item.frequency}</small>
              </span>
              <b>₹{item.amount.toLocaleString("en-IN")}</b>
            </li>
          ))
        ) : (
          <li className="py-4 text-sm text-slate-400">
            {ready ? "No subject fees configured." : "Subject fees are not available yet."}
          </li>
        )}
      </ul>
      <form
        onSubmit={add}
        className="student-fee-arrangements-form mt-4 grid gap-2"
      >
        <input
          disabled={!ready || saving}
          value={subjectName}
          onChange={(event) => setSubjectName(event.target.value)}
          placeholder="Subject, e.g. Piano"
          required
        />
        <input
          disabled={!ready || saving}
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          type="number"
          min="1"
          step="0.01"
          placeholder="Amount"
          required
        />
        <div className="student-fee-frequency">
          <StandardSelectField
            disabled={!ready || saving}
            name="fee-frequency"
            value={frequency}
            onChange={setFrequency}
            placeholder="Billing frequency"
            options={[
              { value: "Monthly", label: "Monthly" },
              { value: "Quarterly", label: "Quarterly" },
              { value: "HalfYearly", label: "Half-yearly" },
              { value: "Annual", label: "Annual" },
            ]}
          />
        </div>
        <button disabled={!ready || saving} className="enterprise-action-button student-fee-add-button">
          {saving ? "Adding…" : "Add"}
        </button>
      </form>
    </section>
  );
}
