"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import {
  StandardDateField,
  StandardSelectField,
} from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Batch = {
  id: string;
  name: string;
  capacity: number;
  activeEnrolments?: number;
};
type Enrollment = { studentId: string; batchId: string; status: string };
type Promotion = {
  id: string;
  studentId: string;
  sourceBatchId: string;
  targetBatchId: string;
  effectiveDate: string;
  status: string;
  notes?: string | null;
};

export default function BatchPromotionsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [enrolments, setEnrolments] = useState<Enrollment[]>([]);
  const [promotions, setPromotions] = useState<Promotion[]>([]);
  const [notice, setNotice] = useState("Loading promotion governance…");
  const [saving, setSaving] = useState(false);
  const [studentId, setStudentId] = useState("");
  const [sourceBatchId, setSourceBatchId] = useState("");
  const [targetBatchId, setTargetBatchId] = useState("");
  const [effectiveDate, setEffectiveDate] = useState("");

  const pending = useRef(false);

  async function fetchWorkspace(id: string) {
    const responses = await Promise.all([academyApi(`/api/academies/${id}/students`, { cache: "no-store" }), academyApi(`/api/academies/${id}/batches`, { cache: "no-store" }), academyApi(`/api/academies/${id}/enrollments`, { cache: "no-store" }), academyApi(`/api/academies/${id}/batch-promotions`, { cache: "no-store" })]);
    if (!responses.every(response => response.ok)) throw new Error();
    const data = await Promise.all(responses.map(response => response.json()));
    if (!data.every(Array.isArray)) throw new Error();
    return data;
  }
  function applyWorkspace(data: Awaited<ReturnType<typeof fetchWorkspace>>) {
    setStudents(data[0]); setBatches(data[1]); setEnrolments(data[2]); setPromotions(data[3]);
  }
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const rows: Academy[] = await response.json();
        if (!Array.isArray(rows) || !rows[0]?.id) throw new Error();
        const data = await fetchWorkspace(rows[0].id);
        if (!active) return;
        setStudents(data[0]); setBatches(data[1]); setEnrolments(data[2]); setPromotions(data[3]); setAcademy(rows[0]);

        setNotice("");
      } catch { if (active) setNotice("Promotion data could not be loaded. Please refresh or check your access."); }
    })();
    return () => { active = false; };
  }, []);
  async function mutate(request: () => Promise<Response>, success: string, reset?: () => void) {
    if (!academy || pending.current) return;
    pending.current = true; setSaving(true); setNotice("");
    try {
      const response = await request();
      if (!response.ok) {
        if (response.status >= 500) throw new Error();
        const payload = await response.json().catch(() => null);
        setNotice(typeof payload?.message === "string" ? payload.message : "The change was not accepted. Your draft has been retained.");
        return;
      }
      reset?.(); setNotice(success);
      try { applyWorkspace(await fetchWorkspace(academy.id)); }
      catch { setNotice(`${success} The register could not be refreshed. Do not repeat the action; refresh to check the saved record.`); }
    } catch { setNotice("The result could not be confirmed. Your draft has been retained. Check the register before trying again."); }
    finally { pending.current = false; setSaving(false);  }
  }
  const activeSourceBatches = useMemo(
    () =>
      studentId
        ? enrolments
            .filter(
              (item) =>
                item.studentId === studentId && item.status === "Active",
            )
            .map((item) => item.batchId)
        : [],
    [enrolments, studentId],
  );
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (
      !academy ||
      !studentId ||
      !sourceBatchId ||
      !targetBatchId ||
      !effectiveDate
    )
      return setNotice(
        "Select the learner, source, target, and effective date.",
      );
    if (pending.current) return;
    const form = event.currentTarget;
    const data = new FormData(form);
    await mutate(() => academyApi(`/api/academies/${academy.id}/batch-promotions`, {
      method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, sourceBatchId, targetBatchId, effectiveDate, notes: data.get("notes") || null }),
    }), "Promotion request recorded.", () => { setTargetBatchId(""); setEffectiveDate(""); });
  }
  async function decide(item: Promotion, status: "Approved" | "Rejected") {
    if (!academy || pending.current) return;
    const notes = window.prompt(status === "Approved" ? "Approval note (optional)" : "Rejection reason");
    if (notes === null) return;
    if (status === "Rejected" && !notes.trim()) return setNotice("A rejection reason is required.");
    await mutate(() => academyApi(`/api/academies/${academy.id}/batch-promotions/${item.id}/decision`, {
      method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ status, notes }),
    }), `Promotion ${status.toLowerCase()}.`);
  }
  const name = (id: string) => {
    const student = students.find((item) => item.id === id);
    return student
      ? `${student.firstName} ${student.lastName}`
      : "Learner unavailable";
  };
  const batch = (id: string) =>
    batches.find((item) => item.id === id)?.name ?? "Batch unavailable";
  return (
    <main className="enterprise-settings governance-standard promotions-standard min-h-screen">
      <WorkspaceNav />
      <div className="governance-content mx-auto max-w-6xl px-6 py-10">
        <header className="governance-heading">
          <div className="governance-title">
            <span className="governance-title-icon" aria-hidden="true">
              ♫
            </span>
            <div>
              <p>Academics</p>
              <h1>Batch Promotions</h1>
            </div>
          </div>
        </header>
        {notice && (
          <p role="status" aria-live="polite" className="enterprise-page-state governance-message">{notice}</p>
        )}
        <section className="governance-form-grid">
          <form onSubmit={create} className="governance-panel">
            <header className="governance-panel-header">
              <div>
                <p>Learner progression</p>
                <h2>New promotion request</h2>
              </div>
            </header>
            <fieldset disabled={saving || !academy} className="governance-fields m-0 min-w-0 border-0">
              <StandardSelectField
                name="student"
                disabled={saving || !academy}
                value={studentId}
                onChange={(id) => {
                  setStudentId(id);
                  setSourceBatchId("");
                }}
                placeholder="Select student"
                options={students.map((student) => ({
                  value: student.id,
                  label: `${student.firstName} ${student.lastName}`,
                }))}
              />
              <StandardSelectField
                name="source"
                disabled={saving || !academy}
                value={sourceBatchId}
                onChange={setSourceBatchId}
                placeholder="Select active source batch"
                options={activeSourceBatches.map((id) => ({
                  value: id,
                  label: batch(id),
                }))}
              />
              <StandardSelectField
                name="target"
                disabled={saving || !academy}
                value={targetBatchId}
                onChange={setTargetBatchId}
                placeholder="Select target batch"
                options={batches
                  .filter((item) => item.id !== sourceBatchId)
                  .map((item) => ({
                    value: item.id,
                    label: `${item.name} · ${item.activeEnrolments ?? 0}/${item.capacity}`,
                  }))}
              />
              <StandardDateField
                name="effective"
                label="Effective date"
                value={effectiveDate}
                onChange={setEffectiveDate}
                required
              />
              <label>
                <span>Academic decision note</span>
                <textarea
                  name="notes"
                  placeholder="Evidence, assessment outcome, or progression rationale"
                />
              </label>
              <button
                disabled={saving || !academy}
                className="enterprise-action-button governance-action"
              >
                {saving ? "Saving…" : "Request promotion"}
              </button>
            </fieldset>
          </form>
          <section className="governance-panel promotions-register">
            <header className="governance-panel-header">
              <div>
                <p>Learner progression</p>
                <h2>Promotion register</h2>
              </div>
              <span>{promotions.length} requests</span>
            </header>
            {promotions.length === 0 ? (
              <p className="governance-empty">No promotion requests yet.</p>
            ) : (
              <ul>
                {promotions.map((item) => (
                  <li key={item.id}>
                    <div>
                      <b>{name(item.studentId)}</b>
                      <small>
                        {batch(item.sourceBatchId)} →{" "}
                        {batch(item.targetBatchId)}
                      </small>
                      <small>
                        {item.effectiveDate} · {item.status}
                      </small>
                    </div>
                    {item.status === "Pending" ||
                    item.status === "PendingApproval" ? (
                      <span>
                        <button
                          disabled={saving}
                          type="button"
                          onClick={() => void decide(item, "Approved")}
                        >
                          Approve
                        </button>
                        <button
                          disabled={saving}
                          type="button"
                          onClick={() => void decide(item, "Rejected")}
                        >
                          Reject
                        </button>
                      </span>
                    ) : (
                      <em>{item.status}</em>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
