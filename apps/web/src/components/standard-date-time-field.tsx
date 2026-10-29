"use client";

import { StandardDateField } from "./standard-date-field";
import { StandardTimeField } from "./standard-time-field";
import { RequiredControlValue } from "./design-system/required-control-value";

/** Local wall-clock string only; the caller retains its existing timezone conversion. */
export function StandardDateTimeField({ name, value, onChange, label, disabled }: { name: string; value: string; onChange: (value: string) => void; label: string; disabled?: boolean }) {
  const [date = "", time = ""] = value.split("T");
  const complete = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value);
  const change = (nextDate: string, nextTime: string) => onChange(nextDate || nextTime ? `${nextDate}T${nextTime}` : "");
  return <div className="standard-date-time-field" role="group" aria-label={label} style={{ position: "relative", display: "grid", gap: 12, minWidth: 0 }}>
    <RequiredControlValue value={complete ? value : ""} label={label} disabled={disabled} />
    <StandardDateField name={`${name}-date`} label={`${label} date`} value={date} onChange={next => change(next, time)} required disabled={disabled} />
    <StandardTimeField name={`${name}-time`} label={`${label} time`} value={time} onChange={next => change(date, next)} intervalMinutes={1} disabled={disabled} />
  </div>;
}
