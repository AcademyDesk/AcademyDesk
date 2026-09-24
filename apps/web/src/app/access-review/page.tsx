"use client";

import { FormEvent, useEffect, useState } from "react";
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

  async function load() {
    try {
      const academies: Academy[] = await (
        await academyApi("/api/academies")
      ).json();
      if (!academies[0]) throw Error();
      setAcademy(academies[0]);
      const [staffResponse, grantsResponse] = await Promise.all([
        academyApi(`/api/academies/${academies[0].id}/staff`),
        academyApi(`/api/academies/${academies[0].id}/access-grants`),
      ]);
      if (!staffResponse.ok || !grantsResponse.ok) throw Error();
      setStaff(await staffResponse.json());
      setGrants(await grantsResponse.json());
      setMessage("");
    } catch {
      setMessage("Access controls could not be loaded.");
    }
  }
  useEffect(() => {
    void load();
  }, []);

  async function grant(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const form = new FormData(event.currentTarget);
    const selected = permissions
      .map(([key]) => key)
      .filter((key) => form.get(key) === "on");
    if (!userId || !selected.length)
      return setMessage("Select a staff member and at least one permission.");
    if (!permanent && !expiryDate)
      return setMessage("Select an expiry date or grant permanent access.");
    const response = await academyApi(
      `/api/academies/${academy.id}/access-grants`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          userId,
          permissions: selected,
          isPermanent: permanent,
          expiresAtUtc: permanent
            ? null
            : new Date(`${expiryDate}T23:59:59`).toISOString(),
          reason: form.get("reason"),
        }),
      },
    );
    if (!response.ok) {
      const payload = await response.json().catch(() => null);
      return setMessage(payload?.message || "Access could not be granted.");
    }
    event.currentTarget.reset();
    setPermanent(false);
    setExpiryDate("");
    setUserId("");
    setMessage("Access granted.");
    await load();
  }
  async function revoke(id: string) {
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/access-grants/${id}/revoke`,
      { method: "PATCH", headers: apiHeaders(true) },
    );
    if (!response.ok) return setMessage("Access could not be revoked.");
    setMessage("Access revoked.");
    await load();
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
          >
            {message}
          </p>
        )}
        <section className="access-review-layout">
          <form onSubmit={grant} className="access-review-panel">
            <header className="access-review-panel-header">
              <div>
                <p>Permissions</p>
                <h2>Grant function access</h2>
              </div>
            </header>
            <div className="access-review-fields">
              <StandardSelectField
                name="userId"
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
                disabled={!academy}
              >
                Grant access
              </button>
            </div>
          </form>
          <section className="access-review-panel access-grants-panel">
            <header className="access-review-panel-header">
              <div>
                <p>Register</p>
                <h2>Active and previous grants</h2>
              </div>
              <span>
                {grants.filter((grant) => !grant.revokedAtUtc).length} active
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
                        {grant.isPermanent
                          ? "Permanent access"
                          : `Expires ${new Intl.DateTimeFormat("en-IN", { dateStyle: "medium", timeZone: "Asia/Kolkata" }).format(new Date(grant.expiresAtUtc!))} IST`}
                      </small>
                      {grant.reason && <small>{grant.reason}</small>}
                    </div>
                    {grant.revokedAtUtc ? (
                      <span>Revoked</span>
                    ) : (
                      <button
                        type="button"
                        onClick={() => void revoke(grant.id)}
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
