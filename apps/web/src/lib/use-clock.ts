"use client";

import { useEffect, useState } from "react";

/** Update time-based views even when their fetched data and filters do not change. */
export function useClock(intervalMs = 60_000): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), intervalMs);
    return () => window.clearInterval(timer);
  }, [intervalMs]);
  return now;
}
