"use client";

import { usePathname } from "next/navigation";
import { ThemeToggle } from "@/components/theme-toggle";

const standaloneRoutes = new Set(["/login", "/register"]);

export function StandaloneThemeControl() {
  const pathname = usePathname();
  if (!standaloneRoutes.has(pathname)) return null;

  return <div className="standalone-theme-control"><ThemeToggle /></div>;
}
