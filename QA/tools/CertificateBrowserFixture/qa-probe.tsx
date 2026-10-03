"use client";
import { useEffect, useRef, useState } from "react";
const api = process.env.NEXT_PUBLIC_API_URL!;
export default function Probe() {
  const mode = useRef("capture");
  const [message, setMessage] = useState("DOM-capture mode: not a PDF/native print test.");
  useEffect(() => {
    const native = window.print.bind(window);
    const printEvent = (event: Event) => { void fetch(api + "/qa/print", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ event: event.type, mode: mode.current }) }); };
    window.addEventListener("beforeprint", printEvent); window.addEventListener("afterprint", printEvent);
    window.print = () => {
      const article = document.querySelector<HTMLElement>(".certificate-preview");
      const rect = article?.getBoundingClientRect();
      const snapshot = { mode: mode.current, text: article?.innerText, state: article?.dataset.printState, className: article?.className, watermark: article ? getComputedStyle(article, "::after").content : null, width: rect?.width, height: rect?.height };
      void fetch(api + "/qa/print", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(snapshot) });
      setMessage("Captured real DOM at print call; mode: " + mode.current);
      if (mode.current === "throw") throw Error("Synthetic unavailable printer");
      if (mode.current === "native") native();
    };
    return () => { window.print = native; window.removeEventListener("beforeprint", printEvent); window.removeEventListener("afterprint", printEvent); };
  }, []);
  async function scenario(action: string) {
    const response = await fetch(api + "/qa/control", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ action }) });
    setMessage((await response.json()).message + ". Use Refresh records except when checking a fresh pre-print read.");
  }
  return <aside aria-label="Synthetic certificate QA controls" style={{ padding: 12, border: "2px dashed #176fb4", background: "#fff", color: "#172033" }}>
    <strong>Isolated certificate DOM probe — synthetic API, not Identity/SQL/full portal or PDF proof</strong>
    <details><summary>QA scenarios and print modes</summary>
      <div style={{ display: "flex", flexWrap: "wrap", gap: 8, padding: 10 }}>
        {["reset", "revoke", "replace", "read-fail", "recover", "issue-fail", "independent", "long-note"].map(action => <button key={action} type="button" onClick={() => void scenario(action)} style={{ border: "1px solid #176fb4", padding: 8 }}>QA {action}</button>)}
        {["capture", "native", "throw"].map(value => <button key={value} type="button" onClick={() => { mode.current = value; setMessage("Print mode: " + value + (value === "capture" ? "; records DOM only, does not print." : "")); }} style={{ border: "1px solid #176fb4", padding: 8 }}>QA print {value}</button>)}
        <button type="button" onClick={() => window.dispatchEvent(new Event("afterprint"))}>QA simulated afterprint</button>
      </div>
    </details>
    <p role="status">{message}</p>
  </aside>;
}
