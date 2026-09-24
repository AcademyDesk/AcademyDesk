"use client";

import { useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { academyApi } from "@/lib/api";

type Academy = { id: string };
type Scheme = {
  id: string;
  name: string;
  passingPercent: number;
  isActive: boolean;
};
export default function AssessmentGovernance() {
  const [schemes, setSchemes] = useState<Scheme[]>([]);
  const [message, setMessage] = useState("Loading assessment policy…");
  useEffect(() => {
    void (async () => {
      try {
        const academies: Academy[] = await (
          await academyApi("/api/academies")
        ).json();
        if (!academies[0]) throw Error();
        const response = await academyApi(
          `/api/academies/${academies[0].id}/academic-governance/grading-schemes`,
        );
        if (!response.ok) throw Error();
        setSchemes(await response.json());
        setMessage("");
      } catch {
        setMessage("Assessment policy could not be loaded.");
      }
    })();
  }, []);
  return (
    <main className="enterprise-settings governance-standard policy-standard min-h-screen">
      <WorkspaceNav />
      <div className="governance-content mx-auto max-w-5xl px-6 py-10">
        <header className="governance-heading">
          <div className="governance-title">
            <span className="governance-title-icon" aria-hidden="true">
              ♫
            </span>
            <div>
              <p>Academics</p>
              <h1>Assessment Policy</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state governance-message">{message}</p>
        )}
        <section className="governance-panel policy-panel">
          <header className="governance-panel-header">
            <div>
              <p>Assessment policy</p>
              <h2>Available grading schemes</h2>
            </div>
            <span>
              {schemes.filter((scheme) => scheme.isActive).length} active
            </span>
          </header>
          {schemes.length === 0 ? (
            <p className="governance-empty">No grading schemes configured.</p>
          ) : (
            <ul>
              {schemes.map((scheme) => (
                <li key={scheme.id}>
                  <div>
                    <b>{scheme.name}</b>
                    <small>
                      {scheme.isActive
                        ? "Active for assessment selection"
                        : "Inactive"}
                    </small>
                  </div>
                  <span>Pass: {scheme.passingPercent}%</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </main>
  );
}
