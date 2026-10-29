"use client";

import { useCallback, useEffect, useId, useRef, useState } from "react";
import { AnchoredPopup } from "./design-system/anchored-popup";
import styles from "./design-system/anchored-popup.module.css";
import { RequiredControlValue } from "./design-system/required-control-value";

type Option = { value: string; label: string };
type StandardSelectFieldProps = {
  name: string; value: string; onChange: (value: string) => void;
  placeholder: string; options: Option[]; disabled?: boolean; label?: string; required?: boolean;
};

export function StandardSelectField({ name, value, onChange, placeholder, options, disabled, label, required = false }: StandardSelectFieldProps) {
  const labelId = useId();
  const [open, setOpen] = useState(false);
  const fieldRef = useRef<HTMLDivElement>(null);
  const search = useRef({ text: "", at: 0 });
  const close = useCallback(() => setOpen(false), []);
  useEffect(() => {
    if (!open || disabled) return;
    search.current = { text: "", at: 0 };
    const field = fieldRef.current;
    const option = field?.querySelector<HTMLButtonElement>('[role="option"][aria-selected="true"]')
      ?? field?.querySelector<HTMLButtonElement>('[role="option"]');
    option?.focus({ preventScroll: true });
    option?.scrollIntoView({ block: "nearest" });
  }, [open, disabled]);
  return <div className="standard-select-field" ref={fieldRef} style={{ position: "relative" }} onKeyDown={event => {
    if (!open || disabled) return;
    if (event.key === "Tab") {
      // Let native Tab advance from the trigger to the adjacent form control.
      fieldRef.current?.querySelector("button")?.focus({ preventScroll: true });
      close();
      return;
    }
    if (event.key.length !== 1 || event.key === " " || event.ctrlKey || event.metaKey || event.altKey || event.nativeEvent.isComposing) return;
    const items = [...fieldRef.current?.querySelectorAll<HTMLButtonElement>('[role="option"]') ?? []];
    const now = Date.now(), character = event.key.toLocaleLowerCase();
    const prefix = now - search.current.at < 700 ? search.current.text + character : character;
    const repeated = [...prefix].every(letter => letter === character);
    const query = repeated ? character : prefix;
    search.current = { text: query, at: now };
    const current = items.indexOf(document.activeElement as HTMLButtonElement);
    const start = repeated ? current + 1 : Math.max(0, current);
    const match = Array.from({ length: items.length }, (_, offset) => items[(start + offset) % items.length])
      .find(item => item.textContent?.trim().toLocaleLowerCase().startsWith(query));
    if (match) { event.preventDefault(); match.focus(); }
  }}>
    {label && <span id={labelId} className="standard-time-label">{label}</span>}
    <input type="hidden" name={name} value={value} />
    {required && <RequiredControlValue value={value} label={label ?? placeholder} disabled={disabled} />}
    <button type="button" disabled={disabled} className={`standard-select-trigger ${styles.trigger}`} aria-labelledby={label ? `${labelId} ${labelId}-value` : undefined} aria-expanded={open} aria-haspopup="listbox"
      onClick={() => setOpen(current => !current)} onKeyDown={event => {
        if (event.key === "ArrowDown" || event.key === "ArrowUp") { event.preventDefault(); setOpen(true); }
      }}>
      <span id={`${labelId}-value`} data-empty={!value}>{options.find(option => option.value === value)?.label ?? placeholder}</span>
      <i aria-hidden="true">⌄</i>
    </button>
    {open && !disabled && <AnchoredPopup anchorRef={fieldRef} className="standard-select-menu" role="listbox" label={placeholder} onClose={close}>
      {[{ value: "", label: placeholder }, ...options].map(option => <button key={option.value || "__placeholder"} type="button"
        role="option" tabIndex={-1} aria-selected={value === option.value} onClick={() => { onChange(option.value); close(); fieldRef.current?.querySelector("button")?.focus({ preventScroll: true }); }}>
        {option.label}
      </button>)}
    </AnchoredPopup>}
  </div>;
}
