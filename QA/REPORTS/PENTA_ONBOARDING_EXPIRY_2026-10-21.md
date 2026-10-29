# PENTA onboarding expiry — 21 October 2026

Worktree `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Continues the named E5-R27
expired-session/history gap; not a new testing plan or model qualification.

## Scope

- Four fresh browser contexts cover selected and pending conversations, each with
  session expiry or draft-only expiry. Draft-only cases retain an unexpired
  conversation. Tested viewports are 320px/light and 1440px/dark, not the complete
  Cartesian product of every viewport, theme and expiry type.
  Session here means the PENTA conversation lifetime, not JWT expiry or Identity
  refresh-token revocation; those retain their separate authentication tests.
- Actual disposable SQL, Identity, HTTP services and the existing exported frontend;
  no fabricated API bodies, inference provider or model activation in the new cases.
- Initial authorized reads expose the selected read-only draft, or withhold it for
  pending claims. Explicit host-state recheck and subsequent reload must return
  HTTP410, remove the cached private value and all actionable onboarding controls,
  and retain the Manual student-onboarding link. No result polling or write replay.
- A Node-only loopback controller advances the exact fixture's isolated host clock
  after observing its successful current-state read. It requires a random in-memory
  secret, exact owned fixture and single use. It rechecks persisted revision/status/
  pending identity before advancing. Protected records and their expiry fields are
  not edited: those fields are cryptographically bound, so editing them would test
  tampering, not expiry. No real system clock or production service is changed.
- Browser traffic is GET-only. The four controller POSTs are separate test actions;
  their secret is never delivered to browser code/storage/URLs or evidence. Synthetic
  account bearer and control secret pass over process stdin, not command arguments.
- Independent SQL postconditions require identical selected snapshots (students,
  guardians/links, invoices/payments, Identity users/memberships, academies/branches,
  PENTA sessions/drafts/turns), exactly eight actor/academy-scoped read audits, two still-pending claims
  with no completion receipts, and two explicit expired-result API denials. The new
  journeys must create zero students; retained operation/dialogue journeys have
  their own intentional synthetic creation fixtures.

## Validation

- [x] Isolated SqlHarness build: zero warnings/errors, 35.08s.
- [x] Node syntax checks for both affected live-browser tools.
- [x] Ten new expiry adapter checks plus retained review48/recovery42/dialogue36/
  terminal33: **169 distinct controlled checks PASS**. These are synthetic transport
  checks, not real model or SQL acceptance. Node's wrapper count is not this total.
- [x] Corrected full runner: `QA_ONBOARDING_REVIEW_BROWSER=1` with
  `pwsh -NoProfile -File QA/tools/SqlHarness/Run-ReconciledPayment.ps1 -Module OnboardingAtomic`.
  Run **`4a6621078bf54a989fa9d241f8b7692e`**, **exit0 /202s**.
- [x] **461 retained SQL/API checks and 33 actual browser contexts PASS**:
  15 review, four operation, five Start, three dialogue, two pending, four new expiry.
  Three nested terminal reload/re-entry journeys remain within operation contexts,
  not three extra contexts. Counts are different evidence layers, not a grand total.
- [x] New expiry contexts: **28 actual browser GETs**, eight observed HTTP410
  current-state responses, zero browser writes, zero result polling, no unexpected
  page/console errors or HTTP429. Expected resource diagnostics for the checked
  HTTP410 responses are recorded separately, not misreported as no console output.
- [x] Independent postconditions: eight exact read audits, two pending claims
  unchanged/no receipts, two expired-result API410 responses, selected snapshots
  byte-identical and zero students created in the new cases. Retained operation
  cases create three synthetic students and dialogue creates one after approval;
  those are separate from the new zero-write cases.
- [x] Runtime route inventory360 and exact owned DbContext/login targets verified;
  102 application and seven Identity migrations applied. Mismatched cleanup marker
  rejected; owned database/login teardown confirmed. Exact successful container
  absent after runner cleanup. Failed stopped container remains. Observed final
  inventory: 29 containers/41 volumes; only the two PENTA services running.
- [x] Inspected light/mobile selected-draft and dark/desktop pending-session expiry
  screenshots. No horizontal overflow at tested widths. Physical devices NOT RUN.

Logs: `.build-check/session-resume/onboarding-expiry-build-clock.log`,
`onboarding-expiry-contract-final.log`, `onboarding-expiry-contract-rerun.log`,
`onboarding-expiry-sql-clock.log`. Evidence remains local under `QA/EVIDENCE/`.
New receipts are in
`penta-onboarding-expiry-live-4a6621078bf54a989fa9d241f8b7692e/`:
`result.json`, `sql-postconditions.json` and four expiry screenshots. Retained
same-run browser folders contain their separate request/audit receipts.

Receipt SHA256: result
`3B36D88E6EF28035EC41646E1564F1B93B25CB95250B0FC62D67F84007E1F617`;
SQL postconditions
`BB9745FBA3CED6A3287DAB5CDD693D2C600ADB9AAF30EBA6B084674A6B63D557`.

### Tested source identities (SHA256)

| Source | SHA256 |
| --- | --- |
| `QA/tools/SqlHarness/PentaOnboardingExpiryBrowserRegression.cs` | `38926C6D4DC1B444616F953898A3EC3DBA531F4F91D267F0D81D7B9289A7DB67` |
| `QA/tools/penta-onboarding-expiry-live.cjs` | `69021C91DBB448741FDD974682498D84309C421520D97065FEB8DC48FD78FD50` |
| `QA/tools/penta-onboarding-expiry.test.cjs` | `9A3AA8298BC7D6F96C8A71EA020AC592769C3580DBF2A8E4B36AB8302B66D02C` |
| `QA/tools/penta-onboarding-review-live.cjs` | `918A88268FA14239E7AF1D06D1AB94991D39A0EF5BE76D14A84AC850EEE6EC47` |
| `QA/tools/SqlHarness/OnboardingAtomicRegression.cs` | `CBA7348A1C2727263B379B624F4E293BACD1E0BBBAE2414D448FD2B3EAB4AB44` |

## Failed/incomplete attempts retained

- RAM preflight initially stopped before container creation; the 3GiB startup
  guard was not reduced. A Windows PowerShell invocation also stopped at its
  execution-policy check before starting SQL; the established `pwsh` runner is used.
- Run `e0909d5d0cfe4aeba70f70773ec9b716` failed on the retained review-browser
  logout expectation, before reaching the new expiry cases. The shared workspace
  boundary correctly renders **Session ended**, unmounts the private workspace,
  and offers **Sign in again**. The stale fixture expected an inner PENTA notice.
  Updated only that fixture to assert the actual shared boundary, `/login` link,
  absence of private controls and absence of further reads. Other assertions remain.
- That failed run is not a pass. Its exact owned container was verified and stopped,
  retained rather than removed; failure receipt/screenshot/log remain. No pruning.
- The early expiry fixture design would have edited expiry columns. It was replaced
  with isolated clocks before the corrected run. No accepted evidence is claimed for
  that design; the initial failing run never reached it.

## Remaining work and boundaries

Durable history, wider roles/workflows, qualified model inference, linked domain/
approval/security gates, physical Android/iOS, enterprise and release acceptance
remain OPEN. BUG-DATA-0001 remains OPEN. No application/schema/security/default/
model/weights/training change in this batch. No main edit, stage, commit, push,
Azure deployment, production data access, install, download or resource restart.

Retain the prior zero-error/zero-warning frontend lint, 87-route export, 438 selected
checks and 172 synthetic-browser cases as prior evidence, not rerun counts here.
All 11 broad E0–E10 packages remain open at their outstanding acceptance gates.
Stay Sol High for authority/integration/model qualification; Medium for settled UI
and routine documentation. No project switch or automatic model switch required.
