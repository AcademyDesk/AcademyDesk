"use client";
import { useEffect, useState } from "react";
const api = process.env.NEXT_PUBLIC_API_URL!;
export default function AppearanceProbe() {
  const [message, setMessage] = useState("Appearance QA: DOM capture only, not native print/PDF proof.");
  useEffect(() => {
    const native = window.print.bind(window);
    window.print = () => {
      const article = document.querySelector<HTMLElement>(".certificate-preview");
      const snapshot = { at: Date.now(), mode: "appearance-capture", text: article?.innerText, state: article?.dataset.printState, className: article?.className,
        images: Array.from(article?.querySelectorAll("img") ?? []).map(image => ({ src: image.src, complete: image.complete, naturalWidth: image.naturalWidth, naturalHeight: image.naturalHeight })) };
      void fetch(api + "/qa/print", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(snapshot) });
      setMessage("Captured certificate DOM and image readiness at print call; no PDF produced.");
    };
    return () => { window.print = native; };
  }, []);
  async function scenario(action: string) {
    const response = await fetch(api + "/qa/control", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ action }) });
    setMessage((await response.json()).message + ". Print directly for the fresh pre-print check; do not refresh first.");
  }
  return <aside aria-label="Synthetic certificate appearance QA" style={{ padding: 12, border: "2px dashed #176fb4", background: "white", color: "#172033" }}>
    <strong>Isolated synthetic logo/theme QA — not production storage or PDF proof</strong>
    <details><summary>QA appearance scenarios</summary>
      {["fresh-delayed-logo", "broken-logo", "no-logo"].map(action => <button key={action} type="button" onClick={() => void scenario(action)} style={{ margin: 8, padding: 8 }}>QA {action}</button>)}
    </details>
    <p role="status">{message}</p>
  </aside>;
}
