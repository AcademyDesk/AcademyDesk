import type { ReactNode } from "react";
import Probe from "./qa-probe";
import { ThemeProvider } from "@/components/theme-provider";
import "./globals.css";
// QA-only shell: no signed-in workspace/session or Google font request.
export default function Layout({ children }: { children: ReactNode }) {
  return <html lang="en" data-theme="light" suppressHydrationWarning><body><ThemeProvider><Probe />{children}</ThemeProvider></body></html>;
}
