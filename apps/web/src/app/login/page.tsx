"use client";

import Link from "next/link";
import { FormEvent, useState } from "react";

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export default function LoginPage() {
  const [email, setEmail] = useState(""); const [password, setPassword] = useState(""); const [message, setMessage] = useState(""); const [busy, setBusy] = useState(false);
  async function login(event: FormEvent<HTMLFormElement>) { event.preventDefault(); setBusy(true); setMessage(""); try { const response = await fetch(`${apiUrl}/api/auth/login`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email, password }) }); if (!response.ok) throw new Error(); const session = await response.json(); localStorage.setItem("academydesk.accessToken", session.accessToken); localStorage.setItem("academydesk.refreshToken", session.refreshToken); setMessage("Login successful. Your AcademyDesk session is ready."); } catch { setMessage("Login failed. Check the email and password, then try again."); } finally { setBusy(false); } }
  return <main className="flex min-h-screen items-center justify-center bg-slate-950 px-6 text-slate-100"><form onSubmit={login} className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-7 shadow-xl"><Link href="/" className="text-sm text-cyan-300">← AcademyDesk</Link><h1 className="mt-6 text-3xl font-semibold">Sign in</h1><p className="mt-2 text-slate-400">Access your academy workspace.</p><label className="mt-7 block text-sm text-slate-300" htmlFor="email">Email</label><input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><label className="mt-4 block text-sm text-slate-300" htmlFor="password">Password</label><input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2" required /><button disabled={busy} className="mt-6 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60">{busy ? "Signing in…" : "Sign in"}</button>{message && <p className="mt-4 rounded-lg border border-slate-700 p-3 text-sm text-slate-300">{message}</p>}</form></main>;
}
