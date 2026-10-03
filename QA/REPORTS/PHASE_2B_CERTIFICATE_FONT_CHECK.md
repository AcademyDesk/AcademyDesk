# Phase 2B — Certificate font readiness successor

2026-10-02. Bounded local repair verified; BUG-FUNC-0032, Phase 2B and release remain OPEN. Continue the previously agreed Sol High allocation. No Azure, commit/push/deploy, API/schema, normal service or dev/customer database change.

## Outcome

An actual delayed Geist font exposed a print race in the accepted appearance page: the print callback ran while the font face was still loading. The generated QA copy alone was temporarily restored to that accepted source hash for reproduction; the product repair was not rolled back. [Corrected baseline](../EVIDENCE/certificate-font-corrected-baseline.json) and [final transport](../EVIDENCE/certificate-font-transport.json) retain the request/completion ordering.

The product now resolves the mounted certificate's layout, then awaits font readiness after the existing logo load/decode guard. A ten-second bounded wait, rejection handling and AbortController cancellation prevent an indefinite wait or printing after close/unmount. Failures return to preview with a retryable notice. The Font Loading API's ready promise settles after loading/layout, but is not a guarantee that every face succeeded: stable browser fallback remains permitted. Glyph coverage and PDF embedding are not proven. [MDN ready contract](https://developer.mozilla.org/en-US/docs/Web/API/FontFaceSet/ready).

## Verification and retained failures

- 66/66 controlled checks PASS: 50 original print cases rerun through a successor adapter, eight retained image-helper checks, eight new font-helper checks. Timeout/rejection/throwing-getter/already-aborted cases are controlled helper checks, not observed whole-page timeout cases. [Regression log](../EVIDENCE/logs/phase-2b-certificate-font-regression.log).
- Product TypeScript exit 0; targeted lint exit 0, zero errors/two inherited image warnings. [Static checks](../EVIDENCE/logs/phase-2b-certificate-font-static-checks.log).
- Real browser: delayed valid Geist held preparation; printing occurred after font completion. Close restored the watermarked preview and no print occurred when the cancelled load subsequently completed. Combined delayed logo/font also completed before printing. Three repaired print captures supplement one valid pre-repair baseline; these overlap rather than count as four independent passing scenarios.
- The first fixture had a Windows path-escaping defect: font loading failed and fallback rendered. Its [failed transport](../EVIDENCE/certificate-font-fixture-failed-attempt.json), first source and log are retained, explicitly excluded from successful-font evidence. Corrected transport served 29,288-byte valid WOFF2 responses. The first combined callback lacked the probe's newly added image field after hot refresh; DOM observation recorded its image state, then a clean reload captured both image/font state at the callback itself.
- Eight [DOM/layout observations](../EVIDENCE/certificate-font-observations.json), including the excluded first attempt, document progress/cancellation and 390/1280 layouts. Loaded Geist body text, Georgia headings, logo, saved number/code/status and footer stayed within the certificate/page bounds. [Mobile screenshot](../EVIDENCE/certificate-font-390.png), [desktop screenshot](../EVIDENCE/certificate-font-1280.png). These are one saved theme, not all-theme/device acceptance.

## Scope and next gate

The existing isolated frontend copy and synthetic loopback API were reused. Actual cached Geist Latin bytes were loaded under QA aliases; this does not exercise the complete production Next/font preload/subset pipeline, Geist Mono, all languages or full authenticated portal. window.print was intercepted to inspect real DOM; no PDF/native preview was produced. Native PDF acceptance remains tool-limited, without another user dialog handoff. Owned QA processes were stopped, tab closed and viewport reset; normal dev services remain untouched. [Cleanup](../EVIDENCE/certificate-font-cleanup.json), [successor receipt](../EVIDENCE/certificate-font-source-receipt.json).

Earlier Astra audit and immutable appearance/browser evidence are retained, not redone. Prior 143/backend/SQL totals are historical, not summed into this run. PDF compositor/pagination/embedding, physical devices, long content/18 remaining themes, separate-read status races, full-portal auth/SQL, linked-case/critical/release gates stay OPEN. Next: certificate linked-case/full-portal coverage readiness and the remaining critical gaps, following the existing Phase 2B plan—not a fresh audit or deployment.
