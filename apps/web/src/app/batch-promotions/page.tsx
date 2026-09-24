"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
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
  async function load(id?: string) {
    try {
      const academyId = id ?? academy?.id;
      if (!academyId) return;
      const [
        studentResponse,
        batchResponse,
        enrollmentResponse,
        promotionResponse,
      ] = await Promise.all([
        academyApi(`/api/academies/${academyId}/students`),
        academyApi(`/api/academies/${academyId}/batches`),
        academyApi(`/api/academies/${academyId}/enrollments`),
        academyApi(`/api/academies/${academyId}/batch-promotions`),
      ]);
      if (
        ![
          studentResponse,
          batchResponse,
          enrollmentResponse,
          promotionResponse,
        ].every((response) => response.ok)
      )
        throw new Error();
      setStudents(await studentResponse.json());
      setBatches(await batchResponse.json());
      setEnrolments(await enrollmentResponse.json());
      setPromotions(await promotionResponse.json());
      setNotice("");
    } catch {
      setNotice("Promotion data could not be loaded.");
    }
  }
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0])
          return setNotice(
            "Create an academy, learner and batch before using promotions.",
          );
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setNotice(
          "Promotion data could not be loaded. Please sign in and restart the API if needed.",
        );
      }
    })();
  }, []);
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
    setSaving(true);
    try {
      const data = new FormData(event.currentTarget);
      const response = await academyApi(
        `/api/academies/${academy.id}/batch-promotions`,
        {
          method: "POST",
          headers: apiHeaders(true),
          body: JSON.stringify({
            studentId,
            sourceBatchId,
            targetBatchId,
            effectiveDate,
            notes: data.get("notes") || null,
          }),
        },
      );
      if (!response.ok) throw new Error();
      setNotice("Promotion request recorded.");
      setTargetBatchId("");
      setEffectiveDate("");
      await load();
    } catch {
      setNotice(
        "Promotion could not be created. Select an active enrolment, different target batch, and valid date.",
      );
    } finally {
      setSaving(false);
    }
  }
  async function decide(item: Promotion, status: "Approved" | "Rejected") {
    if (!academy) return;
    const notes =
      window.prompt(
        status === "Approved" ? "Approval note (optional)" : "Rejection reason",
      ) ?? "";
    if (status === "Rejected" && !notes.trim())
      return setNotice("A rejection reason is required.");
    setSaving(true);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/batch-promotions/${item.id}/decision`,
        {
          method: "PATCH",
          headers: apiHeaders(true),
          body: JSON.stringify({ status, notes }),
        },
      );
      if (!response.ok) throw new Error();
      setNotice(`Promotion ${status.toLowerCase()}.`);
      await load();
    } catch {
      setNotice("Promotion decision could not be saved.");
    } finally {
      setSaving(false);
    }
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
          <p className="enterprise-page-state governance-message">{notice}</p>
        )}
        <section className="governance-form-grid">
          <form onSubmit={create} className="governance-panel">
            <header className="governance-panel-header">
              <div>
                <p>Learner progression</p>
                <h2>New promotion request</h2>
              </div>
            </header>
            <div className="governance-fields">
              <StandardSelectField
                name="student"
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
            </div>
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
