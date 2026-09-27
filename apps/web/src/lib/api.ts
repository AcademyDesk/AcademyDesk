export const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export type PortalWorkspace = "AcademyAdmin" | "Teacher" | "Portal" | "Platform";

function currentWorkspace(): PortalWorkspace {
  if (typeof window === "undefined") return "AcademyAdmin";
  const path = window.location.pathname;
  if (path === "/teacher" || path.startsWith("/teacher/")) return "Teacher";
  if (path === "/portal" || path.startsWith("/portal/")) return "Portal";
  if (path === "/platform" || path.startsWith("/platform/")) return "Platform";
  return "AcademyAdmin";
}

function tokenKey(kind: "accessToken" | "refreshToken", workspace: PortalWorkspace) {
  return `academydesk.${kind}.${workspace}`;
}

/** Saves a session separately for each role workspace so portal testing does not replace another role's login. */
export function savePortalTokens(workspace: PortalWorkspace, accessToken: string, refreshToken?: string) {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(tokenKey("accessToken", workspace), accessToken);
  if (refreshToken) window.localStorage.setItem(tokenKey("refreshToken", workspace), refreshToken);
}

/** Signs out only from the portal currently open, without disturbing other role workspaces. */
export function clearPortalTokens(workspace = currentWorkspace()) {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(tokenKey("accessToken", workspace));
  window.localStorage.removeItem(tokenKey("refreshToken", workspace));
  // Remove the former shared keys only when the administrator session is being cleared.
  if (workspace === "AcademyAdmin") {
    window.localStorage.removeItem("academydesk.accessToken");
    window.localStorage.removeItem("academydesk.refreshToken");
  }
}

export function portalAccessToken(workspace = currentWorkspace()) {
  if (typeof window === "undefined") return null;
  // The fallback keeps existing administrator sessions working after this upgrade.
  return window.localStorage.getItem(tokenKey("accessToken", workspace))
    ?? (workspace === "AcademyAdmin" ? window.localStorage.getItem("academydesk.accessToken") : null);
}

function portalRefreshToken(workspace = currentWorkspace()) {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(tokenKey("refreshToken", workspace))
    ?? (workspace === "AcademyAdmin" ? window.localStorage.getItem("academydesk.refreshToken") : null);
}

export function apiHeaders(json = false): Record<string, string> {
  const token = portalAccessToken();

  return {
    ...(json ? { "Content-Type": "application/json" } : {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

export async function academyApi(path: string, init: RequestInit = {}) {
  const request = (token = portalAccessToken()) => fetch(`${apiUrl}${path}`, {
    ...init,
    headers: { ...apiHeaders(), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...init.headers },
  });
  const response = await request();
  if (response.status !== 401 || path.startsWith("/api/auth/refresh")) return response;

  const workspace = currentWorkspace();
  const refreshToken = portalRefreshToken(workspace);
  if (!refreshToken) return response;
  const refreshed = await fetch(`${apiUrl}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  }).catch(() => null);
  if (!refreshed?.ok) return response;
  const tokens = await refreshed.json().catch(() => null);
  if (!tokens?.accessToken) return response;
  savePortalTokens(workspace, tokens.accessToken, tokens.refreshToken);
  return request(tokens.accessToken);
}
