"use client";

import { useEffect, useMemo, useRef, useState } from "react";

type StandardDateFieldProps = {
  name: string;
  value: string;
  onChange: (value: string) => void;
  label: string;
  required?: boolean;
};

const pad = (value: number) => String(value).padStart(2, "0");
const isoDate = (date: Date) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
const readableDate = (value: string) => value ? new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(`${value}T12:00:00`)) : "Select date";

export function StandardDateField({ name, value, onChange, label, required }: StandardDateFieldProps) {
  const selected = value ? new Date(`${value}T12:00:00`) : undefined;
  const [month, setMonth] = useState(() => selected ?? new Date());
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLLabelElement>(null);
  useEffect(() => { if (selected) setMonth(selected); }, [value]);
  useEffect(() => {
    const close = (event: MouseEvent) => { if (ref.current && !ref.current.contains(event.target as Node)) setOpen(false); };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);
  const days = useMemo(() => {
    const first = new Date(month.getFullYear(), month.getMonth(), 1);
    const offset = (first.getDay() + 6) % 7;
    const start = new Date(month.getFullYear(), month.getMonth(), 1 - offset);
    return Array.from({ length: 42 }, (_, index) => { const day = new Date(start); day.setDate(start.getDate() + index); return day; });
  }, [month]);
  const title = new Intl.DateTimeFormat("en-IN", { month: "long", year: "numeric" }).format(month);
  return <label className="standard-date-field" ref={ref}>
    <span>{label}</span>
    <input type="hidden" name={name} value={value} />
    <button type="button" className="standard-date-trigger" aria-expanded={open} aria-haspopup="dialog" onClick={() => setOpen(current => !current)}>
      <span data-empty={!value}>{readableDate(value)}</span><i aria-hidden="true">▣</i>
    </button>
    {open && <section className="standard-date-popover" role="dialog" aria-label={`${label} calendar`}>
      <header><button type="button" aria-label="Previous month" onClick={() => setMonth(new Date(month.getFullYear(), month.getMonth() - 1, 1))}>‹</button><strong>{title}</strong><button type="button" aria-label="Next month" onClick={() => setMonth(new Date(month.getFullYear(), month.getMonth() + 1, 1))}>›</button></header>
      <div className="standard-date-weekdays">{["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map(day => <span key={day}>{day}</span>)}</div>
      <div className="standard-date-days">{days.map(day => { const isCurrentMonth = day.getMonth() === month.getMonth(); const isSelected = value === isoDate(day); const isToday = isoDate(day) === isoDate(new Date()); return <button key={day.toISOString()} type="button" data-current={isCurrentMonth} data-selected={isSelected} data-today={isToday} onClick={() => { onChange(isoDate(day)); setOpen(false); }}>{day.getDate()}</button>; })}</div>
      <footer><button type="button" onClick={() => { onChange(""); setOpen(false); }} disabled={required}>Clear</button><button type="button" onClick={() => { const today = new Date(); onChange(isoDate(today)); setMonth(today); setOpen(false); }}>Today</button></footer>
    </section>}
  </label>;
}
