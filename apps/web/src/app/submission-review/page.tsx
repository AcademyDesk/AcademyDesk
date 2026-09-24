"use client";

import { useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Submission = {
  id: string;
  status: string;
  responseText?: string | null;
  teacherFeedback?: string | null;
};

export default function SubmissionReviewPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [message, setMessage] = useState("Loading submission review…");
  useEffect(() => {
    void (async () => {
      try {
        const academyResponse = await academyApi("/api/academies");
        const academies: Academy[] = await academyResponse.json();
        if (!academyResponse.ok || !academies[0]) throw new Error();
        setAcademy(academies[0]);
        const response = await academyApi(
          `/api/academies/${academies[0].id}/assignment-submissions`,
        );
        if (!response.ok) throw new Error();
        setSubmissions(await response.json());
        setMessage("");
      } catch {
        setMessage("Submission review could not be loaded.");
      }
    })();
  }, []);
  async function review(id: string) {
    const feedback = window.prompt("Teacher feedback:");
    if (feedback === null || !academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/assignment-submissions/${id}/review`,
      {
        method: "PATCH",
        headers: apiHeaders(true),
        body: JSON.stringify({ feedback }),
      },
    );
    if (!response.ok)
      return setMessage("Submission review could not be saved.");
    const updated = await response.json();
    setSubmissions((items) =>
      items.map((item) => (item.id === id ? updated : item)),
    );
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
          <p className="enterprise-page-state review-message">{message}</p>
        )}
        <section className="review-panel">
          <header className="review-panel-header">
            <div>
              <p>Learning review</p>
              <h2>Assignment submissions</h2>
            </div>
            <span>{pending} pending</span>
          </header>
          {submissions.length === 0 ? (
            <p className="review-empty">No assignment submissions to review.</p>
          ) : (
            <ul>
              {submissions.map((submission) => (
                <li key={submission.id}>
                  <div>
                    <b>{submission.status}</b>
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
                      onClick={() => void review(submission.id)}
                    >
                      Review
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
