"use client";

import { CSSProperties, FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { apiUrl, academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Batch = { id: string; name: string };
type Branding = { academyName: string; logoUrl?: string | null; accentColor: string; signatoryName?: string | null };
type Certificate = { id: string; certificateNumber: string; studentId: string; title: string; templateKey: string; verificationCode: string; issuedDate: string; status: string };

const themes = [
  ["music-recital", "Recital Night", "Music"], ["music-conservatory", "Conservatory", "Music"], ["music-rhythm", "Rhythm & Beats", "Music"], ["music-spotlight", "Stage Spotlight", "Music"], ["music-symphony", "Symphony", "Music"],
  ["music-acoustic", "Acoustic Session", "Music"], ["music-vocal", "Vocal Excellence", "Music"], ["music-orchestra", "Orchestra", "Music"], ["music-virtuoso", "Virtuoso", "Music"], ["music-practice", "Practice Milestone", "Music"],
  ["school-honours", "School Honours", "School"], ["school-crest", "School Crest", "School"], ["school-scholar", "Scholar Award", "School"], ["school-merit", "Merit Medal", "School"], ["school-graduation", "Graduation", "School"],
  ["holiday-independence", "Independence Day", "Public holiday"], ["holiday-republic", "Republic Day", "Public holiday"], ["holiday-diwali", "Diwali", "Public holiday"], ["holiday-ganesh", "Ganesh Chaturthi", "Public holiday"], ["holiday-christmas", "Christmas", "Public holiday"], ["holiday-eid", "Eid Celebration", "Public holiday"],
] as const;

export default function CertificatesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [certificates, setCertificates] = useState<Certificate[]>([]);
  const [branding, setBranding] = useState<Branding>();
  const [studentId, setStudentId] = useState("");
  const [batchId, setBatchId] = useState("");
  const [title, setTitle] = useState("Certificate of Achievement");
  const [themeKey, setThemeKey] = useState("music-recital");
  const [issuedDate, setIssuedDate] = useState("");
  const [notes, setNotes] = useState("");
  const [status, setStatus] = useState("Loading certificates…");
  const [savingBrand, setSavingBrand] = useState(false);
  const fileInput = useRef<HTMLInputElement>(null);
  const selectedStudent = useMemo(() => students.find((student) => student.id === studentId), [students, studentId]);

  async function load(academyId?: string) {
    const id = academyId ?? academy?.id; if (!id) return;
    const [studentResponse, batchResponse, certificateResponse, brandingResponse] = await Promise.all([
      academyApi(`/api/academies/${id}/students`), academyApi(`/api/academies/${id}/batches`), academyApi(`/api/academies/${id}/certificates`), academyApi(`/api/academies/${id}/certificates/branding`),
    ]);
    if (!studentResponse.ok || !batchResponse.ok || !certificateResponse.ok || !brandingResponse.ok) throw new Error();
    const availableStudents: Student[] = await studentResponse.json();
    setStudents(availableStudents); setBatches(await batchResponse.json()); setCertificates(await certificateResponse.json()); setBranding(await brandingResponse.json());
    if (!studentId && availableStudents[0]) setStudentId(availableStudents[0].id); setStatus("");
  }
  useEffect(() => { void (async () => { try { const response = await academyApi("/api/academies"); const academies: Academy[] = await response.json(); if (!response.ok || !academies[0]) throw new Error(); setAcademy(academies[0]); await load(academies[0].id); } catch { setStatus("Certificates could not be loaded."); } })(); }, []);

  async function saveBranding(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy || !branding) return; setSavingBrand(true);
    try { const response = await academyApi(`/api/academies/${academy.id}/certificates/branding`, { method: "PUT", headers: apiHeaders(true), body: JSON.stringify({ accentColor: branding.accentColor, signatoryName: branding.signatoryName || null }) }); if (!response.ok) throw new Error(); setBranding(await response.json()); setStatus("Certificate branding saved."); }
    catch { setStatus("Certificate branding could not be saved."); } finally { setSavingBrand(false); }
  }
  async function uploadLogo(event: React.ChangeEvent<HTMLInputElement>) {
    const logo = event.target.files?.[0]; if (!academy || !logo) return; const body = new FormData(); body.append("logo", logo); setSavingBrand(true);
    try { const response = await academyApi(`/api/academies/${academy.id}/certificates/branding/logo`, { method: "POST", body }); if (!response.ok) throw new Error(); setBranding(await response.json()); setStatus("Academy logo uploaded."); }
    catch { setStatus("Upload a PNG, JPG or WebP logo smaller than 2.5 MB."); } finally { setSavingBrand(false); if (fileInput.current) fileInput.current.value = ""; }
  }
  async function issue(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy || !studentId) return;
    const response = await academyApi(`/api/academies/${academy.id}/certificates`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, batchId: batchId || null, title, templateKey: themeKey, issuedDate: issuedDate || null, notes: notes || null }) });
    if (!response.ok) return setStatus("Certificate could not be issued. Check the required details.");
    setNotes(""); setIssuedDate(""); setStatus("Certificate issued and ready to print."); await load();
  }
  const studentName = (id: string) => { const student = students.find((value) => value.id === id); return student ? `${student.firstName} ${student.lastName}` : "Student"; };
  const batchName = (id?: string | null) => batches.find((value) => value.id === id)?.name ?? "Independent programme";
  const logoUrl = branding?.logoUrl ? `${apiUrl}${branding.logoUrl}` : undefined;

  return <main className="enterprise-settings certificates-admin">
    <header className="enterprise-page-header"><p>Academy experience / recognition</p><h2>Certificates</h2><span>Branded, verifiable recognition for every academy.</span></header>
    {status && <p className="enterprise-page-state" role="status">{status}</p>}
    <section className="certificate-branding surface-panel mt-5 rounded-xl p-5"><div><h3>Academy certificate branding</h3></div><form onSubmit={saveBranding}>
      <div className="certificate-logo-slot">{logoUrl ? <img src={logoUrl} alt="Academy logo" /> : <span>{branding?.academyName?.slice(0, 1) ?? "A"}</span>}</div><input ref={fileInput} type="file" accept="image/png,image/jpeg,image/webp" onChange={uploadLogo} className="sr-only" />
      <button type="button" className="enterprise-action-button-secondary" onClick={() => fileInput.current?.click()} disabled={savingBrand}>Upload logo</button>
      <label>Accent colour<input type="color" value={branding?.accentColor ?? "#0F6CBD"} onChange={(event) => setBranding((current) => current ? { ...current, accentColor: event.target.value } : current)} /></label>
      <label>Authorised signatory<input value={branding?.signatoryName ?? ""} onChange={(event) => setBranding((current) => current ? { ...current, signatoryName: event.target.value } : current)} placeholder="Principal / Director name" /></label>
      <button className="enterprise-action-button" disabled={savingBrand}>{savingBrand ? "Saving…" : "Save branding"}</button>
    </form></section>
    <section className="mt-5 grid gap-5 xl:grid-cols-[.92fr_1.08fr]">
      <form onSubmit={issue} className="surface-panel rounded-xl p-5 certificate-issue-form"><h3>Issue certificate</h3>
        <select value={studentId} onChange={(event) => setStudentId(event.target.value)} required><option value="">Select student</option>{students.map((student) => <option key={student.id} value={student.id}>{student.firstName} {student.lastName}</option>)}</select>
        <select value={batchId} onChange={(event) => setBatchId(event.target.value)}><option value="">No class or batch</option>{batches.map((batch) => <option key={batch.id} value={batch.id}>{batch.name}</option>)}</select>
        <input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Certificate title" required /><input type="date" value={issuedDate} onChange={(event) => setIssuedDate(event.target.value)} />
        <label className="certificate-theme-picker">Certificate theme<select value={themeKey} onChange={(event) => setThemeKey(event.target.value)}><optgroup label="Music themes">{themes.filter(([, , category]) => category === "Music").map(([key, name]) => <option key={key} value={key}>{name}</option>)}</optgroup><optgroup label="School themes">{themes.filter(([, , category]) => category === "School").map(([key, name]) => <option key={key} value={key}>{name}</option>)}</optgroup><optgroup label="Public holiday themes">{themes.filter(([, , category]) => category === "Public holiday").map(([key, name]) => <option key={key} value={key}>{name}</option>)}</optgroup></select></label>
        <textarea value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Recognition note (optional)" /><button className="enterprise-action-button">Issue certificate</button>
      </form>
      <section className="surface-panel rounded-xl p-5 certificate-preview-panel"><div className="flex items-center justify-between gap-3"><h3>Certificate preview</h3><button type="button" className="enterprise-action-button-secondary" onClick={() => window.print()}>Print / save PDF</button></div>
        <article className={`certificate-preview theme-${themeKey}`} style={{ "--academy-certificate-accent": branding?.accentColor ?? "#0F6CBD" } as CSSProperties}><header>{logoUrl ? <img src={logoUrl} alt="Academy logo" /> : <span className="certificate-monogram">{branding?.academyName?.slice(0, 1) ?? "A"}</span>}<strong>{branding?.academyName ?? "Your Academy"}</strong></header><div className="certificate-theme-mark" aria-hidden="true" /><h4>{title || "Certificate of Achievement"}</h4><p className="certificate-presentation">This certificate is proudly presented to</p><h5>{selectedStudent ? `${selectedStudent.firstName} ${selectedStudent.lastName}` : "Student name"}</h5><p className="certificate-body">in recognition of achievement and dedication in <b>{batchName(batchId)}</b>.</p><footer><span>Issued {issuedDate || new Date().toLocaleDateString("en-IN", { day: "numeric", month: "long", year: "numeric" })}</span><span>{branding?.signatoryName || "Authorised signatory"}</span></footer></article>
      </section>
    </section>
    <section className="surface-panel mt-5 rounded-xl p-5"><div className="flex items-center justify-between gap-3"><h3>Issued certificates</h3><span className="text-sm text-slate-400">Each record has a unique verification code.</span></div>{certificates.length === 0 ? <p className="enterprise-settings-empty mt-4">No certificates have been issued yet.</p> : <div className="mt-4 overflow-x-auto"><table><thead><tr><th>Student</th><th>Certificate</th><th>Theme</th><th>Issued</th><th>Verification</th><th>Status</th></tr></thead><tbody>{certificates.map((certificate) => <tr key={certificate.id}><td>{studentName(certificate.studentId)}</td><td><b>{certificate.title}</b><small>{certificate.certificateNumber}</small></td><td>{themes.find(([key]) => key === certificate.templateKey)?.[1] ?? "Archived theme"}</td><td>{certificate.issuedDate}</td><td><code>{certificate.verificationCode}</code></td><td>{certificate.status}</td></tr>)}</tbody></table></div>}</section>
  </main>;
}
