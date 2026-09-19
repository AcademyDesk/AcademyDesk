"use client";
import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
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
    <main className="min-h-screen bg-slate-950 text-slate-100">
      <WorkspaceNav />
      <div className="mx-auto max-w-xl px-6 py-10">
        <p className="text-sm font-semibold uppercase tracking-[.22em] text-cyan-300">
          Administration
        </p>
        <h1 className="mt-3 text-4xl font-semibold">Portal accounts</h1>
        {m && (
          <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">
            {m}
          </p>
        )}
        <form
          onSubmit={create}
          className="mt-8 rounded-2xl border border-slate-800 bg-slate-900 p-6"
        >
          <select
            value={role}
            onChange={(e) => {
              setRole(e.target.value);
              setId("");
            }}
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
          >
            <option>Student</option>
            <option value="Guardian">Parent</option>
            <option>Teacher</option>
          </select>
          <select
            value={id}
            onChange={(e) => setId(e.target.value)}
            className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            required
          >
            <option value="">Select {role === "Guardian" ? "parent" : role.toLowerCase()}</option>
            {choices.map((x) => (
              <option key={x.id} value={x.id}>
                {x.firstName} {x.lastName}
              </option>
            ))}
          </select>
          <input
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            placeholder="Display name"
            className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            required
          />
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="Portal email"
            className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            required
          />
          <input
            type="password"
            minLength={6}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Temporary password"
            className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            required
          />
          <button className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950">
            Create portal account
          </button>
        </form>
      </div>
    </main>
  );
}
