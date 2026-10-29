"use client";

import { ChangeEvent, useEffect, useRef, useState } from "react";
import { academyApi } from "@/lib/api";
import { studentImportErrors } from "@/lib/student-import-feedback";

type Academy = { id: string };
type ImportRow = { firstName: string; lastName: string; studentNumber: string | null; email: string | null; phone: string | null; admissionDate: null };
const exports = [["students", "Student register"], ["guardians", "Parent register"], ["enrollments", "Enrolment register"], ["invoices", "Invoice ledger"], ["payments", "Payment reconciliation ledger"]] as const;

export default function DataOperations() {
  const [academy, setAcademy] = useState<Academy>(); const [message, setMessage] = useState("Loading data operations…"); const [rows, setRows] = useState<ImportRow[]>([]); const [fileName, setFileName] = useState(""); const [validating, setValidating] = useState(false); const [importing, setImporting] = useState(false);
  const [fileLoading, setFileLoading] = useState(false); const [validated, setValidated] = useState(false);
  const pending = useRef(false); const fileVersion = useRef(0);
  const downloadPending = useRef(false);
  const [downloading, setDownloading] = useState<string>();
  useEffect(() => { void academyApi("/api/academies").then(async response => { if (!response.ok) throw Error(); const academies: Academy[] = await response.json(); setAcademy(academies[0]); setMessage(academies[0] ? "" : "No academy found."); }).catch(() => setMessage("Data operations could not be loaded.")); }, []);
  async function download(resource: string) {
    if (!academy || downloadPending.current) return;
    downloadPending.current = true; setDownloading(resource);
    let url: string | undefined;
    try {
      const response = await academyApi(`/api/academies/${academy.id}/exports/${resource}`);
      if (!response.ok || response.headers.get("content-type")?.split(";")[0].trim().toLowerCase() !== "text/csv")
        throw new Error("Export unavailable");
      const blob = await response.blob();
      url = URL.createObjectURL(blob);
      const link = document.createElement("a"); link.href = url; link.download = `academydesk-${resource}.csv`; link.click();
      setMessage("CSV download started. Formula-like text has a leading tab for spreadsheet safety; review before programmatic import.");
    } catch {
      setMessage("Export could not be downloaded. Check your connection and export access, then try again.");
    } finally {
      if (url) URL.revokeObjectURL(url);
      downloadPending.current = false; setDownloading(undefined);
    }
  }
  async function loadCsv(event: ChangeEvent<HTMLInputElement>) {
    if (pending.current) return;
    const file = event.target.files?.[0]; if (!file) return;
    const version = ++fileVersion.current; setFileLoading(true); setValidated(false); setRows([]); setFileName("");
    try {
      const text = await file.text(); if (version !== fileVersion.current) return;
      const [head, ...lines] = text.trim().split(/\r?\n/); const headers = head.split(",").map(value => value.trim());
      setRows(lines.filter(Boolean).map(line => { const values = line.split(",").map(value => value.trim()); return { firstName: values[headers.indexOf("FirstName")] || "", lastName: values[headers.indexOf("LastName")] || "", studentNumber: values[headers.indexOf("StudentNumber")] || null, email: values[headers.indexOf("Email")] || null, phone: values[headers.indexOf("Phone")] || null, admissionDate: null }; }));
      setFileName(file.name); setMessage("CSV loaded. Validate it before importing.");
    } catch { if (version === fileVersion.current) setMessage("CSV could not be read. Choose the file again."); }
    finally { if (version === fileVersion.current) setFileLoading(false); }
  }
  async function validate() {
    if (pending.current || fileLoading || !academy || !rows.length) return;
    pending.current = true; setValidating(true); setValidated(false); setMessage("Validating CSV…");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/imports/students/validate`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ rows }) });
      const result: unknown = await response.json(); const errors = studentImportErrors(result);
      if (!response.ok || !result || typeof result !== "object" || !("errors" in result) || !Array.isArray(result.errors) || !("rowCount" in result) || result.rowCount !== rows.length) return setMessage("CSV validation could not be confirmed. Your file has been retained.");
      if (result.errors.length) return setMessage(errors || "CSV validation could not be confirmed. Your file has been retained.");
      setValidated(true); setMessage("CSV validation passed. Database checks run during import.");
    } catch { setMessage("CSV validation could not be completed. Your file has been retained."); }
    finally { pending.current = false; setValidating(false); }
  }
  async function importRows() {
    if (pending.current || fileLoading || !validated || !academy || !rows.length) return;
    pending.current = true; setImporting(true); setMessage("Importing students…");
    try {
      const response = await academyApi(`/api/academies/${academy.id}/imports/students`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ rows }) });
      if (response.status >= 500) return setMessage("Import could not be confirmed. Review the student register before retrying; your file has been retained.");
      let result: unknown; try { result = await response.json(); } catch { result = null; }
      if (!response.ok) {
        const detail = studentImportErrors(result) || (result && typeof result === "object" && "message" in result && typeof result.message === "string" ? result.message : "Import was rejected.");
        return setMessage(`${detail} Your file has been retained.`);
      }
      if (!result || typeof result !== "object" || !("imported" in result) || result.imported !== rows.length) return setMessage("Import could not be confirmed. Review the student register before retrying; your file has been retained.");
      setMessage(`${result.imported} students imported.`); setRows([]); setFileName(""); setValidated(false);
    } catch { setMessage("Import could not be confirmed. Review the student register before retrying; your file has been retained."); }
    finally { pending.current = false; setImporting(false); }
  }
return <main className="enterprise-settings data-operations-standard"><header className="data-operations-heading"><span className="data-operations-icon" aria-hidden="true">⇄</span><div><p>Administration</p><h1>Data operations centre</h1></div></header>{message && <p className="data-operations-notice" role="status" aria-live="polite">{message}</p>}<section className="data-operations-kpis"><article><span>Export registers</span><b>{exports.length}</b><small>Download controlled academy data sets.</small></article><article><span>Import rows loaded</span><b>{rows.length}</b><small>{fileName || "No student CSV selected."}</small></article><article><span>Import safeguard</span><b>Validate</b><small>Validation is required before importing.</small></article></section><section className="data-export-panel"><header><div><p>Data exports</p><h2>Academy registers</h2><small>Download current CSV copies for analysis, audit, or a controlled migration.</small></div></header><div className="data-export-grid">{exports.map(([resource, title]) => <article key={resource}><span aria-hidden="true">↓</span><h3>{title}</h3><p>CSV export</p><button onClick={() => void download(resource)} disabled={!academy || downloading !== undefined} aria-busy={downloading === resource} className="enterprise-action-button enterprise-action-button-secondary">{downloading === resource ? "Preparing download…" : "Download CSV"}</button></article>)}</div></section><section className="data-import-panel"><header><div><p>Student import</p><h2>Validate &amp; import students</h2><small>Expected headers: FirstName, LastName, StudentNumber, Email, Phone.</small></div></header><div className="data-import-body"><label className="data-file-field"><span>Student CSV</span><input type="file" accept=".csv" disabled={validating || importing} onChange={event => void loadCsv(event)} /><b>{fileName || "Choose a CSV file"}</b></label>{rows.length > 0 && <div className="data-import-summary"><b>{rows.length} rows ready for validation</b><small>Review the result before running the final import.</small></div>}<div className="data-import-actions"><button onClick={() => void validate()} disabled={!academy || !rows.length || fileLoading || validating || importing} className="enterprise-action-button enterprise-action-button-secondary">{validating ? "Validating…" : "Validate CSV"}</button><button onClick={() => void importRows()} disabled={!academy || !rows.length || !validated || fileLoading || validating || importing} className="enterprise-action-button">{importing ? "Importing…" : "Import validated rows"}</button></div></div></section></main>;
}
