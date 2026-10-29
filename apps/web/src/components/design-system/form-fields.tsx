"use client";

import { useState } from "react";
import { StandardDateField, StandardSelectField } from "./controls";

/** Uncontrolled-form adapters: preserve native FormData names and initial values. */
export function StandardFormSelect({ name, label, defaultValue, options, disabled }: {
  name: string; label: string; defaultValue: string; options: string[]; disabled?: boolean;
}) {
  const [value, setValue] = useState(defaultValue);
  return <StandardSelectField name={name} label={label} value={value} onChange={next => { if (next) setValue(next); }} placeholder={`Choose ${label.toLowerCase()}`} options={options.map(option => ({ value: option, label: option }))} disabled={disabled} />;
}

export function StandardFormDate({ name, label, defaultValue = "", disabled }: {
  name: string; label: string; defaultValue?: string; disabled?: boolean;
}) {
  const [value, setValue] = useState(defaultValue);
  return <StandardDateField name={name} label={label} value={value} onChange={setValue} disabled={disabled} />;
}
