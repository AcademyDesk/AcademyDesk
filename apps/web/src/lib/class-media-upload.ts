import { academyApi, apiUrl } from "./api";

export const MAX_MEDIA_BYTES = 2_000_000_000;
export const MEDIA_CHUNK_BYTES = 8_388_608;
export type UploadScope = { ownerId: string; batchId: string; studentId: string | null; classSessionId: string | null };
export type PendingMediaUpload = {
  version: 1; requestId: string; scope: UploadScope; fileName: string; length: number;
  fingerprint: string; title: string; description: string; hashes: Record<string, string>;
};
type StorageLike = Pick<Storage, "getItem" | "setItem" | "removeItem">;
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
export function uploadKey(scope: UploadScope) {
  return `academydesk.pendingMedia.v1:${apiUrl}:${scope.ownerId}:${scope.batchId}:${scope.studentId ?? ""}:${scope.classSessionId ?? ""}`;
}
export function pendingUpload(scope: UploadScope, storage: StorageLike): PendingMediaUpload | null {
  const raw = storage.getItem(uploadKey(scope));
  if (!raw) return null;
  const value = JSON.parse(raw) as PendingMediaUpload;
  if (value.version !== 1 || !guid.test(value.requestId) || JSON.stringify(value.scope) !== JSON.stringify(scope) ||
      typeof value.fileName !== "string" || !Number.isSafeInteger(value.length) || value.length < 1 || value.length > MAX_MEDIA_BYTES ||
      typeof value.fingerprint !== "string" || typeof value.title !== "string" || typeof value.description !== "string" ||
      !value.hashes || typeof value.hashes !== "object") throw Error("Saved upload details are invalid. Clear this draft before starting again.");
  return value;
}
export function forgetUpload(scope: UploadScope, storage: StorageLike) { storage.removeItem(uploadKey(scope)); }
async function digest(blob: Blob) {
  return Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256", await blob.arrayBuffer())), b => b.toString(16).padStart(2, "0")).join("");
}
export async function prepareUpload(file: File, scope: UploadScope, title: string, description: string, storage: StorageLike) {
  if (!guid.test(scope.ownerId) || !guid.test(scope.batchId)) throw Error("Select an assigned class and sign in before uploading.");
  if (file.size < 1 || file.size > MAX_MEDIA_BYTES) throw Error("Choose a nonempty file no larger than 2 GB.");
  if (file.name.length > 250 || /[\x00-\x1f\x7f/\\:]/.test(file.name) || /\.(exe|com|bat|cmd|ps1|msi|dll|scr|hta|js|html?|svg)$/i.test(file.name.trim()))
    throw Error("Executable files, active web content and unsafe filenames are not supported.");
  if (title.length > 250 || description.length > 2000) throw Error("Shorten the title or comment before uploading.");
  const fingerprint = await digest(new Blob([file.slice(0, 16384), file.slice(Math.max(0, file.size - 16384))]));
  const existing = pendingUpload(scope, storage);
  if (existing) {
    if (existing.fileName !== file.name || existing.length !== file.size || existing.fingerprint !== fingerprint)
      throw Error(`Reselect the original file “${existing.fileName}” to resume, or clear the saved draft first.`);
    return existing; // Immutable original title/comment are retained on retry/reload.
  }
  const job: PendingMediaUpload = { version: 1, requestId: crypto.randomUUID(), scope, fileName: file.name,
    length: file.size, fingerprint, title: title.trim() || file.name, description: description.trim(), hashes: {} };
  storage.setItem(uploadKey(scope), JSON.stringify(job)); // Fail before HTTP if durable local save is unavailable.
  return job;
}
export async function mediaResponse(response: Response) {
  const body: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    const message = body && typeof body === "object" && "message" in body && typeof body.message === "string" ? body.message : null;
    throw Error(message ?? ({ 401: "Your login needs renewal. Sign in and resume this upload.", 403: "Your current account cannot perform this action.",
      404: "This file or teaching assignment is no longer available.", 429: "Too many requests. Wait a minute, then resume.",
      503: "Private media storage is unavailable. Your file and draft are retained." } as Record<number, string>)[response.status] ?? "The request did not finish. Keep your file and retry the same upload.");
  }
  if (!body || typeof body !== "object") throw Error("The server returned an unexpected result. Keep your file and resume to check its status.");
  return body as Record<string, unknown>;
}
export async function runMediaUpload(file: File, job: PendingMediaUpload, options: {
  storage: StorageLike; signal: AbortSignal; onProgress: (bytes: number, phase: string) => void;
  api?: typeof academyApi;
}) {
  const api = options.api ?? academyApi;
  const call = (path: string, init: RequestInit = {}) => api(path, { ...init, signal: options.signal, redirect: "error", cache: "no-store" });
  const created = await mediaResponse(await call("/api/teacher/media-uploads", { method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ clientRequestId: job.requestId, batchId: job.scope.batchId, studentId: job.scope.studentId,
      classSessionId: job.scope.classSessionId, fileName: job.fileName, length: job.length, title: job.title, description: job.description || null, type: "Attachment" }) }));
  if (typeof created.id !== "string" || !guid.test(created.id) || created.length !== file.size || created.fileName !== job.fileName.trim() || created.chunkBytes !== MEDIA_CHUNK_BYTES)
    throw Error("Server upload details do not match this file. Keep the draft and contact support.");
  const path = `/api/teacher/media-uploads/${created.id}`;
  const progress = await mediaResponse(await call(path));
  const count = Math.ceil(file.size / MEDIA_CHUNK_BYTES);
  if (!Array.isArray(progress.uploadedBlocks) || !progress.uploadedBlocks.every(n => Number.isInteger(n) && n >= 0 && n < count))
    throw Error("Server upload progress is invalid. Keep the draft and contact support.");
  const uploaded = new Set<number>(progress.uploadedBlocks);
  let bytes = 0;
  for (let i = 0; i < count; i++) {
    options.signal.throwIfAborted();
    const chunk = file.slice(i * MEDIA_CHUNK_BYTES, Math.min(file.size, (i + 1) * MEDIA_CHUNK_BYTES));
    const hash = await digest(chunk);
    options.signal.throwIfAborted();
    if (uploaded.has(i)) {
      // Sampling is only a lookup aid. Every skipped block must match its saved
      // full-block digest, including an ambiguous last-request outcome.
      if (job.hashes[i] !== hash) throw Error("The selected file differs from the saved upload. Reselect the original file; existing bytes were not overwritten.");
    } else {
      job.hashes[i] = hash;
      options.storage.setItem(uploadKey(job.scope), JSON.stringify(job));
      await mediaResponse(await call(`${path}/chunks/${i}`, { method: "PUT", headers: { "Content-Type": "application/octet-stream" }, body: chunk }));
    }
    bytes += chunk.size; options.onProgress(bytes, "Uploading");
  }
  options.onProgress(bytes, "Confirming upload");
  const completed = await mediaResponse(await call(`${path}/complete`, { method: "POST" }));
  if (completed.id !== created.id || completed.url !== `/api/class-media/${created.id}/content`) throw Error("Upload outcome is uncertain. Resume the same draft to check completion.");
  forgetUpload(job.scope, options.storage);
  return completed;
}
export function recordingExtension(mime: string) {
  const type = mime.split(";")[0].toLowerCase();
  return ({ "audio/mp4": "m4a", "video/mp4": "mp4", "audio/ogg": "ogg", "video/ogg": "ogv", "audio/webm": "webm", "video/webm": "webm", "audio/wav": "wav" } as Record<string, string>)[type] ?? "bin";
}
