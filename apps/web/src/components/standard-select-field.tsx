"use client";

import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import type { CSSProperties } from "react";

type Option = { value: string; label: string };
type StandardSelectFieldProps = {
  name: string;
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
  options: Option[];
  disabled?: boolean;
};

export function StandardSelectField({
  name,
  value,
  onChange,
  placeholder,
  options,
  disabled,
}: StandardSelectFieldProps) {
  const [open, setOpen] = useState(false);
  const [menuStyle, setMenuStyle] = useState<CSSProperties>();
  const fieldRef = useRef<HTMLDivElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const updatePosition = () => {
      const trigger = fieldRef.current?.querySelector(".standard-select-trigger");
      if (!(trigger instanceof HTMLElement)) return;
      const rect = trigger.getBoundingClientRect();
      // Keep the menu attached to the trigger. It scrolls internally rather
      // than expanding through the page or jumping to the viewport top.
      const availableBelow = window.innerHeight - rect.bottom - 16;
      const availableAbove = rect.top - 16;
      const openAbove = availableBelow < 220 && availableAbove > availableBelow;
      const maxHeight = Math.min(280, Math.max(150, openAbove ? availableAbove : availableBelow));
      setMenuStyle({
        position: "fixed",
        left: Math.round(rect.left),
        top: Math.round(openAbove ? rect.top - maxHeight - 6 : rect.bottom + 6),
        width: Math.round(rect.width),
        maxHeight,
        overflowY: "auto",
        boxSizing: "border-box",
        zIndex: 2147483000,
      });
    };
    updatePosition();
    window.addEventListener("resize", updatePosition);
    window.addEventListener("scroll", updatePosition, true);
    return () => {
      window.removeEventListener("resize", updatePosition);
      window.removeEventListener("scroll", updatePosition, true);
    };
  }, [open]);

  useEffect(() => {
    const close = (event: MouseEvent) => {
      const target = event.target as Node;
      if (!fieldRef.current?.contains(target) && !menuRef.current?.contains(target)) setOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  return (
    <div className="standard-select-field" ref={fieldRef}>
      <input type="hidden" name={name} value={value} />
      <button
        type="button"
        disabled={disabled}
        className="standard-select-trigger"
        aria-expanded={open}
        aria-haspopup="listbox"
        onClick={() => setOpen((current) => !current)}
      >
        <span data-empty={!value}>{options.find((option) => option.value === value)?.label ?? placeholder}</span>
        <i aria-hidden="true">⌄</i>
      </button>
      {open && menuStyle && typeof document !== "undefined" && createPortal(
        <div ref={menuRef} className="standard-select-menu" role="listbox" style={menuStyle}>
          {[{ value: "", label: placeholder }, ...options].map((option) => (
            <button
              key={option.value || "__placeholder"}
              type="button"
              role="option"
              aria-selected={value === option.value}
              style={{ minHeight: 36, height: 36, maxHeight: 36, padding: "8px 10px", lineHeight: 1.2 }}
              onClick={() => { onChange(option.value); setOpen(false); }}
            >
              {option.label}
            </button>
          ))}
        </div>,
        document.body,
      )}
    </div>
  );
}
