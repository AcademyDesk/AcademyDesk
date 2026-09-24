"use client";
import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Guardian = { id: string; firstName: string; lastName: string };
type Teacher = { id: string; firstName: string; lastName: string };
export default function PortalAccounts() {
  const [a, setA] = useState<Academy>();
  const [s, setS] = useState<Student[]>([]);
  const [g, setG] = useState<Guardian[]>([]);
  const [t, setT] = useState<Teacher[]>([]);
  const [role, setRole] = useState("Student");
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [id, setId] = useState("");
  const [m, setM] = useState("Loading portal accounts…");
  useEffect(() => {
    void (async () => {
      try {
        const r = await academyApi("/api/academies");
        if (!r.ok) throw new Error();
        const x: Academy[] = await r.json();
        if (!x[0]) return setM("Create your academy first.");
        setA(x[0]);
        const [r1, r2, r3] = await Promise.all([
          academyApi(`/api/academies/${x[0].id}/students`),
          academyApi(`/api/academies/${x[0].id}/guardians`),
          academyApi(`/api/academies/${x[0].id}/teachers`),
        ]);
        setS(await r1.json());
        setG(await r2.json());
        setT(await r3.json());
        setM("");
      } catch {
        setM(
          "Portal accounts could not be loaded. Apply the portal migration and restart the API.",
        );
      }
    })();
  }, []);
  async function create(e: FormEvent) {
    e.preventDefault();
    if (!a) return;
    if (!id)
      return setM(
        `Select the ${role === "Guardian" ? "parent" : role.toLowerCase()} first.`,
      );
    const r = await academyApi(`/api/academies/${a.id}/portal-accounts`, {
      method: "POST",
      headers: apiHeaders(true),
      body: JSON.stringify({
        role,
        email,
        password,
        displayName,
        studentId: role === "Student" ? id : null,
        guardianId: role === "Guardian" ? id : null,
        teacherId: role === "Teacher" ? id : null,
      }),
    });
    const q = await r.json().catch(() => null);
    setM(
      r.ok
        ? `Created ${role === "Guardian" ? "parent" : role.toLowerCase()} portal account for ${email}.`
        : (q?.message ?? "Account could not be created."),
    );
    if (r.ok) {
      setEmail("");
      setDisplayName("");
      setPassword("");
      setId("");
    }
  }
  const choices = role === "Student" ? s : role === "Teacher" ? t : g;
  return (
    <main className="enterprise-settings portal-access-standard min-h-screen">
      <WorkspaceNav />
      <div className="portal-access-content mx-auto max-w-xl px-6 py-10">
        <header className="portal-access-heading">
          <div className="portal-access-title">
            <span className="portal-access-title-icon" aria-hidden="true">
              ◉
            </span>
            <div>
              <p>Operations &amp; workforce</p>
              <h1>Portal Access</h1>
            </div>
          </div>
        </header>
        {m && (
          <p className="enterprise-page-state portal-access-message">{m}</p>
        )}
        <form onSubmit={create} className="portal-access-panel">
          <header className="portal-access-panel-header">
            <div>
              <p>New account</p>
              <h2>Create portal account</h2>
            </div>
          </header>
          <div className="portal-access-fields">
            <StandardSelectField
              name="portal-role"
              value={role}
              onChange={(nextRole) => {
                setRole(nextRole);
                setId("");
              }}
              placeholder="Account type"
              options={[
                { value: "Student", label: "Student" },
                { value: "Guardian", label: "Parent" },
                { value: "Teacher", label: "Teacher" },
              ]}
            />
            <StandardSelectField
              name="portal-person"
              value={id}
              onChange={setId}
              placeholder={`Select ${role === "Guardian" ? "parent" : role.toLowerCase()}`}
              options={choices.map((x) => ({
                value: x.id,
                label: `${x.firstName} ${x.lastName}`,
              }))}
            />
            <label>
              <span>Display name</span>
              <input
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder="Full name"
                required
              />
            </label>
            <label>
              <span>Portal email</span>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="name@example.com"
                required
              />
            </label>
            <label>
              <span>Temporary password</span>
              <input
                type="password"
                minLength={6}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Minimum 6 characters"
                required
              />
            </label>
            <button className="enterprise-action-button portal-access-action">
              Create portal account
            </button>
          </div>
        </form>
      </div>
    </main>
  );
}
