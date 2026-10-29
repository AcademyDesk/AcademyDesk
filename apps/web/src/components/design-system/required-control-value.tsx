"use client";

import { useState } from "react";
import styles from "./required-control-value.module.css";

/** Participates in native form validation without exposing an OS date/time picker. */
export function RequiredControlValue({ value, label, disabled }: { value: string; label: string; disabled?: boolean }) {
  // Completing a value remounts the validation state, so a later successful
  // form reset cannot resurrect a previous blank-input warning.
  return <ValidationValue key={value ? "complete" : "required"} value={value} label={label} disabled={disabled} />;
}

function ValidationValue({ value, label, disabled }: { value: string; label: string; disabled?: boolean }) {
  const [invalidValue, setInvalidValue] = useState<string>();
  return <>
    <input className="standard-control-validation" aria-label={label} required disabled={disabled} value={value} readOnly={false} onChange={() => {}} tabIndex={-1}
      style={{ position: "absolute", width: 1, height: 1, maxHeight: 1, opacity: 0, padding: 0, border: 0, pointerEvents: "none", clipPath: "inset(50%)" }}
      onInvalid={event => { event.preventDefault(); setInvalidValue(value); const owner = event.currentTarget.parentElement; owner?.querySelector<HTMLButtonElement>("button:not(:disabled)")?.focus(); }} />
    {invalidValue === value && !value && <span role="alert" className={styles.error}>{label} is required.</span>}
  </>;
}
