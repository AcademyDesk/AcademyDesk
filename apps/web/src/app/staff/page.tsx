"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Teacher = { id: string; firstName: string; lastName: string };
type Staff = {
  id: string;
  displayName: string;
  email: string;
  teacherId?: string | null;
  roles: string[];
  isActive: boolean;
};

export default function StaffPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [staff, setStaff] = useState<Staff[]>([]);
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState("Teacher");
  const [teacherId, setTeacherId] = useState("");
  const [message, setMessage] = useState("Loading staff accounts…");
  const [updatingStaffId, setUpdatingStaffId] = useState<string>();
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const [staffResponse, teacherResponse] = await Promise.all([
      academyApi(`/api/academies/${academyId}/staff`, { cache: "no-store" }),
      academyApi(`/api/academies/${academyId}/teachers`, { cache: "no-store" }),
    ]);
    if (staffResponse.status === 403)
      return setMessage("Only the academy owner can manage staff accounts.");
    if (!staffResponse.ok || !teacherResponse.ok) throw new Error();
    setStaff(await staffResponse.json());
    setTeachers(await teacherResponse.json());
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
        teacherId: role === "Teacher" && teacherId ? teacherId : null,
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
    setTeacherId("");
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
  const teacherName = (id?: string | null) => {
    const teacher = teachers.find((item) => item.id === id);
    return teacher ? `${teacher.firstName} ${teacher.lastName}` : "Not linked";
  };
  return (
    <main className="min-h-screen bg-slate-950 text-slate-100">
      <WorkspaceNav />
      <div className="mx-auto max-w-6xl px-6 py-10">
        <h1 className="text-4xl font-semibold tracking-tight">
          Staff accounts and roles
        </h1>
        {message && (
          <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">
            {message}
          </p>
        )}
        <section className="mt-8 grid gap-6 lg:grid-cols-[0.85fr_1.15fr]">
          <form
            onSubmit={create}
            className="rounded-2xl border border-slate-800 bg-slate-900 p-6"
          >
            <h2 className="text-xl font-semibold">Create staff account</h2>
            <input
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              placeholder="Display name"
              className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
              required
            />
            <input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="Work email"
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
              required
            />
            <input
              type="password"
              minLength={6}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="Temporary password"
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
              required
            />
            <select
              value={role}
              onChange={(event) => setRole(event.target.value)}
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            >
              <option>Teacher</option>
              <option>Manager</option>
              <option>FinanceUser</option>
              <option>FrontDesk</option>
            </select>
            {role === "Teacher" && (
              <select
                value={teacherId}
                onChange={(event) => setTeacherId(event.target.value)}
                className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
              >
                <option value="">No teaching profile linked yet</option>
                {teachers.map((teacher) => (
                  <option key={teacher.id} value={teacher.id}>
                    {teacher.firstName} {teacher.lastName}
                  </option>
                ))}
              </select>
            )}
            <p className="mt-3 text-xs text-slate-400">
              For now, give the temporary password to the staff member
              privately. Password-reset email delivery will be added with the
              email provider integration.
            </p>
            <button
              disabled={!academy}
              className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 disabled:opacity-60"
            >
              Create account
            </button>
          </form>
          <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
            <h2 className="text-xl font-semibold">Staff directory</h2>
            {staff.length === 0 ? (
              <p className="mt-6 text-slate-400">No staff accounts yet.</p>
            ) : (
              <ul className="mt-5 space-y-3">
                {staff.map((person) => (
                  <li
                    key={person.id}
                    className="rounded-lg border border-slate-700 bg-slate-950 p-4"
                  >
                    <div className="flex justify-between gap-3">
                      <div>
                        <div className="font-medium">{person.displayName}</div>
                        <div className="mt-1 text-sm text-slate-400">
                          {person.email}
                        </div>
                      </div>
                      <label className="text-right text-xs text-slate-400">
                        Access role
                        <select
                          value={person.roles[0] ?? "Teacher"}
                          disabled={
                            !person.isActive || updatingStaffId === person.id
                          }
                          onChange={(event) =>
                            void updateRole(person, event.target.value)
                          }
                          className="mt-1 block rounded border border-slate-700 bg-slate-900 px-2 py-1 text-sm text-cyan-200"
                        >
                          <option>Teacher</option>
                          <option>Manager</option>
                          <option>FinanceUser</option>
                          <option>FrontDesk</option>
                        </select>
                      </label>
                    </div>
                    {person.teacherId && (
                      <div className="mt-2 text-sm text-slate-300">
                        Teaching profile: {teacherName(person.teacherId)}
                      </div>
                    )}
                    <div className="mt-3">
                      {person.isActive ? (
                        <button
                          onClick={() => void offboard(person)}
                          className="text-sm text-rose-300"
                        >
                          Offboard and revoke sessions
                        </button>
                      ) : (
                        <span className="text-sm text-slate-500">
                          Offboarded
                        </span>
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
