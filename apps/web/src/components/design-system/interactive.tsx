"use client";

import type { ReactNode } from "react";

/**
 * A clickable summary tile. Use it only when clicking reveals operational
 * details in StandardDetailModal; static KPIs should remain StandardKpi.
 */
export function StandardInteractiveTile({ label, value, detail, onClick, className = "" }: { label: string; value: string | number; detail: string; onClick: () => void; className?: string }) {
  return <button type="button" onClick={onClick} className={`standard-interactive-tile ${className}`.trim()}><span>{label}</span><strong>{value}</strong><small>{detail}</small></button>;
}

/** Shared overlay for details opened from interactive tiles. */
export function StandardDetailModal({ title, eyebrow = "Details", onClose, children }: { title: string; eyebrow?: string; onClose: () => void; children: ReactNode }) {
  return <div className="standard-detail-modal" role="dialog" aria-modal="true" aria-label={title} onMouseDown={onClose}><article onMouseDown={(event) => event.stopPropagation()}><header><div><p>{eyebrow}</p><h2>{title}</h2></div><button type="button" onClick={onClose} aria-label="Close details">×</button></header>{children}</article></div>;
}
