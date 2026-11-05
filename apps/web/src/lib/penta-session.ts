// Compatibility boundary for the recovered PENTA surfaces. Keep the current
// shared request/refresh implementation as the authority for login changes.
import { portalSessionEpoch as currentPortalSessionEpoch, subscribePortalSessionChanges, type PortalWorkspace } from "./api";
export { academyApi, apiUrl, portalAccessToken, portalWorkspaceForPath, isPortalSignOutEvent } from "./api";

export const portalSessionChangedEvent = "academydesk:penta-session-boundary";
export const portalSignOutEvent = "academydesk:penta-signout";
type Watch = { count: number; stop?: () => void };
const watches = new Map<PortalWorkspace, Watch>();
const retained = new WeakMap<AbortSignal, Set<PortalWorkspace>>();

/** Observe only for a mounted view/channel's lifetime; events contain no tokens. */
export function portalSessionEpoch(workspace: PortalWorkspace, signal?: AbortSignal): string | null {
  if (typeof window === "undefined") return null;
  let watch = watches.get(workspace);
  if (!watch) watches.set(workspace, watch = { count: 0 });
  if (signal && !signal.aborted) {
    let scopes = retained.get(signal);
    if (!scopes) retained.set(signal, scopes = new Set());
    if (!scopes.has(workspace)) {
      scopes.add(workspace);
      watch.count++;
      const current = watch;
      if (!current.stop) current.stop = subscribePortalSessionChanges(workspace, () => {
        window.dispatchEvent(new CustomEvent(portalSessionChangedEvent, { detail: workspace }));
      });
      signal.addEventListener("abort", () => {
        scopes.delete(workspace);
        if (--current.count === 0) { current.stop?.(); current.stop = undefined; }
      }, { once: true });
    }
  }
  return currentPortalSessionEpoch(workspace);
}
