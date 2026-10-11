"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
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

async function fetchMusic(id: string) {
  const responses = await Promise.all([
    academyApi(`/api/academies/${id}/students`, { cache: "no-store" }),
    academyApi(`/api/academies/${id}/music-pieces`, { cache: "no-store" }),
    academyApi(`/api/academies/${id}/music-progress`, { cache: "no-store" }),
  ]);
  if (!responses.every((response) => response.ok)) throw new Error();
  const rows: unknown[] = await Promise.all(responses.map((response) => response.json()));
  if (!rows.every(Array.isArray)) throw new Error();
  return { students: rows[0] as Student[], pieces: rows[1] as Piece[], progress: rows[2] as Progress[] };
}

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
  const [saving, setSaving] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [workspaceReady, setWorkspaceReady] = useState(false);
  const pending = useRef(false);
  const ready = useRef(false);
  const lifetime = useRef(0);
  const readVersion = useRef(0);
  const currentProgress = useRef<Progress[]>([]);
  function applyWorkspace(data: Awaited<ReturnType<typeof fetchMusic>>) {
    setStudents(data.students);
    setPieces(data.pieces);
    setProgress(data.progress);
    currentProgress.current = data.progress;
    setStudentId((current) => current || data.students[0]?.id || "");
    setPieceId((current) => current || data.pieces[0]?.id || "");
    ready.current = true;
    setWorkspaceReady(true);
  }
  useEffect(() => {
    const generation = ++lifetime.current;
    const revision = ++readVersion.current;
    ready.current = false;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const academies: unknown = await response.json();
        if (!Array.isArray(academies) || !academies[0]?.id) throw new Error();
        if (lifetime.current !== generation || readVersion.current !== revision) return;
        const data = await fetchMusic(academies[0].id);
        if (lifetime.current !== generation || readVersion.current !== revision) return;
        setAcademy(academies[0]);
        setStudents(data.students);
        setPieces(data.pieces);
        setProgress(data.progress);
        currentProgress.current = data.progress;
        setStudentId((current) => current || data.students[0]?.id || "");
        setPieceId((current) => current || data.pieces[0]?.id || "");
        ready.current = true;
        setWorkspaceReady(true);
        setMessage("");
      } catch {
        if (lifetime.current !== generation || readVersion.current !== revision) return;
        setMessage(
          "Music workspace could not be loaded. Refresh the workspace or contact your administrator.",
        );
      }
    })();
    return () => { lifetime.current = generation + 1; ready.current = false; };
  }, []);
  async function refreshWorkspace() {
    if (pending.current) return;
    pending.current = true;
    ready.current = false;
    setWorkspaceReady(false);
    setRefreshing(true);
    const generation = lifetime.current;
    const revision = ++readVersion.current;
    try {
      let selected = academy;
      if (!selected) {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const academies: unknown = await response.json();
        if (!Array.isArray(academies) || !academies[0]?.id) throw new Error();
        selected = academies[0] as Academy;
      }
      if (lifetime.current !== generation || readVersion.current !== revision) return;
      const data = await fetchMusic(selected.id);
      if (lifetime.current !== generation || readVersion.current !== revision) return;
      setAcademy(selected);
      applyWorkspace(data);
      setMessage("Music workspace refreshed. Check the register before repeating an unconfirmed action.");
    } catch {
      if (lifetime.current === generation && readVersion.current === revision)
        setMessage("Music workspace could not be refreshed. Your draft is retained; refresh again before saving.");
    } finally {
      if (lifetime.current === generation) { pending.current = false; setRefreshing(false); }
    }
  }
  async function mutate(action: () => Promise<Response>, success: string, failure: (status: number) => string, reset?: () => void) {
    if (!academy || !ready.current || pending.current) return;
    pending.current = true;
    setSaving(true);
    setMessage("Saving music change…");
    const generation = lifetime.current;
    const academyId = academy.id;
    const uncertain = "The music change could not be confirmed. Your draft is retained. Refresh and check the register before retrying.";
    const requireRefresh = () => { ready.current = false; setWorkspaceReady(false); };
    try {
      const response = await action();
      if (lifetime.current !== generation) return;
      if (!response.ok) {
        if (response.status >= 500) { requireRefresh(); setMessage(uncertain); }
        else setMessage(failure(response.status));
        return;
      }
      reset?.();
      setMessage(success);
      try {
        const data = await fetchMusic(academyId);
        if (lifetime.current !== generation) return;
        applyWorkspace(data);
      } catch {
        if (lifetime.current !== generation) return;
        requireRefresh();
        setMessage(`${success} The register could not be refreshed; do not repeat the action. Refresh the workspace before saving again.`);
      }
    } catch {
      if (lifetime.current !== generation) return;
      requireRefresh();
      setMessage(uncertain);
    } finally {
      if (lifetime.current === generation) { pending.current = false; setSaving(false); }
    }
  }
  async function addPiece(event: FormEvent) {
    event.preventDefault();
    if (!academy) return;
    await mutate(() => academyApi(
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
    ), "Repertoire piece added.", () => "Piece could not be saved. Check the title and your permissions. Your draft is retained.", () => {
      setTitle((current) => current === title ? "" : current);
      setComposer((current) => current === composer ? "" : current);
    });
  }
  async function assign(event: FormEvent) {
    event.preventDefault();
    if (!academy || !studentId || !pieceId) return;
    await mutate(() => academyApi(
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
    ), "Piece assigned to student.", (status) => status === 409
      ? "This piece is already assigned to this student."
      : "Assignment could not be saved.", () => {
      setTargetDate((current) => current === targetDate ? "" : current);
    });
  }
  async function updateProgress(item: Progress, status: string) {
    if (!academy || currentProgress.current.find((row) => row.id === item.id) !== item
      || !progressStates.includes(status) || status === item.status) return;
    await mutate(() => academyApi(
      `/api/academies/${academy.id}/music-progress/${item.id}`,
      {
        method: "PATCH",
        headers: apiHeaders(true),
        body: JSON.stringify({
          status,
          targetDate: item.targetDate || null,
          score: item.score ?? null,
          notes: item.notes || null,
        }),
      },
    ), `Progress updated to ${label(status)}.`, () => "Progress could not be updated.");
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
          <p role="status" aria-live="polite" className="enterprise-page-state music-message">{message}</p>
        )}
        <button type="button" className="enterprise-action-button" disabled={saving || refreshing}
          onClick={() => void refreshWorkspace()}>{refreshing ? "Refreshing…" : "Refresh workspace"}</button>
        <section className="music-form-grid">
          <form onSubmit={addPiece} className="music-panel">
            <header className="music-panel-header">
              <div>
                <p>Repertoire library</p>
                <h2>Add repertoire piece</h2>
              </div>
            </header>
            <fieldset disabled={!workspaceReady || saving || refreshing} className="music-fields min-w-0 border-0 m-0">
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
                  disabled={!workspaceReady || saving || refreshing}
                  value={difficulty}
                  onChange={setDifficulty}
                  placeholder="Difficulty"
                  options={difficulties.map((value) => ({
                    value,
                    label: value,
                  }))}
                />
              </div>
              <button disabled={!workspaceReady || saving || refreshing} className="enterprise-action-button music-action">
                Add piece
              </button>
            </fieldset>
          </form>
          <form onSubmit={assign} className="music-panel">
            <header className="music-panel-header">
              <div>
                <p>Student repertoire</p>
                <h2>Assign to student</h2>
              </div>
            </header>
            <fieldset disabled={!workspaceReady || saving || refreshing} className="music-fields min-w-0 border-0 m-0">
              <StandardSelectField
                name="student"
                disabled={!workspaceReady || saving || refreshing}
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
                disabled={!workspaceReady || saving || refreshing}
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
              <button disabled={!workspaceReady || saving || refreshing} className="enterprise-action-button music-action">
                Assign piece
              </button>
            </fieldset>
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
                    disabled={!workspaceReady || saving || refreshing}
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
