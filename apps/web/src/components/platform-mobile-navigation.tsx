"use client";

import Link from "next/link";
import { useEffect, useRef } from "react";
import { platformLinks } from "@/components/platform-navigation";
import styles from "@/components/platform-mobile-navigation.module.css";

/** Navigation presentation only: all destination authorization stays in the host. */
export function PlatformMobileNavigation({ currentTab }: { currentTab?: string }) {
  const disclosure = useRef<HTMLDetailsElement>(null);
  const selected = platformLinks.find((item) => "tab" in item ? item.tab === currentTab : !currentTab);

  useEffect(() => {
    const closeOutside = (event: PointerEvent) => {
      if (event.target instanceof Node && !disclosure.current?.contains(event.target)) {
        disclosure.current?.removeAttribute("open");
      }
    };
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape" && disclosure.current?.open) {
        disclosure.current.removeAttribute("open");
        disclosure.current.querySelector("summary")?.focus();
      }
    };
    document.addEventListener("pointerdown", closeOutside);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("pointerdown", closeOutside);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, []);

  return (
    <nav className={`platform-mobile-nav ${styles.navigation}`} aria-label="Platform navigation">
      <details ref={disclosure} className={styles.disclosure}>
        <summary className={styles.trigger}>
          <span className={styles.caption}>Workspace</span>
          <span className={styles.selected}>{selected?.label || "Choose section"}</span>
          <span className={styles.chevron} aria-hidden="true">⌄</span>
        </summary>
        <div className={styles.destinations}>
          {platformLinks.map((item) => (
            <Link
              href={item.href}
              key={item.label}
              aria-current={item === selected ? "page" : undefined}
              onClick={() => disclosure.current?.removeAttribute("open")}
            >
              <i aria-hidden="true">{item.icon}</i>
              <span>{item.label}</span>
            </Link>
          ))}
        </div>
      </details>
    </nav>
  );
}
