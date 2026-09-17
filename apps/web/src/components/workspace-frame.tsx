"use client";

import { usePathname } from "next/navigation";
import { EnterpriseShell } from "@/components/enterprise-shell";

const publicRoutes = new Set(["/login", "/register", "/portal", "/platform", "/teacher"]);

export function WorkspaceFrame({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  if (publicRoutes.has(pathname) || pathname.startsWith("/platform/")) return <>{children}</>;
  return <EnterpriseShell>{children}</EnterpriseShell>;
}
