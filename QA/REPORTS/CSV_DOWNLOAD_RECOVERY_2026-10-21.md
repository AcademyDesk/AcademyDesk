# CSV download outcome and duplicate-click recovery

21 October 2026. Academy `codex/penta-search`, existing dirty work preserved.
This extends the existing form/outcome program; no new backend or permission model.

## Delivered

- [x] Data Operations now catches network, HTTP, body and download-initiation errors;
  its previous fire-and-forget async handler could reject without a notice.
- [x] Both Data Operations and Finance use synchronous ref gates against same-tick
  duplicate/opposing downloads, retained through response-body completion. Visible
  actions are disabled while preparing; Data Operations exposes per-button aria-busy.
- [x] Successful non-CSV/HTML/JSON responses and missing content type are rejected;
  they are not downloaded under a misleading `.csv` filename.
- [x] Object URLs are revoked in finally, including initiation failure. No automatic
  request retry, write, data reset, permission expansion or stored-data change.
- [x] Confirmation says **download started**, not saved/downloaded: the application
  cannot certify the user's final browser Save decision or disk write.

## Validation

22 new actual-TSX controlled cases across both pages: success,403/500/network,
HTML/JSON/missing type, body/click failure, same-tick opposing attempts, body-wait
protection and explicit subsequent attempt. Combined with27 retained import and27
serializer cases: **76 passed**. Later combined Reports/inventory suite101 includes
these76; do not add both totals as distinct tests.

`QA/tools/csv-download-recovery-browser.cjs`: **20 native browser journeys**,
both actual extracted handler functions at320/1440 with normal/denied/network/
wrong-type/body-failure responses. Each makes one request despite a same-tick
opposing call. Four successful native downloads retain exact synthetic bytes and
filenames; failures create no URL/download and show guidance. No page errors.

Receipt:`QA/EVIDENCE/csv-download-recovery-1792557489738/result.json`.
Synthetic transport and extracted functions are **not** authenticated full-page,
SQL, layout, physical-device or spreadsheet-client acceptance. That evidence is
recorded separately, not implied by the20 cases.

First new controlled harness run failed because its CommonJS transpiler collided
with the existing Data Operations `exports` local and its Finance selector used an
incorrect button label. The fixture was corrected to the retained compatible
transpile mode and actual labels; no application assertions were weakened.

TypeScript/targeted lint passed before the subsequent Reports adjustment; final
exact-source build/browser evidence is recorded in the continuation handoff.
Final successor: typecheck/targeted lint and87-route export PASS; full-page
`exports-fullpage-1792557864459` verifies36 actual exported-page journeys with
synthetic APIs, including both changed download pages' light/dark320/1440 normal,
denied,network and wrong-type cases. No page errors. This supplements, not upgrades,
the earlier extracted-handler/native and independent real SQL evidence.
No backend, finance, auth, tenant, database, model or Azure changes in this slice.
No enterprise issue closed. Original28 bounded form-feedback route count unchanged.
