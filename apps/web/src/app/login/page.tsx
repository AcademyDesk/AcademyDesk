"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { ThemeToggle } from "@/components/theme-toggle";

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export default function LoginPage() {
  const router = useRouter();
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  async function login(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setMessage("");
    try {
      const email = userName.includes("@")
        ? userName
        : `${userName.trim()}@academydesk.local`;
      const response = await fetch(
        `${apiUrl}/api/auth/login?useCookies=false`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email, password }),
        },
      );
      if (!response.ok) throw new Error();
      const tokens = await response.json();
      localStorage.setItem("academydesk.accessToken", tokens.accessToken);
      localStorage.setItem("academydesk.refreshToken", tokens.refreshToken);
      const profile = await fetch(`${apiUrl}/api/auth/session`, {
        headers: { Authorization: `Bearer ${tokens.accessToken}` },
      });
      const session = profile.ok ? await profile.json() : null;
      router.push(
        session?.workspace === "Platform"
          ? "/platform"
          : session?.workspace === "Teacher"
            ? "/teacher"
            : session?.workspace === "Portal"
              ? "/portal"
              : "/dashboard",
      );
    } catch {
      setMessage(
        "Login failed. Check the user name and password, then try again.",
      );
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="auth-shell">
      <div className="auth-theme">
        <ThemeToggle />
      </div>
      <form
        onSubmit={login}
        className="auth-card"
      >
        <p className="auth-eyebrow">
          AcademyDesk
        </p>
        <h1>Sign in</h1>
        <p className="auth-intro">Your academy workspace, in one place.</p>
        <label className="auth-label auth-label-first" htmlFor="username">
          User name
        </label>
        <input
          id="username"
          value={userName}
          onChange={(e) => setUserName(e.target.value)}
          className="auth-input"
          required
          autoComplete="username"
        />
        <label className="auth-label" htmlFor="password">
          Password
        </label>
        <input
          id="password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="auth-input"
          required
          autoComplete="current-password"
        />
        <button
          disabled={busy}
          className="auth-submit"
        >
          {busy ? "Signing in…" : "Sign in"}
        </button>
        {message && (
          <p className="auth-message">
            {message}
          </p>
        )}
      </form>
    </main>
  );
}
