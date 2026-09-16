"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";

type Academy = { id: string; name: string };
type Student = { id: string; firstName: string; lastName: string; email?: string | null; phone?: string | null };
const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export default function StudentsPage() {
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [academyId, setAcademyId] = useState("");
  const [students, setStudents] = useState<Student[]>([]);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [message, setMessage] = useState("");

  async function loadAcademies() {
    const response = await fetch(`${apiUrl}/api/academies`, { cache: "no-store" });
    if (!response.ok) throw new Error();
    const data: Academy[] = await response.json(); setAcademies(data);
    if (!academyId && data.length) setAcademyId(data[0].id);
  }
  async function loadStudents(id: string) {
    if (!id) return setStudents([]);
    const response = await fetch(`${apiUrl}/api/academies/${id}/students`, { cache: "no-store" });
    if (!response.ok) throw new Error(); setStudents(await response.json());
  }
  useEffect(() => { void loadAcademies().catch(() => setMessage("The AcademyDesk API is not reachable.")); }, []);
  useEffect(() => { void loadStudents(academyId).catch(() => setMessage("Students could not be loaded.")); }, [academyId]);
  async function createStudent(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academyId) return;
    const response = await fetch(`${apiUrl}/api/academies/${academyId}/students`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ firstName, lastName, email, phone }) });
    if (!response.ok) return setMessage("The student could not be saved.");
    setFirstName(""); setLastName(""); setEmail(""); setPhone(""); setMessage(""); await loadStudents(academyId);
  }
  return <main className="min-h-screen bg-slate-950 px-6 py-12 text-slate-100"><div className="mx-auto max-w-5xl"><Link href="/" className="text-sm text-cyan-300">← Academy setup</Link><h1 className="mt-4 text-4xl font-semibold">Students</h1><p className="mt-3 text-slate-300">Manage learners enrolled in your academy.</p>{message && <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-200">{message}</p>}{academies.length === 0 ? <p className="mt-8 rounded-lg border border-dashed border-slate-700 p-8 text-center text-slate-400">Create an academy first.</p> : <><label className="mt-8 block text-sm text-slate-300" htmlFor="academy">Academy</label><select id="academy" value={academyId} onChange={(e) => setAcademyId(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2">{academies.map((a) => <option key={a.id} value={a.id}>{a.name}</option>)}</select><section className="mt-6 grid gap-6 md:grid-cols-2"><form onSubmit={createStudent} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Add student</h2><input value={firstName} onChange={(e) => setFirstName(e.target.value)} placeholder="First name" className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><input value={lastName} onChange={(e) => setLastName(e.target.value)} placeholder="Last name" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email (optional)" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="Phone (optional)" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><button className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300">Create student</button></form><section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Student roster</h2>{students.length === 0 ? <p className="mt-6 text-slate-400">No students yet.</p> : <ul className="mt-4 space-y-3">{students.map((s) => <li key={s.id} className="rounded-lg border border-slate-700 p-4"><div className="font-medium">{s.firstName} {s.lastName}</div><div className="mt-1 text-sm text-slate-400">{s.email || s.phone || "No contact details"}</div></li>)}</ul>}</section></section></>}</div></main>;
}
