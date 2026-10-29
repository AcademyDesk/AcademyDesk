"use client";

import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
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
  const [busy, setBusy] = useState(false);
  const [needsRefresh, setNeedsRefresh] = useState(false);
  const active = useRef(true);
  const operation = useRef(false);
  const readRequired = useRef(false);
  const read = useCallback(async (signal?: AbortSignal) => {
    const response = await academyApi("/api/academies", { cache: "no-store", signal });
    if (!response.ok) throw Error();
    const academies: Academy[] = await response.json();
    if (!Array.isArray(academies) || !academies[0]?.id) throw Error();
    const directory = await academyApi(`/api/academies/${academies[0].id}/staff`, { cache: "no-store", signal });
    if (!directory.ok) throw Error();
    const people: Staff[] = await directory.json();
    if (!Array.isArray(people) || people.some(person => !person || typeof person.id !== "string" ||
      typeof person.displayName !== "string" || typeof person.email !== "string" ||
      typeof person.isActive !== "boolean" || !Array.isArray(person.roles) ||
      person.roles.some(role => typeof role !== "string"))) throw Error();
    return { academy: academies[0], people };
  }, []);
  const publish = useCallback((snapshot: Awaited<ReturnType<typeof read>>) => {
    setAcademy(snapshot.academy);
    setStaff(snapshot.people);
  }, []);
  useEffect(() => {
    active.current = true;
    const controller = new AbortController();
    void read(controller.signal).then(snapshot => {
      if (!controller.signal.aborted) { publish(snapshot); setMessage(""); }
    }).catch(() => { if (!controller.signal.aborted) setMessage("Staff accounts could not be loaded. Check your connection and current access, then refresh the directory."); });
    return () => { active.current = false; controller.abort(); };
  }, [read, publish]);

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
      setMessage("Staff directory refreshed. Check the directory for the previous operation before making another change.");
    } catch {
      if (active.current) setMessage("Staff directory could not be refreshed. Do not repeat the previous change until its result is checked.");
    } finally {
      operation.current = false;
      if (active.current) setBusy(false);
    }
  }

  async function mutate(path: string, method: "POST" | "PATCH", body: unknown, success: string, kind: "create" | "role" | "offboard") {
    if (!academy || operation.current || readRequired.current) return;
    operation.current = true;
    setBusy(true);
    let confirmed = false;
    try {
      const response = await academyApi(path, { method, headers: apiHeaders(true),
        ...(body === undefined ? {} : { body: JSON.stringify(body) }) });
      if (!active.current) return;
      if (!response.ok && response.status < 500) {
        const result = await response.json().catch(() => null);
        if (active.current) setMessage(typeof result?.message === "string" ? result.message : "The staff change was refused. Check the details and your current permissions.");
        return;
      }
      if (response.status !== (kind === "create" ? 201 : 200)) throw Error("Unconfirmed outcome");
      const receipt = await response.json();
      if (!active.current) return;
      if (kind === "offboard" ? typeof receipt?.message !== "string" :
        typeof receipt?.id !== "string" || !/^[0-9a-f-]{36}$/i.test(receipt.id) || !Array.isArray(receipt.roles)) throw Error("Unconfirmed receipt");
      confirmed = true;
      if (kind === "create") { setEmail(""); setDisplayName(""); setPassword(""); }
      const snapshot = await read();
      if (!active.current) return;
      publish(snapshot);
      setMessage(success);
    } catch {
      if (!active.current) return;
      readRequired.current = true;
      setNeedsRefresh(true);
      setMessage(confirmed
        ? `${success} The staff directory could not be refreshed. Refresh and check it; do not repeat the change.`
        : "The staff change could not be confirmed. Refresh and check the directory before retrying; do not repeat the change automatically.");
    } finally {
      operation.current = false;
      if (active.current) setBusy(false);
    }
  }
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || operation.current || readRequired.current) return;
    await mutate(`/api/academies/${academy.id}/staff`, "POST",
      { email, displayName, password, role }, "Staff account created.", "create");
  }
  async function offboard(person: Staff) {
    if (!academy || operation.current || readRequired.current ||
      !window.confirm(`Offboard ${person.displayName}? Their active sessions will be invalidated.`)) return;
    await mutate(`/api/academies/${academy.id}/staff/${person.id}/offboard`, "POST",
      undefined, `${person.displayName} has been offboarded and active sessions were revoked.`, "offboard");
  }
  async function updateRole(person: Staff, nextRole: string) {
    if (!academy || operation.current || readRequired.current || person.roles.includes(nextRole)) return;
    await mutate(`/api/academies/${academy.id}/staff/${person.id}/role`, "PATCH",
      { role: nextRole }, `${person.displayName}'s access role was updated.`, "role");
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
          <p className="enterprise-page-state staff-message" role="status" aria-live="polite">{message}</p>
        )}
        <button type="button" className="enterprise-action-button" disabled={busy} onClick={refresh}>Refresh staff directory</button>
        <section className="staff-layout">
          <form onSubmit={create} className="staff-panel staff-create-panel">
            <header className="staff-panel-header">
              <div>
                <p>New account</p>
                <h2>Onboard staff</h2>
              </div>
            </header>
            <fieldset className="staff-fields" disabled={!academy || busy || needsRefresh} style={{ border: 0, margin: 0, minWidth: 0 }}>
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
                disabled={!academy || busy || needsRefresh}
                className="enterprise-action-button staff-create-button"
              >
                Create staff account
              </button>
            </fieldset>
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
                            updateRole(person, nextRole)
                          }
                          placeholder="Access role"
                          options={staffRoles.map((value) => ({
                            value,
                            label: roleLabel(value),
                          }))}
                          disabled={
                            !person.isActive || busy || needsRefresh
                          }
                        />
                      </label>
                    </div>
                    <div className="staff-person-actions">
                      {person.isActive ? (
                        <button
                          disabled={busy || needsRefresh}
                          onClick={() => offboard(person)}
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
