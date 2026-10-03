"use client";

import {
  CSSProperties,
  FormEvent,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { WorkspaceNav } from "@/components/workspace-nav";
import { EnterprisePageState } from "@/components/enterprise-page-state";
import {
  StandardDateField,
  StandardSelectField,
} from "@/components/design-system/controls";
import { apiUrl, academyApi, apiHeaders } from "@/lib/api";
import { waitForCertificateImage } from "@/lib/certificate-image";
import { waitForCertificateFonts } from "@/lib/certificate-font";

type Academy = { id: string };
type Student = { id: string; firstName: string; lastName: string };
type Batch = { id: string; name: string };
type Enrollment = { studentId: string; batchId: string; status: string | null };
type Branding = {
  academyName: string;
  logoUrl?: string | null;
  accentColor: string;
  signatoryName?: string | null;
};
type Certificate = {
  id: string;
  certificateNumber: string;
  studentId: string;
  batchId: string | null;
  title: string;
  templateKey: string;
  verificationCode: string;
  issuedDate: string;
  status: string;
  notes: string | null;
};
type CertificateView = { certificate: Certificate; branding: Branding; studentName: string; batchName: string };

const themes = [
  ["music-recital", "Recital Night", "Music"],
  ["music-conservatory", "Conservatory", "Music"],
  ["music-rhythm", "Rhythm & Beats", "Music"],
  ["music-spotlight", "Stage Spotlight", "Music"],
  ["music-symphony", "Symphony", "Music"],
  ["music-acoustic", "Acoustic Session", "Music"],
  ["music-vocal", "Vocal Excellence", "Music"],
  ["music-orchestra", "Orchestra", "Music"],
  ["music-virtuoso", "Virtuoso", "Music"],
  ["music-practice", "Practice Milestone", "Music"],
  ["school-honours", "School Honours", "School"],
  ["school-crest", "School Crest", "School"],
  ["school-scholar", "Scholar Award", "School"],
  ["school-merit", "Merit Medal", "School"],
  ["school-graduation", "Graduation", "School"],
  ["holiday-independence", "Independence Day", "Public holiday"],
  ["holiday-republic", "Republic Day", "Public holiday"],
  ["holiday-diwali", "Diwali", "Public holiday"],
  ["holiday-ganesh", "Ganesh Chaturthi", "Public holiday"],
  ["holiday-christmas", "Christmas", "Public holiday"],
  ["holiday-eid", "Eid Celebration", "Public holiday"],
] as const;

const sameId = (left: string | null | undefined, right: string | null | undefined) =>
  typeof left === "string" && typeof right === "string" && left.length > 0 && left.toLowerCase() === right.toLowerCase();
const eligibleCertificateBatches = (studentId: string, batches: Batch[], enrollments: Enrollment[]) =>
  batches.filter((batch) => enrollments.some((enrollment) =>
    sameId(enrollment.studentId, studentId) && sameId(enrollment.batchId, batch.id) &&
    typeof enrollment.status === "string" && ["active", "completed"].includes(enrollment.status.toLowerCase()),
  ));

const certificateView = (certificate: Certificate | undefined, students: Student[], batches: Batch[], branding: Branding | undefined): CertificateView | undefined => {
  if (!certificate || !branding || ![certificate.id, certificate.certificateNumber, certificate.verificationCode, certificate.title].every((value) => typeof value === "string" && value.trim().length > 0) ||
    typeof certificate.status !== "string" || certificate.status.toLowerCase() !== "issued" ||
    !themes.some(([key]) => key === certificate.templateKey) || typeof certificate.issuedDate !== "string" ||
    !/^\d{4}-\d{2}-\d{2}$/.test(certificate.issuedDate) ||
    !Number.isFinite(Date.parse(certificate.issuedDate + "T00:00:00Z")) ||
    new Date(certificate.issuedDate + "T00:00:00Z").toISOString().slice(0, 10) !== certificate.issuedDate ||
    !(certificate.notes === null || typeof certificate.notes === "string") ||
    !(certificate.batchId === null || typeof certificate.batchId === "string" && certificate.batchId.length > 0) ||
    typeof branding.academyName !== "string" || !branding.academyName.trim() || typeof branding.accentColor !== "string" || !/^#[0-9a-f]{6}$/i.test(branding.accentColor) ||
    !(branding.signatoryName == null || typeof branding.signatoryName === "string") || !(branding.logoUrl == null || typeof branding.logoUrl === "string")) return undefined;
  const student = students.find((value) => sameId(value?.id, certificate.studentId));
  const batch = certificate.batchId === null ? undefined : batches.find((value) => sameId(value?.id, certificate.batchId));
  if (!student || typeof student.firstName !== "string" || typeof student.lastName !== "string" || !`${student.firstName} ${student.lastName}`.trim() ||
    certificate.batchId !== null && (!batch || typeof batch.name !== "string" || !batch.name.trim())) return undefined;
  return { certificate: { ...certificate }, branding: { ...branding }, studentName: `${student.firstName} ${student.lastName}`.trim(), batchName: batch?.name ?? "Independent programme" };
};

export default function CertificatesPage() {
  const [academy, setAcademy] = useState<Academy>();
  const [students, setStudents] = useState<Student[]>([]);
  const [batches, setBatches] = useState<Batch[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [certificates, setCertificates] = useState<Certificate[]>([]);
  const [branding, setBranding] = useState<Branding>();
  const [savedBranding, setSavedBranding] = useState<Branding>();
  const [printCertificateId, setPrintCertificateId] = useState("");
  const [printView, setPrintView] = useState<CertificateView>();
  const printStarted = useRef<CertificateView | undefined>(undefined);
  const [selection, setSelection] = useState({ studentId: "", batchId: "" });
  const { studentId, batchId } = selection;
  const [title, setTitle] = useState("Certificate of Achievement");
  const [themeKey, setThemeKey] = useState("music-recital");
  const [issuedDate, setIssuedDate] = useState("");
  const [notes, setNotes] = useState("");
  const [status, setStatus] = useState("Loading certificates…");
  const [savingBrand, setSavingBrand] = useState(false);
  const [ready, setReady] = useState(false);
  const [loaded, setLoaded] = useState(false);
  const [issuing, setIssuing] = useState(true);
  const [issueNotice, setIssueNotice] = useState<{ message: string; tone: "success" | "error" }>({ message: "", tone: "success" });
  const issueLock = useRef(true);
  const fileInput = useRef<HTMLInputElement>(null);
  const printLogo = useRef<HTMLImageElement>(null);
  const printArticle = useRef<HTMLElement>(null);
  const selectedStudent = useMemo(
    () => students.find((student) => student.id === studentId),
    [students, studentId],
  );
  const eligibleBatches = useMemo(
    () => eligibleCertificateBatches(studentId, batches, enrollments),
    [studentId, batches, enrollments],
  );
  const load = useCallback(async (currentAcademy?: Academy): Promise<boolean> => {
    try {
      let current = currentAcademy;
      if (!current) {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!Array.isArray(academies) || !academies[0]?.id) throw new Error();
        current = academies[0];
      }
      const responses = await Promise.all([
        academyApi(`/api/academies/${current.id}/students`, { cache: "no-store" }),
        academyApi(`/api/academies/${current.id}/batches`, { cache: "no-store" }),
        academyApi(`/api/academies/${current.id}/enrollments`, { cache: "no-store" }),
        academyApi(`/api/academies/${current.id}/certificates`, { cache: "no-store" }),
        academyApi(`/api/academies/${current.id}/certificates/branding`, { cache: "no-store" }),
      ]);
      if (responses.some((response) => !response.ok)) throw new Error();
      const [availableStudents, availableBatches, availableEnrollments, availableCertificates, availableBranding] =
        await Promise.all(responses.map((response) => response.json())) as [Student[], Batch[], Enrollment[], Certificate[], Branding];
      if ([availableStudents, availableBatches, availableEnrollments, availableCertificates].some((rows) => !Array.isArray(rows)) || !availableBranding || typeof availableBranding.academyName !== "string") throw new Error();
      setAcademy(current); setStudents(availableStudents); setBatches(availableBatches);
      setEnrollments(availableEnrollments); setCertificates(availableCertificates); setBranding(availableBranding);
      setSavedBranding({ ...availableBranding });
      setSelection((previous) => {
        const nextStudentId = availableStudents.find((student) => sameId(student.id, previous.studentId))?.id ?? availableStudents[0]?.id ?? "";
        const nextBatchId = sameId(nextStudentId, previous.studentId)
          ? eligibleCertificateBatches(nextStudentId, availableBatches, availableEnrollments).find((batch) => sameId(batch.id, previous.batchId))?.id ?? ""
          : "";
        return { studentId: nextStudentId, batchId: nextBatchId };
      });
      setReady(true); setLoaded(true);
      return true;
    } catch {
      setReady(false);
      return false;
    }
  }, []);
  useEffect(() => {
    void Promise.resolve().then(() => load()).then((ok) => {
      setStatus(ok ? "" : "Certificates could not be loaded. Try refreshing records.");
      issueLock.current = false; setIssuing(false);
    });
  }, [load]);
  const savedPreview = certificateView(certificates.find((value) => sameId(value?.id, printCertificateId)), students, batches, savedBranding);
  const preview = printView ?? savedPreview;
  function closePrint() {
    setPrintView(undefined); printStarted.current = undefined;
    issueLock.current = false; setIssuing(false);
    setIssueNotice({ message: "Print dialog closed. Your device controls whether a PDF is saved or a page is printed.", tone: "success" });
  }
  useEffect(() => {
    if (!printView) return;
    let active = true;
    const controller = new AbortController();
    const finish = () => {
      active = false; controller.abort();
      setPrintView(undefined); printStarted.current = undefined;
      issueLock.current = false; setIssuing(false);
      setIssueNotice({ message: "Print dialog closed. Your device controls whether a PDF is saved or a page is printed.", tone: "success" });
    };
    window.addEventListener("afterprint", finish);
    void Promise.resolve().then(async () => {
      if (!active || printStarted.current === printView) return;
      if (printView.branding.logoUrl) {
        try {
          if (!printLogo.current) throw new Error("Certificate logo is missing");
          await waitForCertificateImage(printLogo.current, controller.signal);
        } catch {
          if (!active) return;
          finish(); setIssueNotice({ message: "The academy logo could not be loaded. Nothing was printed. Check your connection and try again.", tone: "error" });
          return;
        }
      }
      if (!active || printStarted.current === printView) return;
      try {
        // Resolve styles/layout for the verified text before reading the current font-ready promise.
        printArticle.current?.getBoundingClientRect();
        await waitForCertificateFonts(document.fonts, controller.signal);
      } catch {
        if (!active) return;
        finish(); setIssueNotice({ message: "Certificate fonts could not finish loading. Nothing was printed. Check your connection and try again.", tone: "error" });
        return;
      }
      if (!active || printStarted.current === printView) return;
      printStarted.current = printView;
      try { window.print(); } catch {
        finish(); setIssueNotice({ message: "Print dialog could not be opened. Try again on your device.", tone: "error" });
      }
    });
    return () => { active = false; controller.abort(); window.removeEventListener("afterprint", finish); };
  }, [printView]);
  async function printCertificate() {
    if (!academy || !ready || issueLock.current || savingBrand || !savedPreview) return;
    const selectedId = printCertificateId;
    issueLock.current = true; setIssuing(true); setPrintView(undefined);
    setIssueNotice({ message: "Verifying the saved certificate before printing…", tone: "success" });
    let prepared = false;
    try {
      const responses = await Promise.all(["certificates", "students", "batches", "certificates/branding"].map((path) => academyApi(`/api/academies/${academy.id}/${path}`, { cache: "no-store" })));
      if (responses.some((response) => !response.ok)) throw new Error();
      const [currentCertificates, currentStudents, currentBatches, currentBranding] = await Promise.all(responses.map((response) => response.json())) as [Certificate[], Student[], Batch[], Branding];
      if (![currentCertificates, currentStudents, currentBatches].every(Array.isArray)) throw new Error();
      const current = certificateView(currentCertificates.find((value) => sameId(value?.id, selectedId)), currentStudents, currentBatches, currentBranding);
      if (!current) {
        setIssueNotice({ message: "This certificate is no longer available for printing. It may be revoked, replaced or missing required saved details. Refresh records.", tone: "error" });
        return;
      }
      // React commits this verified snapshot before the effect opens the dialog.
      prepared = true; setPrintView(current);
    } catch {
      setIssueNotice({ message: "The saved certificate could not be verified. Nothing was printed. Try again after checking your connection and access.", tone: "error" });
    } finally { if (!prepared) { issueLock.current = false; setIssuing(false); } }
  }
  async function refresh() {
    if (issueLock.current) return;
    issueLock.current = true; setIssuing(true);
    try {
      const refreshed = await load(academy);
      setStatus("");
      setIssueNotice({ message: refreshed ? "Certificate records refreshed. Review the student and class selection before issuing." : "Certificate records could not be loaded. Try refreshing records.", tone: refreshed ? "success" : "error" });
    } finally { issueLock.current = false; setIssuing(false); }
  }
  async function saveBranding(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !branding || issueLock.current) return;
    setSavingBrand(true);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/certificates/branding`,
        {
          method: "PUT",
          headers: apiHeaders(true),
          body: JSON.stringify({
            accentColor: branding.accentColor,
            signatoryName: branding.signatoryName || null,
          }),
        },
      );
      if (!response.ok) throw new Error();
      setBranding(await response.json());
      setStatus("Certificate branding saved.");
    } catch {
      setStatus("Certificate branding could not be saved.");
    } finally {
      setSavingBrand(false);
    }
  }
  async function uploadLogo(event: React.ChangeEvent<HTMLInputElement>) {
    const logo = event.target.files?.[0];
    if (!academy || !logo || issueLock.current) return;
    const body = new FormData();
    body.append("logo", logo);
    setSavingBrand(true);
    try {
      const response = await academyApi(
        `/api/academies/${academy.id}/certificates/branding/logo`,
        { method: "POST", body },
      );
      if (!response.ok) throw new Error();
      setBranding(await response.json());
      setStatus("Academy logo uploaded.");
    } catch {
      setStatus("Upload a PNG, JPG or WebP logo smaller than 2.5 MB.");
    } finally {
      setSavingBrand(false);
      if (fileInput.current) fileInput.current.value = "";
    }
  }
  async function issue(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!academy || !ready || issueLock.current) return;
    if (!selectedStudent) return setIssueNotice({ message: "Choose a valid student before issuing a certificate.", tone: "error" });
    if (batchId && !eligibleBatches.some((batch) => sameId(batch.id, batchId)))
      return setIssueNotice({ message: "Select a class or batch where this student has an Active or Completed enrollment.", tone: "error" });
    issueLock.current = true; setIssuing(true);
    setPrintCertificateId(""); setPrintView(undefined);
    try {
      const response = await academyApi(`/api/academies/${academy.id}/certificates`, {
        method: "POST", headers: apiHeaders(true),
        body: JSON.stringify({ studentId, batchId: batchId || null, title, templateKey: themeKey, issuedDate: issuedDate || null, notes: notes || null }),
      });
      if (!response.ok) {
        const problem = await response.json().catch(() => null);
        setIssueNotice({ message: typeof problem?.message === "string" ? problem.message : "Certificate could not be issued. Check the required details.", tone: "error" });
        return;
      }
      const issued: unknown = await Promise.resolve().then(() => response.json()).catch(() => null);
      setNotes(""); setIssuedDate("");
      const refreshed = await load(academy);
      if (refreshed && issued && typeof issued === "object" && "id" in issued && typeof issued.id === "string") setPrintCertificateId(issued.id);
      setIssueNotice({ message: "Certificate issued." + (refreshed ? " The register has been refreshed." : " Certificate records could not be refreshed. The save succeeded; refresh records before issuing another certificate."), tone: "success" });
    } catch {
      setReady(false);
      setIssueNotice({ message: "Certificate issuance could not be confirmed. Keep your details and refresh records before retrying to avoid a duplicate.", tone: "error" });
    } finally { issueLock.current = false; setIssuing(false); }
  }
  const studentName = (id: string) => {
    const student = students.find((value) => value.id === id);
    return student ? `${student.firstName} ${student.lastName}` : "Student";
  };
  const batchName = (id?: string | null) =>
    batches.find((value) => value.id === id)?.name ?? "Independent programme";
  const logoUrl = branding?.logoUrl
    ? `${apiUrl}${branding.logoUrl}`
    : undefined;
  const previewBranding = preview?.branding ?? branding;
  const previewLogo = previewBranding?.logoUrl ? `${apiUrl}${previewBranding.logoUrl}` : undefined;
  return (
    <main className="enterprise-settings certificates-admin certificates-standard min-h-screen">
      <WorkspaceNav />
      <div className="certificates-content">
        <header className="certificates-heading">
          <span className="certificates-title-icon" aria-hidden="true">
            ✦
          </span>
          <div className="certificates-title">
            <p>Academy experience</p>
            <h1>Certificates</h1>
          </div>
        </header>
        {status && (
          <p className="enterprise-page-state" role="status">
            {status}
          </p>
        )}
        {issueNotice.message && <EnterprisePageState tone={issueNotice.tone}>{issueNotice.message}</EnterprisePageState>}
        <button type="button" className="enterprise-action-button-secondary" disabled={issuing} onClick={refresh}>Refresh records</button>
        <section className="certificate-branding surface-panel certificates-panel">
          <div className="standard-panel-heading">
            <p>Branding</p>
            <h2>Certificate identity</h2>
          </div>
          <form onSubmit={saveBranding}>
            <div className="certificate-logo-control">
              <div className="certificate-logo-slot">
                {logoUrl ? (
                  <img src={logoUrl} alt="Academy logo" />
                ) : (
                  <span>{branding?.academyName?.slice(0, 1) ?? "A"}</span>
                )}
              </div>
              <button
                type="button"
                className="enterprise-action-button-secondary"
                onClick={() => fileInput.current?.click()}
                disabled={savingBrand || issuing}
              >
                Change logo
              </button>
            </div>
            <input
              ref={fileInput}
              type="file"
              accept="image/png,image/jpeg,image/webp"
              onChange={uploadLogo}
              className="sr-only"
            />
            <label>
              Accent colour
              <input
                type="color"
                disabled={savingBrand || issuing}
                value={branding?.accentColor ?? "#0F6CBD"}
                onChange={(event) =>
                  setBranding((current) =>
                    current
                      ? { ...current, accentColor: event.target.value }
                      : current,
                  )
                }
              />
            </label>
            <label>
              Authorised signatory
              <input
                value={branding?.signatoryName ?? ""}
                disabled={savingBrand || issuing}
                onChange={(event) =>
                  setBranding((current) =>
                    current
                      ? { ...current, signatoryName: event.target.value }
                      : current,
                  )
                }
                placeholder="Signatory name"
              />
            </label>
            <button className="enterprise-action-button" disabled={savingBrand || issuing}>
              {savingBrand ? "Saving…" : "Save branding"}
            </button>
          </form>
        </section>
        <section className="certificates-issue-grid">
          <form
            onSubmit={issue}
            className="surface-panel certificates-panel certificate-issue-form"
          >
            <div className="standard-panel-heading">
              <p>Create</p>
              <h2>Issue certificate</h2>
            </div>
            <fieldset disabled={!ready || issuing} className="contents">
            <label>
              Student
              <StandardSelectField
                name="studentId"
                value={studentId}
                onChange={(value) => {
                  if (!issueLock.current) setSelection((current) => sameId(current.studentId, value) ? current : { studentId: value, batchId: "" });
                }}
                disabled={!ready || issuing}
                placeholder="Select student"
                options={students.map((student) => ({
                  value: student.id,
                  label: `${student.firstName} ${student.lastName}`,
                }))}
              />
            </label>
            <label>
              Class / batch
              <StandardSelectField
                name="batchId"
                value={batchId}
                onChange={(value) => { if (!issueLock.current) setSelection((current) => ({ ...current, batchId: value })); }}
                disabled={!ready || issuing || !studentId}
                placeholder="No class or batch"
                options={eligibleBatches.map((batch) => ({
                  value: batch.id,
                  label: batch.name,
                }))}
              />
              <small>Optional. Only Active or Completed enrollments are listed. Leave blank for an independent programme.</small>
            </label>
            <label>
              Certificate title
              <input
                name="title"
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                required
              />
            </label>
            <StandardDateField
              name="issuedDate"
              value={issuedDate}
              onChange={setIssuedDate}
              label="Issue date"
            />
            <label>
              Certificate theme
              <StandardSelectField
                name="themeKey"
                value={themeKey}
                onChange={(value) => { if (!issueLock.current) setThemeKey(value); }}
                disabled={!ready || issuing}
                placeholder="Select theme"
                options={themes.map(([key, name, category]) => ({
                  value: key,
                  label: `${category} · ${name}`,
                }))}
              />
            </label>
            <label className="certificate-notes col-span-full">
              Recognition note
              <textarea
                name="notes"
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
              />
            </label>
            <button className="enterprise-action-button col-span-full" disabled={!ready || issuing}>
              {issuing ? "Please wait…" : "Issue certificate"}
            </button>
            </fieldset>
          </form>
          <section className="surface-panel certificates-panel certificate-preview-panel">
            <div className="standard-panel-heading standard-panel-heading-row">
              <div>
                <p>Preview</p>
                <h2>{preview ? "Saved certificate preview" : "Draft certificate preview"}</h2>
              </div>
              <button
                type="button"
                className="enterprise-action-button-secondary"
                onClick={printCertificate}
                disabled={!ready || issuing || savingBrand || !savedPreview}
              >
                Print / save PDF
              </button>
            </div>
            <label>
              Saved certificate to print
              <StandardSelectField
                name="printCertificateId" value={printCertificateId}
                onChange={(value) => { if (!issueLock.current) setPrintCertificateId(value); }}
                disabled={!ready || issuing} placeholder="Select an issued record"
                options={certificates.map((certificate) => ({ value: certificate.id, label: `${certificate.certificateNumber} · ${certificate.title} · ${certificate.status}` }))}
              />
            </label>
            <p>Official printing requires a saved Issued record. Previews are marked; unsaved changes do not change the saved certificate. Names and branding use current saved academy records.</p>
            {printView && <button type="button" className="enterprise-action-button-secondary" onClick={closePrint}>Close print preparation</button>}
            <article
              ref={printArticle}
              className={`certificate-preview theme-${preview?.certificate.templateKey ?? themeKey}`}
              data-print-state={printView ? "issued" : "preview"}
              style={
                {
                  "--academy-certificate-accent":
                    previewBranding?.accentColor ?? "#0F6CBD",
                } as CSSProperties
              }
            >
              <header>
                {previewLogo ? (
                  <img ref={printLogo} src={previewLogo} alt="Academy logo" />
                ) : (
                  <span className="certificate-monogram">
                    {previewBranding?.academyName?.slice(0, 1) ?? "A"}
                  </span>
                )}
                <strong>{previewBranding?.academyName ?? "Your Academy"}</strong>
              </header>
              {!printView && <p className="certificate-print-watermark">PREVIEW — NOT AN ISSUED PRINT</p>}
              <div className="certificate-theme-mark" aria-hidden="true" />
              <h4>{preview?.certificate.title ?? (title || "Certificate of Achievement")}</h4>
              <p className="certificate-presentation">
                This certificate is proudly presented to
              </p>
              <h5>
                {preview?.studentName ?? (selectedStudent
                  ? `${selectedStudent.firstName} ${selectedStudent.lastName}`
                  : "Student name")}
              </h5>
              <p className="certificate-body">
                in recognition of achievement and dedication in{" "}
                <b>{preview?.batchName ?? batchName(batchId)}</b>.
              </p>
              {(preview ? preview.certificate.notes : notes) && <p className="certificate-body">{preview ? preview.certificate.notes : notes}</p>}
              {preview && <p className="certificate-record-identity">Certificate: {preview.certificate.certificateNumber}<br />Verification: {preview.certificate.verificationCode}<br />Status: {preview.certificate.status}</p>}
              <footer>
                <span>
                  Issued{" "}
                  {preview?.certificate.issuedDate ?? (issuedDate || "Not issued — date set on issuance")}
                </span>
                <span>{previewBranding?.signatoryName || "Authorised signatory"}</span>
              </footer>
            </article>
          </section>
        </section>
        <section className="surface-panel certificates-panel certificates-register">
          <div className="standard-panel-heading">
            <p>Register</p>
            <h2>Issued certificates</h2>
          </div>
          {!loaded ? <p className="enterprise-settings-empty">Certificate records are not available yet.</p> : certificates.length === 0 ? (
            <p className="enterprise-settings-empty">
              No certificates have been issued yet.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table>
                <thead>
                  <tr>
                    <th>Student</th>
                    <th>Certificate</th>
                    <th>Theme</th>
                    <th>Issued</th>
                    <th>Verification</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {certificates.map((certificate) => (
                    <tr key={certificate.id}>
                      <td>{studentName(certificate.studentId)}</td>
                      <td>
                        <b>{certificate.title}</b>
                        <small>{certificate.certificateNumber}</small>
                      </td>
                      <td>
                        {themes.find(
                          ([key]) => key === certificate.templateKey,
                        )?.[1] ?? "Archived theme"}
                      </td>
                      <td>{certificate.issuedDate}</td>
                      <td>
                        <code>{certificate.verificationCode}</code>
                      </td>
                      <td>{certificate.status}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    </main>
  );
}
