# Phase 2B — certificate PDF output handoff

Superseded on2026-10-02 by [autonomous follow-up](PHASE_2B_CERTIFICATE_AUTONOMOUS_CHECK.md). User requested no manual steps. Native attempt/layout checks completed within supported tools; no PDF output acceptance claimed. Owned services/tab have now been cleaned up; historical handoff instructions and PID snapshots below are not current actions to request.

Date: 2026-10-02. Predecessor: PHASE_2B_CERTIFICATE_BROWSER_CHECK.md (accepted consistency validator passed before this preparation). BUG-FUNC-0032, Phase 2B and release remain OPEN. No product edits, new independent app copy, commit, push, Azure, SQL or customer-data changes.

## What is ready

Existing hash-matched copy reused: `D:\AcademyDesk\.build-check\certificate-browser-df5de88d8171499f88a73ef3dc5e47a5`.

Loopback-only frontend `http://127.0.0.1:49131/certificates`; synthetic API `http://127.0.0.1:49132`. The real certificate page/CSS/select/date/API sources match the accepted source. Fixture root/transport/probe remain QA-only, not full portal authentication/SQL evidence. Both ports were free before startup.

Codex in-app Browser ID2, handoff tab ID1: saved `CERT-SYNTHETIC-001 · Saved achievement · Issued` selected; number `CERT-SYNTHETIC-001`, verification `VERIFY-SYNTHETIC-001`, learner Saved Learner, programme Saved programme, date 2020-02-29, note Saved recognition, signatory Saved Signatory. QA mode explicitly set to **native**, so the normal Print / save PDF button will invoke the original browser print action, not capture-only mode. Print enabled; current article remains watermarked **preview** until the actual official-print preparation.

[Prepared page screenshot](../EVIDENCE/certificate-pdf-save-handoff.png). No native print was invoked this turn; no PDF was saved/read/rendered. Screenshot is readiness evidence only.

## Why user input is needed

Connected surfaces list only in-app Browser and MCP Apps, no connected external browser. Advertised browser capabilities are visibility and viewport; there is no supported native PDF-export/print-compositor automation. Earlier native-call cancellation recovery is retained, not repeated or promoted to output acceptance. Browser skill confines control to its supported surface; no raw CDP, replacement headless browser, forced DOM changes or fabricated ReportLab reproduction is used as proof of the app's real print output. PDF skill requires visual inspection of the saved PDF before acceptance.

User step: click **Print / save PDF**, select **Save as PDF** (or Microsoft Print to PDF), save the synthetic output as `D:\AcademyDesk\QA\EVIDENCE\certificate-issued-native.pdf`, then report Saved or attach the file. Do not choose a physical printer. If no dialog appears, report that instead. Optional print settings should be recorded, not silently treated as a universal device result.

Next: inspect that actual PDF's pages, text/identity, watermark absence for official output, clipping/overlap and pagination. Then obtain a separate manual/draft print to check the watermark, followed by long-content/logo/theme and physical-device gates. No new runtime pass count or issue closure claimed here.

## Owned services intentionally left for handoff

Frontend execution session56350: listener PID20720, parent20964. Synthetic API session58597: listener PID17756, parent16180. These identifiers are snapshots, not authority to stop a later reused PID; revalidate exact commands/ports before cleanup. Logs use new names `phase-2b-certificate-pdf-handoff-frontend.log` and `phase-2b-certificate-pdf-handoff-api.log`; prior accepted logs were not overwritten. Services and marked tab intentionally remain available for the requested user print step. No temporary viewport override was set this turn; observed default viewport1280x720. Stop only these owned processes and close the temporary tab after output capture or cancellation.
