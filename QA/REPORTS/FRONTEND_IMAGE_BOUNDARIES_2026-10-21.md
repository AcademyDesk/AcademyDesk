# Image boundaries and zero-warning checkpoint — 21 October 2026

Repository `D:\AcademyDesk-codex-p0`, branch `codex/penta-search`, unchanged HEAD
`b1f8cb6f4209da7fbd14f8089fcb6792438684c4`. Successor to the callback warning batch;
not a restarted audit or production acceptance.

## Delivered

- Migrated all 14 warned image sites in ten existing files to Next Image: portal
  avatars, Platform account photo, certificate branding/print logo, invoice and
  document-template logo/signature, private preview and teacher attachment preview.
- Explicit `unoptimized` retains original browser URLs. No optimizer, remote-pattern
  allowlist, new proxy, authentication bypass, global image config or lint suppression.
  This is not a claim of image compression, lower network bytes or secure URL policy.
- Explicit dimensions reserve avatar/branding geometry. Images load eagerly so
  print preparation is not dependent on lazy visibility. Signatures preserve auto
  height; private/local previews use bounded relative containers and contain-fit.
- Certificate image ref still reaches the real image; existing fresh-record,
  image-decode, font-readiness and print approval guards remain. Private fetch,
  byte/MIME checks, blob cleanup, download fallback and recording/upload handlers
  are unchanged. Audio/video paths remain intact. No domain/API/security change.

## Verified

- Full configured ESLint **0 errors / 0 warnings, exit 0**:
  `.build-check/session-resume/lint-image-final.json`. Previous 14 warnings were
  not suppressed. No ESLint, package or Next configuration changed.
- TypeScript and Next webpack build/export **87/87 routes PASS**:
  `.build-check/session-resume/image-build.log`.
- **438 selected checks PASS**, zero fail/skip/cancel:
  `.build-check/session-resume/image-regression-final.log`.
  Includes 27 new image-prop/source/guard checks plus actual certificate printing,
  certificate image decode, safe material/client protocol, recorder, lifecycle,
  Teacher publication, governed finance recovery and invoice/payment safety tests.
  Overlaps the prior 455/478 selections; do not sum these checkpoints.
- **172 exported-browser cases PASS** with synthetic APIs/tokens:

| Local evidence under `QA/EVIDENCE/` | Cases | Verified scope |
| --- | ---: | --- |
| `finance-document-preview-1792588703461/result.json` | 112 | Invoice/payslip template geometry, default/long branding, both themes, 320/390/768/1440px |
| `portal-read-state-1792588747910/result.json` | 36 | Platform, Platform Control, Teacher read/denied/malformed states, selection and print dialogs; both themes, 320/1440px |
| `image-boundaries-1792589036228/result.json` | 24 | Platform/Control, Teacher, Student, Certificates and Invoices; actual image decoding, both themes, 320/1440px |

The image-specific browser run uses actual exported Next Image, not a mocked image
component. It verifies original-URL/eager/no-srcset rendering and no `/_next/image`
requests; decodes avatar/owner images; opens the Teacher class through visible
controls, previews an authenticated-transport synthetic private PNG, confirms blob
revocation on close, and selects a local PNG for upload preview without uploading.
It selects a saved certificate, verifies the real image ref completes the existing
decode/print-preparation path, captures a stubbed `window.print` call and closes via
afterprint. Invoice logo/signature decode under screen/print CSS. No PDF file saved,
real OS print dialog, physical device, real SQL or server authorization acceptance.
Document geometry cases use synthetic branding; not every arbitrary image format.

## Incomplete runs retained, not counted as passed

- Initial controlled adapters did not expose Next Image or existing certificate
  image/font imports. Added image stubs only to controlled TSX fixtures; existing
  assertions unchanged. Certificate adapters load the real image/font helpers and
  use an explicit resolved synthetic FontFaceSet. Real browser proof remains separate.
- Initial browser fixtures missed the Teacher payments read, did not explicitly
  select the desktop class, used a data URL where certificates expect a relative
  API logo path, omitted the Certificates entitlement, and omitted nullable saved
  certificate notes. Corrected those fixtures rather than weakening host locks,
  printing validation or permission checks. Incomplete receipts retained at
  `image-boundaries-1792588774987`, `1792588799100`, `1792588868277`,
  `1792588905438`, `1792588973883`; only final `1792589036228` is accepted.
- `.build-check/session-resume/image-regression-initial.log` retained; adapted and
  final receipts are separate. No failed evidence file rewritten as a pass.

## Boundaries and next route

BUG-FUNC-0004 remains OPEN pending normal review/critical regression/issue closure,
although the lint gate now passes with zero warnings. Model semantic qualification,
linked PENTA host/domain authorization/approval/audit evidence, remaining enterprise,
device, operational and release gates are OPEN; BUG-DATA-0001 remains OPEN.
Zero warnings is not full visual parity, complete AI integration or production approval.

Main untouched at `f7af512f884ca5fe8ac3ddb287c852492592e709`; staging empty;
QA/EVIDENCE local/untracked; `.build-check` ignored. Docker unchanged: 28 containers,
41 volumes, only two original PENTA services running. No commit/push/Azure/database/
model activation/training/download/resource changes. Prior dirty work preserved.

Next High: resume the existing qualified-private-model/paired PENTA handoff and
consequential linked host/domain gates, with no unchanged failed-model rerun or
unapproved training/resources. Medium remains suitable for settled UI/evidence.
The current 11 E0–E10 broad packages are not 11 small fixes or a completion date.
