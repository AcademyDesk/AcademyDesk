"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Guardian = {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
};
type Link = {
  guardianId: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phone?: string | null;
  relationship?: string | null;
  isPrimary: boolean;
};

export default function GuardiansPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [guardians, setGuardians] = useState<Guardian[]>([]);
  const [studentId, setStudentId] = useState("");
  const [links, setLinks] = useState<Link[]>([]);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [guardianId, setGuardianId] = useState("");
  const [relationship, setRelationship] = useState("Parent");
  const [isPrimary, setIsPrimary] = useState(true);
  const [message, setMessage] = useState("Loading parent contacts…");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editFirstName, setEditFirstName] = useState("");
  const [editLastName, setEditLastName] = useState("");
  const [editEmail, setEditEmail] = useState("");
  const [editPhone, setEditPhone] = useState("");
  const [savingId, setSavingId] = useState<string | null>(null);
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const [studentResponse, guardianResponse] = await Promise.all([
      academyApi(`/api/academies/${academyId}/students`, { cache: "no-store" }),
      academyApi(`/api/academies/${academyId}/guardians`, {
        cache: "no-store",
      }),
    ]);
    if (!studentResponse.ok || !guardianResponse.ok) throw new Error();
    const studentData: Student[] = await studentResponse.json();
    setStudents(studentData);
    setGuardians(await guardianResponse.json());
    if (!studentId && studentData.length) setStudentId(studentData[0].id);
    setMessage("");
  }
  async function loadLinks(id: string) {
    if (!academy || !id) return setLinks([]);
    const response = await academyApi(
      `/api/academies/${academy.id}/students/${id}/guardians`,
      { cache: "no-store" },
    );
    if (!response.ok) throw new Error();
    setLinks(await response.json());
  }
  useEffect(() => {
    async function initialise() {
      try {
        const response = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (response.status === 401)
          return setMessage(
            "Please sign in before managing parent contacts.",
          );
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0])
          return setMessage("Create your academy and students first.");
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setMessage(
          "Parent contacts could not be loaded. Confirm the API is running on port 5092.",
        );
      }
    }
    void initialise();
  }, []);
  useEffect(() => {
    void loadLinks(studentId).catch(() =>
      setMessage("Student-parent links could not be loaded."),
    );
  }, [academy, studentId]);
  async function createGuardian(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/guardians`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          firstName,
          lastName,
          email: email || null,
          phone: phone || null,
        }),
      },
    );
    if (!response.ok)
      return setMessage("First name and last name are required.");
    setFirstName("");
    setLastName("");
    setEmail("");
    setPhone("");
    await load();
  }
  async function linkGuardian(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !studentId || !guardianId) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/students/${studentId}/guardians`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({ guardianId, relationship, isPrimary }),
      },
    );
    if (response.status === 409)
      return setMessage("This parent is already linked to this student.");
    if (!response.ok)
      return setMessage("The parent link could not be saved.");
    setGuardianId("");
    setMessage("");
    await loadLinks(studentId);
  }
  function beginEdit(guardian: Guardian) {
    setEditingId(guardian.id);
    setEditFirstName(guardian.firstName);
    setEditLastName(guardian.lastName);
    setEditEmail(guardian.email ?? "");
    setEditPhone(guardian.phone ?? "");
  }
  async function saveGuardian(guardian: Guardian) {
    if (!academy || !editFirstName.trim() || !editLastName.trim())
      return setMessage("First name and last name are required.");
    setSavingId(guardian.id);
    const response = await academyApi(
      `/api/academies/${academy.id}/guardians/${guardian.id}`,
      {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          firstName: editFirstName,
          lastName: editLastName,
          email: editEmail || null,
          phone: editPhone || null,
          isActive: guardian.isActive,
        }),
      },
    );
    setSavingId(null);
    if (!response.ok) return setMessage("The guardian could not be updated.");
    setEditingId(null);
    setMessage("Guardian updated.");
    await load();
  }
  async function toggleActive(guardian: Guardian) {
    if (!academy) return;
    setSavingId(guardian.id);
    const response = await academyApi(
      `/api/academies/${academy.id}/guardians/${guardian.id}`,
      {
        method: "PUT",
        headers: apiHeaders(true),
        body: JSON.stringify({
          firstName: guardian.firstName,
          lastName: guardian.lastName,
          email: guardian.email,
          phone: guardian.phone,
          isActive: !guardian.isActive,
        }),
      },
    );
    setSavingId(null);
    if (!response.ok)
      return setMessage("The guardian status could not be updated.");
    setMessage(
      guardian.isActive ? "Parent marked inactive." : "Parent reactivated.",
    );
    await load();
  }
  return (
    <main className="enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100">
      <WorkspaceNav />
      <div className="mx-auto max-w-6xl px-6 py-10">
        <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-300">
          Contacts
        </p>
        <h1 className="mt-3 text-4xl font-semibold tracking-tight">
          Parents
        </h1>
        {message && (
          <p className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-sm text-amber-100">
            {message}
          </p>
        )}
        <section className="mt-8 grid gap-6 lg:grid-cols-2">
          <form
            onSubmit={createGuardian}
            className="rounded-2xl border border-slate-800 bg-slate-900 p-6"
          >
            <h2 className="text-xl font-semibold">Add parent contact</h2>
            <div className="mt-5 grid gap-3 sm:grid-cols-2">
              <input
                value={firstName}
                onChange={(event) => setFirstName(event.target.value)}
                placeholder="First name"
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                required
              />
              <input
                value={lastName}
                onChange={(event) => setLastName(event.target.value)}
                placeholder="Last name"
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
                required
              />
            </div>
            <input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="Email (optional)"
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            />
            <input
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
              placeholder="Phone (optional)"
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            />
            <button
              disabled={!academy}
              className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 disabled:opacity-60"
            >
              Add parent
            </button>
          </form>
          <form
            onSubmit={linkGuardian}
            className="rounded-2xl border border-slate-800 bg-slate-900 p-6"
          >
            <h2 className="text-xl font-semibold">Link parent to student</h2>
            <select
              value={studentId}
              onChange={(event) => setStudentId(event.target.value)}
              className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            >
              <option value="">Select student</option>
              {students.map((student) => (
                <option key={student.id} value={student.id}>
                  {student.firstName} {student.lastName}
                </option>
              ))}
            </select>
            <select
              value={guardianId}
              onChange={(event) => setGuardianId(event.target.value)}
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            >
              <option value="">Select parent</option>
              {guardians.map((guardian) => (
                <option key={guardian.id} value={guardian.id}>
                  {guardian.firstName} {guardian.lastName}
                </option>
              ))}
            </select>
            <input
              value={relationship}
              onChange={(event) => setRelationship(event.target.value)}
              placeholder="Relationship, e.g. Parent"
              className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2"
            />
            <label className="mt-4 flex items-center gap-2 text-sm text-slate-300">
              <input
                type="checkbox"
                checked={isPrimary}
                onChange={(event) => setIsPrimary(event.target.checked)}
              />{" "}
              Primary contact for this student
            </label>
            <button
              disabled={!academy || !studentId || !guardianId}
              className="mt-5 w-full rounded-lg border border-cyan-400 px-4 py-2.5 font-semibold text-cyan-200 disabled:opacity-60"
            >
              Link parent
            </button>
          </form>
        </section>
        <section className="mt-6 rounded-2xl border border-slate-800 bg-slate-900 p-6">
          <h2 className="text-xl font-semibold">Parent directory</h2>
          {guardians.length === 0 ? (
            <p className="mt-5 text-slate-400">No parent contacts yet.</p>
          ) : (
            <ul className="mt-4 grid gap-3 md:grid-cols-2">
              {guardians.map((guardian) =>
                editingId === guardian.id ? (
                  <li
                    key={guardian.id}
                    className="rounded-lg border border-cyan-700/60 bg-slate-950 p-4"
                  >
                    <div className="grid gap-2 sm:grid-cols-2">
                      <input
                        value={editFirstName}
                        onChange={(event) =>
                          setEditFirstName(event.target.value)
                        }
                        className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                      />
                      <input
                        value={editLastName}
                        onChange={(event) =>
                          setEditLastName(event.target.value)
                        }
                        className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                      />
                      <input
                        type="email"
                        value={editEmail}
                        onChange={(event) => setEditEmail(event.target.value)}
                        placeholder="Email"
                        className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                      />
                      <input
                        value={editPhone}
                        onChange={(event) => setEditPhone(event.target.value)}
                        placeholder="Phone"
                        className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2"
                      />
                    </div>
                    <div className="mt-3 flex gap-2">
                      <Link
                        href={`/guardian-profile?guardianId=${guardian.id}`}
                        className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-cyan-300 hover:border-cyan-400"
                      >
                        Open record
                      </Link>
                      <button
                        onClick={() => void saveGuardian(guardian)}
                        disabled={savingId === guardian.id}
                        className="rounded-lg bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950 disabled:opacity-50"
                      >
                        {savingId === guardian.id ? "Saving…" : "Save"}
                      </button>
                      <button
                        onClick={() => setEditingId(null)}
                        className="rounded-lg border border-slate-700 px-3 py-2 text-sm"
                      >
                        Cancel
                      </button>
                    </div>
                  </li>
                ) : (
                  <li
                    key={guardian.id}
                    className="rounded-lg border border-slate-700 bg-slate-950 p-4"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <div className="font-medium">
                          {guardian.firstName} {guardian.lastName}
                        </div>
                        <div className="mt-1 text-sm text-slate-400">
                          {guardian.email ||
                            guardian.phone ||
                            "No contact details"}
                        </div>
                      </div>
                      <span
                        className={`rounded-full px-2 py-1 text-xs ${guardian.isActive ? "bg-emerald-950 text-emerald-300" : "bg-slate-800 text-slate-400"}`}
                      >
                        {guardian.isActive ? "Active" : "Inactive"}
                      </span>
                    </div>
                    <div className="mt-3 flex gap-2">
                      <button
                        onClick={() => beginEdit(guardian)}
                        className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm hover:border-cyan-400"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => void toggleActive(guardian)}
                        disabled={savingId === guardian.id}
                        className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm hover:border-amber-400 disabled:opacity-50"
                      >
                        {guardian.isActive ? "Deactivate" : "Reactivate"}
                      </button>
                    </div>
                  </li>
                ),
              )}
            </ul>
          )}
        </section>
        <section className="mt-6 rounded-2xl border border-slate-800 bg-slate-900 p-6">
          <h2 className="text-xl font-semibold">
            Contacts for selected student
          </h2>
          {links.length === 0 ? (
            <p className="mt-5 text-slate-400">
              No parent linked to this student.
            </p>
          ) : (
            <ul className="mt-4 space-y-3">
              {links.map((link) => (
                <li
                  key={link.guardianId}
                  className="rounded-lg border border-slate-700 bg-slate-950 p-4"
                >
                  <div className="font-medium">
                    {link.firstName} {link.lastName}
                    {link.isPrimary && (
                      <span className="ml-2 text-sm text-cyan-200">
                        Primary
                      </span>
                    )}
                  </div>
                  <div className="mt-1 text-sm text-slate-400">
                    {link.relationship || "Relationship not set"} ·{" "}
                    {link.email || link.phone || "No contact details"}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </main>
  );
}
