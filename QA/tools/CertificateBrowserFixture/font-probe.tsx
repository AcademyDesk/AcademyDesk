"use client";
import { useEffect, useRef, useState } from "react";
const api = process.env.NEXT_PUBLIC_API_URL!;
export default function FontProbe() {
  const sequence = useRef(0);
  const faces = useRef<FontFace[]>([]);
  const [message, setMessage] = useState("Font QA: capture only, not native print/PDF proof.");
  useEffect(() => {
    const native = window.print.bind(window);
    window.print = () => {
      const article = document.querySelector<HTMLElement>(".certificate-preview");
      const snapshot = { at: Date.now(), mode: "font-capture", text: article?.innerText, state: article?.dataset.printState,
        fontStatus: document.fonts.status, faces: faces.current.map(face => ({ family: face.family, status: face.status })),
        images: Array.from(article?.querySelectorAll("img") ?? []).map(image => ({ src: image.src, complete: image.complete, naturalWidth: image.naturalWidth })),
        articleFont: article ? getComputedStyle(article).fontFamily : null };
      void fetch(api + "/qa/print", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(snapshot) });
      setMessage("Captured real certificate DOM/font state at print call; no PDF produced.");
    };
    return () => { window.print = native; faces.current.forEach(face => document.fonts.delete(face)); document.documentElement.style.removeProperty("--font-geist-sans"); };
  }, []);
  function slowFont() {
    const family = "QA Certificate Geist " + ++sequence.current;
    const face = new FontFace(family, `url(${api}/qa/font-slow-${sequence.current}.woff2)`, { weight: "100 900", display: "swap" });
    faces.current.push(face); document.fonts.add(face);
    // QA-only font variable injection, not a mutation through browser.evaluate.
    document.documentElement.style.setProperty("--font-geist-sans", `"${family}"`);
    void face.load().then(() => setMessage("Real Geist Latin font loaded: " + family), () => setMessage("QA font could not load: " + family));
    setMessage("Real Geist Latin font loading. Print immediately to test preparation ordering.");
  }
  return <aside aria-label="Synthetic certificate font QA" style={{ padding: 12, border: "2px dashed #176fb4", background: "white", color: "#172033" }}>
    <strong>Isolated font readiness QA — synthetic API, actual cached Geist Latin bytes</strong>
    <button type="button" onClick={slowFont} style={{ margin: 8, padding: 8 }}>QA load slow font</button>
    <button type="button" onClick={() => { void fetch(api + "/qa/control", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ action: "fresh-delayed-logo" }) }).then(response => { if (response.ok) slowFont(); else setMessage("Combined QA scenario failed to prepare"); }); }} style={{ margin: 8, padding: 8 }}>QA load slow font and logo</button>
    <p role="status">{message}</p>
  </aside>;
}
