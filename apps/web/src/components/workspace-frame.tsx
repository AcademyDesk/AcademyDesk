"use client";

import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { EnterpriseShell } from "@/components/enterprise-shell";
import { isPortalSignOutEvent, portalAccessToken, portalSessionEpoch, portalSessionChangedEvent, portalSignOutEvent, portalWorkspaceForPath, type PortalWorkspace } from "@/lib/api";
import { RoleManualModeBar } from "@/components/penta/role-penta-entry";

// The onboarding review supplies its own authenticated read-only bootstrap/shell.
// Shell exclusion is not endpoint authorization and does not enable AI actions.
const publicRoutes = new Set(["/login", "/register", "/portal", "/platform", "/teacher", "/teacher/ai", "/portal/ai", "/penta/onboarding", "/penta/teachers", "/penta/teachers/chat"]);

function WorkspaceSessionBoundary({ children, workspace }: { children: React.ReactNode; workspace: PortalWorkspace }) {
  const [signedOut, setSignedOut] = useState(false);
  useEffect(() => {
    // A suspended/restored page may miss a StorageEvent. Presence is only a UI
    // guard: the server still authorizes each operation; tokens are not decoded.
    let observedSession = Boolean(portalAccessToken(workspace));
    let observedEpoch = portalSessionEpoch(workspace);
    const checkSession = () => {
      const hasSession = Boolean(portalAccessToken(workspace));
      const epoch = portalSessionEpoch(workspace);
      if (observedSession && (!hasSession || epoch !== observedEpoch)) setSignedOut(true);
      if (!observedSession && hasSession) observedEpoch = epoch;
      observedSession ||= hasSession;
    };
    const checkVisibleSession = () => {
      if (document.visibilityState === "visible") checkSession();
    };
    const syncLogout = (event: StorageEvent) => {
      if (isPortalSignOutEvent(event, workspace)) setSignedOut(true);
      else if (event.storageArea === window.localStorage && event.key === `academydesk.sessionEpoch.${workspace}`) checkSession();
    };
    const syncLocalLogout = (event: Event) => {
      if ((event as CustomEvent<PortalWorkspace>).detail === workspace) setSignedOut(true);
    };
    const syncLocalSession = (event: Event) => {
      if ((event as CustomEvent<PortalWorkspace>).detail === workspace) checkSession();
    };
    window.addEventListener("storage", syncLogout);
    window.addEventListener(portalSignOutEvent, syncLocalLogout);
    window.addEventListener(portalSessionChangedEvent, syncLocalSession);
    window.addEventListener("focus", checkSession);
    window.addEventListener("pageshow", checkSession);
    document.addEventListener("visibilitychange", checkVisibleSession);
    return () => {
      window.removeEventListener("storage", syncLogout);
      window.removeEventListener(portalSignOutEvent, syncLocalLogout);
      window.removeEventListener(portalSessionChangedEvent, syncLocalSession);
      window.removeEventListener("focus", checkSession);
      window.removeEventListener("pageshow", checkSession);
      document.removeEventListener("visibilitychange", checkVisibleSession);
    };
  }, [workspace]);
  if (signedOut) return <main className="enterprise-dashboard"><section className="enterprise-data-panel" aria-labelledby="peer-signout-title"><header className="enterprise-panel-header"><h1 id="peer-signout-title">Session ended</h1></header><div className="enterprise-empty-row"><p role="alert">This workspace was signed out, here or in another tab. Sign in again to continue.</p><a href="/login" className="enterprise-primary-action">Sign in again</a></div></section></main>;
  return <>{children}</>;
}

export function WorkspaceFrame({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  if (pathname === "/login" || pathname === "/register") return <>{children}</>;
  const content = publicRoutes.has(pathname) || pathname.startsWith("/platform/") ? children : <EnterpriseShell>{children}</EnterpriseShell>;
  const workspace = portalWorkspaceForPath(pathname);
  const roleManual = workspace !== "AcademyAdmin" && !pathname.endsWith("/ai");
  // Route-keyed boundary discards obsolete UI state when a new workspace is opened.
  return <WorkspaceSessionBoundary key={pathname} workspace={workspace}>{roleManual && <RoleManualModeBar workspace={workspace} />}{content}</WorkspaceSessionBoundary>;
}
