# Admin compact theme layout — 25 October 2026

Existing E3 Manual shell continuation after expiry and select keyboard acceptance.
Only application diff for this packet is two selectors in
`apps/web/src/components/manual-workspace.module.css`.

## Confirmed baseline and repair

Eight select screenshots exposed theme labels colliding at320px. A guarded
diagnostic against the unchanged source-pinned export reproduced both labels
painting beyond their44px button bounds. Buttons themselves were distinct and
inside the group; this is text collision, not a claimed overlapping hit-target
or authority defect. Diagnostic `a32b3e11b7fc49b98b2ce6eb492c09cf`, raw browser
`60e6e956546d4f35b10a8a9443d501d7`, parent
`9a3ad34d73df4a22a5fd6babb9dfaf22` PASS expected reproduction/exact original
restoration/full inventory. Raw browser remains FAIL/zero journeys, not UI PASS.
An initial direct browser attempt was refused by the unchanged RAM guard before
starting; no browser receipt or acceptance inferred from that attempt.

The existing <=360px compact rule intended to hide only the painted Light/Dark
text while retaining glyphs and explicit accessible names. Later PENTA Manual
theme typography won equal specificity and restored text into a narrow grid.
Scope the existing rules to `.enterprise-utilities` and remove the internal gap
at that breakpoint; keep button names,44px targets, icons, theme storage and
all application behavior unchanged. No JavaScript, role or backend change.

## Frontend and linked native retest accepted at bounded scope

- [x]Exact native-browser baseline reproduced paint overflow before repair.
- [x]Fresh UI `ac009cf087bf4114a0a8e4e58ae179cf`, web build
  `caa9cdd48b6a43b7abb96d81f768124e`, window
  `402c768e52504bdcb1632607ab99bef7` PASS63retained handlers/types/component
  lint/fresh CSS-processed627file export/eight viewport-theme keyboard journeys.
- [x]Each theme button44px, painted text within button, buttons within group,
  distinct hit targets and actual keyboard light/dark/local-storage changes.
- [x]Retained select keyboard/commit/cancel/Tab/layer/pointer cases, eight PNGs
  inspected and independent exact-source/export/resource qualifier.
  `node QA/tools/compact-theme-qualification.cjs` PASS. Other six screenshot
  hashes exactly match the preceding select packet; only320px presentation changed.
  Browser `5ac10579051b458c810925938816991a` SHA256
  `d9d4506ff4d52b1a3b2e6433db3fa4b8f74a706976c2da8b28e7bff30aff41e8`;
  window SHA256
  `324f60195aab8b002b77fafa30a61aa936b671735fc13c0a19bc8d525accd290`.
- [x]Current-export real login/Access Review grant/revoke SQL/browser successor,
  existing audit-fault/role/session/ledger assertions unchanged.
  First window `d69e51159ed44290b3743e5f0afb83ee` /
  SQL `be8c99ec038d4078a06c4b426676be71` FAIL after33HTTP checks/zero browser
  journeys: the updated QA userId-field locator incorrectly nested a form-scoped
  `has` locator. Timeout points to its first click, before grant mutation. Corrected
  that inner locator to page-relative, retaining the exact userId field/name and
  every domain/audit assertion. Owned cleanup/exact originals/inventory PASS.
  Successor window `3c5f0f5d781044adb7e46bade23def8b` /
  SQL `17ba5742aea249768b276f56e5d27d78` PASS53HTTP/four actual browser
  journeys/two audit-failure rollback controls/four grant rows/eight attributed
  success audits/same Staff bearer403→200→403. No429/page errors. Four PNGs
  inspected; owned cleanup/exact original service IDs/images/inventory/lease
  release PASS. No application change, backend rebuild or weakened assertion
  between attempts. PENTA API health200 checked after restoration.
- [x]`node QA/tools/shared-control-native-qualification.cjs
  17ba5742aea249768b276f56e5d27d78 3c5f0f5d781044adb7e46bade23def8b`
  PASS: exact latest627file frontend/65file API image/retained2982API baseline/
  full SQL migration and current authoritative browser/audit evidence. API suite
  was retained, not repeated for this frontend-only change.
  SQL SHA256
  `e899c1566095ec2073cf9ad001b892d4fc7e27afdbdc246aa0a0a4882273fb6d`;
  window SHA256
  `90be3e536d7c5f46ed52315e48056e1fe9e7bafecb72ebfb39d35546d7e675f9`.

The current native QA adapter accepts only an explicitly source-pinned completed
compact-theme UI packet; it does not use an incomplete export or mock API data.
Older completed expiry/select proofs remain historical exact-source acceptance.
Whole all-portal/device/screen-reader/AI/release gates remain OPEN. Medium suits
this CSS repair; High suits the linked authority/audit/SQL retest. No new package,
model acquisition/activation, main/Mini edit, commit/push/Azure/customer data.
