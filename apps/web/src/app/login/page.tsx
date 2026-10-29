"use client";

import { FormEvent, useRef, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { beginPortalSession } from "@/lib/api";
import { validWorkspaceAccount, workspaceLanding } from "@/lib/workspace-entry";

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5092";

export default function LoginPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [twoFactorRequired, setTwoFactorRequired] = useState(false);
  const [twoFactorCode, setTwoFactorCode] = useState("");
  const [recoveryMode, setRecoveryMode] = useState(false);
  const pending = useRef(false);
  async function login(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending.current) return;
    if (twoFactorRequired && !twoFactorCode.trim()) {
      setMessage("Enter your authenticator code or choose a recovery code.");
      return;
    }
    pending.current = true;
    setBusy(true);
    setMessage("");
    try {
      const email = userName.trim();
      const response = await fetch(
        `${apiUrl}/api/auth/login?useCookies=false`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email, password, ...(twoFactorRequired ? recoveryMode
            ? { twoFactorRecoveryCode: twoFactorCode.trim() }
            : { twoFactorCode: twoFactorCode.trim() } : {}) }),
        },
      );
      if (!response.ok) {
        // Only the framework's exact challenge is interpreted. Never show raw
        // server error details or save an incomplete authentication response.
        const problem: unknown = await response.json().catch(() => null);
        if (response.status === 401 && problem && typeof problem === "object" &&
          "detail" in problem && problem.detail === "RequiresTwoFactor") {
          setTwoFactorRequired(true);
          setMessage("Enter the code from your authenticator app, or use a recovery code.");
          return;
        }
        if (response.status === 429) {
          setMessage("Too many sign-in attempts. Wait before trying again. Your existing workspaces have not been changed.");
          return;
        }
        throw new Error();
      }
      const tokens = await response.json();
      if (!tokens || Array.isArray(tokens) || typeof tokens.accessToken !== "string" || !tokens.accessToken.trim() ||
        (tokens.refreshToken != null && (typeof tokens.refreshToken !== "string" || !tokens.refreshToken.trim()))) throw new Error();
      const profile = await fetch(`${apiUrl}/api/auth/session`, {
        headers: { Authorization: `Bearer ${tokens.accessToken}` },
      });
      if (!profile.ok) throw new Error();
      const session: unknown = await profile.json();
      if (!validWorkspaceAccount(session)) throw new Error();
      beginPortalSession(session.workspace, tokens.accessToken, tokens.refreshToken);
      setTwoFactorCode("");
      router.push(workspaceLanding(session, searchParams.get("returnTo")));
    } catch {
      setMessage(
        "Login failed. Check your user name, password and any verification code, or try again later.",
      );
    } finally {
      setBusy(false);
      pending.current = false;
    }
  }
  return (
    <main className="auth-shell">
      <form
        onSubmit={login}
        className="auth-card"
        aria-busy={busy}
      >
        <p className="auth-eyebrow">
          AcademyDesk
        </p>
        <h1>Sign in</h1>
        <p className="auth-intro">Your academy workspace, in one place.</p>
        {searchParams.get("passwordChanged") === "1" && (
          <p className="auth-intro" role="status">Password changed. Sign in with your new password.</p>
        )}
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
          disabled={busy}
        />
        <label className="auth-label" htmlFor="password">
          Password
        </label>
        <div className="auth-password-field">
          <input
            id="password"
            type={showPassword ? "text" : "password"}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="auth-input"
            required
            autoComplete="current-password"
            disabled={busy}
          />
          <button type="button" onClick={() => setShowPassword((current) => !current)} aria-label={showPassword ? "Hide password" : "Show password"} aria-pressed={showPassword}>
            {showPassword ? "Hide" : "Show"}
          </button>
        </div>
        {twoFactorRequired && <>
          <label className="auth-label" htmlFor="verification-code">{recoveryMode ? "Recovery code" : "Authenticator code"}</label>
          <input id="verification-code" className="auth-input" type="text" value={twoFactorCode}
            onChange={event => setTwoFactorCode(event.target.value)} required disabled={busy}
            inputMode={recoveryMode ? "text" : "numeric"} autoComplete="one-time-code" maxLength={128}
            aria-describedby="verification-guidance" />
          <p id="verification-guidance" className="auth-intro">{recoveryMode ? "Enter one unused recovery code. A successful sign-in consumes it." : "Enter the current code from your authenticator app."}</p>
          <button type="button" className="auth-challenge-action" disabled={busy} onClick={() => {
            setRecoveryMode(current => !current); setTwoFactorCode(""); setMessage("");
          }}>{recoveryMode ? "Use an authenticator code" : "Use a recovery code"}</button>
        </>}
        <button
          disabled={busy}
          className="auth-submit"
        >
          {busy ? "Signing in…" : "Sign in"}
        </button>
        {message && (
          <p className="auth-message" role="alert">
            {message}
          </p>
        )}
      </form>
    </main>
  );
}
