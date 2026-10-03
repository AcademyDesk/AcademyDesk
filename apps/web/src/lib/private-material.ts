import { academyApi, apiUrl } from "./api";

export const PREVIEW_BYTES = 32 * 1024 * 1024;
export const BUFFERED_DOWNLOAD_BYTES = 64 * 1024 * 1024;
export function privateMaterialPath(url: string) {
  let path = url;
  if (!url.startsWith("/")) {
    try { const target = new URL(url); if (target.origin !== new URL(apiUrl).origin || target.search || target.hash) return null; path = target.pathname; } catch { return null; }
  }
  return /^\/api\/class-media\/[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\/content$/.test(path) || /^\/uploads\/(teacher-materials|learning-resources)\/[0-9a-f]{32}\.[a-zA-Z0-9]+$/.test(path) ? path : null;
}
export function externalMaterialUrl(url: string) {
  try { const target = new URL(url); return ["https:", "http:"].includes(target.protocol) ? target.href : null; } catch { return null; }
}
export function previewMime(bytes: Uint8Array): string | null {
  const text = (start: number, length: number) => String.fromCharCode(...bytes.slice(start, start + length));
  if (bytes[0] === 255 && bytes[1] === 216 && bytes[2] === 255) return "image/jpeg";
  if ([137,80,78,71,13,10,26,10].every((b, i) => bytes[i] === b)) return "image/png";
  if (["GIF87a", "GIF89a"].includes(text(0, 6))) return "image/gif";
  if (text(0,4) === "RIFF" && text(8,4) === "WEBP") return "image/webp";
  if (text(0,4) === "RIFF" && text(8,4) === "WAVE") return "audio/wav";
  if (text(0,4) === "OggS") return "audio/ogg";
  if (text(0,4) === "fLaC") return "audio/flac";
  if (text(0,3) === "ID3" || (bytes[0] === 255 && (bytes[1] & 0xe0) === 0xe0)) return "audio/mpeg";
  if ([26,69,223,163].every((b, i) => bytes[i] === b)) return "video/webm";
  if (text(4,4) === "ftyp") return "video/mp4";
  return null; // No active HTML/SVG/PDF/document embedding or extension-based trust.
}
export async function requireMaterialResponse(response: Response) {
  if (response.ok) return response;
  throw Error(({ 401: "Sign in again to access this file.", 403: "Your account cannot access this file.", 404: "This file is no longer available to your account.", 429: "Wait a minute and try again.", 503: "Private storage is temporarily unavailable. Try again later." } as Record<number,string>)[response.status] ?? "The file could not be retrieved. Try again.");
}
export async function materialInfo(path: string, signal: AbortSignal) {
  if (!privateMaterialPath(path)) throw Error("This is not an approved private material path.");
  const response = await requireMaterialResponse(await academyApi(path, { method: "HEAD", signal, redirect: "error", cache: "no-store" }));
  const length = Number(response.headers.get("Content-Length"));
  if (!Number.isSafeInteger(length) || length <= 0 || length > 2_000_000_000) throw Error("File size could not be verified. Contact your academy.");
  const disposition = response.headers.get("Content-Disposition") ?? "";
  let filename = "class-material";
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition)?.[1];
  const plain = /filename="([^"]+)"/i.exec(disposition)?.[1];
  try { filename = encoded ? decodeURIComponent(encoded) : plain ?? filename; } catch { /* safe default */ }
  return { length, filename: filename.replace(/[\x00-\x1f\x7f/\\:]/g, "_").slice(0,250) };
}
export async function boundedMaterialBlob(path: string, length: number, limit: number, signal: AbortSignal, api = academyApi) {
  if (!privateMaterialPath(path) || length < 1) throw Error("This is not an approved private material or its size is invalid.");
  if (length > limit) throw Error("This file is too large to preview here. Select Download to save the original file.");
  const response = await requireMaterialResponse(await api(path, { signal, redirect: "error", cache: "no-store" }));
  if (!response.body) throw Error("File response has no content.");
  const reader = response.body.getReader(); const chunks: Uint8Array<ArrayBuffer>[] = []; let total = 0;
  try {
    while (true) {
      signal.throwIfAborted(); const result = await reader.read(); if (result.done) break;
      total += result.value.byteLength;
      if (total > length || total > limit) throw Error("The file changed or exceeded the safe memory limit. Retry.");
      chunks.push(new Uint8Array(result.value));
    }
    if (total !== length) throw Error("The download was interrupted. Retry.");
    return new Blob(chunks, { type: "application/octet-stream" });
  } finally { await reader.cancel().catch(() => undefined); reader.releaseLock(); }
}

export async function nativeMaterialTicket(path: string, signal: AbortSignal, api = academyApi) {
  if (!privateMaterialPath(path) || !path.startsWith("/")) throw Error("This is not an approved private material path.");
  const response = await requireMaterialResponse(await api("/api/class-material-downloads/tickets", {
    method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ path }), signal, redirect: "error", cache: "no-store",
  }));
  const result = await response.json();
  signal.throwIfAborted();
  if (!result || typeof result !== "object" || typeof result.ticket !== "string" || !/^[a-zA-Z0-9_-]{1,3000}$/.test(result.ticket) ||
      result.expiresInSeconds !== 60) throw Error("The private download could not be prepared. Retry.");
  return result.ticket as string;
}

// Native attachment responses go to the browser's download manager, not a
// JavaScript Blob. Only this short-lived file credential is in the POST body.
export function handoffNativeMaterial(path: string, ticket: string, doc = document) {
  if (!privateMaterialPath(path) || !path.startsWith("/") || !/^[a-zA-Z0-9_-]{1,3000}$/.test(ticket)) throw Error("Invalid private download.");
  const target = new URL(path, apiUrl);
  const base = new URL(apiUrl);
  if (target.origin !== base.origin || (base.protocol !== "https:" && !(base.protocol === "http:" && ["localhost", "127.0.0.1", "[::1]"].includes(base.hostname)))) throw Error("Private downloads require HTTPS.");
  const frame = doc.createElement("iframe"); frame.name = `material-download-${crypto.randomUUID()}`;
  frame.hidden = true; frame.title = "Private attachment download"; frame.referrerPolicy = "strict-origin-when-cross-origin";
  const form = doc.createElement("form"); form.method = "POST"; form.action = target.href;
  form.target = frame.name; form.enctype = "application/x-www-form-urlencoded"; form.hidden = true;
  const field = doc.createElement("input"); field.type = "hidden"; field.name = "ticket"; field.value = ticket;
  form.appendChild(field); doc.body.appendChild(frame); doc.body.appendChild(form);
  try { form.submit(); } catch (error) { frame.remove(); throw error; }
  finally { field.value = ""; form.remove(); }
  // Keep the target alive while response headers arrive; the caller cleans it
  // on unmount. Do not advertise handoff as confirmed disk persistence.
  return () => frame.remove();
}
