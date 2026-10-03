// Bound preparation to the mounted image, including decode, and cancel on close/unmount.
export function waitForCertificateImage(image: HTMLImageElement, signal: AbortSignal, timeoutMs = 10000): Promise<void> {
  return new Promise((resolve, reject) => {
    let settled = false;
    let decoding = false;
    const finish = (error?: Error) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      image.removeEventListener("load", loaded);
      image.removeEventListener("error", failed);
      signal.removeEventListener("abort", cancelled);
      if (error) reject(error); else resolve();
    };
    const failed = () => finish(new Error("Certificate logo could not be loaded"));
    const cancelled = () => finish(new Error("Certificate image preparation cancelled"));
    const loaded = () => {
      if (settled || decoding) return;
      if (!image.complete || image.naturalWidth <= 0 || image.naturalHeight <= 0) { failed(); return; }
      decoding = true;
      if (typeof image.decode === "function") {
        void image.decode().then(() => finish(), failed);
      } else finish();
    };
    const timer = setTimeout(() => finish(new Error("Certificate logo loading timed out")), timeoutMs);
    image.addEventListener("load", loaded);
    image.addEventListener("error", failed);
    signal.addEventListener("abort", cancelled, { once: true });
    if (signal.aborted) cancelled(); else if (image.complete) loaded();
  });
}
