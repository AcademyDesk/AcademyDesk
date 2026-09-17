import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { ThemeProvider } from "@/components/theme-provider";
import { StandaloneThemeControl } from "@/components/standalone-theme-control";
import { WorkspaceFrame } from "@/components/workspace-frame";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "AcademyDesk",
  description: "Academy operations for music, tuition, and coaching institutes.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      data-theme="dark"
      suppressHydrationWarning
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <head>
        <meta name="theme-color" content="#07111f" />
        <script dangerouslySetInnerHTML={{ __html: "try { const saved = localStorage.getItem('academydesk.theme'); const theme = saved === 'light' || saved === 'dark' ? saved : matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'; document.documentElement.dataset.theme = theme; } catch {}" }} />
      </head>
      <body className="min-h-full flex flex-col"><ThemeProvider><StandaloneThemeControl /><WorkspaceFrame>{children}</WorkspaceFrame></ThemeProvider></body>
    </html>
  );
}
