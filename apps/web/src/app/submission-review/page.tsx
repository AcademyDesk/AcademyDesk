"use client";

import { useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Submission = {
  id: string;
  studentId: string;
  assignmentId: string;
  studentName?: string | null;
  studentNumber?: string | null;
  assignmentTitle?: string | null;
  batchName?: string | null;
  submittedAtUtc: string;
  status: string;
  responseText?: string | null;
  teacherFeedback?: string | null;
};

function isSubmission(value: unknown): value is Submission {
  if (!value || typeof value !== "object") return false;
  const row = value as Record<string, unknown>;
  return ["id", "studentId", "assignmentId", "status", "submittedAtUtc"].every(
    (key) => typeof row[key] === "string" && row[key] !== "",
  ) && ["studentName", "studentNumber", "assignmentTitle", "batchName", "responseText", "teacherFeedback"].every(
    (key) => row[key] == null || typeof row[key] === "string",
  );
}

function identity(submission: Submission) {
  return `${submission.studentName || "Unavailable student"} — ${submission.assignmentTitle || "Unavailable assignment"} (submission ${submission.id})`;
}

function submittedTime(value: string) {
  // SQL DateTime values may serialize without a suffix; this field is explicitly UTC.
  const normalized = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`;
  if (!Number.isFinite(Date.parse(normalized))) return "Submission time unavailable";
  const iso = new Date(normalized).toISOString();
  return <time dateTime={iso}>Submitted: {iso.replace("T", " ").replace(/(?:\.\d+)?Z$/, " UTC")}</time>;
}

export default function SubmissionReviewPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [message, setMessage] = useState("Loading submission review…");
  const [loaded, setLoaded] = useState(false);
  const [savingId, setSavingId] = useState<string>();
  const saving = useRef(false);
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const academyResponse = await academyApi("/api/academies");
        const academies: Academy[] = await academyResponse.json();
        if (!academyResponse.ok || !Array.isArray(academies) || typeof academies[0]?.id !== "string") throw new Error();
        if (!active) return;
        setAcademy(academies[0]);
        const response = await academyApi(
          `/api/academies/${academies[0].id}/assignment-submissions`,
        );
        if (!response.ok) throw new Error();
        const rows: unknown = await response.json();
        if (!Array.isArray(rows) || !rows.every(isSubmission)) throw new Error();
        if (!active) return;
        setSubmissions(rows);
        setLoaded(true);
        setMessage("");
      } catch {
        if (active) setMessage("Submission review could not be loaded.");
      }
    })();
    return () => { active = false; };
  }, []);
  async function review(submission: Submission) {
    if (!academy || saving.current) return;
    const feedback = window.prompt(`Teacher feedback for ${identity(submission)}:\nStudent ID: ${submission.studentId}\nAssignment ID: ${submission.assignmentId}\nBatch: ${submission.batchName || "Unavailable"}`);
    if (feedback === null) return;
    saving.current = true;
    setSavingId(submission.id);
    setMessage("");
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/assignment-submissions/${submission.id}/review`,
        {
          method: "PATCH",
          headers: apiHeaders(true),
          body: JSON.stringify({ feedback }),
        },
      );
      if (!response.ok) throw new Error();
      const updated: unknown = await response.json();
      if (!isSubmission(updated) || updated.id !== submission.id || updated.studentId !== submission.studentId || updated.assignmentId !== submission.assignmentId || updated.status !== "Reviewed" || updated.teacherFeedback !== feedback) throw new Error();
      setSubmissions((items) => items.map((item) => (item.id === submission.id ? updated : item)));
      setMessage(`Review saved for ${identity(updated)}.`);
    } catch {
      setMessage("Submission review could not be saved. Check the saved record before retrying.");
    } finally {
      saving.current = false;
      setSavingId(undefined);
    }
  }
  const pending = submissions.filter(
    (submission) => submission.status !== "Reviewed",
  ).length;
  return (
    <main className="enterprise-settings review-standard min-h-screen">
      <WorkspaceNav />
      <div className="review-content mx-auto max-w-5xl px-6 py-10">
        <header className="review-heading">
          <div className="review-title">
            <span className="review-title-icon" aria-hidden="true">
              ♫
            </span>
            <div>
              <p>Academics</p>
              <h1>Submission Review</h1>
            </div>
          </div>
        </header>
        {message && (
          <p role="status" aria-live="polite" className="enterprise-page-state review-message">{message}</p>
        )}
        <section className="review-panel">
          <header className="review-panel-header">
            <div>
              <p>Learning review</p>
              <h2>Assignment submissions</h2>
            </div>
            <span>{pending} pending</span>
          </header>
          {!loaded ? null : submissions.length === 0 ? (
            <p className="review-empty">No assignment submissions to review.</p>
          ) : (
            <ul>
              {submissions.map((submission) => (
                <li key={submission.id}>
                  <div className="min-w-0 [overflow-wrap:anywhere]">
                    <b>{submission.studentName || "Unavailable student"}{submission.studentNumber ? ` · ${submission.studentNumber}` : ""}</b>
                    <p><strong>Assignment:</strong> {submission.assignmentTitle || "Unavailable assignment"}</p>
                    <p><strong>Batch:</strong> {submission.batchName || "Unavailable"} · <strong>Status:</strong> {submission.status}</p>
                    <small>Student ID: {submission.studentId} · Assignment ID: {submission.assignmentId}</small>
                    <small>Submission ID: {submission.id}</small>
                    <small>{submittedTime(submission.submittedAtUtc)}</small>
                    <p>{submission.responseText || "No written response"}</p>
                    {submission.status === "Reviewed" && (
                      <small>
                        Feedback:{" "}
                        {submission.teacherFeedback || "No feedback added"}
                      </small>
                    )}
                  </div>
                  {submission.status !== "Reviewed" && (
                    <button
                      type="button"
                      disabled={savingId !== undefined}
                      aria-label={`Review ${identity(submission)} · student ${submission.studentId} · assignment ${submission.assignmentId}`}
                      onClick={() => review(submission)}
                    >
                      {savingId === submission.id ? "Saving…" : "Review"}
                    </button>
                  )}
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </main>
  );
}
