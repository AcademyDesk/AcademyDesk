"use client";

import { useId, useLayoutEffect, useRef, type ReactNode, type RefObject } from "react";
import styles from "./anchored-popup.module.css";

/** Native top layer without a body portal: keeps the host's theme and scope. */
export function AnchoredPopup({ anchorRef, className, label, role = "dialog", width, onClose, children }: {
  anchorRef: RefObject<HTMLElement | null>;
  className: string;
  label: string;
  role?: "dialog" | "listbox";
  width?: number;
  onClose: () => void;
  children: ReactNode;
}) {
  const panel = useRef<HTMLElement>(null);
  const id = useId();
  useLayoutEffect(() => {
    const element = panel.current, trigger = anchorRef.current?.querySelector("button");
    if (!element || !trigger) return;
    const place = () => {
      const rect = trigger.getBoundingClientRect();
      const viewport = window.visualViewport;
      const left = viewport?.offsetLeft ?? 0, top = viewport?.offsetTop ?? 0;
      const viewWidth = viewport?.width ?? innerWidth, viewHeight = viewport?.height ?? innerHeight;
      if (rect.bottom < top || rect.top > top + viewHeight || rect.right < left || rect.left > left + viewWidth) { onClose(); return; }
      const gap = 8, edge = 12;
      const below = top + viewHeight - rect.bottom - gap - edge, above = rect.top - top - gap - edge;
      const up = below < 240 && above > below;
      const available = Math.max(0, up ? above : below);
      const popupWidth = Math.min(width ?? rect.width, viewWidth - edge * 2);
      Object.assign(element.style, { position: "fixed", inset: "auto", margin: "0", width: `${popupWidth}px`,
        left: `${Math.max(left + edge, Math.min(rect.left, left + viewWidth - edge - popupWidth))}px`,
        maxHeight: `${Math.min(role === "listbox" ? 420 : 520, available)}px`, visibility: "hidden" });
      const height = element.getBoundingClientRect().height;
      element.style.top = `${Math.max(top + edge, Math.min(up ? rect.top - gap - height : rect.bottom + gap, top + viewHeight - edge - height))}px`;
      element.style.visibility = "visible";
    };
    // Prevent Escape also dismissing an enclosing native modal.
    const escape = (event: KeyboardEvent) => { if (event.key === "Escape" && !event.defaultPrevented) { event.preventDefault(); onClose(); trigger.focus({ preventScroll: true }); } };
    const outside = (event: PointerEvent) => { if (event.target instanceof Node && !anchorRef.current?.contains(event.target) && !element.contains(event.target)) onClose(); };
    element.showPopover(); place();
    const resize = new ResizeObserver(place);
    resize.observe(trigger); resize.observe(element);
    trigger.setAttribute("aria-controls", element.id);
    window.addEventListener("scroll", place, true); window.addEventListener("resize", place);
    window.visualViewport?.addEventListener("resize", place); window.visualViewport?.addEventListener("scroll", place);
    document.addEventListener("keydown", escape); document.addEventListener("pointerdown", outside);
    return () => { resize.disconnect(); if (element.matches(":popover-open")) element.hidePopover(); trigger.removeAttribute("aria-controls");
      window.removeEventListener("scroll", place, true); window.removeEventListener("resize", place);
      window.visualViewport?.removeEventListener("resize", place); window.visualViewport?.removeEventListener("scroll", place);
      document.removeEventListener("keydown", escape); document.removeEventListener("pointerdown", outside); };
  }, [anchorRef, width, role, onClose]);
  return <section ref={panel} id={id} popover="manual" role={role} aria-label={label} className={`${className} ${styles.popup}`} onKeyDown={event => {
    if (role !== "listbox" || !["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) return;
    const items = [...event.currentTarget.querySelectorAll<HTMLButtonElement>('button[role="option"]:not(:disabled)')];
    const current = items.indexOf(document.activeElement as HTMLButtonElement);
    const next = event.key === "Home" ? 0 : event.key === "End" ? items.length - 1 : current < 0 ? (event.key === "ArrowDown" ? 0 : items.length - 1) : (current + (event.key === "ArrowDown" ? 1 : -1) + items.length) % items.length;
    event.preventDefault(); items[next]?.focus();
  }}>{children}</section>;
}
