"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { WorkspaceNav } from "@/components/workspace-nav";

type Academy = { id: string; name: string };
type Student = { id: string; firstName: string; lastName: string; email?: string | null; phone?: string | null; isActive: boolean };

export default function StudentsPage() {
  const [academies, setAcademies] = useState<Academy[]>([]);
  const [academyId, setAcademyId] = useState("");
  const [students, setStudents] = useState<Student[]>([]);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [message, setMessage] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editFirstName, setEditFirstName] = useState("");
  const [editLastName, setEditLastName] = useState("");
  const [editEmail, setEditEmail] = useState("");
  const [editPhone, setEditPhone] = useState("");
  const [savingId, setSavingId] = useState<string | null>(null);

  async function loadAcademies() {
    const response = await academyApi("/api/academies", { cache: "no-store" });
    if (!response.ok) throw new Error();
    const data: Academy[] = await response.json(); setAcademies(data);
    if (!academyId && data.length) setAcademyId(data[0].id);
  }
  async function loadStudents(id: string) {
    if (!id) return setStudents([]);
    const response = await academyApi(`/api/academies/${id}/students`, { cache: "no-store" });
    if (!response.ok) throw new Error(); setStudents(await response.json());
  }
  useEffect(() => { void loadAcademies().catch(() => setMessage("The AcademyDesk API is not reachable.")); }, []);
  useEffect(() => { void loadStudents(academyId).catch(() => setMessage("Students could not be loaded.")); }, [academyId]);
  async function createStudent(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academyId) return;
    const response = await academyApi(`/api/academies/${academyId}/students`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ firstName, lastName, email, phone }) });
    if (!response.ok) return setMessage("The student could not be saved.");
    setFirstName(""); setLastName(""); setEmail(""); setPhone(""); setMessage(""); await loadStudents(academyId);
  }
  function beginEdit(student: Student) {
    setEditingId(student.id); setEditFirstName(student.firstName); setEditLastName(student.lastName); setEditEmail(student.email ?? ""); setEditPhone(student.phone ?? ""); setMessage("");
  }
  async function saveStudent(student: Student) {
    if (!academyId || !editFirstName.trim() || !editLastName.trim()) return setMessage("First name and last name are required.");
    setSavingId(student.id);
    const response = await academyApi(`/api/academies/${academyId}/students/${student.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ firstName: editFirstName, lastName: editLastName, email: editEmail, phone: editPhone, isActive: student.isActive }) });
    setSavingId(null);
    if (!response.ok) return setMessage("The student could not be updated.");
    setEditingId(null); setMessage("Student updated."); await loadStudents(academyId);
  }
  async function toggleActive(student: Student) {
    if (!academyId) return;
    setSavingId(student.id);
    const response = await academyApi(`/api/academies/${academyId}/students/${student.id}`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ firstName: student.firstName, lastName: student.lastName, email: student.email, phone: student.phone, isActive: !student.isActive }) });
    setSavingId(null);
    if (!response.ok) return setMessage("The student status could not be updated.");
    setMessage(student.isActive ? "Student marked inactive." : "Student reactivated."); await loadStudents(academyId);
  }
  return <><WorkspaceNav /><main className="min-h-screen bg-slate-950 px-6 py-12 text-slate-100"><div className="mx-auto max-w-5xl"><Link href="/" className="text-sm text-cyan-300">← Academy setup</Link><h1 className="mt-4 text-4xl font-semibold">Students</h1><p className="mt-3 text-slate-300">Manage learners enrolled in your academy.</p>{message && <p className="mt-5 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-200">{message}</p>}{academies.length === 0 ? <p className="mt-8 rounded-lg border border-dashed border-slate-700 p-8 text-center text-slate-400">Create an academy first.</p> : <><label className="mt-8 block text-sm text-slate-300" htmlFor="academy">Academy</label><select id="academy" value={academyId} onChange={(e) => setAcademyId(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2">{academies.map((a) => <option key={a.id} value={a.id}>{a.name}</option>)}</select><section className="mt-6 grid gap-6 md:grid-cols-2"><form onSubmit={createStudent} className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Add student</h2><input value={firstName} onChange={(e) => setFirstName(e.target.value)} placeholder="First name" className="mt-5 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><input value={lastName} onChange={(e) => setLastName(e.target.value)} placeholder="Last name" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email (optional)" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="Phone (optional)" className="mt-3 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" /><button className="mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300">Create student</button></form><section className="rounded-2xl border border-slate-800 bg-slate-900 p-6"><h2 className="text-xl font-semibold">Student roster</h2>{students.length === 0 ? <p className="mt-6 text-slate-400">No active students yet.</p> : <ul className="mt-4 space-y-3">{students.map((s) => editingId === s.id ? <li key={s.id} className="rounded-lg border border-cyan-700/60 bg-slate-950 p-4"><div className="grid gap-2 sm:grid-cols-2"><input value={editFirstName} onChange={(e) => setEditFirstName(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2" /><input value={editLastName} onChange={(e) => setEditLastName(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2" /><input type="email" value={editEmail} onChange={(e) => setEditEmail(e.target.value)} placeholder="Email" className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2" /><input value={editPhone} onChange={(e) => setEditPhone(e.target.value)} placeholder="Phone" className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2" /></div><div className="mt-3 flex gap-2"><button onClick={() => void saveStudent(s)} disabled={savingId === s.id} className="rounded-lg bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950 disabled:opacity-50">{savingId === s.id ? "Saving…" : "Save"}</button><button onClick={() => setEditingId(null)} className="rounded-lg border border-slate-700 px-3 py-2 text-sm">Cancel</button></div></li> : <li key={s.id} className="rounded-lg border border-slate-700 p-4"><div className="flex items-start justify-between gap-3"><div><div className="font-medium">{s.firstName} {s.lastName}</div><div className="mt-1 text-sm text-slate-400">{s.email || s.phone || "No contact details"}</div></div><span className={`rounded-full px-2 py-1 text-xs ${s.isActive ? "bg-emerald-950 text-emerald-300" : "bg-slate-800 text-slate-400"}`}>{s.isActive ? "Active" : "Inactive"}</span></div><div className="mt-3 flex gap-2"><button onClick={() => beginEdit(s)} className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm hover:border-cyan-400">Edit</button><button onClick={() => void toggleActive(s)} disabled={savingId === s.id} className="rounded-lg border border-slate-700 px-3 py-1.5 text-sm hover:border-amber-400 disabled:opacity-50">{s.isActive ? "Deactivate" : "Reactivate"}</button></div></li>)}</ul>}</section></section></>}</div></main></>;
}
