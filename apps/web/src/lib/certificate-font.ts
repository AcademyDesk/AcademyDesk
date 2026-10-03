// Wait for layout/font loading to settle, without allowing a slow font to hold the UI forever.
// FontFaceSet.ready may settle with browser fallback fonts; it is not glyph/PDF-embedding proof.
export function waitForCertificateFonts(fonts: Pick<FontFaceSet, "ready"> | undefined, signal: AbortSignal, timeoutMs = 10000): Promise<void> {
  return new Promise((resolve, reject) => {
    let settled = false;
    const finish = (error?: Error) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      signal.removeEventListener("abort", cancelled);
      if (error) reject(error); else resolve();
    };
    const cancelled = () => finish(new Error("Certificate font preparation cancelled"));
    const timer = setTimeout(() => finish(new Error("Certificate font loading timed out")), timeoutMs);
    signal.addEventListener("abort", cancelled, { once: true });
    if (signal.aborted) { cancelled(); return; }
    try {
      void Promise.resolve(fonts?.ready).then(() => finish(), () => finish(new Error("Certificate font preparation failed")));
    } catch { finish(new Error("Certificate font preparation failed")); }
  });
}
