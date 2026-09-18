"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";

export type ThemeName = "light" | "dark";

type ThemeContextValue = {
  theme: ThemeName;
  setTheme: (theme: ThemeName) => void;
};

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);
const storageKey = "academydesk.theme";

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  // This deliberately matches the server output. The saved browser preference is
  // applied after hydration, avoiding React attribute-mismatch warnings.
  const [theme, setThemeState] = useState<ThemeName>("dark");

  useEffect(() => {
    function applyStoredTheme() {
      const saved = window.localStorage.getItem(storageKey);
      const nextTheme: ThemeName =
        saved === "light" || saved === "dark"
          ? saved
          : window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
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
    document
      .querySelector('meta[name="theme-color"]')
      ?.setAttribute("content", nextTheme === "dark" ? "#07111f" : "#f4f7fb");
  }

  const value = useMemo(() => ({ theme, setTheme }), [theme]);
  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
}

export function useTheme() {
  const context = useContext(ThemeContext);
  if (!context) throw new Error("useTheme must be used inside ThemeProvider.");
  return context;
}
