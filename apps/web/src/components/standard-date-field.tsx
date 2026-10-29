"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AnchoredPopup } from "./design-system/anchored-popup";

type StandardDateFieldProps = {
  name: string;
  value: string;
  onChange: (value: string) => void;
  label: string;
  required?: boolean;
  disabled?: boolean;
};

const pad = (value: number) => String(value).padStart(2, "0");
const isoDate = (date: Date) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
const monthNames = Array.from({ length: 12 }, (_, index) => new Intl.DateTimeFormat("en-IN", { month: "long" }).format(new Date(2020, index, 1)));
const currentYear = new Date().getFullYear();
const yearOptions = Array.from({ length: currentYear + 25 - 1900 + 1 }, (_, index) => currentYear + 25 - index);
const readableDate = (value: string) => value ? new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(`${value}T12:00:00`)) : "Select date";

export function StandardDateField({ name, value, onChange, label, required, disabled = false }: StandardDateFieldProps) {
  const selected = value ? new Date(`${value}T12:00:00`) : undefined;
  const [view, setView] = useState(() => ({ sourceValue: value, month: selected ?? new Date(2000, 0, 1) }));
  // A changed controlled value wins immediately; user navigation is local to
  // that source value, not copied back into state from a render-syncing effect.
  const month = view.sourceValue === value ? view.month : selected ?? view.month;
  const setMonth = (next: Date) => setView({ sourceValue: value, month: next });
  const [todayKey, setTodayKey] = useState("");
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLLabelElement>(null);
  const close = useCallback(() => setOpen(false), []);
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
  const setCalendarMonth = (nextMonth: number) => setMonth(new Date(month.getFullYear(), nextMonth, 1));
  const setCalendarYear = (nextYear: number) => setMonth(new Date(nextYear, month.getMonth(), 1));
  return <label className="standard-date-field" ref={ref}>
    <span>{label}</span>
    <input type="hidden" name={name} value={value} />
    <button type="button" disabled={disabled} className="standard-date-trigger" aria-expanded={open && !disabled} aria-haspopup="dialog" onClick={() => { if (!open) { setTodayKey(isoDate(new Date())); if (!value && month.getFullYear() === 2000) setMonth(new Date()); } setOpen(current => !current); }}>
      <span data-empty={!value}>{readableDate(value)}</span><i aria-hidden="true">▣</i>
    </button>
    {open && !disabled && <AnchoredPopup anchorRef={ref} className="standard-date-popover" label={`${label} calendar`} width={360} onClose={close}>
      <header><button type="button" aria-label="Previous month" onClick={() => setMonth(new Date(month.getFullYear(), month.getMonth() - 1, 1))}>‹</button><div className="standard-date-selectors"><select aria-label="Select month" value={month.getMonth()} onChange={event => setCalendarMonth(Number(event.target.value))}>{monthNames.map((monthName, index) => <option key={monthName} value={index}>{monthName}</option>)}</select><select aria-label="Select year" value={month.getFullYear()} onChange={event => setCalendarYear(Number(event.target.value))}>{yearOptions.map(year => <option key={year} value={year}>{year}</option>)}</select></div><button type="button" aria-label="Next month" onClick={() => setMonth(new Date(month.getFullYear(), month.getMonth() + 1, 1))}>›</button></header>
      <div className="standard-date-weekdays">{["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map(day => <span key={day}>{day}</span>)}</div>
      <div className="standard-date-days">{days.map(day => { const isCurrentMonth = day.getMonth() === month.getMonth(); const isSelected = value === isoDate(day); const isToday = Boolean(todayKey) && isoDate(day) === todayKey; return <button key={day.toISOString()} type="button" data-current={isCurrentMonth} data-selected={isSelected} data-today={isToday} onClick={() => { onChange(isoDate(day)); setOpen(false); }}>{day.getDate()}</button>; })}</div>
      <footer><button type="button" onClick={() => { onChange(""); setOpen(false); }} disabled={required}>Clear</button><button type="button" onClick={() => { const today = new Date(); onChange(isoDate(today)); setMonth(today); setOpen(false); }}>Today</button></footer>
    </AnchoredPopup>}
  </label>;
}
