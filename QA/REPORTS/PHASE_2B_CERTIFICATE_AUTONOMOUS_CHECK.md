# Phase 2B — autonomous certificate follow-up

Date: 2026-10-02. User requested no manual QA steps; the prior PDF-save handoff is superseded. No action required from the user. No product changes, new app copy, database/customer writes, Azure, commit, push or deployment.

## Results

- Reused the existing accepted loopback QA copy, actual certificate React/CSS/shared controls and synthetic in-memory transport.
- Native Print / save PDF returned without an interaction timeout this time. The probe captured the saved Issued identity and actual native **beforeprint/afterprint** events; the page restored its watermarked preview and enabled controls. Observed native viewport was 319×668, not the earlier handoff screenshot's 1280×720. These events and the recovery notice do **not** prove PDF output was saved or printed.
- Computer Use listed no separately targetable Print window. No Windows UI input was sent; its skill forbids automating the Codex app UI itself. Browser skill permits the documented browser controls, which expose no PDF-export capability. No raw CDP/headless-browser substitution or recreated PDF was used to fake app output acceptance. `certificate-issued-native.pdf` does not exist at the requested evidence path; no saved PDF was inspected. Native compositor, watermark-on-manual-print, PDF pagination and physical-device gates remain **OPEN / tool-limited**, not passed. The user will not be asked to operate the dialog again for this checkpoint.
- Exercised the existing long-note scenario: 160 complete phrases, **5,280 characters**, retained in the actual saved preview at 320×812, 390×844 and 1280×720. Document widths309/379/1269 remain within those viewports; measured note scroll/client widths and heights agree. Header, title, learner, note, identity and footer bounds remain inside the article. At320, the130px signatory extends slightly beyond the127.6px footer flex box but stays within the article and is not clipped; no universal claim about arbitrary unbroken strings is made.
- The390px mobile screenshot confirms the note's end, number/code/status, issue date and signatory are reachable by normal scrolling. Full long-note screen heights are approximately7914/5428/2338px; these are screen dimensions, not PDF pagination proof. DOM reports fonts loaded, but this QA shell does not exercise the normal app's font pipeline or uploaded logos.
- Capture-only official print preparation at1280 retained all160 phrases, saved identity and footer with stateIssued. Capture mode does not invoke native printing. Close print preparation restored the visible preview watermark and enabled retry. The final browser console error/warning query returned an empty array.

These are one native lifecycle observation, three screen-layout observations and one overlapping capture-mode observation, not an extra regression-suite pass count. Prior143 controlled regressions,1019 backend and62 SQL results are retained, not rerun. The accepted source/evidence/binary consistency validator is checked again only to verify preservation, not to repeat the Astra audit.

## Evidence

[DOM/geometry ledger](../EVIDENCE/certificate-autonomous-layout-checks.json), [synthetic transport and native events](../EVIDENCE/certificate-autonomous-print-transport.json), [mobile note/identity/footer screenshot](../EVIDENCE/certificate-long-note-mobile-footer.png), [native-attempt page screenshot](../EVIDENCE/certificate-pdf-autonomous-native-attempt.png). New handoff API/frontend logs are retained under QA/EVIDENCE/logs; predecessor logs were not overwritten.

[Observation validation log](../EVIDENCE/logs/phase-2b-certificate-autonomous-validation.log) confirms the recorded geometry/content/lifecycle assertions, and [source/evidence receipt](../EVIDENCE/certificate-autonomous-source-receipt.json) pins this bounded follow-up. No independent runtime pass count or PDF closure is inferred from the receipt.

Owned QA processes PID17756/20720/20964 were revalidated before stopping. Ports49131/49132 have no listeners; temporary tab closed; viewport override reset; existing generated source/cache copy retained. The intentional stop returns nonzero process-session exits, not test failures. No normal development service was stopped.

## Next without a user handoff

Keep BUG-FUNC-0032, Phase2B and release open. Record PDF/physical-device gates as unavailable to current tools and continue the other queued Phase2B gaps; do not repeatedly ask the user to save files or rerun already accepted source audits. Future PDF acceptance requires an actual inspectable output produced through a supported surface, not a screenshot or lifecycle event. Next automatic certificate gap: uploaded-logo/image readiness and representative theme screen coverage in the isolated fixture, then remaining linked/critical coverage. Previously agreed model allocation remains Sol High; no new model evaluation or automatic model switch is performed.
