"use client";
import { FormEvent, useEffect, useRef, useState } from "react";
import { forgetUpload, MAX_MEDIA_BYTES, pendingUpload, PendingMediaUpload, prepareUpload, recordingExtension, runMediaUpload, UploadScope } from "@/lib/class-media-upload";
import { PREVIEW_BYTES, previewMime } from "@/lib/private-material";

export function TeacherMaterialUploader({ scope, onUploaded }: { scope: UploadScope; onUploaded: () => Promise<void> }) {
  const [file, setFile] = useState<File | null>(null);
  const [draft, setDraft] = useState<PendingMediaUpload | null>(null);
  const [invalidDraft, setInvalidDraft] = useState(false);
  const [title, setTitle] = useState(""); const [description, setDescription] = useState("");
  const [message, setMessage] = useState(""); const [busy, setBusy] = useState(false);
  const [progress, setProgress] = useState(0); const [phase, setPhase] = useState("");
  const [recording, setRecording] = useState(false); const [paused, setPaused] = useState(false);
  const [requestingMicrophone, setRequestingMicrophone] = useState(false);
  const [microphoneUnsettled, setMicrophoneUnsettled] = useState(false);
  const [preview, setPreview] = useState<{ url: string; mime: string } | null>(null);
  const operation = useRef<AbortController | null>(null); const recorder = useRef<MediaRecorder | null>(null);
  const mounted = useRef(true); const microphonePending = useRef(false);
  const microphoneOutstanding = useRef(false); const microphoneCancelled = useRef(false);
  const input = useRef<HTMLInputElement | null>(null);
  useEffect(() => {
    let active = true; mounted.current = true;
    // Async initialization keeps storage/browser work out of server rendering.
    void Promise.resolve().then(() => {
      if (!active) return;
      try { const saved = pendingUpload(scope, localStorage); setDraft(saved); if (saved) { setTitle(saved.title); setDescription(saved.description); setMessage(`Reselect “${saved.fileName}” to resume its saved upload.`); } }
      catch { setInvalidDraft(true); setMessage("Saved upload data cannot be read. Clear this browser draft before starting again."); }
    });
    return () => { active = false; mounted.current = false; operation.current?.abort(); const current = recorder.current; if (current) { current.onstop = null; current.ondataavailable = null; current.onerror = null; if (current.state !== "inactive") current.stop(); current.stream.getTracks().forEach(track => track.stop()); } };
    // Parent keys this component by immutable teaching scope.
  }, [scope]);
  useEffect(() => {
    if (!file || file.size > PREVIEW_BYTES) return;
    let active = true; let url: string | undefined;
    void file.slice(0,32).arrayBuffer().then(buffer => {
      const mime = previewMime(new Uint8Array(buffer));
      if (active && mime) { url = URL.createObjectURL(new Blob([file], { type: mime })); setPreview({ url, mime }); }
    }).catch(() => undefined);
    return () => { active = false; if (url) URL.revokeObjectURL(url); };
  }, [file]);
  function select(next: File | null) {
    if (busy || recording || microphonePending.current) return;
    if (!next && input.current) input.current.value = "";
    setPreview(null);
    if (next && (next.size < 1 || next.size > MAX_MEDIA_BYTES)) { setFile(null); setProgress(0); setMessage("Choose a nonempty file no larger than 2 GB."); if (input.current) input.current.value = ""; return; }
    setFile(next); setProgress(0);
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!file || operation.current || recording || microphonePending.current) return;
    const controller = new AbortController(); operation.current = controller; setBusy(true); setMessage("");
    let completed = false;
    try {
      const job = await prepareUpload(file, scope, title, description, localStorage); setDraft(job); setTitle(job.title); setDescription(job.description);
      await runMediaUpload(file, job, { storage: localStorage, signal: controller.signal, onProgress: (bytes, next) => { setProgress(Math.round(bytes / file.size * 100)); setPhase(next); } });
      completed = true; setFile(null); setPreview(null); setDraft(null); setTitle(""); setDescription(""); if (input.current) input.current.value = "";
      setMessage("Class material uploaded successfully.");
      try { await onUploaded(); } catch { setMessage("Class material uploaded successfully. History could not refresh; reload it to see the saved file."); }
    } catch (error) {
      setMessage(error instanceof DOMException && error.name === "AbortError" ? "Upload paused. Keep or reselect the original file, then resume." : error instanceof Error ? error.message : "Upload did not finish. Your file and draft are retained.");
    } finally { operation.current = null; setBusy(false); if (completed) { setProgress(0); setPhase(""); } }
  }
  function clearDraft() {
    if (busy || recording || microphonePending.current || !window.confirm("Clear the browser draft? This does not delete an already uploaded file. An unfinished private session may remain until cleanup.")) return;
    try { forgetUpload(scope, localStorage); setDraft(null); setInvalidDraft(false); setTitle(""); setDescription(""); select(null); if (input.current) input.current.value = ""; setMessage("Browser draft cleared. You can start a new upload."); }
    catch { setMessage("This browser did not allow the draft to be cleared. Check browser storage permissions."); }
  }
  async function startRecording() {
    if (busy || draft || recording || microphoneOutstanding.current) return;
    if (!navigator.mediaDevices?.getUserMedia || typeof MediaRecorder === "undefined") { setMessage("Recording is not supported here. Record using your device and choose the saved file."); return; }
    let stream: MediaStream | undefined;
    microphonePending.current = true;
    microphoneOutstanding.current = true; microphoneCancelled.current = false;
    setMicrophoneUnsettled(true);
    setRequestingMicrophone(true);
    setMessage("Waiting for microphone permission. Allow access to record, or decline to keep your selected file.");
    try {
      stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      if (!mounted.current || microphoneCancelled.current) { stream.getTracks().forEach(track => track.stop()); return; }
      const mime = ["audio/webm;codecs=opus", "audio/mp4", "audio/ogg;codecs=opus"].find(type => MediaRecorder.isTypeSupported(type));
      const current = new MediaRecorder(stream, mime ? { mimeType: mime } : undefined); recorder.current = current;
      const chunks: Blob[] = []; let bytes = 0; let limited = false; let interrupted = false;
      current.ondataavailable = event => { if (event.data.size) { chunks.push(event.data); bytes += event.data.size; if (bytes >= PREVIEW_BYTES && current.state !== "inactive") { limited = true; current.stop(); } } };
      current.onstop = () => {
        current.stream.getTracks().forEach(track => track.stop());
        if (!mounted.current) return;
        recorder.current = null; setRecording(false); setPaused(false);
        const type = current.mimeType || chunks[0]?.type || "application/octet-stream";
        const result = new File(chunks, `class-recording-${Date.now()}.${recordingExtension(type)}`, { type });
        setPreview(null); setFile(result.size ? result : null); setProgress(0);
        setMessage(interrupted
          ? (result.size ? "Recording was interrupted. Review the captured audio before uploading, or record again." : "Recording was interrupted and no audio was captured. Try again or choose a saved file.")
          : result.size ? (limited ? "Recording stopped at the browser memory limit. Review and upload it; use your device recorder for longer recordings." : "Recording ready. Review it, then confirm upload.") : "No audio was recorded. Try again or choose a saved file.");
      };
      current.onerror = () => {
        if (!mounted.current) return;
        interrupted = true;
        setMessage("Recording was interrupted. Review any captured audio before uploading.");
        // Native error may already set state inactive before final data/stop events.
        // Let onstop retain that final chunk and keep the interruption warning.
        if (current.state !== "inactive") current.stop(); else current.stream.getTracks().forEach(track => track.stop());
      };
      current.start(1000); if (input.current) input.current.value = ""; setFile(null); setPreview(null); setRecording(true); setPaused(false); setMessage("");
    } catch { stream?.getTracks().forEach(track => track.stop()); if (mounted.current && !microphoneCancelled.current) setMessage("Microphone access could not be started. Allow access or choose a recording from your device."); }
    finally { microphonePending.current = false; microphoneOutstanding.current = false; if (mounted.current) { setRequestingMicrophone(false); setMicrophoneUnsettled(false); } }
  }
  function cancelMicrophone() {
    if (!microphonePending.current) return;
    // getUserMedia has no abort signal. Ignore any late grant and stop its tracks.
    // Keep another recording request disabled until this browser request settles.
    microphoneCancelled.current = true; microphonePending.current = false;
    setRequestingMicrophone(false);
    setMessage("Microphone request cancelled. Your selected file and details are kept; you can upload a saved file. Dismiss any browser permission prompt before recording again.");
  }
  return <form className="learner-form teacher-upload-form" onSubmit={event => void submit(event)} aria-busy={busy}>
    <label>Attachment title (optional)<input name="title" maxLength={250} value={title} disabled={busy || Boolean(draft)} onChange={event => setTitle(event.target.value)} placeholder="Uses the filename when blank" /></label>
    <label>Comment (optional)<textarea name="description" maxLength={2000} value={description} disabled={busy || Boolean(draft)} onChange={event => setDescription(event.target.value)} /></label>
    <label className="teacher-file-picker"><input ref={input} type="file" disabled={busy || recording || requestingMicrophone} aria-label="Choose class attachment" onChange={event => select(event.target.files?.[0] ?? null)} /><span>{file?.name ?? "Choose a picture, audio, video or other attachment"}</span></label>
    <small>Up to 2 GB. Upload drafts stay in this browser; retain the original file to resume. Executable files and active web content are not supported.</small>
    {draft && <small>Saved draft: {draft.fileName}. Original title, comment and teaching scope are retained.</small>}
    {file && <div className="teacher-file-preview"><b>Ready to review · {(file.size / 1024 / 1024).toFixed(1)} MB</b>{preview && (preview.mime.startsWith("audio/") ? <audio controls src={preview.url} /> : preview.mime.startsWith("video/") ? <video controls playsInline src={preview.url} /> : <img src={preview.url} alt="Selected attachment" />)}<small>{file.size > PREVIEW_BYTES ? "Files over 32 MB are not previewed here. You can still upload this file and select Download after it is saved." : "If this device cannot preview the format, the original file can still be uploaded."}</small>{!draft && <button type="button" disabled={busy || requestingMicrophone} onClick={() => select(null)}>Remove file</button>}</div>}
    {(busy || progress > 0) && <div className="teacher-upload-progress"><label>{phase || "Upload progress"} · {progress}%<progress max={100} value={progress} /></label></div>}
    <div className="teacher-material-actions"><button type="submit" disabled={busy || recording || requestingMicrophone || invalidDraft || !file || !scope.ownerId}>{busy ? `${phase || "Preparing upload"}…` : draft ? "Resume upload" : "Confirm upload"}</button>{busy && <button type="button" className="enterprise-action-button enterprise-action-button-secondary" onClick={() => operation.current?.abort()}>Pause upload</button>}{!busy && (draft || invalidDraft) && <button type="button" className="enterprise-action-button enterprise-action-button-secondary" disabled={requestingMicrophone} onClick={clearDraft}>Clear saved draft</button>}
      {!recording ? <button type="button" className="enterprise-action-button enterprise-action-button-secondary" disabled={busy || microphoneUnsettled || Boolean(draft)} onClick={() => void startRecording()}>{requestingMicrophone ? "Waiting for microphone…" : "Record audio"}</button> : <><span>● {paused ? "Recording paused" : "Recording"}</span><button type="button" onClick={() => { const current = recorder.current; if (!current) return; if (current.state === "paused") { current.resume(); setPaused(false); } else { current.pause(); setPaused(true); } }}>{paused ? "Resume recording" : "Pause recording"}</button><button type="button" onClick={() => recorder.current?.stop()}>Stop recording</button></>}
      {requestingMicrophone && <button type="button" className="enterprise-action-button enterprise-action-button-secondary" onClick={cancelMicrophone}>Cancel microphone request</button>}
    </div>{message && <p role="status" aria-live="polite">{message}</p>}
  </form>;
}
