# Admin Finance acceptance checklist

Run these checks after starting the API and web application. Use a disposable invoice and payment where the action changes financial data.

| Scenario | Expected result |
| --- | --- |
| Academy Admin opens `/finance` | Finance workspace shows metrics, finance work areas, export actions, and either real collections or a clear empty state. |
| Academy Admin issues an invoice then records a payment | The payment is visible in the ledger and the invoice balance/status updates. |
| Academy Admin reconciles the payment | A bank, UPI, cash-book, gateway, or cheque reference is required; after saving, status is `Reconciled` and the evidence is visible. |
| Academy Admin submits an adjustment | The request appears as `Pending approval` in the adjustment register and Finance Control Centre. |
| Academy Admin approves an adjustment | The adjustment record is approved; the invoice adjustment amount and status update correctly. |
| Academy Admin rejects an adjustment | A decision reason is required; the request is rejected and the invoice balance remains unchanged. |
| Academy Admin creates a collections follow-up | An overdue invoice can create a collection task with priority, due date, note, escalation stage, and optional promise-to-pay date. |
| Finance user opens the application | Navigation is limited to Finance work areas. Finance endpoints and collection tasks remain available; unrelated academy modules are denied. |
| Finance user requests another academy's finance URL | The request returns `403 Forbidden`; no other academy data is returned. |
| Invalid requests | Negative/zero adjustment and payment values, missing reconciliation evidence, duplicate invoice numbers, and payment amounts beyond invoice balance are rejected with feedback. |
| CSV exports | Invoice and payment CSV downloads contain the expected finance ledger columns and only the active academy's data. |

Record the date, tester, sample invoice number, and any finding in the Activity Log or the delivery tracker before Finance is frozen.
