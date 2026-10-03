import type { ReactNode } from "react";
import { ThemeProvider } from "@/components/theme-provider";
import "./globals.css";

// Synthetic browser shell: the page under test is copied unchanged, with no portal session or remote fonts.
export default function Layout({ children }: { children: ReactNode }) {
  return <html lang="en" data-theme="light" suppressHydrationWarning><body><ThemeProvider>{children}</ThemeProvider></body></html>;
}
