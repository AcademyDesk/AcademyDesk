# C23 family finance integration — 9 November 2026

The Desktop branch imported all 15 returned paths exactly from Cloud
`5fbc75f93c0dc9f0e18bb59a9da4a68811708e9c`, after the local cross-tab
session checkpoint `069e318`. The two returns do not overlap. No payment
writer, migration or model configuration changed in C23.
[Cloud implementation and evidence](../HANDOFF/C23_PRIORITY_FINANCE_READERS_2026-11-08.md)

The family balance, invoice download and reminder now count only accepted
payments on the same tenant and invoice, in the same normalized currency.
The family view shows currency and cents, keeps cancelled history out of
the amount due, and uses the latest server finance permission.

All 18 targeted frontend cases passed locally. Cloud reported 2,235 passing
API tests and 24 Chromium scenarios with controlled HTTP. Its full frontend
run had three failures because the communication SQL receipt was absent.
The issue inventory re-created both reports from Cloud LF inputs on Desktop.
It lists 120 records without missing, duplicate or unknown entries and keeps
their recorded acceptance states.

The first local SQL window stopped at the memory guard after compiling the
API. The successor reused that artifact on identical source, compiled the
SQL harness with zero errors or warnings, and verified the API DLL hash copied
into the harness runtime. The RAM guard was retained.

## Disposable SQL acceptance

Window `fc2bae94467c4d46a4900f4812a88c73`, SQL run
`f77a0d8403344e11948754b8b8a6e0f0`, passed the 16 existing C13/C14 and
compensation checks and all 20 new C23 checks.

| Scope | Verified |
| --- | --- |
| 12 family JSON and HTML checks | Student and linked Guardian agreed across all six payment states. A USD 100.55 invoice with USD 0.05 adjustment and same-tenant USD 20.10 receipt showed USD 80.40 for Completed or Reconciled, and USD 100.50 for the four other states. A same-invoice INR 70.15 receipt and a foreign-academy USD 500 receipt never offset it. Read snapshots were unchanged. |
| Six reminders | The authorized Admin's explicit InApp queue used the same amounts. Each eligible action created one scoped notification plus the existing actor and summary audit records. Invoice and payment rows were preserved. |
| Two permission checks | With the same signed-in Guardian session, removal hid finance, returned no invoice rows and denied the download with 403. Restoring the grant returned the finance view and download. |

An earlier attempt used a padded currency fixture SQL could not store in its
three-character field. The successor used lowercase `usd`, which tests
normalization within the actual schema. A native exit-zero run followed.
Its new summaries needed the existing `CLOUDRETURN` log prefix to appear
in the filtered receipt. The final run contains the explicit checks and
successful database, login and container cleanup. The earlier evidence
remains local.

The new C23 application paths still need a live family browser run with
real invoice downloads, physical-device acceptance and exact hosted
verification. The five P0 records and broader session gate remain open.
Staging restore and rollback are still required. No Azure deployment or
model activation took place.
