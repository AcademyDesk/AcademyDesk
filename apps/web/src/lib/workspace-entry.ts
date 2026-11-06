import type { PortalWorkspace } from "./api";

export type WorkspaceAccount = {
  workspace: PortalWorkspace;
  roles: string[];
  isPlatformOwner: boolean;
};

/** UI routing only. Domain/API authorization remains authoritative. */
export function validWorkspaceAccount(value: unknown): value is WorkspaceAccount {
  if (!value || typeof value !== "object") return false;
  const account = value as Record<string, unknown>;
  if (!Array.isArray(account.roles) || !account.roles.every(role => typeof role === "string") || typeof account.isPlatformOwner !== "boolean") return false;
  if (account.isPlatformOwner) return account.workspace === "Platform";
  if (account.workspace === "Teacher") return account.roles.includes("Teacher");
  if (account.workspace === "Portal") return account.roles.some(role => role === "Student" || role === "Guardian");
  return account.workspace === "AcademyAdmin" && account.roles.length > 0 && !account.roles.some(role => ["PlatformOwner", "Teacher", "Student", "Guardian"].includes(role));
}

export const workspaceHomes: Record<PortalWorkspace, { ai: string; manual: string }> = {
  Platform: { ai: "/platform/ai", manual: "/platform" },
  Teacher: { ai: "/teacher/ai", manual: "/teacher" },
  Portal: { ai: "/portal/ai", manual: "/portal" },
  AcademyAdmin: { ai: "/penta", manual: "/dashboard" },
};

export function workspaceLanding(account: WorkspaceAccount, requestedReturn?: string | null): string {
  // Explicit return links are scoped to this server-reported workspace. They
  // cannot switch the token namespace, open an external URL or skip API policy.
  if (requestedReturn?.startsWith("/") && !/[\\\u0000-\u0020]/.test(requestedReturn) && !requestedReturn.startsWith("//")) {
    const url = new URL(requestedReturn, "https://local.invalid");
    const home = workspaceHomes[account.workspace].manual;
    const ownRoute = account.workspace === "AcademyAdmin"
      ? !/^\/(?:platform|teacher|portal)(?:\/|$)/.test(url.pathname) && !["/login", "/register"].includes(url.pathname)
      : url.pathname === home || url.pathname.startsWith(home + "/");
    if (url.origin === "https://local.invalid" && !url.pathname.includes("%") && ownRoute) return url.pathname + url.search + url.hash;
  }
  if (account.workspace === "AcademyAdmin" && !account.roles.some(role => role === "Owner" || role === "AcademyAdmin")) return "/dashboard";
  return workspaceHomes[account.workspace].ai;
}
