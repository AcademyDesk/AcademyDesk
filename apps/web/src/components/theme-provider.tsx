"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";

export type ThemeName = "light" | "dark";

type ThemeContextValue = {
  theme: ThemeName;
  setTheme: (theme: ThemeName) => void;
};

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);
const storageKey = "academydesk.theme";

function initialTheme(): ThemeName {
  if (typeof document === "undefined") return "dark";
  return document.documentElement.dataset.theme === "light" ? "light" : "dark";
}

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  // Read the pre-hydration value so the selected theme never briefly changes after mount.
  const [theme, setThemeState] = useState<ThemeName>(initialTheme);

  useEffect(() => {
    function applyStoredTheme() {
      const saved = window.localStorage.getItem(storageKey);
      const nextTheme: ThemeName = saved === "light" || saved === "dark"
        ? saved
        : window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
      setThemeState(nextTheme);
      document.documentElement.dataset.theme = nextTheme;
    }
    function syncFromOtherTab(event: StorageEvent) {
      if (event.key === storageKey) applyStoredTheme();
    }
    const systemTheme = window.matchMedia("(prefers-color-scheme: dark)");
    function syncSystemTheme() {
      if (!window.localStorage.getItem(storageKey)) applyStoredTheme();
    }
    applyStoredTheme();
    window.addEventListener("storage", syncFromOtherTab);
    systemTheme.addEventListener("change", syncSystemTheme);
    return () => {
      window.removeEventListener("storage", syncFromOtherTab);
      systemTheme.removeEventListener("change", syncSystemTheme);
    };
  }, []);

  function setTheme(nextTheme: ThemeName) {
    setThemeState(nextTheme);
    window.localStorage.setItem(storageKey, nextTheme);
    document.documentElement.dataset.theme = nextTheme;
  }

  const value = useMemo(() => ({ theme, setTheme }), [theme]);
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const context = useContext(ThemeContext);
  if (!context) throw new Error("useTheme must be used inside ThemeProvider.");
  return context;
}
