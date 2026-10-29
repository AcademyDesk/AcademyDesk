# Shared select keyboard continuation — 25 October 2026

Existing BUG-UI-0002/E3 continuation. Preserve the already-implemented native
detail modal and its historical focus evidence; no modal rewrite. Application
scope is only `apps/web/src/components/standard-select-field.tsx`.

## Reproduction and implementation

Actual-source baseline: nine cases, three PASS/six FAIL at
`QA/EVIDENCE/shared-select-keyboard-baseline-20261025.tap`. This historical file
uses the test runner's default reporter despite its .tap extension; do not claim
it is TAP. Focus did not enter the selected option, every option was a tab stop,
Tab failed to dismiss, type-ahead was absent, and labelled triggers omitted the
selected text from their accessible name. Existing pointer selection worked.

The scoped repair moves focus to the selected/first option when opened, keeps
options outside the page tab sequence, closes on Tab/Shift+Tab while letting
native navigation proceed from the trigger, and implements buffered prefix and
repeated-character type-ahead. Existing popup Arrow/Home/End/Escape handling and
native Enter/Space activation are retained. Selection still commits only on
activation, never while browsing. Visible label plus selected value name the
trigger. Modifier shortcuts, IME composition and disabled controls are preserved.

This follows the [WAI listbox interaction guidance](https://www.w3.org/WAI/ARIA/apg/patterns/listbox/).
It is not a claim of full WCAG, screen-reader speech or device acceptance.

## Bounded validation accepted

- [x]Nine actual-source keyboard cases PASS; five required/disabled/payload
  composition cases retained. Test mocks add inert useEffect only where those
  tests do not claim focus behavior; keyboard tests execute the real effect.
- [x]Combined63cases PASS: nine keyboard/five required/23Access Review/26Staff.
  `QA/EVIDENCE/shared-select-handlers-20261025.tap`; zero failures/skips.
- [x]Fresh source-bound typecheck/lint/627file build
  `b685d1caacc241adbd32acd7b16692b4` PASS. UI envelope
  `2e6a10b2410142f183d546379a04d7cf` remains FAIL: first browser packet
  `1261c23a7cd64e9e8090cb65194c97c5` passed two journeys then timed out after
  full-page capture dismissed the popup during host scrolling/resize. Native
  pointer/keyboard assertions were not relaxed. Viewport capture now checks that
  the popup stays visible before activating the same option.
- [x]Browser-only successor `5bb52c9ef1aa4d1d92a8a9712ff1c58a` /
  browser `0c1858bfcd5d4997863b95a95f4cf0b3` PASS8/8 at320/390/768/1440
  light-dark; all eight PNGs inspected. No app change/rebuild between attempts.
- [x]Build window `e4397e8082534cac81fc633ec6a88b21` remains FAIL with exact
  originals/inventory restored. Browser successor window
  `15dac26542a2469f8d04cbec00fc57fe` PASS/exact originals/inventory/lease
  release/API health200. First window
  `ca9187590fd34a9c9fada46d9aa2ea5d`/UI
  `3d8f355c70474ba385a0deb3383d7b10` FAIL before any child step: unchanged3GiB
  guard refused. Exact original services/full inventory restored. New helper
  permits a bounded30second wait for that same guard, never lowering it.
- [x]`node QA/tools/shared-select-qualification.cjs` PASS, explicitly binding
  only the successful build steps from the failed envelope to the fresh complete
  browser successor. Browser SHA256
  `13c40bc933e494316f3ebbc2ec484b385c42021ca5a306d510b6ec48e85ad27f`;
  successor-window SHA256
  `e06eb3c0301beb08d55b05b08bc4a66729ba03c3023fa645faeaf0f13b0cd802`.

Screenshot inspection exposed a separate320px theme-label collision, continuing
as a narrow shell-layout packet. It does not invalidate the measured select
keyboard/selection/layer checks and is not claimed repaired by this report.

The prior native Access Review QA locator now targets the exact userId field
with label+value naming. No business/API assertions changed, no new native SQL
execution claimed for this locator update. No API/package/model/main/Mini/Azure/
production database changes. Broader modal/nested-control/assistive-technology/
physical-device/release cases remain OPEN. Medium is sufficient for this settled
component work; High for consequential backend or authorization integration.
