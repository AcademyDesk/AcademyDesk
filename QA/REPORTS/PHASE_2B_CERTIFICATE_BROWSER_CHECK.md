# Phase 2B — saved certificate real-browser check

Date: 2026-10-02 (host/browser UTC logs: 2026-10-01). Model allocation: Sol High. Issue: BUG-FUNC-0032. Result: bounded browser evidence added; issue, Phase 2B and release remain OPEN.

## Outcome and boundary

Actual certificate TSX, React effects, shared select/date controls, API client, ThemeProvider and CSS ran in the Codex in-app Browser. The transport is a loopback, in-memory synthetic fixture; the root layout/print probe are QA-only. This is real DOM evidence, **not** authenticated full-portal, actual API/SQL, native PDF output or physical-device acceptance. No customer records, account credentials, Azure, provider storage or normal development database/services were used.

The only product change this slice is one certificate-scoped CSS rule restoring the hidden logo file input to a clipped 1×1 element. Broad input styles had defeated the screen-reader-only sizing/padding and caused horizontal page overflow. No certificate TSX, shared control, API, schema, permission or migration change was needed. The final source copy matches the product page, CSS, select/date controls and API client by hash.

## Browser observations

[Complete fact ledger](../EVIDENCE/certificate-browser-observations.json): 25 observations, 24 positive and one retained QA expectation mismatch. Observations overlap; this is not 24 independent regression test cases.

- Draft-only state: no selected saved record and official Print disabled. Saved selection collapses the real dropdown and shows register number/code/status.
- Actual print-call DOM uses saved learner, programme, title, date, recognition note and signatory, excluding later unsaved edits to those controls. The clean-reload test exercised real select/date/text inputs, including year 2020 → February → 29; the date control closed correctly.
- Cached revoked selection is disabled. Server-side synthetic revocation/replacement between selection and print is blocked by the fresh read; failed pre-print GETs retain the draft and unlock controls.
- Synthetic rejected issuance clears stale print selection and shows no false success. Confirmed synthetic issuance selects the returned new ID, not the first register row. The fixture deliberately submitted strings named `UNSAVED DRAFT TITLE`/`UNSAVED PRIVATE NOTE` once; these therefore became legitimate saved data for new-c1, not leaked draft text.
- Print-call capture mode records actual DOM after React rendering, but deliberately does not invoke native printing. Synthetic printer exceptions show the retry notice, restore preview and retain the note. An independent saved certificate with null batch/notes prints Independent programme without borrowing a draft batch/note.
- Manual close and a **simulated**, QA-dispatched afterprint event restore the preview watermark. The simulated event is not native-dialog proof.
- Native mode invoked the original window.print once. The browser command timed out while the native call paused interaction; Escape cancelled it and the page recovered with its print-closed notice. Computer Use found no separate OS Print window and supplied no input. No PDF was saved, no native preview/PDF was visually inspected, and no physical print was sent. This proves only observed cancellation recovery, not compositor/event/PDF correctness.
- Final mobile 390×844: document width 380; hidden logo 1×1; certificate preview right edge ~319.2. Dropdown gap ~5.94 px below its trigger, inside the viewport, and hit-testing reaches the listbox. Final desktop 1280×720: document width 1270; logo 1×1; settled dropdown gap ~6.14 px and width/alignment match its trigger. These are viewport simulations, not iOS/Android hardware tests. The mobile screenshot also shows expected note/selector separation.
- Final clean-reload browser error/warning log query returned an empty array. This does not erase initial QA setup errors or deliberate failure responses.

## Evidence and non-product failures retained

[Synthetic transport](../EVIDENCE/certificate-browser-synthetic-transport.json) retains 101 requests and 6 print records (including the simulated event). This fixture is not an implementation of production validation/security. [Frontend log](../EVIDENCE/logs/phase-2b-certificate-browser-frontend-ready.log) retains the initial missing ThemeProvider error; the QA layout was corrected and subsequent page requests returned 200. The normal product layout was unchanged.

The first launch pipeline used a relative Tee-Object path after changing directory and failed before any listener survived; terminal evidence was observed, but no first-attempt log file exists. A date-day locator initially matched two visible 29 buttons; the grounded current-month locator corrected that QA ambiguity. A mobile assertion expected saved-c1 while the currently selected record was newly issued new-c1; the false observation is retained, not counted as a product failure or deleted. A clean reload and explicit saved-c1 selection passed. Intermediate viewport dimensions varied (559/550) and one desktop geometry query caught stale mobile placement during resize; final accepted geometry is explicitly 390/1280 with settled layout. An unsupported viewport introspection call failed; documented set/reset usage was used to finish. The final screenshot below is from the verified final 390-pixel geometry, not those intermediate captures.

[Final mobile dropdown](../EVIDENCE/certificate-browser-mobile-dropdown-final-verified.png), [settled desktop dropdown](../EVIDENCE/certificate-browser-desktop-dropdown-settled.png), and [initial mobile overflow](../EVIDENCE/certificate-browser-mobile-before.png) provide visual evidence. Other intermediate screenshots are retained in the source snapshot for provenance; cropped/transient captures are not acceptance proof. Screenshots display synthetic names and a QA shell.

## Regression and preservation

[Current regression log](../EVIDENCE/logs/phase-2b-certificate-browser-regression.log): **143/143 PASS** (50 print + 48 eligibility + 45 compliance), zero failures/skips/cancellations. Prior TypeScript and lint evidence retained, not rerun because product TSX was unchanged; Next dev compiled the real page, not a production build. Earlier 1019 backend and 62 certificate HTTP/SQL results are retained, not rerun. The [accepted predecessor](PHASE_2B_CERTIFICATE_PRINT_UI_REPAIR.md), its snapshot/logs, API/security/schema/shared sources, 96 QA binaries and two normal assemblies remain hash-identical except six declared CSS/checkpoint/issue updates. Normal `git diff --check` passes; usual LF/CRLF warnings are not suppressed.

[Verification boundary](../EVIDENCE/certificate-browser-verification.json) and [cleanup/source-copy evidence](../EVIDENCE/certificate-browser-cleanup.json): exact owned processes stopped; ports 49131/49132 have no listeners; temporary browser tab closed; viewport reset. Both generated source/cache copies remain recoverable under .build-check; no recursive junction deletion. Deliberately stopping the owned processes returned nonzero session exits, not application test failures. No commit, push or deployment was done.

The [first checkpoint validator run](../EVIDENCE/logs/phase-2b-certificate-browser-validator-initial.log) wrongly expected saved-c1 in every print capture, including the correctly selected native-mode saved new-c1. Its failure is retained as a QA-validator expectation error. The corrected validator checks each explicit fixture identity and verification code and still rejects later unsaved text; no capture or product code was altered to make it pass.

## Still open / next

Saved PDF/native-compositor inspection (issued identity versus watermarked manual print, pagination, clipping, long content, logos/fonts, themes), full signed-in shell, physical Android/iOS and linked/critical/release checks remain open. Current saved reference names/branding are used; immutable original name/branding snapshots are not stored. Separate-read status races remain open. This work does not close shared dropdown keyboard/short-space placement findings or certify all portal navigation.

Next: same certificate PDF-output/layout and device verification; **Sol High**. Do not restart the completed Astra source audit or claim issue closure from these synthetic browser checks.
