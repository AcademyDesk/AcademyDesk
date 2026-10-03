# BUG-UI-0004 — Failed platform status changes leave unsaved values selected

| Field | Value |
| --- | --- |
| Status | OPEN |
| Confirmation status | STATIC-FINDING |
| Final verification | NOT RUN |
| Severity | Moderate misleading mutation state |
| Priority | P2 |
| Category | UI |
| Module | PLATFORM |
| Role | See reproduction; same-tenant Admin unless stated otherwise |
| Screen / route | /platform/control?tab=Billing; /platform/control?tab=Support |
| API | PATCH /api/platform/billing-invoices/{invoiceId}/status; PATCH /api/platform/support-cases/{caseId} |
| Environment | Source review of local working tree; runtime production state not inferred |
| Device/viewport | NOT RUN; use QA/06_DEVICE_VIEWPORT_MATRIX.md where UI applies |
| Baseline | 20bb6047f9edf733ac8e2a226621cc582ec54b3c + pre-existing student UI diff |
| Discovery test / review ID | PLATFORM-STATUS-001 |
| Evidence classification | Static trace; runtime reproduction pending |
| Preconditions | Isolated synthetic fixture from QA/10_TEST_DATA_STRATEGY.md; never customer data |
| Reproduction frequency | Not measured; reproduction instructions are proposed |
| Source | apps/web/src/app/platform/control/page.tsx:779 |
| Class/function | invoiceStatuses / caseStatuses / request |
| Related source | INVENTORY/API_ENDPOINTS.md, CONTRACTS.md, FORMS_AND_FIELDS.md and ASYNC_FORM_RISKS.md |
| Root-cause confidence | HIGH |
| Evidence location | Source excerpt below; source fingerprint in INVENTORY/SOURCE_MANIFEST.md |
| Screenshot | Not captured on pinned baseline |
| Console logs | Not captured; required in retest |
| API request | Load an Issued invoice or Open case. Reject its status PATCH before persistence with403 or500. Select Paid or Closed and compare dropdown with fresh persisted state and invoice badge. Repeat successful change followed by external update and reload. |
| API response | Not captured for this issue; use synthetic request/response in isolated reproduction |
| Database before/after | Not executed; fixture and fresh-context assertions defined below |
| Dependencies | Safe SQL/HTTP/browser harness as applicable; desired policy review where noted |
| Fix commit | Not implemented (Phase 1) |
| Retest result | NOT RUN |
| Regression result | NOT RUN |
| Closure notes | Remain OPEN; follow closure requirements in QA README |

## Exact reproduction

Load an Issued invoice or Open case. Reject its status PATCH before persistence with403 or500. Select Paid or Closed and compare dropdown with fresh persisted state and invoice badge. Repeat successful change followed by external update and reload.

## Expected

Rejected changes restore persisted selection or clearly label an unsaved draft; successful reload reflects the latest authoritative state.

## Actual / evidence

onChange writes a per-record override before the request; shared helper catches failures without reverting it. Render always prefers the override, which is never cleared on reload, leaving an unsaved or stale status selected. Runtime NOT RUN.

Source snapshot:

```text
778:                   <header className="platform-control-panel-header"><div><p>Invoice register</p><h2>Platform invoices</h2></div></header>
779:                   {invoices.length ? <ul>{invoices.map((item) => <li key={item.id}><div className="platform-invoice-copy"><b>{item.invoiceNumber}</b><small>{item.academyName} · Due {new Intl.DateTimeFormat("en-IN", { day: "2-digit", month: "short", year: "numeric" }).format(new Date(`${item.dueDate}T12:00:00`))}{item.paymentReference ? ` · Payment ref: ${item.paymentReference}` : ""}</small></div><strong>{item.currency} {item.amount.toLocaleString("en-IN", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</strong><div className="platform-invoice-status"><span data-status={item.status.toLowerCase()}>{item.status}</span><StandardSelectField name={`invoice-status-${item.id}`} value={invoiceStatuses[item.id] ?? item.status} onChange={(value) => { setInvoiceStatuses((current) => ({ ...current, [item.id]: value })); void updateInvoice(item, value); }} placeholder="Update status" options={["Draft", "Issued", "Payment submitted", "Overdue", "Paid", "Void"].map((value) => ({ value, label: value }))} disabled={busy} /></div></li>)}</ul> : <p className="platform-billing-empty">No platform invoices have been created yet.</p>}
780:                 </section>
781:               </section>
```

## Suspected root cause

Optimistic selector state has no failure rollback or successful readback reconciliation.

## Business impact and blast radius

Platform invoice and support status controls, potentially confusing owners about collection or closure; server state is not changed by the demonstrated rejection.

## Related / required regression

PLATFORM-STATUS-001: Browser controlled failed/successful PATCH and fresh GET tests, independent records, repeated edits, network loss with uncertain outcome, concurrent updates and tab revisit; assert displayed and stored state separately.

Also run all endpoint/form cases pointing to this issue in QA/03_TEST_MATRIX.md and the critical regression suite before closure.
