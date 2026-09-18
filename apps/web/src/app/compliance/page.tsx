"use client";

import { useEffect, useMemo, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { EnterprisePageState } from "@/components/enterprise-page-state";

type Academy = { id: string };
type Person = { id: string; firstName: string; lastName: string };
type Document = { id: string; studentId?: string; guardianId?: string; documentType: string; fileName: string; secureReference?: string; expiryDate?: string; reviewedDate?: string; status: string; visibility: string };
type Consent = { id: string; studentId?: string; guardianId?: string; consentType: string; granted: boolean; evidenceReference?: string; recordedAtUtc: string; withdrawnAtUtc?: string };

const label = (person?: Person) => person ? `${person.firstName} ${person.lastName}` : "Unassigned record";
const documentState = (document: Document) => {
  if (!document.expiryDate) return document.status;
  const days = Math.ceil((new Date(`${document.expiryDate}T00:00:00`).getTime() - Date.now()) / 86_400_000);
  return days < 0 ? "Expired" : days <= 30 ? `Expires in ${days} days` : document.status;
};

export default function ComplianceCentre() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Person[]>([]);
  const [guardians, setGuardians] = useState<Person[]>([]);
  const [documents, setDocuments] = useState<Document[]>([]);
  const [consents, setConsents] = useState<Consent[]>([]);
  const [message, setMessage] = useState("Loading compliance records…");
  const people = useMemo(() => [...students, ...guardians], [students, guardians]);

  async function load() {
    try {
      const academies: Academy[] = await (await academyApi("/api/academies", { cache: "no-store" })).json();
      if (!academies[0]) throw new Error("No academy available.");
      const current = academies[0]; setAcademy(current);
      const [documentsResponse, consentsResponse, studentsResponse, guardiansResponse] = await Promise.all([
        academyApi(`/api/academies/${current.id}/compliance/documents`), academyApi(`/api/academies/${current.id}/compliance/consents`),
        academyApi(`/api/academies/${current.id}/students`), academyApi(`/api/academies/${current.id}/guardians`),
      ]);
      setDocuments(await documentsResponse.json()); setConsents(await consentsResponse.json());
      setStudents(await studentsResponse.json()); setGuardians(await guardiansResponse.json()); setMessage("");
    } catch { setMessage("Compliance records could not be loaded."); }
  }
  useEffect(() => { void load(); }, []);

  async function addDocument(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy) return;
    const form = new FormData(event.currentTarget);
    const response = await academyApi(`/api/academies/${academy.id}/compliance/documents`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId: form.get("personType") === "student" ? form.get("personId") : null, guardianId: form.get("personType") === "guardian" ? form.get("personId") : null, documentType: form.get("documentType"), fileName: form.get("fileName"), secureReference: form.get("secureReference"), expiryDate: form.get("expiryDate") || null, visibility: form.get("visibility") }) });
    if (!response.ok) return setMessage("Document record could not be saved.");
    event.currentTarget.reset(); setMessage("Document registered for review."); await load();
  }
  async function addConsent(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy) return;
    const form = new FormData(event.currentTarget);
    const response = await academyApi(`/api/academies/${academy.id}/compliance/consents`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ studentId: form.get("personType") === "student" ? form.get("personId") : null, guardianId: form.get("personType") === "guardian" ? form.get("personId") : null, consentType: form.get("consentType"), granted: true, evidenceReference: form.get("evidenceReference") }) });
    if (!response.ok) return setMessage("Consent evidence could not be saved.");
    event.currentTarget.reset(); setMessage("Consent evidence recorded."); await load();
  }
  async function review(document: Document, status: string) {
    if (!academy) return;
    const response = await academyApi(`/api/academies/${academy.id}/compliance/documents/${document.id}/review`, { method: "PATCH", headers: apiHeaders(true), body: JSON.stringify({ status }) });
    if (!response.ok) return setMessage("Document review could not be updated."); setMessage(`Document marked ${status}.`); await load();
  }
  async function withdraw(consent: Consent) {
    if (!academy) return;
    const response = await academyApi(`/api/academies/${academy.id}/compliance/consents/${consent.id}/withdraw`, { method: "PATCH", headers: apiHeaders(true) });
    if (!response.ok) return setMessage("Consent could not be withdrawn."); setMessage("Consent withdrawn and retained in the evidence ledger."); await load();
  }

  return <main className="enterprise-settings">
    <header className="enterprise-page-header"><p>Administration / compliance</p><h2>Compliance control centre</h2><span>Evidence-led document, consent, expiry and safeguarding controls for this academy.</span></header>
    {message && <EnterprisePageState tone={message.includes("could not") ? "error" : message.includes("registered") || message.includes("recorded") || message.includes("marked") || message.includes("withdrawn") ? "success" : "loading"}>{message}</EnterprisePageState>}
    <section className="mt-5 grid gap-5 xl:grid-cols-2">
      <form onSubmit={addDocument} className="surface-panel rounded-xl p-5"><h3 className="font-semibold">Register document</h3><p className="mt-1 text-sm text-slate-400">Record the secure document reference; file storage stays with your approved storage provider.</p><PersonPicker people={people} /><input required name="documentType" className="field mt-3" placeholder="Document type — e.g. ID proof"/><input required name="fileName" className="field mt-3" placeholder="File name"/><input name="secureReference" className="field mt-3" placeholder="Secure storage reference"/><div className="mt-3 grid gap-3 md:grid-cols-2"><input name="expiryDate" type="date" className="field"/><select name="visibility" className="field"><option>AdminOnly</option><option>StaffRestricted</option></select></div><button className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950">Register for review</button></form>
      <form onSubmit={addConsent} className="surface-panel rounded-xl p-5"><h3 className="font-semibold">Record consent</h3><p className="mt-1 text-sm text-slate-400">Use a traceable consent type and retain the signed/electronic evidence reference.</p><PersonPicker people={people} /><input required name="consentType" className="field mt-3" placeholder="Consent type — e.g. photo and media"/><input name="evidenceReference" className="field mt-3" placeholder="Evidence reference / signed form ID"/><button className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950">Record consent</button></form>
    </section>
    <section className="surface-panel mt-5 rounded-xl p-5"><div className="flex items-center justify-between"><h3 className="font-semibold">Document review queue</h3><span className="text-sm text-slate-400">{documents.length} records</span></div><div className="mt-4 overflow-x-auto"><table className="min-w-full text-left text-sm"><thead className="text-slate-400"><tr><th className="pb-3 pr-4">Document</th><th className="pb-3 pr-4">Subject</th><th className="pb-3 pr-4">Expiry / state</th><th className="pb-3 pr-4">Visibility</th><th className="pb-3">Review</th></tr></thead><tbody>{documents.map(document => <tr key={document.id} className="border-t border-slate-800"><td className="py-3 pr-4"><b>{document.documentType}</b><br/><span className="text-slate-400">{document.fileName}</span></td><td className="py-3 pr-4">{label(people.find(person => person.id === (document.studentId || document.guardianId)))}</td><td className="py-3 pr-4">{documentState(document)}{document.expiryDate && <><br/><span className="text-slate-400">{document.expiryDate}</span></>}</td><td className="py-3 pr-4">{document.visibility}</td><td className="py-3 whitespace-nowrap"><button onClick={() => review(document, "Approved")} className="mr-2 text-cyan-300">Approve</button><button onClick={() => review(document, "Rejected")} className="text-rose-300">Reject</button></td></tr>)}</tbody></table>{!documents.length && <p className="py-5 text-slate-400">No document records yet.</p>}</div></section>
    <section className="surface-panel mt-5 rounded-xl p-5"><div className="flex items-center justify-between"><h3 className="font-semibold">Consent evidence ledger</h3><span className="text-sm text-slate-400">Withdrawal events remain auditable</span></div><div className="mt-4 overflow-x-auto"><table className="min-w-full text-left text-sm"><thead className="text-slate-400"><tr><th className="pb-3 pr-4">Consent</th><th className="pb-3 pr-4">Subject</th><th className="pb-3 pr-4">Evidence</th><th className="pb-3 pr-4">Recorded</th><th className="pb-3">Status</th></tr></thead><tbody>{consents.map(consent => <tr key={consent.id} className="border-t border-slate-800"><td className="py-3 pr-4 font-medium">{consent.consentType}</td><td className="py-3 pr-4">{label(people.find(person => person.id === (consent.studentId || consent.guardianId)))}</td><td className="py-3 pr-4 text-slate-400">{consent.evidenceReference || "Not recorded"}</td><td className="py-3 pr-4">{new Date(consent.recordedAtUtc).toLocaleDateString()}</td><td className="py-3">{consent.granted ? <button onClick={() => withdraw(consent)} className="text-amber-300">Granted · withdraw</button> : <span className="text-rose-300">Withdrawn</span>}</td></tr>)}</tbody></table>{!consents.length && <p className="py-5 text-slate-400">No consent evidence yet.</p>}</div></section>
  </main>;
}

function PersonPicker({ people }: { people: Person[] }) { return <div className="mt-4 grid gap-3 md:grid-cols-2"><select name="personType" className="field"><option value="student">Student</option><option value="guardian">Guardian</option></select><select required name="personId" className="field"><option value="">Select person…</option>{people.map(person => <option key={person.id} value={person.id}>{label(person)}</option>)}</select></div>; }
