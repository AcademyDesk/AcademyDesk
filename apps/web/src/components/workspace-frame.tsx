"use client";

import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { EnterpriseShell } from "@/components/enterprise-shell";
import { isPortalSignOutEvent, portalWorkspaceForPath, type PortalWorkspace } from "@/lib/api";

const publicRoutes = new Set(["/login", "/register", "/portal", "/platform", "/teacher"]);

function WorkspaceSessionBoundary({ children, workspace }: { children: React.ReactNode; workspace: PortalWorkspace }) {
  const [signedOut, setSignedOut] = useState(false);
  useEffect(() => {
    const syncLogout = (event: StorageEvent) => {
      if (isPortalSignOutEvent(event, workspace)) setSignedOut(true);
    };
    window.addEventListener("storage", syncLogout);
    return () => window.removeEventListener("storage", syncLogout);
  }, [workspace]);
  if (signedOut) return <main className="enterprise-dashboard"><section className="enterprise-data-panel" aria-labelledby="peer-signout-title"><header className="enterprise-panel-header"><h1 id="peer-signout-title">Session ended</h1></header><div className="enterprise-empty-row"><p role="alert">You signed out of this workspace in another tab. Sign in again to continue.</p><a href="/login" className="enterprise-primary-action">Sign in again</a></div></section></main>;
  return <>{children}</>;
}

export function WorkspaceFrame({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  if (pathname === "/login" || pathname === "/register") return <>{children}</>;
  const content = publicRoutes.has(pathname) || pathname.startsWith("/platform/") ? children : <EnterpriseShell>{children}</EnterpriseShell>;
  // Route-keyed boundary discards obsolete UI state when a new workspace is opened.
  return <WorkspaceSessionBoundary key={pathname} workspace={portalWorkspaceForPath(pathname)}>{content}</WorkspaceSessionBoundary>;
}
