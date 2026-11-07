# C18 platform overview currencies — 8 November 2026

Cloud owner. Status CLAIMED; implementation/verification pending.
Branch `codex/cloud-c18-platform-currencies-20261108`.
Clean base `cf98371e5edf57b883de41ef5786c2c90ad72e00` confirmed.
Latest Desktop inspected `88fd8e1`; C17 remains a separate published return.
No active competing Platform overview/dashboard source claim found. Keep all
Desktop SQL/protected-host/profile-audit work and shared handoffs intact.

Exact paths:

- `apps/api/Controllers/PlatformControlController.cs` — Overview only
- `apps/web/src/app/platform/page.tsx` — monetary overview tiles only
- `apps/web/src/lib/platform-finance.ts`
- `tests/AcademyDesk.Api.Tests/PlatformCurrencyOverviewTests.cs`
- `QA/tools/platform-currency-overview.test.cjs`
- `QA/tools/platform-currency-overview-browser.cjs`
- `QA/tools/frontend-page-runtime.cjs` — new helper resolver only
- `QA/tools/provisioning-ui.test.cjs` — real helper import adapter only
- `QA/ISSUES/BUG-DATA-0016.md`
- `QA/HANDOFF/C18_PLATFORM_CURRENCIES_2026-11-08.md`

Reproduce invalid mixed-currency totals and rounded cents. Add explicit
per-currency billed/collected/outstanding groups, retain single-currency and
empty scalar values, and return null mixed monetary scalars rather than a
fictional converted total. Reuse C17's pure currency formatter, not its academy
ledger contract. Keep Draft/Void out of billing, Paid in collections and
Issued/Overdue/Payment submitted in outstanding. No payment confirmation,
status, permission, onboarding, academy, audit or write-flow change.

Static caller review found the owner dashboard and the existing SQL billing
harness. The latter asserts single-INR values and retains that contract.
Both platform/control billing rows and their modal already label each amount
with currency; they have count-only summary tiles and require no money change.
The separate academy Dashboard contract/fees-plus-payroll tile is NOT claimed.

Validate before/after money/state matrices, real Identity/HTTP owner/denial
checks with isolated InMemory stores, actual TSX plus retained provisioning,
actual Chromium, build/types/lint and full API/frontend after final source.
Real SQL, media/provider/device, full critical and production acceptance stay
Desktop-owned. No new dependencies/images, configured SQL, external sends,
main/Azure/environment/public-network changes or model loading/activation.
Keep BUG-DATA-0016 and linked P0/release gates OPEN. Sol High for monetary
contract/authority/SQL; Medium for settled import/docs.
