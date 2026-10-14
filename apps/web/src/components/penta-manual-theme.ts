"use client";

import { useSyncExternalStore, type CSSProperties } from "react";
import { useTheme } from "@/components/theme-provider";
import { colors, FOUNDATION_VERSION, TOKEN_VERSION } from "@/vendor/penta-shared-ui/tokens";
import styles from "./penta-manual-theme.module.css";

const subscribe = () => () => {};
const clientSnapshot = () => true;
const serverSnapshot = () => false;

/** Presentation only. Does not resolve identity, scope, availability or mode. */
export function usePentaManualTheme() {
  const { theme } = useTheme();
  // A suspended route can hydrate after ThemeProvider has restored light mode.
  // Match its server attributes first, then update; React does not repair mismatches.
  const hydrated = useSyncExternalStore(subscribe, clientSnapshot, serverSnapshot);
  const light = hydrated && theme === "light";
  return {
    className: `penta-ui pui-academy-mapping ${styles.manual}`,
    "data-penta-theme": light ? "prism" : "orbit",
    "data-penta-foundation": FOUNDATION_VERSION,
    "data-penta-tokens": TOKEN_VERSION,
    "data-penta-manual": "fw1-r1",
    style: { "--manual-aura": light ? "#6446b4" : colors.aura.accent } as CSSProperties,
  };
}
