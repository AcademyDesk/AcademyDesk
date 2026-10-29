"use client";

import { useSyncExternalStore } from "react";
import { useTheme } from "@/components/theme-provider";

const subscribe = () => () => {};
const clientSnapshot = () => true;
const serverSnapshot = () => false;

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  // Same hydration boundary as the PENTA Manual theme, without a mount-effect
  // state update. Server and first hydration attributes remain identical.
  const hydrated = useSyncExternalStore(subscribe, clientSnapshot, serverSnapshot);

  return (
    <div className="theme-toggle" role="group" aria-label="Color theme">
      <button
        type="button"
        title="Use light theme"
        aria-label="Use light theme"
        aria-pressed={hydrated ? theme === "light" : undefined}
        onClick={() => setTheme("light")}
      >
        <span aria-hidden="true">☼</span> Light
      </button>
      <button
        type="button"
        title="Use dark theme"
        aria-label="Use dark theme"
        aria-pressed={hydrated ? theme === "dark" : undefined}
        onClick={() => setTheme("dark")}
      >
        <span aria-hidden="true">◐</span> Dark
      </button>
    </div>
  );
}
