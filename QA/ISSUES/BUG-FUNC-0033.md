# BUG-FUNC-0033 — Payroll Print / Save PDF has no visible selected payslip in print output

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Major payroll document workflow failure |
| Priority | P1 |
| Category | FUNC |
| Module | PAYROLL |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /payroll |
| API | GET /api/academies/{academyId}/payroll/payouts (print uses loaded page only) |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PAYROLL-PRINT-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/payroll/page.tsx:24 |
| Class/function | PayrollPage payout register print action / global print CSS |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | In an isolated academy with two synthetic payout rows, open /payroll and click Print / Save PDF on the second row. Capture browser print preview/PDF and compare it with that row’s stored payslipNumber, gross, deductions, net, worker and period. Repeat first row, no rows and mobile layout. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

In an isolated academy with two synthetic payout rows, open /payroll and click Print / Save PDF on the second row. Capture browser print preview/PDF and compare it with that row’s stored payslipNumber, gross, deductions, net, worker and period. Repeat first row, no rows and mobile layout.

## Expected

The selected payout prints a readable, row-specific payslip with its stored identity and money breakdown. Other records and unrelated app UI are excluded from that document.

## Actual / evidence

Every register-row button calls window.print() with no row selection or payslip document. Global @media print hides body descendants except .invoice-paper and .certificate-preview; the payroll page renders neither, so source CSS predicts a blank print page. The register also omits gross/deductions and payslipNumber. Static source trace; browser output NOT RUN.

Source snapshot:

```text
23:   const teacherProfiles = activeProfiles.filter(profile => profile.workerType === "Teacher").length; const paidThisCycle = payouts.reduce((sum, payout) => sum + payout.netAmount, 0);
24:   return <main className="enterprise-settings finance-module payroll-standard"><header className="payroll-heading"><span className="payroll-title-icon" aria-hidden="true">₹</span><div><p>Finance</p><h1>Teacher &amp; staff payouts</h1></div></header>{message && <p className="payroll-notice" role="status">{message}</p>}<section className="payroll-kpis"><article><span>Active payout profiles</span><b>{activeProfiles.length}</b><small>{teacherProfiles} teacher and {activeProfiles.length - teacherProfiles} staff profiles.</small></article><article><span>Recorded payouts</span><b>{payouts.length}</b><small>Payslips generated in the register.</small></article><article><span>Paid value</span><b>{money(paidThisCycle)}</b><small>Total amount captured in this register.</small></article></section><section className="payroll-layout"><form onSubmit={createProfile} className="payroll-panel"><header className="payroll-panel-header"><div><p>Profile setup</p><h2>Set up payout profile</h2></div></header><div className="payroll-fields"><label><span>Worker type</span><StandardSelectField name="workerType" value={workerType} onChange={value => { setWorkerType(value as "Teacher" | "Staff"); setWorkerId(""); }} placeholder="Choose worker type" options={[{ value: "Teacher", label: "Teacher" }, { value: "Staff", label: "Staff" }]} /></label><label><span>Worker</span><StandardSelectField name="workerId" value={workerId} onChange={setWorkerId} placeholder={`Select ${workerType.toLowerCase()}`} options={workers.map(worker => ({ value: worker.id, label: worker.name }))} /></label><label><span>Payment cycle</span><StandardSelectField name="paymentModel" value={paymentModel} onChange={value => setPaymentModel(value as "Monthly" | "SessionBlock")} placeholder="Choose payment cycle" options={[{ value: "Monthly", label: "Monthly salary" }, { value: "SessionBlock", label: "Completed-session cycle" }]} /></label>{paymentModel === "Monthly" ? <label><span>Monthly salary</span><input name="monthlyAmount" type="number" min="1" step="0.01" className="field" placeholder="Amount in INR" required /></label> : <><label><span>Sessions per cycle</span><input name="sessionsPerCycle" type="number" min="1" className="field" placeholder="e.g. 4 or 8" required /></label><label><span>Payout per cycle</span><input name="amountPerCycle" type="number" min="1" step="0.01" className="field" placeholder="Amount in INR" required /></label></>}<StandardDateField name="effectiveFrom" value={effectiveFrom} onChange={setEffectiveFrom} label="Effective from" required /><button className="enterprise-action-button payroll-action" disabled={!academy || !workerId || !effectiveFrom}>Save payout profile</button></div></form><form onSubmit={pay} className="payroll-panel"><header className="payroll-panel-header"><div><p>Payout record</p><h2>Record payout</h2></div></header><div className="payroll-fields"><label className="payroll-wide"><span>Payout profile</span><StandardSelectField name="payrollProfileId" value={payoutProfileId} onChange={setPayoutProfileId} placeholder="Select active profile" options={activeProfiles.map(profile => ({ value: profile.id, label: `${profile.workerName} · ${profile.paymentModel === "Monthly" ? `Monthly ${money(profile.monthlyAmount ?? 0)}` : `${profile.sessionsPerCycle} sessions · ${money(profile.amountPerCycle ?? 0)}`}` }))} /></label><label><span>Pay period</span><input name="periodLabel" className="field" placeholder="e.g. September 2026" required /></label><label><span>Deductions</span><input name="deductions" type="number" min="0" step="0.01" defaultValue="0" className="field" /></label>{currentProfile?.paymentModel === "SessionBlock" && <><label><span>Sessions completed</span><input name="sessionsCovered" type="number" min="1" className="field" placeholder="e.g. 8" required /></label><label><span>Cycle gross amount</span><input name="grossAmount" type="number" min="1" step="0.01" className="field" placeholder="Amount in INR" required /></label></>}<label><span>Payment method</span><StandardSelectField name="paymentMethod" value={paymentMethod} onChange={setPaymentMethod} placeholder="Choose payment method" options={[{ value: "BankTransfer", label: "Bank transfer" }, { value: "UPI", label: "UPI" }, { value: "Cash", label: "Cash" }, { value: "Cheque", label: "Cheque" }]} /></label><label><span>Reference</span><input name="reference" className="field" placeholder="UTR / payment reference" /></label><button className="enterprise-action-button payroll-action payroll-wide" disabled={!academy || !payoutProfileId}>Record payout &amp; generate payslip</button></div></form></section><section className="payroll-panel payroll-register"><header className="payroll-panel-header"><div><p>Payslip register</p><h2>Recorded payouts</h2></div></header>{payouts.length === 0 ? <p className="payroll-empty">No payouts recorded yet.</p> : <div className="payroll-table-wrap"><table><thead><tr><th>Worker</th><th>Pay period</th><th>Cycle</th><th>Net payout</th><th>Reference</th><th>Action</th></tr></thead><tbody>{payouts.map(payout => <tr key={payout.id}><td><b>{payout.workerName}</b><small>{payout.workerType}</small></td><td>{payout.periodLabel}<small>{new Date(payout.paidAtUtc).toLocaleDateString("en-IN")}</small></td><td>{payout.sessionsCovered ? `${payout.sessionsCovered} sessions` : "Monthly"}</td><td><b>{money(payout.netAmount)}</b></td><td>{payout.reference || "—"}</td><td><button type="button" onClick={() => window.print()} className="enterprise-action-button enterprise-action-button-secondary">Print / Save PDF</button></td></tr>)}</tbody></table></div>}</section></main>;
25: }
26:
```

## Suspected root cause

The payroll row action has no printable payslip target and inherits print rules for other document pages.

## Business impact and blast radius

Academy payroll operators cannot obtain the promised selected payslip from this register; a printed document cannot be reconciled to a payout row.

## Related / required regression

PAYROLL-PRINT-001: Browser print/PDF snapshots for each synthetic payout row and no-row state; assert selected-row payslipNumber, gross/deductions/net, period, worker and status, exclude other rows, test mobile and browser print engines.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
