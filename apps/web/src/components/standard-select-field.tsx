"use client";

import { useCallback, useRef, useState } from "react";
import { AnchoredPopup } from "./design-system/anchored-popup";
import styles from "./design-system/anchored-popup.module.css";

type Option = { value: string; label: string };
type StandardSelectFieldProps = {
  name: string; value: string; onChange: (value: string) => void;
  placeholder: string; options: Option[]; disabled?: boolean;
};

export function StandardSelectField({ name, value, onChange, placeholder, options, disabled }: StandardSelectFieldProps) {
  const [open, setOpen] = useState(false);
  const fieldRef = useRef<HTMLDivElement>(null);
  const close = useCallback(() => setOpen(false), []);
  return <div className="standard-select-field" ref={fieldRef}>
    <input type="hidden" name={name} value={value} />
    <button type="button" disabled={disabled} className={`standard-select-trigger ${styles.trigger}`} aria-expanded={open} aria-haspopup="listbox"
      onClick={() => setOpen(current => !current)} onKeyDown={event => {
        if (event.key === "ArrowDown" || event.key === "ArrowUp") { event.preventDefault(); setOpen(true); }
      }}>
      <span data-empty={!value}>{options.find(option => option.value === value)?.label ?? placeholder}</span>
      <i aria-hidden="true">⌄</i>
    </button>
    {open && !disabled && <AnchoredPopup anchorRef={fieldRef} className="standard-select-menu" role="listbox" label={placeholder} onClose={close}>
      {[{ value: "", label: placeholder }, ...options].map(option => <button key={option.value || "__placeholder"} type="button"
        role="option" aria-selected={value === option.value} onClick={() => { onChange(option.value); close(); fieldRef.current?.querySelector("button")?.focus({ preventScroll: true }); }}>
        {option.label}
      </button>)}
    </AnchoredPopup>}
  </div>;
}
