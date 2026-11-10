"use client";

import { useEffect, useRef, useState } from "react";
import { academyApi, acknowledgePortalMaintenanceSignal, isPortalSignOutEvent, portalAccessToken, portalMaintenanceEvent, portalSessionChangedEvent, portalSessionEpoch, portalSignOutEvent, portalWorkspaceForPath, readPortalMaintenanceSignal, type PortalMaintenanceSignal, type PortalWorkspace } from "@/lib/api";

type Notice = { kind: "checking" | "maintenance" | "unknown" | "recovered"; message?: string; restricted?: boolean };

export function ServiceAvailabilityNotice({ workspace }: { workspace: PortalWorkspace }) {
  const [notice, setNotice] = useState<Notice | null>(null);
  const [busy, setBusy] = useState(false);
  const check = useRef<() => void>(() => {});

  useEffect(() => {
    let mounted = true, monitoring = false, queued = false, latestSerial = 0;
    let pending: AbortController | null = null;
    let login: { epoch: string | null; token: string } | null = null;
    let verified: Notice | null = null;
    const current = () => mounted && login && Boolean(portalAccessToken(workspace)?.trim()) &&
      portalSessionEpoch(workspace) === login.epoch && (login.epoch !== null || portalAccessToken(workspace) === login.token) &&
      portalWorkspaceForPath(window.location.pathname) === workspace;
    const reset = () => {
      pending?.abort(); pending = null; queued = false; monitoring = false; login = null; verified = null;
      if (mounted) { setNotice(null); setBusy(false); }
    };
    const read = async () => {
      if (!monitoring || !current()) { if (monitoring) reset(); return; }
      if (pending) return;
      const controller = new AbortController(), serial = latestSerial;
      let timeout: ReturnType<typeof setTimeout> | undefined, timedOut = false;
      pending = controller; queued = false; setBusy(true);
      try {
        const value = await Promise.race([
          (async () => {
            const response = await academyApi("/api/auth/session/availability", { method: "GET", cache: "no-store", signal: controller.signal });
            if (!response.ok) throw new Error("Unavailable");
            return response.json();
          })(),
          new Promise<never>((_, reject) => { timeout = setTimeout(() => { timedOut = true; controller.abort(); reject(new Error("Availability timeout")); }, 10_000); }),
        ]);
        if (typeof value?.maintenanceMode !== "boolean" || typeof value?.restricted !== "boolean" ||
            (!value.maintenanceMode && value.restricted) || (value.maintenanceMode && (typeof value.message !== "string" || !value.message.trim()))) throw new Error("Invalid availability");
        if (!mounted || controller.signal.aborted || !current() || queued || serial !== latestSerial) return;
        if (value.maintenanceMode) {
          verified = { kind: "maintenance", restricted: value.restricted, message: value.message.trim() };
          setNotice(verified);
        } else {
          monitoring = false; verified = null;
          acknowledgePortalMaintenanceSignal(workspace, serial);
          setNotice({ kind: "recovered" });
        }
      } catch {
        if (mounted && (!controller.signal.aborted || timedOut) && current() && !queued && serial === latestSerial)
          setNotice({ ...verified, kind: "unknown" });
      } finally {
        if (timeout !== undefined) clearTimeout(timeout);
        if (mounted && pending === controller) {
          pending = null;
          if (!current()) reset();
          else if (queued && monitoring) void read();
          else setBusy(false);
        }
      }
    };
    check.current = () => { void read(); };
    const receive = (hint: PortalMaintenanceSignal | null) => {
      const cached = readPortalMaintenanceSignal(workspace);
      if (!hint || hint.workspace !== workspace || hint.serial !== cached?.serial || hint.epoch !== portalSessionEpoch(workspace) ||
          !portalAccessToken(workspace)?.trim() || portalWorkspaceForPath(window.location.pathname) !== workspace) return;
      if (login && !current()) reset();
      login ??= { epoch: hint.epoch, token: portalAccessToken(workspace)! };
      latestSerial = hint.serial; monitoring = true;
      if (pending) { queued = true; return; }
      setNotice(verified ?? { kind: "checking" }); void read();
    };
    const signal = (event: Event) => receive((event as CustomEvent<PortalMaintenanceSignal>).detail);
    const session = (event: Event) => {
      if ((event as CustomEvent<PortalWorkspace>).detail === workspace && !current()) reset();
    };
    const logout = (event: Event) => { if ((event as CustomEvent<PortalWorkspace>).detail === workspace) reset(); };
    const storage = (event: StorageEvent) => {
      if (isPortalSignOutEvent(event, workspace) || (event.storageArea === window.localStorage && event.key === `academydesk.sessionEpoch.${workspace}` && !current())) reset();
    };
    const focus = () => { if (monitoring) void read(); };
    window.addEventListener(portalMaintenanceEvent, signal);
    window.addEventListener(portalSessionChangedEvent, session);
    window.addEventListener(portalSignOutEvent, logout);
    window.addEventListener("storage", storage);
    window.addEventListener("focus", focus);
    const timer = setInterval(focus, 30_000);
    receive(readPortalMaintenanceSignal(workspace));
    return () => {
      mounted = false; pending?.abort(); check.current = () => {};
      clearInterval(timer);
      window.removeEventListener(portalMaintenanceEvent, signal);
      window.removeEventListener(portalSessionChangedEvent, session);
      window.removeEventListener(portalSignOutEvent, logout);
      window.removeEventListener("storage", storage);
      window.removeEventListener("focus", focus);
    };
  }, [workspace]);

  if (!notice) return null;
  const owner = notice.restricted === false;
  return <section data-service-availability="" role="status" aria-live="polite" aria-busy={busy}
    style={{ margin: "12px 16px", padding: "16px", border: "1px solid var(--border, #888)", borderRadius: 12, background: "var(--surface, Canvas)", color: "var(--text, CanvasText)", overflowWrap: "anywhere" }}>
    <h2 style={{ fontSize: "1rem", margin: "0 0 8px" }}>{notice.kind === "recovered" ? "Service is available again" : notice.kind === "unknown" ? "Service status could not be verified" : notice.kind === "checking" ? "Checking service status…" : owner ? "Maintenance is enabled" : "Maintenance in progress"}</h2>
    {notice.message && <p style={{ margin: "8px 0" }}>{notice.message}</p>}
    <p style={{ margin: "8px 0" }}>{notice.kind === "recovered" ? "Review saved records before submitting again. Checking status has not submitted your entries." : notice.kind === "unknown" ? "Try checking again. If your session has expired, sign in again. Your entries remain on this page." : owner ? "Your platform owner access remains available to manage maintenance settings." : "Your entries remain on this page. Availability checks do not submit them."}</p>
    <div style={{ display: "flex", flexWrap: "wrap", gap: 12, alignItems: "center" }}>
      {notice.kind === "recovered" ? <button type="button" className="enterprise-action-button" onClick={() => setNotice(null)}>Dismiss notice</button> : <button type="button" className="enterprise-action-button" disabled={busy} onClick={() => check.current()}>{busy ? "Checking…" : "Check availability"}</button>}
      {owner && notice.kind !== "recovered" && <a href="/platform/control?tab=Settings">Manage maintenance settings</a>}
    </div>
  </section>;
}
