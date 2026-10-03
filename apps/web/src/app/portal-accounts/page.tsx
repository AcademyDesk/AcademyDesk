"use client";
import { FormEvent, useEffect, useRef, useState } from "react";
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
  const [saving, setSaving] = useState(false);
  const submitting = useRef(false);
  const [messageTone, setMessageTone] = useState<"success" | "error" | "neutral">("neutral");
  useEffect(() => {
    void (async () => {
      try {
        const r = await academyApi("/api/academies");
        if (!r.ok) throw new Error();
        const x: Academy[] = await r.json();
        if (!x[0]) return setM("Create your academy first.");
        const [r1, r2, r3] = await Promise.all([
          academyApi(`/api/academies/${x[0].id}/students`),
          academyApi(`/api/academies/${x[0].id}/guardians`),
          academyApi(`/api/academies/${x[0].id}/teachers`),
        ]);
        if (!r1.ok || !r2.ok || !r3.ok) throw new Error();
        const [students, guardians, teachers] = await Promise.all([r1.json(), r2.json(), r3.json()]);
        if (![students, guardians, teachers].every(Array.isArray)) throw new Error();
        setS(students);
        setG(guardians);
        setT(teachers);
        setA(x[0]);
        setM("");
      } catch {
        setMessageTone("error");
        setM(
          "Portal accounts could not be loaded. Check your connection and access, then refresh the page.",
        );
      }
    })();
  }, []);
  async function create(e: FormEvent) {
    e.preventDefault();
    if (!a || submitting.current) return;
    if (!id) {
      setMessageTone("error");
      return setM(
        `Select the ${role === "Guardian" ? "parent" : role.toLowerCase()} first.`,
      );
    }
    submitting.current = true;
    setSaving(true);
    setM("");
    try {
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
    setMessageTone(r.ok ? "success" : "error");
    setM(
      r.ok
        ? `Created ${role === "Guardian" ? "parent" : role.toLowerCase()} portal account for ${email}.`
        : (q?.message ?? (r.status === 401 ? "Your session has expired. Sign in again before creating an account." : r.status === 403 ? "You do not have access to create portal accounts." : "Account could not be created.")),
    );
    if (r.ok) {
      setEmail("");
      setDisplayName("");
      setPassword("");
      setId("");
    }
    } catch {
      // A lost response does not prove rollback. Keep the draft; never replay automatically.
      setMessageTone("error");
      setM("The result could not be confirmed. Check whether this login already exists before trying again. Your entries have been kept.");
    } finally {
      submitting.current = false;
      setSaving(false);
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
          <p className="enterprise-page-state portal-access-message" role={messageTone === "error" ? "alert" : "status"} data-tone={messageTone}>{m}</p>
        )}
        <form onSubmit={create} className="portal-access-panel">
          <header className="portal-access-panel-header">
            <div>
              <p>New account</p>
              <h2>Create portal account</h2>
            </div>
          </header>
          <fieldset className="portal-access-fields" disabled={saving || !a} style={{ border: 0, margin: 0, minWidth: 0 }} aria-busy={saving}>
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
              {saving ? "Creating account…" : "Create portal account"}
            </button>
          </fieldset>
        </form>
      </div>
    </main>
  );
}
