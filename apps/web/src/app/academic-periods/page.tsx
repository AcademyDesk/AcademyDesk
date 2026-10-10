"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import {
  StandardDateField,
  StandardSelectField,
} from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Year = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  isClosed: boolean;
};
type Term = {
  id: string;
  academicYearId: string;
  name: string;
  startDate: string;
  endDate: string;
  isClosed: boolean;
};

async function fetchPeriods(id: string) {
  const response = await academyApi(`/api/academies/${id}/academic-periods`, { cache: "no-store" });
  if (!response.ok) throw new Error();
  const data = await response.json();
  if (!Array.isArray(data?.years) || !Array.isArray(data?.terms)) throw new Error();
  return data as { years: Year[]; terms: Term[] };
}

export default function AcademicPeriods() {
  const [academy, setAcademy] = useState<Academy>();
  const [years, setYears] = useState<Year[]>([]);
  const [terms, setTerms] = useState<Term[]>([]);
  const [message, setMessage] = useState("Loading academic governance…");
  const [yearStart, setYearStart] = useState("");
  const [yearEnd, setYearEnd] = useState("");
  const [termYearId, setTermYearId] = useState("");
  const [termStart, setTermStart] = useState("");
  const [termEnd, setTermEnd] = useState("");
  const [saving, setSaving] = useState(false);
  const pending = useRef(false);
  function applyPeriods(data: Awaited<ReturnType<typeof fetchPeriods>>) {
      setYears(data.years);
      setTerms(data.terms);
  }
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const rows: Academy[] = await response.json();
        if (!Array.isArray(rows) || !rows[0]?.id) throw new Error();
        const data = await fetchPeriods(rows[0].id);
        if (!active) return;
        applyPeriods(data);
        setAcademy(rows[0]);
        setMessage("");
      } catch {
        if (active) setMessage("Academic periods could not be loaded. Please refresh or check your access.");
      }
    })();
    return () => { active = false; };
  }, []);
  async function mutate(request: () => Promise<Response>, success: string, reset?: () => void) {
    if (!academy || pending.current) return;
    pending.current = true;
    setSaving(true);
    setMessage("");
    try {
      const response = await request();
      if (!response.ok) {
        if (response.status >= 500) throw new Error();
        const payload = await response.json().catch(() => null);
        setMessage(typeof payload?.message === "string" ? payload.message : "Period change was not accepted. Your draft has been retained.");
        return;
      }
      reset?.();
      setMessage(success);
      try { applyPeriods(await fetchPeriods(academy.id)); }
      catch { setMessage(`${success} The register could not be refreshed. Do not repeat the action; refresh to check the saved record.`); }
    } catch {
      setMessage("The result could not be confirmed. Your draft has been retained. Check the register before trying again.");
    } finally {
      pending.current = false;
      setSaving(false);
    }
  }
  async function save(
    event: FormEvent<HTMLFormElement>,
    kind: "years" | "terms",
  ) {
    event.preventDefault();
    if (!academy || pending.current) return;
    const element = event.currentTarget;
    const form = new FormData(element);
    if (kind === "years" && (!yearStart || !yearEnd))
      return setMessage("Select the year start and end dates.");
    if (kind === "terms" && (!termYearId || !termStart || !termEnd))
      return setMessage("Select an academic year and term dates.");
    const body =
      kind === "years"
        ? {
            name: form.get("name"),
            startDate: yearStart,
            endDate: yearEnd,
            isCurrent: form.get("isCurrent") === "on",
          }
        : {
            academicYearId: termYearId,
            name: form.get("name"),
            startDate: termStart,
            endDate: termEnd,
          };
    await mutate(() => academyApi(
      `/api/academies/${academy.id}/academic-periods/${kind}`,
      { method: "POST", headers: apiHeaders(true), body: JSON.stringify(body) },
    ), `${kind === "years" ? "Academic year" : "Term"} created.`, () => {
    element.reset();
    if (kind === "years") {
      setYearStart("");
      setYearEnd("");
    } else {
      setTermYearId("");
      setTermStart("");
      setTermEnd("");
    }
    });
  }
  async function close(kind: "years" | "terms", id: string) {
    if (!academy || pending.current) return;
    await mutate(() => academyApi(
      `/api/academies/${academy.id}/academic-periods/${kind}/${id}/close`,
      { method: "PATCH", headers: apiHeaders(true) },
    ), "Period closed and retained for audit.");
  }
  const formatDate = (value: string) =>
    new Intl.DateTimeFormat("en-IN", {
      dateStyle: "medium",
      timeZone: "Asia/Kolkata",
    }).format(new Date(`${value}T12:00:00`));
  return (
    <main className="enterprise-settings periods-standard min-h-screen">
      <WorkspaceNav />
      <div className="periods-content mx-auto max-w-6xl px-6 py-10">
        <header className="periods-heading">
          <div className="periods-title">
            <span className="periods-title-icon" aria-hidden="true">
              ♫
            </span>
            <div>
              <p>Academics</p>
              <h1>Academic Periods</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state periods-message" role="status" aria-live="polite">{message}</p>
        )}
        <section className="periods-form-grid">
          <form
            onSubmit={(event) => void save(event, "years")}
            className="periods-panel"
          >
            <header className="periods-panel-header">
              <div>
                <p>Academic calendar</p>
                <h2>Create academic year</h2>
              </div>
            </header>
            <fieldset disabled={saving || !academy} className="periods-fields m-0 min-w-0 border-0">
              <label>
                <span>Year name</span>
                <input required name="name" placeholder="2026–27" />
              </label>
              <div className="periods-fields-two">
                <StandardDateField
                  name="year-start"
                  label="Start date"
                  value={yearStart}
                  onChange={setYearStart}
                  required
                />
                <StandardDateField
                  name="year-end"
                  label="End date"
                  value={yearEnd}
                  onChange={setYearEnd}
                  required
                />
              </div>
              <label className="periods-current">
                <input name="isCurrent" type="checkbox" />
                <span>Make this the current year</span>
              </label>
              <button className="enterprise-action-button periods-action">
                Create year
              </button>
            </fieldset>
          </form>
          <form
            onSubmit={(event) => void save(event, "terms")}
            className="periods-panel"
          >
            <header className="periods-panel-header">
              <div>
                <p>Academic calendar</p>
                <h2>Create term / semester</h2>
              </div>
            </header>
            <fieldset disabled={saving || !academy} className="periods-fields m-0 min-w-0 border-0">
              <StandardSelectField
                name="academicYearId"
                disabled={saving || !academy}
                value={termYearId}
                onChange={setTermYearId}
                placeholder="Select academic year"
                options={years
                  .filter((year) => !year.isClosed)
                  .map((year) => ({ value: year.id, label: year.name }))}
              />
              <label>
                <span>Term name</span>
                <input required name="name" placeholder="Term 1 / Semester 1" />
              </label>
              <div className="periods-fields-two">
                <StandardDateField
                  name="term-start"
                  label="Start date"
                  value={termStart}
                  onChange={setTermStart}
                  required
                />
                <StandardDateField
                  name="term-end"
                  label="End date"
                  value={termEnd}
                  onChange={setTermEnd}
                  required
                />
              </div>
              <button className="enterprise-action-button periods-action">
                Create term
              </button>
            </fieldset>
          </form>
        </section>
        <section className="periods-panel periods-year-register">
          <header className="periods-panel-header">
            <div>
              <p>Academic calendar</p>
              <h2>Academic-year register</h2>
            </div>
            <span>{years.length} years</span>
          </header>
          {years.length === 0 ? (
            <p className="periods-empty">No academic years created.</p>
          ) : (
            <div className="periods-table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Year</th>
                    <th>Dates</th>
                    <th>Terms</th>
                    <th>Control</th>
                  </tr>
                </thead>
                <tbody>
                  {years.map((year) => (
                    <tr key={year.id}>
                      <td>
                        <b>{year.name}</b>
                        {year.isCurrent && <span>Current</span>}
                      </td>
                      <td>
                        {formatDate(year.startDate)} —{" "}
                        {formatDate(year.endDate)}
                      </td>
                      <td>
                        {
                          terms.filter(
                            (term) => term.academicYearId === year.id,
                          ).length
                        }
                      </td>
                      <td>
                        {year.isClosed ? (
                          <em>Closed</em>
                        ) : (
                          <button
                            type="button"
                            disabled={saving}
                            onClick={() => void close("years", year.id)}
                          >
                            Close year
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
        <section className="periods-panel periods-term-register">
          <header className="periods-panel-header">
            <div>
              <p>Academic calendar</p>
              <h2>Term and semester register</h2>
            </div>
            <span>{terms.length} terms</span>
          </header>
          {terms.length === 0 ? (
            <p className="periods-empty">No terms or semesters created.</p>
          ) : (
            <ul>
              {terms.map((term) => (
                <li key={term.id}>
                  <div>
                    <b>{term.name}</b>
                    <small>
                      {
                        years.find((year) => year.id === term.academicYearId)
                          ?.name
                      }{" "}
                      · {formatDate(term.startDate)} —{" "}
                      {formatDate(term.endDate)}
                    </small>
                  </div>
                  {term.isClosed ? (
                    <em>Closed</em>
                  ) : (
                    <button
                      type="button"
                      disabled={saving}
                      onClick={() => void close("terms", term.id)}
                    >
                      Close term
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
