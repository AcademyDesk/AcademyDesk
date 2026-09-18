"use client";

import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
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
  const [items, setItems] = useState<Item[]>([]);
  const [subjectName, setSubjectName] = useState("");
  const [amount, setAmount] = useState("");
  const [frequency, setFrequency] = useState("Monthly");
  const [message, setMessage] = useState("");
  async function load() {
    if (!academyId || !studentId) return;
    const response = await academyApi(
      `/api/academies/${academyId}/students/${studentId}/fee-arrangements`,
    );
    if (response.ok) setItems(await response.json());
  }
  useEffect(() => {
    void load();
  }, [academyId, studentId]);
  async function add(event: FormEvent) {
    event.preventDefault();
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
    if (!response.ok) return setMessage("Enter a subject and a positive fee.");
    setSubjectName("");
    setAmount("");
    setMessage("Fee arrangement added.");
    await load();
  }
  return (
    <section className="surface-panel rounded-xl p-5">
      <header className="flex items-center justify-between">
        <h3 className="font-semibold">Subject fees</h3>
        <span className="text-sm text-slate-400">{items.length} active</span>
      </header>
      {message && <p className="mt-3 text-sm text-amber-200">{message}</p>}
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
            No subject fees configured.
          </li>
        )}
      </ul>
      <form
        onSubmit={add}
        className="mt-4 grid gap-2 sm:grid-cols-[1fr_120px_135px_auto]"
      >
        <input
          value={subjectName}
          onChange={(event) => setSubjectName(event.target.value)}
          placeholder="Subject, e.g. Piano"
          required
        />
        <input
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          type="number"
          min="1"
          placeholder="Amount"
          required
        />
        <select
          value={frequency}
          onChange={(event) => setFrequency(event.target.value)}
        >
          <option>Monthly</option>
          <option>Quarterly</option>
          <option value="HalfYearly">Half-yearly</option>
          <option>Annual</option>
        </select>
        <button className="rounded bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950">
          Add
        </button>
      </form>
    </section>
  );
}
