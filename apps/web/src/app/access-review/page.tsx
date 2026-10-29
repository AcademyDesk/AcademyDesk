"use client";

import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import {
  StandardDateField,
  StandardSelectField,
} from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Staff = {
  id: string;
  displayName: string;
  email: string;
  roles: string[];
  isActive: boolean;
};
type Grant = {
  id: string;
  userName: string;
  permissions: string[];
  isPermanent: boolean;
  expiresAtUtc?: string;
  reason?: string;
  revokedAtUtc?: string;
};
function grantStatus(grant: Grant, now: number) {
  if (grant.revokedAtUtc) return "Revoked";
  if (grant.isPermanent) return "Active";
  const expiry = Date.parse(grant.expiresAtUtc ?? "");
  if (!Number.isFinite(expiry)) return "Expiry unavailable";
  return expiry > now ? "Active" : "Expired";
}
const permissions = [
  ["sales.manage", "Sales & marketing"],
  ["students.onboard", "Student onboarding"],
  ["students.manage", "Student management"],
  ["student-fees.manage", "Student fees"],
  ["batches.manage", "Class & batch"],
  ["scheduling.manage", "Scheduling"],
  ["attendance.manage", "Attendance"],
  ["makeup.manage", "Make-up classes"],
  ["finance.manage", "Finance"],
  ["academics.manage", "Academics"],
  ["workforce.manage", "Operations & workforce"],
  ["reports.export", "Reports & exports"],
] as const;

export default function AccessReview() {
  const [academy, setAcademy] = useState<Academy>();
  const [staff, setStaff] = useState<Staff[]>([]);
  const [grants, setGrants] = useState<Grant[]>([]);
  const [message, setMessage] = useState("Loading access controls…");
  const [permanent, setPermanent] = useState(false);
  const [expiryDate, setExpiryDate] = useState("");
  const [userId, setUserId] = useState("");
  const [now, setNow] = useState(0);
  const [busy, setBusy] = useState(false);
  const [needsRefresh, setNeedsRefresh] = useState(false);
  const active = useRef(true);
  const operation = useRef(false);
  const readRequired = useRef(false);

  async function read(signal?: AbortSignal) {
      const response = await academyApi("/api/academies", { signal });
      if (!response.ok) throw Error();
      const academies: Academy[] = await response.json();
      if (!Array.isArray(academies) || !academies[0]?.id) throw Error();
      const [staffResponse, grantsResponse] = await Promise.all([
        academyApi(`/api/academies/${academies[0].id}/staff`, { signal }),
        academyApi(`/api/academies/${academies[0].id}/access-grants`, { signal }),
      ]);
      if (!staffResponse.ok || !grantsResponse.ok) throw Error();
      const [people, permissions]: [Staff[], Grant[]] = await Promise.all([staffResponse.json(), grantsResponse.json()]);
      if (!Array.isArray(people) || !Array.isArray(permissions)) throw Error();
      return { academy: academies[0], people, permissions };
  }
  const publish = useCallback((snapshot: Awaited<ReturnType<typeof read>>) => {
      setAcademy(snapshot.academy);
      setStaff(snapshot.people);
      setGrants(snapshot.permissions);
      setNow(Date.now());
  }, []);
  useEffect(() => {
    const updateClock = () => setNow(Date.now());
    const visible = () => { if (document.visibilityState === "visible") updateClock(); };
    const current = Date.now();
    const future = grants.filter(grant => !grant.revokedAtUtc && !grant.isPermanent)
      .map(grant => Date.parse(grant.expiresAtUtc ?? "")).filter(expiry => Number.isFinite(expiry) && expiry > current);
    const delay = Math.max(1, Math.min(60_000, ...future.map(expiry => expiry - current)));
    const timer = setTimeout(updateClock, delay);
    window.addEventListener("focus", updateClock);
    document.addEventListener("visibilitychange", visible);
    return () => {
      clearTimeout(timer);
      window.removeEventListener("focus", updateClock);
      document.removeEventListener("visibilitychange", visible);
    };
  }, [grants, now]);
  useEffect(() => {
    active.current = true;
    const controller = new AbortController();
    void read(controller.signal).then(snapshot => {
      if (!controller.signal.aborted) { publish(snapshot); setMessage(""); }
    }).catch(() => { if (!controller.signal.aborted) setMessage("Access controls could not be loaded."); });
    return () => { active.current = false; controller.abort(); };
  }, [publish]);

  async function refresh() {
    if (operation.current) return;
    operation.current = true;
    setBusy(true);
    try {
      const snapshot = await read();
      if (!active.current) return;
      publish(snapshot);
      readRequired.current = false;
      setNeedsRefresh(false);
      setMessage("Access controls refreshed. Check the register for the previous operation before submitting another change.");
    } catch {
      if (active.current) setMessage("Access controls could not be refreshed. Do not repeat the previous change until its result is checked.");
    } finally {
      operation.current = false;
      if (active.current) setBusy(false);
    }
  }

  async function mutate(method: "POST" | "PATCH", path: string, body?: unknown, formElement?: HTMLFormElement) {
    if (!academy || operation.current || readRequired.current) return;
    operation.current = true;
    setBusy(true);
    const success = method === "POST" ? "Access granted." : "Access revoked.";
    let confirmed = false;
    try {
      const response = await academyApi(path, {
        method, headers: apiHeaders(true), ...(body === undefined ? {} : { body: JSON.stringify(body) }),
      });
      if (!active.current) return;
      if (!response.ok && response.status < 500) {
        const payload = await response.json().catch(() => null);
        if (active.current) setMessage(typeof payload?.message === "string" ? payload.message : "Access could not be changed. Check your permissions and entered details.");
        return;
      }
      if (response.status !== (method === "POST" ? 200 : 204)) throw Error("Unconfirmed operation");
      if (method === "POST") {
        const receipt = await response.json();
        if (!receipt || typeof receipt.id !== "string" || !/^[0-9a-f-]{36}$/i.test(receipt.id)) throw Error("Unconfirmed receipt");
      }
      if (!active.current) return;
      confirmed = true;
      if (formElement) {
        formElement.reset();
        setPermanent(false);
        setExpiryDate("");
        setUserId("");
      }
      const snapshot = await read();
      if (!active.current) return;
      publish(snapshot);
      setMessage(success);
    } catch {
      if (!active.current) return;
      readRequired.current = true;
      setNeedsRefresh(true);
      setMessage(confirmed
        ? `${success} The grant register could not be refreshed. Refresh access controls and check the register; do not repeat the change.`
        : "The access change could not be confirmed. Refresh access controls and check the register before retrying; do not repeat the change automatically.");
    } finally {
      operation.current = false;
      if (active.current) setBusy(false);
    }
  }

  async function grant(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || operation.current || readRequired.current) return;
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    const selected = permissions
      .map(([key]) => key)
      .filter((key) => form.get(key) === "on");
    if (!userId || !selected.length)
      return setMessage("Select a staff member and at least one permission.");
    if (!permanent && !expiryDate)
      return setMessage("Select an expiry date or grant permanent access.");
    await mutate("POST",
      `/api/academies/${academy.id}/access-grants`,
      {
          userId,
          permissions: selected,
          isPermanent: permanent,
          expiresAtUtc: permanent
            ? null
            : new Date(`${expiryDate}T23:59:59`).toISOString(),
          reason: form.get("reason"),
      }, formElement,
    );
  }
  async function revoke(id: string) {
    if (!academy) return;
    await mutate("PATCH",
      `/api/academies/${academy.id}/access-grants/${id}/revoke`,
    );
  }

  return (
    <main className="enterprise-settings access-review-standard min-h-screen">
      <WorkspaceNav />
      <div className="access-review-content mx-auto max-w-6xl px-6 py-10">
        <header className="access-review-heading">
          <div className="access-review-title">
            <span className="access-review-title-icon" aria-hidden="true">
              ✓
            </span>
            <div>
              <p>Operations &amp; workforce</p>
              <h1>Access Review</h1>
            </div>
          </div>
        </header>
        {message && (
          <p
            className="enterprise-page-state access-review-message"
            role="status"
            aria-live="polite"
          >
            {message}
          </p>
        )}
        <button type="button" className="enterprise-action-button" disabled={busy} onClick={refresh}>
          Refresh access controls
        </button>
        <section className="access-review-layout">
          <form onSubmit={grant} className="access-review-panel" aria-busy={busy}>
            <header className="access-review-panel-header">
              <div>
                <p>Permissions</p>
                <h2>Grant function access</h2>
              </div>
            </header>
            <fieldset className="access-review-fields" disabled={busy || needsRefresh || !academy} style={{ border: 0, margin: 0, minWidth: 0 }}>
              <StandardSelectField
                name="userId"
                label="Staff member"
                value={userId}
                onChange={setUserId}
                placeholder="Select staff member"
                options={staff
                  .filter((person) => person.isActive)
                  .map((person) => ({
                    value: person.id,
                    label: `${person.displayName} · ${person.roles.join(", ") || "No role"}`,
                  }))}
              />
              <fieldset>
                <legend>Permissions</legend>
                <div className="access-permission-grid">
                  {permissions.map(([key, label]) => (
                    <label key={key}>
                      <input name={key} type="checkbox" />
                      <span>{label}</span>
                    </label>
                  ))}
                </div>
              </fieldset>
              <label className="access-permanent">
                <input
                  checked={permanent}
                  onChange={(event) => setPermanent(event.target.checked)}
                  type="checkbox"
                />
                <span>Permanent access</span>
              </label>
              {!permanent && (
                <StandardDateField
                  name="expiresAtUtc"
                  label="Expires on"
                  value={expiryDate}
                  onChange={setExpiryDate}
                  required
                />
              )}
              <label>
                <span>Reason</span>
                <textarea name="reason" placeholder="Reason for this access" />
              </label>
              <button
                className="enterprise-action-button access-grant-button"
                disabled={!academy || busy || needsRefresh}
              >
                Grant access
              </button>
            </fieldset>
          </form>
          <section className="access-review-panel access-grants-panel">
            <header className="access-review-panel-header">
              <div>
                <p>Register</p>
                <h2>Active and previous grants</h2>
              </div>
              <span>
                {grants.filter((grant) => grantStatus(grant, now) === "Active").length} active
              </span>
            </header>
            {grants.length ? (
              <ul>
                {grants.map((grant) => (
                  <li key={grant.id}>
                    <div>
                      <b>{grant.userName}</b>
                      <small>{grant.permissions.join(" · ")}</small>
                      <small>
                        {grant.isPermanent ? "Permanent access"
                          : Number.isFinite(Date.parse(grant.expiresAtUtc ?? ""))
                            ? `Expires ${new Intl.DateTimeFormat("en-IN", { dateStyle: "medium", timeStyle: "short", timeZone: "Asia/Kolkata" }).format(new Date(grant.expiresAtUtc!))} IST`
                            : "Expiry unavailable"}
                      </small>
                      {grantStatus(grant, now) === "Active" && <small aria-label="Grant status">Active</small>}
                      {grant.reason && <small>{grant.reason}</small>}
                    </div>
                    {grantStatus(grant, now) !== "Active" ? (
                      <span>{grantStatus(grant, now)}</span>
                    ) : (
                      <button
                        type="button"
                        disabled={busy || needsRefresh || !academy}
                        onClick={() => revoke(grant.id)}
                      >
                        Revoke access
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            ) : (
              <p className="access-grants-empty">No access grants created.</p>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
