"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { academyApi, apiHeaders } from "@/lib/api";
import { EnterprisePageState } from "@/components/enterprise-page-state";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";

type Academy = { id: string };
type Person = { id: string; firstName: string; lastName: string };
type Document = { id: string; studentId?: string; guardianId?: string; documentType: string; fileName: string; secureReference?: string; expiryDate?: string; reviewedDate?: string; status: string; visibility: string };
type Consent = { id: string; studentId?: string; guardianId?: string; consentType: string; granted: boolean; evidenceReference?: string; recordedAtUtc: string; withdrawnAtUtc?: string };

const label = (person?: Person) => person ? `${person.firstName} ${person.lastName}` : "Unassigned record";
type PersonSelection = { personType: string; personId: string };
const emptySelection = (): PersonSelection => ({ personType: "student", personId: "" });
const sameId = (left: string, right: string) => left.toLowerCase() === right.toLowerCase();
const subjectLabel = (record: { studentId?: string; guardianId?: string }, students: Person[], guardians: Person[]) => {
  if (record.studentId && record.guardianId) return `Ambiguous subject · Student ${record.studentId} / Parent ${record.guardianId}`;
  const id = record.studentId || record.guardianId;
  if (!id) return "Unassigned record";
  const type = record.studentId ? "Student" : "Parent";
  const person = (record.studentId ? students : guardians).find(person => sameId(person.id, id));
  return person ? `${type} · ${label(person)}` : `${type} unavailable (${id})`;
};
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
  const [notice, setNotice] = useState<{ message: string; tone: "loading" | "success" | "error" }>({ message: "Loading compliance records…", tone: "loading" });
  const [ready, setReady] = useState(false);
  const [loaded, setLoaded] = useState(false);
  const [pending, setPending] = useState(false);
  const writeLock = useRef(false);
  const [documentPerson, setDocumentPerson] = useState<PersonSelection>(emptySelection);
  const [consentPerson, setConsentPerson] = useState<PersonSelection>(emptySelection);
  const [expiryDate, setExpiryDate] = useState("");
  const [visibility, setVisibility] = useState("AdminOnly");

  const load = useCallback(async (currentAcademy?: Academy): Promise<boolean> => {
    try {
      let current = currentAcademy;
      if (!current) {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error("Academy unavailable.");
        const academies: Academy[] = await response.json();
        if (!Array.isArray(academies) || !academies[0]?.id) throw new Error("No academy available.");
        current = academies[0];
      }
      const [documentsResponse, consentsResponse, studentsResponse, guardiansResponse] = await Promise.all([
        academyApi(`/api/academies/${current.id}/compliance/documents`), academyApi(`/api/academies/${current.id}/compliance/consents`),
        academyApi(`/api/academies/${current.id}/students`), academyApi(`/api/academies/${current.id}/guardians`),
      ]);
      if ([documentsResponse, consentsResponse, studentsResponse, guardiansResponse].some(response => !response.ok)) throw new Error("Records unavailable.");
      const [nextDocuments, nextConsents, nextStudents, nextGuardians] = await Promise.all([documentsResponse.json(), consentsResponse.json(), studentsResponse.json(), guardiansResponse.json()]);
      if ([nextDocuments, nextConsents, nextStudents, nextGuardians].some(rows => !Array.isArray(rows))) throw new Error("Invalid records.");
      setAcademy(current); setDocuments(nextDocuments); setConsents(nextConsents);
      setStudents(nextStudents); setGuardians(nextGuardians); setReady(true); setLoaded(true);
      return true;
    } catch { setReady(false); return false; }
  }, []);
  useEffect(() => {
    void Promise.resolve().then(() => load()).then(ok => setNotice({ message: ok ? "" : "Compliance records could not be loaded. Try refreshing records.", tone: ok ? "success" : "error" }));
  }, [load]);

  async function refresh() {
    if (writeLock.current) return;
    writeLock.current = true; setPending(true);
    try {
      const ok = await load(academy);
      setNotice({ message: ok ? "Compliance records refreshed." : "Compliance records could not be loaded. Try refreshing records.", tone: ok ? "success" : "error" });
    } finally { writeLock.current = false; setPending(false); }
  }

  async function mutate(url: string, body: object | undefined, success: string, failure: string, afterSave?: () => void) {
    if (!academy || !ready || writeLock.current) return;
    writeLock.current = true; setPending(true);
    try {
      const response = await academyApi(url, { method: afterSave ? "POST" : "PATCH", headers: apiHeaders(true), ...(body ? { body: JSON.stringify(body) } : {}) });
      if (!response.ok) { setNotice({ message: failure, tone: "error" }); return; }
      afterSave?.();
      const refreshed = await load(academy);
      setNotice({ message: success + (refreshed ? "" : " Records could not be refreshed. The save succeeded; refresh records before making another change."), tone: "success" });
    } catch { setReady(false); setNotice({ message: failure + " The result could not be confirmed. Check your connection and refresh records before retrying.", tone: "error" }); }
    finally { writeLock.current = false; setPending(false); }
  }

  function validPerson(selection: PersonSelection) {
    const people = selection.personType === "student" ? students : selection.personType === "guardian" ? guardians : [];
    if (!selection.personId || !people.some(person => sameId(person.id, selection.personId))) {
      setNotice({ message: "Choose a valid Student or Parent before saving.", tone: "error" }); return false;
    }
    return true;
  }

  async function addDocument(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy || !ready || writeLock.current || !validPerson(documentPerson)) return;
    const element = event.currentTarget;
    const form = new FormData(element);
    await mutate(`/api/academies/${academy.id}/compliance/documents`, { studentId: documentPerson.personType === "student" ? documentPerson.personId : null, guardianId: documentPerson.personType === "guardian" ? documentPerson.personId : null, documentType: form.get("documentType"), fileName: form.get("fileName"), secureReference: form.get("secureReference"), expiryDate: expiryDate || null, visibility }, "Document registered for review.", "Document record could not be saved.", () => {
      element.reset(); setDocumentPerson(emptySelection()); setExpiryDate(""); setVisibility("AdminOnly");
    });
  }
  async function addConsent(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!academy || !ready || writeLock.current || !validPerson(consentPerson)) return;
    const element = event.currentTarget;
    const form = new FormData(element);
    await mutate(`/api/academies/${academy.id}/compliance/consents`, { studentId: consentPerson.personType === "student" ? consentPerson.personId : null, guardianId: consentPerson.personType === "guardian" ? consentPerson.personId : null, consentType: form.get("consentType"), granted: true, evidenceReference: form.get("evidenceReference") }, "Consent evidence recorded.", "Consent evidence could not be saved.", () => { element.reset(); setConsentPerson(emptySelection()); });
  }
  async function review(document: Document, status: string) {
    if (!academy) return;
    await mutate(`/api/academies/${academy.id}/compliance/documents/${document.id}/review`, { status }, `Document marked ${status}.`, "Document review could not be updated.");
  }
  async function withdraw(consent: Consent) {
    if (!academy) return;
    await mutate(`/api/academies/${academy.id}/compliance/consents/${consent.id}/withdraw`, undefined, "Consent withdrawn and retained in the evidence ledger.", "Consent could not be withdrawn.");
  }

  return <main className="enterprise-settings compliance-standard">
    <header className="enterprise-page-header"><p>Administration / compliance</p><h2>Compliance control centre</h2><span>Evidence-led document, consent, expiry and safeguarding controls for this academy.</span></header>
    {notice.message && <EnterprisePageState tone={notice.tone}>{notice.message}</EnterprisePageState>}
    <button type="button" onClick={refresh} disabled={pending} className="mt-3 rounded border px-4 py-2">Refresh records</button>
    <section className="mt-5 grid gap-5 xl:grid-cols-2">
      <form onSubmit={addDocument} className="surface-panel rounded-xl p-5"><h3 className="font-semibold">Register document</h3><p className="mt-1 text-sm text-slate-400">Record the secure document reference; file storage stays with your approved storage provider.</p><fieldset disabled={!ready || pending}><PersonPicker students={students} guardians={guardians} selection={documentPerson} onChange={setDocumentPerson} disabled={!ready || pending} /><input required name="documentType" className="field mt-3" placeholder="Document type — e.g. ID proof"/><input required name="fileName" className="field mt-3" placeholder="File name"/><input name="secureReference" className="field mt-3" placeholder="Secure storage reference"/><div className="compliance-document-controls"><StandardDateField name="expiryDate" value={expiryDate} onChange={setExpiryDate} label="Expiry date" /><StandardSelectField name="visibility" value={visibility} onChange={setVisibility} disabled={!ready || pending} placeholder="Choose visibility" options={[{ value: "AdminOnly", label: "Administrators only" }, { value: "StaffRestricted", label: "Restricted staff" }]} /></div><button disabled={!ready || pending} className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950">Register for review</button></fieldset></form>
      <form onSubmit={addConsent} className="surface-panel rounded-xl p-5"><h3 className="font-semibold">Record consent</h3><p className="mt-1 text-sm text-slate-400">Use a traceable consent type and retain the signed/electronic evidence reference.</p><fieldset disabled={!ready || pending}><PersonPicker students={students} guardians={guardians} selection={consentPerson} onChange={setConsentPerson} disabled={!ready || pending} /><input required name="consentType" className="field mt-3" placeholder="Consent type — e.g. photo and media"/><input name="evidenceReference" className="field mt-3" placeholder="Evidence reference / signed form ID"/><button disabled={!ready || pending} className="mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950">Record consent</button></fieldset></form>
    </section>
    <section className="surface-panel mt-5 rounded-xl p-5"><div className="flex items-center justify-between"><h3 className="font-semibold">Document review queue</h3><span className="text-sm text-slate-400">{loaded ? `${documents.length} records` : "Records unavailable"}</span></div><div className="mt-4 overflow-x-auto"><table className="min-w-full text-left text-sm"><thead className="text-slate-400"><tr><th className="pb-3 pr-4">Document</th><th className="pb-3 pr-4">Subject</th><th className="pb-3 pr-4">Expiry / state</th><th className="pb-3 pr-4">Visibility</th><th className="pb-3">Review</th></tr></thead><tbody>{documents.map(document => <tr key={document.id} className="border-t border-slate-800"><td className="py-3 pr-4"><b>{document.documentType}</b><br/><span className="text-slate-400">{document.fileName}</span></td><td className="py-3 pr-4">{subjectLabel(document, students, guardians)}</td><td className="py-3 pr-4">{documentState(document)}{document.expiryDate && <><br/><span className="text-slate-400">{document.expiryDate}</span></>}</td><td className="py-3 pr-4">{document.visibility}</td><td className="py-3 whitespace-nowrap"><button disabled={!ready || pending} onClick={() => review(document, "Approved")} className="mr-2 text-cyan-300">Approve</button><button disabled={!ready || pending} onClick={() => review(document, "Rejected")} className="text-rose-300">Reject</button></td></tr>)}</tbody></table>{loaded && !documents.length && <p className="py-5 text-slate-400">No document records yet.</p>}</div></section>
    <section className="surface-panel mt-5 rounded-xl p-5"><div className="flex items-center justify-between"><h3 className="font-semibold">Consent evidence ledger</h3><span className="text-sm text-slate-400">Withdrawal events remain auditable</span></div><div className="mt-4 overflow-x-auto"><table className="min-w-full text-left text-sm"><thead className="text-slate-400"><tr><th className="pb-3 pr-4">Consent</th><th className="pb-3 pr-4">Subject</th><th className="pb-3 pr-4">Evidence</th><th className="pb-3 pr-4">Recorded</th><th className="pb-3">Status</th></tr></thead><tbody>{consents.map(consent => <tr key={consent.id} className="border-t border-slate-800"><td className="py-3 pr-4 font-medium">{consent.consentType}</td><td className="py-3 pr-4">{subjectLabel(consent, students, guardians)}</td><td className="py-3 pr-4 text-slate-400">{consent.evidenceReference || "Not recorded"}</td><td className="py-3 pr-4">{new Date(consent.recordedAtUtc).toLocaleDateString()}</td><td className="py-3">{consent.granted ? <button disabled={!ready || pending} onClick={() => withdraw(consent)} className="text-amber-300">Granted · withdraw</button> : <span className="text-rose-300">Withdrawn</span>}</td></tr>)}</tbody></table>{loaded && !consents.length && <p className="py-5 text-slate-400">No consent evidence yet.</p>}</div></section>
  </main>;
}

function PersonPicker({ students, guardians, selection, onChange, disabled }: { students: Person[]; guardians: Person[]; selection: PersonSelection; onChange: (selection: PersonSelection) => void; disabled: boolean }) {
  const people = selection.personType === "student" ? students : selection.personType === "guardian" ? guardians : [];
  return <div className="compliance-person-picker">
    <StandardSelectField name="personType" value={selection.personType} onChange={value => onChange({ personType: value, personId: "" })} disabled={disabled} placeholder="Choose record type" options={[{ value: "student", label: "Student" }, { value: "guardian", label: "Parent" }]} />
    <StandardSelectField name="personId" value={selection.personId} onChange={value => onChange({ ...selection, personId: value })} disabled={disabled || !selection.personType} placeholder={selection.personType === "guardian" ? "Select parent" : "Select student"} options={people.map(person => ({ value: person.id, label: label(person) }))} />
  </div>;
}
