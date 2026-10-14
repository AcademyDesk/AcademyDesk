"use client";

import { useEffect, useRef } from "react";
import { Button } from "@/vendor/penta-shared-ui/primitives";
import styles from "@/components/platform-mobile-navigation.module.css";

type Section = { value: string; label: string; icon: string };

/** Local portal sections, not authorization or persisted workflow state. */
export function ManualSectionNavigation({ sections, value, onSelect, className = "", label }: {
  sections: readonly Section[];
  value: string;
  onSelect: (value: string) => void;
  className?: string;
  label: string;
}) {
  const disclosure = useRef<HTMLDetailsElement>(null);
  const selected = sections.find(section => section.value === value);
  useEffect(() => {
    disclosure.current?.removeAttribute("open");
  }, [value]);
  useEffect(() => {
    const outside = (event: PointerEvent) => {
      if (event.target instanceof Node && !disclosure.current?.contains(event.target)) disclosure.current?.removeAttribute("open");
    };
    const escape = (event: KeyboardEvent) => {
      if (event.key === "Escape" && disclosure.current?.open) {
        disclosure.current.removeAttribute("open");
        disclosure.current.querySelector("summary")?.focus();
      }
    };
    document.addEventListener("pointerdown", outside);
    document.addEventListener("keydown", escape);
    return () => { document.removeEventListener("pointerdown", outside); document.removeEventListener("keydown", escape); };
  }, []);
  return <nav className={`${className} ${styles.navigation}`} aria-label={label}>
    <details ref={disclosure} className={styles.disclosure}>
      <summary className={styles.trigger}><span className={styles.caption}>Workspace</span><span className={styles.selected}>{selected?.label || "Choose section"}</span><span className={styles.chevron} aria-hidden="true">⌄</span></summary>
      <div className={styles.destinations}>{sections.map(section => <Button type="button" key={section.value} aria-current={section.value === value ? "page" : undefined} data-active={section.value === value} onClick={() => { disclosure.current?.removeAttribute("open"); onSelect(section.value); }}><i aria-hidden="true">{section.icon}</i><span>{section.label}</span></Button>)}</div>
    </details>
  </nav>;
}
