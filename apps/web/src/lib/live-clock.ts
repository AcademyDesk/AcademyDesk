"use client";

import { useSyncExternalStore } from "react";

// A cached external snapshot, not Date.now() evaluated by rendering. Server and
// first hydration use zero; subscription samples client time and notifies React.
let snapshot = 0;
const listeners = new Set<() => void>();
let stop: (() => void) | undefined;
const getSnapshot = () => snapshot;
const getServerSnapshot = () => 0;
function subscribe(notify: () => void) {
  listeners.add(notify);
  if (!stop) {
    const update = () => {
      if (document.visibilityState === "hidden") return;
      const next = Date.now();
      if (next !== snapshot) { snapshot = next; for (const listener of [...listeners]) listener(); }
    };
    const timer = window.setInterval(update, 60000);
    window.addEventListener("focus", update);
    document.addEventListener("visibilitychange", update);
    stop = () => {
      window.clearInterval(timer);
      window.removeEventListener("focus", update);
      document.removeEventListener("visibilitychange", update);
    };
    update();
  }
  return () => {
    listeners.delete(notify);
    if (!listeners.size) { stop?.(); stop = undefined; }
  };
}
export function useLiveClock() { return useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot); }
