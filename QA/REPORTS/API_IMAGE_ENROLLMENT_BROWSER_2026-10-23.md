# Native enrollment save, duplicate and lifecycle — 23 October 2026

PASS36 retained native SQL/API setup/security/seed checks plus four real-browser
journeys,390/1440px ×light/dark. SQL `3a58c2f1f1234d2cac12f1d3741cab37`,
resource window `2e396cf0951042139badab5e5b812200`, both PASS including finally.
Owner account creates through the actual Manual form; this does not claim every
administrative role or enrollment policy has been accepted.

Same API image `sha256:afc653e56a4231451fd53713bbf6fbf1188db8df4d4bdd382d8b87a2e4e94a70`
and627-file frontend candidate `293e5beba439453794e6c1e68adc2b71`.
No application code/authority/business-rule change or rebuild. QA tools/docs only,
branch `codex/penta-search`, HEAD `b1f8cb6f4209da7fbd14f8089fcb6792438684c4`.

## Exact native outcomes

- Four synthetic students and one open in-person batch/course seeded through
  real domain APIs. Visible actual login/form submits; no injected bearer, mocked
  API result or external service. Null optional start date keeps server default.
- Per journey, actual statuses exactly201create,409duplicate,200pause,200resume.
  Pending duplicate-submit event produces only one initial POST. A later deliberate
  same-student/batch create gets409 and visible duplicate explanation; SQL/audit
  unchanged by rejection. This is not a concurrent admission-race qualification.
- Blank-space lifecycle reason is rejected by the existing form before any PUT;
  cancel discards the pending status without writing. Actual picker selections
  collapse; status remains Active until a reviewed save. Pause trims the supplied
  reason; resume clears it and keeps end date SQLnull. Real register readback
  shows each committed status, not only a success banner.
- Each journey has one row and three success audits. Final four Active rows,
  null lifecycle reason/end date; exact student/batch IDs and valid start dates.
  Eleven domain/Identity table hashes unchanged after seed. Sixteen captured
  writes are independently checked for exact verbs/paths/bodies/status order;
  four unique students/rows. Two academies/five identities/zero payments retained.
- No429/5xx/page exceptions/document overflow. Four PNGs retained; selected
 390dark inspected and readable. Not all screen-state/accessibility/device proof.
-109migration histories/current API source pins retained. Admission3.023GiB with
  unchanged3GiB guard; only owned SQL/API/network removed. Exact two originals
  restored healthy/full29containers/41volumes/four networks/ten images preserved;
  exclusive validation lease released. No engine restart/prune/model/Git/Azure.

SQL receipt SHA256 `a6fca59eb5a3980409db45a9b90d3043df292b7310e90e5d4f462d561326a4d3`.
Window SHA256 `cccac6d84cf5da228f62d838d09c3c9bda07a67ab6627a27f5816249f7616ffc`.
Read-only `qualifyNative('native-enrollment-browser', SQLrun, windowRun,
{build:'293e5beba439453794e6c1e68adc2b71',files:627})` PASS, including all four
PNG hashes. The stronger student loaded-profile successor now PASS; full-chain
`node QA/tools/current-native-successor.cjs` verifies19receipts/56PNG/fourCSV
hashes. Old loading-state screenshots do not satisfy the stronger student gate.

141focused QA-tool tests PASS before final qualification strengthening; three
enrollment negative-control tests and actual receipt then passed the stronger
sixteen-write oracle. Remaining prerequisites/transfers/waitlists/capacity and
activation races, all roles/devices, security/release and applicable issue gates
remain OPEN. Existing issues are not silently closed. Full AI is not delivered
by a Manual browser test. All11broader packages/production gates remain OPEN.
Stay High for consequential host/domain integration; Medium for settled visuals.
