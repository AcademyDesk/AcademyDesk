"use client";
import { useEffect, useRef, useState } from "react";
import { boundedMaterialBlob, externalMaterialUrl, handoffNativeMaterial, materialInfo, nativeMaterialTicket, PREVIEW_BYTES, previewMime, privateMaterialPath } from "@/lib/private-material";

export function PrivateMaterialActions({ url, title }: { url: string; title: string }) {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [preview, setPreview] = useState<{ url: string; mime: string } | null>(null);
  const operation = useRef<AbortController | null>(null);
  const objectUrl = useRef<string | null>(null);
  const nativeTargets = useRef<Array<() => void>>([]);
  useEffect(() => () => { operation.current?.abort(); if (objectUrl.current) URL.revokeObjectURL(objectUrl.current); nativeTargets.current.forEach(cleanup => cleanup()); nativeTargets.current = []; }, [url]);
  function closePreview() { if (objectUrl.current) URL.revokeObjectURL(objectUrl.current); objectUrl.current = null; setPreview(null); }
  const path = privateMaterialPath(url);
  if (!path) { const external = externalMaterialUrl(url); return external ? <a href={external} target="_blank" rel="noreferrer">Open link</a> : null; }
  async function act(mode: "preview" | "save") {
    if (!path || operation.current) return;
    const controller = new AbortController(); operation.current = controller; setBusy(true); setMessage("");
    try {
      const info = await materialInfo(path, controller.signal);
      if (mode === "save") {
        const ticket = await nativeMaterialTicket(path, controller.signal);
        controller.signal.throwIfAborted();
        nativeTargets.current.push(handoffNativeMaterial(path, ticket));
        setMessage("Download requested. Check your browser’s downloads; retry if it does not start.");
      } else {
        const blob = await boundedMaterialBlob(path, info.length, PREVIEW_BYTES, controller.signal);
        const mime = previewMime(new Uint8Array(await blob.slice(0,32).arrayBuffer()));
        controller.signal.throwIfAborted();
        if (!mime) throw Error("This format cannot be previewed safely here. Download the original file instead.");
        closePreview(); const next = URL.createObjectURL(new Blob([blob], { type: mime })); objectUrl.current = next; setPreview({ url: next, mime });
      }
    } catch (error) {
      setMessage(error instanceof DOMException && error.name === "AbortError" ? "File action cancelled." : error instanceof Error ? error.message : "File action failed. Try again.");
    } finally { operation.current = null; setBusy(false); }
  }
  return <div className="private-material-actions">
    <div><button type="button" className="enterprise-action-button enterprise-action-button-secondary" disabled={busy} onClick={() => void act("preview")}>Preview</button><button type="button" className="enterprise-action-button enterprise-action-button-secondary" disabled={busy} onClick={() => void act("save")}>{busy ? "Working…" : "Download"}</button>{busy && <button type="button" onClick={() => operation.current?.abort()}>Cancel</button>}</div>
    {preview && <div className="private-material-preview">{preview.mime.startsWith("image/") ? <img src={preview.url} alt={title} onError={() => setMessage("This device cannot preview the image. Download the original file.")} /> : preview.mime.startsWith("audio/") ? <audio controls src={preview.url} onError={() => setMessage("This device cannot play this recording. Download the original file.")} /> : <video controls playsInline src={preview.url} onError={() => setMessage("This device cannot play this recording. Download the original file.")} />}<button type="button" onClick={closePreview}>Close preview</button></div>}
    {message && <p role="status">{message}</p>}
  </div>;
}
