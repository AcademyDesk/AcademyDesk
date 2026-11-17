import { academyApi, apiUrl } from "./api";

export const PREVIEW_BYTES = 32 * 1024 * 1024;
export const BUFFERED_DOWNLOAD_BYTES = 64 * 1024 * 1024;
const MAX_FILE_BYTES = 2_000_000_000;
const TICKET_RESPONSE_BYTES = 8192;
const ticketError = "The private download could not be prepared. Retry.";
const discardBody = (response: Response) => { void response.body?.cancel().catch(() => undefined); };
const validBytes = (value: number, maximum: number) => Number.isSafeInteger(value) && value > 0 && value <= maximum;

function contentLength(response: Response) {
  const raw = response.headers.get("Content-Length");
  if (raw === null) return undefined;
  const length = Number(raw);
  if (!/^[1-9]\d{0,9}$/.test(raw) || !validBytes(length, MAX_FILE_BYTES))
    throw Error("File size could not be verified. Contact your academy.");
  return length;
}

async function completeResponse(response: Response, path: string, signal: AbortSignal) {
  if (signal.aborted) { discardBody(response); signal.throwIfAborted(); }
  await requireMaterialResponse(response);
  if (signal.aborted) { discardBody(response); signal.throwIfAborted(); }
  if (response.status !== 200 || response.redirected || response.headers.has("Content-Range") ||
      response.url && response.url !== new URL(path, apiUrl).href) {
    discardBody(response);
    throw Error("The complete private response could not be verified. Retry.");
  }
  return response;
}

function decodedLength(response: Response, expected: number | undefined, limit: number) {
  try {
    // Fetch exposes decoded bytes. A compressed wire Content-Length cannot be
    // compared with those bytes; the decoded stream still has the strict cap.
    const encoding = response.headers.get("Content-Encoding");
    const declared = encoding && encoding.toLowerCase() !== "identity" ? undefined : contentLength(response);
    if (declared !== undefined && (declared > limit || expected !== undefined && declared !== expected))
      throw Error("The file changed or exceeded the safe memory limit. Retry.");
    return expected ?? declared;
  } catch (error) { discardBody(response); throw error; }
}

async function boundedBody(response: Response, length: number | undefined, limit: number, signal: AbortSignal) {
  if (!response.body) throw Error("File response has no content.");
  const reader = response.body.getReader();
  const chunks: Uint8Array<ArrayBuffer>[] = [];
  let total = 0;
  let result: Blob;
  const cancel = () => { void reader.cancel(signal.reason).catch(() => undefined); };
  signal.addEventListener("abort", cancel, { once: true });
  try {
    while (true) {
      signal.throwIfAborted();
      const result = await reader.read();
      signal.throwIfAborted();
      if (result.done) break;
      total += result.value.byteLength;
      if (total > limit || length !== undefined && total > length)
        throw Error("The file changed or exceeded the safe memory limit. Retry.");
      chunks.push(new Uint8Array(result.value));
    }
    if (length !== undefined && total !== length) throw Error("The download was interrupted. Retry.");
    signal.throwIfAborted();
    result = new Blob(chunks, { type: "application/octet-stream" });
  } finally {
    signal.removeEventListener("abort", cancel);
    await reader.cancel().catch(() => undefined);
    reader.releaseLock();
  }
  signal.throwIfAborted();
  return result;
}
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
  discardBody(response);
  throw Error(({ 401: "Sign in again to access this file.", 403: "Your account cannot access this file.", 404: "This file is no longer available to your account.", 429: "Wait a minute and try again.", 503: "Private storage is temporarily unavailable. Try again later." } as Record<number,string>)[response.status] ?? "The file could not be retrieved. Try again.");
}
export async function materialInfo(path: string, signal: AbortSignal) {
  signal.throwIfAborted();
  const contentPath = privateMaterialPath(path);
  if (!contentPath) throw Error("This is not an approved private material path.");
  const response = await completeResponse(await academyApi(contentPath, { method: "HEAD", signal, redirect: "error", cache: "no-store" }), contentPath, signal);
  let length: number | undefined;
  try { length = contentLength(response); } finally { discardBody(response); }
  if (length === undefined) throw Error("File size could not be verified. Contact your academy.");
  const disposition = response.headers.get("Content-Disposition") ?? "";
  let filename = "class-material";
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition)?.[1];
  const plain = /filename="([^"]+)"/i.exec(disposition)?.[1];
  try { filename = encoded ? decodeURIComponent(encoded) : plain ?? filename; } catch { /* safe default */ }
  signal.throwIfAborted();
  return { length, filename: filename.replace(/[\x00-\x1f\x7f/\\:]/g, "_").slice(0,250) };
}
export async function boundedMaterialBlob(path: string, length: number, limit: number, signal: AbortSignal, api = academyApi) {
  signal.throwIfAborted();
  const contentPath = privateMaterialPath(path);
  if (!contentPath || !validBytes(length, MAX_FILE_BYTES) || !validBytes(limit, BUFFERED_DOWNLOAD_BYTES))
    throw Error("This is not an approved private material or its size or memory limit is invalid.");
  if (length > limit) throw Error("This file is too large to preview here. Select Download to save the original file.");
  const response = await completeResponse(await api(contentPath, { signal, redirect: "error", cache: "no-store" }), contentPath, signal);
  return boundedBody(response, decodedLength(response, length, limit), limit, signal);
}

export async function nativeMaterialTicket(path: string, signal: AbortSignal, api = academyApi) {
  signal.throwIfAborted();
  if (!privateMaterialPath(path) || !path.startsWith("/")) throw Error("This is not an approved private material path.");
  const endpoint = "/api/class-material-downloads/tickets";
  const response = await completeResponse(await api(endpoint, {
    method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ path }), signal, redirect: "error", cache: "no-store",
  }), endpoint, signal);
  const body = await boundedBody(response, decodedLength(response, undefined, TICKET_RESPONSE_BYTES), TICKET_RESPONSE_BYTES, signal);
  const text = await body.text();
  signal.throwIfAborted();
  let result;
  try { result = JSON.parse(text); } catch { throw Error(ticketError); }
  signal.throwIfAborted();
  if (!result || typeof result !== "object" || Array.isArray(result) || Object.keys(result).length !== 2 ||
      typeof result.ticket !== "string" || !/^[a-zA-Z0-9_-]{1,3000}$/.test(result.ticket) ||
      result.expiresInSeconds !== 60) throw Error(ticketError);
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
