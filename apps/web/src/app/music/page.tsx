"use client";

import { FormEvent, useEffect, useState } from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import {
  StandardDateField,
  StandardSelectField,
} from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Piece = {
  id: string;
  title: string;
  composer?: string | null;
  instrument?: string | null;
  genre?: string | null;
  difficulty: string;
  durationMinutes?: number | null;
};
type Progress = {
  id: string;
  studentId: string;
  musicPieceId: string;
  status: string;
  targetDate?: string | null;
  score?: number | null;
  notes?: string | null;
};
const difficulties = ["Beginner", "Intermediate", "Advanced"];
const progressStates = [
  "Assigned",
  "Learning",
  "ReadyForReview",
  "Mastered",
  "Paused",
];
const label = (value: string) =>
  value === "ReadyForReview" ? "Ready for review" : value;

export default function MusicPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [pieces, setPieces] = useState<Piece[]>([]);
  const [progress, setProgress] = useState<Progress[]>([]);
  const [title, setTitle] = useState("");
  const [composer, setComposer] = useState("");
  const [instrument, setInstrument] = useState("Piano");
  const [difficulty, setDifficulty] = useState("Beginner");
  const [studentId, setStudentId] = useState("");
  const [pieceId, setPieceId] = useState("");
  const [targetDate, setTargetDate] = useState("");
  const [message, setMessage] = useState("Loading music workspace…");
  async function load(id?: string) {
    const academyId = id ?? academy?.id;
    if (!academyId) return;
    const [studentResponse, pieceResponse, progressResponse] =
      await Promise.all([
        academyApi(`/api/academies/${academyId}/students`),
        academyApi(`/api/academies/${academyId}/music-pieces`),
        academyApi(`/api/academies/${academyId}/music-progress`),
      ]);
    if (!studentResponse.ok || !pieceResponse.ok || !progressResponse.ok)
      throw new Error();
    const studentRows: Student[] = await studentResponse.json();
    const pieceRows: Piece[] = await pieceResponse.json();
    setStudents(studentRows);
    setPieces(pieceRows);
    setProgress(await progressResponse.json());
    if (!studentId && studentRows.length) setStudentId(studentRows[0].id);
    if (!pieceId && pieceRows.length) setPieceId(pieceRows[0].id);
    setMessage("");
  }
  useEffect(() => {
    void (async () => {
      try {
        const response = await academyApi("/api/academies");
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!academies[0]) return setMessage("Create your academy first.");
        setAcademy(academies[0]);
        await load(academies[0].id);
      } catch {
        setMessage(
          "Music workspace could not be loaded. Apply the music migration and restart the API.",
        );
      }
    })();
  }, []);
  async function addPiece(event: FormEvent) {
    event.preventDefault();
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/music-pieces`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          title,
          composer: composer || null,
          instrument,
          genre: null,
          difficulty,
          durationMinutes: null,
        }),
      },
    );
    if (!response.ok) return setMessage("Piece title is required.");
    setTitle("");
    setComposer("");
    await load();
  }
  async function assign(event: FormEvent) {
    event.preventDefault();
    if (!academy || !studentId || !pieceId) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/music-progress`,
      {
        method: "POST",
        headers: apiHeaders(true),
        body: JSON.stringify({
          studentId,
          musicPieceId: pieceId,
          targetDate: targetDate || null,
          notes: null,
        }),
      },
    );
    if (response.status === 409)
      return setMessage("This piece is already assigned to this student.");
    if (!response.ok) return setMessage("Assignment could not be saved.");
    setTargetDate("");
    await load();
  }
  async function updateProgress(item: Progress, status: string) {
    if (!academy) return;
    const response = await academyApi(
      `/api/academies/${academy.id}/music-progress/${item.id}`,
      {
        method: "PATCH",
        headers: apiHeaders(true),
        body: JSON.stringify({
          status,
          targetDate: item.targetDate || null,
          score: item.score || null,
          notes: item.notes || null,
        }),
      },
    );
    if (!response.ok) return setMessage("Progress could not be updated.");
    await load();
  }
  const name = (id: string) => {
    const student = students.find((item) => item.id === id);
    return student
      ? `${student.firstName} ${student.lastName}`
      : "Unknown student";
  };
  const piece = (id: string) =>
    pieces.find((item) => item.id === id)?.title ?? "Unknown piece";
  return (
    <main className="enterprise-settings music-standard min-h-screen">
      <WorkspaceNav />
      <div className="music-content mx-auto max-w-6xl px-6 py-10">
        <header className="music-heading">
          <div className="music-title">
            <span className="music-title-icon" aria-hidden="true">
              ♫
            </span>
            <div>
              <p>Academics</p>
              <h1>Music Progress</h1>
            </div>
          </div>
        </header>
        {message && (
          <p className="enterprise-page-state music-message">{message}</p>
        )}
        <section className="music-form-grid">
          <form onSubmit={addPiece} className="music-panel">
            <header className="music-panel-header">
              <div>
                <p>Repertoire library</p>
                <h2>Add repertoire piece</h2>
              </div>
            </header>
            <div className="music-fields">
              <label>
                <span>Piece title</span>
                <input
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                  placeholder="Piece title"
                  required
                />
              </label>
              <label>
                <span>Composer</span>
                <input
                  value={composer}
                  onChange={(event) => setComposer(event.target.value)}
                  placeholder="Optional composer"
                />
              </label>
              <div className="music-fields-two">
                <label>
                  <span>Instrument</span>
                  <input
                    value={instrument}
                    onChange={(event) => setInstrument(event.target.value)}
                    placeholder="Instrument"
                  />
                </label>
                <StandardSelectField
                  name="difficulty"
                  value={difficulty}
                  onChange={setDifficulty}
                  placeholder="Difficulty"
                  options={difficulties.map((value) => ({
                    value,
                    label: value,
                  }))}
                />
              </div>
              <button className="enterprise-action-button music-action">
                Add piece
              </button>
            </div>
          </form>
          <form onSubmit={assign} className="music-panel">
            <header className="music-panel-header">
              <div>
                <p>Student repertoire</p>
                <h2>Assign to student</h2>
              </div>
            </header>
            <div className="music-fields">
              <StandardSelectField
                name="student"
                value={studentId}
                onChange={setStudentId}
                placeholder="Select student"
                options={students.map((student) => ({
                  value: student.id,
                  label: `${student.firstName} ${student.lastName}`,
                }))}
              />
              <StandardSelectField
                name="piece"
                value={pieceId}
                onChange={setPieceId}
                placeholder="Select piece"
                options={pieces.map((item) => ({
                  value: item.id,
                  label: `${item.title} · ${item.difficulty}`,
                }))}
              />
              <StandardDateField
                name="target"
                label="Target date"
                value={targetDate}
                onChange={setTargetDate}
              />
              <button className="enterprise-action-button music-action">
                Assign piece
              </button>
            </div>
          </form>
        </section>
        <section className="music-panel music-register">
          <header className="music-panel-header">
            <div>
              <p>Student repertoire</p>
              <h2>Progress register</h2>
            </div>
            <span>{progress.length} assignments</span>
          </header>
          {progress.length === 0 ? (
            <p className="music-empty">No pieces assigned yet.</p>
          ) : (
            <ul>
              {progress.map((item) => (
                <li key={item.id}>
                  <div>
                    <b>
                      {name(item.studentId)} — {piece(item.musicPieceId)}
                    </b>
                    <small>
                      {item.targetDate
                        ? `Target ${item.targetDate}`
                        : "No target date"}
                    </small>
                  </div>
                  <StandardSelectField
                    name={`progress-${item.id}`}
                    value={item.status}
                    onChange={(status) => void updateProgress(item, status)}
                    placeholder="Progress"
                    options={progressStates.map((value) => ({
                      value,
                      label: label(value),
                    }))}
                  />
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </main>
  );
}
