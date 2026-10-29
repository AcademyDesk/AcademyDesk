# Shared calendar/theme quality repair —21 October2026

Local `codex/penta-search`,unchangedHEADb1f8cb6. This follows the existing
QUALITY-LINT-001 gate,not a restarted portal audit.

`ThemeToggle` now uses the same React external-store hydration boundary already
used by the PENTA Manual theme instead of a mount-effect state update. Server and
first hydration omit pressed state;the client then reflects the verified theme.
ThemeProvider,storage key,system preference and selected PENTA tokens are unchanged.

`StandardDateField` derives the viewed month from its controlled source value and
local navigation. An external date replacement is reflected directly without a
state-sync effect. Today highlighting is refreshed when the calendar is opened,
not copied into state by a mount effect. Direct month/year selection,1900through
current-year+25 options,hidden form value,required clear guard,disabled popup,
date choice/closure,Today and anchored overlay implementation are retained.
No date/UTC/backend business rule or native time-control change.

11actual-component controlled-hook checks PASS. Before8/11PASS,3FAIL in this
**no-mount-sync adapter**;not a claim of3previous live browser defects. Two were
client hydration checks and one immediate controlled replacement without syncing
effects. Existing whole-app lint independently identified3synchronous-effect
errors in these two files;all3removed without rule suppression. Source snapshots
and logs under `.build-check/session-resume/{date-before.tsx,theme-toggle-before.tsx,date-theme-baseline.log,date-theme-final.log}`.

The practice-page browser successor exercises actual hydrated theme pressed state,
calendar direct year/month selection,reopening at the selected year,selection
and Escape across320/1440/light/dark;see its report for final receipt. This is
synthetic-API browser proof,not physical device/cross-timezone/civil-calendar
exhaustiveness or full-portal sign-off. Earlier shared-control/anchoring reports
remain retained. No vendor token,framework config,package,domain or API change.

Full configured frontend lint went27errors/41warnings→19errors/40warnings after
these3errors and the5explicit-any practice-page errors were repaired. The two
receipts are `.build-check/session-resume/lint-current.json` and
`lint-after-controls.json`. **Gate still FAILS** on19errors across9other files;
no issue/enterprise/release closure. Next Medium:bounded remaining quality/UI
work;High only for consequential session/domain/authority integration.
