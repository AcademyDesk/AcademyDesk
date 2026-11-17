# C49 — private-file preview lifetime

Cloud / Sol High, 17 November 2026, Asia/Calcutta.
Branch: `codex/cloud-c49-private-preview-scope-20261117`.
Clean base: Desktop `f1f62158eb692564d50aeb60a36dad3c109b9c45`, freshly
fetched. C48 is preserved at `67f24f79d1ab6ca3fc5b81b215b4637cdfefa312`.

## Published ownership before implementation

Only these paths may change:

- `apps/web/src/lib/private-material.ts`: integrate C48 byte-exact, no new edits.
- `QA/tools/private-material-boundaries.test.cjs`: integrate C48 byte-exact.
- `QA/HANDOFF/C48_PRIVATE_MEDIA_BOUNDARIES_2026-11-17.md`: original C48 handoff.
- `apps/web/src/components/private-material-actions.tsx`: bind preview, pending
  action, message and native target lifetime to the rendered private file.
- `QA/tools/private-material-actions-browser.cjs`: new isolated actual-component
  React/Chromium regression runner, synthetic transport only.
- this C49 handoff.

Reproduce the old preview/message surviving a URL change in real Chromium first.
Use the smallest component-lifetime fix, retaining current cancellation and
server authorization. Verify late replies, replacement files, close/unmount,
external links and the native target cleanup. No new package or application
fixture route; the browser runner may create only owned temporary QA fixtures.

Desktop owns real Identity/SQL/private storage, migrations, CI credentials and
workflows, live browser/device/staging acceptance and Mini original-byte checks.
No edits to those paths, shared roots, session transport, dependency manifests,
domain controllers, models, vendor or raw original evidence are claimed here.
Do not integrate the private C45 comparison. Preserve C48's native handoff and
all newer Desktop fixes, including the payroll access fixture and paired job.

Run the new actual-component browser regressions, retained media tests, types,
lint and exact-lock build. Keep synthetic browser results distinct from SQL,
provider, physical-device or model qualification. BUG-SEC-0001 and all six
critical acceptance records stay open pending their full closure protocols.

No main merge, Azure deployment, production database use, environment publication,
public-access change or model download/loading/training/inference/activation.
Mini/Plus/Pro remain unqualified; Ultra deferred;90% coverage remains unmeasured.

Status: claimed before integration and implementation; results pending.
