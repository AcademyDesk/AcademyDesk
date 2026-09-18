"use client";

import { useTheme } from "@/components/theme-provider";

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  return (
    <div className="theme-toggle" role="group" aria-label="Color theme">
      <button
        type="button"
        title="Use light theme"
        aria-label="Use light theme"
        aria-pressed={theme === "light"}
        onClick={() => setTheme("light")}
      >
        <span aria-hidden="true">☼</span> Light
      </button>
      <button
        type="button"
        title="Use dark theme"
        aria-label="Use dark theme"
        aria-pressed={theme === "dark"}
        onClick={() => setTheme("dark")}
      >
        <span aria-hidden="true">◐</span> Dark
      </button>
    </div>
  );
}
