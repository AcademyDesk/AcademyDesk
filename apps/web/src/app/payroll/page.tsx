"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders, subscribePortalSessionChanges } from "@/lib/api";

type Academy = { id: string };
type Teacher = { id: string; firstName: string; lastName: string };
type Staff = { id: string; displayName: string; email: string };
type Profile = { id: string; workerType: "Teacher" | "Staff"; workerName: string; paymentModel: "Monthly" | "SessionBlock"; monthlyAmount?: number; amountPerCycle?: number; sessionsPerCycle?: number; effectiveFrom: string; isActive: boolean };
type Payout = { id: string; payrollProfileId: string; workerName: string; workerType: string; payslipNumber: string; periodLabel: string; sessionsCovered?: number; grossAmount: number; deductions: number; netAmount: number; currency: string; status: string; paymentMethod: string; reference?: string; paidAtUtc: string };
const money = (amount: number, currency = "INR") => new Intl.NumberFormat("en-IN", { style: "currency", currency, minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);

function isSavedPayout(value: unknown): value is Payout {
  if (!value || typeof value !== "object" || Array.isArray(value)) return false;
  const row = value as Record<string, unknown>;
  return ["id", "payrollProfileId", "workerName", "workerType", "payslipNumber", "periodLabel", "status", "paymentMethod"].every(key => typeof row[key] === "string" && Boolean((row[key] as string).trim()))
    && ["grossAmount", "deductions", "netAmount"].every(key => typeof row[key] === "number" && Number.isFinite(row[key]))
    && typeof row.currency === "string" && /^[A-Z]{3}$/.test(row.currency)
    && typeof row.paidAtUtc === "string" && Number.isFinite(Date.parse(row.paidAtUtc))
    && (row.sessionsCovered == null || typeof row.sessionsCovered === "number" && Number.isFinite(row.sessionsCovered))
    && (row.reference == null || typeof row.reference === "string");
}

export default function PayrollPage() {
  const [academy, setAcademy] = useState<Academy>(); const [teachers, setTeachers] = useState<Teacher[]>([]); const [staff, setStaff] = useState<Staff[]>([]); const [profiles, setProfiles] = useState<Profile[]>([]); const [payouts, setPayouts] = useState<Payout[]>([]); const [message, setMessage] = useState("Loading payout data…");
  const [workerType, setWorkerType] = useState<"Teacher" | "Staff">("Teacher"); const [workerId, setWorkerId] = useState(""); const [paymentModel, setPaymentModel] = useState<"Monthly" | "SessionBlock">("Monthly"); const [effectiveFrom, setEffectiveFrom] = useState(""); const [payoutProfileId, setPayoutProfileId] = useState(""); const [paymentMethod, setPaymentMethod] = useState("BankTransfer");
  const [printPayout, setPrintPayout] = useState<Payout>();
  const [printing, setPrinting] = useState(false);
  const [writing, setWriting] = useState<"profile" | "payout">();
  const [reviewRequired, setReviewRequired] = useState(false);
  const [sessionEnded, setSessionEnded] = useState(false);
  const pendingWrite = useRef(false);
  const needsReview = useRef(false);
  const pendingPrint = useRef(false);
  const mounted = useRef(false);
  const sessionRetired = useRef(false);
  const isCurrent = () => mounted.current && !sessionRetired.current;
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  useEffect(() => subscribePortalSessionChanges("AcademyAdmin", () => {
    sessionRetired.current = true; needsReview.current = true;
    if (!mounted.current) return;
    setSessionEnded(true); setAcademy(undefined); setTeachers([]); setStaff([]); setProfiles([]); setPayouts([]);
    setWorkerId(""); setEffectiveFrom(""); setPayoutProfileId(""); setPaymentMethod("BankTransfer");
    setPrintPayout(undefined); setPrinting(false); setWriting(undefined); setReviewRequired(true);
    setMessage("Your session changed. Reload and check the saved payroll register before continuing.");
  }), []);
  useEffect(() => {
    if (!printPayout) return;
    // React has committed the selected saved document before the dialog opens.
    const frame = window.requestAnimationFrame(() => {
      if (!isCurrent()) return;
      try { window.print(); if (isCurrent()) setMessage("Payslip print dialog closed."); }
      catch { if (isCurrent()) setMessage("The payslip print dialog could not be opened. Try again."); }
      finally {
        pendingPrint.current = false;
        if (isCurrent()) { setPrinting(false); setPrintPayout(undefined); }
      }
    });
    return () => { window.cancelAnimationFrame(frame); };
  }, [printPayout]);

  async function printSavedPayout(id: string) {
    if (!academy || pendingPrint.current || !isCurrent()) return;
    pendingPrint.current = true; setPrinting(true); setMessage("Preparing selected payslip…");
    let prepared = false;
    try {
      const response = await academyApi(`/api/academies/${academy.id}/payroll/payouts`, { cache: "no-store" });
      if (!response.ok) throw Error();
      const data: unknown = await response.json();
      if (!Array.isArray(data)) throw Error();
      const saved: unknown = data.find((row: unknown) => row !== null && typeof row === "object" && "id" in row && row.id === id);
      if (!isSavedPayout(saved)) throw Error();
      if (!isCurrent()) return;
      setPrintPayout(saved); prepared = true;
    } catch {
      if (isCurrent()) setMessage("The selected saved payslip could not be loaded. Try again.");
    } finally {
      if (!prepared) { pendingPrint.current = false; if (isCurrent()) setPrinting(false); }
    }
  }
  async function load(id?: string) {
    const academyId = id ?? academy?.id; if (!academyId) return;
    const results = await Promise.all([academyApi(`/api/academies/${academyId}/teachers`, { cache: "no-store" }), academyApi(`/api/academies/${academyId}/staff`, { cache: "no-store" }), academyApi(`/api/academies/${academyId}/payroll/profiles`, { cache: "no-store" }), academyApi(`/api/academies/${academyId}/payroll/payouts`, { cache: "no-store" })]);
    if (!results.every(response => response.ok)) throw Error();
    const [teacherData, staffData, profileData, payoutData] = await Promise.all(results.map(response => response.json()));
    if (!isCurrent()) return;
    setTeachers(teacherData); setStaff(staffData); setProfiles(profileData); setPayouts(payoutData);
  }
  useEffect(() => { void (async () => {
    try {
      const response = await academyApi("/api/academies", { cache: "no-store" }); if (!response.ok) throw Error();
      const academies: Academy[] = await response.json(); if (!isCurrent()) return;
      if (!academies[0]) return setMessage("Create an academy before setting up payroll.");
      setAcademy(academies[0]); await load(academies[0].id); if (isCurrent()) setMessage("");
    } catch { if (isCurrent()) setMessage("Teacher and staff payouts could not be loaded."); }
  })(); }, []);
  const workers = useMemo(() => workerType === "Teacher" ? teachers.map(teacher => ({ id: teacher.id, name: `${teacher.firstName} ${teacher.lastName}` })) : staff.map(member => ({ id: member.id, name: member.displayName })), [workerType, teachers, staff]);
  const activeProfiles = profiles.filter(profile => profile.isActive); const currentProfile = profiles.find(profile => profile.id === payoutProfileId);
  function requireReview() { needsReview.current = true; setReviewRequired(true); }
  async function savePayroll(kind: "profile" | "payout", submittedForm: HTMLFormElement, body: object) {
    if (!academy || pendingWrite.current || needsReview.current || !isCurrent()) return;
    pendingWrite.current = true; setWriting(kind); setMessage(kind === "profile" ? "Saving payout profile…" : "Recording payout…");
    const confirmed = kind === "profile" ? "Payout profile saved." : "Payout recorded and payslip generated.";
    try {
      const response = await academyApi(`/api/academies/${academy.id}/payroll/${kind === "profile" ? "profiles" : "payouts"}`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify(body) });
      if (!isCurrent()) return;
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408 || response.status === 0) {
          requireReview(); setMessage("The payroll save outcome is unconfirmed. Reload and check the saved profiles and payout register before recording again.");
        } else setMessage(kind === "profile" ? "Payout profile could not be saved. Check the payment values." : "Payout could not be recorded. Complete the required pay-cycle details.");
        return;
      }
      submittedForm.reset();
      if (kind === "profile") { setWorkerId(""); setEffectiveFrom(""); }
      else { setPayoutProfileId(""); setPaymentMethod("BankTransfer"); }
      try { await load(); }
      catch {
        if (isCurrent()) { requireReview(); setMessage(`${confirmed} The payroll list could not be refreshed. Reload and check the register before recording again.`); }
        return;
      }
      if (isCurrent()) setMessage(confirmed);
    } catch {
      if (isCurrent()) { requireReview(); setMessage("Could not confirm whether the payroll save completed. Reload and check the saved profiles and payout register before recording again."); }
    } finally { pendingWrite.current = false; if (isCurrent()) setWriting(undefined); }
  }
  async function createProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (pendingWrite.current || needsReview.current || !isCurrent()) return;
    if (!academy || !workerId || !effectiveFrom) return setMessage("Choose a worker and effective date first.");
    const worker = workers.find(item => item.id === workerId); const form = new FormData(event.currentTarget);
    await savePayroll("profile", event.currentTarget, { workerType, teacherId: workerType === "Teacher" ? workerId : null, staffUserId: workerType === "Staff" ? workerId : null, workerName: worker?.name, paymentModel, monthlyAmount: paymentModel === "Monthly" ? Number(form.get("monthlyAmount")) : null, amountPerCycle: paymentModel === "SessionBlock" ? Number(form.get("amountPerCycle")) : null, sessionsPerCycle: paymentModel === "SessionBlock" ? Number(form.get("sessionsPerCycle")) : null, effectiveFrom, isActive: true });
  }
  async function pay(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (pendingWrite.current || needsReview.current || !isCurrent()) return;
    if (!academy || !payoutProfileId) return setMessage("Select an active payout profile first.");
    const form = new FormData(event.currentTarget);
    await savePayroll("payout", event.currentTarget, { payrollProfileId: payoutProfileId, periodLabel: form.get("periodLabel"), sessionsCovered: currentProfile?.paymentModel === "SessionBlock" ? Number(form.get("sessionsCovered")) : null, grossAmount: currentProfile?.paymentModel === "SessionBlock" ? Number(form.get("grossAmount")) : null, deductions: Number(form.get("deductions") || 0), paymentMethod, reference: form.get("reference"), paidAtUtc: null });
  }
  const teacherProfiles = activeProfiles.filter(profile => profile.workerType === "Teacher").length; const paidByCurrency = Array.from(payouts.reduce((totals, payout) => totals.set(payout.currency, (totals.get(payout.currency) ?? 0) + payout.netAmount), new Map<string, number>())).sort(([a], [b]) => a.localeCompare(b));
  const writeDisabled = Boolean(writing) || reviewRequired;
  return <main className="enterprise-settings finance-module payroll-standard"><header className="payroll-heading"><span className="payroll-title-icon" aria-hidden="true">₹</span><div><p>Finance</p><h1>Teacher &amp; staff payouts</h1></div></header>{message && <p className="payroll-notice" role="status">{message}</p>}<section className="payroll-kpis"><article><span>Active payout profiles</span><b>{activeProfiles.length}</b><small>{teacherProfiles} teacher and {activeProfiles.length - teacherProfiles} staff profiles.</small></article><article><span>Recorded payouts</span><b>{payouts.length}</b><small>Payslips generated in the register.</small></article><article><span>Paid value</span><b>{paidByCurrency.length ? paidByCurrency.map(([currency, amount]) => <span key={currency} style={{ display: "block", fontSize: "inherit", color: "inherit", letterSpacing: "inherit", textTransform: "none", fontWeight: "inherit" }}>{currency} {money(amount, currency)}</span>) : "No payouts recorded"}</b><small>Total amount captured in this register.</small></article></section>{!sessionEnded && <section className="payroll-layout"><form onSubmit={createProfile} className="payroll-panel"><header className="payroll-panel-header"><div><p>Profile setup</p><h2>Set up payout profile</h2></div></header><fieldset className="payroll-fields" disabled={writeDisabled} style={{ border: 0, margin: 0, minWidth: 0 }}><label><span>Worker type</span><StandardSelectField name="workerType" value={workerType} onChange={value => { setWorkerType(value as "Teacher" | "Staff"); setWorkerId(""); }} placeholder="Choose worker type" options={[{ value: "Teacher", label: "Teacher" }, { value: "Staff", label: "Staff" }]} /></label><label><span>Worker</span><StandardSelectField name="workerId" value={workerId} onChange={setWorkerId} placeholder={`Select ${workerType.toLowerCase()}`} options={workers.map(worker => ({ value: worker.id, label: worker.name }))} /></label><label><span>Payment cycle</span><StandardSelectField name="paymentModel" value={paymentModel} onChange={value => setPaymentModel(value as "Monthly" | "SessionBlock")} placeholder="Choose payment cycle" options={[{ value: "Monthly", label: "Monthly salary" }, { value: "SessionBlock", label: "Completed-session cycle" }]} /></label>{paymentModel === "Monthly" ? <label><span>Monthly salary</span><input name="monthlyAmount" type="number" min="1" step="0.01" className="field" placeholder="Amount in INR" required /></label> : <><label><span>Sessions per cycle</span><input name="sessionsPerCycle" type="number" min="1" className="field" placeholder="e.g. 4 or 8" required /></label><label><span>Payout per cycle</span><input name="amountPerCycle" type="number" min="1" step="0.01" className="field" placeholder="Amount in INR" required /></label></>}<StandardDateField name="effectiveFrom" value={effectiveFrom} onChange={setEffectiveFrom} label="Effective from" required /><button className="enterprise-action-button payroll-action" disabled={writeDisabled || !academy || !workerId || !effectiveFrom}>{writing === "profile" ? "Saving payout profile…" : "Save payout profile"}</button></fieldset></form><form onSubmit={pay} className="payroll-panel"><header className="payroll-panel-header"><div><p>Payout record</p><h2>Record payout</h2></div></header><fieldset className="payroll-fields" disabled={writeDisabled} style={{ border: 0, margin: 0, minWidth: 0 }}><label className="payroll-wide"><span>Payout profile</span><StandardSelectField name="payrollProfileId" value={payoutProfileId} onChange={setPayoutProfileId} placeholder="Select active profile" options={activeProfiles.map(profile => ({ value: profile.id, label: `${profile.workerName} · ${profile.paymentModel === "Monthly" ? `Monthly ${money(profile.monthlyAmount ?? 0)}` : `${profile.sessionsPerCycle} sessions · ${money(profile.amountPerCycle ?? 0)}`}` }))} /></label><label><span>Pay period</span><input name="periodLabel" className="field" placeholder="e.g. September 2026" required /></label><label><span>Deductions</span><input name="deductions" type="number" min="0" step="0.01" defaultValue="0" className="field" /></label>{currentProfile?.paymentModel === "SessionBlock" && <><label><span>Sessions completed</span><input name="sessionsCovered" type="number" min="1" className="field" placeholder="e.g. 8" required /></label><label><span>Cycle gross amount</span><input name="grossAmount" type="number" min="1" step="0.01" className="field" placeholder="Amount in INR" required /></label></>}<label><span>Payment method</span><StandardSelectField name="paymentMethod" value={paymentMethod} onChange={setPaymentMethod} placeholder="Choose payment method" options={[{ value: "BankTransfer", label: "Bank transfer" }, { value: "UPI", label: "UPI" }, { value: "Cash", label: "Cash" }, { value: "Cheque", label: "Cheque" }]} /></label><label><span>Reference</span><input name="reference" className="field" placeholder="UTR / payment reference" /></label><button className="enterprise-action-button payroll-action payroll-wide" disabled={writeDisabled || !academy || !payoutProfileId}>{writing === "payout" ? "Recording payout…" : "Record payout & generate payslip"}</button></fieldset></form></section>}<section className="payroll-panel payroll-register"><header className="payroll-panel-header"><div><p>Payslip register</p><h2>Recorded payouts</h2></div></header>{payouts.length === 0 ? <p className="payroll-empty">No payouts recorded yet.</p> : <div className="payroll-table-wrap"><table><thead><tr><th>Worker</th><th>Pay period</th><th>Cycle</th><th>Net payout</th><th>Reference</th><th>Action</th></tr></thead><tbody>{payouts.map(payout => <tr key={payout.id}><td><b>{payout.workerName}</b><small>{payout.workerType}</small></td><td>{payout.periodLabel}<small>{new Date(payout.paidAtUtc).toLocaleDateString("en-IN")}</small></td><td>{payout.sessionsCovered ? `${payout.sessionsCovered} sessions` : "Monthly"}</td><td><b>{money(payout.netAmount, payout.currency)}</b></td><td>{payout.reference || "—"}</td><td><button type="button" disabled={printing} onClick={() => { void printSavedPayout(payout.id); }} className="enterprise-action-button enterprise-action-button-secondary">{printing ? "Preparing payslip…" : "Print / Save PDF"}</button></td></tr>)}</tbody></table></div>}</section>{printPayout && <article className="payroll-payslip" data-payout-id={printPayout.id} aria-label="Saved payslip">
    <header><p>Payroll record</p><h2>Payslip</h2><b>{printPayout.payslipNumber}</b></header>
    <dl>
      <div><dt>Worker</dt><dd>{printPayout.workerName}</dd></div>
      <div><dt>Worker type</dt><dd>{printPayout.workerType}</dd></div>
      <div><dt>Pay period</dt><dd>{printPayout.periodLabel}</dd></div>
      <div><dt>Paid date</dt><dd>{new Date(printPayout.paidAtUtc).toLocaleDateString("en-IN")}</dd></div>
      <div><dt>Cycle</dt><dd>{printPayout.sessionsCovered != null ? `${printPayout.sessionsCovered} sessions` : "Monthly"}</dd></div>
      <div><dt>Gross pay</dt><dd>{money(printPayout.grossAmount, printPayout.currency)}</dd></div>
      <div><dt>Deductions</dt><dd>{money(printPayout.deductions, printPayout.currency)}</dd></div>
      <div className="payroll-payslip-net"><dt>Net payout</dt><dd>{money(printPayout.netAmount, printPayout.currency)}</dd></div>
      <div><dt>Currency</dt><dd>{printPayout.currency}</dd></div>
      <div><dt>Status</dt><dd>{printPayout.status}</dd></div>
      <div><dt>Payment method</dt><dd>{printPayout.paymentMethod}</dd></div>
      <div><dt>Reference</dt><dd>{printPayout.reference || "—"}</dd></div>
    </dl>
  </article>}</main>;
}
