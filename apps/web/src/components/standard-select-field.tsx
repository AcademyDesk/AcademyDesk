"use client";

import { useEffect, useRef, useState } from "react";

type Option = { value: string; label: string };
type StandardSelectFieldProps = {
  name: string;
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
  options: Option[];
};

export function StandardSelectField({ name, value, onChange, placeholder, options }: StandardSelectFieldProps) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const close = (event: MouseEvent) => { if (ref.current && !ref.current.contains(event.target as Node)) setOpen(false); };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);
  const current = options.find(option => option.value === value);
  return <div className="standard-select-field" ref={ref}>
    <input type="hidden" name={name} value={value} />
    <button type="button" className="standard-select-trigger" aria-expanded={open} aria-haspopup="listbox" onClick={() => setOpen(currentOpen => !currentOpen)}>
      <span data-empty={!current}>{current?.label ?? placeholder}</span><i aria-hidden="true">⌄</i>
    </button>
    {open && <div className="standard-select-menu" role="listbox" aria-label={placeholder}>
      <button type="button" role="option" aria-selected={!value} onClick={() => { onChange(""); setOpen(false); }}>{placeholder}</button>
      {options.map(option => <button key={option.value} type="button" role="option" aria-selected={value === option.value} onClick={() => { onChange(option.value); setOpen(false); }}>{option.label}</button>)}
    </div>}
  </div>;
}
