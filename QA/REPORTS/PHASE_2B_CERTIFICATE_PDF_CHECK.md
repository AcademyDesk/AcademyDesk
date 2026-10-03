# Certificate PDF check — BUG-FUNC-0032

Date: 2026-10-03. Branch: `cursor/certificate-validation`. Model: Grok 4.7 in Cursor. No product source change. No Azure deploy. No merge to `main`.

## Result

BUG-FUNC-0032 stays **OPEN**.

Synthetic official PDF text matches the saved Issued row, and a draft print is watermarked and has no certificate number. That is not enough to close the issue. The same official PDF is three letter pages that each repeat the full certificate. Linked authentication plus disposable SQL was not run. A physical phone was not available, so the device check is **NOT RUN**.

## What was executed

Controlled checks, font-adapted runner only:

`node --test QA/tools/certificate-font-ui.test.cjs QA/tools/certificate-image.test.cjs QA/tools/certificate-font.test.cjs`

66 passed, 0 failed. The raw `certificate-print-ui.test.cjs` file was not run on its own. That file fails when Node loads it without the font and image aliases. That failure is the harness, not a product regression.

Synthetic loopback page: `http://127.0.0.1:49131/certificates`, fixture API `http://127.0.0.1:49132`. Headless Chrome `Page.printToPDF`. PDF text read with pypdf. Local files stayed under `.build-check/certificate-pdf/` and are not committed.

## Official PDF

Saved record selected: `CERT-SYNTHETIC-001`. Draft title field set to `UNSAVED DRAFT TITLE` before print. Print button was enabled.

`issued.pdf`: 3 pages, each 612×792. Extracted text on every page:

- Synthetic QA Academy
- Saved achievement
- Saved Learner
- Saved programme
- Saved recognition
- Certificate: CERT-SYNTHETIC-001
- Verification: VERIFY-SYNTHETIC-001
- Status: Issued
- Issued 2020-02-29
- Saved Signatory

`UNSAVED DRAFT TITLE` is absent. `PREVIEW` is absent.

DOM at the print call matched that saved text (`data-print-state=issued`).

## Draft PDF

`draft.pdf` was printed before an Issued record was selected. 3 pages. Text includes `PREVIEW — NOT AN ISSUED PRINT`, `Certificate of Achievement`, `Not issued — date set on issuance`, and `Independent programme`. It does not contain `CERT-SYNTHETIC-001` or `VERIFY-SYNTHETIC-001`.

## Revoked record

`CERT-SYNTHETIC-REVOKED` can be selected. Print / save PDF stays disabled. The article remains `data-print-state=preview` with the draft watermark. It does not print as a newly Issued credential.

## Layout

Pagination is not accepted. Chrome wrote the official certificate three times, once per letter page. Print CSS uses `position: fixed` on `.certificate-preview`, which repeats across pages.

Screenshots taken after `Page.printToPDF` show the certificates screen again, because print completion clears the print view. They are not a second copy of the PDF pages. Earlier 390 and 1280 on-screen geometry from the browser and appearance checks stays historical and was not rerun as a new pass.

Theme for this PDF is the saved school-merit record (`Saved achievement` / `theme-school-merit` on the print article). Recital and Eid checks from the appearance report were not repeated.

## Not run

- Linked portal: issue and print through the authenticated app against disposable SQL, then match the register row, printed identity, and audit. Historical 62 HTTP/SQL passes are not this check.
- Physical phone. Simulated 390 is not that check.
- Failed and replaced print paths were not repeated in this PDF capture. Revoked selection was.

## Next

Same issue, linked authentication and disposable SQL portal path. Then the open P0 queue: BUG-DATA-0001, BUG-DATA-0002, BUG-DATA-0003, BUG-DATA-0010, BUG-SEC-0001. Do not merge this branch on PDF text alone.
