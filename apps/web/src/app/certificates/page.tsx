"use client";

import { CSSProperties, FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { apiUrl, academyApi, apiHeaders } from "@/lib/api";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Batch = { id: string; name: string };
type Branding = { academyName: string; logoUrl?: string | null; accentColor: string; signatoryName?: string | null };
type Certificate = { id: string; certificateNumber: string; studentId: string; batchId?: string | null; title: string; templateKey: string; designKey: string; artworkX: number; artworkY: number; artworkSize: number; verificationCode: string; issuedDate: string; status: string; notes?: string | null };

const templates = [
  ["classic", "Classic Laurels", "Professional"], ["modern", "Modern Horizon", "Professional"], ["minimal", "Minimal Studio", "Professional"], ["navy", "Premium Navy", "Professional"], ["academic", "Academic Crest", "Professional"],
  ["gold", "Gold Achievement", "Medal"], ["silver", "Silver Achievement", "Medal"], ["bronze", "Bronze Achievement", "Medal"],
  ["performance", "Stage Performance", "Music & coaching"], ["completion", "Course Completion", "Music & coaching"], ["excellence", "Excellence Award", "Music & coaching"],
  ["independence-day", "Independence Day", "Seasonal"], ["republic-day", "Republic Day", "Seasonal"], ["diwali", "Diwali Celebration", "Seasonal"], ["ganesh-festival", "Ganesh Festival", "Seasonal"],
] as const;
const designs = [
  ["laurels", "Laurels", "❦"], ["music-notes", "Music notes", "♫"], ["medal-ribbon", "Medal ribbon", "✦"], ["starburst", "Starburst", "✺"], ["geometric", "Geometric", "◇"],
  ["academy-seal", "Academy seal", "A"], ["tricolour", "Tricolour ribbon", "✦"], ["diya", "Diya", "✧"], ["ganesh", "Ganesh motif", "ॐ"], ["celebration", "Celebration", "✹"],
] as const;
const placementPresets = [["Top left", 16, 17], ["Top centre", 50, 16], ["Top right", 84, 17], ["Centre left", 16, 50], ["Centre", 50, 50], ["Centre right", 84, 50], ["Bottom left", 16, 83], ["Bottom centre", 50, 84], ["Bottom right", 84, 83]] as const;

export default function CertificatesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [certificates, setCertificates] = useState<Certificate[]>([]);
  const [branding, setBranding] = useState<Branding>();
  const [studentId, setStudentId] = useState("");
  const [batchId, setBatchId] = useState("");
  const [title, setTitle] = useState("Certificate of Achievement");
  const [templateKey, setTemplateKey] = useState("classic");
  const [designKey, setDesignKey] = useState("laurels");
  const [artworkX, setArtworkX] = useState(50);
  const [artworkY, setArtworkY] = useState(30);
  const [artworkSize, setArtworkSize] = useState(72);
  const [issuedDate, setIssuedDate] = useState("");
  const [notes, setNotes] = useState("");
  const [status, setStatus] = useState("Loading certificates…");
  const [savingBrand, setSavingBrand] = useState(false);
  const fileInput = useRef<HTMLInputElement>(null);
  const selectedStudent = useMemo(() => students.find((student) => student.id === studentId), [students, studentId]);
  const selectedTemplate = templates.find(([key]) => key === templateKey) ?? templates[0];
  const selectedDesign = designs.find(([key]) => key === designKey) ?? designs[0];

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
    const response = await academyApi(`/api/academies/${academy.id}/certificates`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId, batchId: batchId || null, title, templateKey, designKey, artworkX, artworkY, artworkSize, issuedDate: issuedDate || null, notes: notes || null }) });
    if (!response.ok) return setStatus("Certificate could not be issued. Check the required details."); setNotes(""); setIssuedDate(""); setStatus("Certificate issued and ready to print."); await load();
  }
  const studentName = (id: string) => { const student = students.find((value) => value.id === id); return student ? `${student.firstName} ${student.lastName}` : "Student"; };
  const batchName = (id?: string | null) => batches.find((value) => value.id === id)?.name ?? "Independent programme";
  const logoUrl = branding?.logoUrl ? `${apiUrl}${branding.logoUrl}` : undefined;

  return <main className="enterprise-settings certificates-admin">
    <header className="enterprise-page-header"><p>Operations / recognition</p><h2>Certificates</h2><span>Branded, verifiable recognition for every academy.</span></header>
    {status && <p className="enterprise-page-state" role="status">{status}</p>}
    <section className="certificate-branding surface-panel mt-5 rounded-xl p-5"><div><h3>Academy certificate branding</h3><p>Use your academy logo and approved signatory on every certificate.</p></div><form onSubmit={saveBranding}>
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
        <div className="certificate-template-picker"><span>Choose a template</span><div>{templates.map(([key, name, group]) => <button type="button" key={key} data-selected={key === templateKey} className={`certificate-template-card template-${key}`} onClick={() => setTemplateKey(key)}><b>{name}</b><small>{group}</small></button>)}</div></div>
        <div className="certificate-design-picker"><span>Certificate artwork</span><div>{designs.map(([key, name, symbol]) => <button type="button" key={key} data-selected={key === designKey} onClick={() => setDesignKey(key)}><i className={`certificate-design-icon design-${key}`} aria-hidden="true">{symbol}</i><b>{name}</b></button>)}</div></div>
        <div className="certificate-placement-editor"><span>Artwork placement</span><div className="certificate-placement-presets">{placementPresets.map(([label, x, y]) => <button type="button" key={label} aria-label={label} title={label} data-selected={artworkX === x && artworkY === y} onClick={() => { setArtworkX(x); setArtworkY(y); }}><i /></button>)}</div><div className="certificate-placement-sliders"><label>Horizontal <output>{artworkX}%</output><input type="range" min="0" max="100" value={artworkX} onChange={(event) => setArtworkX(Number(event.target.value))} /></label><label>Vertical <output>{artworkY}%</output><input type="range" min="0" max="100" value={artworkY} onChange={(event) => setArtworkY(Number(event.target.value))} /></label><label>Size <output>{artworkSize}px</output><input type="range" min="36" max="180" value={artworkSize} onChange={(event) => setArtworkSize(Number(event.target.value))} /></label></div></div>
        <textarea value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Recognition note (optional)" /><button className="enterprise-action-button">Issue certificate</button>
      </form>
      <section className="surface-panel rounded-xl p-5 certificate-preview-panel"><div className="flex items-center justify-between gap-3"><h3>Certificate preview</h3><button type="button" className="enterprise-action-button-secondary" onClick={() => window.print()}>Print / save PDF</button></div>
        <article className={`certificate-preview template-${templateKey}`} data-design={designKey} style={{ "--academy-certificate-accent": branding?.accentColor ?? "#0F6CBD", "--certificate-artwork-x": `${artworkX}%`, "--certificate-artwork-y": `${artworkY}%`, "--certificate-artwork-size": `${artworkSize}px`, "--certificate-artwork-font-size": `${Math.max(18, Math.round(artworkSize * .46))}px` } as CSSProperties}><header>{logoUrl ? <img src={logoUrl} alt="Academy logo" /> : <span className="certificate-monogram">{branding?.academyName?.slice(0, 1) ?? "A"}</span>}<strong>{branding?.academyName ?? "Your Academy"}</strong></header><div className={`certificate-artwork design-${designKey}`} aria-hidden="true">{selectedDesign[2]}</div><p className="certificate-kicker">{selectedTemplate[2]}</p><h4>{title || "Certificate of Achievement"}</h4><p className="certificate-presentation">This certificate is proudly presented to</p><h5>{selectedStudent ? `${selectedStudent.firstName} ${selectedStudent.lastName}` : "Student name"}</h5><p className="certificate-body">in recognition of achievement and dedication in <b>{batchName(batchId)}</b>.</p><footer><span>Issued {issuedDate || new Date().toLocaleDateString("en-IN", { day: "numeric", month: "long", year: "numeric" })}</span><span>{branding?.signatoryName || "Authorised signatory"}</span></footer></article>
      </section>
    </section>
    <section className="surface-panel mt-5 rounded-xl p-5"><div className="flex items-center justify-between gap-3"><h3>Issued certificates</h3><span className="text-sm text-slate-400">Each record has a unique verification code.</span></div>{certificates.length === 0 ? <p className="enterprise-settings-empty mt-4">No certificates have been issued yet.</p> : <div className="mt-4 overflow-x-auto"><table><thead><tr><th>Student</th><th>Certificate</th><th>Template</th><th>Artwork</th><th>Issued</th><th>Verification</th><th>Status</th></tr></thead><tbody>{certificates.map((certificate) => <tr key={certificate.id}><td>{studentName(certificate.studentId)}</td><td><b>{certificate.title}</b><small>{certificate.certificateNumber}</small></td><td>{templates.find(([key]) => key === certificate.templateKey)?.[1] ?? certificate.templateKey}</td><td>{designs.find(([key]) => key === certificate.designKey)?.[1] ?? certificate.designKey}</td><td>{certificate.issuedDate}</td><td><code>{certificate.verificationCode}</code></td><td>{certificate.status}</td></tr>)}</tbody></table></div>}</section>
  </main>;
}
