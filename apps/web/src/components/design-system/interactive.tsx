"use client";

import { useEffect, useId, useRef, type ReactNode } from "react";
import styles from "./detail-modal.module.css";

/**
 * A clickable summary tile. Use it only when clicking reveals operational
 * details in StandardDetailModal; static KPIs should remain StandardKpi.
 */
export function StandardInteractiveTile({ label, value, detail, onClick, className = "" }: { label: string; value: string | number; detail: string; onClick: () => void; className?: string }) {
  return <button type="button" onClick={onClick} className={`standard-interactive-tile ${className}`.trim()}><span>{label}</span><strong>{value}</strong><small>{detail}</small></button>;
}

/** Shared overlay for details opened from interactive tiles. */
export function StandardDetailModal({ title, eyebrow = "Details", onClose, children }: { title: string; eyebrow?: string; onClose: () => void; children: ReactNode }) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);
  const titleId = useId();

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    // The native top layer avoids ancestor clipping and makes the background
    // inert. Explicit focus restoration also covers React removing the dialog
    // before the passive-effect cleanup runs.
    const previousOverflow = document.body.style.overflow;
    const previousFocus = document.activeElement;
    dialog.showModal();
    closeRef.current?.focus({ preventScroll: true });
    document.body.style.overflow = "hidden";
    return () => {
      dialog.close();
      document.body.style.overflow = previousOverflow;
      if (previousFocus instanceof HTMLElement && previousFocus.isConnected) {
        previousFocus.focus({ preventScroll: true });
      }
    };
  }, []);

  return (
    <dialog
      ref={dialogRef}
      className={`standard-detail-modal ${styles.overlay}`}
      aria-labelledby={titleId}
      onCancel={(event) => { event.preventDefault(); onClose(); }}
      onClick={(event) => { if (event.target === event.currentTarget) onClose(); }}
    >
      <article className={styles.panel}>
        <header className={styles.header}>
          <div><p>{eyebrow}</p><h2 id={titleId}>{title}</h2></div>
          <button ref={closeRef} className={styles.close} type="button" onClick={onClose} aria-label="Close details">×</button>
        </header>
        {children}
      </article>
    </dialog>
  );
}
