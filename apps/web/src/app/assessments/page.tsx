"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import {
  StandardDateField,
  StandardSelectField,
  StandardTimeField,
} from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Batch = { id: string; name: string };
type Student = { id: string; firstName: string; lastName: string };
type Enrollment = { studentId: string; batchId: string; status: string };
type Assessment = {
  id: string;
  batchId: string;
  title: string;
  type: string;
  maxScore: number;
  scheduledAtUtc?: string | null;
  isPublished: boolean;
};
type Result = {
  studentId: string;
  score: number;
  grade?: string | null;
  isGradeManual?: boolean | null;
  remarks?: string | null;
  isPublished: boolean;
};
type GradingScheme = {
  id: string;
  name: string;
  passingPercent: number;
};
type AssessmentOptions = {
  batches: Batch[];
  students: Student[];
  enrollments: Enrollment[];
  gradingSchemes: GradingScheme[];
};
const assessmentTypes = ["Performance", "Exam", "Recital", "Test", "Practical"];

export default function AssessmentsPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [batches, setBatches] = useState<Batch[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [assessments, setAssessments] = useState<Assessment[]>([]);
  const [gradingSchemes, setGradingSchemes] = useState<GradingScheme[]>([]);
  const [assessmentId, setAssessmentId] = useState("");
  const [results, setResults] = useState<Result[]>([]);
  const [batchId, setBatchId] = useState("");
  const [title, setTitle] = useState("");
  const [type, setType] = useState("Performance");
  const [maxScore, setMaxScore] = useState("100");
  const [scheduledDate, setScheduledDate] = useState("");
  const [scheduledTime, setScheduledTime] = useState("10:00");
  const [gradingSchemeId, setGradingSchemeId] = useState("");
  const [message, setMessage] = useState("Loading assessments…");
  const [savingStudentId, setSavingStudentId] = useState("");
  async function load(academyId?: string) {
    const id = academyId ?? academy?.id;
    if (!id) return;
    const [optionsResponse, assessmentResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/assessments/options`, { cache: "no-store" }),
      academyApi(`/api/academies/${id}/assessments`, { cache: "no-store" }),
    ]);
    if (![optionsResponse, assessmentResponse].every((response) => response.ok)) {
      const denied = [optionsResponse, assessmentResponse].some((response) => response.status === 403);
      throw new Error(denied
        ? "Assessments could not be loaded. Your account needs academic access and an enabled academic module."
        : "Assessments could not be loaded. Please try again.");
    }
    const [options, assessmentData]: [AssessmentOptions, Assessment[]] = await Promise.all([
      optionsResponse.json(), assessmentResponse.json(),
    ]);
    if (!options || ![options.batches, options.students, options.enrollments, options.gradingSchemes, assessmentData].every(Array.isArray))
      throw new Error("Assessment options could not be loaded completely. Please try again.");
    setBatches(options.batches);
    setStudents(options.students);
    setEnrollments(options.enrollments);
    setAssessments(assessmentData);
    setGradingSchemes(options.gradingSchemes);
    if (!batchId && options.batches.length) setBatchId(options.batches[0].id);
    if (!assessmentId && assessmentData.length)
      setAssessmentId(assessmentData[0].id);
    setMessage("");
  }
  async function loadResults(id: string) {
    if (!academy || !id) return setResults([]);
    const response = await academyApi(
      `/api/academies/${academy.id}/assessments/${id}/results`,
      { cache: "no-store" },
    );
    if (!response.ok) throw new Error();
    setResults(await response.json());
  }
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies", {
          cache: "no-store",
        });
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0])
          return setMessage("Create your academy and batch first.");
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch (error) {
        setMessage(
          error instanceof Error && error.message ? error.message : "Assessments could not be loaded. Please try again.",
        );
      }
    })();
  }, []);
  useEffect(() => {
    void loadResults(assessmentId).catch(() =>
      setMessage("Assessment results could not be loaded."),
    );
  }, [academy, assessmentId]);
  async function createAssessment(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !batchId) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/assessments`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          batchId,
          title,
          type,
          maxScore: Number(maxScore),
          gradingSchemeId: gradingSchemeId || null,
          scheduledAtUtc: scheduledDate
            ? new Date(`${scheduledDate}T${scheduledTime}:00`).toISOString()
            : null,
          isPublished: true,
        }),
      },
    );
    if (!response.ok)
      return setMessage("Enter a title and positive maximum score.");
    setTitle("");
    setScheduledDate("");
    setGradingSchemeId("");
    setMessage("");
    await load();
  }
  const selected = assessments.find((item) => item.id === assessmentId);
  const roster = selected
    ? enrollments
        .filter(
          (item) =>
            item.batchId === selected.batchId && item.status === "Active",
        )
        .map((item) =>
          students.find((student) => student.id === item.studentId),
        )
        .filter((student): student is Student => Boolean(student))
    : [];
  const currentResult = (studentId: string) =>
    results.find((result) => result.studentId === studentId);
  async function saveResult(
    studentId: string,
    score: number,
    grade: string,
    remarks: string,
    isGradeManual: boolean,
  ) {
    if (!academy || !selected || Number.isNaN(score)) return;
    setSavingStudentId(studentId);
    setMessage("");
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/assessments/${selected.id}/results`,
        {
          method: "POST",
          headers: apiHeaders(true),
          body: JSON.stringify({
            studentId,
            score,
            grade: isGradeManual ? grade.trim() || null : null,
            isGradeManual,
            remarks: remarks || null,
            isPublished: true,
          }),
        },
      );
      if (!response.ok) {
        const details = await response.json().catch(() => null);
        setMessage(details?.message ?? "Assessment result could not be saved. Your entries have been retained.");
        return;
      }
      setMessage("Assessment result saved.");
      try {
        await loadResults(selected.id);
      } catch {
        setMessage("Assessment result saved, but results could not be refreshed. Refresh the page to see the saved result.");
      }
    } catch {
      setMessage("Assessment result could not be confirmed. Your entries have been retained; check the saved result before retrying.");
    } finally {
      setSavingStudentId("");
    }
  }
  const batchName = (id: string) =>
    batches.find((batch) => batch.id === id)?.name ?? "Unknown batch";
  return (
    <main className="enterprise-settings assessments-standard min-h-screen">
      <WorkspaceNav />
      <div className="assessments-content mx-auto max-w-6xl px-6 py-10">
        <header className="assessments-heading">
          <div className="assessments-title">
            <span className="assessments-title-icon" aria-hidden="true">
              ♫
            </span>
            <div>
              <p>Academics</p>
              <h1>Assessments</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state assessments-message" role="status" aria-live="polite">{message}</p>
        )}
        <section className="assessments-layout">
          <form onSubmit={createAssessment} className="assessments-panel">
            <header className="assessments-panel-header">
              <div>
                <p>Assessment setup</p>
                <h2>Create assessment</h2>
              </div>
            </header>
            <div className="assessments-fields">
              <StandardSelectField
                name="batch"
                value={batchId}
                onChange={setBatchId}
                placeholder="Select batch"
                options={batches.map((batch) => ({
                  value: batch.id,
                  label: batch.name,
                }))}
              />
              <label>
                <span>Assessment title</span>
                <input
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                  placeholder="Assessment title"
                  required
                />
              </label>
              <div className="assessments-fields-two">
                <StandardSelectField
                  name="assessment-type"
                  value={type}
                  onChange={setType}
                  placeholder="Assessment type"
                  options={assessmentTypes.map((value) => ({
                    value,
                    label: value,
                  }))}
                />
                <label>
                  <span>Maximum score</span>
                  <input
                    type="number"
                    min="1"
                    step="0.01"
                    value={maxScore}
                    onChange={(event) => setMaxScore(event.target.value)}
                    required
                  />
                </label>
              </div>
              <StandardSelectField
                name="grading-scheme"
                value={gradingSchemeId}
                onChange={setGradingSchemeId}
                placeholder="No scheme — manual grade"
                options={gradingSchemes.map((scheme) => ({
                  value: scheme.id,
                  label: `${scheme.name} · pass ${scheme.passingPercent}%`,
                }))}
              />
              <div className="assessments-fields-two">
                <StandardDateField
                  name="scheduled-date"
                  label="Scheduled date"
                  value={scheduledDate}
                  onChange={setScheduledDate}
                />
                <StandardTimeField
                  name="scheduled-time"
                  label="Start time"
                  value={scheduledTime}
                  onChange={setScheduledTime}
                />
              </div>
              <button
                disabled={!academy || !batchId}
                className="enterprise-action-button assessments-action"
              >
                Create assessment
              </button>
            </div>
          </form>
          <section className="assessments-panel assessments-results-panel">
            <header className="assessments-panel-header">
              <div>
                <p>Assessment results</p>
                <h2>Record results</h2>
              </div>
            </header>
            <div className="assessments-picker">
              <StandardSelectField
                name="assessment"
                value={assessmentId}
                onChange={setAssessmentId}
                placeholder="Select assessment"
                options={assessments.map((assessment) => ({
                  value: assessment.id,
                  label: `${assessment.title} · ${batchName(assessment.batchId)} · /${assessment.maxScore}`,
                }))}
              />
            </div>
            {selected &&
              (roster.length === 0 ? (
                <p className="assessments-empty">
                  No active students are enrolled in this assessment’s batch.
                </p>
              ) : (
                <ul>
                  {roster.map((student) => (
                    <ResultRow
                      key={`${selected.id}-${student.id}-${JSON.stringify(currentResult(student.id))}`}
                      student={student}
                      result={currentResult(student.id)}
                      maxScore={selected.maxScore}
                      saving={savingStudentId === student.id}
                      onSave={saveResult}
                    />
                  ))}
                </ul>
              ))}
          </section>
        </section>
      </div>
    </main>
  );
}

function ResultRow({
  student,
  result,
  maxScore,
  saving,
  onSave,
}: {
  student: Student;
  result?: Result;
  maxScore: number;
  saving: boolean;
  onSave: (
    studentId: string,
    score: number,
    grade: string,
    remarks: string,
    isGradeManual: boolean,
  ) => Promise<void>;
}) {
  const [score, setScore] = useState(result?.score.toString() ?? "");
  const [grade, setGrade] = useState(result?.grade ?? "");
  const [remarks, setRemarks] = useState(result?.remarks ?? "");
  const [isGradeManual, setIsGradeManual] = useState(
    result?.isGradeManual ?? Boolean(result?.grade?.trim()),
  );
  return (
    <li>
      <b>
        {student.firstName} {student.lastName}
      </b>
      <div>
        <label className="assessment-grading-mode">
          Grading mode
          <StandardSelectField
            name={`grading-mode-${student.id}`}
            placeholder="Select grading mode"
            value={isGradeManual ? "manual" : "automatic"}
            disabled={saving}
            onChange={(value) => setIsGradeManual(value === "manual")}
            options={[
              { value: "automatic", label: "Automatic (grading scheme)" },
              { value: "manual", label: "Manual override" },
            ]}
          />
        </label>
        <small className="assessment-grading-help">
          Automatic grades follow the assessment’s grading scheme. Without a scheme, only the score is saved.
          {result?.isGradeManual == null && result?.grade?.trim() ? " This existing grade has been preserved. Select Automatic to recalculate it." : ""}
        </small>
        <input
          type="number"
          min="0"
          max={maxScore}
          step="0.01"
          value={score}
          onChange={(event) => setScore(event.target.value)}
          placeholder={`Score / ${maxScore}`}
          aria-label="Assessment score"
          disabled={saving}
        />
        <input
          value={grade}
          disabled={saving || !isGradeManual}
          maxLength={30}
          aria-label="Manual grade"
          onChange={(event) => setGrade(event.target.value)}
          placeholder={isGradeManual ? "Manual grade" : "Calculated when saved"}
        />
        <input
          value={remarks}
          onChange={(event) => setRemarks(event.target.value)}
          placeholder="Remarks"
        />
        <button
          disabled={saving || score.trim() === "" || !Number.isFinite(Number(score)) || Number(score) < 0 || Number(score) > maxScore || (isGradeManual && !grade.trim())}
          type="button"
          onClick={() => void onSave(student.id, Number(score), grade, remarks, isGradeManual)}
        >
          {saving ? "Saving…" : "Save"}
        </button>
      </div>
    </li>
  );
}
