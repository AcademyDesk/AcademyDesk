"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string; name: string };
type Student = {
  id: string;
  firstName: string;
  lastName: string;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
};

export default function StudentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("Active");
  const [message, setMessage] = useState("Loading learner register…");
  const [saving, setSaving] = useState(false);
  const filtered = useMemo(
    () =>
      students.filter(
        (student) =>
          (status === "All" || (status === "Active") === student.isActive) &&
          `${student.firstName} ${student.lastName} ${student.email ?? ""} ${student.phone ?? ""}`
            .toLowerCase()
            .includes(query.toLowerCase()),
      ),
    [students, query, status],
  );
  async function load() {
    const response = await academyApi("/api/academies", { cache: "no-store" });
    if (!response.ok) throw new Error();
    const academies: Academy[] = await response.json();
    const current = academies[0];
    if (!current) {
      setMessage("Create an academy before managing learners.");
      return;
    }
    setAcademy(current);
    const roster = await academyApi(`/api/academies/${current.id}/students`, {
      cache: "no-store",
    });
    if (!roster.ok) throw new Error();
    setStudents(await roster.json());
    setMessage("");
  }
  useEffect(() => {
    void load().catch(() =>
      setMessage("The learner register could not be loaded."),
    );
  }, []);
  async function toggle(student: Student) {
    if (!academy) return;
    setSaving(true);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/students/${student.id}`,
        {
          method: "PUT",
          headers: apiHeaders(true),
          body: JSON.stringify({
            firstName: student.firstName,
            lastName: student.lastName,
            email: student.email,
            phone: student.phone,
            branchId: null,
            isActive: !student.isActive,
          }),
        },
      );
      if (!response.ok) throw new Error();
      await load();
      setMessage(
        `${student.firstName} ${student.lastName} ${student.isActive ? "made inactive" : "reactivated"}.`,
      );
    } catch {
      setMessage("The learner status could not be changed.");
    } finally {
      setSaving(false);
    }
  }
  return (
    <main className="enterprise-settings">
      <header className="enterprise-page-header">
        <p>Learners & families / register</p>
        <h2>Student overview</h2>
        <span>
          Search, review and act on every learner. Use the 360 record to connect
          family, enrolment, attendance, fees, learning, and communications.
        </span>
      </header>
      <section className="enterprise-settings-toolbar">
        <div className="enterprise-settings-tabs">
          <button aria-selected>All learners</button>
          <button
            onClick={() => setStatus("Active")}
            aria-selected={status === "Active"}
          >
            Active
          </button>
          <button
            onClick={() => setStatus("Inactive")}
            aria-selected={status === "Inactive"}
          >
            Inactive
          </button>
        </div>
        <label className="enterprise-settings-search">
          ⌕
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search name, email or phone"
          />
        </label>
      </section>
      {message && (
        <p className="mt-4 rounded-lg border border-amber-700/50 bg-amber-950/40 p-3 text-sm text-amber-100">
          {message}
        </p>
      )}
      <section className="mt-5 grid gap-5 xl:grid-cols-[1fr_340px]">
        <section className="surface-panel overflow-hidden rounded-xl">
          <header className="flex items-center justify-between gap-4 border-b p-5">
            <div>
          <h3 className="font-semibold">Students</h3>
              <p className="mt-1 text-sm text-slate-400">
                {filtered.length} of {students.length} records ·{" "}
                {academy?.name ?? "Academy"}
              </p>
            </div>
            <Link
              href="/enrollments"
              className="text-sm font-semibold text-cyan-300"
            >
              Manage enrolments →
            </Link>
          </header>
          <div className="overflow-auto">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead className="bg-slate-900 text-slate-400">
                <tr>
                  <th className="p-4 font-medium">Learner</th>
                  <th className="p-4 font-medium">Contact</th>
                  <th className="p-4 font-medium">Status</th>
                  <th className="p-4 font-medium">Next action</th>
                </tr>
              </thead>
              <tbody>
                {filtered.length ? (
                  filtered.map((student) => (
                    <tr
                      key={student.id}
                      className="border-t hover:bg-slate-900/50"
                    >
                      <td className="p-4">
                        <b>
                          {student.firstName} {student.lastName}
                        </b>
                        <small className="mt-1 block text-slate-400">
                          Learner record
                        </small>
                      </td>
                      <td className="p-4 text-slate-300">
                        {student.email || student.phone || "No contact details"}
                      </td>
                      <td className="p-4">
                        <span
                          className={
                            student.isActive
                              ? "platform-status active"
                              : "platform-status"
                          }
                        >
                          {student.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td className="p-4">
                        <div className="flex gap-3">
                          <Link
                            className="text-cyan-300"
                            href={`/student-profile?studentId=${student.id}`}
                          >
                            Open record
                          </Link>
                          <Link
                            className="text-cyan-300"
                            href={`/enrollments?studentId=${student.id}`}
                          >
                            Enrol
                          </Link>
                          <button
                            disabled={saving}
                            onClick={() => void toggle(student)}
                            className="text-slate-400 hover:text-slate-100"
                          >
                            {student.isActive ? "Deactivate" : "Reactivate"}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={4} className="p-10 text-center text-slate-400">
                      No learners match this view.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </section>
        <aside className="surface-panel rounded-xl p-5">
          <h3 className="font-semibold">New student</h3>
          <p className="mt-2 text-sm leading-6 text-slate-400">
            Use the guided onboarding record for all student and parent details.
          </p>
          <Link
            href="/student-onboarding"
            className="mt-5 inline-flex w-full items-center justify-center rounded bg-cyan-400 p-2.5 font-semibold text-slate-950"
          >
            Start onboarding
          </Link>
        </aside>
      </section>
    </main>
  );
}
