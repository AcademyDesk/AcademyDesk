"use client";

import { FormEvent, useCallback, useEffect, useMemo, useRef, useState } from "react";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { academyApi, apiHeaders } from "@/lib/api";
import { PayrollPayslip, type Payout } from "./payslip";
import { payrollMoney as money, payrollTotals, payrollRequestJson } from "./payroll-money";
import visual from "./payroll.module.css";

type Academy = { id: string; name?: string };
type Teacher = { id: string; firstName: string; lastName: string };
type Staff = { id: string; displayName: string; email: string };
type Profile = { id: string; workerType: "Teacher" | "Staff"; workerName: string; paymentModel: "Monthly" | "SessionBlock"; monthlyAmount?: number; amountPerCycle?: number; monthlyAmountExact?: string; amountPerCycleExact?: string; sessionsPerCycle?: number; effectiveFrom: string; isActive: boolean };

export default function PayrollPage() {
  const [academy, setAcademy] = useState<Academy>(); const [teachers, setTeachers] = useState<Teacher[]>([]); const [staff, setStaff] = useState<Staff[]>([]); const [profiles, setProfiles] = useState<Profile[]>([]); const [payouts, setPayouts] = useState<Payout[]>([]); const [message, setMessage] = useState("Loading payout data…");
  const [workerType, setWorkerType] = useState<"Teacher" | "Staff">("Teacher"); const [workerId, setWorkerId] = useState(""); const [paymentModel, setPaymentModel] = useState<"Monthly" | "SessionBlock">("Monthly"); const [effectiveFrom, setEffectiveFrom] = useState(""); const [payoutProfileId, setPayoutProfileId] = useState(""); const [paymentMethod, setPaymentMethod] = useState("BankTransfer");
  const [selectedPayout, setSelectedPayout] = useState<Payout | null>(null);
  const [ready, setReady] = useState(false);
  const [saving, setSaving] = useState(false);
  const [needsReview, setNeedsReview] = useState(false);
  const pending = useRef(false);
  const mounted = useRef(false);
  const guard = useRef({ ready: false, review: false, snapshot: 0, profileDraft: 0, payoutDraft: 0 });
  const snapshot = guard.current.snapshot;
  function canSave() { return mounted.current && academy && guard.current.ready && !guard.current.review && !pending.current && snapshot === guard.current.snapshot; }
  function requireReview() { guard.current.review = true; setNeedsReview(true); }
  const load = useCallback(async (id: string) => {
    const responses = await Promise.all(["teachers", "staff", "payroll/profiles", "payroll/payouts"].map(route =>
      academyApi(`/api/academies/${id}/${route}`, { cache: "no-store" })));
    if (!responses.every(response => response.ok)) throw new Error("Payroll workspace unavailable.");
    const data = await Promise.all(responses.map(response => response.json()));
    if (!data.every(Array.isArray)) throw new Error("Invalid payroll workspace.");
    if (!mounted.current) return;
    setTeachers(data[0]); setStaff(data[1]); setProfiles(data[2]); setPayouts(data[3]); setSelectedPayout(null);
    guard.current.ready = true; guard.current.snapshot++;
  }, []);
  useEffect(() => {
    mounted.current = true;
    void (async () => {
      try {
        const response = await academyApi("/api/academies", { cache: "no-store" });
        if (!response.ok) throw new Error();
        const academies: Academy[] = await response.json();
        if (!Array.isArray(academies)) throw new Error();
        if (!mounted.current) return;
        if (!academies[0]) { setMessage("Create an academy before setting up payroll."); return; }
        setAcademy(academies[0]);
        await load(academies[0].id);
        if (mounted.current) { setReady(true); setMessage(""); }
      } catch { if (mounted.current) setMessage("Teacher and staff payouts could not be loaded. Refresh before entering payroll."); }
    })();
    return () => { mounted.current = false; };
  }, [load]);
  const workers = useMemo(() => workerType === "Teacher" ? teachers.map(teacher => ({ id: teacher.id, name: `${teacher.firstName} ${teacher.lastName}` })) : staff.map(member => ({ id: member.id, name: member.displayName })), [workerType, teachers, staff]);
  const activeProfiles = profiles.filter(profile => profile.isActive); const currentProfile = profiles.find(profile => profile.id === payoutProfileId);
  async function refreshRegister() {
    if (!academy || pending.current) return;
    pending.current = true; guard.current.ready = false; setSaving(true); setReady(false);
    try {
      await load(academy.id);
      if (mounted.current) { guard.current.review = false; setReady(true); setNeedsReview(false); setMessage("Payout register refreshed. Check for the previous profile or payout before recording it again."); }
    } catch { if (mounted.current) { requireReview(); setMessage("The payout register could not be refreshed. Please try again later; do not repeat an unconfirmed payroll entry."); } }
    finally { pending.current = false; if (mounted.current) setSaving(false); }
  }
  // Capture the native form before awaiting: React currentTarget is cleared afterwards.
  // One synchronous guard covers both forms through authoritative readback.
  async function save(formElement: HTMLFormElement, kind: "profile" | "payout", body: Record<string, unknown>, reset: () => void) {
    if (!canSave() || !academy) return;
    const json = payrollRequestJson(body);
    if (json === null) { setMessage("Enter positive payroll amounts and non-negative deductions with at most 16 whole digits and two decimal places."); return; }
    const id = academy.id;
    const draftKey = kind === "profile" ? "profileDraft" : "payoutDraft";
    const draftVersion = guard.current[draftKey];
    const nativeFields = kind === "profile" ? ["monthlyAmount", "amountPerCycle", "sessionsPerCycle"] : ["periodLabel", "deductions", "sessionsCovered", "grossAmount", "reference"];
    const capturedFields = nativeFields.map(name => ({ name, value: (formElement.elements.namedItem(name) as HTMLInputElement | null)?.value }));
    let success = kind === "profile" ? "Payout profile saved." : "Payout recorded and payslip generated.";
    pending.current = true; setSaving(true); setMessage(kind === "profile" ? "Saving payout profile…" : "Recording payout…");
    let confirmed = false;
    try {
      const response = await academyApi(`/api/academies/${id}/payroll/${kind === "profile" ? "profiles" : "payouts"}`, {
        method: "POST", headers: apiHeaders(true), body: json,
      });
      if (!mounted.current) return;
      if (!response.ok) {
        if (response.status >= 500 || response.status === 408) {
          requireReview(); setMessage("Payroll save could not be confirmed. Refresh and check the register before retrying.");
        } else {
          let detail = "";
          try { const result: unknown = await response.json(); if (result && typeof result === "object" && "message" in result && typeof result.message === "string") detail = result.message; } catch { /* Safe fallback for empty/malformed errors. */ }
          if (mounted.current) setMessage(detail || (kind === "profile" ? "Payout profile could not be saved. Check the payment values." : "Payout could not be recorded. Complete the required pay-cycle details."));
        }
        return;
      }
      confirmed = true;
      if (kind === "payout") {
        success = "Payroll entry accepted. Its payout status could not be read; refresh and check the register before retrying.";
        const result: unknown = await response.json().catch(() => null);
        if (!mounted.current) return;
        if (!result || typeof result !== "object" || !("status" in result) || !["Paid", "NoCashDue"].includes(String(result.status))) {
          requireReview(); setMessage(success + " Do not repeat this entry; your draft is retained."); return;
        }
        success = result.status === "NoCashDue" ? "Earnings statement recorded. No cash due." : "Payout recorded and payslip generated.";
      }
      if (guard.current[draftKey] === draftVersion && capturedFields.every(field => (formElement.elements.namedItem(field.name) as HTMLInputElement | null)?.value === field.value)) { formElement.reset(); reset(); }
      setMessage(success);
      await load(id);
      if (mounted.current) setMessage(success);
    } catch {
      if (mounted.current) {
        requireReview();
        setMessage(confirmed ? success + " The register could not be refreshed; do not repeat this entry." :
          "Payroll save could not be confirmed. Refresh and check the register before retrying.");
      }
    } finally { pending.current = false; if (mounted.current) setSaving(false); }
  }
  async function createProfile(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canSave()) return;
    if (!workerId || !effectiveFrom) { setMessage("Choose a worker and effective date first."); return; }
    const worker = workers.find(item => item.id === workerId);
    if (!worker || !["Teacher", "Staff"].includes(workerType) || !["Monthly", "SessionBlock"].includes(paymentModel)) { setMessage("Choose a current worker and supported payment cycle before saving."); return; }
    const formElement = event.currentTarget, form = new FormData(formElement);
    await save(formElement, "profile", { workerType, teacherId: workerType === "Teacher" ? workerId : null,
      staffUserId: workerType === "Staff" ? workerId : null, workerName: worker?.name, paymentModel,
      monthlyAmount: paymentModel === "Monthly" ? form.get("monthlyAmount") : null,
      amountPerCycle: paymentModel === "SessionBlock" ? form.get("amountPerCycle") : null,
      sessionsPerCycle: paymentModel === "SessionBlock" ? Number(form.get("sessionsPerCycle")) : null,
      effectiveFrom, isActive: true }, () => { setWorkerId(""); setEffectiveFrom(""); });
  }
  async function pay(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canSave()) return;
    if (!currentProfile?.isActive || !["Monthly", "SessionBlock"].includes(currentProfile.paymentModel)) { setMessage("Select an active payout profile first."); return; }
    if (!["BankTransfer", "UPI", "Cash", "Cheque"].includes(paymentMethod)) { setMessage("Choose a supported payment method."); return; }
    const formElement = event.currentTarget, form = new FormData(formElement);
    await save(formElement, "payout", { payrollProfileId: payoutProfileId, periodLabel: form.get("periodLabel"),
      sessionsCovered: currentProfile?.paymentModel === "SessionBlock" ? Number(form.get("sessionsCovered")) : null,
      grossAmount: currentProfile?.paymentModel === "SessionBlock" ? form.get("grossAmount") : null,
      deductions: form.get("deductions") || "0", paymentMethod, reference: form.get("reference"), paidAtUtc: null },
      () => { setPayoutProfileId(""); setPaymentMethod("BankTransfer"); });
  }
  const blocked = !ready || saving || needsReview;
  const teacherProfiles = activeProfiles.filter(profile => profile.workerType === "Teacher").length; const paidThisCycle = payrollTotals(payouts);
  return <main className={`enterprise-settings finance-module payroll-standard ${visual.page}`} ><header className="payroll-heading"><span className="payroll-title-icon" aria-hidden="true">₹</span><div><p>Finance</p><h1>Teacher &amp; staff payouts</h1></div></header>{message && <p className="payroll-notice" role="status" aria-live="polite">{message}</p>}<section className="payroll-kpis"><article><span>Active payout profiles</span><b>{ready ? activeProfiles.length : "—"}</b><small>{ready ? `${teacherProfiles} teacher and ${activeProfiles.length - teacherProfiles} staff profiles.` : "Available after a successful load."}</small></article><article><span>Recorded payouts</span><b>{ready ? payouts.length : "—"}</b><small>Payslips generated in the register.</small></article><article><span>Recorded net value</span><b>{ready ? paidThisCycle : "—"}</b><small>Recorded net amounts; not proof of bank settlement.</small></article></section><button type="button" className="enterprise-action-button enterprise-action-button-secondary" onClick={refreshRegister} disabled={!academy || saving}>Refresh payout register</button><section className="payroll-layout"><form onChangeCapture={() => { guard.current.profileDraft++; }} onSubmit={createProfile} className="payroll-panel" aria-busy={saving}><header className="payroll-panel-header"><div><p>Profile setup</p><h2>Set up payout profile</h2></div></header><fieldset className="payroll-fields" disabled={blocked} style={{ margin: 0, border: 0, minWidth: 0 }}><div><StandardSelectField name="workerType" label="Worker type" disabled={blocked} value={workerType} onChange={value => { guard.current.profileDraft++; setWorkerType(value as "Teacher" | "Staff"); setWorkerId(""); }} placeholder="Choose worker type" options={[{ value: "Teacher", label: "Teacher" }, { value: "Staff", label: "Staff" }]} /></div><div><StandardSelectField name="workerId" label="Worker" disabled={blocked} value={workerId} onChange={value => { guard.current.profileDraft++; setWorkerId(value); }} placeholder={`Select ${workerType.toLowerCase()}`} options={workers.map(worker => ({ value: worker.id, label: worker.name }))} /></div><div><StandardSelectField name="paymentModel" label="Payment cycle" disabled={blocked} value={paymentModel} onChange={value => { guard.current.profileDraft++; setPaymentModel(value as "Monthly" | "SessionBlock"); }} placeholder="Choose payment cycle" options={[{ value: "Monthly", label: "Monthly salary" }, { value: "SessionBlock", label: "Completed-session cycle" }]} /></div>{paymentModel === "Monthly" ? <label><span>Monthly salary</span><input name="monthlyAmount" type="number" min="0.01" step="0.01" className="field" placeholder="Amount in INR" required /></label> : <><label><span>Sessions per cycle</span><input name="sessionsPerCycle" type="number" min="1" className="field" placeholder="e.g. 4 or 8" required /></label><label><span>Payout per cycle</span><input name="amountPerCycle" type="number" min="0.01" step="0.01" className="field" placeholder="Amount in INR" required /></label></>}<StandardDateField name="effectiveFrom" value={effectiveFrom} onChange={value => { guard.current.profileDraft++; setEffectiveFrom(value); }} label="Effective from" required /><button className="enterprise-action-button payroll-action" disabled={blocked || !academy || !workerId || !effectiveFrom}>Save payout profile</button></fieldset></form><form onChangeCapture={() => { guard.current.payoutDraft++; }} onSubmit={pay} className="payroll-panel" aria-busy={saving}><header className="payroll-panel-header"><div><p>Payout record</p><h2>Record payout</h2></div></header><fieldset className="payroll-fields" disabled={blocked} style={{ margin: 0, border: 0, minWidth: 0 }}><div className="payroll-wide"><StandardSelectField name="payrollProfileId" label="Payout profile" disabled={blocked} value={payoutProfileId} onChange={value => { guard.current.payoutDraft++; setPayoutProfileId(value); }} placeholder="Select active profile" options={activeProfiles.map(profile => ({ value: profile.id, label: `${profile.workerName} · ${profile.paymentModel === "Monthly" ? `Monthly ${money(profile.monthlyAmount ?? 0, profile.monthlyAmountExact)}` : `${profile.sessionsPerCycle} sessions · ${money(profile.amountPerCycle ?? 0, profile.amountPerCycleExact)}`}` }))} /></div><label><span>Pay period</span><input name="periodLabel" className="field" placeholder="e.g. September 2026" required /></label><label><span>Deductions</span><input name="deductions" type="number" min="0" step="0.01" defaultValue="0" className="field" /></label>{currentProfile?.paymentModel === "SessionBlock" && <><label><span>Sessions completed</span><input name="sessionsCovered" type="number" min="1" className="field" placeholder="e.g. 8" required /></label><label><span>Cycle gross amount</span><input name="grossAmount" type="number" min="0.01" step="0.01" className="field" placeholder="Amount in INR" required /></label></>}<div><StandardSelectField name="paymentMethod" label="Payment method" disabled={blocked} value={paymentMethod} onChange={value => { guard.current.payoutDraft++; setPaymentMethod(value); }} placeholder="Choose payment method" options={[{ value: "BankTransfer", label: "Bank transfer" }, { value: "UPI", label: "UPI" }, { value: "Cash", label: "Cash" }, { value: "Cheque", label: "Cheque" }]} /></div><label><span>Reference</span><input name="reference" className="field" placeholder="UTR / payment reference" /></label><button className="enterprise-action-button payroll-action payroll-wide" disabled={blocked || !academy || !payoutProfileId}>Record payout &amp; generate payslip</button></fieldset></form></section><section className="payroll-panel payroll-register"><header className="payroll-panel-header"><div><p>Payslip register</p><h2>Recorded payouts</h2></div></header>{!ready ? <p className="payroll-empty">Payout register unavailable until loading succeeds.</p> : payouts.length === 0 ? <p className="payroll-empty">No payouts recorded yet.</p> : <div className="payroll-table-wrap" role="region" aria-label="Payout register — scroll horizontally to view all columns" tabIndex={0}><table><thead><tr><th>Worker</th><th>Pay period</th><th>Cycle</th><th>Net payout</th><th>Reference</th><th>Action</th></tr></thead><tbody>{payouts.map(payout => <tr key={payout.id} data-payout-id={payout.id}><td><b>{payout.workerName}</b><small>{payout.workerType}</small></td><td>{payout.periodLabel}<small>{payout.paidAtUtc ? new Date(payout.paidAtUtc).toLocaleDateString("en-IN") : "No cash payment date"}</small></td><td>{payout.sessionsCovered ? `${payout.sessionsCovered} sessions` : "Monthly"}</td><td><b>{money(payout.netAmount, payout.netAmountExact, payout.currency)}</b></td><td>{payout.reference || "—"}</td><td><button type="button" onClick={() => setSelectedPayout(payout)} className="enterprise-action-button enterprise-action-button-secondary">Preview / Print</button></td></tr>)}</tbody></table></div>}</section>{selectedPayout && <PayrollPayslip payout={selectedPayout} academyName={academy?.name} onClose={() => setSelectedPayout(null)} />}</main>;
}
