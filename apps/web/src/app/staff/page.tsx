"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Staff = {
  id: string;
  displayName: string;
  email: string;
  roles: string[];
  isActive: boolean;
};

const staffRoles = [
  "Manager",
  "Operations",
  "Sales",
  "Marketing",
  "FinanceUser",
  "FrontDesk",
];

const roleLabel = (role: string) =>
  role === "FinanceUser"
    ? "Finance"
    : role === "FrontDesk"
      ? "Front desk"
      : role;

export default function StaffPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [staff, setStaff] = useState<Staff[]>([]);
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState("Operations");
  const [message, setMessage] = useState("Loading staff accounts…");
  const [updatingStaffId, setUpdatingStaffId] = useState<string>();
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const staffResponse = await academyApi(
      `/api/academies/${academyId}/staff`,
      { cache: "no-store" },
    );
    if (staffResponse.status === 403)
      return setMessage("Only the academy owner can manage staff accounts.");
    if (!staffResponse.ok) throw new Error();
    setStaff(await staffResponse.json());
    setMessage("");
  }
  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (response.status === 401)
          return setMessage("Please sign in before managing staff.");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create your academy first.");
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setMessage(
          "Staff accounts could not be loaded. Restart the API after applying the staff migration.",
        );
      }
    }
    void initialise();
  }, []);
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const response = await academyApi(`/api/academies/${academy.id}/staff`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify({
        email,
        displayName,
        password,
        role,
      }),
    });
    if (!response.ok) {
      const result = await response.json().catch(() => null);
      return setMessage(
        result?.message ??
          "The staff account could not be created. Check the password requirements and email.",
      );
    }
    setEmail("");
    setDisplayName("");
    setPassword("");
    setMessage("");
    await load();
  }
  async function offboard(person: Staff) {
    if (
      !academy ||
      !window.confirm(
        `Offboard ${person.displayName}? Their active sessions will be invalidated.`,
      )
    )
      return;
    const response = await academyApi(
      `/api/academies/${academy.id}/staff/${person.id}/offboard`,
      { method: "POST", headers: apiHeaders(true) },
    );
    if (!response.ok)
      return setMessage("Staff offboarding could not be completed.");
    setMessage(
      `${person.displayName} has been offboarded and active sessions were revoked.`,
    );
    await load();
  }
  async function updateRole(person: Staff, nextRole: string) {
    if (!academy || person.roles.includes(nextRole)) return;
    setUpdatingStaffId(person.id);
    const response = await academyApi(
      `/api/academies/${academy.id}/staff/${person.id}/role`,
      {
        method: "PATCH",
        headers: apiHeaders(true),
        body: JSON.stringify({ role: nextRole }),
      },
    );
    setUpdatingStaffId(undefined);
    if (!response.ok) return setMessage("The staff role could not be updated.");
    setMessage(`${person.displayName}'s access role was updated.`);
    await load();
  }
  return (
    <main className="enterprise-settings staff-standard min-h-screen">
      <WorkspaceNav />
      <div className="staff-content mx-auto max-w-6xl px-6 py-10">
        <header className="staff-heading">
          <div className="staff-title">
            <span className="staff-title-icon" aria-hidden="true">
              ♙
            </span>
            <div>
              <p>Operations &amp; workforce</p>
              <h1>Staff Directory &amp; Onboarding</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state staff-message">{message}</p>
        )}
        <section className="staff-layout">
          <form onSubmit={create} className="staff-panel staff-create-panel">
            <header className="staff-panel-header">
              <div>
                <p>New account</p>
                <h2>Onboard staff</h2>
              </div>
            </header>
            <div className="staff-fields">
              <label>
                <span>Staff name</span>
                <input
                  value={displayName}
                  onChange={(event) => setDisplayName(event.target.value)}
                  placeholder="Full name"
                  required
                />
              </label>
              <label>
                <span>Work email</span>
                <input
                  type="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  placeholder="name@academy.com"
                  required
                />
              </label>
              <label>
                <span>Temporary password</span>
                <input
                  type="password"
                  minLength={6}
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  placeholder="Minimum 6 characters"
                  required
                />
              </label>
              <StandardSelectField
                name="staff-role"
                value={role}
                onChange={setRole}
                placeholder="Access role"
                options={staffRoles.map((value) => ({
                  value,
                  label: roleLabel(value),
                }))}
              />
              <button
                disabled={!academy}
                className="enterprise-action-button staff-create-button"
              >
                Create staff account
              </button>
            </div>
          </form>
          <section className="staff-panel staff-directory-panel">
            <header className="staff-panel-header">
              <div>
                <p>Directory</p>
                <h2>Staff accounts</h2>
              </div>
              <span>
                {staff.filter((person) => person.isActive).length} active
              </span>
            </header>
            {staff.length === 0 ? (
              <p className="staff-empty">No staff accounts yet.</p>
            ) : (
              <ul>
                {staff.map((person) => (
                  <li key={person.id}>
                    <div className="staff-person">
                      <div>
                        <b>{person.displayName}</b>
                        <small>{person.email}</small>
                      </div>
                      <label>
                        <span>Access role</span>
                        <StandardSelectField
                          name={`staff-role-${person.id}`}
                          value={person.roles[0] ?? "Operations"}
                          onChange={(nextRole) =>
                            void updateRole(person, nextRole)
                          }
                          placeholder="Access role"
                          options={staffRoles.map((value) => ({
                            value,
                            label: roleLabel(value),
                          }))}
                          disabled={
                            !person.isActive || updatingStaffId === person.id
                          }
                        />
                      </label>
                    </div>
                    <div className="staff-person-actions">
                      {person.isActive ? (
                        <button
                          onClick={() => void offboard(person)}
                          className="staff-offboard-button"
                        >
                          Offboard
                        </button>
                      ) : (
                        <span>Offboarded</span>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
