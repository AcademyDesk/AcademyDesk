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

async function readFeeArrangements(academyId: string, studentId: string): Promise<Item[] | undefined> {
  if (!academyId || !studentId) return;
  const response = await academyApi(`/api/academies/${academyId}/students/${studentId}/fee-arrangements`);
  if (!response.ok) throw new Error();
  const rows: unknown = await response.json();
  if (!Array.isArray(rows)) throw new Error();
  return rows as Item[];
}

export function StudentFeeArrangements({
  academyId,
  studentId,
}: {
  academyId: string;
  studentId: string;
}) {
  const [items, setItems] = useState<Item[]>([]);
  const [subjectName, setSubjectName] = useState("");
  const [amount, setAmount] = useState("");
  const [frequency, setFrequency] = useState("Monthly");
  const [message, setMessage] = useState("");
  const [ready, setReady] = useState(false);
  const [saving, setSaving] = useState(false);
  const [failed, setFailed] = useState(false);
  const identity = `${academyId}/${studentId}`;
  const [source, setSource] = useState({ identity });
  const lifecycle = useRef<{ source: typeof source; active: boolean } | null>(null);
  const pending = useRef<object | null>(null);
  if (source.identity !== identity) {
    setSource({ identity });
    setItems([]); setSubjectName(""); setAmount(""); setFrequency("Monthly");
    setMessage(""); setReady(false); setSaving(false); setFailed(false);
  }
  useEffect(() => {
    const current = { source, active: true };
    lifecycle.current = current;
    void readFeeArrangements(academyId, studentId).then((next) => {
      if (current.active && next) { setItems(next); setReady(true); }
    }).catch(() => {
      if (current.active) { setMessage("Subject fees could not be loaded. Try again."); setFailed(true); }
    });
    return () => { current.active = false; };
  }, [academyId, studentId, source]);
  function retry() {
    setFailed(false); setReady(false); setMessage(""); setSource({ identity });
  }
  async function add(event: FormEvent) {
    event.preventDefault();
    const current = lifecycle.current;
    if (!academyId || !studentId || !ready || !current?.active || current.source !== source || pending.current === current) return;
    pending.current = current;
    setSaving(true); setMessage("");
    try {
      const response = await academyApi(
        `/api/academies/${academyId}/students/${studentId}/fee-arrangements`,
        {
          method: "POST", headers: apiHeaders(true),
          body: JSON.stringify({ subjectName, amount: Number(amount), frequency, effectiveFrom: new Date().toISOString().slice(0, 10) }),
        },
      );
      if (!current.active) return;
      if (!response.ok) { setMessage("Enter a subject and a positive fee."); return; }
      setSubjectName(""); setAmount(""); setMessage("Fee arrangement added.");
      try {
        const next = await readFeeArrangements(academyId, studentId);
        if (current.active && next) setItems(next);
      } catch {
        if (current.active) setMessage("Fee arrangement added. Refresh to load the latest subject fees.");
      }
    } catch {
      if (current.active) setMessage("The fee arrangement could not be saved. Check the record before retrying.");
    } finally {
      if (pending.current === current) pending.current = null;
      if (current.active) setSaving(false);
    }
  }
  return (
    <section className="surface-panel rounded-xl p-5 student-fee-arrangements">
      <header className="flex items-center justify-between">
        <h3 className="font-semibold">Subject fees</h3>
        <span className="text-sm text-slate-400">{items.length} active</span>
      </header>
      {message && <p className="mt-3 text-sm text-amber-200">{message}</p>}
      {failed && <button onClick={retry} className="enterprise-action-button mt-3">Retry loading subject fees</button>}
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
            {ready ? "No subject fees configured." : failed ? "Subject fee details are unavailable." : "Loading subject fees…"}
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
          {saving ? "Saving…" : "Add"}
        </button>
      </form>
    </section>
  );
}
