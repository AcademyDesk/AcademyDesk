"use client";

import { FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";

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
  const pendingPrint = useRef(false);
  const mounted = useRef(false);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  useEffect(() => {
    if (!printPayout) return;
    // React has committed the selected saved document before the dialog opens.
    const frame = window.requestAnimationFrame(() => {
      if (!mounted.current) return;
      try { window.print(); if (mounted.current) setMessage("Payslip print dialog closed."); }
      catch { if (mounted.current) setMessage("The payslip print dialog could not be opened. Try again."); }
      finally {
        pendingPrint.current = false;
        if (mounted.current) { setPrinting(false); setPrintPayout(undefined); }
      }
    });
    return () => { window.cancelAnimationFrame(frame); };
  }, [printPayout]);

  async function printSavedPayout(id: string) {
    if (!academy || pendingPrint.current || !mounted.current) return;
    pendingPrint.current = true; setPrinting(true); setMessage("Preparing selected payslip…");
    let prepared = false;
    try {
      const response = await academyApi(`/api/academies/${academy.id}/payroll/payouts`, { cache: "no-store" });
      if (!response.ok) throw Error();
      const data: unknown = await response.json();
      if (!Array.isArray(data)) throw Error();
      const saved: unknown = data.find((row: unknown) => row !== null && typeof row === "object" && "id" in row && row.id === id);
      if (!isSavedPayout(saved)) throw Error();
      if (!mounted.current) return;
      setPrintPayout(saved); prepared = true;
    } catch {
      if (mounted.current) setMessage("The selected saved payslip could not be loaded. Try again.");
    } finally {
      if (!prepared) { pendingPrint.current = false; if (mounted.current) setPrinting(false); }
    }
  }
  async function load(id?: string) { const academyId = id ?? academy?.id; if (!academyId) return; const results = await Promise.all([academyApi(`/api/academies/${academyId}/teachers`), academyApi(`/api/academies/${academyId}/staff`), academyApi(`/api/academies/${academyId}/payroll/profiles`), academyApi(`/api/academies/${academyId}/payroll/payouts`)]); if (!results.every(response => response.ok)) throw Error(); const [teacherData, staffData, profileData, payoutData] = await Promise.all(results.map(response => response.json())); setTeachers(teacherData); setStaff(staffData); setProfiles(profileData); setPayouts(payoutData); setMessage(""); }
  useEffect(() => { void (async () => { try { const response = await academyApi("/api/academies"); if (!response.ok) throw Error(); const academies: Academy[] = await response.json(); if (!academies[0]) return setMessage("Create an academy before setting up payroll."); setAcademy(academies[0]); await load(academies[0].id); } catch { setMessage("Teacher and staff payouts could not be loaded."); } })(); }, []);
  const workers = useMemo(() => workerType === "Teacher" ? teachers.map(teacher => ({ id: teacher.id, name: `${teacher.firstName} ${teacher.lastName}` })) : staff.map(member => ({ id: member.id, name: member.displayName })), [workerType, teachers, staff]);
  const activeProfiles = profiles.filter(profile => profile.isActive); const currentProfile = profiles.find(profile => profile.id === payoutProfileId);
  async function createProfile(event: FormEvent<HTMLFormElement>) {
    const submittedForm = event.currentTarget; event.preventDefault(); if (!academy || !workerId || !effectiveFrom) return setMessage("Choose a worker and effective date first."); const worker = workers.find(item => item.id === workerId); const form = new FormData(event.currentTarget); const response = await academyApi(`/api/academies/${academy.id}/payroll/profiles`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ workerType, teacherId: workerType === "Teacher" ? workerId : null, staffUserId: workerType === "Staff" ? workerId : null, workerName: worker?.name, paymentModel, monthlyAmount: paymentModel === "Monthly" ? Number(form.get("monthlyAmount")) : null, amountPerCycle: paymentModel === "SessionBlock" ? Number(form.get("amountPerCycle")) : null, sessionsPerCycle: paymentModel === "SessionBlock" ? Number(form.get("sessionsPerCycle")) : null, effectiveFrom, isActive: true }) }); if (!response.ok) return setMessage("Payout profile could not be saved. Check the payment values."); submittedForm.reset(); setWorkerId(""); setEffectiveFrom(""); try { await load(); } catch { setMessage("Payout profile saved. The payroll list could not be refreshed. Reload before recording it again."); return; } setMessage("Payout profile saved."); }
  async function pay(event: FormEvent<HTMLFormElement>) {
    const submittedForm = event.currentTarget; event.preventDefault(); if (!academy || !payoutProfileId) return setMessage("Select an active payout profile first."); const form = new FormData(event.currentTarget); const response = await academyApi(`/api/academies/${academy.id}/payroll/payouts`, { method: "POST", headers: apiHeaders(true), body: JSON.stringify({ payrollProfileId: payoutProfileId, periodLabel: form.get("periodLabel"), sessionsCovered: currentProfile?.paymentModel === "SessionBlock" ? Number(form.get("sessionsCovered")) : null, grossAmount: currentProfile?.paymentModel === "SessionBlock" ? Number(form.get("grossAmount")) : null, deductions: Number(form.get("deductions") || 0), paymentMethod, reference: form.get("reference"), paidAtUtc: null }) }); if (!response.ok) return setMessage("Payout could not be recorded. Complete the required pay-cycle details."); submittedForm.reset(); setPayoutProfileId(""); setPaymentMethod("BankTransfer"); try { await load(); } catch { setMessage("Payout recorded and payslip generated. The payroll list could not be refreshed. Reload before recording it again."); return; } setMessage("Payout recorded and payslip generated."); }
  const teacherProfiles = activeProfiles.filter(profile => profile.workerType === "Teacher").length; const paidThisCycle = payouts.reduce((sum, payout) => sum + payout.netAmount, 0);
  return <main className="enterprise-settings finance-module payroll-standard"><header className="payroll-heading"><span className="payroll-title-icon" aria-hidden="true">₹</span><div><p>Finance</p><h1>Teacher &amp; staff payouts</h1></div></header>{message && <p className="payroll-notice" role="status">{message}</p>}<section className="payroll-kpis"><article><span>Active payout profiles</span><b>{activeProfiles.length}</b><small>{teacherProfiles} teacher and {activeProfiles.length - teacherProfiles} staff profiles.</small></article><article><span>Recorded payouts</span><b>{payouts.length}</b><small>Payslips generated in the register.</small></article><article><span>Paid value</span><b>{money(paidThisCycle)}</b><small>Total amount captured in this register.</small></article></section><section className="payroll-layout"><form onSubmit={createProfile} className="payroll-panel"><header className="payroll-panel-header"><div><p>Profile setup</p><h2>Set up payout profile</h2></div></header><div className="payroll-fields"><label><span>Worker type</span><StandardSelectField name="workerType" value={workerType} onChange={value => { setWorkerType(value as "Teacher" | "Staff"); setWorkerId(""); }} placeholder="Choose worker type" options={[{ value: "Teacher", label: "Teacher" }, { value: "Staff", label: "Staff" }]} /></label><label><span>Worker</span><StandardSelectField name="workerId" value={workerId} onChange={setWorkerId} placeholder={`Select ${workerType.toLowerCase()}`} options={workers.map(worker => ({ value: worker.id, label: worker.name }))} /></label><label><span>Payment cycle</span><StandardSelectField name="paymentModel" value={paymentModel} onChange={value => setPaymentModel(value as "Monthly" | "SessionBlock")} placeholder="Choose payment cycle" options={[{ value: "Monthly", label: "Monthly salary" }, { value: "SessionBlock", label: "Completed-session cycle" }]} /></label>{paymentModel === "Monthly" ? <label><span>Monthly salary</span><input name="monthlyAmount" type="number" min="1" step="0.01" className="field" placeholder="Amount in INR" required /></label> : <><label><span>Sessions per cycle</span><input name="sessionsPerCycle" type="number" min="1" className="field" placeholder="e.g. 4 or 8" required /></label><label><span>Payout per cycle</span><input name="amountPerCycle" type="number" min="1" step="0.01" className="field" placeholder="Amount in INR" required /></label></>}<StandardDateField name="effectiveFrom" value={effectiveFrom} onChange={setEffectiveFrom} label="Effective from" required /><button className="enterprise-action-button payroll-action" disabled={!academy || !workerId || !effectiveFrom}>Save payout profile</button></div></form><form onSubmit={pay} className="payroll-panel"><header className="payroll-panel-header"><div><p>Payout record</p><h2>Record payout</h2></div></header><div className="payroll-fields"><label className="payroll-wide"><span>Payout profile</span><StandardSelectField name="payrollProfileId" value={payoutProfileId} onChange={setPayoutProfileId} placeholder="Select active profile" options={activeProfiles.map(profile => ({ value: profile.id, label: `${profile.workerName} · ${profile.paymentModel === "Monthly" ? `Monthly ${money(profile.monthlyAmount ?? 0)}` : `${profile.sessionsPerCycle} sessions · ${money(profile.amountPerCycle ?? 0)}`}` }))} /></label><label><span>Pay period</span><input name="periodLabel" className="field" placeholder="e.g. September 2026" required /></label><label><span>Deductions</span><input name="deductions" type="number" min="0" step="0.01" defaultValue="0" className="field" /></label>{currentProfile?.paymentModel === "SessionBlock" && <><label><span>Sessions completed</span><input name="sessionsCovered" type="number" min="1" className="field" placeholder="e.g. 8" required /></label><label><span>Cycle gross amount</span><input name="grossAmount" type="number" min="1" step="0.01" className="field" placeholder="Amount in INR" required /></label></>}<label><span>Payment method</span><StandardSelectField name="paymentMethod" value={paymentMethod} onChange={setPaymentMethod} placeholder="Choose payment method" options={[{ value: "BankTransfer", label: "Bank transfer" }, { value: "UPI", label: "UPI" }, { value: "Cash", label: "Cash" }, { value: "Cheque", label: "Cheque" }]} /></label><label><span>Reference</span><input name="reference" className="field" placeholder="UTR / payment reference" /></label><button className="enterprise-action-button payroll-action payroll-wide" disabled={!academy || !payoutProfileId}>Record payout &amp; generate payslip</button></div></form></section><section className="payroll-panel payroll-register"><header className="payroll-panel-header"><div><p>Payslip register</p><h2>Recorded payouts</h2></div></header>{payouts.length === 0 ? <p className="payroll-empty">No payouts recorded yet.</p> : <div className="payroll-table-wrap"><table><thead><tr><th>Worker</th><th>Pay period</th><th>Cycle</th><th>Net payout</th><th>Reference</th><th>Action</th></tr></thead><tbody>{payouts.map(payout => <tr key={payout.id}><td><b>{payout.workerName}</b><small>{payout.workerType}</small></td><td>{payout.periodLabel}<small>{new Date(payout.paidAtUtc).toLocaleDateString("en-IN")}</small></td><td>{payout.sessionsCovered ? `${payout.sessionsCovered} sessions` : "Monthly"}</td><td><b>{money(payout.netAmount, payout.currency)}</b></td><td>{payout.reference || "—"}</td><td><button type="button" disabled={printing} onClick={() => { void printSavedPayout(payout.id); }} className="enterprise-action-button enterprise-action-button-secondary">{printing ? "Preparing payslip…" : "Print / Save PDF"}</button></td></tr>)}</tbody></table></div>}</section>{printPayout && <article className="payroll-payslip" data-payout-id={printPayout.id} aria-label="Saved payslip">
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
