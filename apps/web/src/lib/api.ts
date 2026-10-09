export const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export type PortalWorkspace = "AcademyAdmin" | "Teacher" | "Portal" | "Platform";
export const portalSignOutEvent = "academydesk:portal-sign-out";

type Session = { accessToken: string | null; refreshToken: string | null; generation: number };
type RenewedSession = Session & { accessToken: string };
type RefreshFlight = { session: Session; controller: AbortController; consumers: number; finished: boolean; result: RenewedSession | null; promise: Promise<RenewedSession | null> };
// One entry per workspace, scoped to this JS realm. Cross-tab coordination is separate.
const sessionGenerations = new Map<PortalWorkspace, number>();
const refreshFlights = new Map<PortalWorkspace, RefreshFlight>();
const sessionGeneration = (workspace: PortalWorkspace) => sessionGenerations.get(workspace) ?? 0;
function invalidateSession(workspace: PortalWorkspace) {
  sessionGenerations.set(workspace, sessionGeneration(workspace) + 1);
  refreshFlights.delete(workspace);
}

function currentWorkspace(): PortalWorkspace {
  if (typeof window === "undefined") return "AcademyAdmin";
  return portalWorkspaceForPath(window.location.pathname);
}

export function portalWorkspaceForPath(path: string): PortalWorkspace {
  if (path === "/teacher" || path.startsWith("/teacher/")) return "Teacher";
  if (path === "/portal" || path.startsWith("/portal/")) return "Portal";
  if (path === "/platform" || path.startsWith("/platform/")) return "Platform";
  return "AcademyAdmin";
}

function tokenKey(kind: "accessToken" | "refreshToken", workspace: PortalWorkspace) {
  return `academydesk.${kind}.${workspace}`;
}

/** A delivered same-origin logout event, not a token renewal or another storage area. */
export function isPortalSignOutEvent(event: StorageEvent, workspace: PortalWorkspace) {
  if (typeof window === "undefined" || event.storageArea !== window.localStorage || event.newValue !== null) return false;
  return event.key === null || event.key === tokenKey("accessToken", workspace) || event.key === tokenKey("refreshToken", workspace)
    || (workspace === "AcademyAdmin" && (event.key === "academydesk.accessToken" || event.key === "academydesk.refreshToken"));
}

// Other tabs have independent generations: a delivered removal must invalidate
// their pending renewal even when another tab has already restored the same pair.
if (typeof window !== "undefined" && typeof window.addEventListener === "function") {
  window.addEventListener("storage", (event) => {
    for (const workspace of ["AcademyAdmin", "Teacher", "Portal", "Platform"] as const) {
      if (isPortalSignOutEvent(event, workspace)) invalidateSession(workspace);
    }
  });
}

/** Saves a session separately for each role workspace so portal testing does not replace another role's login. */
export function savePortalTokens(workspace: PortalWorkspace, accessToken: string, refreshToken?: string) {
  if (typeof window === "undefined") return;
  invalidateSession(workspace);
  window.localStorage.setItem(tokenKey("accessToken", workspace), accessToken);
  if (refreshToken) window.localStorage.setItem(tokenKey("refreshToken", workspace), refreshToken);
}

/** Signs out only from the portal currently open, without disturbing other role workspaces. */
export function clearPortalTokens(workspace = currentWorkspace()) {
  if (typeof window === "undefined") return;
  invalidateSession(workspace);
  window.localStorage.removeItem(tokenKey("accessToken", workspace));
  window.localStorage.removeItem(tokenKey("refreshToken", workspace));
  // Remove the former shared keys only when the administrator session is being cleared.
  if (workspace === "AcademyAdmin") {
    window.localStorage.removeItem("academydesk.accessToken");
    window.localStorage.removeItem("academydesk.refreshToken");
  }
  // Storage events reach other tabs only. Notify the current tab before navigation too.
  window.dispatchEvent(new CustomEvent(portalSignOutEvent, { detail: workspace }));
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

function sessionMatches(workspace: PortalWorkspace, session: Session) {
  return sessionGeneration(workspace) === session.generation && portalAccessToken(workspace) === session.accessToken && portalRefreshToken(workspace) === session.refreshToken;
}

function startRefresh(workspace: PortalWorkspace, session: Session): RefreshFlight {
  const flight: RefreshFlight = { session, controller: new AbortController(), consumers: 0, finished: false, result: null, promise: Promise.resolve(null) };
  refreshFlights.set(workspace, flight);
  flight.promise = (async () => {
    try {
      const response = await fetch(`${apiUrl}/api/auth/refresh`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ refreshToken: session.refreshToken }), signal: flight.controller.signal });
      if (!response.ok) return null;
      const tokens = await response.json().catch(() => null);
      if (typeof tokens?.accessToken !== "string" || !tokens.accessToken.trim() ||
        (tokens.refreshToken != null && (typeof tokens.refreshToken !== "string" || !tokens.refreshToken.trim())) ||
        flight.controller.signal.aborted || !sessionMatches(workspace, session)) return null;
      savePortalTokens(workspace, tokens.accessToken, tokens.refreshToken);
      flight.result = { accessToken: tokens.accessToken, refreshToken: portalRefreshToken(workspace), generation: sessionGeneration(workspace) };
      // Keep the successful original-pair receipt for late401s; a new login/refresh replaces it.
      refreshFlights.set(workspace, flight);
      return flight.result;
    } catch {
      return null;
    } finally {
      flight.finished = true;
      if (!flight.result && refreshFlights.get(workspace) === flight) refreshFlights.delete(workspace);
    }
  })();
  return flight;
}

async function waitForRefresh(workspace: PortalWorkspace, flight: RefreshFlight, signal?: AbortSignal | null) {
  let onAbort: (() => void) | undefined;
  flight.consumers++;
  try {
    const aborted = new Promise<null>((resolve) => {
      onAbort = () => resolve(null);
      if (signal?.aborted) onAbort();
      else signal?.addEventListener("abort", onAbort, { once: true });
    });
    return await Promise.race([flight.promise, aborted]);
  } finally {
    if (onAbort) signal?.removeEventListener("abort", onAbort);
    flight.consumers--;
    if (!flight.finished && flight.consumers === 0) {
      flight.controller.abort();
      if (refreshFlights.get(workspace) === flight) refreshFlights.delete(workspace);
    }
  }
}

export async function academyApi(path: string, init: RequestInit = {}) {
  // Pin the initiating workspace/session: navigation must not borrow another portal's credentials.
  const workspace = currentWorkspace();
  const originalToken = portalAccessToken(workspace);
  const refreshToken = portalRefreshToken(workspace);
  const session = { accessToken: originalToken, refreshToken, generation: sessionGeneration(workspace) };
  const originalHeaders = new Headers(init.headers);
  if (!originalHeaders.has("Authorization") && originalToken) originalHeaders.set("Authorization", `Bearer ${originalToken}`);
  const request = (renewedToken?: string) => {
    const headers = new Headers(originalHeaders);
    // A copied apiHeaders() token must never overwrite the freshly renewed bearer.
    if (renewedToken) headers.set("Authorization", `Bearer ${renewedToken}`);
    return fetch(`${apiUrl}${path}`, { ...init, headers });
  };
  const response = await request();
  if (response.status !== 401 || path.startsWith("/api/auth/refresh") || init.signal?.aborted) return response;

  // Explicit foreign credentials and replaced/signed-out sessions must not be refreshed on this account's behalf.
  if (!refreshToken || originalHeaders.get("Authorization") !== (originalToken ? `Bearer ${originalToken}` : null)) return response;
  let flight = refreshFlights.get(workspace);
  const matchingFlight = flight && flight.session.generation === session.generation && flight.session.accessToken === originalToken && flight.session.refreshToken === refreshToken;
  if (!matchingFlight) {
    if (!sessionMatches(workspace, session)) return response;
    flight = startRefresh(workspace, session);
  } else if (!flight?.result && !sessionMatches(workspace, session)) return response;
  const renewed = await waitForRefresh(workspace, flight!, init.signal);
  if (!renewed || init.signal?.aborted || !sessionMatches(workspace, renewed)) return response;
  return request(renewed.accessToken);
}
